# 03 — Work Breakdown Structure and WBS Dictionary

Status as of 2026-09-13.

```
1  Service Tray Monitor — Stabilisation and Documentation
├─ 1.1 Project management
│  ├─ 1.1.1 Charter and stakeholder register
│  ├─ 1.1.2 Statement of work and scope statement
│  ├─ 1.1.3 Change control process and CR log
│  ├─ 1.1.4 Risk register, decision and assumption log
│  └─ 1.1.5 Closure: lessons learned, final project document
├─ 1.2 Discovery and analysis (WP1)
│  ├─ 1.2.1 Source and installer review
│  ├─ 1.2.2 Knowledge base (graphify)
│  └─ 1.2.3 Findings and recommendations report
├─ 1.3 Configuration management (WP4)
│  ├─ 1.3.1 v1.0.0 baseline snapshot and hash manifest
│  ├─ 1.3.2 GitHub baseline commit (client)
│  └─ 1.3.3 Release 1.0.1 configuration record
├─ 1.4 Defect testing and resolution (WP2)
│  ├─ 1.4.1 Reproduction and root-cause analysis
│  ├─ 1.4.2 Defect fixes DEF-001…DEF-017
│  ├─ 1.4.3 Automated test suite
│  ├─ 1.4.4 Build verification
│  ├─ 1.4.5 Installer corrections and Release MSI build
│  └─ 1.4.6 User acceptance testing support
└─ 1.5 Documentation (WP3)
   ├─ 1.5.1 Software description
   ├─ 1.5.2 README update
   ├─ 1.5.3 Test plan, test report, traceability matrix
   └─ 1.5.4 Compiled project document
```

## WBS dictionary

| WBS | Work package | Description / output | Owner | Acceptance | Status |
|---|---|---|---|---|---|
| 1.1.1 | Charter | Doc 01 | PM [TBD] | Sponsor signature | Draft |
| 1.1.2 | SoW | Doc 02 | PM [TBD] | Sponsor signature | Draft |
| 1.1.3 | Change control | Doc 07, CR form | PM [TBD] | Process agreed | In place |
| 1.1.4 | Risk and decision logs | Docs 08, 09 | PM [TBD] | Reviewed at status meetings | Open |
| 1.1.5 | Closure | Doc 11, compiled document | PM [TBD] | Sponsor sign-off | Not started |
| 1.2.1 | Source review | All `.cs`, `.csproj`, `.sln`, manifest, `.vdproj` | Dev team | Findings presented | Complete |
| 1.2.2 | Knowledge base | `graphify-out/` (170 nodes, 293 edges, 12 communities) | Dev team | Graph renders; report generated | Complete |
| 1.2.3 | Findings report | 14 findings + decisions requested | Dev team | Client decisions received | Complete |
| 1.3.1 | v1.0.0 baseline | Zip + SHA-256 manifest (doc 10) | Dev team | Hashes recorded | Complete |
| 1.3.2 | GitHub baseline commit | Commit of v1.0.0, then 1.0.1 | Client | Commits visible in GitHub | Pending |
| 1.3.3 | 1.0.1 record | File hashes, build and test evidence | Dev team | Doc 10 complete | Complete |
| 1.4.1 | Root cause | Defect Log entries with evidence | Dev team | Each DEF has a root cause | Complete |
| 1.4.2 | Fixes | Code changes, 1.0.1 | Dev team | Build + tests pass | Complete |
| 1.4.3 | Test suite | xUnit project, 38 tests | Dev team | 100 % pass | Complete |
| 1.4.4 | Build verification | Release build 0 errors / 0 warnings | Dev team | Build log | Complete |
| 1.4.5 | Installer | `.vdproj` 1.0.1; Release MSI | Dev team / client | UAT-11 | Release MSI built (doc 10); UAT pending |
| 1.4.6 | UAT support | UAT-01…UAT-12 | Client testers | Signed test report | Not started |
| 1.5.1 | Software description | Doc 04 | Dev team | Reviewed | Complete |
| 1.5.2 | README | Updated README.md | Dev team | Consistent with 1.0.1 | Complete |
| 1.5.3 | Test documentation | Doc 06 | Dev team | Reviewed | Complete |
| 1.5.4 | Compiled document | `ServiceTrayMonitor-Project-Document.docx` (DOCX, DEC-012) | PM [TBD] | Sponsor sign-off | Draft compiled |

Change requests (CR-001…) are outside this WBS baseline. Once approved, each CR adds its own work
packages under a new element `1.6 Approved changes`.
