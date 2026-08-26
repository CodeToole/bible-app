namespace LumenScriptura;

public partial class App : Application
{
	public App()
	{
		try
		{
			InitializeComponent();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"App InitializeComponent exception: {ex}");
			throw;
		}
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		try
		{
			var window = new Window(new MainPage())
			{
				Title = "Bible Study App",
				Width = 1280,
				Height = 850,
				MinimumWidth = 900,
				MinimumHeight = 600
			};
			return window;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"CreateWindow exception: {ex}");
			throw;
		}
	}
}
