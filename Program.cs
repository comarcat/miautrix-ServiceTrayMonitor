using ServiceTrayMonitor.Forms;
using ServiceTrayMonitor.Licensing;

namespace ServiceTrayMonitor
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            LicenseManager licensing;
            try
            {
                licensing = LicenseManager.CreateDefault();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"The license state couldn't be read:\n{ex.Message}\n\nService Tray Monitor will now close.",
                    "License", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (licensing)
            {
                // Paid edition: nothing starts until the license lets the app run.
                if (!ActivationForm.EnsureLicensed(licensing))
                    return;

                // No visible window on startup — the app lives in the tray.
                Application.Run(new TrayAppContext(licensing));
            }
        }
    }
}
