using System.IO;
using System.Windows.Input;

namespace MonitorInputWizzard.Tests;

// Uses a temp directory per test; never touches the real %AppData% settings.
public sealed class SettingsTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "miw-tests-" + Guid.NewGuid().ToString("N"));
    private string SettingsPath => Path.Combine(_dir, "nested", "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Missing_file_loads_lg_defaults()
    {
        var s = Settings.Load(SettingsPath);

        Assert.Equal(0xF4, s.InputRegister);
        Assert.True(s.UseNvApi);
        Assert.Equal(0x50, s.SourceAddr);
        Assert.Equal(
            new[] { "DisplayPort 1", "DisplayPort 2", "HDMI 1", "HDMI 2", "USB-C" },
            s.Inputs.Select(i => i.Name));
    }

    [Fact]
    public void Corrupt_file_loads_defaults()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, "{ this is not json");

        var s = Settings.Load(SettingsPath);

        Assert.Equal(Settings.Defaults().Inputs.Count, s.Inputs.Count);
        Assert.True(s.UseNvApi);
    }

    [Fact]
    public void Json_null_loads_defaults()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, "null");

        Assert.Equal(0xF4, Settings.Load(SettingsPath).InputRegister);
    }

    [Fact]
    public void Save_creates_directory_and_round_trips()
    {
        var original = new Settings
        {
            MonitorDescription = "#1 LG ULTRAGEAR — 2560×1440 @(0,0) [primary]",
            InputRegister = 0x60,
            UseNvApi = false,
            SourceAddr = 0x51,
            Inputs =
            {
                new() { Name = "Work", Code = 0x0F, Modifiers = ModifierKeys.Control | ModifierKeys.Alt, Key = Key.D1 },
                new() { Name = "Game", Code = 0x11 },
            },
        };

        original.Save(SettingsPath);
        var loaded = Settings.Load(SettingsPath);

        Assert.Equal(original.MonitorDescription, loaded.MonitorDescription);
        Assert.Equal(0x60, loaded.InputRegister);
        Assert.False(loaded.UseNvApi);
        Assert.Equal(0x51, loaded.SourceAddr);
        Assert.Equal(2, loaded.Inputs.Count);
        Assert.Equal("Work", loaded.Inputs[0].Name);
        Assert.Equal(0x0Fu, loaded.Inputs[0].Code);
        Assert.Equal(ModifierKeys.Control | ModifierKeys.Alt, loaded.Inputs[0].Modifiers);
        Assert.Equal(Key.D1, loaded.Inputs[0].Key);
        Assert.Equal(Key.None, loaded.Inputs[1].Key);
    }

    [Fact]
    public void Saved_json_omits_ui_only_properties()
    {
        new Settings { Inputs = { new() { Name = "HDMI 1", Code = 0x90 } } }.Save(SettingsPath);
        var json = File.ReadAllText(SettingsPath);

        Assert.DoesNotContain(nameof(InputPreset.CodeHex), json);
        Assert.DoesNotContain(nameof(InputPreset.HotkeyText), json);
        Assert.DoesNotContain(nameof(InputPreset.HasHotkey), json);
    }

    [Fact]
    public void Missing_fields_use_property_defaults()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, """{ "Inputs": [ { "Name": "HDMI 1", "Code": 144 } ] }""");

        var s = Settings.Load(SettingsPath);

        Assert.Equal(0x60, s.InputRegister);
        Assert.False(s.UseNvApi);
        Assert.Equal(0x50, s.SourceAddr);
        Assert.Equal(0x90u, Assert.Single(s.Inputs).Code);
    }
}
