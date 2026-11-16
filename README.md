# MonitorInputWizzard

A small Windows system-tray utility that switches your monitor's **input source** (DisplayPort, HDMI, USB-C/Thunderbolt, …) with a global hotkey, a tray menu, or the command line, instead of digging through the monitor's on-screen menu with the joystick nub on the back. It makes a handy software KVM: one hotkey hands the monitor to the laptop on the other input.

It talks to the display over **DDC/CI**, the side-channel that rides along the video cable. Two transports are supported:

| Transport | How | Use it when |
|---|---|---|
| **dxva2** (Windows `SetVCPFeature`) | Standard MCCS write to VCP register `0x60` on the monitor you pick | Your monitor follows the spec (most do, including Dell UltraSharps) |
| **NVAPI raw I2C** | Writes the DDC/CI packet directly on the I2C bus of an NVIDIA GPU | Your monitor ignores standard writes (notably LG UltraGear — see below) |

## Requirements

- Windows 10/11 (x64)
- .NET 10 SDK to build (the published binary is self-contained — no runtime install needed)
- **DDC/CI must be enabled in the monitor's OSD.** Many monitors ship with it off, and nothing will work until you turn it on.
- NVIDIA GPU + driver, *only* if you use the NVAPI transport (`nvapi64.dll` comes with the driver)

## Build & install

```
git clone https://github.com/ianchesal/MonitorInputWizzard.git
cd MonitorInputWizzard
dotnet run
```

(This is a fork of [Lunat1q/MonitorInputWizzard](https://github.com/Lunat1q/MonitorInputWizzard) that adds the tray app and autostart.)

Single-file release build:

```
dotnet publish -c Release
```

The `.exe` lands in `bin/Release/net10.0-windows/win-x64/publish/`. It's self-contained (~72 MB), so to install it just copy it somewhere permanent, such as `%USERPROFILE%\Apps\`. Don't run it from `bin\`, which rebuilds and `dotnet clean` wipe. Then turn on **Start with Windows** from the tray menu. To upgrade, choose **Exit** from the tray menu, copy the new `.exe` over the old one, and start it again.

The first time you run an unsigned build on another PC, Windows SmartScreen may say "Windows protected your PC". Choose **More info → Run anyway**.

## Setting it up

The window is a list of **input presets**. Each preset is a name, a VCP code, and an optional global hotkey.

1. **Pick your transport.** For a standard monitor, **untick NVIDIA I2C** (this enables the monitor picker) and choose your monitor in the dropdown. Leave it ticked only for an LG UltraGear. It's on by default for the LG, and in that mode every write goes to *every* connected display, so untick it first on anything else.
2. **Set the input register.** `0x60` is the MCCS standard. `0xF4` is LG UltraGear.
3. **Find your input codes.** Switch the monitor to an input with its own buttons, then click **Read current**. It shows the register's current value, which is that input's code. Repeat for each input, typing each value into the preset's *VCP code* box. Codes vary by manufacturer, so don't assume the MCCS list below applies.
4. **Bind a hotkey.** Click a preset's hotkey button, press the combo. `Esc` clears it. Hotkeys are system-wide (registered via `RegisterHotKey`, with auto-repeat suppressed) and fire even when the window is hidden.
5. **Save.** Settings persist to `%AppData%\MonitorInputWizzard\settings.json`. Hotkeys, the tray menu and the command line only target the monitor that's *saved*, so click **Save** after choosing it.

**Test DDC** dims the selected monitor's brightness for ~1 second and restores it. If the screen visibly dims, DDC/CI writes are reaching the panel — so any input-switch failure is a wrong register or a wrong code, not a broken connection. (dxva2 mode only.)

### Reading a code from the other computer

When you switch to an input that belongs to another computer (say a MacBook on Thunderbolt), you can't see the PC's window to click **Read current**. Read the code from that computer instead. On a Mac, BetterDisplay's DDC input-source control shows it, or use [`m1ddc`](https://github.com/waydabber/m1ddc) on Apple Silicon:

```
brew install m1ddc
m1ddc get input        # prints decimal, e.g. 25 = 0x19
```

As long as the PC's own cable stays connected, it can still send commands to the monitor while another input is on screen. That's how the hotkey switches *to* the other computer. Switching *back* has to come from the other computer (for example a BetterDisplay shortcut that sets the PC's input code) or the monitor's buttons, because the PC's keyboard and screen aren't in front of you any more.

## The tray icon

The app lives in the notification area (the icons by the clock). Windows 11 puts new icons in the **^** overflow at first. Drag it onto the taskbar, or turn it on under *Settings → Personalization → Taskbar → Other system tray icons*.

- **Left-click** opens the window.
- **Right-click** gives a menu with a **Switch to …** entry for each preset (the last one used is ticked), **Start with Windows**, and **Exit**.
- **Closing the window** hides it to the tray; hotkeys keep working. Only **Exit** in the tray menu quits.
- **Start with Windows** adds a per-user `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` entry that launches the app with `--tray`, so it starts hidden at login. It's re-pointed at the current `.exe` on every start, so moving the file is fine. It also shows up in Task Manager → Startup apps.
- Only one copy runs at a time. Launching it again just brings up the existing window.
- If a hotkey or tray switch fails while the window is hidden, a notification says why.

## Known-good settings

| Monitor | Transport | Register | Codes |
|---|---|---|---|
| LG UltraGear 27GN800 | NVAPI, source `0x50` | `0xF4` | DisplayPort 1 `0xD0`, DisplayPort 2 `0xD1`, HDMI 1 `0x90`, HDMI 2 `0x91`, USB-C `0xD2` (these are the app's defaults) |
| Dell U4025QW | dxva2 | `0x60` | DisplayPort 1 `0x0F`, Thunderbolt `0x19` |

For a standard monitor on register `0x60`, the usual MCCS codes are `0x0F` (DisplayPort 1), `0x10` (DisplayPort 2), `0x11` (HDMI 1), `0x12` (HDMI 2). USB-C/Thunderbolt has no standard code; it varies by manufacturer and model (`0x19` on the U4025QW), so read it rather than guessing. Sending a code the monitor doesn't know is harmless: it ignores it.

## Why the NVAPI path exists

Most monitors accept a plain `SetVCPFeature(0x60, <input code>)` and switch. Some don't. LG UltraGear panels (confirmed on the **27GN800**) silently ignore VCP `0x60` entirely. Their input select lives on a **non-standard register `0xF4`**, reachable only through the **`0x50` service side-channel** — an I2C source address that the Windows dxva2 API will not emit.

So `NvApi.cs` builds the DDC/CI packet by hand (length byte, Set-VCP opcode, register, hi/lo value, XOR checksum) and pushes it through `NvAPI_I2CWrite` to the DDC/CI address (`0x6E`) of every connected display on every NVIDIA GPU. That's the monitor's control address, not its EDID, so nothing is written to the monitor's identity data. The LG acts on `0xF4`; other monitors ignore a VCP code they don't know, though `0xE0`–`0xFF` is a manufacturer-specific range, so another brand *could* map it to something else.

## Command line

Passing an input name switches to it and exits immediately — no window, no tray icon:

```
MonitorInputWizzard.exe "HDMI 1"
MonitorInputWizzard.exe HDMI1          # spaces and case are ignored
MonitorInputWizzard.exe --switch "USB-C"
```

It reuses the saved settings, so configure once in the GUI, then wire the `.exe` into a Stream Deck button, an AutoHotkey script, or a shortcut. If the name matches no preset, or the saved monitor isn't found, it exits silently without switching.

`MonitorInputWizzard.exe --tray` starts the tray app hidden, without opening the window. Any other argument is treated as an input name.

## Troubleshooting

**Nothing happens.** Check DDC/CI is on in the monitor's OSD. Then hit **Test DDC** — if brightness doesn't move, DDC/CI isn't getting through at all (bad/adapter cable, DDC/CI off, or a KVM/dock in the path eating the channel).

**Test DDC dims fine, but the input won't switch.** Wrong register or wrong code. Use **Read current** on each input to harvest the real codes. If `0x60` reads back as unsupported, try `0xF4` with the NVAPI box ticked.

**The window works but hotkeys / the tray menu / the command line don't.** They use the *saved* monitor. Pick it in the dropdown and click **Save**. The saved name includes the monitor's number and desktop position (e.g. `#1 Dell U4025QW (DP HDR) — 5120×2160 @(0,0) [primary]`), so after rearranging displays, re-select it and save again.

**"No DDC/CI monitors found."** Windows sees no monitor exposing a physical DDC/CI handle. Common with DisplayLink/USB display adapters and some laptop internal panels — those simply can't be driven this way.

**Hotkey says it's already in use.** Another app grabbed that combo first, or another copy of this app is running (for example a pre-tray build that isn't covered by the single-instance check). Pick a different combo or close the other copy.

**Can't find the tray icon.** Look in the **^** overflow next to the clock.

**The app disappeared.** Check `%TEMP%\miw-crash.txt`. Unhandled exceptions, on the UI thread and on background threads, are appended there with a timestamp. No entry usually means something else ended the process; it only comes back by itself at your next login (via Start with Windows).

## Layout

| File | What it does |
|---|---|
| [App.xaml.cs](App.xaml.cs) | Startup: headless command-line switch, `--tray`, single-instance handoff, crash logging |
| [MainWindow.xaml.cs](MainWindow.xaml.cs) | The UI: preset list, hotkey capture, Read/Test diagnostics, hide-to-tray |
| [TrayIcon.cs](TrayIcon.cs) | Notification-area icon and its right-click menu |
| [Autostart.cs](Autostart.cs) | "Start with Windows" via the per-user Run registry key |
| [MonitorController.cs](MonitorController.cs) | Enumerates physical monitors, reads/writes VCP features via `dxva2.dll` |
| [NvApi.cs](NvApi.cs) | Hand-rolled DDC/CI packets over NVAPI raw I2C, for monitors dxva2 can't reach |
| [HotkeyManager.cs](HotkeyManager.cs) | System-wide hotkeys via `RegisterHotKey` + a WndProc hook |
| [Settings.cs](Settings.cs) | Presets and transport config, persisted as JSON in `%AppData%` |
| [app.ico](app.ico) | App and tray icon |

## Known limits

- The NVAPI path **broadcasts** the write to every connected display on every NVIDIA GPU rather than targeting one. If that ever bites, per-display targeting by EDID match is the fix.
- NVAPI transport is NVIDIA-only. AMD/Intel users on a monitor that needs the side-channel are out of luck for now.
- The exe isn't code-signed, so SmartScreen warns on first run on a new PC.
