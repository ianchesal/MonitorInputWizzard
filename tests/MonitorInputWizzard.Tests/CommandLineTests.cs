namespace MonitorInputWizzard.Tests;

public class CommandLineTests
{
    [Fact]
    public void StripTrayFlag_removes_flag_in_any_case_and_position()
    {
        var rest = CommandLine.StripTrayFlag(new[] { "HDMI", "--TRAY", "1" }, out bool tray);

        Assert.True(tray);
        Assert.Equal(new[] { "HDMI", "1" }, rest);
    }

    [Fact]
    public void StripTrayFlag_without_flag_leaves_args_alone()
    {
        var rest = CommandLine.StripTrayFlag(new[] { "HDMI1" }, out bool tray);

        Assert.False(tray);
        Assert.Equal(new[] { "HDMI1" }, rest);
    }

    [Fact]
    public void Tray_flag_alone_is_not_a_switch_request()
    {
        var rest = CommandLine.StripTrayFlag(new[] { "--tray" }, out _);

        Assert.Null(CommandLine.ParseTarget(rest));
    }

    [Theory]
    [InlineData(new string[0], null)]
    [InlineData(new[] { "HDMI1" }, "HDMI1")]
    [InlineData(new[] { "HDMI", "1" }, "HDMI 1")]
    [InlineData(new[] { "--switch", "HDMI1" }, "HDMI1")]
    [InlineData(new[] { "-s", "DisplayPort", "2" }, "DisplayPort 2")]
    [InlineData(new[] { "--switch" }, null)]
    [InlineData(new[] { "  " }, null)]
    public void ParseTarget(string[] args, string? expected)
    {
        Assert.Equal(expected, CommandLine.ParseTarget(args));
    }

    private static readonly InputPreset[] Presets =
    {
        new() { Name = "DisplayPort 1", Code = 0xD0 },
        new() { Name = "HDMI 1", Code = 0x90 },
        new() { Name = "USB-C", Code = 0xD2 },
    };

    [Theory]
    [InlineData("HDMI 1", 0x90u)]
    [InlineData("hdmi1", 0x90u)]
    [InlineData("HDMI1", 0x90u)]
    [InlineData("displayport 1", 0xD0u)]
    [InlineData("usb-c", 0xD2u)]
    public void FindPreset_ignores_case_and_spaces(string name, uint expectedCode)
    {
        Assert.Equal(expectedCode, CommandLine.FindPreset(Presets, name)?.Code);
    }

    [Fact]
    public void FindPreset_returns_null_for_unknown_name()
    {
        Assert.Null(CommandLine.FindPreset(Presets, "HDMI 3"));
    }
}
