# 02 — Statement of Work and Scope Statement

| Field | Value |
|---|---|
| Project | Service Tray Monitor — Stabilisation and Documentation |
| SoW baseline date | 2026-09-13 |
| Document version | 0.1 (draft) |
| Product version in scope | 1.0.0 (baseline) → 1.0.1 (defect-fix release) |

## 1. Background

Service Tray Monitor v1.0.0 (C#/.NET 8 WinForms, MSI installer) is deployed and working. The client plans
to add features. Before that, the client asked for (a) a full understanding of the project, captured in
a knowledge base, (b) the problems found to be reported and solved, and (c) a project document following
PMI guidelines.

## 2. Scope of work

### WP1 — Discovery, analysis, and knowledge base
- Review all source, project, manifest, and installer files.
- Build a knowledge graph with graphify (`graphify-out/graph.html`, `GRAPH_REPORT.md`, `graph.json`).
- Report findings and recommendations to the client before any change is made.

### WP2 — Defect testing and resolution ("testing / solving")
- Reproduce or confirm each defect and record its root cause ([Defect Log](05-defect-log.md)).
- Fix defects so the product behaves as documented and intended.
- Add an automated test suite covering the fixed logic.
- Verify the release build (0 errors, 0 warnings).
- Correct installer upgrade settings; produce version 1.0.1.
- Provide UAT test cases and support the client's UAT.

### WP3 — Project documentation (PMI)
- Charter, SoW, WBS, software description, defect log, test plan and report, CR log, risk register,
  decision log, configuration baseline, lessons learned.
- Keep the README consistent with the behaviour of 1.0.1.

### WP4 — Configuration management
- Capture the v1.0.0 baseline (zip snapshot + SHA-256 manifest) before the first change.
- Record the 1.0.1 configuration (file hashes, build and test evidence).
- The client commits the baseline and the release to GitHub.

## 3. Deliverables

| ID | Deliverable | Acceptance criterion | Status |
|---|---|---|---|
| D1 | Knowledge base (`graphify-out/`) | Graph opens in a browser; report lists components and communities | Delivered |
| D2 | Findings and recommendations report | Presented to the client before changes; decisions recorded | Delivered (session record, DEC-001…DEC-009) |
| D3 | v1.0.0 baseline snapshot and hash manifest | Zip exists outside the monitored folder; hashes in doc 10 | Delivered |
| D4 | Source code 1.0.1 with defect fixes | DEF-001…DEF-017 fixed; build 0 errors/0 warnings | Delivered — pending UAT |
| D5 | Automated test suite (`Tests/ServiceTrayMonitor.Tests`) | All tests pass on Release | Delivered |
| D6 | Installer project 1.0.1 | Upgrades 1.0.0 in place; no .NET Framework 4.7.2 prerequisite | Release MSI built — UAT-11 pending |
| D7 | UAT test cases | UAT-01…UAT-12 defined with expected results | Delivered |
| D8 | Project documentation pack (`docs/project/`) | Artifacts 01–11 present and cross-referenced | Delivered (draft) |
| D9 | Updated README | Describes 1.0.1 behaviour, tests, troubleshooting | Delivered |

## 4. Exclusions (out of scope — change control required)

- New features of any kind, including the **Restart** action (CR-001).
- Renaming the installed product "SystemTrayMonitor" to match the app name (CR-002).
- Code signing of the executable or MSI (CR-003).
- Changing the elevation model (for example, running without administrator rights or adding a helper
  Windows service).
- Deployment or installation on production machines, and changes to their services, policies, or
  scheduled tasks.
- Renaming or moving the repository folder `miautrix-ServiceTrayMonitor`.

## 5. Classification rule: defect or change?

| Question | Yes → | No → |
|---|---|---|
| Does the current behaviour contradict the README, the UI text, or evident design intent, or does it lose data, hang, or leak? | Defect (in SoW, WP2) | Next question |
| Does the request add, remove, or alter functionality, UI, installed footprint, or operating model? | Change request (CR log) | Clarify with the sponsor |

Borderline items resolved as defects are recorded with their rationale in the [Decision Log](09-decision-log.md).

## 6. Acceptance

- Defect fixes are accepted when the linked UAT cases pass on the client's test environment and the
  sponsor signs the test report (doc 06).
- Documentation is accepted on sponsor review of the compiled project document.

## 7. Constraints and assumptions

See Charter §7. In addition, development-time testing is non-invasive: automated tests only *read*
service status and use a temporary configuration folder.

## 8. Approval

| Role | Name | Signature | Date |
|---|---|---|---|
| Sponsor | [TBD] | | |
| Project manager | [TBD] | | |
