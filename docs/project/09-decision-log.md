# 09 — Decision and Assumption Log

| ID | Date | Decision | Rationale | Alternatives considered | Made by | Status |
|---|---|---|---|---|---|---|
| DEC-001 | 2026-09-13 | Version control is **GitHub**. The GitHub app monitors the folder `miautrix-ServiceTrayMonitor`; the folder isn't renamed or moved, and no local `git init` is done by the dev team. | Client's tooling | Local git repository | Client | Confirmed |
| DEC-002 | 2026-09-13 | **Keep the Admin Credentials feature.** Service actions use the app's own elevated token first. On *Access is denied* they are retried as the saved account via `LogonUser` (batch, then interactive) plus impersonation. `sc.exe` is no longer used. | The client must be sure the app can start/stop/restart services. Own-token-first succeeds whenever the elevated app has rights; the fallback covers restricted services. Batch logon avoids UAC token filtering. In-process impersonation removes the pipe deadlock (DEF-004) and the password-to-child-process exposure. | (A) Remove credentials and rely on elevation; (B) run as a normal user and always use credentials; (C) helper Windows service running as LocalSystem | Client (keep credentials); dev team (mechanism) | Confirmed — mechanism pending UAT (R-01); audit implication R-10 |
| DEC-003 | 2026-09-13 | Keep the `requireAdministrator` manifest. | Needed for own-token-first control; changing the elevation model is a change, not a fix | `asInvoker` + credentials; helper service | Dev team (within fix scope) | Confirmed |
| DEC-004 | 2026-09-13 | Implement Start with Windows as a **per-user scheduled task** (logon trigger, HighestAvailable, no execution time limit); remove the legacy Run entry automatically. | The only supported way to auto-start an elevated app at logon without a UAC prompt | Run key (doesn't work elevated); Startup folder (same limitation); Windows service (different model) | Dev team | Confirmed — pending UAT-09 |
| DEC-005 | 2026-09-13 | The defect-fix release is **1.0.1**: assembly `Version` 1.0.1; MSI `ProductVersion` 1.0.1 with new ProductCode/PackageCode, same UpgradeCode, `RemovePreviousVersions = TRUE`. | Enables in-place upgrade from 1.0.0 | Keep 1.0.0 (can't upgrade) | Dev team | Confirmed |
| DEC-006 | 2026-09-13 | Project governance: SoW = session work (analysis, KB, testing/solving, documentation). Defect fixes are part of the SoW; changes are CRs; PMI guidelines apply. | Client instruction | — | Client | Confirmed |
| DEC-007 | 2026-09-13 | Test stack: xUnit 2.9.3, xunit.runner.visualstudio 4.0.0, Microsoft.NET.Test.Sdk 18.10.0. Tests are non-invasive (temporary config folder; read-only SCM queries). | Latest stable at the time; safe to run on admin workstations | MSTest, NUnit | Dev team | Confirmed |
| DEC-008 | 2026-09-13 | Product-name alignment, code signing, and the Restart action are **not** part of the fix release; they are raised as CR-002, CR-003, and CR-001. | They change functionality, footprint, or process | Include in 1.0.1 | Dev team per DEC-006 | Confirmed |
| DEC-009 | 2026-09-13 | Feature discussion starts after the defect-fix tasks are complete. | Client instruction | — | Client | Confirmed |
| DEC-010 | 2026-09-13 | The v1.0.0 baseline snapshot is stored **outside** the monitored folder, in `..\miautrix-ServiceTrayMonitor-baseline\`. | Avoid committing binaries to the repository; keep the monitored folder unchanged | Inside `docs/` | Dev team | Confirmed |
| DEC-011 | 2026-09-13 | Behaviour refinements made as part of fixes: Start on a *paused* service returns "use Resume" (1.0.0 threw a raw error); saving a credential requires a password unless the login is unchanged; the monitored-services window resets on reopen. | They make documented behaviour work and don't add functionality (SoW §5) | Log as CRs | Dev team | Proposed — sponsor to confirm at UAT |
| DEC-012 | 2026-09-13 | Every project document deliverable is issued as **DOCX**. The Markdown files in `docs/project/` are the source; the compiled document is `docs/project/ServiceTrayMonitor-Project-Document.docx`. | Client instruction ("Always DOCX") | PDF | Client | Confirmed |
| DEC-013 | 2026-09-13 | CR numbers from CR-004 onward are reserved for the new features the client will describe; none is assigned until a feature is submitted. | Keep the CR log to real requests only | Pre-register placeholder CRs | Dev team | Confirmed |

## Assumptions

| ID | Assumption | Validation | Status |
|---|---|---|---|
| A1 | Targets have the .NET 8 Desktop Runtime x64 (bootstrapper installs it if missing) | UAT-11 on a clean machine | Open |
| A2 | Operators launch the app elevated as their own admin account | R-02 | Open |
| A3 | Saved admin accounts hold "Log on as a batch job" (Administrators do by default) | UAT-06 on domain machines; R-03 | Open |
| A4 | Test accounts and a test machine are available for UAT | PM to arrange | Open |
| A5 | The `EventLog` service exists on every build/test machine (used by read-only tests) | Always present on Windows | Validated |
