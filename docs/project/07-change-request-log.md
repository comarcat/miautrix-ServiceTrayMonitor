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

New features to be discussed after the defect-fix tasks will be added here as CR-004 onward.

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
