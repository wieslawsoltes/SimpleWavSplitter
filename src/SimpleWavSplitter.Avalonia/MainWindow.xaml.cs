// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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
        private CancellationTokenSource _tokenSource;
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

            btnGetWavHeader = this.FindControl<Button>("btnGetWavHeader");
            progress = this.FindControl<ProgressBar>("progress");
            btnCancel = this.FindControl<Button>("btnCancel");
            btnSplitWavFiles = this.FindControl<Button>("btnSplitWavFiles");
            textOutputPath = this.FindControl<TextBox>("textOutputPath");
            btnBrowseOutputPath = this.FindControl<Button>("btnBrowseOutputPath");
            textOutput = this.FindControl<TextBox>("textOutput");

            var v = Assembly.GetExecutingAssembly().GetName().Version;
            Title = string.Format("SimpleWavSplitter v{0}.{1}.{2}", v.Major, v.Minor, v.Build);

            btnBrowseOutputPath.Click += async (sender, e) => await GetOutputPath();
            btnGetWavHeader.Click += async (sender, e) => await GetWavHeader();
            btnSplitWavFiles.Click += async (sender, e) => await SplitWavFiles();
            btnCancel.Click += async (sender, e) => await CancelSplitWavFiles();
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

            var result = await dlg.ShowAsync();
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

            var result = await dlg.ShowAsync();
            if (result != null)
            {
                string[] fileNames = result;
                var sb = new StringBuilder();
                int totalFiles = 0;
                foreach (string fileName in fileNames)
                {
                    try
                    {
                        using (var fs = File.OpenRead(fileName))
                        {
                            var h = WavFileInfo.ReadFileHeader(fs);
                            if (totalFiles > 0)
                            {
                                sb.Append("\n\n");
                            }
                            sb.Append(
                                string.Format(
                                    "FileName:\t\t{0}\nFileSize:\t\t{1}\n{2}",
                                    Path.GetFileName(fileName), fs.Length.ToString(), h.ToString()));
                            totalFiles++;
                        }
                    }
                    catch (Exception ex)
                    {
                        string text = string.Format("Error: {0}\n", ex.Message);
                        sb.Append(text);
                        textOutput.Text = sb.ToString();
                    }
                }
                textOutput.Text = sb.ToString();
            }
        }

        private async Task SplitWavFiles()
        {
            var dlg = new OpenFileDialog();
            dlg.Filters.Add(new FileDialogFilter() { Name = "WAV Files", Extensions = { "wav" } });
            dlg.Filters.Add(new FileDialogFilter() { Name = "All Files", Extensions = { "*" } });
            dlg.AllowMultiple = true;

            var result = await dlg.ShowAsync();
            if (result != null)
            {
                await SplitWavFiles(result);
            }
        }

        private async Task SplitWavFiles(string[] fileNames)
        {
            progress.Value = 0;
            _tokenSource = new CancellationTokenSource();
            CancellationToken ct = _tokenSource.Token;

            var sw = Stopwatch.StartNew();
            var sb = new StringBuilder();

            sb.Append(string.Format("Files to split: {0}\n", fileNames.Count()));
            textOutput.Text = sb.ToString();

            string userOutputPath = textOutputPath.Text;

            if (userOutputPath.EndsWith("\\") == false && userOutputPath.Length > 0)
            {
                userOutputPath += "\\";
            }

            long totalBytesProcessed = await Task<long>.Factory.StartNew(() =>
            {
                ct.ThrowIfCancellationRequested();
                long countBytesTotal = 0;

                var splitter = new WavFileSplitter(
                    value => Dispatcher.UIThread.InvokeAsync(() =>
                    {
                    progress.Value = value;
                        //Thread.Sleep(100);
                    }));

                foreach (string fileName in fileNames)
                {
                    try
                    {
                        string outputPath = userOutputPath.Length > 0 ?
                            userOutputPath :
                            fileName.Remove(fileName.Length - Path.GetFileName(fileName).Length);

                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            sb.Append(
                                string.Format(
                                    "Split file: {0}\n",
                                    Path.GetFileName(fileName)));
                            textOutput.Text = sb.ToString();
                        });

                        countBytesTotal += splitter.SplitWavFile(fileName, outputPath, ct);
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            sb.Append(string.Format("Error: {0}\n", ex.Message));
                            textOutput.Text = sb.ToString();
                        });
                        return countBytesTotal;
                    }
                }
                return countBytesTotal;
            }, ct);

            sw.Stop();
            if (_tokenSource.IsCancellationRequested == false)
            {
                string text = string.Format(
                    "Done.\nData bytes processed: {0} ({1} MB)\nElapsed time: {2}\n",
                    totalBytesProcessed,
                    Math.Round((double)totalBytesProcessed / (1024 * 1024), 1),
                    sw.Elapsed);
                sb.Append(text);
                textOutput.Text = sb.ToString();
            }
        }

        private async Task CancelSplitWavFiles()
        {
            if (_tokenSource != null)
            {
                await Task.Factory.StartNew(() => _tokenSource.Cancel());
            }
            progress.Value = 0;
        }
    }
}
