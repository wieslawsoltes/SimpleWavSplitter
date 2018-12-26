// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Reflection;
using System.Threading.Tasks;
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
        private Button btnGetWavHeader;
        private ProgressBar progress;
        private Button btnCancel;
        private Button btnSplitWavFiles;
        private TextBox textOutputPath;
        private Button btnBrowseOutputPath;
        private TextBox textOutput;

        /// <summary>
        /// Initializes the new instance of <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();
            App.AttachDevTools(this);

            _wavFileSplitter = new SimpleWavFileSplitter();

            btnGetWavHeader = this.FindControl<Button>("btnGetWavHeader");
            progress = this.FindControl<ProgressBar>("progress");
            btnCancel = this.FindControl<Button>("btnCancel");
            btnSplitWavFiles = this.FindControl<Button>("btnSplitWavFiles");
            textOutputPath = this.FindControl<TextBox>("textOutputPath");
            btnBrowseOutputPath = this.FindControl<Button>("btnBrowseOutputPath");
            textOutput = this.FindControl<TextBox>("textOutput");

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Title = string.Format("SimpleWavSplitter v{0}.{1}.{2}", version.Major, version.Minor, version.Build);

            btnBrowseOutputPath.Click += async (sender, e) => await GetOutputPath();
            btnGetWavHeader.Click += async (sender, e) => await GetWavHeader();
            btnSplitWavFiles.Click += async (sender, e) => await SplitWavFiles();
            btnCancel.Click += async (sender, e) => await _wavFileSplitter.CancelSplitWavFiles(
                value => Dispatcher.UIThread.InvokeAsync(() => progress.Value = value));
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async Task GetOutputPath()
        {
            var dlg = new OpenFolderDialog();

            string text = textOutputPath.Text;
            if (text.Length > 0)
            {
                dlg.InitialDirectory = textOutputPath.Text;
            }

            var result = await dlg.ShowAsync(this);
            if (!string.IsNullOrWhiteSpace(result))
            {
                textOutputPath.Text = result;
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
                _wavFileSplitter.GetWavHeader(result, text => textOutput.Text = text);
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
                    textOutputPath.Text,
                    value => Dispatcher.UIThread.InvokeAsync(() => progress.Value = value),
                    text => Dispatcher.UIThread.InvokeAsync(() => textOutput.Text = text));
            }
        }
    }
}
