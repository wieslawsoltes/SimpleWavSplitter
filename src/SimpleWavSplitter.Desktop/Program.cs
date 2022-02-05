using Avalonia;

namespace SimpleWavSplitter.Desktops;

class Program
{
    /// <summary>
    /// Program entry point.
    /// </summary>
    /// <param name="args">The program arguments.</param>
    static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// Build Avalonia app.
    /// </summary>
    /// <returns>The Avalonia app builder.</returns>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
