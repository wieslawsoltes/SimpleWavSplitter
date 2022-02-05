using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SimpleWavSplitter;

/// <summary>
/// Main window.
/// </summary>
public class MainWindow : Window
{
    /// <summary>
    /// Initializes the new instance of <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        this.InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        Title = $"SimpleWavSplitter v{version?.Major}.{version?.Minor}.{version?.Build}";
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
