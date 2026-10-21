using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Orbit.Services;
using Orbit.Views;

namespace Orbit
{
    /// <summary>
    /// Optional helpers: the "Recommended extras" prompt (first run and right after Install) and the Install buttons in
    /// Settings › Engine. FFmpeg is offered when missing; LibreOffice only when neither Microsoft Office nor LibreOffice is
    /// found. The browser is only reported.
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>Set by App when this copy was just started by Install, so the prompt shows even if it was seen before.</summary>
        public bool JustInstalled { get; set; }

        /// <summary>Off for snapshots, which show the window off-screen and can't answer a dialog.</summary>
        public bool OfferExtras { get; set; } = true;

        private void SetupExtrasPrompt()
        {
            ContentRendered += async (_, _) =>
            {
                await Task.Delay(400); // let the window settle before a dialog appears over it
                ShowExtrasPromptIfNeeded();
            };
        }

        private void ShowExtrasPromptIfNeeded()
        {
            if (!OfferExtras || (!JustInstalled && _settingsService.Current.ExtrasPromptShown)) return;

            bool ffmpegMissing = !_engine.IsFFmpegAvailable;
            bool officeMissing = !DocumentEngine.HasAnyBackend;
            if (!ffmpegMissing && !officeMissing) return;

            _settingsService.Current.ExtrasPromptShown = true;
            _settingsService.SaveSettings(_settingsService.Current);

            var dialog = new ExtrasDialog(ffmpegMissing, officeMissing, InstallDependencyAsync) { Owner = this };
            dialog.ShowDialog();
        }

        private async void OnInstallFFmpegClick(object sender, RoutedEventArgs e) =>
            await InstallFromSettingsAsync(Dependency.FFmpeg, InstallFFmpegButton, FFmpegStatusText);

        private async void OnInstallLibreOfficeClick(object sender, RoutedEventArgs e) =>
            await InstallFromSettingsAsync(Dependency.LibreOffice, InstallLibreOfficeButton, DocEngineStatusText);

        private async Task InstallFromSettingsAsync(Dependency dep, Button button, TextBlock badgeText)
        {
            if (!DependencyService.CanInstall)
            {
                OpenDownloadPage(dep);
                return;
            }

            if (dep == Dependency.LibreOffice)
            {
                var answer = MessageBox.Show(this,
                    "Install LibreOffice, the free office suite?\n\nIt's about 350 MB and can take several minutes. Windows will ask for admin permission once.\n\n" +
                    "Prefer Microsoft Office? It isn't free, but if you install it, Orbit uses it automatically.",
                    "Install LibreOffice", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                if (answer != MessageBoxResult.OK) return;
            }

            object content = button.Content;
            button.IsEnabled = false;
            button.Content = "Installing…";
            badgeText.Text = "Installing…";
            var elapsed = Stopwatch.StartNew();
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (_, _) => badgeText.Text = $"Installing… {elapsed.Elapsed:mm\\:ss}";
            timer.Start();
            string? error;
            try { error = await InstallDependencyAsync(dep); }
            finally
            {
                timer.Stop();
                button.Content = content;
                button.IsEnabled = true;
                CheckFFmpegStatus();
            }
            if (error != null)
            {
                MessageBox.Show(this, error, $"Install {DependencyService.DisplayName(dep)}", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>Installs <paramref name="dep"/> and looks for it again. Returns null when it's ready to use, otherwise why not.</summary>
        private async Task<string?> InstallDependencyAsync(Dependency dep)
        {
            string name = DependencyService.DisplayName(dep);
            int exitCode;
            string output;
            try
            {
                (exitCode, output) = await DependencyService.InstallAsync(dep);
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or System.IO.IOException)
            {
                CheckFFmpegStatus();
                return $"Couldn't start the installer: {ex.Message}\n\nYou can download {name} from {DependencyService.DownloadPage(dep)}";
            }

            // winget's exit code isn't enough ("already installed" is an error to it): success means we can find it now
            if (dep == Dependency.FFmpeg) _engine.RefreshFFmpeg();
            else DocumentEngine.RefreshLibreOffice();
            CheckFFmpegStatus();

            bool ready = dep == Dependency.FFmpeg ? _engine.IsFFmpegAvailable : DocumentEngine.LibreOfficePath != null;
            if (ready) return null;

            string detail = string.IsNullOrWhiteSpace(output) ? $"winget exit code 0x{exitCode:X8}" : output;
            return exitCode == 0
                ? $"{name} was installed, but Orbit can't find it yet. Restart Orbit to pick it up."
                : $"{name} wasn't installed ({detail}). If you cancelled the admin prompt, try again, or download it from {DependencyService.DownloadPage(dep)}";
        }

        private void OpenDownloadPage(Dependency dep)
        {
            try { DependencyService.OpenDownloadPage(dep); }
            catch (Win32Exception ex)
            {
                MessageBox.Show(this, $"Couldn't open the browser:\n{ex.Message}\n\n{DependencyService.DownloadPage(dep)}",
                    $"Install {DependencyService.DisplayName(dep)}", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
