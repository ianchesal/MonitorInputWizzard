namespace MonitorInputWizzard.Tests;

// Only the command string is tested; Set/IsEnabled would write to the real HKCU Run key.
public class AutostartTests
{
    [Fact]
    public void Command_quotes_path_and_starts_in_tray()
    {
        Assert.Equal(
            "\"C:\\Users\\me\\Apps\\MonitorInputWizzard.exe\" --tray",
            Autostart.CommandFor(@"C:\Users\me\Apps\MonitorInputWizzard.exe"));
    }

    [Fact]
    public void Command_keeps_paths_with_spaces_in_one_argument()
    {
        Assert.Equal(
            "\"C:\\Program Files\\MIW\\MonitorInputWizzard.exe\" --tray",
            Autostart.CommandFor(@"C:\Program Files\MIW\MonitorInputWizzard.exe"));
    }
}
