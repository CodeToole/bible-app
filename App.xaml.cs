namespace LumenScriptura;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
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
}
