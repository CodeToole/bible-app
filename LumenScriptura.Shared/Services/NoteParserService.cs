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
    private readonly IBibleService _bibleDb;
    
    // Regex matching references like "1 KINGS 8:27-30 41-44, 46-53", "GENESIS 1:1-5, 8-10, 12", "JOHN 3:16, 18-21", "EXODUS 20:1-17", "1 John 1:9"
    private static readonly Regex ReferenceRegex = new(
        @"^([1-3]?\s?[A-Za-z]+(?:\s+[A-Za-z]+)*)\s+([0-9]+)\s*:\s*([0-9\s,\-;–—]+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex RangeRegex = new(
        @"(\d+)(?:\s*[-–—]\s*(\d+))?",
        RegexOptions.Compiled);

    public NoteParserService(IBibleService bibleDb)
    {
        _bibleDb = bibleDb;
    }

    public static List<(int Start, int End)> ParseVerseRanges(string verseStr)
    {
        var ranges = new List<(int Start, int End)>();
        if (string.IsNullOrWhiteSpace(verseStr)) return ranges;

        var matches = RangeRegex.Matches(verseStr);
        foreach (Match m in matches)
        {
            if (int.TryParse(m.Groups[1].Value, out var start))
            {
                var end = m.Groups[2].Success && int.TryParse(m.Groups[2].Value, out var parsedEnd)
                    ? parsedEnd
                    : start;

                if (start > end)
                {
                    (start, end) = (end, start);
                }

                ranges.Add((start, end));
            }
        }

        return ranges;
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
                if (int.TryParse(match.Groups[2].Value, out var chapter))
                {
                    var verseStr = match.Groups[3].Value;
                    var ranges = ParseVerseRanges(verseStr);

                    if (ranges.Count > 0)
                    {
                        var combinedVerses = new List<Verse>();
                        var seenVerseNums = new HashSet<int>();

                        foreach (var (start, end) in ranges)
                        {
                            var rangeVerses = await _bibleDb.GetVerseRangeAsync(bookName, chapter, start, end);
                            foreach (var v in rangeVerses)
                            {
                                if (seenVerseNums.Add(v.VerseNum))
                                {
                                    combinedVerses.Add(v);
                                }
                            }
                        }

                        if (combinedVerses.Count > 0)
                        {
                            var canonBook = combinedVerses[0].BookName;
                            var rangeSegments = ranges.Select(r => r.Start == r.End ? $"{r.Start}" : $"{r.Start}-{r.End}");
                            var header = $"{canonBook} {chapter}:{string.Join(", ", rangeSegments)}";

                            blocks.Add(new ScripturePassageBlock
                            {
                                IsScripture = true,
                                ReferenceHeader = header,
                                Book = canonBook,
                                Chapter = chapter,
                                StartVerse = ranges[0].Start,
                                EndVerse = ranges[^1].End,
                                Verses = combinedVerses
                            });
                            continue;
                        }
                    }
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
        if (!int.TryParse(match.Groups[2].Value, out chapter)) return false;

        var ranges = ParseVerseRanges(match.Groups[3].Value);
        if (ranges.Count == 0) return false;

        startVerse = ranges[0].Start;
        endVerse = ranges[^1].End;
        return true;
    }

    public bool TryParseReference(string input, out string book, out int chapter, out List<(int Start, int End)> ranges)
    {
        book = string.Empty;
        chapter = 0;
        ranges = new List<(int Start, int End)>();

        if (string.IsNullOrWhiteSpace(input)) return false;

        var match = ReferenceRegex.Match(input.Trim());
        if (!match.Success) return false;

        book = match.Groups[1].Value.Trim();
        if (!int.TryParse(match.Groups[2].Value, out chapter)) return false;

        ranges = ParseVerseRanges(match.Groups[3].Value);
        return ranges.Count > 0;
    }
}
