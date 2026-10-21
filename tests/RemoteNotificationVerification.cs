using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using Orbit.Models;
using Orbit.Services;

namespace Orbit.Tests;

public static class RemoteNotificationVerification
{
    public static void Run()
    {
        var now = new DateTimeOffset(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
        string json = """
            {"notifications":[
              {"id":"post-1","title":"Hello","message":"Read this","buttonLabel":"Open","actionUrl":"https://example.com/news","startDate":"2026-10-21T00:00:00Z"},
              {"id":"post-1","title":"Duplicate","message":"Skip"},
              {"id":"bad-url","title":"Bad","message":"Skip","buttonLabel":"Run","actionUrl":"file:///C:/bad.exe"},
              {"id":"future","title":"Later","message":"Skip","startDate":"2026-10-22T00:00:00Z"},
              {"id":"expired","title":"Old","message":"Skip","expiresAt":"2026-10-20T00:00:00Z"},
              {"id":"bad-type","title":"Bad","message":"Skip","description":42}
            ]}
            """;
        var parsed = RemoteNotificationService.ParseFeed(Encoding.UTF8.GetBytes(json));
        Require(parsed.Count == 3, "Malformed or duplicate entries were accepted.");
        var state = new AppSettings();
        RemoteNotificationService.Merge(state, parsed, now);
        Require(state.NotificationHistory.Count == 1 && state.NotificationHistory[0].Id == "post-1", "Date filtering failed.");
        state.NotificationHistory[0].IsRead = true;
        state.NotificationHistory[0].BannerShown = true;
        var edited = RemoteNotificationService.ParseFeed(Encoding.UTF8.GetBytes("""
            {"notifications":[{"id":"post-1","title":"Updated","message":"Changed"}]}
            """));
        RemoteNotificationService.Merge(state, edited, now.AddHours(1));
        Require(state.NotificationHistory.Count == 1 && state.NotificationHistory[0].IsRead &&
                state.NotificationHistory[0].BannerShown && state.NotificationHistory[0].Title == "Updated", "Edit reset notification state.");
        string saved = JsonSerializer.Serialize(state);
        var restored = JsonSerializer.Deserialize<AppSettings>(saved)!;
        Require(restored.NotificationHistory.Count == 1 && restored.NotificationHistory[0].IsRead, "Persistence failed.");
        restored.NotificationHistory.Clear();
        RemoteNotificationService.Merge(restored, edited, now.AddHours(2));
        Require(restored.NotificationHistory.Count == 0, "Seen ID alerted twice after removal.");
        Require(!RemoteNotificationService.IsSafeActionUrl("https://user:pass@example.com") &&
                !RemoteNotificationService.IsSafeActionUrl("https://127.0.0.1/path") &&
                RemoteNotificationService.IsSafeActionUrl("https://github.com/bettercallsoham/orbit"), "URL validation failed.");
        Console.WriteLine("[notifications] feed validation, dates, deduplication, edits, persistence and URL safety passed");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
