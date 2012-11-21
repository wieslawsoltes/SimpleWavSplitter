/*
 * SimpleWavSplitter
 * Copyright © Wiesław Šoltés 2010-2012. All Rights Reserved
 */

namespace SimpleWavSplitter
{
    #region References

    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Threading;
    using WavFile;

    #endregion

    #region SimpleWavSplitterWindow

    /// <summary>
    /// Main window
    /// </summary>
    public partial class SimpleWavSplitterWindow : Window
    {
        #region Properties

        /// <summary>
        /// Background worker task.
        /// </summary>
        private Task task;

        /// <summary>
        /// Background worker cancellation token source.
        /// </summary>
        private CancellationTokenSource tokenSource;

        #endregion

        #region Constructor

        /// <summary>
        /// SimpleWavSplitterWindow Constructor
        /// </summary>
        public SimpleWavSplitterWindow()
        {
            InitializeComponent();
            this.Title = "SimpleWavSplitter v0.1.0.0";
        }

        #endregion

        #region Button Events

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

        #endregion

        #region Methods

        private static void GetWavHeader()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "WAV Files (*.wav)|*.wav|All Files (*.*)|*.*";
            dlg.FilterIndex = 0;
            dlg.Multiselect = true;

            if (dlg.ShowDialog() == true)
            {
                string[] fileNames = dlg.FileNames;

                foreach (string fileName in fileNames)
                {
                    try
                    {
                        // create WAV file stream
                        using (System.IO.FileStream f = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                        {
                            // read WAV file header
                            var h = WavFileInfo.ReadFileHeader(f);

                            // show WAV header information
                            var result = MessageBox.Show(
                                "FileName:\t\t" + System.IO.Path.GetFileName(fileName) + "\n" +
                                "FileSize:\t\t" + f.Length.ToString() + "\n\n" +
                                h.ToString(),
                                "WAV Info",
                                MessageBoxButton.OKCancel,
                                MessageBoxImage.None);

                            if (result == MessageBoxResult.Cancel)
                                return;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Show Open file dialog and split nulti-channel WAV files
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
        /// Split nulti-channel WAV files
        /// </summary>
        /// <param name="fileNames">Input file names</param>
        private void SplitWavFiles(string[] fileNames)
        {
            // reset progress
            progress.Value = 0;

            // get cancelletion token
            tokenSource = new CancellationTokenSource();
            CancellationToken ct = tokenSource.Token;

            var sw = System.Diagnostics.Stopwatch.StartNew();

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
                        // get file output file
                        string outputPath = fileName.Remove(fileName.Length - System.IO.Path.GetFileName(fileName).Length);

                        // split multi-channel WAV file into single channel WAV files
                        countBytesTotal += splitter.SplitWavFile(fileName, outputPath, ct);
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    string stats = string.Format("Total data bytes processed: {0} ({1} MB)\nTotal elapsed time: {2}",
                        totalBytesProcessed.Result,
                        Math.Round((double)totalBytesProcessed.Result / (1024 * 1024), 1),
                        sw.Elapsed);

                    // show split stats
                    MessageBox.Show(stats, "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    progress.Value = 0;
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void CancelSplitWorker()
        {
            if (task != null && tokenSource != null)
                tokenSource.Cancel();

            progress.Value = 0;
        }

        #endregion
    }

    #endregion
}
