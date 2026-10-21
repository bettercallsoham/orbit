using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Orbit.Models;

namespace Orbit.Services;

/// <summary>Public JSON announcements only. All state changes occur on the WPF UI thread.</summary>
public sealed class RemoteNotificationService
{
    public const string FeedUrl = "https://raw.githubusercontent.com/bettercallsoham/orbit/main/notifications.json";
    public static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private readonly SettingsService _settings;
    private bool _fetching;

    public event Action? Changed;

    public RemoteNotificationService(SettingsService settings)
    {
        _settings = settings;
        Normalize(_settings.Current);
    }

    public IReadOnlyList<RemoteNotification> History => _settings.Current.NotificationHistory
        .OrderByDescending(x => x.FirstSeenUtc).ToList();

    public int UnreadCount => _settings.Current.NotificationHistory.Count(x => !x.IsRead);

    public RemoteNotification? NextBanner(DateTimeOffset now) => _settings.Current.NotificationHistory
        .Where(x => !x.BannerShown && IsActive(x, now))
        .OrderBy(x => x.FirstSeenUtc).FirstOrDefault();

    public async Task FetchIfDueAsync()
    {
        var state = _settings.Current;
        var now = DateTimeOffset.UtcNow;
        if (_fetching || (state.LastNotificationAttemptUtc is { } last && now - last < CheckInterval)) return;
        _fetching = true;
        state.LastNotificationAttemptUtc = now;
        Save();
        try
        {
            using var response = await Http.GetAsync(FeedUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength > 128 * 1024) return;
            await using var stream = await response.Content.ReadAsStreamAsync();
            using var buffer = new MemoryStream();
            var chunk = new byte[8192];
            int read;
            while ((read = await stream.ReadAsync(chunk)) > 0)
            {
                if (buffer.Length + read > 128 * 1024) return;
                buffer.Write(chunk, 0, read);
            }
            var entries = ParseFeed(buffer.ToArray());
            Merge(state, entries, DateTimeOffset.UtcNow);
            state.LastNotificationSuccessUtc = DateTimeOffset.UtcNow;
            Save();
            Changed?.Invoke();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException or JsonException)
        {
            // Announcements never interrupt the app, including offline launches.
        }
        finally { _fetching = false; }
    }

    public void MarkBannerShown(string id)
    {
        var item = Find(id);
        if (item == null || item.BannerShown) return;
        item.BannerShown = true;
        Save();
    }

    public void SetRead(string id, bool read)
    {
        var item = Find(id);
        if (item == null || item.IsRead == read) return;
        item.IsRead = read;
        Save();
        Changed?.Invoke();
    }

    public void MarkAllRead()
    {
        foreach (var item in _settings.Current.NotificationHistory) item.IsRead = true;
        Save();
        Changed?.Invoke();
    }

    public void Remove(string id)
    {
        var state = _settings.Current;
        if (state.NotificationHistory.RemoveAll(x => x.Id == id) == 0) return;
        if (!state.DismissedNotificationIds.Contains(id, StringComparer.Ordinal)) state.DismissedNotificationIds.Add(id);
        Save();
        Changed?.Invoke();
    }

    public RemoteNotification? Find(string id) => _settings.Current.NotificationHistory.FirstOrDefault(x => x.Id == id);

    private void Save() => _settings.SaveSettings(_settings.Current);

    private static void Normalize(AppSettings state)
    {
        state.NotificationHistory ??= new();
        state.SeenNotificationIds ??= new();
        state.DismissedNotificationIds ??= new();
        state.NotificationHistory.RemoveAll(x => x == null || !ValidId(x.Id));
        state.NotificationHistory = state.NotificationHistory.DistinctBy(x => x.Id).ToList();
        foreach (var item in state.NotificationHistory)
            if (!state.SeenNotificationIds.Contains(item.Id, StringComparer.Ordinal)) state.SeenNotificationIds.Add(item.Id);
    }

    public static bool IsActive(RemoteNotification item, DateTimeOffset now) =>
        (item.StartDate == null || item.StartDate <= now) &&
        (item.EndDate == null || item.EndDate > now) &&
        (item.ExpiresAt == null || item.ExpiresAt > now);

    public static bool IsSafeActionUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 2048 || !Uri.TryCreate(value, UriKind.Absolute, out var uri)) return false;
        return uri.Scheme == Uri.UriSchemeHttps && uri.Port == 443 &&
               uri.HostNameType == UriHostNameType.Dns && uri.Host.Contains('.') &&
               !uri.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) &&
               string.IsNullOrEmpty(uri.UserInfo) && !value.Any(char.IsControl);
    }

    public static IReadOnlyList<RemoteNotification> ParseFeed(byte[] data)
    {
        using var document = JsonDocument.Parse(data);
        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty("notifications", out var items) || items.ValueKind != JsonValueKind.Array)
            throw new JsonException("Expected a notifications array.");
        var result = new List<RemoteNotification>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var element in items.EnumerateArray().Take(100))
        {
            if (element.ValueKind != JsonValueKind.Object) continue;
            string? id = GetString(element, "id", 80);
            string? title = GetString(element, "title", 120);
            string? message = GetString(element, "message", 500);
            string? description = GetString(element, "description", 1200);
            string? button = GetString(element, "buttonLabel", 60);
            string? url = GetString(element, "actionUrl", 2048);
            if (!ValidId(id) || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message) ||
                !ValidStringField(element, "id", id) || !ValidStringField(element, "title", title) ||
                !ValidStringField(element, "message", message) || !ValidStringField(element, "description", description) ||
                !ValidStringField(element, "buttonLabel", button) || !ValidStringField(element, "actionUrl", url) ||
                ((button != null || url != null) && (string.IsNullOrWhiteSpace(button) || !IsSafeActionUrl(url))) ||
                !TryDate(element, "startDate", out var start) || !TryDate(element, "endDate", out var end) ||
                !TryDate(element, "expiresAt", out var expiry) || (start != null && end != null && start >= end) ||
                !ids.Add(id!)) continue;
            result.Add(new RemoteNotification { Id = id!, Title = title!, Message = message!, Description = description,
                ButtonLabel = button, ActionUrl = url, StartDate = start, EndDate = end, ExpiresAt = expiry });
        }
        return result;
    }

    private static bool ValidId(string? id) => id != null && Regex.IsMatch(id, "^[A-Za-z0-9][A-Za-z0-9._-]{0,79}$");

    private static bool ValidStringField(JsonElement element, string name, string? parsed) =>
        !element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null || parsed != null;

    private static string? GetString(JsonElement element, string name, int max)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null) return null;
        if (value.ValueKind != JsonValueKind.String) return null;
        string? text = value.GetString()?.Trim();
        return text is { Length: > 0 } && text.Length <= max && !text.Any(char.IsControl) ? text : null;
    }

    private static bool TryDate(JsonElement element, string name, out DateTimeOffset? date)
    {
        date = null;
        if (!element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null) return true;
        string? text = value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        if (text == null || !Regex.IsMatch(text, "(Z|[+-][0-9]{2}:[0-9]{2})$", RegexOptions.IgnoreCase) ||
            !DateTimeOffset.TryParse(text, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var parsed)) return false;
        date = parsed.ToUniversalTime();
        return true;
    }

    public static void Merge(AppSettings state, IEnumerable<RemoteNotification> incoming, DateTimeOffset now)
    {
        Normalize(state);
        foreach (var item in incoming)
        {
            if (!IsActive(item, now)) continue;
            var existing = state.NotificationHistory.FirstOrDefault(x => x.Id == item.Id);
            if (existing != null)
            {
                existing.Title = item.Title;
                existing.Message = item.Message;
                existing.Description = item.Description;
                existing.ButtonLabel = item.ButtonLabel;
                existing.ActionUrl = item.ActionUrl;
                existing.StartDate = item.StartDate;
                existing.EndDate = item.EndDate;
                existing.ExpiresAt = item.ExpiresAt;
            }
            else if (!state.SeenNotificationIds.Contains(item.Id, StringComparer.Ordinal) &&
                     !state.DismissedNotificationIds.Contains(item.Id, StringComparer.Ordinal))
            {
                item.FirstSeenUtc = now;
                state.NotificationHistory.Add(item);
                state.SeenNotificationIds.Add(item.Id);
            }
        }
        // Keep a bounded center; seen IDs are retained to prevent repeat alerts after cleanup.
        state.NotificationHistory = state.NotificationHistory.OrderByDescending(x => x.FirstSeenUtc)
            .Where(x => x.FirstSeenUtc >= now.AddDays(-180) || !x.BannerShown)
            .Take(100).ToList();
    }
}
