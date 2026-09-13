# Service Tray Monitor

A lightweight Windows system-tray app that monitors a set of Windows Services
you choose, shows their status with color coding, and lets you start, stop,
pause, and resume them — all from the tray icon's right-click menu.

## Features

- Lives in the system tray (no taskbar window on launch)
- Right-click menu lists every monitored service with a colored status dot
  (🟢 running, 🔴 stopped, 🟡 paused/pending, ⚪ not found)
- Per-service submenu: Start / Stop / Pause / Resume
- "Manage Monitored Services" window with a filterable checklist of every
  service installed on the machine
- Main window: sortable grid with color-coded status column and action buttons
- Configurable poll interval
- Optional "start with Windows" toggle — registers a per-user Task Scheduler task
  (`ServiceTrayMonitor (<user>)`) that starts the app at logon with admin rights
- Optional admin credential (Domain/Username/Password). Service actions run with the
  app's own admin rights first; if Windows denies access, the action is retried as the
  saved account. Stored inside the same `config.json`, with the password DPAPI-encrypted —
  never written to disk in plain text, and only decryptable by the same Windows user account.
- Settings persisted to `%AppData%\ServiceTrayMonitor\config.json`

## Requirements

- Windows 10 or 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or just the runtime if you're only running a published build)
- **Administrator rights** — starting, stopping, and pausing services requires
  elevation. The app's manifest is set to `requireAdministrator`, so Windows
  will show a UAC prompt on launch.

## Building & running

1. Unzip the project folder somewhere, e.g. `C:\Dev\ServiceTrayMonitor`.
2. Open a terminal in that folder.
3. Restore & run directly:
   ```
   dotnet run
   ```
   Or open `ServiceTrayMonitor.csproj` in Visual Studio 2022+ and press F5.

4. To build a standalone `.exe` you can pin/run without the SDK installed:
   ```
   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
   ```
   The `.exe` will be in `bin\Release\net8.0-windows\win-x64\publish\`.

## First run

1. Launch the app (approve the UAC prompt — it needs admin rights to control services).
2. Right-click the new tray icon → **Manage Monitored Services...**
3. Check the services you want to watch (use the filter box to search), set your
   preferred poll interval, and click **Save**.
4. Right-click the tray icon again — your chosen services now appear with
   live color-coded status and Start/Stop/Pause/Resume options.
5. Double-click the tray icon (or choose **Open Service Monitor**) any time
   for the full grid view.
6. If Windows refuses an action to your own admin rights (for example, a service
   whose permissions only allow a specific account), right-click the tray icon →
   **Admin Credentials...** and enter a Windows account (Domain optional, blank =
   local account) that has permission to control services. Use **Test** to confirm
   it works before saving. From then on, any action Windows denies to the app is
   retried under that account. To change the domain or username later, re-enter the
   password; leaving it blank with the same login keeps the saved password.

## Running the tests

```
dotnet test Tests/ServiceTrayMonitor.Tests
```

The tests use a temporary config folder and only *read* service status — they never
start, stop, or reconfigure a service.

## Troubleshooting: "Access is denied" with a saved credential

The saved account is signed in with a *batch* logon, which Windows doesn't filter
through UAC, so an Administrators-group account keeps its full admin token. **Test**
tells you which token it got:

- *"Credential works … full administrator token"* — the account can control services.
- *"Signed in … but without a full administrator token"* — the account isn't an admin,
  or it lacks the **Log on as a batch job** right (then an interactive logon is used,
  which UAC filters). Either grant that right (Local Security Policy → User Rights
  Assignment), use the built-in "Administrator" account, or grant the account explicit
  rights on just the services it needs, as below.

**Grant your account explicit rights on just that service**, so it never needs
to be an admin at all (recommended — least privilege):

```powershell
# 1. Get the account's SID
wmic useraccount where name='YourServiceAccount' get sid

# 2. See the service's current permissions
sc sdshow <serviceName>

# 3. Append an ACE granting that SID Start/Stop/Pause/Query rights — keep the
#    existing ACEs from step 2 and add the new one before the trailing S: part
sc sdset <serviceName> D:(A;;CCLCSWRPWPDTLOCRRC;;;SY)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)(A;;CCLCSWLOCRRC;;;IU)(A;;CCLCSWLOCRRC;;;SU)(A;;RPWPDTLOCRRC;;;<SID-FROM-STEP-1>)S:(AU;FA;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;WD)
```

Repeat step 3 for each service. Run all of this from an elevated prompt, once.

## Notes on customization

- Colors are defined in `Models/StatusPalette.cs` — tweak the RGB values there
  if you want a different palette; menu dots, grid, and tray icon all follow it.
- Poll interval and monitored service list live in `ConfigManager`/`AppSettings`;
  the config file is plain JSON if you ever want to edit it by hand.
- Icons are drawn at runtime (`Services/IconFactory.cs`) — no external image
  files needed, so there's nothing to go missing when you move the app.
