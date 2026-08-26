using System.Net.Http.Json;
using System.Text.Json.Serialization;
using LumenScriptura.Models;
using LumenScriptura.Services;

namespace LumenScriptura.Web.Services;

public class WebBibleService : IBibleService
{
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _isInitialized;
    private bool _isLoading;
    private string? _initError;

    private List<Book> _books = new();
    private readonly Dictionary<string, Book> _booksByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Book> _booksByNormalizedKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, Book> _booksByNumber = new();
    private readonly Dictionary<(int BookNumber, int Chapter), List<Verse>> _chapterLookup = new();
    private List<Verse> _allVerses = new();

    public bool IsInitialized => _isInitialized;
    public bool IsLoading => _isLoading;
    public string? InitializationError => _initError;

    public WebBibleService(HttpClient http)
    {
        _http = http;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_isInitialized) return;

            _isLoading = true;
            _initError = null;

            BiblePayload? payload;
            try
            {
                payload = await _http.GetFromJsonAsync<BiblePayload>("data/kjv.json");
            }
            catch (Exception ex)
            {
                _initError = $"Failed to load scripture data from server: {ex.Message}";
                throw new InvalidOperationException(_initError, ex);
            }

            if (payload == null || payload.Books == null || payload.Verses == null)
            {
                _initError = "Scripture data payload is empty or invalid.";
                throw new InvalidOperationException(_initError);
            }

            _books = payload.Books;
            _booksByName.Clear();
            _booksByNormalizedKey.Clear();
            _booksByNumber.Clear();
            foreach (var b in _books)
            {
                _booksByName[b.LongName] = b;
                _booksByName[b.ShortName] = b;
                _booksByNumber[b.BookNumber] = b;

                var normLong = BibleBookAliases.NormalizeKey(b.LongName);
                var normShort = BibleBookAliases.NormalizeKey(b.ShortName);
                if (!string.IsNullOrEmpty(normLong)) _booksByNormalizedKey[normLong] = b;
                if (!string.IsNullOrEmpty(normShort)) _booksByNormalizedKey[normShort] = b;
            }

            _allVerses = payload.Verses;
            _chapterLookup.Clear();
            foreach (var v in _allVerses)
            {
                var key = (v.BookNumber, v.Chapter);
                if (!_chapterLookup.TryGetValue(key, out var list))
                {
                    list = new List<Verse>();
                    _chapterLookup[key] = list;
                }
                list.Add(v);
            }

            _isInitialized = true;
        }
        finally
        {
            _isLoading = false;
            _initLock.Release();
        }
    }

    private async Task EnsureInitializedAsync()
    {
        if (!_isInitialized)
        {
            await InitializeAsync();
        }
    }

    public async Task<List<Book>> GetBooksAsync()
    {
        await EnsureInitializedAsync();
        return _books;
    }

    public async Task<Book?> GetBookByNameAsync(string name)
    {
        await EnsureInitializedAsync();
        if (string.IsNullOrWhiteSpace(name)) return null;

        var trimmed = name.Trim();

        // 1. Direct name match
        if (_booksByName.TryGetValue(trimmed, out var book))
        {
            return book;
        }

        // 2. Common alias lookup (e.g., PS -> Psalms, REV -> Revelation, 1 KGS -> 1 Kings)
        if (BibleBookAliases.TryGetCanonicalName(trimmed, out var canonical) &&
            _booksByName.TryGetValue(canonical, out book))
        {
            return book;
        }

        // 3. Normalized key lookup (stripping spaces, periods, and punctuation)
        var normKey = BibleBookAliases.NormalizeKey(trimmed);
        if (_booksByNormalizedKey.TryGetValue(normKey, out book))
        {
            return book;
        }

        return null;
    }

    public async Task<List<Verse>> GetChapterVersesAsync(string bookName, int chapter)
    {
        await EnsureInitializedAsync();
        var book = await GetBookByNameAsync(bookName);
        if (book == null) return new List<Verse>();

        if (_chapterLookup.TryGetValue((book.BookNumber, chapter), out var verses))
        {
            return verses.Select(v => new Verse
            {
                BookNumber = v.BookNumber,
                BookName = v.BookName,
                Chapter = v.Chapter,
                VerseNum = v.VerseNum,
                Text = v.Text
            }).ToList();
        }

        return new List<Verse>();
    }

    public async Task<Verse?> GetVerseAsync(string bookName, int chapter, int verseNum)
    {
        await EnsureInitializedAsync();
        var book = await GetBookByNameAsync(bookName);
        if (book == null) return null;

        if (_chapterLookup.TryGetValue((book.BookNumber, chapter), out var verses))
        {
            var found = verses.FirstOrDefault(v => v.VerseNum == verseNum);
            if (found != null)
            {
                return new Verse
                {
                    BookNumber = found.BookNumber,
                    BookName = found.BookName,
                    Chapter = found.Chapter,
                    VerseNum = found.VerseNum,
                    Text = found.Text
                };
            }
        }

        return null;
    }

    public async Task<List<Verse>> GetVerseRangeAsync(string bookName, int chapter, int startVerse, int endVerse)
    {
        await EnsureInitializedAsync();
        var book = await GetBookByNameAsync(bookName);
        if (book == null) return new List<Verse>();

        if (startVerse > endVerse)
        {
            (startVerse, endVerse) = (endVerse, startVerse);
        }

        if (_chapterLookup.TryGetValue((book.BookNumber, chapter), out var verses))
        {
            return verses
                .Where(v => v.VerseNum >= startVerse && v.VerseNum <= endVerse)
                .Select(v => new Verse
                {
                    BookNumber = v.BookNumber,
                    BookName = v.BookName,
                    Chapter = v.Chapter,
                    VerseNum = v.VerseNum,
                    Text = v.Text
                })
                .ToList();
        }

        return new List<Verse>();
    }

    public async Task<List<Verse>> SearchVersesAsync(string query, int limit = 60)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<Verse>();

        await EnsureInitializedAsync();

        var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0) return new List<Verse>();

        var results = new List<Verse>();
        foreach (var v in _allVerses)
        {
            var matchesAll = true;
            for (int i = 0; i < words.Length; i++)
            {
                if (!v.Text.Contains(words[i], StringComparison.OrdinalIgnoreCase))
                {
                    matchesAll = false;
                    break;
                }
            }

            if (matchesAll)
            {
                results.Add(new Verse
                {
                    BookNumber = v.BookNumber,
                    BookName = v.BookName,
                    Chapter = v.Chapter,
                    VerseNum = v.VerseNum,
                    Text = v.Text
                });

                if (results.Count >= limit) break;
            }
        }

        return results;
    }

    private class BiblePayload
    {
        [JsonPropertyName("books")]
        public List<Book> Books { get; set; } = new();

        [JsonPropertyName("verses")]
        public List<Verse> Verses { get; set; } = new();
    }
}
