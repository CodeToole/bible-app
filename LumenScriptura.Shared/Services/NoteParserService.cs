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

    // Expanded book aliases for handwritten and OCR shorthands
    private static readonly Dictionary<string, string> ExpandedAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Zach"] = "Zechariah",
        ["1 Petr"] = "1 Peter",
        ["2 Petr"] = "2 Peter",
        ["1 Pt"] = "1 Peter",
        ["2 Pt"] = "2 Peter",
        ["Mat"] = "Matthew",
        ["Mt"] = "Matthew",
        ["Ez"] = "Ezekiel",
        ["Ezek"] = "Ezekiel",
        ["1 Thes"] = "1 Thessalonians",
        ["2 Thes"] = "2 Thessalonians",
        ["1 Thess"] = "1 Thessalonians",
        ["Eccl"] = "Ecclesiastes",
        ["Ex"] = "Exodus",
        ["Phil"] = "Philippians",
        ["Mal"] = "Malachi",
        ["Num"] = "Numbers",
        ["Heb"] = "Hebrews"
    };

    // Regex matching references like "1 KINGS 8:27-30 41-44, 46-53", "Mat 24:1-5, 11-13, 22:1-5, 8-14", "REV 22:14, 16a"
    private static readonly Regex ReferenceRegex = new(
        @"^([1-3]?\s?[A-Za-z]+(?:\s+[A-Za-z]+)*)\s*([0-9]+\s*:\s*[0-9\s,\-;:\-a-zA-Z]+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex RangeRegex = new(
        @"(\d+)[a-zA-Z]*(?:\s*[-–—−]\s*(\d+)[a-zA-Z]*)?",
        RegexOptions.Compiled);

    private static readonly Regex ParentheticalRegex = new(
        @"\([^)]*\)",
        RegexOptions.Compiled);

    private static readonly Regex NumberedBookGluedRegex = new(
        @"(?<=\d)([1-3])\s*(Petr|Peter|Pet|Pt|Thess|Thes|Sam|Samuel|Kgs|King|Kings|Chron|Chr|Chronicles|Cor|Corinthians|Tim|Timothy|Jn|John)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex NonNumberedBookGluedRegex = new(
        @"(?<=\d)(Eccl|Ecc|Rev|Revelation|Revelations|Mat|Matt|Matthew|Mt|Ez|Ezek|Ezekiel|Ex|Exod|Exo|Exodus|Phil|Philippians|Mal|Malachi|Num|Numbers|Heb|Hebrews|Zach|Zech|Zechariah|Gen|Genesis|Lev|Leviticus|Deut|Deuteronomy|Josh|Joshua|Judg|Judges|Ruth|Ezra|Neh|Nehemiah|Esth|Esther|Job|Ps|Psalm|Psalms|Prov|Proverbs|Song|Songs|Isa|Isaiah|Jer|Jeremiah|Lam|Lamentations|Dan|Daniel|Hos|Hosea|Joel|Amos|Obad|Obadiah|Jonah|Mic|Micah|Nah|Nahum|Hab|Habakkuk|Zeph|Zephaniah|Hag|Haggai|Mark|Mrk|Luke|Luk|John|Acts|Rom|Romans|Gal|Galatians|Eph|Ephesians|Col|Colossians|Tit|Titus|Philem|Philemon|Jas|James|Jude)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex NumberedBookSpaceFixRegex = new(
        @"\b([1-3])(Petr|Peter|Pet|Pt|Thess|Thes|Sam|Samuel|Kgs|King|Kings|Chron|Chr|Chronicles|Cor|Corinthians|Tim|Timothy|Jn|John)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex LeadingItemNumberRegex = new(
        @"^(?:\d+[\.\)]\s*|(?!\b[1-3]\s+(?:Petr|Peter|Pet|Pt|Thess|Thes|Sam|Samuel|Kgs|King|Kings|Chron|Chr|Chronicles|Cor|Corinthians|Tim|Timothy|Jn|John)\b)\d+\s+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ConcatenatedReferenceSplitRegex = new(
        @"(?<=:\s*\d+[^\r\n:]*?)\s+(?=(?:\d+[\.\)]\s+|\d+\s+)?(?:[1-3]\s+)?[A-Za-z]+(?:\s+[A-Za-z]+)*\s+\d+\s*:)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ChapterMarkerRegex = new(
        @"(?:^|[\s,;])(\d+)\s*:",
        RegexOptions.Compiled);

    public NoteParserService(IBibleService bibleDb)
    {
        _bibleDb = bibleDb;
    }

    public static string ResolveCanonicalBookName(string rawBookName)
    {
        if (string.IsNullOrWhiteSpace(rawBookName)) return string.Empty;
        var trimmed = rawBookName.Trim();
        if (ExpandedAliases.TryGetValue(trimmed, out var alias)) return alias;
        if (BibleBookAliases.TryGetCanonicalName(trimmed, out var canon)) return canon;
        return trimmed;
    }

    public static string PreClean(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input ?? string.Empty;

        // 1. Standardize en-dashes, em-dashes, and unicode minus to standard hyphens
        var text = input.Replace('–', '-').Replace('—', '-').Replace('−', '-');

        // 2. Remove parenthetical text matching \([^)]*\)
        text = ParentheticalRegex.Replace(text, "");

        // 3. Insert boundary spaces whenever a book abbreviation is glued directly to a number
        // e.g. 151 Petr -> 15 1 Petr, 151Petr -> 15 1 Petr
        text = NumberedBookGluedRegex.Replace(text, " $1 $2");

        // e.g. 17Eccl -> 17 Eccl, 14Rev -> 14 Rev, 19Rev -> 19 Rev
        text = NonNumberedBookGluedRegex.Replace(text, " $1");

        // Ensure space between [1-3] prefix and book abbreviation if glued (e.g. 1Petr -> 1 Petr)
        text = NumberedBookSpaceFixRegex.Replace(text, "$1 $2");

        return text;
    }

    public static string StripLeadingItemNumber(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input ?? string.Empty;
        var trimmed = input.Trim();
        var match = LeadingItemNumberRegex.Match(trimmed);
        if (match.Success)
        {
            return trimmed.Substring(match.Length).Trim();
        }
        return trimmed;
    }

    public static List<(int Chapter, List<(int Start, int End)> Ranges)> ParseChapterSegments(string payload)
    {
        var results = new List<(int Chapter, List<(int Start, int End)> Ranges)>();
        if (string.IsNullOrWhiteSpace(payload)) return results;

        var matches = ChapterMarkerRegex.Matches(payload);
        if (matches.Count == 0) return results;

        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            if (!int.TryParse(match.Groups[1].Value, out var chapter)) continue;

            var colonIndex = payload.IndexOf(':', match.Groups[1].Index);
            if (colonIndex < 0) continue;

            var contentStart = colonIndex + 1;
            var contentEnd = (i + 1 < matches.Count) ? matches[i + 1].Index : payload.Length;

            if (contentStart > contentEnd) continue;

            var rawVerseText = payload.Substring(contentStart, contentEnd - contentStart).Trim();
            rawVerseText = rawVerseText.TrimEnd(',', ';', ' ');

            var ranges = ParseVerseRanges(rawVerseText);
            if (ranges.Count > 0)
            {
                results.Add((chapter, ranges));
            }
        }

        return results;
    }

    public static List<(int Start, int End)> ParseVerseRanges(string verseStr)
    {
        var ranges = new List<(int Start, int End)>();
        if (string.IsNullOrWhiteSpace(verseStr)) return ranges;

        // Split by comma or semicolon to preserve explicit list boundaries
        var segments = verseStr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var rawSegment in segments)
        {
            var segment = rawSegment.Trim();
            if (string.IsNullOrEmpty(segment)) continue;

            // In each segment, find all range/verse matches (handles space-separated ranges within segment too)
            var matches = RangeRegex.Matches(segment);
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
        }

        return ranges;
    }

    public async Task<List<ScripturePassageBlock>> ParseAndExpandAsync(string rawContent)
    {
        var blocks = new List<ScripturePassageBlock>();
        if (string.IsNullOrWhiteSpace(rawContent)) return blocks;

        // 1. Pre-cleaning & Boundary Injection
        var preCleaned = PreClean(rawContent);

        // 2. Separate concatenated references onto separate lines if applicable
        var separated = ConcatenatedReferenceSplitRegex.Replace(preCleaned, "\n");

        var lines = separated.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            var refCandidate = StripLeadingItemNumber(trimmed);
            var match = ReferenceRegex.Match(refCandidate);
            if (match.Success)
            {
                var rawBookName = match.Groups[1].Value.Trim();
                var resolvedCanon = ResolveCanonicalBookName(rawBookName);
                var book = await _bibleDb.GetBookByNameAsync(resolvedCanon) 
                           ?? await _bibleDb.GetBookByNameAsync(rawBookName);
                var bookName = book?.LongName ?? resolvedCanon;

                var chapterSegments = ParseChapterSegments(match.Groups[2].Value);
                if (chapterSegments.Count > 0)
                {
                    bool anyAdded = false;
                    foreach (var (chapter, ranges) in chapterSegments)
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
                            var canonBook = book?.LongName ?? combinedVerses[0].BookName ?? bookName;
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
                            anyAdded = true;
                        }
                    }

                    if (anyAdded)
                    {
                        continue;
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

        var cleaned = PreClean(input);
        var refCandidate = StripLeadingItemNumber(cleaned);
        var match = ReferenceRegex.Match(refCandidate);
        if (!match.Success) return false;

        var rawBook = match.Groups[1].Value.Trim();
        book = ResolveCanonicalBookName(rawBook);

        var chapterSegments = ParseChapterSegments(match.Groups[2].Value);
        if (chapterSegments.Count == 0) return false;

        var firstSegment = chapterSegments[0];
        chapter = firstSegment.Chapter;
        startVerse = firstSegment.Ranges[0].Start;
        endVerse = firstSegment.Ranges[^1].End;
        return true;
    }

    public bool TryParseReference(string input, out string book, out int chapter, out List<(int Start, int End)> ranges)
    {
        book = string.Empty;
        chapter = 0;
        ranges = new List<(int Start, int End)>();

        if (string.IsNullOrWhiteSpace(input)) return false;

        var cleaned = PreClean(input);
        var refCandidate = StripLeadingItemNumber(cleaned);
        var match = ReferenceRegex.Match(refCandidate);
        if (!match.Success) return false;

        var rawBook = match.Groups[1].Value.Trim();
        book = ResolveCanonicalBookName(rawBook);

        var chapterSegments = ParseChapterSegments(match.Groups[2].Value);
        if (chapterSegments.Count == 0) return false;

        var firstSegment = chapterSegments[0];
        chapter = firstSegment.Chapter;
        ranges = firstSegment.Ranges;
        return true;
    }

    public bool TryParseReference(string input, out string book, out List<(int Chapter, List<(int Start, int End)> Ranges)> chapters)
    {
        book = string.Empty;
        chapters = new List<(int Chapter, List<(int Start, int End)> Ranges)>();

        if (string.IsNullOrWhiteSpace(input)) return false;

        var cleaned = PreClean(input);
        var refCandidate = StripLeadingItemNumber(cleaned);
        var match = ReferenceRegex.Match(refCandidate);
        if (!match.Success) return false;

        var rawBook = match.Groups[1].Value.Trim();
        book = ResolveCanonicalBookName(rawBook);

        chapters = ParseChapterSegments(match.Groups[2].Value);
        return chapters.Count > 0;
    }
}
