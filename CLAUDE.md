# CLAUDE.md

Windows-only WPF tray app (.NET 10) that switches monitor inputs over DDC/CI. See README.md for user-facing behaviour; this file is the working notes for changing the code.

## Build, run, publish

- `dotnet build -c Release`: quick compile check. There are no tests; verify by running the app.
- `dotnet publish -c Release`: self-contained single-file exe at `bin/Release/net10.0-windows/win-x64/publish/MonitorInputWizzard.exe` (~72 MB).
- Keep builds warning-free (`Nullable` is on).

## Architecture

- `App.xaml.cs`: arg parsing, then one of three modes:
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
- **New CLI flags** must be stripped in `App.OnStartup` before `ParseTarget`, or they're treated as an input name and the app silently exits.
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
- `artifacts/` is gitignored. It holds a hand-assembled share package (`artifacts/MonitorInputWizzard/` with the exe, the user's `settings.json`, `Install.cmd`/`install.ps1`, `Uninstall.cmd`/`uninstall.ps1`, `README.txt`) zipped to `artifacts/MonitorInputWizzard.zip`. After a code change, refresh it by publishing, copying the exe in and re-zipping. The installer sets the same Run key as `Autostart.cs` and removes any legacy Startup-folder shortcut.
- The repo has no LICENSE, so don't publish the binary or zip publicly.
