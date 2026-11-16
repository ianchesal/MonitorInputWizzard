using System.Globalization;

namespace MonitorInputWizzard;

/// <summary>Parses the "0x11"-style hex values used in the UI and settings.</summary>
public static class Hex
{
    /// <summary>Accepts "11", "0x11" or "0X11", with surrounding whitespace.</summary>
    public static bool TryParse(string? s, out uint value)
    {
        value = 0;
        if (s is null) return false;
        s = s.Trim();
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s[2..];
        return uint.TryParse(s, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>Parses a single byte, returning <paramref name="fallback"/> if it's not valid hex or doesn't fit.</summary>
    public static byte ParseByte(string? s, byte fallback)
        => TryParse(s, out uint v) && v <= byte.MaxValue ? (byte)v : fallback;
}
