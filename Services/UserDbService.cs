using Microsoft.Data.Sqlite;
using LumenScriptura.Models;

namespace LumenScriptura.Services;

public class UserDbService : IUserDbService
{
    private readonly string _connectionString;
    private bool _initialized;

    public UserDbService()
    {
        var dbPath = ResolveUserDbPath();
        _connectionString = $"Data Source={dbPath};";
    }

    private static string ResolveUserDbPath()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(FileSystem.AppDataDirectory))
            {
                var dir = FileSystem.AppDataDirectory;
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                return Path.Combine(dir, "user_data.db");
            }
        }
        catch
        {
            // Ignore if FileSystem.AppDataDirectory is unavailable
        }

        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LumenScriptura");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return Path.Combine(dir, "user_data.db");
        }
        catch
        {
            return Path.Combine(AppContext.BaseDirectory, "user_data.db");
        }
    }

    public async Task EnsureInitializedAsync()
    {
        if (_initialized) return;

        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        const string ddl = @"
            CREATE TABLE IF NOT EXISTS Highlights (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                VerseRef TEXT NOT NULL,
                Text TEXT NOT NULL,
                Color TEXT NOT NULL,
                CreatedAt DATETIME NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Bookmarks (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Book TEXT NOT NULL,
                Chapter INT NOT NULL,
                Verse INT NOT NULL,
                CreatedAt DATETIME NOT NULL,
                UNIQUE(Book, Chapter, Verse)
            );

            CREATE TABLE IF NOT EXISTS Notes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                RawContent TEXT NOT NULL,
                CreatedAt DATETIME NOT NULL,
                UpdatedAt DATETIME NOT NULL
            );

            CREATE TABLE IF NOT EXISTS History (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Book TEXT NOT NULL,
                Chapter INT NOT NULL,
                Timestamp DATETIME NOT NULL
            );";

        using var cmd = new SqliteCommand(ddl, conn);
        await cmd.ExecuteNonQueryAsync();
        _initialized = true;
    }

    // ================= HIGHLIGHTS =================
    public async Task<List<Highlight>> GetHighlightsAsync()
    {
        await EnsureInitializedAsync();
        var list = new List<Highlight>();

        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        const string sql = "SELECT Id, VerseRef, Text, Color, CreatedAt FROM Highlights ORDER BY CreatedAt DESC";
        using var cmd = new SqliteCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(new Highlight
            {
                Id = reader.GetInt32(0),
                VerseRef = reader.GetString(1),
                Text = reader.GetString(2),
                Color = reader.GetString(3),
                CreatedAt = DateTime.TryParse(reader.GetString(4), out var dt) ? dt : DateTime.UtcNow
            });
        }

        return list;
    }

    public async Task<Dictionary<int, string>> GetChapterHighlightsAsync(string book, int chapter)
    {
        await EnsureInitializedAsync();
        var prefix = $"{book} {chapter}:";
        var dict = new Dictionary<int, string>();

        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        const string sql = "SELECT VerseRef, Color FROM Highlights WHERE VerseRef LIKE @pattern";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@pattern", $"{prefix}%");

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var vRef = reader.GetString(0);
            var color = reader.GetString(1);
            var colonIdx = vRef.LastIndexOf(':');
            if (colonIdx >= 0 && int.TryParse(vRef[(colonIdx + 1)..], out var vNum))
            {
                dict[vNum] = color;
            }
        }

        return dict;
    }

    public async Task SaveHighlightAsync(string verseRef, string text, string color)
    {
        await EnsureInitializedAsync();
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        // Delete any existing highlight for this reference first
        const string delSql = "DELETE FROM Highlights WHERE VerseRef = @verseRef";
        using var delCmd = new SqliteCommand(delSql, conn);
        delCmd.Parameters.AddWithValue("@verseRef", verseRef);
        await delCmd.ExecuteNonQueryAsync();

        // Insert new highlight
        const string insSql = @"
            INSERT INTO Highlights (VerseRef, Text, Color, CreatedAt)
            VALUES (@verseRef, @text, @color, @createdAt)";
        using var insCmd = new SqliteCommand(insSql, conn);
        insCmd.Parameters.AddWithValue("@verseRef", verseRef);
        insCmd.Parameters.AddWithValue("@text", text);
        insCmd.Parameters.AddWithValue("@color", color);
        insCmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("o"));
        await insCmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteHighlightAsync(int id)
    {
        await EnsureInitializedAsync();
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        const string sql = "DELETE FROM Highlights WHERE Id = @id";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteHighlightByReferenceAsync(string verseRef)
    {
        await EnsureInitializedAsync();
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        const string sql = "DELETE FROM Highlights WHERE VerseRef = @verseRef";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@verseRef", verseRef);
        await cmd.ExecuteNonQueryAsync();
    }

    // ================= BOOKMARKS =================
    public async Task<List<Bookmark>> GetBookmarksAsync()
    {
        await EnsureInitializedAsync();
        var list = new List<Bookmark>();

        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        const string sql = "SELECT Id, Book, Chapter, Verse, CreatedAt FROM Bookmarks ORDER BY CreatedAt DESC";
        using var cmd = new SqliteCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(new Bookmark
            {
                Id = reader.GetInt32(0),
                Book = reader.GetString(1),
                Chapter = reader.GetInt32(2),
                Verse = reader.GetInt32(3),
                CreatedAt = DateTime.TryParse(reader.GetString(4), out var dt) ? dt : DateTime.UtcNow
            });
        }

        return list;
    }

    public async Task<HashSet<int>> GetChapterBookmarksAsync(string book, int chapter)
    {
        await EnsureInitializedAsync();
        var set = new HashSet<int>();

        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        const string sql = "SELECT Verse FROM Bookmarks WHERE Book = @book AND Chapter = @chapter";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@book", book);
        cmd.Parameters.AddWithValue("@chapter", chapter);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            set.Add(reader.GetInt32(0));
        }

        return set;
    }

    public async Task<bool> ToggleBookmarkAsync(string book, int chapter, int verse)
    {
        await EnsureInitializedAsync();
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        const string checkSql = "SELECT Id FROM Bookmarks WHERE Book = @book AND Chapter = @chapter AND Verse = @verse";
        using var checkCmd = new SqliteCommand(checkSql, conn);
        checkCmd.Parameters.AddWithValue("@book", book);
        checkCmd.Parameters.AddWithValue("@chapter", chapter);
        checkCmd.Parameters.AddWithValue("@verse", verse);

        var existingId = await checkCmd.ExecuteScalarAsync();
        if (existingId != null && existingId != DBNull.Value)
        {
            const string delSql = "DELETE FROM Bookmarks WHERE Id = @id";
            using var delCmd = new SqliteCommand(delSql, conn);
            delCmd.Parameters.AddWithValue("@id", existingId);
            await delCmd.ExecuteNonQueryAsync();
            return false; // Removed
        }
        else
        {
            const string insSql = "INSERT INTO Bookmarks (Book, Chapter, Verse, CreatedAt) VALUES (@book, @chapter, @verse, @createdAt)";
            using var insCmd = new SqliteCommand(insSql, conn);
            insCmd.Parameters.AddWithValue("@book", book);
            insCmd.Parameters.AddWithValue("@chapter", chapter);
            insCmd.Parameters.AddWithValue("@verse", verse);
            insCmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("o"));
            await insCmd.ExecuteNonQueryAsync();
            return true; // Added
        }
    }

    public async Task DeleteBookmarkAsync(int id)
    {
        await EnsureInitializedAsync();
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        const string sql = "DELETE FROM Bookmarks WHERE Id = @id";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    // ================= NOTES =================
    public async Task<List<Note>> GetNotesAsync()
    {
        await EnsureInitializedAsync();
        var list = new List<Note>();

        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        const string sql = "SELECT Id, Title, RawContent, CreatedAt, UpdatedAt FROM Notes ORDER BY UpdatedAt DESC";
        using var cmd = new SqliteCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(new Note
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                RawContent = reader.GetString(2),
                CreatedAt = DateTime.TryParse(reader.GetString(3), out var cdt) ? cdt : DateTime.UtcNow,
                UpdatedAt = DateTime.TryParse(reader.GetString(4), out var udt) ? udt : DateTime.UtcNow
            });
        }

        return list;
    }

    public async Task<Note> SaveNoteAsync(Note note)
    {
        await EnsureInitializedAsync();
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        note.UpdatedAt = DateTime.UtcNow;

        if (note.Id == 0)
        {
            note.CreatedAt = DateTime.UtcNow;
            const string sql = @"
                INSERT INTO Notes (Title, RawContent, CreatedAt, UpdatedAt)
                VALUES (@title, @content, @createdAt, @updatedAt);
                SELECT last_insert_rowid();";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@title", note.Title);
            cmd.Parameters.AddWithValue("@content", note.RawContent);
            cmd.Parameters.AddWithValue("@createdAt", note.CreatedAt.ToString("o"));
            cmd.Parameters.AddWithValue("@updatedAt", note.UpdatedAt.ToString("o"));

            var newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            note.Id = newId;
        }
        else
        {
            const string sql = @"
                UPDATE Notes 
                SET Title = @title, RawContent = @content, UpdatedAt = @updatedAt 
                WHERE Id = @id";

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", note.Id);
            cmd.Parameters.AddWithValue("@title", note.Title);
            cmd.Parameters.AddWithValue("@content", note.RawContent);
            cmd.Parameters.AddWithValue("@updatedAt", note.UpdatedAt.ToString("o"));
            await cmd.ExecuteNonQueryAsync();
        }

        return note;
    }

    public async Task DeleteNoteAsync(int id)
    {
        await EnsureInitializedAsync();
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        const string sql = "DELETE FROM Notes WHERE Id = @id";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    // ================= HISTORY =================
    public async Task<List<HistoryItem>> GetHistoryAsync(int limit = 30)
    {
        await EnsureInitializedAsync();
        var list = new List<HistoryItem>();

        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        const string sql = "SELECT Id, Book, Chapter, Timestamp FROM History ORDER BY Timestamp DESC LIMIT @limit";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@limit", limit);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new HistoryItem
            {
                Id = reader.GetInt32(0),
                Book = reader.GetString(1),
                Chapter = reader.GetInt32(2),
                Timestamp = DateTime.TryParse(reader.GetString(3), out var dt) ? dt : DateTime.UtcNow
            });
        }

        return list;
    }

    public async Task AddHistoryAsync(string book, int chapter)
    {
        await EnsureInitializedAsync();
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        // Delete recent duplicate to prevent spamming
        const string delRecent = "DELETE FROM History WHERE Book = @book AND Chapter = @chapter";
        using var delCmd = new SqliteCommand(delRecent, conn);
        delCmd.Parameters.AddWithValue("@book", book);
        delCmd.Parameters.AddWithValue("@chapter", chapter);
        await delCmd.ExecuteNonQueryAsync();

        const string sql = "INSERT INTO History (Book, Chapter, Timestamp) VALUES (@book, @chapter, @timestamp)";
        using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue("@book", book);
        cmd.Parameters.AddWithValue("@chapter", chapter);
        cmd.Parameters.AddWithValue("@timestamp", DateTime.UtcNow.ToString("o"));
        await cmd.ExecuteNonQueryAsync();
    }
}
