namespace BorderlessApp;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        using var mutex = new Mutex(true, @"Local\BorderlessApp.SingleInstance", out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show("BorderlessApp is already running. Look for its icon in the system tray.",
                "BorderlessApp", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Application.Run(new TrayContext());
    }
}
