namespace LumenScriptura.Models;

public class Book
{
    public int BookNumber { get; set; }
    public string ShortName { get; set; } = string.Empty;
    public string LongName { get; set; } = string.Empty;
    public bool IsNewTestament => BookNumber >= 470;
    public int TotalChapters { get; set; }
}
