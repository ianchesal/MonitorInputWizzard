namespace MonitorInputWizzard.Tests;

public class HexTests
{
    [Theory]
    [InlineData("11", 0x11u)]
    [InlineData("0x11", 0x11u)]
    [InlineData("0X11", 0x11u)]
    [InlineData("  0xF4  ", 0xF4u)]
    [InlineData("d0", 0xD0u)]
    [InlineData("0x1234", 0x1234u)]
    public void TryParse_accepts_hex_with_or_without_prefix(string input, uint expected)
    {
        Assert.True(Hex.TryParse(input, out uint value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0x")]
    [InlineData("zz")]
    [InlineData("-1")]
    [InlineData("0x0x11")]
    public void TryParse_rejects_invalid_input(string? input)
    {
        Assert.False(Hex.TryParse(input, out _));
    }

    [Theory]
    [InlineData("0x60", 0x60)]
    [InlineData("ff", 0xFF)]
    [InlineData("0x00", 0x00)]
    public void ParseByte_parses_valid_bytes(string input, byte expected)
    {
        Assert.Equal(expected, Hex.ParseByte(input, fallback: 0xAA));
    }

    [Theory]
    [InlineData("0x100")] // doesn't fit in a byte
    [InlineData("nope")]
    [InlineData("")]
    [InlineData(null)]
    public void ParseByte_returns_fallback_for_bad_input(string? input)
    {
        Assert.Equal(0xAA, Hex.ParseByte(input, fallback: 0xAA));
    }
}
