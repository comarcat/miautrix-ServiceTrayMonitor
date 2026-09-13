# Service Tray Monitor — Project Documentation Pack

Source material for the formal project document of **Service Tray Monitor** (Miautrix), organised
along PMI (PMBOK® Guide) practices. Each file is one project artifact. Together they are compiled into
the project document, always issued as DOCX (DEC-012): `ServiceTrayMonitor-Project-Document.docx`.

| # | Artifact | File | PMI reference | Status |
|---|---|---|---|---|
| 01 | Project Charter & Stakeholder Register | [01-project-charter.md](01-project-charter.md) | Initiating — Develop Project Charter, Identify Stakeholders | Draft — pending sponsor sign-off |
| 02 | Statement of Work & Scope Statement | [02-statement-of-work.md](02-statement-of-work.md) | Planning — Define Scope | Draft — pending sign-off |
| 03 | WBS & WBS Dictionary | [03-wbs.md](03-wbs.md) | Planning — Create WBS | Current |
| 04 | Software Description (technical baseline) | [04-software-description.md](04-software-description.md) | Product scope / Configuration baseline | Current (v1.0.1) |
| 05 | Defect Log | [05-defect-log.md](05-defect-log.md) | Executing / M&C — Issue log, Control Quality | Current |
| 06 | Test Plan, Test Report & Traceability Matrix | [06-test-plan-and-report.md](06-test-plan-and-report.md) | Plan/Control Quality, Requirements Traceability Matrix | Current |
| 07 | Change Request Log & CR Form | [07-change-request-log.md](07-change-request-log.md) | M&C — Perform Integrated Change Control | Open |
| 08 | Risk Register | [08-risk-register.md](08-risk-register.md) | Planning / M&C — Risk management | Open |
| 09 | Decision & Assumption Log | [09-decision-log.md](09-decision-log.md) | Assumption log / Decision log | Current |
| 10 | Configuration Baseline & Release Record | [10-configuration-baseline.md](10-configuration-baseline.md) | Configuration management | Current |
| 11 | Lessons Learned Register | [11-lessons-learned.md](11-lessons-learned.md) | Manage Project Knowledge | Open |

## Governance rules (agreed 2026-09-13)

1. **Statement of Work** — the work started in the 2026-09-13 session: project analysis and knowledge
   base, defect testing and resolution, and preparation of this documentation.
2. **Defect fixes** are part of the SoW under *testing / solving*. They restore documented or clearly
   intended behaviour and are tracked in the [Defect Log](05-defect-log.md).
3. **Changes** — any new or altered functionality beyond intended behaviour — are **Change Requests**.
   They go through integrated change control ([CR Log](07-change-request-log.md)) and are not built
   until approved.
4. **Version control** is GitHub. The GitHub app monitors the folder `miautrix-ServiceTrayMonitor`;
   its name and location must not change.
5. PMI guidelines apply to planning, control, and closure artifacts.

## Conventions

| Prefix | Meaning | Status values |
|---|---|---|
| `DEF-nnn` | Defect | New → Analysed → Fixed → Verified (automated) → Accepted (UAT) → Closed |
| `CR-nnn` | Change request | Submitted → Under analysis → Approved / Rejected / Deferred → Implemented → Closed |
| `R-nn` | Risk | Open → Mitigating → Closed / Occurred |
| `DEC-nnn` | Decision | Proposed → Confirmed / Superseded |
| `AT-nn` | Automated test group | Pass / Fail |
| `UAT-nn` | User acceptance test case | Not run → Pass / Fail / Blocked |
| `LL-nn` | Lesson learned | — |

Dates are ISO 8601 (YYYY-MM-DD). Placeholders that need client input are marked **[TBD]**.
