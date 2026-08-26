using Microsoft.JSInterop;

namespace LumenScriptura.Services;

public class UserProfile
{
    public string Uid { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(Uid);
}

public class AuthService : IDisposable
{
    private readonly IJSRuntime _js;
    private DotNetObjectReference<AuthService>? _dotNetRef;

    public UserProfile? CurrentUser { get; private set; }
    public bool IsLoading { get; private set; }
    public string? ErrorMessage { get; private set; }
    public bool IsAvailable { get; private set; } = true;

    public event Action? OnChange;

    public AuthService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            var hasAuth = await _js.InvokeAsync<bool>("eval", "typeof window.firebaseAuth !== 'undefined'");
            if (!hasAuth)
            {
                IsAvailable = false;
                return;
            }

            IsAvailable = true;
            await _js.InvokeVoidAsync("firebaseAuth.onAuthStateChanged", _dotNetRef);

            var initialUser = await _js.InvokeAsync<UserProfile?>("firebaseAuth.getCurrentUser");
            if (initialUser != null)
            {
                CurrentUser = initialUser;
                NotifyStateChanged();
            }
        }
        catch
        {
            // Desktop MAUI or environment where eval / window.firebaseAuth is unavailable
            IsAvailable = false;
        }
    }

    [JSInvokable]
    public void OnUserChanged(UserProfile? user)
    {
        CurrentUser = user;
        ErrorMessage = null;
        IsLoading = false;
        NotifyStateChanged();
    }

    public async Task SignInWithGoogleAsync()
    {
        await ExecuteLoginAsync(() => _js.InvokeAsync<UserProfile>("firebaseAuth.loginWithGoogle"));
    }

    public async Task SignInWithMicrosoftAsync()
    {
        await ExecuteLoginAsync(() => _js.InvokeAsync<UserProfile>("firebaseAuth.loginWithMicrosoft"));
    }

    public async Task SignInAnonymouslyAsync()
    {
        await ExecuteLoginAsync(() => _js.InvokeAsync<UserProfile>("firebaseAuth.loginAnonymously"));
    }

    public async Task SignOutAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            NotifyStateChanged();
            await _js.InvokeVoidAsync("firebaseAuth.logout");
            CurrentUser = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    private async Task ExecuteLoginAsync(Func<ValueTask<UserProfile>> loginFunc)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            NotifyStateChanged();

            var user = await loginFunc();
            CurrentUser = user;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    public void Dispose()
    {
        _dotNetRef?.Dispose();
    }
}
