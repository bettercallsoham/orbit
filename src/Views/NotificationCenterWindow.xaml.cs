using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Orbit.Models;
using Orbit.Services;

namespace Orbit.Views;

public partial class NotificationCenterWindow : Window
{
    private readonly RemoteNotificationService _notifications;

    public NotificationCenterWindow(RemoteNotificationService notifications)
    {
        InitializeComponent();
        _notifications = notifications;
        _notifications.Changed += Refresh;
        Closed += (_, _) => _notifications.Changed -= Refresh;
        Refresh();
    }

    private void Refresh()
    {
        var items = _notifications.History.ToList();
        NotificationItems.ItemsSource = items;
        EmptyMessage.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string? IdFrom(object sender) => (sender as Button)?.Tag as string;

    private void OnMarkAllReadClick(object sender, RoutedEventArgs e) => _notifications.MarkAllRead();

    private void OnReadToggleClick(object sender, RoutedEventArgs e)
    {
        if (IdFrom(sender) is not { } id || _notifications.Find(id) is not { } item) return;
        _notifications.SetRead(id, !item.IsRead);
    }

    private void OnRemoveClick(object sender, RoutedEventArgs e)
    {
        if (IdFrom(sender) is { } id) _notifications.Remove(id);
    }

    private void OnOpenClick(object sender, RoutedEventArgs e)
    {
        if (IdFrom(sender) is not { } id || _notifications.Find(id) is not { } item) return;
        _notifications.SetRead(id, true);
        if (RemoteNotificationService.IsSafeActionUrl(item.ActionUrl))
        {
            try { Process.Start(new ProcessStartInfo(item.ActionUrl!) { UseShellExecute = true }); }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
            { MessageBox.Show(this, "Could not open the link in your browser.", "Orbit", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }
    }
}
