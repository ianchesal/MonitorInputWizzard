using System.Windows.Forms;
using Drawing = System.Drawing;

namespace MonitorInputWizzard;

/// <summary>Notification-area icon. Left-click opens the window; right-click switches inputs, toggles autostart, or exits.</summary>
public sealed class TrayIcon : IDisposable
{
    private const string AppName = "Monitor Input Wizzard";

    private readonly NotifyIcon _icon;
    private readonly ContextMenuStrip _menu = new();
    private readonly Drawing.Font _bold;
    private readonly Func<IEnumerable<InputPreset>> _inputs;
    private readonly Action<InputPreset> _switch;
    private readonly Action _open;
    private readonly Action _exit;
    private string? _last;

    public TrayIcon(Func<IEnumerable<InputPreset>> inputs, Action<InputPreset> switchTo, Action open, Action exit)
    {
        _inputs = inputs;
        _switch = switchTo;
        _open = open;
        _exit = exit;
        _bold = new Drawing.Font(_menu.Font, Drawing.FontStyle.Bold);

        // Rebuilt on every open so it always reflects the current presets and hotkeys.
        // An empty strip opens with Cancel already set, so clear it once items are in.
        _menu.Opening += (_, e) => { Rebuild(); e.Cancel = false; };
        Rebuild();

        _icon = new NotifyIcon { Icon = LoadIcon(), Text = AppName, ContextMenuStrip = _menu, Visible = true };
        _icon.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) _open(); };
    }

    /// <summary>Records the last input switched to: ticked in the menu and shown in the hover tooltip.</summary>
    public void SetLast(string name)
    {
        _last = name;
        var text = $"{AppName}\nLast: {name}";
        _icon.Text = text.Length > 127 ? text[..127] : text;
    }

    public void ShowMessage(string text, bool warning = false)
        => _icon.ShowBalloonTip(4000, AppName, text, warning ? ToolTipIcon.Warning : ToolTipIcon.Info);

    private void Rebuild()
    {
        _menu.Items.Clear();

        var inputs = _inputs().ToList();
        if (inputs.Count == 0)
            _menu.Items.Add(new ToolStripMenuItem("No inputs set up yet") { Enabled = false });
        foreach (var input in inputs)
        {
            var target = input;
            _menu.Items.Add(new ToolStripMenuItem("Switch to " + input.Name, null, (_, _) => _switch(target))
            {
                ShortcutKeyDisplayString = input.HasHotkey ? input.HotkeyText : null,
                Checked = input.Name == _last,
            });
        }

        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(new ToolStripMenuItem("Open " + AppName, null, (_, _) => _open()) { Font = _bold });

        var autostart = new ToolStripMenuItem("Start with Windows") { Checked = Autostart.IsEnabled, CheckOnClick = true };
        autostart.Click += (_, _) => Autostart.Set(autostart.Checked);
        _menu.Items.Add(autostart);

        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => _exit()));
    }

    private static Drawing.Icon LoadIcon()
    {
        using var stream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/app.ico")).Stream;
        return new Drawing.Icon(stream, SystemInformation.SmallIconSize);
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        _menu.Dispose();
        _bold.Dispose();
    }
}
