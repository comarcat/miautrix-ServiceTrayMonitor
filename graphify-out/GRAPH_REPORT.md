# Graph Report - miautrix-ServiceTrayMonitor  (2026-09-13)

## Corpus Check
- Corpus is ~7,312 words - fits in a single context window. You may not need a graph.

## Summary
- 170 nodes · 293 edges · 12 communities (11 shown, 1 thin omitted)
- Extraction: 95% EXTRACTED · 5% INFERRED · 0% AMBIGUOUS · INFERRED: 15 edges (avg confidence: 0.78)
- Token cost: 69,492 input · 0 output

## Community Hubs (Navigation)
- README Feature Concepts
- Tray Context & Credentials UI
- Service Monitor Engine
- Elevated Runner & Credential Model
- Main Service Grid Window
- Namespaces & File Structure
- Config Persistence & Settings
- Service Status Model
- Runtime Icon Rendering
- Service Selection Form
- Project Build Config
- Solution & Installer

## God Nodes (most connected - your core abstractions)
1. `TrayAppContext` - 20 edges
2. `ServiceMonitorEngine` - 19 edges
3. `MonitoredService` - 16 edges
4. `StoredCredential` - 15 edges
5. `ManageServicesForm` - 14 edges
6. `SettingsForm` - 14 edges
7. `AppSettings` - 13 edges
8. `CredentialsForm` - 11 edges
9. `ServiceTrayMonitor.Models` - 9 edges
10. `Service Tray Monitor` - 9 edges

## Surprising Connections (you probably didn't know these)
- `ManageServicesForm` --references--> `MonitoredService`  [EXTRACTED]
  Forms/ManageServicesForm.cs → Models/MonitoredService.cs
- `ManageServicesForm` --references--> `ServiceMonitorEngine`  [EXTRACTED]
  Forms/ManageServicesForm.cs → Services/ServiceMonitorEngine.cs
- `TrayAppContext` --references--> `ManageServicesForm`  [EXTRACTED]
  TrayAppContext.cs → Forms/ManageServicesForm.cs
- `SettingsForm` --references--> `AppSettings`  [EXTRACTED]
  Forms/SettingsForm.cs → Models/MonitoredService.cs
- `TrayAppContext` --references--> `SettingsForm`  [EXTRACTED]
  TrayAppContext.cs → Forms/SettingsForm.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Resolving UAC Access Denied for Alternate Credentials** — readme_uac_token_filtering, readme_admin_credentials, readme_builtin_administrator_account, readme_service_acl_least_privilege [EXTRACTED 1.00]
- **Settings Persistence to config.json** — services_configmanager_configmanager, models_monitoredservice_appsettings, readme_config_json, readme_poll_interval, readme_dpapi [INFERRED 0.85]
- **Color-Coded Status Visualization** — readme_color_coded_status, models_monitoredservice_statuscolor, readme_system_tray_icon, readme_main_window_grid [EXTRACTED 1.00]

## Communities (12 total, 1 thin omitted)

### Community 0 - "README Feature Concepts"
Cohesion: 0.11
Nodes (25): AppSettings, MonitoredService, StatusColor, Optional Admin Credentials, Built-in Administrator Account Workaround, Color-Coded Service Status, %AppData%\ServiceTrayMonitor\config.json, .NET 8 SDK (+17 more)

### Community 1 - "Tray Context & Credentials UI"
Cohesion: 0.12
Nodes (11): ApplicationContext, ContextMenuStrip, Label, TextBox, CredentialsForm, NotifyIcon, SynchronizationContext, Func (+3 more)

### Community 2 - "Service Monitor Engine"
Cohesion: 0.24
Nodes (8): IDisposable, List, message, ServiceController, success, ServiceMonitorEngine, CurrentStatuses, Timer

### Community 3 - "Elevated Runner & Credential Model"
Cohesion: 0.26
Nodes (9): StoredCredential, DisplayLogin, Domain, IsConfigured, Password, Username, message, success (+1 more)

### Community 4 - "Main Service Grid Window"
Cohesion: 0.14
Nodes (11): Button, DataGridView, DataGridViewCellFormattingEventArgs, Form, Func, Label, List, message (+3 more)

### Community 5 - "Namespaces & File Structure"
Cohesion: 0.20
Nodes (6): ServiceTrayMonitor, ServiceTrayMonitor.Models, ServiceTrayMonitor.Services, ServiceTrayMonitor.Forms, Program, STAThread

### Community 6 - "Config Persistence & Settings"
Cohesion: 0.18
Nodes (9): List, AppSettings, CredentialDomain, CredentialEncryptedPasswordBase64, CredentialUsername, MonitoredServiceNames, PollIntervalSeconds, StartWithWindows (+1 more)

### Community 7 - "Service Status Model"
Cohesion: 0.18
Nodes (11): Color, MonitoredService, CanPauseAndContinue, DisplayName, Exists, ServiceName, Status, StatusColor (+3 more)

### Community 8 - "Runtime Icon Rendering"
Cohesion: 0.27
Nodes (7): Bitmap, Dictionary, Icon, IEnumerable, Color, MonitoredService, IconFactory

### Community 9 - "Service Selection Form"
Cohesion: 0.24
Nodes (7): CheckBox, CheckedListBox, List, ServiceController, TextBox, SettingsForm, NumericUpDown

### Community 10 - "Project Build Config"
Cohesion: 0.50
Nodes (4): net8.0-windows, System.ServiceProcess.ServiceController (8.0.0), Microsoft.NET.Sdk, ServiceTrayMonitor

## Ambiguous Edges - Review These
- `Optional Admin Credentials` → `ElevatedServiceRunner`  [AMBIGUOUS]
  README.md · relation: implements

## Knowledge Gaps
- **28 isolated node(s):** `SelectedService`, `ServiceName`, `DisplayName`, `Status`, `CanPauseAndContinue` (+23 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 62 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **1 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `Optional Admin Credentials` and `ElevatedServiceRunner`?**
  _Edge tagged AMBIGUOUS (relation: implements) - confidence is low._
- **Why does `TrayAppContext` connect `Tray Context & Credentials UI` to `Service Monitor Engine`, `Main Service Grid Window`, `Namespaces & File Structure`, `Config Persistence & Settings`, `Service Status Model`, `Service Selection Form`?**
  _High betweenness centrality (0.258) - this node is a cross-community bridge._
- **Why does `ServiceMonitorEngine` connect `Service Monitor Engine` to `Tray Context & Credentials UI`, `Main Service Grid Window`, `Namespaces & File Structure`, `Config Persistence & Settings`, `Service Status Model`?**
  _High betweenness centrality (0.170) - this node is a cross-community bridge._
- **Why does `ManageServicesForm` connect `Main Service Grid Window` to `Tray Context & Credentials UI`, `Service Monitor Engine`, `Namespaces & File Structure`, `Service Status Model`?**
  _High betweenness centrality (0.113) - this node is a cross-community bridge._
- **What connects `SelectedService`, `ServiceName`, `DisplayName` to the rest of the system?**
  _28 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `README Feature Concepts` be split into smaller, more focused modules?**
  _Cohesion score 0.10666666666666667 - nodes in this community are weakly interconnected._
- **Should `Tray Context & Credentials UI` be split into smaller, more focused modules?**
  _Cohesion score 0.12121212121212122 - nodes in this community are weakly interconnected._