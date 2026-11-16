using System.IO;
using System.Windows;

namespace MonitorInputWizzard
{
    public partial class App : Application
    {
        private const string TrayFlag = "--tray";

        // Held for the life of the process so a second launch can find us and ask us to show the window.
        private Mutex? _instance;
        private EventWaitHandle? _showRequest;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            bool startInTray = e.Args.Contains(TrayFlag, StringComparer.OrdinalIgnoreCase);
            var args = e.Args.Where(a => !a.Equals(TrayFlag, StringComparison.OrdinalIgnoreCase)).ToArray();

            // Headless mode: `MonitorInputWizzard.exe <input name>` switches and exits, no window.
            string? target = ParseTarget(args);
            if (target is not null)
            {
                HeadlessSwitch(target);
                Shutdown();
                return;
            }

            // Single instance: a second launch tells the running copy to show itself, then quits.
            _instance = new Mutex(true, @"Local\MonitorInputWizzard", out bool first);
            _showRequest = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\MonitorInputWizzard.Show");
            if (!first)
            {
                if (!startInTray) _showRequest.Set();
                Shutdown();
                return;
            }

            DispatcherUnhandledException += (_, args) =>
            {
                LogCrash(args.Exception);
                MessageBox.Show(args.Exception.ToString(), "Crash");
                args.Handled = true;
            };
            // Background-thread failures (e.g. the show-request wait callback) can't be handled, but leave a record.
            AppDomain.CurrentDomain.UnhandledException += (_, args) => LogCrash(args.ExceptionObject);

            // The window hides to the tray instead of closing, so only an explicit Exit ends the app.
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var window = new MainWindow();
            SessionEnding += (_, _) => window.ExitApp();
            ThreadPool.RegisterWaitForSingleObject(_showRequest,
                (_, _) => Dispatcher.BeginInvoke(window.ShowFromTray), null, Timeout.Infinite, executeOnlyOnce: false);

            Autostart.RefreshPath();
            if (!startInTray) window.ShowFromTray();
        }

        // Accepts "HDMI1", "HDMI 1", or "--switch HDMI1". Returns null if no switch requested.
        private static string? ParseTarget(string[] args)
        {
            if (args.Length == 0) return null;
            var parts = args[0] is "--switch" or "-s" ? args.Skip(1) : args;
            var name = string.Join(" ", parts).Trim();
            return name.Length == 0 ? null : name;
        }

        private static void LogCrash(object exception)
        {
            try
            {
                File.AppendAllText(Path.Combine(Path.GetTempPath(), "miw-crash.txt"),
                    $"=== {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
            }
            catch { /* nothing more we can do */ }
        }

        private static string Norm(string s) => s.Replace(" ", "").ToLowerInvariant();

        private static void HeadlessSwitch(string name)
        {
            var s = Settings.Load();
            var preset = s.Inputs.FirstOrDefault(i => Norm(i.Name) == Norm(name));
            if (preset is null) return;

            if (s.UseNvApi)
            {
                using var nv = new NvApi();
                nv.SetVcp(s.InputRegister, (ushort)preset.Code, s.SourceAddr);
            }
            else
            {
                using var mc = new MonitorController();
                var mon = mc.Enumerate().FirstOrDefault(m => m.Description == s.MonitorDescription);
                if (mon.Handle != IntPtr.Zero) mc.SetVcp(mon.Handle, s.InputRegister, preset.Code);
            }
        }
    }
}
