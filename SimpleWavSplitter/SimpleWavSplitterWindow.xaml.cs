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
    using System.Windows;
    using WavFile;

    #endregion

    #region SimpleWavSplitterWindow

    public partial class SimpleWavSplitterWindow : Window
    {
        #region Constructor

        /// <summary>
        /// SimpleWavSplitterWindow Constructor
        /// </summary>
        public SimpleWavSplitterWindow()
        {
            InitializeComponent();
            this.Title = "SimpleWavSplitter v0.0.3";
        }

        #endregion

        #region Worker

        /// <summary>
        /// Using the BackgroundWorker to run jobs in background
        /// </summary>
        private BackgroundWorker worker;

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
                    // parse WAV file header
                    try
                    {
                        // create WAV file stream
                        using (System.IO.FileStream f = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                        {
                            // read WAV file header
                            var h = WavFileInfo.ReadFileHeader(f);

                            // show WAV header
                            MessageBox.Show(
                                "FileName:\t\t" + System.IO.Path.GetFileName(fileName) + "\n" +
                                "FileSize:\t\t" + f.Length.ToString() + "\n\n" +
                                h.ToString(),
                                "WAV Info",
                                MessageBoxButton.OK,
                                MessageBoxImage.None);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
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

        private void SplitWavFiles(string[] dlgFileNames)
        {
            // reset window controls to defaults
            progress.Value = 0;

            // create background worker
            worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;

            worker.DoWork += (s, args) =>
            {
                string[] fileNames = (string[])args.Argument;
                long countBytesTotal = 0;
                var sw = new System.Diagnostics.Stopwatch();

                sw.Start();

                foreach (string fileName in fileNames)
                {
                    // parse WAV file header
                    try
                    {
                        // update progress
                        Dispatcher.Invoke((Action)delegate()
                        {
                            progress.Value = 0.0;
                        });

                        // bytes counter
                        long countBytes = 0;

                        // create WAV file stream
                        var f = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read);

                        // read WAV file header
                        WavFileHeader h = WavFileInfo.ReadFileHeader(f);

                        countBytes += h.HeaderSize;
                        countBytesTotal += h.HeaderSize;

                        // print debug
                        System.Diagnostics.Debug.Print(string.Format("FileName: {0}, Header:\n{1}",
                            fileName,
                            h.ToString()));

                        // create output filenames
                        string filePath = fileName.Remove(fileName.Length - System.IO.Path.GetFileName(fileName).Length);
                        string fileNameOnly = System.IO.Path.GetFileNameWithoutExtension(fileName);
                        string[] outputFileNames = new string[h.NumChannels];
                        WavChannel[] channels = new WavChannel[h.NumChannels];
                        int countChannels = 0;

                        // set channel names
                        if (h.IsExtensible == false)
                        {
                            for (int c = 0; c < h.NumChannels; c++)
                            {
                                string chNum = (c + 1).ToString("D2");
                                var ch = new WavChannel("Channel" + chNum, "CH" + chNum, 0);
                                channels[c] = ch;
                            }
                        }
                        else
                        {
                            foreach (WavChannel ch in WavFileHeader.WavMultiChannelTypes)
                            {
                                if (((uint)ch.Mask & h.ChannelMask) != 0)
                                    channels[countChannels++] = ch;
                            }
                        }

                        // join: input path + input file name without extension + ''. + short channel name + '.wav' extension
                        for (int p = 0; p < channels.Count(); p++)
                        {
                            outputFileNames[p] = filePath + fileNameOnly + "." + channels[p].ShortName + ".wav";
                        }

                        // create data buffers
                        long dataSize = h.Subchunk2Size;
                        int bufferSize = (int)h.ByteRate;
                        int channelBufferSize = (int)(h.ByteRate / h.NumChannels);
                        byte[] buffer = new byte[bufferSize];
                        byte[][] channelBuffer = new byte[h.NumChannels][];
                        int copySize = h.BlockAlign / h.NumChannels;

                        // create output files
                        System.IO.FileStream[] outputFile = new System.IO.FileStream[h.NumChannels];

                        // each mono output file has the same header
                        WavFileHeader mh = WavFileInfo.GetMonoWavFileHeader(h);

                        // write output files header and create temp buffer for each channel
                        for (int c = 0; c < h.NumChannels; c++)
                        {
                            channelBuffer[c] = new byte[channelBufferSize];
                            outputFile[c] = new System.IO.FileStream(outputFileNames[c], System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite);

                            WavFileInfo.WriteFileHeader(outputFile[c], mh);
                        }

                        // cleanup action
                        var cleanUp = new Action(() =>
                        {
                            // close input file
                            f.Close();
                            f.Dispose();

                            // close output files
                            for (int c = 0; c < h.NumChannels; c++)
                            {
                                outputFile[c].Close();
                                outputFile[c].Dispose();
                            }
                        });

                        // read data from input file and write to multiple-output files
                        for (long i = 0; i < dataSize; i += bufferSize)
                        {
                            int n = f.Read(buffer, 0, bufferSize);
                            if (n > 0)
                            {
                                // split channel data
                                int[] count = new int[h.NumChannels];

                                for (int j = 0; j < n; j += h.BlockAlign)
                                {
                                    for (int c = 0; c < h.NumChannels; c++)
                                    {
                                        for (int k = 0; k < copySize; k++)
                                        {
                                            channelBuffer[c][count[c]++] = buffer[j + (c * copySize) + k];
                                        }
                                    }
                                }

                                // write single channel data to a file
                                for (int c = 0; c < h.NumChannels; c++)
                                {
                                    outputFile[c].Write(channelBuffer[c], 0, count[c]);
                                }

                                // cancel background job
                                if (worker.CancellationPending)
                                {
                                    cleanUp();

                                    // cancel job
                                    args.Cancel = true;
                                    return;
                                }

                                // update stats
                                countBytes += n;
                                countBytesTotal += n;

                                // update progress
                                Dispatcher.Invoke((Action)delegate()
                                {
                                    progress.Value = ((double)countBytes / (double)f.Length) * 100;
                                });
                            }
                        }

                        cleanUp();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                sw.Stop();

                args.Result = new Tuple<TimeSpan, long>(sw.Elapsed, countBytesTotal);
            };

            worker.RunWorkerCompleted += (s, args) =>
            {
                if (args.Cancelled == false)
                {
                    //SplitWorkResult result = (SplitWorkResult)args.Result;
                    Tuple<TimeSpan, long> result = (Tuple<TimeSpan, long>)args.Result;

                    string stats = string.Format("Total data bytes processed: {0} ({1} MB)\nTotal elapsed time: {2}",
                        result.Item2,
                        Math.Round((double)result.Item2 / (1024 * 1024), 1),
                        result.Item1);

                    // show statistics to user
                    MessageBox.Show(stats, "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    progress.Value = 0;
                }
            };

            worker.RunWorkerAsync(dlgFileNames);
        }

        private void CancelSplitWorker()
        {
            if (worker != null)
                worker.CancelAsync();

            progress.Value = 0;
        }

        #endregion
    }

    #endregion
}
