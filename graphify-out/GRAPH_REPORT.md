# Graph Report - miautrix-ServiceTrayMonitor  (2026-09-13)

## Corpus Check
- 46 files · ~33,616 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 891 nodes · 2276 edges · 44 communities (35 shown, 9 thin omitted)
- Extraction: 88% EXTRACTED · 12% INFERRED · 0% AMBIGUOUS · INFERRED: 272 edges (avg confidence: 0.85)
- Token cost: 191,953 input · 0 output

## Community Hubs (Navigation)
- Main Grid & Status Polling UI
- Activation Window & Licensing Client
- Architecture & Startup Task Flows
- Licensing Tests & License State
- Credential Fallback & Logon Runner
- Defect Fixes & UAT (1.0.1)
- Tray Shell & Core Types
- Documentation & README Concepts
- Paid Branch & Licensing Forms
- Namespaces & Source Files
- Service Selection Settings
- Paid Edition Tests & Builds
- Service Monitor Engine
- License Decision Rules & API Flows
- Change Control & Governance
- Scope, Installer & Product Overview
- Stakeholders & Security Risks
- Configuration Baseline & Milestones
- Licensing Package & Paid Decisions
- Hardware Fingerprint & Vendor Feedback
- API Result DTO
- Result Code Enum
- Test Strategy & Lessons Learned
- SoW Deliverables
- Activation Result Data DTO
- Activate & Check-in Requests
- Activation API HTTP Client
- WBS Root & Branch Strategy
- Rejection Handling & Live Test Findings
- Charter Objectives & Milestones
- Project Management & DOCX
- Hardware Info DTO
- Licensing API Adapter
- Test Project & Solution
- Project Build Dependencies
- Type Ref: Button
- Type Ref: Label
- Type Ref: Label (2)
- Type Ref: ServiceController
- Type Ref: IEnumerable
- Type Ref: ServiceControllerStatus
- Type Ref: MonitoredService
- Type Ref: message
- Type Ref: success

## God Nodes (most connected - your core abstractions)
1. `CR-004 License key activation - paid edition test build 1.0.1-p (Approved)` - 66 edges
2. `LicenseManager` - 52 edges
3. `06 Test Plan, Test Report & Traceability Matrix` - 50 edges
4. `TrayAppContext` - 48 edges
5. `04 Software Description (technical baseline v1.0.1)` - 44 edges
6. `ServiceMonitorEngine` - 34 edges
7. `Service Tray Monitor` - 32 edges
8. `LicenseState` - 31 edges
9. `09 Decision & Assumption Log` - 30 edges
10. `01 Project Charter & Stakeholder Register` - 29 edges

## Surprising Connections (you probably didn't know these)
- `Feedback 2: ActivationApiClient comment gives LAN address as http instead of https` --references--> `ActivationApiClient`  [INFERRED]
  docs/project/07-change-request-log.md → Licensing/Client/ApiClient.cs
- `Tray menu per-service Start/Stop/Pause/Resume` --references--> `TrayAppContext`  [INFERRED]
  README.md → TrayAppContext.cs
- `DEF-011 Resource leaks (menu items, event handlers, service handles) (Low)` --references--> `ManageServicesForm`  [INFERRED]
  docs/project/05-defect-log.md → Forms/ManageServicesForm.cs
- `Main Window Sortable Grid` --references--> `ManageServicesForm`  [INFERRED]
  README.md → Forms/ManageServicesForm.cs
- `AT-01 StatusAndRulesTests (15 tests)` --references--> `ServiceActionRules`  [INFERRED]
  docs/project/06-test-plan-and-report.md → Models/StatusPalette.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Paid edition only (branch paid)** — docs_project_readme_branch_paid, licensing_licensemanager_servicetraymonitor_licensing_licensemanager, licensing_licenseevaluator_servicetraymonitor_licensing_licenseevaluator, licensing_licensestate_servicetraymonitor_licensing_licensestate, licensing_licensestate_servicetraymonitor_licensing_licensestatestore, licensing_licensingapi_servicetraymonitor_licensing_licensingapi, licensing_licensingapi_servicetraymonitor_licensing_ilicensingapi, forms_activationform_servicetraymonitor_forms_activationform, forms_licenseform_servicetraymonitor_forms_licenseform [EXTRACTED 1.00]
- **Own-token-first credential fallback mechanism** — docs_project_09_decision_log_dec_002, docs_project_05_defect_log_def_005, docs_project_04_software_description_flow_service_action, docs_project_04_software_description_batch_logon_impersonation, services_elevatedservicerunner_servicetraymonitor_services_elevatedservicerunner, docs_project_08_risk_register_r_01, docs_project_06_test_plan_and_report_uat_07 [INFERRED 0.85]
- **7-day license rule (decision, rules, tests)** — docs_project_09_decision_log_dec_015, docs_project_04_software_description_license_decision_rules, docs_project_04_software_description_decision_grace, docs_project_04_software_description_decision_blocked, docs_project_06_test_plan_and_report_req_14, docs_project_06_test_plan_and_report_at_08, docs_project_06_test_plan_and_report_uat_17 [INFERRED 0.85]

## Communities (44 total, 9 thin omitted)

### Community 0 - "Main Grid & Status Polling UI"
Cohesion: 0.06
Nodes (40): Bitmap, DataGridView, DataGridViewCellFormattingEventArgs, DataGridViewColumn, DataGridViewRow, Dictionary, Flow 4.1 Status polling (serialized refreshes, skipped overlapping ticks), Button (+32 more)

### Community 1 - "Activation Window & Licensing Client"
Cohesion: 0.06
Nodes (36): Action, Miautrix.Licensing.Client, Task, DateTime, Guid, LicenseFileContents, ActivationId, InstallGuid (+28 more)

### Community 2 - "Architecture & Startup Task Flows"
Cohesion: 0.06
Nodes (41): Architecture (component flowchart), config.json schema (%AppData%, per user), Flow 4.3 Saving settings (SaveGeneralSettings merge, atomic write), Flow 4.5 Start with Windows (per-user scheduled task, legacy Run-key removal), Windows Task Scheduler, Windows Service Control Manager (SCM), DEF-003 Start with Windows never starts the app (High), AT-04 StartupManagerTests (2 tests) (+33 more)

### Community 3 - "Licensing Tests & License State"
Cohesion: 0.08
Nodes (31): Fact, InlineData, RSA, DateTime, Guid, LicenseState, ActivationId, BlockedReason (+23 more)

### Community 4 - "Credential Fallback & Logon Runner"
Cohesion: 0.08
Nodes (34): DllImport, O3 Control services with stored admin credentials, LogonUser batch-then-interactive logon plus impersonation, Flow 4.4 Credential save and test, Flow 4.2 Service action with credential fallback, DEF-004 Credential Test hangs the application (High), DEF-005 A saved credential makes service actions fail with Access is denied (High), AT-05 ServiceActionsTests (4 tests) (+26 more)

### Community 5 - "Defect Fixes & UAT (1.0.1)"
Cohesion: 0.11
Nodes (41): M5 User acceptance testing (UAT-01...UAT-12), WBS 1.4 Defect testing and resolution (WP2), WBS 1.4.1 Reproduction and root-cause analysis, WBS 1.4.2 Defect fixes DEF-001...DEF-017, WBS 1.4.4 Build verification, WBS 1.4.6 User acceptance testing support, DEF-001 Saving monitored services erases the saved admin credential (High), DEF-002 Filter loses checks; Save drops services hidden by the filter (High) (+33 more)

### Community 6 - "Tray Shell & Core Types"
Cohesion: 0.10
Nodes (19): ApplicationContext, AppSettings, ContextMenuStrip, CredentialsForm, Font, ManageServicesForm, message, MonitoredService (+11 more)

### Community 7 - "Documentation & README Concepts"
Cohesion: 0.08
Nodes (34): WBS 1.5 Documentation (WP3), WBS 1.5.1 Software description, WBS 1.5.2 README update, WBS 1.5.3 Test plan, test report, traceability matrix, AppSettings, MonitoredService, StatusColor, Optional admin credential with retry-as-saved-account fallback (+26 more)

### Community 8 - "Paid Branch & Licensing Forms"
Cohesion: 0.11
Nodes (23): Color, M8 Paid edition test build 1.0.1-p (CR-004), WBS 1.6.1.2 Implementation (licensing module, forms, startup gate), Paid edition licensing components (9.1), Paid edition licensing (branch paid, 1.0.1-p), Branch paid (secondary, 1.0.1-p test build), Form, Button (+15 more)

### Community 9 - "Namespaces & Source Files"
Cohesion: 0.10
Nodes (12): ServiceTrayMonitor, ServiceTrayMonitor.Tests, ServiceTrayMonitor.Licensing, ServiceTrayMonitor.Models, ServiceTrayMonitor.Services, ServiceTrayMonitor.Forms, StoredCredential, DisplayLogin (+4 more)

### Community 10 - "Service Selection Settings"
Cohesion: 0.12
Nodes (16): CheckBox, CheckedListBox, Functional summary, List, TextBox, SettingsForm, IEnumerable, List (+8 more)

### Community 11 - "Paid Edition Tests & Builds"
Cohesion: 0.21
Nodes (30): WBS 1.6.1 CR-004 Paid edition test build (1.0.1-p), WBS 1.6.1.4 Live licensing test, AT-07 LicenseKeyTests (8 tests, paid), AT-08 LicenseManagerTests (17 tests, paid), AT-09 LicenseStateStoreTests (3 tests, paid), Automated test suite (38 tests on 1.0.1; 66 on 1.0.1-p), BV-01 1.0.1 Release build (0 errors, 0 warnings), BV-02 1.0.1-p Release build (0 errors, 0 warnings) (+22 more)

### Community 12 - "Service Monitor Engine"
Cohesion: 0.15
Nodes (10): Func, IEnumerable, List, message, ServiceController, success, ServiceMonitorEngine, CurrentStatuses (+2 more)

### Community 13 - "License Decision Rules & API Flows"
Cohesion: 0.16
Nodes (26): POST /api/activate, POST /api/checkin, Build, test, and packaging commands, Decision Grace (pending review / not received, within 7 days), Decision Licensed (approved, verified within 7 days), Decision NotActivated (no key saved -> activation window), Flow 9.2.4 Activate with a different key (failure keeps current license), Flow 9.2.2 Activation (normalise key, format check, fingerprint, activate, verify, save) (+18 more)

### Community 14 - "Change Control & Governance"
Cohesion: 0.18
Nodes (19): High-level requirements (keep monitoring, credentials, DPAPI, elevation, CR-only changes), D2 Findings and recommendations report, Integrated change control process (submit, analyse, decide, plan, implement, close), 07 Change Request Log & CR Form, Further features as CR-005 onward, CR form template, R-11 Feature requests during UAT cause scope creep (score 9), A5 EventLog service exists on every build/test machine (read-only tests) (+11 more)

### Community 15 - "Scope, Installer & Product Overview"
Cohesion: 0.21
Nodes (18): C1 GitHub source control; folder miautrix-ServiceTrayMonitor not renamed or moved, C2 Installer is a Visual Studio Installer Project (.vdproj), High-level scope (in: analysis, defect fixes, tests, installer, docs; out: new features), SoW exclusions (change control required), WBS 1.4.5 Installer corrections and Release MSI build, Installer Setup/SetupSTM (vdproj -> SetupSTM.msi + setup.exe), Known limitations (1.0.1), Product overview (WinForms tray app, .NET 8, requireAdministrator) (+10 more)

### Community 16 - "Stakeholders & Security Risks"
Cohesion: 0.12
Nodes (18): Development team (AI-assisted engineering with Claude Code), IT administrators / operators (end users), Product owner, Miautrix, Project manager, Security / infrastructure owner, Sponsor, Stakeholder register, Security model (elevated process, DPAPI, in-process impersonation, unsigned binaries) (+10 more)

### Community 17 - "Configuration Baseline & Milestones"
Cohesion: 0.17
Nodes (17): M2 v1.0.0 configuration baseline captured, M4 Baseline and 1.0.1 committed to GitHub, M6 Release 1.0.1 approved, O5 Establish project control (CR process, risks, decisions, baseline), O6 Releasable build 1.0.1 (0 errors/0 warnings; in-place upgrade), WBS 1.3 Configuration management (WP4), WBS 1.3.1 v1.0.0 baseline snapshot and hash manifest, WBS 1.3.2 GitHub baseline commit (client) (+9 more)

### Community 18 - "Licensing Package & Paid Decisions"
Cohesion: 0.24
Nodes (16): WBS 1.6.1.1 Package review and questions, WBS 1.6.1.3 Paid installer (SetupSTM-Paid.msi), System.Management 8.0.0 (WMI) dependency, Vendor activation client package v2 (Licensing/Client, unmodified), UAT-21 Paid installer (paid), DEC-014 CR-004 integrates vendor package v2 source files plus System.Management, not the compiled DLL, DEC-017 License state per machine in %ProgramData%\ServiceTrayMonitor\license.json, DEC-018 Paid edition version 1.0.1-p; separate installer product SystemTrayMonitor (Paid Test) (+8 more)

### Community 19 - "Hardware Fingerprint & Vendor Feedback"
Cohesion: 0.23
Nodes (11): WMI hardware fingerprint, IT-01 Live integration: activation pending review, check-in, unknown key, IT-03 Same key from simulated second machine gets its own activation, Feedback for the licensing team (activation client package v2), Feedback 1: result-code table points to the wrong section for the key format, Feedback 2: ActivationApiClient comment gives LAN address as http instead of https, Feedback 3: ReadTpmId returns TPM vendor ID and falls back to machine name, Feedback 4: ReadPrimaryMac picks first Up Ethernet adapter (Hyper-V virtual adapters) (+3 more)

### Community 20 - "API Result DTO"
Cohesion: 0.21
Nodes (13): ApiResult, Code, Data, Message, Success, Queue, CancellationToken, Func (+5 more)

### Community 21 - "Result Code Enum"
Cohesion: 0.14
Nodes (14): ResultCode, Activated, ActivationNotFound, InstallGuidMismatch, InvalidKeyFormat, LicenseExpired, LicenseNotFound, LicenseRevoked (+6 more)

### Community 22 - "Test Strategy & Lessons Learned"
Cohesion: 0.15
Nodes (13): C3 No real production services changed during development testing, Classification rule: defect or change?, Non-invasive test rule (no service changes, no scheduled tasks, no real config.json), Test strategy (RP, unit/component/integration AT, BV, UAT; entry/exit criteria), A4 Test accounts and a test machine are available for UAT, 11 Lessons Learned Register, LL-01 Test security-sensitive features in the exact elevation scenario with a real second account, LL-02 Drain redirected stdout/stderr concurrently; prefer in-process APIs (+5 more)

### Community 23 - "SoW Deliverables"
Cohesion: 0.26
Nodes (13): D1 Knowledge base (graphify-out), D3 v1.0.0 baseline snapshot and hash manifest, D4 Source code 1.0.1 with defect fixes, D5 Automated test suite (Tests/ServiceTrayMonitor.Tests), D6 Installer project 1.0.1, D7 UAT test cases, D8 Project documentation pack, D9 Updated README (+5 more)

### Community 24 - "Activation Result Data DTO"
Cohesion: 0.17
Nodes (12): ActivationResultData, ActivationId, LicenseFileBase64, Policy, Reason, ReviewDeadlineUtc, Status, SubscriptionExpiryUtc (+4 more)

### Community 25 - "Activate & Check-in Requests"
Cohesion: 0.17
Nodes (13): DateTime, Guid, ActivateRequest, AppVersion, ClientTimestampUtc, Hardware, InstallGuid, LicenseKey (+5 more)

### Community 26 - "Activation API HTTP Client"
Cohesion: 0.29
Nodes (7): HttpClient, HttpResponseMessage, CancellationToken, HttpStatus, Result, Task, ActivationApiClient

### Community 27 - "WBS Root & Branch Strategy"
Cohesion: 0.22
Nodes (11): WBS 1 Service Tray Monitor - Stabilisation and Documentation, WBS 1.2 Discovery and analysis (WP1), WBS 1.2.1 Source and installer review, WBS 1.2.3 Findings and recommendations report, WBS 1.6 Approved changes, 03 WBS & WBS Dictionary, Branch main (default), Branch release/1.0.1 (primary release line) (+3 more)

### Community 28 - "Rejection Handling & Live Test Findings"
Cohesion: 0.40
Nodes (11): Decision Blocked (rejection, >7 days unverified, subscription grace ended, bad signature), DEF-018 (paid) Rejected activation shows raw reason code ACTIVATION_REJECTED (Low, closed won't fix), DEF-019 (paid) Server error (HTTP 500) reported as Couldn't reach the licensing server (Low, closed won't fix), IT-04 Rejected second-machine activation: Locked/Blocked; server error on re-activation, IT-05 Raw HTTP capture of re-activation: not reproduced; numeric result codes, IT-06 Check-in after second rejection replayed through LicenseManager: Blocked, Feedback 5: result codes sent as numbers, not strings, Feedback 6: server error on a rejected installation's re-activation (+3 more)

### Community 29 - "Charter Objectives & Milestones"
Cohesion: 0.27
Nodes (10): M1 Analysis and knowledge base delivered, M3 Defect fixes implemented and automated tests passing, M7 Feature planning (change requests), O1 Understand and document the existing software, O2 Find and fix defects in v1.0.0, O4 Regression safety net (automated tests, 100% pass), 01 Project Charter & Stakeholder Register, WBS 1.2.2 Knowledge base (graphify) (+2 more)

### Community 30 - "Project Management & DOCX"
Cohesion: 0.25
Nodes (9): WBS 1.1 Project management, WBS 1.1.1 Charter and stakeholder register, WBS 1.1.2 Statement of work and scope statement, WBS 1.1.3 Change control process and CR log, WBS 1.1.4 Risk register, decision and assumption log, WBS 1.1.5 Closure: lessons learned, final project document, WBS 1.5.4 Compiled project document, DEC-012 Every project document deliverable issued as DOCX (+1 more)

### Community 31 - "Hardware Info DTO"
Cohesion: 0.22
Nodes (9): HardwareInfo, CpuId, CpuModel, MacAddressPrimary, MotherboardSerial, OsType, OsVersion, RamGb (+1 more)

### Community 32 - "Licensing API Adapter"
Cohesion: 0.57
Nodes (4): CancellationToken, HttpStatus, Result, Task

### Community 33 - "Test Project & Solution"
Cohesion: 0.29
Nodes (5): Microsoft.NET.Test.Sdk (18.10.0), xunit (2.9.3), xunit.runner.visualstudio (4.0.0), SetupSTM, ServiceTrayMonitor.Tests

### Community 34 - "Project Build Dependencies"
Cohesion: 0.40
Nodes (4): net8.0-windows, System.Management (8.0.0), System.ServiceProcess.ServiceController (8.0.0), Microsoft.NET.Sdk

## Knowledge Gaps
- **124 isolated node(s):** `MonitoredService`, `ServiceTrayMonitor.csproj`, `Start with Windows Toggle`, `SelectedService`, `CanPauseAndContinue` (+119 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 219 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **9 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `LicenseManager` connect `Activation Window & Licensing Client` to `Architecture & Startup Task Flows`, `Licensing Tests & License State`, `Tray Shell & Core Types`, `Paid Branch & Licensing Forms`, `Paid Edition Tests & Builds`, `License Decision Rules & API Flows`, `Licensing Package & Paid Decisions`, `Hardware Fingerprint & Vendor Feedback`, `Rejection Handling & Live Test Findings`, `Hardware Info DTO`?**
  _High betweenness centrality (0.184) - this node is a cross-community bridge._
- **Why does `TrayAppContext` connect `Tray Shell & Core Types` to `Main Grid & Status Polling UI`, `Activation Window & Licensing Client`, `Architecture & Startup Task Flows`, `Defect Fixes & UAT (1.0.1)`, `Documentation & README Concepts`, `Paid Branch & Licensing Forms`, `Namespaces & Source Files`, `Service Selection Settings`, `Paid Edition Tests & Builds`, `License Decision Rules & API Flows`, `Scope, Installer & Product Overview`, `Licensing Package & Paid Decisions`, `Test Strategy & Lessons Learned`, `Charter Objectives & Milestones`?**
  _High betweenness centrality (0.155) - this node is a cross-community bridge._
- **Why does `Architecture (component flowchart)` connect `Architecture & Startup Task Flows` to `Main Grid & Status Polling UI`, `Credential Fallback & Logon Runner`, `Tray Shell & Core Types`, `Paid Branch & Licensing Forms`, `Namespaces & Source Files`, `Service Selection Settings`, `Service Monitor Engine`, `License Decision Rules & API Flows`, `Scope, Installer & Product Overview`?**
  _High betweenness centrality (0.132) - this node is a cross-community bridge._
- **Are the 9 inferred relationships involving `CR-004 License key activation - paid edition test build 1.0.1-p (Approved)` (e.g. with `License decision rules (9.3)` and `DEC-015 7-day rule for unconfirmed and unverified licenses`) actually correct?**
  _`CR-004 License key activation - paid edition test build 1.0.1-p (Approved)` has 9 INFERRED edges - model-reasoned connections that need verification._
- **Are the 5 inferred relationships involving `LicenseManager` (e.g. with `WBS 1.6.1.2 Implementation (licensing module, forms, startup gate)` and `Flow 9.2.2 Activation (normalise key, format check, fingerprint, activate, verify, save)`) actually correct?**
  _`LicenseManager` has 5 INFERRED edges - model-reasoned connections that need verification._
- **Are the 5 inferred relationships involving `TrayAppContext` (e.g. with `Flow 4.1 Status polling (serialized refreshes, skipped overlapping ticks)` and `DEF-011 Resource leaks (menu items, event handlers, service handles) (Low)`) actually correct?**
  _`TrayAppContext` has 5 INFERRED edges - model-reasoned connections that need verification._
- **What connects `MonitoredService`, `ServiceTrayMonitor.csproj`, `Start with Windows Toggle` to the rest of the system?**
  _124 weakly-connected nodes found - possible documentation gaps or missing edges._