# 06 — Test Plan, Test Report, and Requirements Traceability Matrix

| Field | Value |
|---|---|
| Release under test | 1.0.1 (defect-fix release) |
| Baseline for comparison | 1.0.0 |
| Report date | 2026-09-13 |
| Document version | 0.1 |

## 1. Test strategy

| Level | Purpose | Technique | Where |
|---|---|---|---|
| Reproduction (RP) | Confirm a defect exists before fixing | Run the defective pattern in isolation | Developer machine |
| Unit (AT) | Pure logic: colours, action rules, selection, error classification | xUnit | `Tests/ServiceTrayMonitor.Tests` |
| Component (AT) | `ConfigManager` against a temporary folder; `StartupManager` task XML | xUnit | same |
| Integration, read-only (AT) | `ServiceMonitorEngine` against the local Service Control Manager | xUnit, queries only | same |
| Build verification (BV) | Compile Release with no warnings | `dotnet build` | Developer machine |
| User acceptance (UAT) | End-to-end behaviour, elevation, credentials, installer, startup | Manual test cases below | Client test machine |

**Non-invasive rule (constraint C3):** automated tests never start, stop, or reconfigure services, never
register scheduled tasks, and never touch the user's real `config.json`. Everything that changes machine
state is covered by UAT on a test machine.

**Entry criteria (UAT):** 1.0.1 build and MSI available; a test machine with 1.0.0 installed; accounts for
UAT-06/07 (local admin, domain admin, non-admin with a per-service ACL); a non-critical test service.

**Exit criteria:** all AT pass; BV clean; UAT-01…UAT-12 pass, or failures are logged as new DEF entries
and either fixed or accepted by the sponsor; sign-off in §7.

## 2. Test environment (development verification)

| Item | Value |
|---|---|
| OS | Windows Server 2025 Datacenter 10.0.26100 |
| Account | Domain administrator, process elevated |
| .NET SDK | 10.0.400 (target framework net8.0-windows); 8.0.420 also installed |
| Visual Studio | 18 Community (18.9.2) |
| Test packages | xunit 2.9.3, xunit.runner.visualstudio 4.0.0, Microsoft.NET.Test.Sdk 18.10.0 |

## 3. Results — reproduction and build

| ID | Description | Result | Evidence |
|---|---|---|---|
| RP-01 | 1.0.0 `TestCredential` pattern: `sc.exe query state= all` with stdout redirected but unread, stderr read first | **Defect reproduced.** The stderr read didn't finish within 10 s and `sc.exe` hadn't exited. After draining stdout the process exited with code 0, having produced 94,479 characters. | Session log 2026-09-13 (DEF-004) |
| BV-00 | 1.0.0 Debug build (before changes) | 0 errors, **1 warning** (WFAC010) | Session log 2026-09-13 |
| BV-01 | 1.0.1 `dotnet build ServiceTrayMonitor.sln -c Release` | **Pass** — 0 errors, 0 warnings | Session log 2026-09-13 (DEF-014) |
| BV-02 | 1.0.1-p (branch `paid`) `dotnet build ServiceTrayMonitor.sln -c Release` | **Pass** — 0 errors, 0 warnings | Session log 2026-09-13 (CR-004) |
| IT-01 | 1.0.1-p live integration against `https://licensing-api.miautrix.tech` from the reference server. Harness drives the real `LicenseManager`, vendor fingerprint, HTTP client and signature verification; state kept in a scratch file, not ProgramData. Client test key (value not recorded). | **Pass.** `GET /health` → 200. Activate → `PendingReview`, license file signature verified, InstallGuid matched, status `pending_review`, no subscription expiry (perpetual), review deadline +15 days; decision Grace until +7 days. Check-in → `Renewed`, file re-issued (new IssuedAt), still pending, Grace unchanged. Non-existent key → `LicenseNotFound`, fatal, "This license key doesn't exist.", nothing stored. Fingerprint: real CPU ID and board serial; TPM fallback to machine name (R-14). | Session log 2026-09-13 (CR-004); activation ID `57a3959b-28a5-4f71-b2e9-6785036c8184` |
| IT-02 | 1.0.1-p live check after the client approved activation `57a3959b…` in the admin panel. Same harness, saved state, InstallGuid and hardware as IT-01. | **Pass.** Check-in → `Renewed`, license file `approved`, signature verified, no subscription expiry; decision changed Grace → **Licensed**, 7-day window cleared, LastVerified set, verify-by +7 days. Re-activation with the same key from the same installation → `Activated` immediately, same ActivationId (no new slot), status `approved`. Automated evidence for UAT-16; the GUI steps are still to be run. | Session log 2026-09-14 00:13 UTC (CR-004) |
| IT-03 | 1.0.1-p live check: same key from a **simulated second machine**, with a new InstallGuid, its own state file, and synthetic hardware IDs (`TEST-CPU-0002`, `TEST-MB-0002`, `TEST-TPM-0002`, `02:00:00:00:00:02`). Then a check-in with the original approved activation. | **Pass.** Second machine → `PendingReview`, **new** ActivationId `2f5316c8-4ae7-40c1-bed0-f0d6c755b1cb`, signed `pending_review` file for its own InstallGuid; decision Grace (7-day window). The license's MaxActivations limit wasn't reached. Original machine check-in → `Renewed`, still `approved`, same ActivationId `57a3959b…`; decision Licensed. A new machine gets its own activation and doesn't affect existing ones. Not tested: hardware drift on the **same** installation (R-14), which the reference says reopens review on the existing activation. | Session log 2026-09-14 00:18 UTC (CR-004). Synthetic activation `2f5316c8…` left pending in the admin panel. |
| IT-04 | 1.0.1-p live re-test of the simulated second machine after the client **rejected** activation `2f5316c8…`: check-in with the saved activation, then the same key again from the same InstallGuid and synthetic hardware, then a check-in with the original activation. | **Pass (client), with findings.** Check-in → `Locked`, Reason `ACTIVATION_REJECTED` → decision Blocked, app would close ✔; message shows the raw reason code (DEF-018). Re-activation → HTTP 500 `ServerError`, "An unexpected error occurred." → treated as temporary, stayed Blocked ✔; message starts "Couldn't reach the licensing server" although the server answered (DEF-019). The server should return a defined code for a rejected installation (licensing-team feedback item 6). Original activation → `Renewed`, still approved, Licensed ✔. | Session log 2026-09-14 00:28 UTC (CR-004) |
| IT-05 | Repeat of the IT-04 re-activation of the second machine, with a raw HTTP capture: same key, InstallGuid `843543a8…`, synthetic hardware and app version; body built with the vendor client's serializer; state not updated. | **Not reproduced.** HTTP/1.1 **200 OK** in 338 ms (server `Date` 2026-09-14 01:04:21 GMT, `CF-RAY a3ab7a227cc41242-ORD`). Body: `success: true`, **`code: 1`** (numeric = PendingReview), same ActivationId `2f5316c8…`, status `pending_review`, `reviewDeadlineUtc` 2026-09-29T00:53:04.846411Z. The rejected activation had been reopened for review at about 00:53:04 UTC, before this repeat. Finding: result codes are sent as numbers, not the strings shown in reference §7 (feedback item 5, R-17). | Raw log `raw-activate-20260914-010443Z.log` (session 2026-09-14) |
| IT-06 | Check-in for the second machine after the client **rejected** activation `2f5316c8…` again. Raw HTTP capture; the captured response is replayed through `LicenseManager` so the decision matches the app exactly. No activation call. | **Pass.** HTTP/1.1 200 OK in 283 ms (server `Date` 2026-09-14 02:00:41 GMT, `CF-RAY a3abccaa1ebeaa8f-ORD`). Body: `success: true`, `code: 3` (numeric = Locked), status `locked`, reason `ACTIVATION_REJECTED`, no license file. App decision: **Blocked**, fatal — the app would show the reason and close ✔. Message still shows the raw reason code (DEF-018). Numeric result codes confirmed again (R-17). | Raw log `raw-checkin-20260914-020103Z.log` (session 2026-09-14) |

## 4. Results — automated tests

Command: `dotnet test Tests/ServiceTrayMonitor.Tests -c Release`
Summary (release 1.0.1): **Passed 38, Failed 0, Skipped 0, Total 38** (duration 88 ms).
Summary (paid edition 1.0.1-p, branch `paid`): **Passed 66, Failed 0, Skipped 0, Total 66** (duration 286 ms) — the 38 above plus 28 licensing tests (AT-07…AT-09).

| Group | Test class | Tests | Covers | Result |
|---|---|---|---|---|
| AT-01 | `StatusAndRulesTests` | 15 (palette theory ×5; not-found colour; summary colour ×4; action rules ×5) | DEF-012 | 15/15 pass |
| AT-02 | `ServiceSelectionTests` | 9 (checks kept across filter; not-installed kept; case-insensitive uncheck; order; filter theory ×5) | DEF-002 | 9/9 pass |
| AT-03 | `ConfigManagerTests` | 5 (credential kept on general save; clear keeps settings; password not plain text; corrupt file backed up; no temp file left) | DEF-001, DEF-010 | 5/5 pass |
| AT-04 | `StartupManagerTests` | 2 (logon/elevated/no-limit XML; XML escaping) | DEF-003 | 2/2 pass |
| AT-05 | `ServiceActionsTests` | 4 (access-denied detection ×2; Win32 reason in message; runner without credential) | DEF-004, DEF-005, DEF-008 | 4/4 pass |
| AT-06 | `ServiceMonitorEngineTests` | 3 (existing vs. missing service; list copied; installed list sorted) | DEF-009 | 3/3 pass |
| AT-07 | `LicenseKeyTests` *(paid)* | 8 (key format theory ×7 incl. lower-case and spaces; key masking) | CR-004 | 8/8 pass |
| AT-08 | `LicenseManagerTests` *(paid)* | 17 (vendor verifier accepts test format; invalid format not sent; approved; pending → 7 days → blocked → approved; unknown key; unreachable → 7 days → blocked; retry of unsent activation; new key doesn't restart window; locked after subscription grace; tampered file; file for another install; approved offline ≤ 7 days; subscription past grace offline; clock moved back; different-key failure keeps license; check due every 6 h / hourly) | CR-004 | 17/17 pass |
| AT-09 | `LicenseStateStoreTests` *(paid)* | 3 (missing file; round trip; corrupt file backed up) | CR-004 | 3/3 pass |

## 5. User acceptance test cases

Use a **test machine**. "Test service" = a non-critical service. For pause cases, pick one where
`(Get-Service <name>).CanPauseAndContinue` is `True`.

| ID | Title | Preconditions | Steps | Expected result | Covers | Result |
|---|---|---|---|---|---|---|
| UAT-01 | Tray status and colours | App running; monitor one running, one stopped, and one name that doesn't exist (edit config) | 1. Hover the tray icon. 2. Open the tray menu. | Icon is red. Menu dots: green (running), red (stopped), **gray** (not found, "[Not found]"). Tooltip lists the services. No errors. | DEF-012, DEF-015 | Not run |
| UAT-02 | Actions from tray and window; responsiveness | Test service stopped | 1. Tray → service → Start. 2. While it starts, open the tray menu and move the main window. 3. Main window → Stop. 4. Pause then Resume (pausable service). 5. Start a paused service from the tray. | Each action shows success only after the target state is reached. UI stays responsive, and buttons show "Starting…" and are disabled while running. Step 5 shows "The service is paused. Use Resume instead." | DEF-006, DEF-008 | Not run |
| UAT-03 | Grid keeps selection, sort, scroll | Monitor ≥ 15 services; poll interval 2 s | 1. Sort by Status. 2. Scroll down and select a row near the bottom. 3. Wait 20 s. 4. Click Stop on the selected (test) service. | Sort, scroll position, and selection stay put across polls. Stop acts on the highlighted service. | DEF-007 | Not run |
| UAT-04 | Service selection, filter, and Cancel | Monitoring A and B | 1. Manage Monitored Services. 2. Filter to show only A; check C (visible via filter). 3. Clear the filter; confirm A, B, C are checked. 4. Filter to A only; Save. 5. Reopen; uncheck B; Cancel; reopen. 6. Set `PollIntervalSeconds` to 999 in config, restart, open the window. | Step 3: all three checked. Step 4: A, B, C monitored. Step 5: B still checked (Cancel discarded). Step 6: the window opens with 300. | DEF-002, DEF-016 | Not run |
| UAT-05 | Credential survives settings save | No credential saved | 1. Admin Credentials → enter a valid account → Save. 2. Manage Monitored Services → change the interval → Save. 3. Reopen Admin Credentials. | Status shows "Currently configured: DOMAIN\user". `config.json` still holds username and encrypted password. | DEF-001 | Not run |
| UAT-06 | Credential Test | Accounts: (a) local admin, (b) domain admin, (c) wrong password, (d) standard user | Admin Credentials → enter each → Test | (a)/(b): "Credential works … full administrator token" within a few seconds; UI never hangs. (c): "Sign-in failed … user name or password is incorrect". (d): "Signed in … without a full administrator token". | DEF-004, DEF-005 | Not run |
| UAT-07 | Fallback to the saved credential | Test service whose DACL denies the operator's admin group but grants the saved account (`sc sdset`); credential saved | 1. Start the test service from the tray. 2. Clear the credential and repeat. | Step 1: succeeds (the retry as the saved account runs automatically). Step 2: fails with "Access is denied. Save an account with rights…". | DEF-005, DEF-008 | Not run |
| UAT-08 | Re-save the credential without retyping the password | Credential saved | 1. Admin Credentials → leave the password blank → Save. 2. Test. 3. Change the username, leave the password blank → Save. | Step 1 saves; step 2 still says "Credential works". Step 3 asks for the password. | DEF-017 | Not run |
| UAT-09 | Start with Windows | App 1.0.1 installed; legacy Run value present from 1.0.0 (optional) | 1. Enable Start with Windows → Save. 2. `schtasks /Query /TN "ServiceTrayMonitor (<user>)" /XML`. 3. Sign out and back in. 4. Disable → Save → query again. | Step 2: task exists with HighestAvailable and PT0S; the Run value is gone. Step 3: the app starts at logon, elevated, with no UAC prompt. Step 4: the task no longer exists. | DEF-003 | Not run |
| UAT-10 | Corrupt configuration | App closed | 1. Replace `config.json` with `{ broken`. 2. Start the app. | A balloon reports the reset. `config.json.corrupt-<timestamp>` holds the broken content, and the app runs with defaults. | DEF-010 | Not run |
| UAT-11 | Installer upgrade | Test machine with SystemTrayMonitor 1.0.0 installed | 1. Run the 1.0.1 `setup.exe`. 2. Open Apps & Features. 3. Launch the app. | No .NET Framework 4.7.2 prompt. A single "SystemTrayMonitor" entry, version 1.0.1. The app starts, and the existing config and credential are kept. | DEF-013 | Not run |
| UAT-12 | Long run and exit | Monitor 10 services; poll 2 s | 1. Leave running 8 h, opening the tray menu and the windows periodically. 2. Exit from the tray. | Handle and memory counts (Task Manager: GDI/USER objects) stay flat. No error dialogs. Exit removes the icon and ends the process. | DEF-009, DEF-011, DEF-015 | Not run |

### 5.1 Paid edition (branch `paid`, 1.0.1-p) — CR-004

Preconditions for all: `SetupSTM-Paid.msi` installed on a test machine **without** the free edition (R-15), internet access to
`licensing-api.miautrix.tech` unless stated, and test keys issued by the client from the licensing admin panel.

| ID | Title | Preconditions | Steps | Expected result | Covers | Result |
|---|---|---|---|---|---|---|
| UAT-13 | First start without activation | Fresh install; no `license.json` | 1. Start the app. 2. Close the activation window (X or Exit). | The activation window appears before any tray icon; closing it exits the process. | CR-004 | Not run |
| UAT-14 | Malformed and rejected keys | As UAT-13 | 1. Type `ABCD-1234`. 2. Type a well-formed key that doesn't exist and click Activate. | Step 1: Activate stays disabled. Step 2: warning "This license key doesn't exist." then the app closes; `license.json` holds no key. | CR-004 | Not run |
| UAT-15 | New activation (pending review) | Valid unused test key | Activate with the key | Message says the license is waiting for approval and shows a date 7 days ahead; the tray starts; **License…** shows the masked key, pending status, and confirm-by date. The admin panel shows the activation with this machine's real CPU ID (not `CPU-UNAVAILABLE`). | CR-004 | Not run |
| UAT-16 | Approval picked up | After UAT-15 | 1. Approve the activation in the admin panel. 2. **License… → Check now** (or wait up to 1 h). | Status "Activated"; Last verified = now; Confirm by = +7 days; subscription expiry or "None (perpetual)". | CR-004 | Not run |
| UAT-17 | Offline behaviour | Approved | 1. Disconnect the network; restart the app. 2. Set the test machine clock 8 days ahead. | Step 1: app runs; License… shows the connection error. Step 2: within 5 minutes a warning says the license couldn't be verified for more than 7 days and the app closes; setting the clock back doesn't reopen it. | CR-004 | Not run |
| UAT-18 | Revoked license | Approved | 1. Revoke the license in the admin panel. 2. **Check now** (or restart). 3. Start again. | Step 2: warning "This license has been revoked." and the app closes. Step 3: automatic re-check, then the activation window with the reason. | CR-004 | Not run |
| UAT-19 | Fingerprint stability | Approved, on a machine with Hyper-V/VPN adapters or without a readable TPM | Reboot 3 times; connect/disconnect VPN; **Check now** each time | Status stays "Activated" (no return to pending review). A failure confirms R-14. | CR-004, R-14 | Not run |
| UAT-20 | Activate with a different key | Approved | 1. **License… → Activate with a different key** → nonexistent key. 2. Repeat with a second valid key. | Step 1: warning, "Your current license stays active"; app keeps running. Step 2: License… shows the new masked key and its status. | CR-004, DEC-016 | Not run |
| UAT-21 | Paid installer | Clean test machine | Install `setup.exe` / `SetupSTM-Paid.msi`; open Apps & Features; open License… | Entry "SystemTrayMonitor (Paid Test)" 1.0.1; License… shows "Service Tray Monitor 1.0.1-p"; UAT-15 CPU ID check passes (WMI dependency shipped correctly). | CR-004, DEC-018 | Not run |

## 6. Requirements traceability matrix

| Req. | Requirement | Source | Defects / CRs | Automated tests | UAT |
|---|---|---|---|---|---|
| REQ-01 | Show service status with colour coding (tray icon, menu, grid) | README | DEF-012, DEF-015 | AT-01, AT-06 | UAT-01 |
| REQ-02 | Start, Stop, Pause, Resume from tray and main window | README | DEF-006, DEF-008; CR-001 (Restart) | AT-05 | UAT-02 |
| REQ-03 | Choose monitored services with a filterable list | README | DEF-002, DEF-016 | AT-02 | UAT-04 |
| REQ-04 | Sortable grid view | README | DEF-007 | — | UAT-03 |
| REQ-05 | Configurable poll interval | README | DEF-009, DEF-016 | AT-06 | UAT-04, UAT-12 |
| REQ-06 | Start with Windows | README | DEF-003 | AT-04 | UAT-09 |
| REQ-07 | Store admin credential with a DPAPI-encrypted password | README, DEC-002 | DEF-001, DEF-017 | AT-03 | UAT-05, UAT-08 |
| REQ-08 | Service actions succeed using the saved credential when needed | Client 2026-09-13, DEC-002 | DEF-004, DEF-005 | AT-05 | UAT-06, UAT-07 |
| REQ-09 | Settings persist safely in `%AppData%` | README | DEF-001, DEF-010 | AT-03 | UAT-10 |
| REQ-10 | Installable and upgradable via MSI | Installer project | DEF-013; CR-002, CR-003 | — | UAT-11 |
| REQ-11 | Stable long-running tray process | Implied | DEF-009, DEF-011, DEF-015 | AT-06 | UAT-12 |
| REQ-12 | Clean build | Quality | DEF-014 | BV-01, BV-02 | — |
| REQ-13 | *(paid)* App runs only with an activated license; activation window at startup | CR-004 | CR-004 | AT-07, AT-08 | UAT-13, UAT-14 |
| REQ-14 | *(paid)* 7-day rule for unconfirmed or unverified licenses | CR-004, DEC-015 | CR-004 | AT-08 | UAT-15, UAT-16, UAT-17 |
| REQ-15 | *(paid)* Rejected, revoked, or locked license shows the reason and closes the app | CR-004, DEC-016 | CR-004 | AT-08 | UAT-14, UAT-18 |
| REQ-16 | *(paid)* License window: status, masked key, expiry, activate a different key | CR-004 | CR-004 | AT-07, AT-08 | UAT-15, UAT-20 |
| REQ-17 | *(paid)* License state per machine; separate installer product | CR-004, DEC-017, DEC-018 | CR-004; R-14, R-15 | AT-09 | UAT-19, UAT-21 |

## 7. Sign-off

| Role | Name | Result (Accept / Accept with conditions / Reject) | Signature | Date |
|---|---|---|---|---|
| Client test lead | [TBD] | | | |
| Sponsor | [TBD] | | | |
