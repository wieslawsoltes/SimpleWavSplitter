// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics;
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
    /// Main window
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Background worker task.
        /// </summary>
        private Task task;

        /// <summary>
        /// Background worker cancellation token source.
        /// </summary>
        private CancellationTokenSource tokenSource;

        /// <summary>
        /// MainWindow Constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            this.Title = string.Format("SimpleWavSplitter v{0}.{1}.{2}", version.Major, version.Minor, version.Build);
        }

        /// <summary>
        /// Browse for custom output path
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBrowseOutputPath_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();

            string text = this.textOutputPath.Text;

            if (text.Length > 0)
                dlg.SelectedPath = this.textOutputPath.Text;

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.textOutputPath.Text = dlg.SelectedPath;
            }
        }

        /// <summary>
        /// Get WAV file header.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnGetWavHeader_Click(object sender, RoutedEventArgs e)
        {
            GetWavHeader();
        }

        /// <summary>
        /// Split multi-channel WAV files into multiple mono WAV files.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSplitWavFiles_Click(object sender, RoutedEventArgs e)
        {
            SplitWavFiles();
        }

        /// <summary>
        /// Cancel current worker.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            CancelSplitWorker();
        }

        /// <summary>
        /// Get WAV file headers
        /// </summary>
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
                        // create WAV file stream
                        using (System.IO.FileStream f = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                        {
                            // read WAV file header
                            var h = WavFileInfo.ReadFileHeader(f);

                            string text = string.Format("FileName:\t\t{0}\nFileSize:\t\t{1}\n{2}",
                                System.IO.Path.GetFileName(fileName),
                                f.Length.ToString(),
                                h.ToString());

                            if (totalFiles > 0)
                                sb.Append("\n\n");

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

        /// <summary>
        /// Show Open file dialog and split multi-channel WAV files
        /// </summary>
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

        /// <summary>
        /// Split multi-channel WAV files
        /// </summary>
        /// <param name="fileNames">Input file names</param>
        private void SplitWavFiles(string[] fileNames)
        {
            // reset progress
            progress.Value = 0;

            // get cancellation token
            tokenSource = new CancellationTokenSource();
            CancellationToken ct = tokenSource.Token;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var sb = new System.Text.StringBuilder();

            // debug: total files to split
            sb.Append(string.Format("Files to split: {0}\n", fileNames.Count()));
            textOutput.Text = sb.ToString();

            string userOutputPath = this.textOutputPath.Text;

            if (userOutputPath.EndsWith("\\") == false && userOutputPath.Length > 0)
                userOutputPath += "\\";

            // start background task
            task = Task<long>.Factory.StartNew(() =>
            {
                ct.ThrowIfCancellationRequested();

                long countBytesTotal = 0;
                var splitter = new WavFileSplitter(new SplitProgress(this.progress, this.Dispatcher));

                foreach (string fileName in fileNames)
                {
                    try
                    {
                        // set or get file output file path
                        string outputPath =
                            userOutputPath.Length > 0 ? userOutputPath :
                            fileName.Remove(fileName.Length - System.IO.Path.GetFileName(fileName).Length);

                        // debug: split file
                        Dispatcher.Invoke(new Action(() =>
                        {
                            string text = string.Format("Split file: {0}\n",
                                System.IO.Path.GetFileName(fileName));

                            sb.Append(text);

                            textOutput.Text = sb.ToString();
                        }));

                        // split multi-channel WAV file into single channel WAV files
                        countBytesTotal += splitter.SplitWavFile(fileName, outputPath, ct);
                    }
                    catch (Exception ex)
                    {
                        // debug: error
                        Dispatcher.Invoke(new Action(() =>
                        {
                            string text = string.Format("Error: {0}\n", ex.Message);
                            sb.Append(text);

                            textOutput.Text = sb.ToString();
                        }));

                        return countBytesTotal;
                    }
                }

                return countBytesTotal;
            })
            .ContinueWith((totalBytesProcessed) =>
            {
                sw.Stop();

                if (tokenSource.IsCancellationRequested == false)
                {
                    // debug
                    string text = string.Format("Done.\nData bytes processed: {0} ({1} MB)\nElapsed time: {2}\n",
                        totalBytesProcessed.Result,
                        Math.Round((double)totalBytesProcessed.Result / (1024 * 1024), 1),
                        sw.Elapsed);

                    sb.Append(text);

                    textOutput.Text = sb.ToString();
                }
                else
                {
                    progress.Value = 0;
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// Cancel WAV file split jobs
        /// </summary>
        private void CancelSplitWorker()
        {
            if (task != null && tokenSource != null)
            {
                tokenSource.Cancel();
            }

            progress.Value = 0;
        }
    }
}
