# 01 — Project Charter

| Field | Value |
|---|---|
| Project | Service Tray Monitor — Stabilisation and Documentation |
| Product | Service Tray Monitor (assembly `ServiceTrayMonitor`, installer `SetupSTM`) |
| Organisation | Miautrix |
| Charter date | 2026-09-13 |
| Document version | 0.1 (draft) |
| Sponsor | [TBD] |
| Project manager | [TBD] |

## 1. Purpose and business justification

Service Tray Monitor is a Windows system-tray application that lets administrators watch selected
Windows services and start, stop, pause, and resume them. Version 1.0.0 is deployed and in use. Before
new features are added, the product needs a verified technical baseline: a documented architecture, a
list of known defects with fixes, automated tests, and a controlled way to handle future changes.

## 2. Objectives and success criteria

| # | Objective | Success criterion |
|---|---|---|
| O1 | Understand and document the existing software | Knowledge base (graphify) and software description are published in the repository |
| O2 | Find and fix defects in v1.0.0 | Every logged defect has a root cause, a fix, and a verification reference; UAT accepted by the sponsor |
| O3 | Guarantee the app can control services with stored admin credentials | UAT-06 and UAT-07 pass on the target environment |
| O4 | Add a regression safety net | Automated test suite in the solution; 100 % pass on the release build |
| O5 | Establish project control | Change-request process, risk register, decision log, and configuration baseline in place |
| O6 | Prepare a releasable build | Version 1.0.1 builds with 0 errors/0 warnings; installer upgrades 1.0.0 in place |

## 3. High-level requirements

- Keep monitoring and control of Windows services from the tray icon and the main window.
- Keep the **Admin Credentials** capability: service actions must succeed when the app's own rights
  are refused and a valid admin account is saved (decision DEC-002).
- Settings and credentials persist in `%AppData%\ServiceTrayMonitor\config.json`; the password stays
  DPAPI-encrypted.
- The app keeps requiring administrator elevation (DEC-003).
- New functionality enters only through approved change requests.

## 4. High-level scope

**In scope:** see the [Statement of Work](02-statement-of-work.md) — analysis and knowledge base, defect
testing and resolution (v1.0.1), automated tests, installer corrections, and project documentation.

**Out of scope:** new features (for example CR-001, Restart action), product renaming (CR-002), code
signing (CR-003), and deployment to production. All are handled through change control.

## 5. Summary milestones

| Milestone | Target date | Status |
|---|---|---|
| M1 Analysis and knowledge base delivered | 2026-09-13 | Complete |
| M2 v1.0.0 configuration baseline captured | 2026-09-13 | Complete (zip + SHA-256 manifest) |
| M3 Defect fixes implemented and automated tests passing | 2026-09-13 | Complete |
| M4 Baseline and 1.0.1 committed to GitHub | [TBD] | Pending — client action |
| M5 User acceptance testing (UAT-01…UAT-12) | [TBD] | Not started |
| M6 Release 1.0.1 approved | [TBD] | Not started |
| M7 Feature planning (change requests) | After M3 | In progress — CR-004 approved |
| M8 Paid edition test build 1.0.1-p (CR-004) | 2026-09-13 | Implemented on branch `paid` — UAT pending |

## 6. Stakeholder register

| Stakeholder / role | Interest | Influence | Engagement / communication |
|---|---|---|---|
| Sponsor — [TBD] | Reliable service control, controlled change | High | Approves charter, CRs, UAT, release |
| Project manager — [TBD] | Scope, schedule, PMI artifacts | High | Owns logs; weekly status [TBD] |
| Product owner, Miautrix — [TBD] | Feature roadmap | High | Submits and prioritises CRs |
| IT administrators / operators (end users) | Day-to-day service control from the tray | Medium | UAT participants; release notes |
| Security / infrastructure owner — [TBD] | Credential storage, elevation, batch-logon rights, scheduled tasks | Medium | Reviews R-02, R-03, R-04 |
| Development team (AI-assisted engineering with Claude Code) | Implementation, tests, documentation | Medium | Delivers defect fixes and artifacts |

## 7. Assumptions and constraints

**Assumptions**
- A1 Target platform is Windows 10/11 or Windows Server with the .NET 8 Desktop Runtime (x64).
- A2 Operators run the app as an administrator (UAC prompt at launch).
- A3 Accounts saved under Admin Credentials have the "Log on as a batch job" right, which
  Administrators have by default.
- A4 Test accounts and a test machine will be provided for UAT.

**Constraints**
- C1 Source control is GitHub; the working folder `miautrix-ServiceTrayMonitor` must not be renamed or moved.
- C2 The installer is a Visual Studio Installer Project (`.vdproj`), built only inside Visual Studio.
- C3 No real services on production machines are changed during development testing.

## 8. High-level risks

See the [Risk Register](08-risk-register.md). Top risks: R-01 (credential path not yet verified with a
real alternate account), R-03 (batch-logon right removed by policy), R-05 (no GitHub baseline commit yet).

## 9. Approval

| Role | Name | Signature | Date |
|---|---|---|---|
| Sponsor | [TBD] | | |
| Project manager | [TBD] | | |
