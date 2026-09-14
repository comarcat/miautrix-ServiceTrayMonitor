using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using Miautrix.Licensing.Client;

namespace ServiceTrayMonitor.Licensing
{
    /// <summary>Result of an activation or check-in, for the windows to report.</summary>
    /// <param name="Success">The app may run (licensed or within the grace window).</param>
    /// <param name="Fatal">The server (or signature check) rejected the key or license.</param>
    public record ActivationOutcome(bool Success, bool Fatal, string Message, LicenseDecision Decision);

    /// <summary>
    /// Paid edition licensing: activates a key against the Miautrix Licensing API, checks in
    /// periodically, and keeps the saved state that LicenseEvaluator judges. Async methods are
    /// awaited from the UI thread, so DecisionChanged is raised there.
    /// </summary>
    public sealed class LicenseManager : IDisposable
    {
        public const string DefaultApiBaseUrl = "https://licensing-api.miautrix.tech";

        /// <summary>Environment variable that points the app at another licensing server (e.g. the LAN instance) for testing.</summary>
        public const string ApiUrlVariable = "STM_LICENSING_API_URL";

        /// <summary>How often to retry while the license isn't confirmed or the server was unreachable.</summary>
        public static readonly TimeSpan RetryInterval = TimeSpan.FromHours(1);

        public static readonly Regex KeyFormat = new(
            @"^[A-Z0-9]{4}-[A-Z0-9]{5}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{2}$",
            RegexOptions.CultureInvariant);

        private const string UnreachableCode = "Unreachable";

        private readonly ILicensingApi _api;
        private readonly Func<HardwareInfo> _readHardware;
        private readonly RSA _publicKey;
        private readonly Func<DateTime> _clock;
        private readonly Action<LicenseState> _save;
        private readonly string _appVersion;

        public LicenseState State { get; }

        /// <summary>The saved license file after signature verification; null when there is none.</summary>
        public LicenseFileContents? SavedLicense { get; private set; }

        public LicenseDecision Decision { get; private set; }

        public event Action<LicenseDecision>? DecisionChanged;

        public LicenseManager(LicenseState state, ILicensingApi api, Func<HardwareInfo> readHardware, RSA publicKey,
            Func<DateTime> clock, Action<LicenseState> save, string appVersion)
        {
            State = state;
            _api = api;
            _readHardware = readHardware;
            _publicKey = publicKey;
            _clock = clock;
            _save = save;
            _appVersion = appVersion;

            if (State.InstallGuid == Guid.Empty)
                State.InstallGuid = Guid.NewGuid();

            SavedLicense = string.IsNullOrEmpty(State.LicenseFileBase64) ? null : Verify(State.LicenseFileBase64);
            Decision = LicenseEvaluator.Evaluate(State, SavedLicense, Now());
        }

        public static LicenseManager CreateDefault()
        {
            string baseUrl = Environment.GetEnvironmentVariable(ApiUrlVariable) is { Length: > 0 } url ? url : DefaultApiBaseUrl;
            var publicKey = RSA.Create();
            publicKey.ImportFromPem(Miautrix.Licensing.Client.Keys.LicensingPublicKeyPem);

            return new LicenseManager(LicenseStateStore.Load(), new LicensingApi(baseUrl), HardwareFingerprint.Read,
                publicKey, () => DateTime.UtcNow, LicenseStateStore.Save, Application.ProductVersion);
        }

        /// <summary>Re-evaluates the saved state against the clock and raises DecisionChanged.</summary>
        public LicenseDecision Evaluate()
        {
            Decision = LicenseEvaluator.Evaluate(State, SavedLicense, Now());
            DecisionChanged?.Invoke(Decision);
            return Decision;
        }

        /// <summary>
        /// True when a check-in is due: every Policy.CheckIntervalHours while approved, hourly while the
        /// license isn't confirmed or the last attempt couldn't reach the server.
        /// </summary>
        public bool IsCheckDue()
        {
            bool confirmed = Decision.Access == LicenseAccess.Licensed && State.LastResultCode != UnreachableCode;
            TimeSpan interval = confirmed ? TimeSpan.FromHours(Math.Max(1, State.CheckIntervalHours)) : RetryInterval;
            return State.LastCheckAttemptUtc is not DateTime last || Now() - last >= interval;
        }

        public async Task<ActivationOutcome> ActivateAsync(string rawKey)
        {
            string key = NormalizeKey(rawKey);
            if (!KeyFormat.IsMatch(key))
                return new ActivationOutcome(false, false, "The license key format is invalid. Expected XXXX-XXXXX-XXXX-XXXX-XXXX-XXXX-XX.", Decision);

            // Replacing a license that currently lets the app run: a failure must leave it in place.
            var current = LicenseEvaluator.Evaluate(State, SavedLicense, Now());
            bool switching = current.Access is LicenseAccess.Licensed or LicenseAccess.Grace
                && !string.Equals(key, State.LicenseKey, StringComparison.Ordinal);

            State.LastCheckAttemptUtc = Now();
            ApiResult result;
            try
            {
                HardwareInfo hardware = await Task.Run(_readHardware);
                (_, result) = await _api.ActivateAsync(new ActivateRequest
                {
                    LicenseKey = key,
                    InstallGuid = State.InstallGuid,
                    Hardware = hardware,
                    AppVersion = _appVersion,
                    ClientTimestampUtc = Now(),
                });
            }
            catch (Exception ex) when (IsTransient(ex))
            {
                return HandleUnreachable(key, switching, DescribeException(ex));
            }

            return HandleResult(result, key, switching, isCheckin: false);
        }

        public async Task<ActivationOutcome> CheckInAsync()
        {
            if (string.IsNullOrEmpty(State.LicenseKey))
                return new ActivationOutcome(false, false, "Service Tray Monitor isn't activated.", Evaluate());

            // The first activation never reached the server: retry it instead of checking in.
            if (State.ActivationId is not Guid activationId)
                return await ActivateAsync(State.LicenseKey);

            State.LastCheckAttemptUtc = Now();
            ApiResult result;
            try
            {
                HardwareInfo hardware = await Task.Run(_readHardware);
                (_, result) = await _api.CheckinAsync(new CheckinRequest
                {
                    ActivationId = activationId,
                    InstallGuid = State.InstallGuid,
                    Hardware = hardware,
                    ClientTimestampUtc = Now(),
                });
            }
            catch (Exception ex) when (IsTransient(ex))
            {
                return HandleUnreachable(State.LicenseKey, switching: false, DescribeException(ex));
            }

            return HandleResult(result, State.LicenseKey, switching: false, isCheckin: true);
        }

        private ActivationOutcome HandleResult(ApiResult result, string key, bool switching, bool isCheckin)
        {
            switch (result.Code)
            {
                case ResultCode.Activated:
                case ResultCode.PendingReview:
                case ResultCode.Renewed:
                    return ApplyLicenseFile(result, key, switching, isCheckin);
                case ResultCode.RateLimited:
                case ResultCode.ServerError:
                    return HandleUnreachable(key, switching, DescribeResult(result));
                default:
                    return HandleFatal(result.Code.ToString(), DescribeResult(result), key, switching, isCheckin);
            }
        }

        private ActivationOutcome ApplyLicenseFile(ApiResult result, string key, bool switching, bool isCheckin)
        {
            ActivationResultData? data = result.Data;
            string? base64 = data?.LicenseFileBase64;
            LicenseFileContents? file = string.IsNullOrEmpty(base64) ? null : Verify(base64);

            if (data is null || file is null || !file.SignatureVerified)
                return HandleFatal("SignatureInvalid", "The licensing server's response failed signature verification.", key, switching, isCheckin);
            if (!string.Equals(file.LicenseKey, key, StringComparison.Ordinal) || file.InstallGuid != State.InstallGuid)
                return HandleFatal("LicenseMismatch", "The license returned by the server doesn't match this key and installation.", key, switching, isCheckin);

            DateTime now = Now();
            State.LicenseKey = key;
            State.ActivationId = data.ActivationId ?? file.ActivationId;
            State.LicenseFileBase64 = base64;
            State.CheckIntervalHours = data.Policy is { CheckIntervalHours: > 0 } interval ? interval.CheckIntervalHours : 6;
            State.SubscriptionGraceDays = data.Policy is { SubscriptionGraceDays: > 0 } grace ? grace.SubscriptionGraceDays : 30;
            State.ReviewDeadlineUtc = data.ReviewDeadlineUtc;
            State.BlockedReason = null;
            SavedLicense = file;

            if (file.Status == LicenseStatus.Approved)
            {
                State.LastVerifiedUtc = now;
                State.GraceStartedUtc = null;   // only a confirmed approval resets the 7-day window
            }
            else if (file.Status == LicenseStatus.PendingReview)
            {
                State.GraceStartedUtc ??= now;
            }

            var decision = Record(result.Code.ToString(), null);
            bool allowed = decision.Access is LicenseAccess.Licensed or LicenseAccess.Grace;
            return new ActivationOutcome(allowed, !allowed, decision.Summary, decision);
        }

        private ActivationOutcome HandleUnreachable(string key, bool switching, string detail)
        {
            string message = $"Couldn't reach the licensing server: {detail}";
            if (switching)
            {
                Record(UnreachableCode, message);
                return new ActivationOutcome(false, false, message + " Your current license stays active.", Decision);
            }

            if (!string.Equals(State.LicenseKey, key, StringComparison.Ordinal))
            {
                // A key whose activation never reached the server: keep it and retry. The 7-day
                // window isn't restarted by a new key — only a confirmed approval resets it.
                State.LicenseKey = key;
                State.ActivationId = null;
                State.LicenseFileBase64 = null;
                State.BlockedReason = null;
                SavedLicense = null;
            }

            if (SavedLicense?.Status != LicenseStatus.Approved)
                State.GraceStartedUtc ??= Now();

            var decision = Record(UnreachableCode, message);
            bool allowed = decision.Access is LicenseAccess.Licensed or LicenseAccess.Grace;
            return new ActivationOutcome(allowed, false, message, decision);
        }

        private ActivationOutcome HandleFatal(string code, string message, string key, bool switching, bool isCheckin)
        {
            if (switching)
            {
                Record(code, message);
                return new ActivationOutcome(false, true, message + " Your current license stays active.", Decision);
            }

            // The server rejected the license this installation holds: block it. A rejected
            // first-time key isn't stored, so the app simply stays unactivated.
            if (isCheckin || string.Equals(State.LicenseKey, key, StringComparison.Ordinal))
                State.BlockedReason = message;

            var decision = Record(code, message);
            return new ActivationOutcome(false, true, message, decision);
        }

        private LicenseDecision Record(string code, string? message)
        {
            State.LastResultCode = code;
            State.LastMessage = message;
            State.LastSeenUtc = Now();
            try
            {
                _save(State);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                State.LastMessage = $"{message} (The license state couldn't be saved: {ex.Message})".Trim();
            }
            return Evaluate();
        }

        private DateTime Now()
        {
            DateTime clock = _clock();
            return clock > State.LastSeenUtc ? clock : State.LastSeenUtc;
        }

        private LicenseFileContents Verify(string base64)
        {
            try
            {
                return Miautrix.Licensing.Client.LicenseFile.VerifyAndParse(base64, _publicKey);
            }
            catch (Exception ex) when (ex is FormatException or InvalidDataException or XmlException or CryptographicException)
            {
                return new LicenseFileContents { SignatureVerified = false };
            }
        }

        private static bool IsTransient(Exception ex) =>
            ex is HttpRequestException or TaskCanceledException or JsonException or IOException;

        private static string DescribeException(Exception ex) =>
            ex is TaskCanceledException ? "the request timed out." : ex.Message;

        /// <summary>User-facing text for a result code; switches on the code, never on the server's message text.</summary>
        internal static string DescribeResult(ApiResult result)
        {
            string text = result.Code switch
            {
                ResultCode.InvalidKeyFormat => "The license key format is invalid.",
                ResultCode.LicenseNotFound => "This license key doesn't exist.",
                ResultCode.LicenseExpired => "This license has expired.",
                ResultCode.LicenseRevoked => "This license has been revoked.",
                ResultCode.MaxActivationsReached => "This license key is already activated on the maximum number of machines.",
                ResultCode.InstallGuidMismatch => "This activation belongs to a different installation.",
                ResultCode.ActivationNotFound => "The licensing server has no record of this activation.",
                ResultCode.RateLimited => "The licensing server is receiving too many requests; try again in a minute.",
                ResultCode.ServerError => "The licensing server reported an internal error.",
                ResultCode.Locked => result.Data?.Reason switch
                {
                    "SUBSCRIPTION_EXPIRED_GRACE_ENDED" => "The subscription expired and its grace period has ended.",
                    "LICENSE_REVOKED" => "This license has been revoked.",
                    { Length: > 0 } reason => $"The license is locked ({reason}).",
                    _ => "The license is locked.",
                },
                _ => $"The licensing server returned '{result.Code}'.",
            };

            return string.IsNullOrWhiteSpace(result.Message) ? text : $"{text} Server message: {result.Message}";
        }

        public static string NormalizeKey(string? key) =>
            new string((key ?? string.Empty).Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();

        /// <summary>Shows only the first and last group, e.g. BRCX-•••••-••••-••••-••••-••••-8P.</summary>
        public static string MaskKey(string key)
        {
            string[] groups = key.Split('-');
            if (groups.Length < 3)
                return new string('•', key.Length);

            for (int i = 1; i < groups.Length - 1; i++)
                groups[i] = new string('•', groups[i].Length);
            return string.Join("-", groups);
        }

        public void Dispose()
        {
            (_api as IDisposable)?.Dispose();
            _publicKey.Dispose();
        }
    }
}
