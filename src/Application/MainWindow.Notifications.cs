using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Orbit.Services;
using Orbit.Views;

namespace Orbit;

public partial class MainWindow
{
    private RemoteNotificationService? _remoteNotifications;
    private DispatcherTimer? _remoteNotificationTimer;
    private NotificationCenterWindow? _notificationCenter;
    private string? _currentBannerId;

    private void SetupRemoteNotifications()
    {
        _remoteNotifications = new RemoteNotificationService(_settingsService);
        _remoteNotifications.Changed += RefreshRemoteNotificationUi;
        _remoteNotificationTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) };
        _remoteNotificationTimer.Tick += async (_, _) => await _remoteNotifications.FetchIfDueAsync();
        _remoteNotificationTimer.Start();
        IsVisibleChanged += (_, _) => RefreshRemoteNotificationUi();
        RefreshRemoteNotificationUi();
        // Begin after construction; the HTTP request itself never holds the startup path.
        Dispatcher.BeginInvoke(async () => await _remoteNotifications.FetchIfDueAsync(), DispatcherPriority.Background);
    }

    private void RefreshRemoteNotificationUi()
    {
        if (_remoteNotifications == null || NotificationBadge == null) return;
        int unread = _remoteNotifications.UnreadCount;
        NotificationBadge.Visibility = unread > 0 ? Visibility.Visible : Visibility.Collapsed;
        NotificationBadgeText.Text = unread > 99 ? "99+" : unread.ToString();
        if (_currentBannerId != null && _remoteNotifications.Find(_currentBannerId) == null)
        {
            _currentBannerId = null;
            RemoteNotificationBanner.Visibility = Visibility.Collapsed;
        }
        if (_currentBannerId != null || !IsVisible || ViewOverview.Visibility != Visibility.Visible) return;
        var item = _remoteNotifications.NextBanner(DateTimeOffset.UtcNow);
        if (item == null) return;
        _currentBannerId = item.Id;
        RemoteBannerTitle.Text = item.Title;
        RemoteBannerMessage.Text = item.Message;
        RemoteBannerAction.Content = item.ButtonLabel;
        RemoteBannerAction.Visibility = RemoteNotificationService.IsSafeActionUrl(item.ActionUrl) ? Visibility.Visible : Visibility.Collapsed;
        RemoteNotificationBanner.Visibility = Visibility.Visible;
        _remoteNotifications.MarkBannerShown(item.Id);
    }

    private void AdvanceBanner()
    {
        _currentBannerId = null;
        RemoteNotificationBanner.Visibility = Visibility.Collapsed;
        RefreshRemoteNotificationUi();
    }

    private void OnRemoteBannerDismissClick(object sender, RoutedEventArgs e) => AdvanceBanner();

    private void OnRemoteBannerActionClick(object sender, RoutedEventArgs e)
    {
        if (_currentBannerId is { } id && _remoteNotifications?.Find(id) is { } item)
        {
            _remoteNotifications.SetRead(id, true);
            OpenBrowserUrl(item.ActionUrl);
        }
        AdvanceBanner();
    }

    private void OnNotificationBellClick(object sender, RoutedEventArgs e)
    {
        if (_remoteNotifications == null) return;
        if (_notificationCenter is { IsVisible: true }) { _notificationCenter.Activate(); return; }
        _notificationCenter = new NotificationCenterWindow(_remoteNotifications) { Owner = this };
        _notificationCenter.Closed += (_, _) => _notificationCenter = null;
        _notificationCenter.Show();
    }

    private void OnStarOrbitClick(object sender, RoutedEventArgs e) => OpenBrowserUrl("https://github.com/bettercallsoham/orbit");

    private void OpenBrowserUrl(string? url)
    {
        if (!RemoteNotificationService.IsSafeActionUrl(url)) return;
        try { Process.Start(new ProcessStartInfo(url!) { UseShellExecute = true }); }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        { MessageBox.Show(this, "Could not open the link in your browser.", "Orbit", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
}
