## Install

1. Download `MonitorInputWizzard-<version>-win-x64.zip` below and extract it.
2. Move `MonitorInputWizzard.exe` somewhere permanent, such as `%USERPROFILE%\Apps\`. It's self-contained, so nothing else needs installing.
3. Run it. The exe isn't code-signed, so Windows may say "Windows protected your PC". Choose **More info → Run anyway**.
4. **Turn on DDC/CI in your monitor's on-screen menu.** Nothing works without it.
5. Set it up (full details in the [README](https://github.com/ianchesal/MonitorInputWizzard#setting-it-up)):
   - Unless you have an LG UltraGear, **untick NVIDIA I2C**, set **Input VCP** to `0x60`, and pick your monitor.
   - Use **Read current** to find each input's code, bind hotkeys, and click **Save**.
6. Right-click the tray icon (look behind the **^** next to the clock) and turn on **Start with Windows**.

**Upgrading:** choose **Exit** from the tray menu, replace the exe with the new one, and start it again. Your settings in `%AppData%\MonitorInputWizzard` are kept.

**Verify the download** (optional): compare `Get-FileHash <zip>` with `SHA256SUMS.txt`.
