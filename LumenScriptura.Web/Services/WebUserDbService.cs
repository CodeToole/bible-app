using System.Text.Json;
using Microsoft.JSInterop;
using LumenScriptura.Models;
using LumenScriptura.Services;

namespace LumenScriptura.Web.Services;

public class WebUserDbService : IUserDbService
{
    private readonly IJSRuntime _js;
    private bool _isInitialized;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private const string KeyPrefix = "ls_v1_";
    private const string HighlightsKey = KeyPrefix + "highlights";
    private const string BookmarksKey = KeyPrefix + "bookmarks";
    private const string NotesKey = KeyPrefix + "notes";
    private const string HistoryKey = KeyPrefix + "history";

    private List<Highlight> _highlights = new();
    private List<Bookmark> _bookmarks = new();
    private List<Note> _notes = new();
    private List<HistoryItem> _history = new();

    public WebUserDbService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task EnsureInitializedAsync()
    {
        if (_isInitialized) return;

        await _lock.WaitAsync();
        try
        {
            if (_isInitialized) return;

            _highlights = await LoadListAsync<Highlight>(HighlightsKey);
            _bookmarks = await LoadListAsync<Bookmark>(BookmarksKey);
            _notes = await LoadListAsync<Note>(NotesKey);
            _history = await LoadListAsync<HistoryItem>(HistoryKey);

            _isInitialized = true;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<T>> LoadListAsync<T>(string key)
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", key);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var items = JsonSerializer.Deserialize<List<T>>(json);
                if (items != null) return items;
            }
        }
        catch
        {
            // Fallback to in-memory list if localStorage is not accessible
        }
        return new List<T>();
    }

    private async Task SaveListAsync<T>(string key, List<T> items)
    {
        try
        {
            var json = JsonSerializer.Serialize(items);
            await _js.InvokeVoidAsync("localStorage.setItem", key, json);
        }
        catch
        {
            // Best effort persistence to localStorage; remains in-memory
        }
    }

    // ================= HIGHLIGHTS =================
    public async Task<List<Highlight>> GetHighlightsAsync()
    {
        await EnsureInitializedAsync();
        return _highlights.OrderByDescending(h => h.CreatedAt).ToList();
    }

    public async Task<Dictionary<int, string>> GetChapterHighlightsAsync(string book, int chapter)
    {
        await EnsureInitializedAsync();
        var prefix = $"{book} {chapter}:";
        var dict = new Dictionary<int, string>();

        foreach (var h in _highlights)
        {
            if (h.VerseRef.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var colonIdx = h.VerseRef.LastIndexOf(':');
                if (colonIdx >= 0 && int.TryParse(h.VerseRef[(colonIdx + 1)..], out var vNum))
                {
                    dict[vNum] = h.Color;
                }
            }
        }

        return dict;
    }

    public async Task SaveHighlightAsync(string verseRef, string text, string color)
    {
        await EnsureInitializedAsync();

        // Remove existing highlight for this reference first
        _highlights.RemoveAll(h => string.Equals(h.VerseRef, verseRef, StringComparison.OrdinalIgnoreCase));

        var nextId = _highlights.Count > 0 ? _highlights.Max(h => h.Id) + 1 : 1;
        _highlights.Add(new Highlight
        {
            Id = nextId,
            VerseRef = verseRef,
            Text = text,
            Color = color,
            CreatedAt = DateTime.UtcNow
        });

        await SaveListAsync(HighlightsKey, _highlights);
    }

    public async Task DeleteHighlightAsync(int id)
    {
        await EnsureInitializedAsync();
        _highlights.RemoveAll(h => h.Id == id);
        await SaveListAsync(HighlightsKey, _highlights);
    }

    public async Task DeleteHighlightByReferenceAsync(string verseRef)
    {
        await EnsureInitializedAsync();
        _highlights.RemoveAll(h => string.Equals(h.VerseRef, verseRef, StringComparison.OrdinalIgnoreCase));
        await SaveListAsync(HighlightsKey, _highlights);
    }

    // ================= BOOKMARKS =================
    public async Task<List<Bookmark>> GetBookmarksAsync()
    {
        await EnsureInitializedAsync();
        return _bookmarks.OrderByDescending(b => b.CreatedAt).ToList();
    }

    public async Task<HashSet<int>> GetChapterBookmarksAsync(string book, int chapter)
    {
        await EnsureInitializedAsync();
        return _bookmarks
            .Where(b => string.Equals(b.Book, book, StringComparison.OrdinalIgnoreCase) && b.Chapter == chapter)
            .Select(b => b.Verse)
            .ToHashSet();
    }

    public async Task<bool> ToggleBookmarkAsync(string book, int chapter, int verse)
    {
        await EnsureInitializedAsync();
        var existing = _bookmarks.FirstOrDefault(b =>
            string.Equals(b.Book, book, StringComparison.OrdinalIgnoreCase) &&
            b.Chapter == chapter &&
            b.Verse == verse);

        if (existing != null)
        {
            _bookmarks.Remove(existing);
            await SaveListAsync(BookmarksKey, _bookmarks);
            return false;
        }
        else
        {
            var nextId = _bookmarks.Count > 0 ? _bookmarks.Max(b => b.Id) + 1 : 1;
            _bookmarks.Add(new Bookmark
            {
                Id = nextId,
                Book = book,
                Chapter = chapter,
                Verse = verse,
                CreatedAt = DateTime.UtcNow
            });
            await SaveListAsync(BookmarksKey, _bookmarks);
            return true;
        }
    }

    public async Task DeleteBookmarkAsync(int id)
    {
        await EnsureInitializedAsync();
        _bookmarks.RemoveAll(b => b.Id == id);
        await SaveListAsync(BookmarksKey, _bookmarks);
    }

    // ================= NOTES =================
    public async Task<List<Note>> GetNotesAsync()
    {
        await EnsureInitializedAsync();
        return _notes.OrderByDescending(n => n.UpdatedAt).ToList();
    }

    public async Task<Note> SaveNoteAsync(Note note)
    {
        await EnsureInitializedAsync();
        note.UpdatedAt = DateTime.UtcNow;

        if (note.Id == 0)
        {
            note.CreatedAt = DateTime.UtcNow;
            var nextId = _notes.Count > 0 ? _notes.Max(n => n.Id) + 1 : 1;
            note.Id = nextId;
            _notes.Add(note);
        }
        else
        {
            var idx = _notes.FindIndex(n => n.Id == note.Id);
            if (idx >= 0)
            {
                _notes[idx] = note;
            }
            else
            {
                _notes.Add(note);
            }
        }

        await SaveListAsync(NotesKey, _notes);
        return note;
    }

    public async Task DeleteNoteAsync(int id)
    {
        await EnsureInitializedAsync();
        _notes.RemoveAll(n => n.Id == id);
        await SaveListAsync(NotesKey, _notes);
    }

    // ================= HISTORY =================
    public async Task<List<HistoryItem>> GetHistoryAsync(int limit = 30)
    {
        await EnsureInitializedAsync();
        return _history
            .OrderByDescending(h => h.Timestamp)
            .Take(limit)
            .ToList();
    }

    public async Task AddHistoryAsync(string book, int chapter)
    {
        await EnsureInitializedAsync();

        // Remove recent duplicate
        _history.RemoveAll(h => string.Equals(h.Book, book, StringComparison.OrdinalIgnoreCase) && h.Chapter == chapter);

        var nextId = _history.Count > 0 ? _history.Max(h => h.Id) + 1 : 1;
        _history.Add(new HistoryItem
        {
            Id = nextId,
            Book = book,
            Chapter = chapter,
            Timestamp = DateTime.UtcNow
        });

        await SaveListAsync(HistoryKey, _history);
    }
}
