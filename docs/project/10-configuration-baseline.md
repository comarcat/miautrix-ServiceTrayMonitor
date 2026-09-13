# 10 — Configuration Baseline and Release Record

## 1. Baseline v1.0.0 (as found, before any change)

| Item | Value |
|---|---|
| Captured | 2026-09-13, before the first modification |
| Snapshot | `..\miautrix-ServiceTrayMonitor-baseline\ServiceTrayMonitor-v1.0.0-baseline-2026-09-13.zip` (outside the monitored folder, DEC-010) |
| Snapshot size | 2,731,810 bytes |
| Snapshot SHA-256 | `F9F3323983357B11275491A3F2F3086F6CA3D7498B67565971D0F00EDBEFA49F` |
| Contents | All project files, excluding `bin/`, `obj/`, `.vs/` (build output and IDE state) |
| Build status | Debug build: 0 errors, 1 warning (WFAC010) |
| Installed on reference server | SystemTrayMonitor 1.0.0 |

### v1.0.0 file manifest (SHA-256)

| File | Bytes | SHA-256 |
|---|---|---|
| app.manifest | 1467 | 1010F11E9488FB9C4FE193D718A5353DFA6C39C84CAE21DBC9093752AB60890A |
| process_monitor_icon.ico | 96887 | 7FA3DAD29CA3CCD02D03434CD3E57062051A8928E91C6F8B582CC3B47DA3C6E4 |
| process_monitor_icon_preview_128.png | 21917 | 7198C746C4503BABAEED56D0DD38CAB965C479B2A9139DEA682DBD522EDABF2A |
| Program.cs | 308 | 3D0FD5A2D890CF28C62280D3C54EADDAF1D5BA7E627EBD2CC8FF363664717860 |
| README.md | 4719 | DA546F587D319568A2F99703F361CD1CEF7DB8508748BA0E81AF5FD8433F71F4 |
| ServiceTrayMonitor.csproj | 1200 | 70224760857BBF5C63082AE3A37AFD98448241BBF5D89626B8700B01E6075682 |
| ServiceTrayMonitor.csproj.user | 487 | 2953576DE197F8DCF9DC6B346F98B3B899B77D5C792A511BB7792DF9456D0024 |
| ServiceTrayMonitor.sln | 1449 | 40218F8559077745F2AD71E0D5EBE77DF07862BABB337F310B9DD9D8E82AE101 |
| TrayAppContext.cs | 8036 | 511E76B5FF2E25DD97087ED9D4AAD18439EAB6A076FD5EF28F0B15C672B17830 |
| Forms\CredentialsForm.cs | 6172 | 2384BE1EA3FAE7FA6DE68CC5A3A3DEE36FD31FAF8EDD37B4329367B733B951AC |
| Forms\ManageServicesForm.cs | 7168 | DEA58824E35DC89F7307FAA7274F19C8BA139EC3B6DF332BBCDAD2FA0FC0EF38 |
| Forms\SettingsForm.cs | 6408 | 7630BC53106488431285798028A6AF4A75952652D65A01D3399E1E590689907E |
| Models\MonitoredService.cs | 2367 | 8FDC93A7DD949043981E6E6821F6CB91421F3EDDB05C5942F0FEE72CBE478FBB |
| Models\StoredCredential.cs | 801 | C28EAB64F8A31C4D279B9C458EA064DA646D42B5E94B2ADF44670D444B795FC7 |
| Services\ConfigManager.cs | 5733 | D65420960DAA1450CA4FD55B710C301CE0DB56852EE7BF4CF43DE9267126FC6B |
| Services\ElevatedServiceRunner.cs | 6511 | BBA29B96DF6B063F3759251D8614FB4F8E409D042FDB3E5D99EBD0152037F348 |
| Services\IconFactory.cs | 2891 | 1BD6E8EE8651927C49611A680C15368B508AC7DAA11819C50E2D2611360CDA5E |
| Services\ServiceMonitorEngine.cs | 5779 | A8C98409DFF86BA589E023FB8D48EC260B7C5D3F0F6D025C4D1AB979F26B8C89 |
| Setup\SetupSTM\SetupSTM.vdproj | 33877 | 59A179C6AE138F11AD12F9D6405FE67C8A16A5A5DD69BC11736F4421EB57016F |
| Setup\SetupSTM\Debug\setup.exe | 551424 | 4B4D02075DD7D488F82D1F602E81DC2BE0E2AA895DC7532206771FA90BDC93FD |
| Setup\SetupSTM\Debug\SetupSTM.msi | 1353728 | 30A98FF580F36C79E3ADD5488962E915FE5B4DCF37E6BD6DF1CE2040FB51D2D9 |
| Setup\SetupSTM\Debug\SetupSTM.zip | 1293767 | 7515E6F32FD41A14B03E6215765C36FF989DA0FBECF7B17DD9BA6F1E17B3D555 |

## 2. Release 1.0.1 (defect-fix release)

| Item | Value |
|---|---|
| Date | 2026-09-13 |
| Scope | DEF-001…DEF-017 ([doc 05](05-defect-log.md)); no change requests included |
| Assembly version | 1.0.1 (`ServiceTrayMonitor.dll` FileVersion 1.0.1.0) |
| Build | `dotnet build ServiceTrayMonitor.sln -c Release` → 0 errors, 0 warnings (BV-01) |
| Automated tests | 38 passed, 0 failed, 0 skipped (doc 06 §4) |
| UAT | Not started (UAT-01…UAT-12) |
| Approval | Pending sponsor sign-off |

### Change inventory versus v1.0.0

| Change | Files |
|---|---|
| Modified | `app.manifest`, `README.md`, `ServiceTrayMonitor.csproj`, `ServiceTrayMonitor.sln` (test project added), `TrayAppContext.cs`, `Forms\CredentialsForm.cs`, `Forms\ManageServicesForm.cs`, `Forms\SettingsForm.cs`, `Models\MonitoredService.cs`, `Services\ConfigManager.cs`, `Services\ElevatedServiceRunner.cs`, `Services\IconFactory.cs`, `Services\ServiceMonitorEngine.cs`, `Setup\SetupSTM\SetupSTM.vdproj` |
| Added — source | `Models\StatusPalette.cs`, `Models\ServiceSelection.cs`, `Services\ServiceActions.cs`, `Services\StartupManager.cs` |
| Added — tests | `Tests\ServiceTrayMonitor.Tests\` (project + 4 test files) |
| Added — project control | `.gitignore`, `docs\project\` (documents 01–11), `graphify-out\` (knowledge base) |
| Added — build output | `Setup\SetupSTM\Release\SetupSTM.msi`, `Setup\SetupSTM\Release\setup.exe` |
| Unchanged | `Program.cs`, `Models\StoredCredential.cs`, `ServiceTrayMonitor.csproj.user`, icons, `Setup\SetupSTM\Debug\*` (still the 1.0.0 installer) |
| Removed | None |

### 1.0.1 file manifest (SHA-256, source and project files)

| File | Bytes | SHA-256 |
|---|---|---|
| app.manifest | 1233 | 58652422A7BA89C832E9AA41D194BF57E2F9B635648D98F7BFE5D439BBEFCE9D |
| Program.cs | 308 | 3D0FD5A2D890CF28C62280D3C54EADDAF1D5BA7E627EBD2CC8FF363664717860 |
| README.md | 5515 | 20DE9BCCD3E361C1D4EA1C9A14E12C221D74A16EC64F88305FE3D40F248D7F2D |
| ServiceTrayMonitor.csproj | 1482 | EC8F855E23C65419FC1EA472D5B3BBE5391FAC4F65EC1C9C0D5585A7383DFDF4 |
| ServiceTrayMonitor.csproj.user | 487 | 2953576DE197F8DCF9DC6B346F98B3B899B77D5C792A511BB7792DF9456D0024 |
| ServiceTrayMonitor.sln | 3945 | F964E3CA5983FC701AC7B3552FD8314082DC9DCD447E7C0D52F85CCBE543B7D5 |
| TrayAppContext.cs | 10700 | 1CAB58BD37A4196DADDC392BC8F9D00C56C58B9C83E75A0FEB3C4E731352954F |
| Forms\CredentialsForm.cs | 8749 | B4DA98F531ABADCBE03CE519A454EC8010FB9499CF640C5D074BA306C476FCAD |
| Forms\ManageServicesForm.cs | 11326 | 7ADB9153C5F22968DC85473D6FBB2D21BE7E005C0160840E447EA7DDB98C83C2 |
| Forms\SettingsForm.cs | 7210 | D3FF85E84312506E63D5FE2A27E81B63926FE81F126231AF638D62FE36BA8B07 |
| Models\MonitoredService.cs | 1786 | 851C46A7F0083A2B491CEA20B269280939FB92957F28A13450D9CA1B3E9FD35B |
| Models\ServiceSelection.cs | 2308 | 747392C328D33A63B025BE59D904A14EAD62C081971AF093D87F94E814DF74DB |
| Models\StatusPalette.cs | 2399 | 2B47E513A5FD9F4D87705F88D2DECBD6528CF30C26218DD75DB5D09DE4605738 |
| Models\StoredCredential.cs | 801 | C28EAB64F8A31C4D279B9C458EA064DA646D42B5E94B2ADF44670D444B795FC7 |
| Services\ConfigManager.cs | 7884 | F94DE96B5758AC8F2C7945CF96AA54F764F1FBF951F10E4A69E78661C1968555 |
| Services\ElevatedServiceRunner.cs | 5887 | 73D32FBEE0A932DED5CACCB83CCE784556F5E2F94A202A6DD13553140DE527D8 |
| Services\IconFactory.cs | 2901 | D38EC652B0A345BC5F18EF6A653863ECA67615B0FF2AC09A2BA46136D497D555 |
| Services\ServiceActions.cs | 4049 | 092DBEA18D5A3C929A75EFEA8304E847B55578039EF4F7C0E28398CB9A74B976 |
| Services\ServiceMonitorEngine.cs | 5965 | 5D9FE1498C706ACD31E8EF062FEE6F5C71D2589C1B39F57464CC27064B638A5E |
| Services\StartupManager.cs | 6241 | 65FB6B3432B1FEFC89DB8EB0395265EF8AD1AD5F34349AAF7611AADCDAFF2F0C |
| Setup\SetupSTM\SetupSTM.vdproj | 33590 | 80B6DA1D990D67E6DC72BB2F8980E09B6C70D3D1D5F8E32C4F8E82D44D9262D9 |
| Tests\ServiceTrayMonitor.Tests\ServiceTrayMonitor.Tests.csproj | 842 | 45F4F42F78C8DD4E24369A1CCC40FE77A8C5956CA43CEC19691B5F96C5083D32 |
| Tests\ServiceTrayMonitor.Tests\ConfigManagerTests.cs | 4148 | D29E71E7B4797EBC762E52BD84F38F9138AC1B359954A945B96F08B6799D85D7 |
| Tests\ServiceTrayMonitor.Tests\ServiceSelectionTests.cs | 2004 | 07C2F684138D6BF70D88B74E678F046220C379AE06E1DEF15B10B677BAE4FBEA |
| Tests\ServiceTrayMonitor.Tests\ServicesTests.cs | 5189 | E9BC146400A5602ACC23C23199EB9C6BF090AE3E2E8ED3DB9E808D106726A0CC |
| Tests\ServiceTrayMonitor.Tests\StatusAndRulesTests.cs | 4342 | AD5E6558078406D1A9BAE1B536F82C32B3CA1F13F7042D4E8319484A146BA660 |

### 1.0.1 installer artefacts

Built 2026-09-13 11:31 with Visual Studio 18 (`devenv ServiceTrayMonitor.sln /Build "Release|Any CPU" /Project SetupSTM /ProjectConfig Release`).

| File | Bytes | SHA-256 |
|---|---|---|
| Setup\SetupSTM\Release\SetupSTM.msi | 1365504 | A43FCF32E7817D078954697705EC6E849D445EB18212BDA0B172C03E41182757 |
| Setup\SetupSTM\Release\setup.exe | 683520 | 919E5DC017898F58B879886B1D344A1662D0728558CC188543B715D6B2C3AA2B |

MSI properties, read from the built package:

| Property | Value | Check |
|---|---|---|
| ProductName | SystemTrayMonitor | Unchanged (CR-002) |
| ProductVersion | 1.0.1 | ✔ |
| ProductCode | {6E2B7C41-3F9D-4A8E-B5C2-9D17E4A30F58} | ✔ new |
| UpgradeCode | {1B549785-373D-417D-A58F-C1CE513727F4} | ✔ same as 1.0.0 |
| Manufacturer | Miautrix | ✔ |
| Upgrade table | `PREVIOUSVERSIONSINSTALLED` for versions < 1.0.1 (removes 1.0.0); `NEWERPRODUCTFOUND` for ≥ 1.0.1 (detect only) | ✔ in-place upgrade |
| Bootstrapper prerequisites | .NET 8 Desktop Runtime (x64) only | ✔ .NET Framework 4.7.2 removed from project; no "v4.7.2" string in `setup.exe` |

Packaged files: `ServiceTrayMonitor.exe`, `ServiceTrayMonitor.dll`, `.pdb`, `.deps.json`, `.runtimeconfig.json`,
`System.ServiceProcess.ServiceController.dll`, icons, `README.md`.

## 3. Placing the baseline and release in GitHub (client action — WBS 1.3.2)

The folder `miautrix-ServiceTrayMonitor` is not yet a repository and must not be renamed or moved (DEC-001).

1. In the GitHub app, add `miautrix-ServiceTrayMonitor` as a repository. `.gitignore` already excludes
   `bin/`, `obj/`, `.vs/`, and the graphify cache.
2. Commit the current state as **"Release 1.0.1 — defect fixes DEF-001…DEF-017"**.
3. Create a GitHub Release **v1.0.0-baseline** and attach the baseline zip (§1) with its SHA-256 in the
   description. The as-found state is then preserved and auditable without rewriting working files.
4. After UAT sign-off, create GitHub Release **v1.0.1** and attach `SetupSTM.msi` and `setup.exe` (§2).
5. Decide whether installer binaries under `Setup\SetupSTM\Debug` and `\Release` stay committed or are
   only published as release assets (recommended: release assets only; then add both folders to `.gitignore`).

## 4. Release notes — 1.0.1 (draft)

**Fixed**
- Saving monitored services no longer erases the saved admin credential.
- Filtering the service list no longer loses checks or drops services hidden by the filter.
- "Start automatically when Windows starts" now works: it uses a scheduled task with administrator
  rights and removes the old, non-working registry entry.
- The credential **Test** button no longer hangs; it reports whether the account gets a full
  administrator token.
- With a saved credential, actions no longer fail with "Access is denied". The app uses its own rights
  first and the saved account only when Windows refuses.
- The UI stays responsive during start/stop/pause/resume; results are reported when the service
  actually reaches the new state.
- The main window keeps sort order, scroll position, and selection while statuses refresh.
- A missing service shows a gray dot, as documented.
- A damaged settings file is kept as a backup instead of being silently replaced.
- Re-saving the credential without retyping the password keeps the saved password.
- Installer upgrades 1.0.0 in place and no longer asks for .NET Framework 4.7.2.
- Stability: fixed thread-marshalling and resource-leak issues.

**Known limitations:** no Restart action (CR-001); installed product name "SystemTrayMonitor" (CR-002); unsigned binaries (CR-003).
