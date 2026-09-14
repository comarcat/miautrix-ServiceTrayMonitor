using Miautrix.Licensing.Client;

namespace ServiceTrayMonitor.Licensing
{
    public enum LicenseAccess
    {
        /// <summary>No key activated on this machine.</summary>
        NotActivated,
        /// <summary>Approved and verified within the last 7 days.</summary>
        Licensed,
        /// <summary>Not yet confirmed (awaiting approval, or server not reached); allowed for 7 days.</summary>
        Grace,
        /// <summary>The app must close until the license is activated or confirmed.</summary>
        Blocked,
    }

    /// <param name="DeadlineUtc">
    /// Licensed: the license must be re-verified with the server by this time.
    /// Grace: the license must be confirmed by this time.
    /// </param>
    public record LicenseDecision(LicenseAccess Access, string Summary, DateTime? DeadlineUtc = null);

    /// <summary>License file status values (activation-dll-integration-reference.md §3).</summary>
    public static class LicenseStatus
    {
        public const string Approved = "approved";
        public const string PendingReview = "pending_review";
        public const string Locked = "locked";
        public const string Rejected = "rejected";
    }

    /// <summary>
    /// Decides whether the app may run, from the saved state and the verified license file. Pure
    /// logic — no I/O — so the rules are unit-tested directly:
    /// - approved license: runs while it was verified with the server within the last 7 days
    ///   and the subscription (plus its grace days) hasn't ended;
    /// - awaiting approval, or the server hasn't been reached yet: runs for 7 days from the
    ///   first attempt, then blocks until the license is confirmed;
    /// - tampered file, revoked/locked/rejected license: blocks.
    /// </summary>
    public static class LicenseEvaluator
    {
        public static readonly TimeSpan GraceWindow = TimeSpan.FromDays(7);

        public static LicenseDecision Evaluate(LicenseState state, LicenseFileContents? file, DateTime nowUtc)
        {
            if (string.IsNullOrEmpty(state.LicenseKey))
                return new LicenseDecision(LicenseAccess.NotActivated, "Service Tray Monitor isn't activated.");

            // Never let time run backwards: a clock moved back doesn't reopen a window.
            DateTime now = nowUtc > state.LastSeenUtc ? nowUtc : state.LastSeenUtc;

            if (!string.IsNullOrEmpty(state.BlockedReason))
                return Blocked(state.BlockedReason);

            if (file != null)
            {
                if (!file.SignatureVerified)
                    return Blocked("The saved license file failed signature verification.");
                if (!string.Equals(file.LicenseKey, state.LicenseKey, StringComparison.Ordinal) || file.InstallGuid != state.InstallGuid)
                    return Blocked("The saved license file doesn't belong to this installation.");

                switch (file.Status)
                {
                    case LicenseStatus.Approved:
                        return EvaluateApproved(state, file, now);
                    case LicenseStatus.PendingReview:
                        break;
                    case LicenseStatus.Rejected:
                        return Blocked("The activation was rejected by the licensing team.");
                    case LicenseStatus.Locked:
                        return Blocked("The license is locked.");
                    default:
                        return Blocked($"The license has an unknown status ('{file.Status}').");
                }
            }

            DateTime graceEnds = (state.GraceStartedUtc ?? now) + GraceWindow;
            bool pendingReview = file?.Status == LicenseStatus.PendingReview;

            if (now <= graceEnds)
            {
                return new LicenseDecision(LicenseAccess.Grace,
                    pendingReview
                        ? "Activated — waiting for approval by the licensing team."
                        : "Activation not confirmed — the licensing server hasn't been reached yet.",
                    graceEnds);
            }

            return Blocked(pendingReview
                ? $"The activation wasn't approved within 7 days (deadline {graceEnds:yyyy-MM-dd HH:mm} UTC)."
                : $"The licensing server couldn't be reached within 7 days (deadline {graceEnds:yyyy-MM-dd HH:mm} UTC).");
        }

        private static LicenseDecision EvaluateApproved(LicenseState state, LicenseFileContents file, DateTime now)
        {
            if (file.SubscriptionExpiryUtc is DateTime expiry && now > expiry.AddDays(state.SubscriptionGraceDays))
            {
                return Blocked($"The subscription expired on {expiry:yyyy-MM-dd} and its {state.SubscriptionGraceDays}-day grace period has ended.");
            }

            DateTime verified = state.LastVerifiedUtc is DateTime last && last > file.IssuedAtUtc ? last : file.IssuedAtUtc;
            DateTime verifyBy = verified + GraceWindow;

            return now <= verifyBy
                ? new LicenseDecision(LicenseAccess.Licensed, "Activated.", verifyBy)
                : Blocked($"The license couldn't be verified with the licensing server for more than 7 days (last verified {verified:yyyy-MM-dd HH:mm} UTC).");
        }

        private static LicenseDecision Blocked(string reason) => new(LicenseAccess.Blocked, reason);
    }
}
