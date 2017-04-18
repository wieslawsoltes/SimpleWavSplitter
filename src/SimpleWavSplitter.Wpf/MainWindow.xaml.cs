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
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using WavFile;

namespace SimpleWavSplitter.Wpf
{
    /// <summary>
    /// Main window.
    /// </summary>
    public partial class MainWindow : Window
    {
        private CancellationTokenSource _tokenSource;

        /// <summary>
        /// Initializes the new instance of <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            var v = Assembly.GetExecutingAssembly().GetName().Version;
            Title = string.Format("SimpleWavSplitter v{0}.{1}.{2}", v.Major, v.Minor, v.Build);

            btnBrowseOutputPath.Click += (sender, e) => GetOutputPath();
            btnGetWavHeader.Click += (sender, e) => GetWavHeader();
            btnSplitWavFiles.Click += async (sender, e) => await SplitWavFiles();
            btnCancel.Click += async (sender, e) => await CancelSplitWavFiles();
        }

        private void GetOutputPath()
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            string text = textOutputPath.Text;
            if (text.Length > 0)
            {
                dlg.SelectedPath = textOutputPath.Text;
            }

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textOutputPath.Text = dlg.SelectedPath;
            }
        }

        private void GetWavHeader()
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "WAV Files (*.wav)|*.wav|All Files (*.*)|*.*";
            dlg.FilterIndex = 0;
            dlg.Multiselect = true;

            if (dlg.ShowDialog() == true)
            {
                GetWavHeader(dlg.FileNames, (text) => textOutput.Text = text);
            }
        }

        private async Task SplitWavFiles()
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "WAV Files (*.wav)|*.wav|All Files (*.*)|*.*";
            dlg.FilterIndex = 0;
            dlg.Multiselect = true;

            if (dlg.ShowDialog() == true)
            {
                await SplitWavFiles(
                    dlg.FileNames,
                    (text) => Dispatcher.Invoke(() => textOutput.Text = text));
            }
        }

        private void GetWavHeader(string[] fileNames, Action<string> setOutput)
        {
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
                    setOutput(sb.ToString());
                }
            }
            setOutput(sb.ToString());
        }

        private async Task SplitWavFiles(string[] fileNames, Action<string> setOutput)
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
                    value => Dispatcher.Invoke(() => progress.Value = value));

                foreach (string fileName in fileNames)
                {
                    try
                    {
                        string outputPath = userOutputPath.Length > 0 ?
                            userOutputPath :
                            fileName.Remove(fileName.Length - Path.GetFileName(fileName).Length);

                        sb.Append(
                            string.Format(
                                "Split file: {0}\n",
                                Path.GetFileName(fileName)));

                        setOutput(sb.ToString());

                        countBytesTotal += splitter.SplitWavFile(fileName, outputPath, ct);
                    }
                    catch (Exception ex)
                    {
                        sb.Append(string.Format("Error: {0}\n", ex.Message));

                        setOutput(sb.ToString());

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
                setOutput(sb.ToString());
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
