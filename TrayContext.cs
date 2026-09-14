using System.ComponentModel;
using System.Diagnostics;

namespace BorderlessApp;

internal sealed class TrayContext : ApplicationContext
{
    private const string AppName = "BorderlessApp";

    private readonly BorderlessManager _manager = new();
    private readonly HotkeyWindow _hotkeyWindow = new();
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _hotkeyLabel = new() { Enabled = false };
    private readonly ToolStripMenuItem _autostartItem;

    public TrayContext()
    {
        _autostartItem = new ToolStripMenuItem("Start with Windows", null, (_, _) => ToggleAutostart());

        var menu = new ContextMenuStrip();
        menu.Items.AddRange(new ToolStripItem[]
        {
            _hotkeyLabel,
            new ToolStripSeparator(),
            new ToolStripMenuItem("Restore all windows", null, (_, _) => _manager.RestoreAll()),
            new ToolStripSeparator(),
            new ToolStripMenuItem("Edit settings...", null, (_, _) => EditSettings()),
            new ToolStripMenuItem("Reload settings", null, (_, _) => LoadSettings()),
            _autostartItem,
            new ToolStripSeparator(),
            new ToolStripMenuItem("Exit", null, (_, _) => ExitThread()),
        });
        menu.Opening += (_, _) => _autostartItem.Checked = Autostart.IsEnabled;

        _trayIcon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = AppName,
            ContextMenuStrip = menu,
            Visible = true,
        };

        _hotkeyWindow.Pressed += (_, _) => ToggleActiveWindow();
        LoadSettings();
    }

    private void ToggleActiveWindow()
    {
        try
        {
            _manager.ToggleForegroundWindow();
        }
        catch (Win32Exception ex)
        {
            ShowWarning(ex.Message);
        }
    }

    private void LoadSettings()
    {
        Settings settings;
        try
        {
            settings = Settings.Load();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            ShowWarning($"Couldn't read settings, using defaults. {ex.Message}");
            settings = new Settings();
        }

        if (!Hotkey.TryParse(settings.Hotkey, out var hotkey))
        {
            ShowWarning($"\"{settings.Hotkey}\" isn't a valid hotkey, using {Settings.DefaultHotkey}.");
            Hotkey.TryParse(Settings.DefaultHotkey, out hotkey);
        }

        if (_hotkeyWindow.Register(hotkey))
        {
            _hotkeyLabel.Text = $"Toggle borderless: {hotkey.Text}";
            _trayIcon.Text = $"{AppName} ({hotkey.Text})";
        }
        else
        {
            _hotkeyLabel.Text = $"Hotkey {hotkey.Text} unavailable";
            _trayIcon.Text = $"{AppName} (no hotkey)";
            ShowWarning($"{hotkey.Text} is already used by another app. Pick a different hotkey in the settings.");
        }
    }

    private void EditSettings()
    {
        try
        {
            if (!File.Exists(Settings.FilePath))
                new Settings().Save();
            Process.Start(new ProcessStartInfo("notepad.exe", $"\"{Settings.FilePath}\"") { UseShellExecute = true });
            _trayIcon.ShowBalloonTip(5000, AppName, "After saving your changes, choose \"Reload settings\" from the tray menu.", ToolTipIcon.Info);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception)
        {
            ShowWarning(ex.Message);
        }
    }

    private void ToggleAutostart()
    {
        try
        {
            Autostart.SetEnabled(!Autostart.IsEnabled);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            ShowWarning($"Couldn't change startup setting. {ex.Message}");
        }
    }

    private void ShowWarning(string message) =>
        _trayIcon.ShowBalloonTip(5000, AppName, message, ToolTipIcon.Warning);

    private static Icon LoadIcon() =>
        (Environment.ProcessPath is { } exe ? Icon.ExtractIcon(exe, 0, SystemInformation.SmallIconSize.Width) : null)
        ?? SystemIcons.Application;

    protected override void ExitThreadCore()
    {
        _manager.RestoreAll();
        _hotkeyWindow.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        base.ExitThreadCore();
    }
}
