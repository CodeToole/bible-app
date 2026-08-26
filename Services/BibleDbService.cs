using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using LumenScriptura.Models;

namespace LumenScriptura.Services;

public class BibleDbService : IBibleService
{
    private readonly string _connectionString;
    private static readonly Regex StrongAndNoteRegex = new(@"<(?:S|n)>[^<]*</(?:S|n)>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex FormattingTagRegex = new(@"<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex MultiSpaceRegex = new(@"\s{2,}", RegexOptions.Compiled);
    private static readonly Regex PunctuationSpaceRegex = new(@"\s+([,.;:!?])", RegexOptions.Compiled);

    private List<Book>? _cachedBooks;
    private bool _isInitialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public BibleDbService()
    {
        var dbPath = ResolveDatabasePath();
        _connectionString = $"Data Source={dbPath};Mode=ReadOnly;Cache=Shared;";
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_isInitialized) return;

            try
            {
                SQLitePCL.Batteries_V2.Init();
            }
            catch
            {
                // Ignore if already initialized or not needed
            }

            await Task.Run(async () =>
            {
                await GetBooksAsync();
            });

            _isInitialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public static string CleanScriptureText(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText)) return string.Empty;
        
        // 1. Strip Strong's tags <S>...</S> and footnote notes <n>...</n> including their contents
        var cleaned = StrongAndNoteRegex.Replace(rawText, "");
        // 2. Strip formatting tag markers (<J>, </J>, <i>, </i>, etc.) preserving inner text
        cleaned = FormattingTagRegex.Replace(cleaned, "");
        // 3. Remove spaces before punctuation caused by removed tags
        cleaned = PunctuationSpaceRegex.Replace(cleaned, "$1");
        // 4. Normalize whitespace
        cleaned = MultiSpaceRegex.Replace(cleaned, " ").Trim();
        return cleaned;
    }

    private static string ResolveDatabasePath()
    {
        var candidatePaths = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "kjv.sqlite"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "kjv.sqlite"),
            Path.Combine(AppContext.BaseDirectory, "wwwroot", "kjv.sqlite"),
            Path.Combine(AppContext.BaseDirectory, "kjv.sqlite"),
            Path.Combine(Environment.CurrentDirectory, "wwwroot", "kjv.sqlite"),
            Path.Combine(Environment.CurrentDirectory, "kjv.sqlite"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LumenScriptura", "kjv.sqlite"),
            @"C:\Users\CorneliusToole\Downloads\bible app\wwwroot\kjv.sqlite"
        };

        foreach (var path in candidatePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "kjv.sqlite");
    }

    public async Task<List<Book>> GetBooksAsync()
    {
        if (_cachedBooks != null) return _cachedBooks;

        var books = new List<Book>();
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        // Query books along with maximum chapter from verses
        const string sql = @"
            SELECT b.book_number, b.short_name, b.long_name, IFNULL(MAX(v.chapter), 1) as max_chapter
            FROM books b
            LEFT JOIN verses v ON b.book_number = v.book_number
            GROUP BY b.book_number, b.short_name, b.long_name
            ORDER BY b.book_number";

        using var cmd = new SqliteCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            books.Add(new Book
            {
                BookNumber = reader.GetInt32(0),
                ShortName = reader.GetString(1),
                LongName = reader.GetString(2),
                TotalChapters = reader.GetInt32(3)
            });
        }

        _cachedBooks = books;
        return books;
    }

    public async Task<Book?> GetBookByNameAsync(string name)
    {
        var books = await GetBooksAsync();
        return books.FirstOrDefault(b =>
            b.LongName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            b.ShortName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<Verse>> GetChapterVersesAsync(string bookName, int chapter)
    {
        var book = await GetBookByNameAsync(bookName);
        if (book == null) return new List<Verse>();

        var verses = new List<Verse>();
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        const string sql = @"
            SELECT verse, text 
            FROM verses 
            WHERE book_number = @bookNum AND chapter = @chapter 
            ORDER BY verse";

        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@bookNum", book.BookNumber);
        cmd.Parameters.AddWithValue("@chapter", chapter);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var verseNum = reader.GetInt32(0);
            var rawText = reader.GetString(1);
            verses.Add(new Verse
            {
                BookNumber = book.BookNumber,
                BookName = book.LongName,
                Chapter = chapter,
                VerseNum = verseNum,
                Text = CleanScriptureText(rawText)
            });
        }

        return verses;
    }

    public async Task<Verse?> GetVerseAsync(string bookName, int chapter, int verseNum)
    {
        var book = await GetBookByNameAsync(bookName);
        if (book == null) return null;

        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        const string sql = @"
            SELECT text 
            FROM verses 
            WHERE book_number = @bookNum AND chapter = @chapter AND verse = @verse
            LIMIT 1";

        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@bookNum", book.BookNumber);
        cmd.Parameters.AddWithValue("@chapter", chapter);
        cmd.Parameters.AddWithValue("@verse", verseNum);

        var result = await cmd.ExecuteScalarAsync();
        if (result == null || result is DBNull) return null;

        return new Verse
        {
            BookNumber = book.BookNumber,
            BookName = book.LongName,
            Chapter = chapter,
            VerseNum = verseNum,
            Text = CleanScriptureText(result.ToString() ?? "")
        };
    }

    public async Task<List<Verse>> GetVerseRangeAsync(string bookName, int chapter, int startVerse, int endVerse)
    {
        var book = await GetBookByNameAsync(bookName);
        if (book == null) return new List<Verse>();

        var verses = new List<Verse>();
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        const string sql = @"
            SELECT verse, text 
            FROM verses 
            WHERE book_number = @bookNum AND chapter = @chapter AND verse >= @startVerse AND verse <= @endVerse
            ORDER BY verse";

        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@bookNum", book.BookNumber);
        cmd.Parameters.AddWithValue("@chapter", chapter);
        cmd.Parameters.AddWithValue("@startVerse", startVerse);
        cmd.Parameters.AddWithValue("@endVerse", endVerse);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            verses.Add(new Verse
            {
                BookNumber = book.BookNumber,
                BookName = book.LongName,
                Chapter = chapter,
                VerseNum = reader.GetInt32(0),
                Text = CleanScriptureText(reader.GetString(1))
            });
        }

        return verses;
    }

    public async Task<List<Verse>> SearchVersesAsync(string query, int limit = 60)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<Verse>();

        var books = await GetBooksAsync();
        var bookLookup = books.ToDictionary(b => b.BookNumber, b => b.LongName);

        var results = new List<Verse>();
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0) return results;

        var whereClauses = new List<string>();
        for (int i = 0; i < words.Length; i++)
        {
            whereClauses.Add($"text LIKE @w{i}");
        }

        var sql = $@"
            SELECT book_number, chapter, verse, text 
            FROM verses 
            WHERE {string.Join(" AND ", whereClauses)} 
            LIMIT @limit";

        using var cmd = new SqliteCommand(sql, conn);
        for (int i = 0; i < words.Length; i++)
        {
            cmd.Parameters.AddWithValue($"@w{i}", $"%{words[i]}%");
        }
        cmd.Parameters.AddWithValue("@limit", limit);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var bookNum = reader.GetInt32(0);
            var chapter = reader.GetInt32(1);
            var verseNum = reader.GetInt32(2);
            var rawText = reader.GetString(3);
            var cleanText = CleanScriptureText(rawText);

            // Double check all search words exist in cleaned text
            var matchesAll = words.All(w => cleanText.Contains(w, StringComparison.OrdinalIgnoreCase));
            if (matchesAll)
            {
                results.Add(new Verse
                {
                    BookNumber = bookNum,
                    BookName = bookLookup.TryGetValue(bookNum, out var name) ? name : $"Book {bookNum}",
                    Chapter = chapter,
                    VerseNum = verseNum,
                    Text = cleanText
                });
            }
        }

        return results;
    }
}
