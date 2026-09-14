# 07 — Change Request Log

Integrated change control (PMI): every change to scope, functionality, installed footprint, or operating
model is submitted as a CR, analysed for impact, and decided by the sponsor (or change control board)
**before** implementation. Defect fixes that restore intended behaviour are not CRs (see SoW §5).

## Process

1. **Submit** — anyone fills in the CR form below and adds a row to the log.
2. **Analyse** — the dev team estimates impact on scope, schedule, cost, quality, and risk, and lists
   affected components and tests.
3. **Decide** — the sponsor approves, rejects, or defers the CR. The decision is recorded in the log and in doc 09.
4. **Plan** — approved CRs get WBS elements under `1.6 Approved changes`, acceptance criteria, and UAT cases.
5. **Implement and verify** — code, tests, and documentation are updated; the release is recorded in doc 10.
6. **Close** — the CR is accepted in UAT and the log is updated.

## Log

| ID | Title | Requested by | Date | Type | Priority | Impact (summary) | Status | Decision / date |
|---|---|---|---|---|---|---|---|---|
| CR-001 | Add **Restart** action for monitored services | Client | 2026-09-13 | New functionality | [TBD] | Engine action (stop → wait → start, dependent services), tray menu, main window, rules, tests, README | Submitted | — |
| CR-002 | Align installed product name "SystemTrayMonitor" with "Service Tray Monitor" | Dev team (analysis) | 2026-09-13 | Installer / branding | Low | `.vdproj` ProductName and Title, Start-menu entry, install folder; upgrade path must be tested | Submitted | — |
| CR-003 | Code-sign the executable and MSI | Dev team (analysis) | 2026-09-13 | Release / security | Medium | Certificate procurement, signing step, timestamp server; removes "unknown publisher" at UAC and SmartScreen | Submitted | — |
| CR-004 | License key activation — paid edition test build 1.0.1-p | Client | 2026-09-13 | New functionality | High | New licensing module and vendor client (Miautrix Licensing API), activation window, License window, startup gate, 7-day rule, per-machine state, `System.Management` dependency, separate MSI product. Branch `paid` only. | Approved — implemented on branch `paid`, awaiting UAT | Approved by client, 2026-09-13 (see CR-004 record) |

Further features will be added as CR-005 onward.

---

## CR form (template)

| Field | Entry |
|---|---|
| CR ID | CR-nnn |
| Title | |
| Requested by / date | |
| Description of change | |
| Business justification | |
| Type | New functionality / Change to existing functionality / Installer / Documentation / Other |
| Priority | Critical / High / Medium / Low |
| Affected components | |
| Impact — scope | |
| Impact — schedule | |
| Impact — cost / effort | |
| Impact — quality / testing | New or changed AT and UAT cases |
| Impact — risk | New or changed risk IDs |
| Acceptance criteria | WHEN … THE SYSTEM SHALL … |
| Alternatives considered | |
| Decision | Approved / Rejected / Deferred |
| Decided by / date | |
| Target release | |
| Implementation reference | Commit / release |
| Closure date | |

---

## CR-001 — Restart action (initial record)

| Field | Entry |
|---|---|
| Description | Add a Restart command for each monitored service in the tray menu and main window. |
| Justification | The client needs to be sure the app can start, stop, and **restart** services (2026-09-13). 1.0.1 has Start/Stop/Pause/Resume only. |
| Affected components | `ServiceActions`, `ServiceMonitorEngine`, `ServiceActionRules`, `TrayAppContext`, `ManageServicesForm`, tests, README |
| Open questions | Behaviour for services with dependents (stop dependents and restart them?); Restart on a stopped service (just start?); single timeout or per-phase timeouts |
| Proposed acceptance criteria | WHEN the user chooses Restart on a running service THE SYSTEM SHALL stop it, wait for Stopped, start it, wait for Running, and report success only when Running is reached. WHEN the app's own token is denied THE SYSTEM SHALL retry both phases as the saved admin credential. |
| Status | Submitted — analysis to be done in the feature planning session |

---

## CR-004 — License key activation, paid edition test build (1.0.1-p)

| Field | Entry |
|---|---|
| CR ID | CR-004 |
| Title | License key activation — paid edition test build 1.0.1-p |
| Requested by / date | Client, 2026-09-13 |
| Description of change | Branch `paid` from release 1.0.1. The app must be activated with a license key against the Miautrix Licensing API (activation client package v2: signed-only license file, RSA public key embedded, no symmetric key). An activation window appears at startup; the tray menu gains **License…** (status, masked key, subscription expiry, **Activate with a different key**). |
| Business justification | Test a paid edition that runs only with a valid license. |
| Type | New functionality |
| Priority | High |
| Affected components | New `Licensing/` (`LicenseManager`, `LicenseEvaluator`, `LicenseState`/`LicenseStateStore`, `LicensingApi` adapter, vendor sources in `Licensing/Client/`); `Forms/ActivationForm`, `Forms/LicenseForm`; `Program`, `TrayAppContext`; `ServiceTrayMonitor.csproj` (`System.Management` 8.0.0, InformationalVersion 1.0.1-p); `SetupSTM.vdproj` (separate product); tests |
| Rules (client decisions, 2026-09-13) | 1. Not activated → activation window at startup; closing it exits. 2. Not confirmed (pending admin approval, or server unreachable) → the app runs and keeps checking hourly for 7 days, then closes until activated. 3. Approved → runs while verified with the server within the last 7 days; checks in every `Policy.CheckIntervalHours` (6 h). 4. Server rejections (unknown/expired/revoked key, maximum activations, install mismatch, activation not found, locked, invalid signature) → warning with the reason, then the app closes. 5. State per machine in `%ProgramData%\ServiceTrayMonitor\license.json`. 6. Version 1.0.1, displayed as 1.0.1-p; installer "SystemTrayMonitor (Paid Test)". 7. Live-server testing with a client-provided test key. |
| Impact — scope | Adds WBS 1.6.1. Branch `paid` only; not merged into `main` (DEC-019). |
| Impact — schedule | [TBD] |
| Impact — cost / effort | [TBD] |
| Impact — quality / testing | AT-07…AT-09 (28 new automated tests), BV-02; UAT-13…UAT-21 |
| Impact — risk | R-12…R-16 |
| Acceptance criteria | WHEN the app starts without an activated key THE SYSTEM SHALL show the activation window and exit if it is closed. WHEN a well-formed key is rejected by the server THE SYSTEM SHALL show the reason and close. WHEN the activation is pending review or the server can't be reached THE SYSTEM SHALL run, retry hourly, and close once 7 days pass without confirmation. WHEN an approved license hasn't been verified for more than 7 days THE SYSTEM SHALL close. WHEN the user activates a different key and it fails THE SYSTEM SHALL keep the current license. |
| Alternatives considered | Compiled vendor DLL (package option A) — source files chosen for reviewability (DEC-014) |
| Decision | Approved |
| Decided by / date | Client, 2026-09-13 |
| Target release | 1.0.1-p (test build) |
| Implementation reference | Branch `paid`; configuration record in doc 10 |
| Closure date | — |

### Feedback for the licensing team (activation client package v2)

1. §3 result-code table still points to §5 for the key format; the format is in §4.
2. The `ActivationApiClient` comment gives the LAN address as `http://10.11.1.41:8080`; the reference says `https://`.
3. `HardwareFingerprint.ReadTpmId` returns `Win32_Tpm.ManufacturerId` — a TPM *vendor* ID shared by every machine with that TPM brand — and falls back to `TPM-FALLBACK-<machine name>` when the TPM namespace is unavailable (the case on the reference server). Renaming the machine changes the fingerprint.
4. `ReadPrimaryMac` picks the first "Up" Ethernet-type adapter; on the reference server that is one of two Hyper-V virtual adapters, so the chosen MAC can change and send the activation back to pending review (R-14).
5. **Result codes are sent as numbers, not strings (IT-05 raw capture):** the API returned `"code":1` for PendingReview, while §7's sample shows `"code": "PendingReview"`. The vendor client still parses it, because `JsonStringEnumConverter` accepts integers. But the meaning then depends on the client `ResultCode` enum having exactly the server's order, so a reordered or inserted server value silently changes every code. Please send codes as strings, or document and freeze the numeric values (R-17). An unknown number becomes an undefined enum value, which the app treats as a rejection.
6. **Server error on a rejected installation (IT-04, 2026-09-14 00:28 UTC, about 00:28:05 server time):** after an admin rejected activation `2f5316c8-4ae7-40c1-bed0-f0d6c755b1cb`, `POST /api/activate` with the same key, InstallGuid `843543a8-0405-4be1-878f-d26574ab8d3b` and hardware returned `ServerError`, "An unexpected error occurred." The HTTP status wasn't captured in IT-04. **Repeated in IT-05** (server `Date` 01:04:21 GMT, `CF-RAY a3ab7a227cc41242-ORD`) with the identical request: HTTP 200, `PendingReview`, same ActivationId, `reviewDeadlineUtc` 2026-09-29T00:53:04.846411Z. The activation had been reopened for review at about 00:53:04 UTC, so the error wasn't reproduced. Please check the server log around 00:28:05 UTC for that InstallGuid, and confirm what reopened the review at 00:53:04 (admin action, or a rule that lets a rejected installation re-enter review).
7. The check-in for that rejected activation returned `Locked` with Reason `ACTIVATION_REJECTED`, which §6 of the reference doesn't document (only `SUBSCRIPTION_EXPIRED_GRACE_ENDED` and `LICENSE_REVOKED`). Please list every reason code.
