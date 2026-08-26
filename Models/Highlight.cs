namespace LumenScriptura.Models;

public class Highlight
{
    public int Id { get; set; }
    public string VerseRef { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Color { get; set; } = "gold"; // "gold", "blue", "neutral"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

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
