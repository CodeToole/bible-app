namespace LumenScriptura.Models;

public class Bookmark
{
    public int Id { get; set; }
    public string Book { get; set; } = string.Empty;
    public int Chapter { get; set; }
    public int Verse { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Reference => $"{Book} {Chapter}:{Verse}";

    public string RelativeTime
    {
        get
        {
            var span = DateTime.UtcNow - CreatedAt;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 2) return "Yesterday";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays} days ago";
            return CreatedAt.ToString("MMM d, yyyy");
        }
    }
}
