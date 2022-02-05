using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Threading.Tasks;
using Avalonia.Threading;
using Avalonia.VisualTree;
using WavFile;

namespace SimpleWavSplitter;

/// <summary>
/// Main view.
/// </summary>
public class MainView : UserControl
{
    private SimpleWavFileSplitter _wavFileSplitter;
    private Button _btnGetWavHeader;
    private ProgressBar _progress;
    private Button _btnCancel;
    private Button _btnSplitWavFiles;
    private TextBox _textOutputPath;
    private Button _btnBrowseOutputPath;
    private TextBox _textOutput;

    /// <summary>
    /// Initializes the new instance of <see cref="MainView"/> class.
    /// </summary>
    public MainView()
    {
        InitializeComponent();

        _wavFileSplitter = new SimpleWavFileSplitter();

        _btnGetWavHeader = this.FindControl<Button>("btnGetWavHeader");
        _progress = this.FindControl<ProgressBar>("progress");
        _btnCancel = this.FindControl<Button>("btnCancel");
        _btnSplitWavFiles = this.FindControl<Button>("btnSplitWavFiles");
        _textOutputPath = this.FindControl<TextBox>("textOutputPath");
        _btnBrowseOutputPath = this.FindControl<Button>("btnBrowseOutputPath");
        _textOutput = this.FindControl<TextBox>("textOutput");

        _btnBrowseOutputPath.Click += async (sender, e) => await GetOutputPath();
        _btnGetWavHeader.Click += async (sender, e) => await GetWavHeader();
        _btnSplitWavFiles.Click += async (sender, e) => await SplitWavFiles();
        _btnCancel.Click += async (sender, e) => await _wavFileSplitter.CancelSplitWavFiles(
            value => Dispatcher.UIThread.InvokeAsync(() => _progress.Value = value));
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async Task GetOutputPath()
    {
        var dlg = new OpenFolderDialog();

        string text = _textOutputPath.Text;
        if (text.Length > 0)
        {
            dlg.Directory = _textOutputPath.Text;
        }

        var window = this.GetVisualRoot() as Window;
        if (window is null)
        {
            return;
        }

        var result = await dlg.ShowAsync(window);
        if (!string.IsNullOrWhiteSpace(result))
        {
            _textOutputPath.Text = result;
        }
    }

    private async Task GetWavHeader()
    {
        var dlg = new OpenFileDialog();
        dlg.Filters.Add(new FileDialogFilter() { Name = "WAV Files", Extensions = { "wav" } });
        dlg.Filters.Add(new FileDialogFilter() { Name = "All Files", Extensions = { "*" } });
        dlg.AllowMultiple = true;

        var window = this.GetVisualRoot() as Window;
        if (window is null)
        {
            return;
        }

        var result = await dlg.ShowAsync(window);
        if (result != null)
        {
            _wavFileSplitter.GetWavHeader(result, text => _textOutput.Text = text);
        }
    }

    private async Task SplitWavFiles()
    {
        var dlg = new OpenFileDialog();
        dlg.Filters.Add(new FileDialogFilter() { Name = "WAV Files", Extensions = { "wav" } });
        dlg.Filters.Add(new FileDialogFilter() { Name = "All Files", Extensions = { "*" } });
        dlg.AllowMultiple = true;

        var window = this.GetVisualRoot() as Window;
        if (window is null)
        {
            return;
        }

        var result = await dlg.ShowAsync(window);
        if (result != null)
        {
            await _wavFileSplitter.SplitWavFiles(
                result,
                _textOutputPath.Text,
                value => Dispatcher.UIThread.InvokeAsync(() => _progress.Value = value),
                text => Dispatcher.UIThread.InvokeAsync(() => _textOutput.Text = text));
        }
    }
}

