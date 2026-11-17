# CLAUDE.md

Windows-only WPF tray app (.NET 10) that switches monitor inputs over DDC/CI. See README.md for user-facing behaviour; this file is the working notes for changing the code.

## Build, run, publish

- `dotnet test MonitorInputWizzard.slnx -c Release`: builds the app and runs the xUnit tests in `tests/MonitorInputWizzard.Tests` (Windows only). They cover only hardware-free logic (arg parsing, hex, settings JSON, the DDC/CI packet), so UI and monitor behaviour still needs a manual run of the app.
- `.github/workflows/ci.yml` runs the build and tests on every PR and push to `main`.
- The tests reach `internal` members via `InternalsVisibleTo`. To test something new, pull it out of the WPF/P/Invoke code into a plain static helper (see `Hex.cs`, `CommandLine.cs`, `NvApi.BuildSetVcpPayload`).
- The app csproj excludes `tests/**` from its default globs; keep it that way or the tests get compiled into the exe.
- `dotnet publish -c Release`: self-contained single-file exe at `bin/Release/net10.0-windows/win-x64/publish/MonitorInputWizzard.exe` (~72 MB).
- Keep builds warning-free (`Nullable` is on).
- Name the project explicitly (`dotnet publish MonitorInputWizzard.csproj …`) when passing `-o`, because the folder also has `MonitorInputWizzard.slnx`.
- Releases: `.github/workflows/release.yml` runs on `v*` tag pushes (windows-latest, .NET 10). It publishes with `-p:Version=<tag minus v>`, zips the exe and README, writes `SHA256SUMS.txt`, and runs `gh release create` with `.github/release-notes.md` plus generated notes. Tags with a `-suffix` are pre-releases. `workflow_dispatch` builds only and uploads an artifact. Creating a tag publishes a public release, so only tag when the user asks.

## Architecture

- `App.xaml.cs`: arg parsing (via `CommandLine.cs`), then one of three modes:
  - **Headless**: any argument other than `--tray` is an input name. It switches and exits without creating the window, mutex or tray.
  - **Second instance**: if the named mutex `Local\MonitorInputWizzard` is taken, it signals `Local\MonitorInputWizzard.Show` (unless `--tray`) and exits.
  - **Tray app**: `ShutdownMode.OnExplicitShutdown`. The window is shown unless `--tray`.
- `MainWindow.xaml.cs`: does *all* setup (tray, hotkeys, monitor enumeration) in the constructor, not `Loaded`, because with `--tray` the window is never shown. `OnClosing` cancels and hides unless `ExitApp()` set `_exiting`. `SwitchTo` returns `bool`. `SwitchInBackground` shows a balloon on failure when the window is hidden.
- `TrayIcon.cs`: WinForms `NotifyIcon` + `ContextMenuStrip`, rebuilt on every `Opening`. The icon is loaded from the WPF resource `pack://application:,,,/app.ico`.
- `Autostart.cs`: per-user Run key `HKCU\...\Run\MonitorInputWizzard = "<exe>" --tray`, refreshed on every start.
- `HotkeyManager.cs`: `RegisterHotKey` on the main window's HWND (works while hidden).
- `MonitorController.cs` (dxva2) and `NvApi.cs` (NVAPI raw I2C broadcast) are the two transports.
- `Settings.cs`: JSON in `%AppData%\MonitorInputWizzard\settings.json`. The saved monitor is matched by its full description string, which includes the index and position (`#1 … @(0,0) [primary]`).

## Gotchas

- **WinForms + WPF**: `UseWindowsForms` is on only for the tray. The csproj removes the `System.Windows.Forms` and `System.Drawing` implicit usings because they clash with WPF types (`Application`, `MessageBox`, `KeyEventArgs`…). In WinForms files, import explicitly and alias `System.Drawing` (see `TrayIcon.cs`); fully qualify `System.Windows.Application`.
- **New CLI flags** must be stripped in `App.OnStartup` (like `CommandLine.StripTrayFlag`) before `CommandLine.ParseTarget`, or they're treated as an input name and the app silently exits.
- **Crash logging**: dispatcher and AppDomain unhandled exceptions are appended to `%TEMP%\miw-crash.txt`. A `ThreadPool` callback that throws kills the process, so keep background callbacks trivial (marshal to the dispatcher).
- **The NVAPI default is on** (tuned for LG UltraGear) and broadcasts to every display. Don't press Switch while testing on a non-LG setup with it ticked.
- `.cmd`/`.ps1` files meant for Windows PowerShell 5.1 should be ASCII with CRLF line endings.

## Testing the running app from Claude's shell

- **Launching**: don't start the app with `Start-Process` from the agent shell when it should keep running. It becomes a child of `claude.exe` and dies when the Claude app restarts. Launch detached via WMI:
  `Invoke-CimMethod Win32_Process -MethodName Create -Arguments @{ CommandLine = '"<exe>" --tray' }`
  For short smoke tests, `Start-Process` and then `Stop-Process` is fine.
- **Clean test runs**: the user's installed copy (from `%USERPROFILE%\Apps`) normally holds the hotkeys, so a test build reports "hotkey already in use" and the single-instance check hands off to the installed copy. Stop the installed copy first, or test only things that don't need the hotkeys or the mutex.
- **Checking the tray icon** without clicking: Windows lists registered tray icons under `HKCU\Control Panel\NotifyIconSettings` (`ExecutablePath`).
- **Deploying to the user's machine**: Exit the running copy, copy the published exe to `%USERPROFILE%\Apps\MonitorInputWizzard.exe`, and relaunch via WMI.

## Repo conventions

- `origin` is the user's fork (`ianchesal/MonitorInputWizzard`); upstream is `Lunat1q/MonitorInputWizzard`. The user pushes straight to `main` on the fork. Never open PRs against upstream unless asked. Note that `gh pr create` in a fork defaults to the upstream repo.
- `main` is protected: PRs need the CI `test` check (from GitHub Actions) to pass, and force-pushes and deletion are blocked. Admins are exempt, so direct pushes still work but skip that gate. Dependabot opens weekly grouped PRs for NuGet and Actions. `release.yml` isn't run by CI, so check action bumps with a build-only `gh workflow run release.yml --ref <branch>`.
- `artifacts/` is gitignored. It holds a hand-assembled share package (`artifacts/MonitorInputWizzard/` with the exe, the user's `settings.json`, `Install.cmd`/`install.ps1`, `Uninstall.cmd`/`uninstall.ps1`, `README.txt`) zipped to `artifacts/MonitorInputWizzard.zip`. After a code change, refresh it by publishing, copying the exe in and re-zipping. The installer sets the same Run key as `Autostart.cs` and removes any legacy Startup-folder shortcut.
- Neither this fork nor upstream has a LICENSE. Check with the user before anything that redistributes the binary (releases, public uploads).
