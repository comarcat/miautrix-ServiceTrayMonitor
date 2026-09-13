# 05 — Defect Log

Baseline: v1.0.0 (snapshot 2026-09-13). Fix release: 1.0.1.
Detection methods: **CR** = code review, **RP** = reproduced, **KB** = knowledge-base analysis, **BLD** = build output.
Verification references point to [doc 06](06-test-plan-and-report.md).

## Summary

| ID | Title | Severity | Status |
|---|---|---|---|
| DEF-001 | Saving monitored services erases the saved admin credential | High | Verified (automated) — awaiting UAT |
| DEF-002 | Filter loses checks; Save drops services hidden by the filter | High | Verified (automated) — awaiting UAT |
| DEF-003 | "Start with Windows" never starts the app | High | Verified (automated) — awaiting UAT |
| DEF-004 | Credential **Test** hangs the application | High | Fixed (reproduced on 1.0.0 pattern) — awaiting UAT |
| DEF-005 | A saved credential makes service actions fail with "Access is denied" | High | Fixed — awaiting UAT |
| DEF-006 | UI freezes for up to 15 s during service actions | Medium | Fixed — awaiting UAT |
| DEF-007 | Grid loses sort, scroll, and (likely) selection on every poll | Medium | Fixed — awaiting UAT |
| DEF-008 | Action results differ between the direct and credential paths | Medium | Fixed — awaiting UAT |
| DEF-009 | Overlapping polls, blocking cross-thread Invoke, shared list reference | Medium | Verified (automated) — awaiting UAT |
| DEF-010 | Corrupt config.json silently replaced; non-atomic writes | Medium | Verified (automated) — awaiting UAT |
| DEF-011 | Resource leaks (menu items, event handlers, service handles) | Low | Fixed — awaiting UAT |
| DEF-012 | Missing services shown red instead of gray; duplicated colour/rule logic | Low | Verified (automated) — awaiting UAT |
| DEF-013 | Installer cannot upgrade; unneeded .NET Framework 4.7.2 prerequisite | Medium | Fixed — Release MSI built — awaiting UAT |
| DEF-014 | Build warning WFAC010 (DPI settings in manifest) | Low | Verified (build) |
| DEF-015 | Tray updates run on thread-pool threads instead of the UI thread | Medium | Fixed — awaiting UAT |
| DEF-016 | Cancel keeps unsaved checks; poll interval > 300 in config crashes the window | Low | Fixed — awaiting UAT |
| DEF-017 | Re-saving the credential without retyping the password stores an empty password | Medium | Fixed — awaiting UAT |

**Totals:** 17 defects — High 5, Medium 8, Low 4. All fixed in 1.0.1; none closed until UAT is accepted.

---

### DEF-001 — Saving monitored services erases the saved admin credential
- **Severity / priority:** High / P1 — data loss. **Detected:** 2026-09-13, CR.
- **Component:** `TrayAppContext.cs`, `Services/ConfigManager.cs`.
- **Steps (1.0.0):** Launch → Admin Credentials → Save an account → Manage Monitored Services → Save.
- **Expected:** The credential stays saved. **Actual:** The credential fields revert to their values at
  app start (empty on a fresh setup).
- **Root cause:** `TrayAppContext` loaded `AppSettings` once at startup. The credential was saved
  separately (load → modify → save), but saving settings serialised the stale startup object over the file.
- **Resolution:** `ConfigManager.SaveGeneralSettings` re-reads the file and replaces only the general
  fields. Credential saves change only credential fields. All writes are serialised by a lock.
- **Verification:** AT-03 `SaveGeneralSettings_KeepsCredentialSavedSinceStartup`, `ClearCredential_KeepsGeneralSettings`; UAT-05.

### DEF-002 — Filter loses checks; Save drops services hidden by the filter
- **Severity / priority:** High / P1 — monitored services silently removed. **Detected:** CR.
- **Component:** `Forms/SettingsForm.cs`.
- **Steps (1.0.0):** Monitor A and B → open Manage Monitored Services → type a filter that shows only A → Save.
- **Expected:** A and B stay monitored. **Actual:** Only A is saved. Checks made before changing the
  filter are also lost.
- **Root cause:** The list was rebuilt from saved settings on every keystroke, and Save read only the
  `CheckedItems` currently visible.
- **Resolution:** A new `ServiceSelection` working set records every check/uncheck and is saved
  regardless of the filter. Names that aren't installed are preserved.
- **Verification:** AT-02 (9 tests); UAT-04.

### DEF-003 — "Start with Windows" never starts the app
- **Severity / priority:** High / P1 — documented feature non-functional. **Detected:** CR; the
  reference server has `StartWithWindows = true` with the Run-key entry present.
- **Component:** `Services/ConfigManager.cs` (1.0.0 `ApplyStartupSetting`).
- **Root cause:** The app requires elevation, and Windows does not launch elevated programs from
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` at logon.
- **Resolution:** `StartupManager` registers a per-user scheduled task (logon trigger, highest
  privileges, no 72-hour limit, IgnoreNew). The legacy Run value is removed automatically. Failures are
  reported in a balloon.
- **Verification:** AT-04 (task XML content and escaping); UAT-09.

### DEF-004 — Credential Test hangs the application
- **Severity / priority:** High / P1 — application hang. **Detected:** CR; **reproduced** 2026-09-13 (RP-01).
- **Component:** `Services/ElevatedServiceRunner.cs` (1.0.0 `TestCredential`).
- **Root cause:** `sc.exe query state= all` ran with stdout redirected but never read, while stderr was
  read to the end first. The output (~94 KB on the reference server) filled the pipe buffer, so `sc.exe`
  blocked, stderr never closed, and the UI thread waited forever. The 1.0.0 `RunAction` read before
  `WaitForExit`, so its 15 s timeout was ineffective too.
- **Resolution:** `sc.exe` removed. The test signs in with `LogonUser` and checks the token and SCM
  access in-process, off the UI thread.
- **Verification:** RP-01 (defect evidence on the 1.0.0 pattern); AT-05 `CredentialRunner_WithoutCredential_FailsCleanly`; UAT-06.

### DEF-005 — A saved credential makes service actions fail with "Access is denied"
- **Severity / priority:** High / P1 — the core function fails in the configured scenario. **Detected:** CR/KB.
- **Component:** `Services/ServiceMonitorEngine.cs`, `Services/ElevatedServiceRunner.cs`.
- **Root cause:** Once a credential was saved, every action went through `sc.exe` launched with
  alternate credentials. That uses an interactive-type logon, and UAC filters it to a non-admin token.
  The app was already elevated, so actions that would have succeeded with its own token failed.
- **Resolution (DEC-002):** Actions run with the app's own elevated token first. Only on *Access is
  denied* are they retried as the saved account, using `LogonUser` **batch** logon (full token) plus
  impersonation, falling back to interactive logon. Messages explain filtered-token outcomes.
- **Verification:** AT-05; UAT-06, UAT-07. Real alternate-account verification is pending (R-01).

### DEF-006 — UI freezes for up to 15 s during service actions
- **Severity:** Medium. **Detected:** CR. **Component:** `TrayAppContext.cs`, `Forms/ManageServicesForm.cs`.
- **Root cause:** Actions (including `WaitForStatus` up to 15 s) ran synchronously on the UI thread.
- **Resolution:** Actions run on `Task.Run`. The window shows "Starting…" and disables action buttons
  while an action runs.
- **Verification:** UAT-02.

### DEF-007 — Grid loses sort, scroll, and (likely) selection on every poll
- **Severity:** Medium. **Detected:** CR. **Component:** `Forms/ManageServicesForm.cs`.
- **Root cause:** Every poll cleared and re-added all rows. Selection was restored with `Row.Selected`,
  but actions read `CurrentRow`, which moves to the first row after a rebuild. The highlight and the
  action target could differ (by inspection; not reproduced).
- **Resolution:** Rows are updated in place when the service set is unchanged. On a rebuild, sort,
  current cell, and scroll are restored.
- **Verification:** UAT-03.

### DEF-008 — Action results differ between the direct and credential paths
- **Severity:** Medium. **Detected:** CR. **Component:** `ElevatedServiceRunner.cs` (1.0.0), `ServiceMonitorEngine.cs`.
- **Root cause:** The `sc.exe` path reported success when the request was *accepted* (pending), and
  reported "already running" as a failure. The direct path waited and treated "already" as success.
- **Resolution:** Both paths use `ServiceActions`: they wait for the target state, handle pending
  states, and give a clear message for Start on a paused service.
- **Verification:** AT-05; UAT-02, UAT-07.

### DEF-009 — Overlapping polls, blocking cross-thread Invoke, shared list reference
- **Severity:** Medium. **Detected:** CR. **Component:** `ServiceMonitorEngine.cs`, `ManageServicesForm.cs`.
- **Root cause:** Timer and UI both called `RefreshNow` with no guard, so slow SCM queries could overlap.
  The window used synchronous `Invoke` from the timer thread. The engine held the caller's list instance.
- **Resolution:** Refreshes are serialised, and overlapping timer ticks are skipped. `BeginInvoke` is
  used with disposal guards, and the engine copies the list.
- **Verification:** AT-06 `UpdateMonitoredList_CopiesTheCallersList`, `Refresh_ReportsExistingAndMissingServices`; UAT-12.

### DEF-010 — Corrupt config.json silently replaced; non-atomic writes
- **Severity:** Medium. **Detected:** CR. **Component:** `Services/ConfigManager.cs`.
- **Root cause:** Parse errors were swallowed, and defaults were written over the file at the next save.
  Writes were in place.
- **Resolution:** The corrupt file is renamed `config.json.corrupt-<timestamp>` and a balloon warns.
  Saves refuse to overwrite a file that is locked or unreadable for I/O reasons. Writes go to a temp file
  that is then moved into place.
- **Verification:** AT-03 `CorruptConfig_IsBackedUp_AndDefaultsReturned`, `Save_LeavesNoTempFileBehind`; UAT-10.

### DEF-011 — Resource leaks
- **Severity:** Low. **Detected:** CR/KB.
- **Root cause:**
  - A new `Format` handler was attached on every filter keystroke.
  - Menu items and a bold `Font` were recreated on every menu open without disposal.
  - `ServiceController` objects from `GetServices` were never disposed.
  - The main window never unsubscribed from engine events.
- **Resolution:**
  - The handler is attached once.
  - Menu items are disposed, with the cached images detached first; the font is created once.
  - Controllers are disposed after reading names.
  - The window unsubscribes on dispose.
- **Verification:** UAT-12 (long-running observation).

### DEF-012 — Missing services shown red instead of gray; duplicated colour and rule logic
- **Severity:** Low. **Detected:** KB (duplicated logic) / CR. **Component:** `Models/MonitoredService.cs`, `Services/IconFactory.cs`, forms.
- **Root cause:** `StatusColor` ignored `Exists`, so a missing service kept the default `Stopped` colour.
  The README documents ⚪. Colours were defined twice, and action enable rules were duplicated in two UIs.
- **Resolution:** `StatusPalette` is the single source of colours (gray when missing).
  `ServiceActionRules` is shared by the menu and the window.
- **Verification:** AT-01 (15 tests); UAT-01.

### DEF-013 — Installer cannot upgrade; unneeded prerequisite
- **Severity:** Medium. **Detected:** CR. **Component:** `Setup/SetupSTM/SetupSTM.vdproj`.
- **Root cause:** `RemovePreviousVersions = FALSE` with a fixed `ProductVersion 1.0.0` means a new build
  would install side by side or be blocked. The bootstrapper listed .NET Framework 4.7.2, which a
  .NET 8 app doesn't use.
- **Resolution:** `ProductVersion 1.0.1`, new `ProductCode` and `PackageCode`, same `UpgradeCode`,
  `RemovePreviousVersions = TRUE`, .NET Framework 4.7.2 prerequisite removed. The .NET 8 Desktop Runtime
  prerequisite is kept.
- **Verification:** Release MSI built 2026-09-13; MSI properties checked (doc 10); UAT-11.

### DEF-014 — Build warning WFAC010
- **Severity:** Low. **Detected:** BLD. **Component:** `app.manifest`, `ServiceTrayMonitor.csproj`.
- **Resolution:** DPI settings were moved from the manifest to `<ApplicationHighDpiMode>PerMonitorV2`.
- **Verification:** BV-01 (Release build, 0 warnings).

### DEF-015 — Tray updates run on thread-pool threads
- **Severity:** Medium — cross-thread UI access, intermittent faults. **Detected:** 2026-09-13 during DEF-006 fix, CR.
- **Component:** `TrayAppContext.cs`.
- **Root cause:** `SynchronizationContext.Current` was captured in the constructor before any control
  existed, so it was null. The fallback `new SynchronizationContext()` posts to the thread pool.
- **Resolution:** A `WindowsFormsSynchronizationContext` is installed explicitly before it is captured.
- **Verification:** UAT-01, UAT-12.

### DEF-016 — Cancel keeps unsaved checks; out-of-range interval crashes the window
- **Severity:** Low. **Detected:** CR. **Component:** `Forms/SettingsForm.cs`, `TrayAppContext.cs`.
- **Root cause:** The hidden form kept its state between openings. `NumericUpDown.Value` was set to
  `max(2, value)` without an upper bound (300), so a value over 300 threw.
- **Resolution:** The window resets from saved settings each time it opens (unless already open), and
  the interval is clamped to 2–300.
- **Verification:** UAT-04.

### DEF-017 — Re-saving the credential without retyping the password stores an empty password
- **Severity:** Medium — silently breaks the credential. **Detected:** 2026-09-13 during DEF-001 fix, CR.
- **Component:** `Forms/CredentialsForm.cs`.
- **Root cause:** The form never re-displays the password, but Save encrypted whatever was in the empty
  password box.
- **Resolution:** A blank password with an unchanged login keeps the saved password (placeholder text
  explains this). A different login requires a password.
- **Verification:** UAT-08.
