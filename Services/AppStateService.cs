namespace LumenScriptura.Services;

public class AppStateService
{
    public string CurrentBook { get; private set; } = "Genesis";
    public int CurrentChapter { get; private set; } = 1;
    public int TotalChapters { get; set; } = 50;
    public int? TargetVerse { get; private set; }
    public string ActiveView { get; private set; } = "reader"; // "reader", "highlights", "notes", "history"
    public bool IsSearchOpen { get; private set; }
    public bool IsSidebarCollapsed { get; private set; }

    public event Action? OnChange;

    public void NavigateTo(string book, int chapter, int? verse = null)
    {
        CurrentBook = book;
        CurrentChapter = Math.Max(1, chapter);
        TargetVerse = verse;
        ActiveView = "reader";
        NotifyStateChanged();
    }

    public void NextChapter()
    {
        if (CurrentChapter < TotalChapters)
        {
            CurrentChapter++;
            TargetVerse = null;
            ActiveView = "reader";
            NotifyStateChanged();
        }
    }

    public void PrevChapter()
    {
        if (CurrentChapter > 1)
        {
            CurrentChapter--;
            TargetVerse = null;
            ActiveView = "reader";
            NotifyStateChanged();
        }
    }

    public void SetView(string view)
    {
        ActiveView = view;
        NotifyStateChanged();
    }

    public void SetSearchOpen(bool isOpen)
    {
        IsSearchOpen = isOpen;
        NotifyStateChanged();
    }

    public void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
        NotifyStateChanged();
    }

    public void ClearTargetVerse()
    {
        TargetVerse = null;
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
