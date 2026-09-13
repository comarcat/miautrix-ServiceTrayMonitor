using ServiceTrayMonitor.Models;
using ServiceTrayMonitor.Services;
using Xunit;

namespace ServiceTrayMonitor.Tests
{
    /// <summary>
    /// Runs against a temporary config folder. ConfigManager is static, so every test that
    /// touches it lives in this one class (xUnit runs a class's tests sequentially).
    /// </summary>
    public class ConfigManagerTests : IDisposable
    {
        private readonly string _originalDir = ConfigManager.ConfigDir;
        private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "STM-Tests-" + Guid.NewGuid().ToString("N"));

        public ConfigManagerTests()
        {
            ConfigManager.ConfigDir = _tempDir;
        }

        public void Dispose()
        {
            ConfigManager.ConfigDir = _originalDir;
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        private static StoredCredential SampleCredential() =>
            new() { Domain = "CONTOSO", Username = "svc-admin", Password = "P@ss-w0rd!" };

        // DEF-001: saving the monitored-services window wiped a credential saved after startup.
        [Fact]
        public void SaveGeneralSettings_KeepsCredentialSavedSinceStartup()
        {
            var loadedAtStartup = ConfigManager.Load();          // what TrayAppContext holds
            ConfigManager.SaveCredential(SampleCredential());   // user saves a credential later

            loadedAtStartup.MonitoredServiceNames = new List<string> { "Spooler" };
            loadedAtStartup.PollIntervalSeconds = 10;
            var saved = ConfigManager.SaveGeneralSettings(loadedAtStartup);

            var credential = ConfigManager.LoadCredential();
            Assert.Equal("CONTOSO", credential.Domain);
            Assert.Equal("svc-admin", credential.Username);
            Assert.Equal("P@ss-w0rd!", credential.Password);
            Assert.Equal(new[] { "Spooler" }, saved.MonitoredServiceNames);
            Assert.Equal(10, ConfigManager.Load().PollIntervalSeconds);
        }

        [Fact]
        public void ClearCredential_KeepsGeneralSettings()
        {
            ConfigManager.SaveGeneralSettings(new AppSettings { MonitoredServiceNames = new() { "W32Time" }, PollIntervalSeconds = 30 });
            ConfigManager.SaveCredential(SampleCredential());

            ConfigManager.ClearCredential();

            var settings = ConfigManager.Load();
            Assert.False(ConfigManager.LoadCredential().IsConfigured);
            Assert.Equal(new[] { "W32Time" }, settings.MonitoredServiceNames);
            Assert.Equal(30, settings.PollIntervalSeconds);
        }

        [Fact]
        public void Password_IsNotStoredInPlainText()
        {
            ConfigManager.SaveCredential(SampleCredential());

            string json = File.ReadAllText(Path.Combine(_tempDir, "config.json"));
            Assert.DoesNotContain("P@ss-w0rd!", json);
            Assert.Contains("CredentialEncryptedPasswordBase64", json);
        }

        // DEF-010: a corrupt config.json was silently replaced by defaults on the next save.
        [Fact]
        public void CorruptConfig_IsBackedUp_AndDefaultsReturned()
        {
            Directory.CreateDirectory(_tempDir);
            File.WriteAllText(Path.Combine(_tempDir, "config.json"), "{ this is not json");

            var settings = ConfigManager.Load();

            Assert.Empty(settings.MonitoredServiceNames);
            Assert.Equal(5, settings.PollIntervalSeconds);
            Assert.NotNull(ConfigManager.LastLoadWarning);
            Assert.False(File.Exists(Path.Combine(_tempDir, "config.json")));
            var backup = Assert.Single(Directory.GetFiles(_tempDir, "config.json.corrupt-*"));
            Assert.Equal("{ this is not json", File.ReadAllText(backup));
        }

        [Fact]
        public void Save_LeavesNoTempFileBehind()
        {
            ConfigManager.SaveGeneralSettings(new AppSettings());

            Assert.True(File.Exists(Path.Combine(_tempDir, "config.json")));
            Assert.False(File.Exists(Path.Combine(_tempDir, "config.json.tmp")));
        }
    }
}
