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

## 4. Results — automated tests

Command: `dotnet test Tests/ServiceTrayMonitor.Tests -c Release`
Summary: **Passed 38, Failed 0, Skipped 0, Total 38** (duration 88 ms).

| Group | Test class | Tests | Covers | Result |
|---|---|---|---|---|
| AT-01 | `StatusAndRulesTests` | 15 (palette theory ×5; not-found colour; summary colour ×4; action rules ×5) | DEF-012 | 15/15 pass |
| AT-02 | `ServiceSelectionTests` | 9 (checks kept across filter; not-installed kept; case-insensitive uncheck; order; filter theory ×5) | DEF-002 | 9/9 pass |
| AT-03 | `ConfigManagerTests` | 5 (credential kept on general save; clear keeps settings; password not plain text; corrupt file backed up; no temp file left) | DEF-001, DEF-010 | 5/5 pass |
| AT-04 | `StartupManagerTests` | 2 (logon/elevated/no-limit XML; XML escaping) | DEF-003 | 2/2 pass |
| AT-05 | `ServiceActionsTests` | 4 (access-denied detection ×2; Win32 reason in message; runner without credential) | DEF-004, DEF-005, DEF-008 | 4/4 pass |
| AT-06 | `ServiceMonitorEngineTests` | 3 (existing vs. missing service; list copied; installed list sorted) | DEF-009 | 3/3 pass |

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
| REQ-12 | Clean build | Quality | DEF-014 | BV-01 | — |

## 7. Sign-off

| Role | Name | Result (Accept / Accept with conditions / Reject) | Signature | Date |
|---|---|---|---|---|
| Client test lead | [TBD] | | | |
| Sponsor | [TBD] | | | |
