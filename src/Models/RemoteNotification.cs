using System;

namespace Orbit.Models;

public sealed class RemoteNotification
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string? Description { get; set; }
    public string? ButtonLabel { get; set; }
    public string? ActionUrl { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset FirstSeenUtc { get; set; }
    public bool IsRead { get; set; }
    public bool BannerShown { get; set; }
}
