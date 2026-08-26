using LumenScriptura.Models;

namespace LumenScriptura.Services;

public interface IUserDbService
{
    Task EnsureInitializedAsync();

    // Highlights
    Task<List<Highlight>> GetHighlightsAsync();
    Task<Dictionary<int, string>> GetChapterHighlightsAsync(string book, int chapter);
    Task SaveHighlightAsync(string verseRef, string text, string color);
    Task DeleteHighlightAsync(int id);
    Task DeleteHighlightByReferenceAsync(string verseRef);

    // Bookmarks
    Task<List<Bookmark>> GetBookmarksAsync();
    Task<HashSet<int>> GetChapterBookmarksAsync(string book, int chapter);
    Task<bool> ToggleBookmarkAsync(string book, int chapter, int verse);
    Task DeleteBookmarkAsync(int id);

    // Notes
    Task<List<Note>> GetNotesAsync();
    Task<Note> SaveNoteAsync(Note note);
    Task DeleteNoteAsync(int id);

    // History
    Task<List<HistoryItem>> GetHistoryAsync(int limit = 30);
    Task AddHistoryAsync(string book, int chapter);
}
