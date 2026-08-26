namespace LumenScriptura.Models;

public class Verse
{
    public int BookNumber { get; set; }
    public string BookName { get; set; } = string.Empty;
    public int Chapter { get; set; }
    public int VerseNum { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? HighlightColor { get; set; }
    public bool IsBookmarked { get; set; }
    public string Reference => $"{BookName} {Chapter}:{VerseNum}";
}
