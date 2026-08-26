namespace LumenScriptura.Models;

public class HistoryItem
{
    public int Id { get; set; }
    public string Book { get; set; } = string.Empty;
    public int Chapter { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string Reference => $"{Book} {Chapter}";

    public string RelativeTime
    {
        get
        {
            var span = DateTime.UtcNow - Timestamp;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 2) return "Yesterday";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays} days ago";
            return Timestamp.ToString("MMM d, yyyy");
        }
    }
}
