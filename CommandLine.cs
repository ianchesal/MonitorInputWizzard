namespace MonitorInputWizzard;

/// <summary>Command-line parsing, kept free of WPF so it can be tested.</summary>
public static class CommandLine
{
    public const string TrayFlag = "--tray";

    /// <summary>Removes <c>--tray</c> (any case) and reports whether it was present.</summary>
    public static string[] StripTrayFlag(string[] args, out bool startInTray)
    {
        startInTray = args.Contains(TrayFlag, StringComparer.OrdinalIgnoreCase);
        return args.Where(a => !a.Equals(TrayFlag, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    /// <summary>Accepts "HDMI1", "HDMI 1", or "--switch HDMI1". Returns null if no switch was requested.</summary>
    public static string? ParseTarget(string[] args)
    {
        if (args.Length == 0) return null;
        var parts = args[0] is "--switch" or "-s" ? args.Skip(1) : args;
        var name = string.Join(" ", parts).Trim();
        return name.Length == 0 ? null : name;
    }

    /// <summary>Finds a preset by name, ignoring case and spaces ("hdmi1" matches "HDMI 1").</summary>
    public static InputPreset? FindPreset(IEnumerable<InputPreset> inputs, string name)
        => inputs.FirstOrDefault(i => Norm(i.Name) == Norm(name));

    private static string Norm(string s) => s.Replace(" ", "").ToLowerInvariant();
}
