using LumenScriptura.Models;

namespace LumenScriptura.Services;

public interface IBibleService
{
    Task InitializeAsync();
    Task<List<Book>> GetBooksAsync();
    Task<Book?> GetBookByNameAsync(string name);
    Task<List<Verse>> GetChapterVersesAsync(string bookName, int chapter);
    Task<Verse?> GetVerseAsync(string bookName, int chapter, int verseNum);
    Task<List<Verse>> GetVerseRangeAsync(string bookName, int chapter, int startVerse, int endVerse);
    Task<List<Verse>> SearchVersesAsync(string query, int limit = 60);
}
