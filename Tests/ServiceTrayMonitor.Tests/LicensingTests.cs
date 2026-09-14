using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Miautrix.Licensing.Client;
using ServiceTrayMonitor.Licensing;
using Xunit;

namespace ServiceTrayMonitor.Tests
{
    /// <summary>Builds license files the way LicensingApi signs them (integration reference §5), with a test key pair.</summary>
    internal static class TestLicenses
    {
        private static readonly XNamespace Ns = "urn:licensing:v1";
        private static readonly RSA ServerKey = RSA.Create(2048);

        public static readonly Guid ActivationId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

        public static RSA PublicKey()
        {
            var key = RSA.Create();
            key.ImportRSAPublicKey(ServerKey.ExportRSAPublicKey(), out _);
            return key;
        }

        public static string Create(string licenseKey, Guid installGuid, string status, DateTime issuedAtUtc,
            DateTime? subscriptionExpiryUtc = null, RSA? signer = null)
        {
            var xml = new XElement(Ns + "LicenseActivation",
                new XElement(Ns + "LicenseKey", licenseKey),
                new XElement(Ns + "ActivationId", ActivationId),
                new XElement(Ns + "InstallGuid", installGuid),
                new XElement(Ns + "Status", status),
                new XElement(Ns + "Hardware",
                    new XElement(Ns + "CpuId", "CPU"),
                    new XElement(Ns + "MotherboardSerial", "MB"),
                    new XElement(Ns + "TpmId", "TPM"),
                    new XElement(Ns + "MacAddressPrimary", "00:11:22:33:44:55")),
                new XElement(Ns + "Policy",
                    new XElement(Ns + "CheckIntervalHours", 6),
                    new XElement(Ns + "GraceDays", 15),
                    new XElement(Ns + "SubscriptionGraceDays", 30)),
                new XElement(Ns + "SubscriptionExpiryUtc", subscriptionExpiryUtc?.ToString("O") ?? string.Empty),
                new XElement(Ns + "IssuedAtUtc", issuedAtUtc.ToString("O")));

            byte[] canonical = Encoding.UTF8.GetBytes(xml.ToString(SaveOptions.DisableFormatting));
            byte[] signature = (signer ?? ServerKey).SignData(canonical, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            xml.Add(new XElement(Ns + "Signature", Convert.ToBase64String(signature)));

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(xml.ToString(SaveOptions.DisableFormatting)));
        }

        public static ApiResult Success(ResultCode code, string licenseFile) => new()
        {
            Success = true,
            Code = code,
            Data = new ActivationResultData
            {
                ActivationId = ActivationId,
                LicenseFileBase64 = licenseFile,
                Policy = new PolicyDto { CheckIntervalHours = 6, GraceDays = 15, SubscriptionGraceDays = 30 },
            },
        };

        public static ApiResult Failure(ResultCode code, string? reason = null) => new()
        {
            Success = code == ResultCode.Locked,
            Code = code,
            Data = reason == null ? null : new ActivationResultData { Status = LicenseStatus.Locked, Reason = reason },
        };
    }

    internal sealed class FakeLicensingApi : ILicensingApi
    {
        private readonly Queue<Func<ApiResult>> _responses = new();

        public List<object> Requests { get; } = new();

        public void Reply(ApiResult result) => _responses.Enqueue(() => result);

        public void Fail(Exception exception) => _responses.Enqueue(() => throw exception);

        public Task<(int HttpStatus, ApiResult Result)> ActivateAsync(ActivateRequest request, CancellationToken ct = default) => Next(request);

        public Task<(int HttpStatus, ApiResult Result)> CheckinAsync(CheckinRequest request, CancellationToken ct = default) => Next(request);

        private Task<(int HttpStatus, ApiResult Result)> Next(object request)
        {
            Requests.Add(request);
            return Task.FromResult((200, _responses.Dequeue()()));
        }
    }

    public class LicenseKeyTests
    {
        [Theory]
        [InlineData("BRCX-IH9BC-X9ZV-KQXC-2NLY-CO03-8P", true)]
        [InlineData("brcx-ih9bc-x9zv-kqxc-2nly-co03-8p", true)]
        [InlineData("  BRCX-IH9BC-X9ZV-KQXC-2NLY-CO03-8P ", true)]
        [InlineData("BRCX-IH9BC-X9ZV-KQXC-2NLY-CO03", false)]
        [InlineData("BRCX-IH9B-X9ZV-KQXC-2NLY-CO03-8P", false)]
        [InlineData("BRCX_IH9BC_X9ZV_KQXC_2NLY_CO03_8P", false)]
        [InlineData("", false)]
        public void KeyFormat_MatchesIntegrationReference(string raw, bool valid) =>
            Assert.Equal(valid, LicenseManager.KeyFormat.IsMatch(LicenseManager.NormalizeKey(raw)));

        [Fact]
        public void MaskKey_ShowsOnlyFirstAndLastGroup() =>
            Assert.Equal("BRCX-•••••-••••-••••-••••-••••-8P", LicenseManager.MaskKey("BRCX-IH9BC-X9ZV-KQXC-2NLY-CO03-8P"));
    }

    // CR-004: key activation with a 7-day window for unconfirmed licenses.
    public class LicenseManagerTests
    {
        private const string Key = "BRCX-IH9BC-X9ZV-KQXC-2NLY-CO03-8P";
        private const string OtherKey = "ZZZZ-ZZZZZ-ZZZZ-ZZZZ-ZZZZ-ZZZZ-ZZ";
        private static readonly Guid Install = Guid.Parse("8b1e6f2a-1111-2222-3333-000000000001");
        private static readonly DateTime Start = new(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);

        private readonly FakeLicensingApi _api = new();
        private DateTime _now = Start;
        private int _saves;

        private LicenseManager Create(LicenseState? state = null) =>
            new(state ?? new LicenseState { InstallGuid = Install }, _api,
                () => new HardwareInfo { CpuId = "CPU", MotherboardSerial = "MB", TpmId = "TPM", MacAddressPrimary = "00:11:22:33:44:55" },
                TestLicenses.PublicKey(), () => _now, _ => _saves++, "1.0.1-p");

        private string LicenseFileFor(string status, DateTime? issued = null, RSA? signer = null, DateTime? expiry = null) =>
            TestLicenses.Create(Key, Install, status, issued ?? _now, expiry, signer);

        [Fact]
        public void VendorVerifier_AcceptsTheTestLicenseFormat()
        {
            var contents = LicenseFile.VerifyAndParse(LicenseFileFor(LicenseStatus.Approved), TestLicenses.PublicKey());

            Assert.True(contents.SignatureVerified);
            Assert.Equal(Key, contents.LicenseKey);
            Assert.Equal(Install, contents.InstallGuid);
            Assert.Equal(Start, contents.IssuedAtUtc);
        }

        [Fact]
        public async Task InvalidFormat_DoesNotCallTheServer()
        {
            var manager = Create();

            var outcome = await manager.ActivateAsync("BRCX-1234");

            Assert.False(outcome.Success);
            Assert.False(outcome.Fatal);
            Assert.Empty(_api.Requests);
            Assert.Equal(LicenseAccess.NotActivated, manager.Decision.Access);
        }

        [Fact]
        public async Task Approved_IsLicensed()
        {
            var manager = Create();
            _api.Reply(TestLicenses.Success(ResultCode.Activated, LicenseFileFor(LicenseStatus.Approved)));

            var outcome = await manager.ActivateAsync(Key.ToLowerInvariant());

            Assert.True(outcome.Success);
            Assert.Equal(LicenseAccess.Licensed, outcome.Decision.Access);
            Assert.Equal(Start.AddDays(7), outcome.Decision.DeadlineUtc);
            Assert.Equal(Start, manager.State.LastVerifiedUtc);
            Assert.Equal(TestLicenses.ActivationId, manager.State.ActivationId);
            var request = Assert.IsType<ActivateRequest>(Assert.Single(_api.Requests));
            Assert.Equal(Key, request.LicenseKey);
            Assert.Equal(Install, request.InstallGuid);
            Assert.Equal("1.0.1-p", request.AppVersion);
            Assert.True(_saves > 0);
        }

        [Fact]
        public async Task PendingReview_RunsSevenDays_ThenBlocks_UntilApproved()
        {
            var manager = Create();
            _api.Reply(TestLicenses.Success(ResultCode.PendingReview, LicenseFileFor(LicenseStatus.PendingReview)));

            var outcome = await manager.ActivateAsync(Key);
            Assert.True(outcome.Success);
            Assert.Equal(LicenseAccess.Grace, outcome.Decision.Access);
            Assert.Equal(Start.AddDays(7), outcome.Decision.DeadlineUtc);

            _now = Start.AddDays(7).AddMinutes(1);
            Assert.Equal(LicenseAccess.Blocked, manager.Evaluate().Access);

            _api.Reply(TestLicenses.Success(ResultCode.Renewed, LicenseFileFor(LicenseStatus.Approved, issued: _now)));
            var approved = await manager.CheckInAsync();

            Assert.True(approved.Success);
            Assert.Equal(LicenseAccess.Licensed, approved.Decision.Access);
            Assert.Null(manager.State.GraceStartedUtc);
            Assert.IsType<CheckinRequest>(_api.Requests[1]);
        }

        [Fact]
        public async Task UnknownKey_FailsFatally_AndIsNotStored()
        {
            var manager = Create();
            _api.Reply(TestLicenses.Failure(ResultCode.LicenseNotFound));

            var outcome = await manager.ActivateAsync(Key);

            Assert.False(outcome.Success);
            Assert.True(outcome.Fatal);
            Assert.Contains("doesn't exist", outcome.Message);
            Assert.Equal(string.Empty, manager.State.LicenseKey);
            Assert.Equal(LicenseAccess.NotActivated, manager.Decision.Access);
        }

        [Fact]
        public async Task ServerUnreachable_RunsSevenDays_ThenBlocks()
        {
            var manager = Create();
            _api.Fail(new HttpRequestException("No such host is known."));

            var outcome = await manager.ActivateAsync(Key);
            Assert.True(outcome.Success);
            Assert.False(outcome.Fatal);
            Assert.Equal(LicenseAccess.Grace, outcome.Decision.Access);
            Assert.Equal(Key, manager.State.LicenseKey);
            Assert.Null(manager.State.ActivationId);

            _now = Start.AddDays(7).AddMinutes(1);
            Assert.Equal(LicenseAccess.Blocked, manager.Evaluate().Access);
        }

        [Fact]
        public async Task CheckIn_BeforeActivationReachedServer_RetriesActivation()
        {
            var manager = Create();
            _api.Fail(new TaskCanceledException());
            await manager.ActivateAsync(Key);

            _now = Start.AddHours(2);
            _api.Reply(TestLicenses.Success(ResultCode.PendingReview, LicenseFileFor(LicenseStatus.PendingReview)));
            var outcome = await manager.CheckInAsync();

            Assert.True(outcome.Success);
            Assert.IsType<ActivateRequest>(_api.Requests[1]);
            Assert.Equal(Start, manager.State.GraceStartedUtc);   // the window started at the first attempt
        }

        [Fact]
        public async Task NewKeyWhileBlockedAndOffline_DoesNotRestartTheWindow()
        {
            var manager = Create();
            _api.Fail(new HttpRequestException("offline"));
            await manager.ActivateAsync(Key);

            _now = Start.AddDays(8);
            _api.Fail(new HttpRequestException("offline"));
            var outcome = await manager.ActivateAsync(OtherKey);

            Assert.False(outcome.Success);
            Assert.Equal(LicenseAccess.Blocked, outcome.Decision.Access);
            Assert.Equal(Start, manager.State.GraceStartedUtc);
        }

        [Fact]
        public async Task CheckIn_LockedAfterSubscriptionGrace_Blocks()
        {
            var manager = Create();
            _api.Reply(TestLicenses.Success(ResultCode.Activated, LicenseFileFor(LicenseStatus.Approved)));
            await manager.ActivateAsync(Key);

            _api.Reply(TestLicenses.Failure(ResultCode.Locked, "SUBSCRIPTION_EXPIRED_GRACE_ENDED"));
            var outcome = await manager.CheckInAsync();

            Assert.False(outcome.Success);
            Assert.True(outcome.Fatal);
            Assert.Equal(LicenseAccess.Blocked, outcome.Decision.Access);
            Assert.Contains("subscription expired", outcome.Decision.Summary);
        }

        [Fact]
        public async Task TamperedLicenseFile_IsRejected_AndBlocksWhenPlanted()
        {
            using var attacker = RSA.Create(2048);
            var manager = Create();
            _api.Reply(TestLicenses.Success(ResultCode.Activated, LicenseFileFor(LicenseStatus.Approved, signer: attacker)));

            var outcome = await manager.ActivateAsync(Key);
            Assert.False(outcome.Success);
            Assert.True(outcome.Fatal);
            Assert.Equal(string.Empty, manager.State.LicenseKey);

            var planted = Create(new LicenseState
            {
                InstallGuid = Install,
                LicenseKey = Key,
                ActivationId = TestLicenses.ActivationId,
                LicenseFileBase64 = LicenseFileFor(LicenseStatus.Approved, signer: attacker),
            });
            Assert.Equal(LicenseAccess.Blocked, planted.Decision.Access);
        }

        [Fact]
        public async Task LicenseFileForAnotherInstallation_IsRejected()
        {
            var manager = Create();
            _api.Reply(TestLicenses.Success(ResultCode.Activated, TestLicenses.Create(Key, Guid.NewGuid(), LicenseStatus.Approved, _now)));

            var outcome = await manager.ActivateAsync(Key);

            Assert.True(outcome.Fatal);
            Assert.Equal(LicenseAccess.NotActivated, manager.Decision.Access);
        }

        [Fact]
        public async Task Approved_WorksOfflineUpToSevenDays_ThenBlocks()
        {
            var manager = Create();
            _api.Reply(TestLicenses.Success(ResultCode.Activated, LicenseFileFor(LicenseStatus.Approved)));
            await manager.ActivateAsync(Key);

            _now = Start.AddDays(6);
            _api.Fail(new HttpRequestException("offline"));
            var offline = await manager.CheckInAsync();
            Assert.True(offline.Success);
            Assert.Equal(LicenseAccess.Licensed, offline.Decision.Access);
            Assert.Null(manager.State.GraceStartedUtc);

            _now = Start.AddDays(7).AddMinutes(1);
            Assert.Equal(LicenseAccess.Blocked, manager.Evaluate().Access);
        }

        [Fact]
        public async Task SubscriptionPastItsGrace_BlocksEvenOffline()
        {
            var manager = Create();
            _api.Reply(TestLicenses.Success(ResultCode.Activated, LicenseFileFor(LicenseStatus.Approved, expiry: Start.AddDays(-29))));

            var outcome = await manager.ActivateAsync(Key);
            Assert.Equal(LicenseAccess.Licensed, outcome.Decision.Access);

            _now = Start.AddDays(2);   // expiry + 31 days
            Assert.Equal(LicenseAccess.Blocked, manager.Evaluate().Access);
        }

        [Fact]
        public async Task ClockMovedBack_DoesNotReopenTheWindow()
        {
            var manager = Create();
            _api.Reply(TestLicenses.Success(ResultCode.PendingReview, LicenseFileFor(LicenseStatus.PendingReview)));
            await manager.ActivateAsync(Key);

            _now = Start.AddDays(8);
            _api.Fail(new HttpRequestException("offline"));
            await manager.CheckInAsync();   // records the latest time seen

            _now = Start.AddDays(1);        // clock set back
            Assert.Equal(LicenseAccess.Blocked, manager.Evaluate().Access);
        }

        [Fact]
        public async Task DifferentKeyFailure_KeepsTheCurrentLicense()
        {
            var manager = Create();
            _api.Reply(TestLicenses.Success(ResultCode.Activated, LicenseFileFor(LicenseStatus.Approved)));
            await manager.ActivateAsync(Key);

            _api.Reply(TestLicenses.Failure(ResultCode.MaxActivationsReached));
            var outcome = await manager.ActivateAsync(OtherKey);

            Assert.False(outcome.Success);
            Assert.True(outcome.Fatal);
            Assert.Contains("current license stays active", outcome.Message);
            Assert.Equal(Key, manager.State.LicenseKey);
            Assert.Equal(LicenseAccess.Licensed, manager.Decision.Access);
        }

        [Fact]
        public async Task CheckDue_EveryPolicyIntervalWhenApproved()
        {
            var manager = Create();
            _api.Reply(TestLicenses.Success(ResultCode.Activated, LicenseFileFor(LicenseStatus.Approved)));
            await manager.ActivateAsync(Key);

            _now = Start.AddHours(5);
            Assert.False(manager.IsCheckDue());
            _now = Start.AddHours(6);
            Assert.True(manager.IsCheckDue());
        }

        [Fact]
        public async Task CheckDue_HourlyWhileNotConfirmed()
        {
            var manager = Create();
            _api.Reply(TestLicenses.Success(ResultCode.PendingReview, LicenseFileFor(LicenseStatus.PendingReview)));
            await manager.ActivateAsync(Key);

            _now = Start.AddMinutes(59);
            Assert.False(manager.IsCheckDue());
            _now = Start.AddHours(1);
            Assert.True(manager.IsCheckDue());
        }
    }

    public class LicenseStateStoreTests : IDisposable
    {
        private readonly string _originalDir = LicenseStateStore.StateDir;
        private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "STM-LicenseTests-" + Guid.NewGuid().ToString("N"));

        public LicenseStateStoreTests()
        {
            LicenseStateStore.StateDir = _tempDir;
        }

        public void Dispose()
        {
            LicenseStateStore.StateDir = _originalDir;
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        [Fact]
        public void Missing_ReturnsUnactivatedStateWithNewInstallGuid()
        {
            var state = LicenseStateStore.Load();

            Assert.NotEqual(Guid.Empty, state.InstallGuid);
            Assert.Equal(string.Empty, state.LicenseKey);
        }

        [Fact]
        public void SaveAndLoad_RoundTrips()
        {
            var saved = new LicenseState
            {
                InstallGuid = Guid.NewGuid(),
                LicenseKey = "BRCX-IH9BC-X9ZV-KQXC-2NLY-CO03-8P",
                ActivationId = Guid.NewGuid(),
                GraceStartedUtc = new DateTime(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc),
                LastSeenUtc = new DateTime(2026, 9, 14, 8, 30, 0, DateTimeKind.Utc),
            };

            LicenseStateStore.Save(saved);
            var loaded = LicenseStateStore.Load();

            Assert.Equal(saved.InstallGuid, loaded.InstallGuid);
            Assert.Equal(saved.LicenseKey, loaded.LicenseKey);
            Assert.Equal(saved.ActivationId, loaded.ActivationId);
            Assert.Equal(saved.GraceStartedUtc, loaded.GraceStartedUtc);
            Assert.Equal(saved.LastSeenUtc, loaded.LastSeenUtc);
            Assert.False(File.Exists(Path.Combine(_tempDir, "license.json.tmp")));
        }

        [Fact]
        public void CorruptFile_IsBackedUp_AndActivationStartsOver()
        {
            Directory.CreateDirectory(_tempDir);
            File.WriteAllText(Path.Combine(_tempDir, "license.json"), "{ not json");

            var state = LicenseStateStore.Load();

            Assert.Equal(string.Empty, state.LicenseKey);
            Assert.False(File.Exists(Path.Combine(_tempDir, "license.json")));
            Assert.Single(Directory.GetFiles(_tempDir, "license.json.corrupt-*"));
        }
    }
}
