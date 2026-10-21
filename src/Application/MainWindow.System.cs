using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using Orbit.Services;

namespace Orbit
{
    /// <summary>Settings › System (optional per-user install, uninstall, start with Windows).</summary>
    public partial class MainWindow
    {
        private bool _syncingStartup;

        private void RefreshInstallState()
        {
            bool installed = InstallService.IsInstalled;
            bool runningInstalled = InstallService.IsRunningInstalled;
            bool hasExe = InstallService.CurrentExePath != null;
            string here = hasExe ? Path.GetDirectoryName(InstallService.CurrentExePath)! : AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);

            if (runningInstalled)
            {
                InstallTitleText.Text = "Installed";
                InstallStatusText.Text = $"Version {InstallService.CurrentVersion} in {InstallService.InstallDirectory}. Also listed in Windows Settings › Apps.";
                InstallButton.Visibility = Visibility.Collapsed;
            }
            else if (!hasExe)
            {
                InstallTitleText.Text = "Development launch";
                InstallStatusText.Text = $"Orbit is running from a build folder ({here}) through dotnet. Use a packaged executable for the optional per-user installation.";
                InstallButton.Visibility = Visibility.Collapsed;
            }
            else if (installed)
            {
                InstallTitleText.Text = "Installed copy found";
                InstallStatusText.Text = $"Version {InstallService.InstalledVersion ?? "?"} is installed. This copy is running from {here}. Updating replaces the installed copy with this one.";
                InstallButton.Content = "Update installed copy";
                InstallButton.Visibility = Visibility.Visible;
            }
            else
            {
                InstallTitleText.Text = "Optional installation";
                InstallStatusText.Text = $"Orbit runs from this folder ({here}). Optional installation adds Orbit to Start at {InstallService.InstallDirectory}. Portable use needs no installation.";
                InstallButton.Content = "Install";
                InstallButton.Visibility = Visibility.Visible;
            }
            UninstallButton.Visibility = installed ? Visibility.Visible : Visibility.Collapsed;
            InstallDesktopShortcutCheck.Visibility = runningInstalled ? Visibility.Collapsed : Visibility.Visible;
            InstallDesktopShortcutCheck.IsEnabled = hasExe && !runningInstalled;

            _syncingStartup = true;
            StartWithWindowsCheck.IsChecked = InstallService.IsStartWithWindowsEnabled;
            _syncingStartup = false;
            StartWithWindowsCheck.IsEnabled = installed || hasExe;
            StartupDescText.Text = installed || runningInstalled
                ? "Opens quietly in the system tray when you sign in, so the wheel is always ready"
                : hasExe
                    ? "Opens quietly in the system tray when you sign in. Points to this copy: install first if you might move or delete it."
                    : "Available when you launch the packaged Orbit executable.";

        }

        private void OnStartWithWindowsChanged(object sender, RoutedEventArgs e)
        {
            if (_syncingStartup || _settingsService == null) return;
            bool enabled = StartWithWindowsCheck.IsChecked == true;
            try
            {
                InstallService.SetStartWithWindows(enabled);
                _settingsService.Current.StartWithWindows = enabled;
                _settingsService.SaveSettings(_settingsService.Current);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException or InvalidOperationException)
            {
                MessageBox.Show(this, $"Couldn't change the startup setting:\n{ex.Message}", "Start with Windows", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            RefreshInstallState();
        }

        private void OnInstallClick(object sender, RoutedEventArgs e) =>
            RunInstall(startWithWindows: StartWithWindowsCheck.IsChecked == true || !InstallService.IsInstalled,
                       desktopShortcut: InstallDesktopShortcutCheck.IsChecked == true,
                       confirm: false);

        private void RunInstall(bool startWithWindows, bool desktopShortcut, bool confirm)
        {
            if (confirm)
            {
                var answer = MessageBox.Show(this,
                    $"Install Orbit to\n{InstallService.InstallDirectory}\n\nIt will be added to Start, listed in Windows Settings › Apps, and start in the tray when you sign in (you can turn that off in Settings › System).",
                    "Install Orbit", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                if (answer != MessageBoxResult.OK) return;
            }

            try
            {
                Cursor = System.Windows.Input.Cursors.Wait;
                _settingsService.Flush(); // the installed copy reads the same settings file
                InstallService.Install(startWithWindows, desktopShortcut);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or Win32Exception)
            {
                Cursor = null;
                MessageBox.Show(this, $"Installation didn't finish:\n{ex.Message}", "Install Orbit", MessageBoxButton.OK, MessageBoxImage.Warning);
                RefreshInstallState();
                return;
            }

            Cursor = null;
            MessageBox.Show(this, "Orbit is installed. It will now restart from its new home.", "Install Orbit", MessageBoxButton.OK, MessageBoxImage.Information);
            try
            {
                InstallService.LaunchInstalled();
                Application.Current.Shutdown();
            }
            catch (Win32Exception ex)
            {
                MessageBox.Show(this, $"Installed, but couldn't start it:\n{ex.Message}\n\nOpen Orbit from the Start menu.", "Install Orbit", MessageBoxButton.OK, MessageBoxImage.Warning);
                RefreshInstallState();
            }
        }

        private void OnUninstallClick(object sender, RoutedEventArgs e)
        {
            bool self = InstallService.IsRunningInstalled;
            var answer = MessageBox.Show(this,
                "Remove the installed Orbit, its Start menu entry and the startup entry?\n\nYour settings and wheel templates are kept." +
                (self ? "\n\nOrbit will close." : string.Empty),
                "Uninstall Orbit", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (answer != MessageBoxResult.OK) return;

            try
            {
                InstallService.Uninstall();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception)
            {
                MessageBox.Show(this, $"Couldn't finish removing Orbit:\n{ex.Message}", "Uninstall Orbit", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            if (self) Application.Current.Shutdown();
            else RefreshInstallState();
        }
    }
}
