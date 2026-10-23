using System;
using System.Threading;
using System.Windows;
using Orbit.Services;
using System.Runtime.InteropServices;

namespace Orbit
{
    public partial class App : Application
    {
        private Mutex? _singleInstanceMutex;

        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;

        protected override void OnStartup(StartupEventArgs e)
        {
            AttachConsole(ATTACH_PARENT_PROCESS);

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"[Orbit Fatal Error]: {args.ExceptionObject}");
                Console.ResetColor();
            };

            DispatcherUnhandledException += (s, args) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"[Orbit UI Exception]: {args.Exception}");
                Console.ResetColor();
                MessageBox.Show($"Startup Error:\n{args.Exception.Message}\n\n{args.Exception.StackTrace}", "Orbit Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            if (e.Args.Length > 0 && e.Args[0] == "--notices")
            {
                foreach (string name in new[] { "LICENSE", "COPYRIGHT", "THIRD-PARTY-NOTICES.txt", "Anton-OFL.txt" })
                {
                    using var stream = typeof(App).Assembly.GetManifestResourceStream("Orbit.Notices." + name);
                    if (stream == null) throw new InvalidOperationException($"Missing embedded notice: {name}");
                    using var reader = new System.IO.StreamReader(stream);
                    Console.WriteLine($"===== {name} =====\n{reader.ReadToEnd()}");
                }
                Shutdown();
                return;
            }

            if (e.Args.Length > 0 && e.Args[0] == "--test")
            {
                // Headless: report and exit with a code instead of showing an error dialog nobody can close
                int exitCode = 0;
                string templateTestDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "orbit-template-test-" + Guid.NewGuid().ToString("N")[..8]);
                Tests.TemplateVerification.IsolateTemplatesFolder(templateTestDir);
                try
                {
                    // "--test markdown,everyday" runs just those sections
                    string[]? only = e.Args.Length > 1 ? e.Args[1].Split(',', StringSplitOptions.RemoveEmptyEntries) : null;
                    if (only == null || Array.Exists(only, section => section != "templates"))
                        Task.Run(async () => await Tests.VerificationRunner.RunSelfTestAsync(only)).GetAwaiter().GetResult();
                    // Renders WPF visuals, so it runs here on the UI (STA) thread
                    if (only == null || Array.Exists(only, section => section == "templates"))
                        Tests.TemplateVerification.Run(templateTestDir);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"SELF-TEST FAILED: {ex}");
                    exitCode = 1;
                }
                Shutdown(exitCode);
                return;
            }

            if (e.Args.Length > 0 && e.Args[0] == "--convert")
            {
                // On the UI thread, like a job started from the wheel
                Dispatcher.InvokeAsync(async () => Shutdown(await CommandLine.ConvertAsync(e.Args)));
                return;
            }

            if (e.Args.Length > 1 && e.Args[0] == "--snapshot")
            {
                Tests.SnapshotRunner.Run(e.Args[1]);
                Shutdown();
                return;
            }

            if (Array.IndexOf(e.Args, Services.InstallService.UninstallArg) >= 0)
            {
                RunUninstall(quiet: Array.IndexOf(e.Args, "--quiet") >= 0);
                Shutdown();
                return;
            }

            // One interactive instance per Windows session. A second launch reopens the existing window,
            // including when it is hidden in the tray, without tearing down its hooks or active work.
            _singleInstanceMutex = new Mutex(true, "Local\\Orbit.InteractiveInstance", out bool isFirstInstance);
            if (!isFirstInstance)
            {
                TrayIconService.RequestExistingWindow();
                Shutdown();
                return;
            }

            Services.UpdateService.CleanupAfterUpdate();
            Services.InstallService.RefreshUninstallEntryVersion();

            base.OnStartup(e);

            var main = new MainWindow { JustInstalled = Array.IndexOf(e.Args, Services.InstallService.JustInstalledArg) >= 0 };
            MainWindow = main;
            if (Array.IndexOf(e.Args, Services.InstallService.BackgroundArg) >= 0)
            {
                // Started with Windows: stay in the tray. Hooks and the tray icon need the window handle, not a visible window.
                main.StartHidden();
            }
            else
            {
                main.Show();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _singleInstanceMutex?.Dispose();
            base.OnExit(e);
        }

        private static void RunUninstall(bool quiet)
        {
            if (!quiet)
            {
                var answer = MessageBox.Show(
                    "Remove Orbit from this PC?\n\nYour settings and wheel templates are kept.",
                    "Uninstall Orbit", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                if (answer != MessageBoxResult.OK) return;
            }

            try
            {
                Services.InstallService.Uninstall();
                if (!quiet) MessageBox.Show("Orbit has been removed.", "Uninstall Orbit", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                if (!quiet) MessageBox.Show($"Couldn't finish removing Orbit:\n{ex.Message}", "Uninstall Orbit", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
