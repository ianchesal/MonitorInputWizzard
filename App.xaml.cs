using System.IO;
using System.Windows;

namespace MonitorInputWizzard
{
    public partial class App : Application
    {
        // Held for the life of the process so a second launch can find us and ask us to show the window.
        private Mutex? _instance;
        private EventWaitHandle? _showRequest;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var args = CommandLine.StripTrayFlag(e.Args, out bool startInTray);

            // Headless mode: `MonitorInputWizzard.exe <input name>` switches and exits, no window.
            string? target = CommandLine.ParseTarget(args);
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

        private static void LogCrash(object exception)
        {
            try
            {
                File.AppendAllText(Path.Combine(Path.GetTempPath(), "miw-crash.txt"),
                    $"=== {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
            }
            catch { /* nothing more we can do */ }
        }

        private static void HeadlessSwitch(string name)
        {
            var s = Settings.Load();
            var preset = CommandLine.FindPreset(s.Inputs, name);
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
