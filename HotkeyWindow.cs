using static BorderlessApp.NativeMethods;

namespace BorderlessApp;

/// <summary>Invisible message-only window that receives the global hotkey.</summary>
internal sealed class HotkeyWindow : NativeWindow, IDisposable
{
    private const int HotkeyId = 1;
    private bool _registered;

    public event EventHandler? Pressed;

    public HotkeyWindow() => CreateHandle(new CreateParams { Parent = HWND_MESSAGE });

    public bool Register(Hotkey hotkey)
    {
        Unregister();
        _registered = RegisterHotKey(Handle, HotkeyId, hotkey.Modifiers | MOD_NOREPEAT, (uint)hotkey.Key);
        return _registered;
    }

    public void Unregister()
    {
        if (_registered)
        {
            UnregisterHotKey(Handle, HotkeyId);
            _registered = false;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY && m.WParam == HotkeyId)
            Pressed?.Invoke(this, EventArgs.Empty);
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        Unregister();
        DestroyHandle();
    }
}
