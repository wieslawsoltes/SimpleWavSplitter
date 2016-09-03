// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WavFile;

namespace SimpleWavSplitter
{
    /// <summary>
    /// Main window.
    /// </summary>
    public partial class MainWindow : Window
    {
        private Task _task;
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
            btnSplitWavFiles.Click += (sender, e) => SplitWavFiles();
            btnCancel.Click += (sender, e) => CancelSplitWorker();
        }

        private void GetOutputPath()
        {
            System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();
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
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "WAV Files (*.wav)|*.wav|All Files (*.*)|*.*";
            dlg.FilterIndex = 0;
            dlg.Multiselect = true;

            if (dlg.ShowDialog() == true)
            {
                string[] fileNames = dlg.FileNames;
                var sb = new System.Text.StringBuilder();
                int totalFiles = 0;
                foreach (string fileName in fileNames)
                {
                    try
                    {
                        using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                        {
                            var h = WavFileInfo.ReadFileHeader(fs);
                            string text = string.Format("FileName:\t\t{0}\nFileSize:\t\t{1}\n{2}", Path.GetFileName(fileName), fs.Length.ToString(), h.ToString());
                            if (totalFiles > 0)
                            {
                                sb.Append("\n\n");
                            }
                            sb.Append(text);
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

        private void SplitWavFiles()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "WAV Files (*.wav)|*.wav|All Files (*.*)|*.*";
            dlg.FilterIndex = 0;
            dlg.Multiselect = true;

            if (dlg.ShowDialog() == true)
            {
                SplitWavFiles(dlg.FileNames);
            }
        }

        private void SplitWavFiles(string[] fileNames)
        {
            progress.Value = 0;
            _tokenSource = new CancellationTokenSource();
            CancellationToken ct = _tokenSource.Token;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var sb = new System.Text.StringBuilder();

            sb.Append(string.Format("Files to split: {0}\n", fileNames.Count()));
            textOutput.Text = sb.ToString();

            string userOutputPath = textOutputPath.Text;

            if (userOutputPath.EndsWith("\\") == false && userOutputPath.Length > 0)
            {
                userOutputPath += "\\";
            }

            _task = Task<long>.Factory.StartNew(() =>
            {
                ct.ThrowIfCancellationRequested();
                long countBytesTotal = 0;
                var splitter = new WavFileSplitter(value => Dispatcher.Invoke(() => progress.Value = value));

                foreach (string fileName in fileNames)
                {
                    try
                    {
                        string outputPath = userOutputPath.Length > 0 ? userOutputPath : fileName.Remove(fileName.Length - Path.GetFileName(fileName).Length);
                        Dispatcher.Invoke(new Action(() =>
                        {
                            string text = string.Format("Split file: {0}\n", Path.GetFileName(fileName));
                            sb.Append(text);
                            textOutput.Text = sb.ToString();
                        }));
                        countBytesTotal += splitter.SplitWavFile(fileName, outputPath, ct);
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            sb.Append(string.Format("Error: {0}\n", ex.Message));
                            textOutput.Text = sb.ToString();
                        });
                        return countBytesTotal;
                    }
                }
                return countBytesTotal;
            })
            .ContinueWith((totalBytesProcessed) =>
            {
                sw.Stop();
                if (_tokenSource.IsCancellationRequested == false)
                {
                    string text = string.Format(
                        "Done.\nData bytes processed: {0} ({1} MB)\nElapsed time: {2}\n",
                        totalBytesProcessed.Result,
                        Math.Round((double)totalBytesProcessed.Result / (1024 * 1024), 1),
                        sw.Elapsed);
                    sb.Append(text);
                    textOutput.Text = sb.ToString();
                }
                progress.Value = 0;
            },
            TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void CancelSplitWorker()
        {
            if (_task != null && _tokenSource != null)
            {
                _tokenSource.Cancel();
            }
            progress.Value = 0;
        }
    }
}
