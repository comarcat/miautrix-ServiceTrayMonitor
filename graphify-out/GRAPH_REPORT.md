# Graph Report - miautrix-ServiceTrayMonitor  (2026-09-13)

## Corpus Check
- 35 files · ~22,557 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 495 nodes · 1253 edges · 20 communities (13 shown, 7 thin omitted)
- Extraction: 86% EXTRACTED · 14% INFERRED · 0% AMBIGUOUS · INFERRED: 179 edges (avg confidence: 0.86)
- Token cost: 130,544 input · 0 output

## Community Hubs (Navigation)
- Charter, Scope & Deliverables
- Main Grid & Icon UI
- Credential Logon Runner
- Defects, Tests & Quality Objectives
- Admin Credentials Form
- Service Selection Settings
- Architecture & Startup Flows
- Namespaces & Source Files
- Credential Fallback Traceability
- Status Palette & Credential Model
- Tray Shell & Sync Context
- README Feature Concepts
- Build & Test Dependencies
- Type Ref: Button
- Type Ref: Label
- Type Ref: Label (2)
- Type Ref: ServiceController
- Type Ref: IEnumerable
- Type Ref: ServiceControllerStatus
- Type Ref: MonitoredService

## God Nodes (most connected - your core abstractions)
1. `06 Test Plan, Test Report & Traceability Matrix` - 40 edges
2. `ServiceMonitorEngine` - 38 edges
3. `TrayAppContext` - 37 edges
4. `MonitoredService` - 28 edges
5. `01 Project Charter & Stakeholder Register` - 27 edges
6. `ManageServicesForm` - 26 edges
7. `05 Defect Log` - 24 edges
8. `ConfigManager` - 23 edges
9. `09 Decision & Assumption Log` - 23 edges
10. `SettingsForm` - 21 edges

## Surprising Connections (you probably didn't know these)
- `DPAPI Password Encryption` --references--> `ConfigManager`  [INFERRED]
  README.md → Services/ConfigManager.cs
- `Credential save and test flow` --references--> `CredentialsForm`  [INFERRED]
  docs/project/04-software-description.md → Forms/CredentialsForm.cs
- `DEF-011 Resource leaks (menu items, event handlers, service handles)` --references--> `ManageServicesForm`  [INFERRED]
  docs/project/05-defect-log.md → Forms/ManageServicesForm.cs
- `DEF-011 Resource leaks (menu items, event handlers, service handles)` --references--> `SettingsForm`  [INFERRED]
  docs/project/05-defect-log.md → Forms/SettingsForm.cs
- `config.json data schema (MonitoredServiceNames, PollIntervalSeconds, Credential*)` --shares_data_with--> `AppSettings`  [INFERRED]
  docs/project/04-software-description.md → Models/MonitoredService.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Own-token-then-credential fallback mechanism** — readme_credential_fallback, readme_batch_logon, docs_project_09_decision_log_dec_002, docs_project_05_defect_log_def_005, services_servicemonitorengine_servicetraymonitor_services_servicemonitorengine, services_elevatedservicerunner_servicetraymonitor_services_elevatedservicerunner, services_serviceactions_servicetraymonitor_services_serviceactions, docs_project_08_risk_register_r_01, docs_project_06_test_plan_and_report_uat_07 [INFERRED 0.85]
- **Start with Windows via per-user scheduled task** — readme_start_with_windows_task, docs_project_04_software_description_start_with_windows_flow, docs_project_05_defect_log_def_003, docs_project_09_decision_log_dec_004, services_startupmanager_servicetraymonitor_services_startupmanager, docs_project_04_software_description_task_scheduler, docs_project_06_test_plan_and_report_at_04, docs_project_06_test_plan_and_report_uat_09, docs_project_08_risk_register_r_04 [INFERRED 0.85]
- **Defect-vs-change classification and integrated change control** — docs_project_02_statement_of_work_defect_or_change_rule, docs_project_07_change_request_log_integrated_change_control_process, docs_project_03_wbs_1_6_approved_changes, docs_project_09_decision_log_dec_008, docs_project_07_change_request_log_cr_001, docs_project_07_change_request_log_cr_002, docs_project_07_change_request_log_cr_003, docs_project_08_risk_register_r_11 [INFERRED 0.85]

## Communities (20 total, 7 thin omitted)

### Community 0 - "Charter, Scope & Deliverables"
Cohesion: 0.06
Nodes (80): C1 GitHub folder must not be renamed or moved, C2 Installer is a VS Installer Project (.vdproj), C3 No real production services changed during testing, High-level requirements, O1 Understand and document the existing software, O5 Establish project control, 01 Project Charter & Stakeholder Register, Stakeholder register (+72 more)

### Community 1 - "Main Grid & Icon UI"
Cohesion: 0.06
Nodes (37): Bitmap, DataGridView, DataGridViewCellFormattingEventArgs, DataGridViewColumn, DataGridViewRow, Dictionary, Tray icon summary colour, Button (+29 more)

### Community 2 - "Credential Logon Runner"
Cohesion: 0.07
Nodes (25): DllImport, error, Exception, IntPtr, SafeAccessTokenHandle, Func, message, StoredCredential (+17 more)

### Community 3 - "Defects, Tests & Quality Objectives"
Cohesion: 0.09
Nodes (56): O2 Find and fix defects in v1.0.0, O4 Add a regression safety net, O6 Prepare a releasable build (1.0.1), config.json data schema (MonitoredServiceNames, PollIntervalSeconds, Credential*), Tests/ServiceTrayMonitor.Tests xUnit suite, DEF-001 Saving monitored services erases the saved admin credential, DEF-002 Filter loses checks; Save drops services hidden by the filter, DEF-006 UI freezes up to 15 s during service actions (+48 more)

### Community 4 - "Admin Credentials Form"
Cohesion: 0.09
Nodes (22): Button, StoredCredential, TextBox, CredentialsForm, IDisposable, JsonSerializerOptions, List, AppSettings (+14 more)

### Community 5 - "Service Selection Settings"
Cohesion: 0.12
Nodes (16): CheckBox, CheckedListBox, Form, List, TextBox, SettingsForm, IEnumerable, List (+8 more)

### Community 6 - "Architecture & Startup Flows"
Cohesion: 0.15
Nodes (23): Service Tray Monitor architecture (component diagram), Saving settings flow, 04 Software Description (Technical Baseline v1.0.1), Start with Windows flow (scheduled task), Status polling flow, System.ServiceProcess.ServiceController 8.0.0 dependency, Windows Task Scheduler, Windows Service Control Manager (+15 more)

### Community 7 - "Namespaces & Source Files"
Cohesion: 0.13
Nodes (7): ServiceTrayMonitor, ServiceTrayMonitor.Tests, ServiceTrayMonitor.Models, ServiceTrayMonitor.Services, ServiceTrayMonitor.Forms, Program, STAThread

### Community 8 - "Credential Fallback Traceability"
Cohesion: 0.21
Nodes (24): O3 Control services with stored admin credentials, Credential save and test flow, Security model, Service action flow (Start/Stop/Pause/Resume), DEF-004 Credential Test hangs the application, DEF-005 Saved credential makes service actions fail with Access is denied, DEF-008 Action results differ between direct and credential paths, AT-05 ServiceActionsTests (+16 more)

### Community 9 - "Status Palette & Credential Model"
Cohesion: 0.15
Nodes (16): Change inventory 1.0.1 vs 1.0.0, Color, ServiceControllerStatus, StatusPalette, StoredCredential, DisplayLogin, Domain, IsConfigured (+8 more)

### Community 10 - "Tray Shell & Sync Context"
Cohesion: 0.12
Nodes (12): ApplicationContext, ContextMenuStrip, LL-04 Install WindowsFormsSynchronizationContext before capturing it, Font, NotifyIcon, SynchronizationContext, ToolStripItem, ToolTipIcon (+4 more)

### Community 11 - "README Feature Concepts"
Cohesion: 0.13
Nodes (19): MonitoredService, StatusColor, Optional Admin Credentials, Built-in Administrator Account Workaround, Color-Coded Service Status, .NET 8 SDK, Main Window Sortable Grid, Manage Monitored Services Window (+11 more)

### Community 12 - "Build & Test Dependencies"
Cohesion: 0.20
Nodes (9): Microsoft.NET.Test.Sdk (18.10.0), System.ServiceProcess.ServiceController (8.0.0), xunit (2.9.3), xunit.runner.visualstudio (4.0.0), net8.0-windows, Microsoft.NET.Sdk, ServiceTrayMonitor, SetupSTM (+1 more)

## Ambiguous Edges - Review These
- `requireAdministrator manifest elevation` → `DEF-014 Build warning WFAC010 (DPI settings in manifest)`  [AMBIGUOUS]
  docs/project/05-defect-log.md · relation: references

## Knowledge Gaps
- **35 isolated node(s):** `DisplayLogin`, `Domain`, `IsConfigured`, `Password`, `Username` (+30 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 95 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **7 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `requireAdministrator manifest elevation` and `DEF-014 Build warning WFAC010 (DPI settings in manifest)`?**
  _Edge tagged AMBIGUOUS (relation: references) - confidence is low._
- **Why does `Service Tray Monitor architecture (component diagram)` connect `Architecture & Startup Flows` to `Main Grid & Icon UI`, `Credential Logon Runner`, `Defects, Tests & Quality Objectives`, `Admin Credentials Form`, `Service Selection Settings`, `Credential Fallback Traceability`, `Status Palette & Credential Model`, `Tray Shell & Sync Context`?**
  _High betweenness centrality (0.174) - this node is a cross-community bridge._
- **Why does `ServiceMonitorEngine` connect `Credential Logon Runner` to `Charter, Scope & Deliverables`, `Main Grid & Icon UI`, `Defects, Tests & Quality Objectives`, `Admin Credentials Form`, `Architecture & Startup Flows`, `Namespaces & Source Files`, `Credential Fallback Traceability`, `Status Palette & Credential Model`, `Tray Shell & Sync Context`?**
  _High betweenness centrality (0.141) - this node is a cross-community bridge._
- **Why does `TrayAppContext` connect `Tray Shell & Sync Context` to `Charter, Scope & Deliverables`, `Main Grid & Icon UI`, `Credential Logon Runner`, `Defects, Tests & Quality Objectives`, `Admin Credentials Form`, `Service Selection Settings`, `Architecture & Startup Flows`, `Namespaces & Source Files`, `Status Palette & Credential Model`?**
  _High betweenness centrality (0.135) - this node is a cross-community bridge._
- **Are the 5 inferred relationships involving `ServiceMonitorEngine` (e.g. with `DEF-011 Resource leaks (menu items, event handlers, service handles)` and `DEC-002 Keep Admin Credentials; own token first, retry as saved account via LogonUser batch/interactive + impersonation`) actually correct?**
  _`ServiceMonitorEngine` has 5 INFERRED edges - model-reasoned connections that need verification._
- **Are the 3 inferred relationships involving `TrayAppContext` (e.g. with `DEF-011 Resource leaks (menu items, event handlers, service handles)` and `LL-04 Install WindowsFormsSynchronizationContext before capturing it`) actually correct?**
  _`TrayAppContext` has 3 INFERRED edges - model-reasoned connections that need verification._
- **Are the 5 inferred relationships involving `01 Project Charter & Stakeholder Register` (e.g. with `A1 Targets have .NET 8 Desktop Runtime x64` and `A2 Operators launch the app elevated as their own admin account`) actually correct?**
  _`01 Project Charter & Stakeholder Register` has 5 INFERRED edges - model-reasoned connections that need verification._