using System.Text.RegularExpressions;
using LumenScriptura.Models;

namespace LumenScriptura.Services;

public class ScripturePassageBlock
{
    public bool IsScripture { get; set; }
    public string ReferenceHeader { get; set; } = string.Empty;
    public string Book { get; set; } = string.Empty;
    public int Chapter { get; set; }
    public int StartVerse { get; set; }
    public int EndVerse { get; set; }
    public List<Verse> Verses { get; set; } = new();
    public string PlainText { get; set; } = string.Empty;
}

public class NoteParserService
{
    private readonly BibleDbService _bibleDb;
    
    // Regex matching references like "EXODUS 20:1-17", "1 John 1:9", "John 3:16"
    private static readonly Regex ReferenceRegex = new(
        @"^([1-3]?\s?[A-Za-z]+(?:\s+of\s+[A-Za-z]+)?)\s+([0-9]+):([0-9]+)(?:-([0-9]+))?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public NoteParserService(BibleDbService bibleDb)
    {
        _bibleDb = bibleDb;
    }

    public async Task<List<ScripturePassageBlock>> ParseAndExpandAsync(string rawContent)
    {
        var blocks = new List<ScripturePassageBlock>();
        if (string.IsNullOrWhiteSpace(rawContent)) return blocks;

        var lines = rawContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            var match = ReferenceRegex.Match(trimmed);
            if (match.Success)
            {
                var bookName = match.Groups[1].Value.Trim();
                var chapter = int.Parse(match.Groups[2].Value);
                var startVerse = int.Parse(match.Groups[3].Value);
                var endVerse = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : startVerse;

                // Ensure startVerse <= endVerse
                if (startVerse > endVerse)
                {
                    (startVerse, endVerse) = (endVerse, startVerse);
                }

                var verses = await _bibleDb.GetVerseRangeAsync(bookName, chapter, startVerse, endVerse);
                if (verses.Count > 0)
                {
                    var canonBook = verses[0].BookName;
                    var header = startVerse == endVerse 
                        ? $"{canonBook} {chapter}:{startVerse}" 
                        : $"{canonBook} {chapter}:{startVerse}-{endVerse}";

                    blocks.Add(new ScripturePassageBlock
                    {
                        IsScripture = true,
                        ReferenceHeader = header,
                        Book = canonBook,
                        Chapter = chapter,
                        StartVerse = startVerse,
                        EndVerse = endVerse,
                        Verses = verses
                    });
                    continue;
                }
            }

            // Normal text line
            blocks.Add(new ScripturePassageBlock
            {
                IsScripture = false,
                PlainText = line
            });
        }

        return blocks;
    }

    public bool TryParseReference(string input, out string book, out int chapter, out int startVerse, out int endVerse)
    {
        book = string.Empty;
        chapter = 0;
        startVerse = 0;
        endVerse = 0;

        if (string.IsNullOrWhiteSpace(input)) return false;

        var match = ReferenceRegex.Match(input.Trim());
        if (!match.Success) return false;

        book = match.Groups[1].Value.Trim();
        chapter = int.Parse(match.Groups[2].Value);
        startVerse = int.Parse(match.Groups[3].Value);
        endVerse = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : startVerse;
        return true;
    }
}
