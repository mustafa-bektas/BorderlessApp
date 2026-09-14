using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using static BorderlessApp.NativeMethods;

namespace BorderlessApp;

/// <summary>
/// Makes windows borderless fullscreen and remembers their original state so they can be put back.
/// </summary>
internal sealed class BorderlessManager
{
    private const long RemovedStyles = WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;
    private const long RemovedExStyles = WS_EX_DLGMODALFRAME | WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE | WS_EX_STATICEDGE;
    private const long ShowStateStyles = WS_MINIMIZE | WS_MAXIMIZE;

    private static readonly string[] ShellWindowClasses = ["Shell_TrayWnd", "Shell_SecondaryTrayWnd", "Progman", "WorkerW"];

    private sealed record SavedWindow(long Style, long ExStyle, WINDOWPLACEMENT Placement, uint ProcessId);

    private readonly Dictionary<IntPtr, SavedWindow> _saved = [];

    /// <summary>Toggles borderless fullscreen on the active window. Throws <see cref="Win32Exception"/> on failure.</summary>
    public void ToggleForegroundWindow()
    {
        PruneClosedWindows();

        var hwnd = GetAncestor(GetForegroundWindow(), GA_ROOT);
        if (!IsEligible(hwnd))
            return;

        GetWindowThreadProcessId(hwnd, out var processId);
        if (_saved.TryGetValue(hwnd, out var saved) && saved.ProcessId == processId)
        {
            Restore(hwnd, saved);
            _saved.Remove(hwnd);
        }
        else
        {
            // Not tracked, or the handle was reused by a different process's window.
            MakeBorderless(hwnd, processId);
        }
    }

    public void RestoreAll()
    {
        PruneClosedWindows();
        foreach (var (hwnd, saved) in _saved)
        {
            try
            {
                Restore(hwnd, saved);
            }
            catch (Win32Exception)
            {
                // Best effort: keep restoring the others.
            }
        }
        _saved.Clear();
    }

    private void MakeBorderless(IntPtr hwnd, uint processId)
    {
        var style = GetWindowLong(hwnd, GWL_STYLE);
        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

        var placement = new WINDOWPLACEMENT { length = Marshal.SizeOf<WINDOWPLACEMENT>() };
        if (!GetWindowPlacement(hwnd, ref placement))
            throw Failure("read the window position");
        var saved = new SavedWindow(style, exStyle, placement, processId);

        // Pick the monitor before un-maximizing so the window stays where the user has it.
        var monitorInfo = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        GetMonitorInfo(MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST), ref monitorInfo);
        var bounds = monitorInfo.rcMonitor;

        try
        {
            // A maximized window would snap back to the work area and fight the new bounds.
            if (IsZoomed(hwnd))
                ShowWindow(hwnd, SW_RESTORE);

            // Re-read the styles: un-maximizing cleared WS_MAXIMIZE, and writing the old value would set it again.
            ApplyStyles(hwnd, GetWindowLong(hwnd, GWL_STYLE) & ~RemovedStyles, GetWindowLong(hwnd, GWL_EXSTYLE) & ~RemovedExStyles);

            if (!SetWindowPos(hwnd, HWND_TOP, bounds.Left, bounds.Top, bounds.Right - bounds.Left, bounds.Bottom - bounds.Top,
                    SWP_FRAMECHANGED | SWP_NOOWNERZORDER | SWP_SHOWWINDOW))
                throw Failure("resize the window");
        }
        catch (Win32Exception)
        {
            // Don't leave the window half-changed.
            try { Restore(hwnd, saved); } catch (Win32Exception) { }
            throw;
        }

        _saved[hwnd] = saved;
    }

    private static void Restore(IntPtr hwnd, SavedWindow saved)
    {
        // Keep the window's current min/max bits; SetWindowPlacement below does the state change properly.
        var style = (saved.Style & ~ShowStateStyles) | (GetWindowLong(hwnd, GWL_STYLE) & ShowStateStyles);
        ApplyStyles(hwnd, style, saved.ExStyle);
        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOOWNERZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);

        var placement = saved.Placement;
        if (IsIconic(hwnd))
        {
            // Don't pop a minimized window back up (e.g. when restoring everything on exit).
            if (placement.showCmd == SW_SHOWMAXIMIZED)
                placement.flags |= WPF_RESTORETOMAXIMIZED;
            placement.showCmd = SW_SHOWMINNOACTIVE;
        }

        if (!SetWindowPlacement(hwnd, ref placement))
            throw Failure("restore the window position");
    }

    private static void ApplyStyles(IntPtr hwnd, long style, long exStyle)
    {
        var error = SetWindowLong(hwnd, GWL_STYLE, style);
        if (error == 0)
            error = SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
        if (error != 0)
            throw Failure("change the window style", error);
    }

    private void PruneClosedWindows()
    {
        foreach (var hwnd in _saved.Keys.Where(h => !IsWindow(h)).ToList())
            _saved.Remove(hwnd);
    }

    private static bool IsEligible(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero || !IsWindowVisible(hwnd) || hwnd == GetShellWindow() || hwnd == GetDesktopWindow())
            return false;

        GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == Environment.ProcessId)
            return false;

        var className = new StringBuilder(256);
        GetClassName(hwnd, className, className.Capacity);
        return !ShellWindowClasses.Contains(className.ToString());
    }

    private static Win32Exception Failure(string action) => Failure(action, Marshal.GetLastPInvokeError());

    private static Win32Exception Failure(string action, int error) =>
        error == ERROR_ACCESS_DENIED
            ? new Win32Exception(error,
                $"Windows blocked BorderlessApp from trying to {action}. " +
                "If the game is running as administrator, run BorderlessApp as administrator too.")
            : new Win32Exception(error, $"Couldn't {action}: {new Win32Exception(error).Message}");
}
