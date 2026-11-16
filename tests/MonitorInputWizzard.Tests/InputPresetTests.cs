using System.Windows.Input;

namespace MonitorInputWizzard.Tests;

public class InputPresetTests
{
    [Fact]
    public void CodeHex_formats_with_prefix_and_two_digits()
    {
        Assert.Equal("0x0F", new InputPreset { Code = 0x0F }.CodeHex);
        Assert.Equal("0x1234", new InputPreset { Code = 0x1234 }.CodeHex);
    }

    [Fact]
    public void CodeHex_setter_parses_hex()
    {
        var p = new InputPreset { CodeHex = "0x11" };
        Assert.Equal(0x11u, p.Code);

        p.CodeHex = "d0";
        Assert.Equal(0xD0u, p.Code);
    }

    [Fact]
    public void CodeHex_setter_ignores_invalid_text()
    {
        var p = new InputPreset { Code = 0x11 };
        p.CodeHex = "not hex";
        Assert.Equal(0x11u, p.Code);
    }

    [Fact]
    public void HotkeyText_without_key_is_none()
    {
        var p = new InputPreset();
        Assert.False(p.HasHotkey);
        Assert.Equal("(none)", p.HotkeyText);
    }

    [Fact]
    public void HotkeyText_without_modifiers_is_just_the_key()
    {
        var p = new InputPreset { Key = Key.F9 };
        Assert.True(p.HasHotkey);
        Assert.Equal("F9", p.HotkeyText);
    }

    [Fact]
    public void HotkeyText_joins_modifiers_with_plus()
    {
        var p = new InputPreset { Modifiers = ModifierKeys.Control | ModifierKeys.Alt, Key = Key.D1 };
        Assert.Equal("Alt+Control+D1", p.HotkeyText);
    }

    [Fact]
    public void Changing_code_notifies_code_and_codehex()
    {
        var p = new InputPreset();
        var changed = new List<string?>();
        p.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        p.Code = 0x90;

        Assert.Equal(new[] { nameof(InputPreset.Code), nameof(InputPreset.CodeHex) }, changed);
    }

    [Fact]
    public void Changing_key_notifies_hotkey_properties()
    {
        var p = new InputPreset();
        var changed = new List<string?>();
        p.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        p.Key = Key.F1;

        Assert.Equal(
            new[] { nameof(InputPreset.Key), nameof(InputPreset.HotkeyText), nameof(InputPreset.HasHotkey) },
            changed);
    }

    [Fact]
    public void Setting_same_value_does_not_notify()
    {
        var p = new InputPreset { Name = "HDMI 1" };
        bool fired = false;
        p.PropertyChanged += (_, _) => fired = true;

        p.Name = "HDMI 1";

        Assert.False(fired);
    }
}
