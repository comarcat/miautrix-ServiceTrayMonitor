namespace ServiceTrayMonitor
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

			// No visible window on startup — the app lives in the tray.
			Application.Run(new TrayAppContext());
        }
    }
}
