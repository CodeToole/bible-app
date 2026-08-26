using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;

namespace LumenScriptura.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
	[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
	private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

	/// <summary>
	/// Initializes the singleton application object.  This is the first line of authored code
	/// executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public App()
	{
		AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
		{
			var ex = args.ExceptionObject as Exception;
			ShowFatalError("Unhandled AppDomain Exception", ex);
		};

		this.UnhandledException += (sender, args) =>
		{
			ShowFatalError("Unhandled WinUI Exception", args.Exception);
			args.Handled = true;
		};

		try
		{
			this.InitializeComponent();
		}
		catch (Exception ex)
		{
			ShowFatalError("Failed during InitializeComponent", ex);
			throw;
		}
	}

	protected override MauiApp CreateMauiApp()
	{
		try
		{
			return MauiProgram.CreateMauiApp();
		}
		catch (Exception ex)
		{
			ShowFatalError("Failed during MauiProgram.CreateMauiApp()", ex);
			throw;
		}
	}

	private static void ShowFatalError(string context, Exception? ex)
	{
		try
		{
			var message = $"{context}:\n\n{(ex != null ? ex.ToString() : "Unknown error occurred.")}";
			MessageBox(IntPtr.Zero, message, "Bible Study App - Fatal Launch Error", 0x00000010 /* MB_ICONERROR */);
		}
		catch
		{
			// Fallback
		}
	}
}


