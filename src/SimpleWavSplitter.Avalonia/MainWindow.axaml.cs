using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using WavFile;

namespace SimpleWavSplitter.Avalonia
{
    /// <summary>
    /// Main window.
    /// </summary>
    public class MainWindow : Window
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
        /// Initializes the new instance of <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();

            _wavFileSplitter = new SimpleWavFileSplitter();

            _btnGetWavHeader = this.FindControl<Button>("btnGetWavHeader");
            _progress = this.FindControl<ProgressBar>("progress");
            _btnCancel = this.FindControl<Button>("btnCancel");
            _btnSplitWavFiles = this.FindControl<Button>("btnSplitWavFiles");
            _textOutputPath = this.FindControl<TextBox>("textOutputPath");
            _btnBrowseOutputPath = this.FindControl<Button>("btnBrowseOutputPath");
            _textOutput = this.FindControl<TextBox>("textOutput");

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Title = string.Format("SimpleWavSplitter v{0}.{1}.{2}", version?.Major, version?.Minor, version?.Build);

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

            var result = await dlg.ShowAsync(this);
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

            var result = await dlg.ShowAsync(this);
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

            var result = await dlg.ShowAsync(this);
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
}
