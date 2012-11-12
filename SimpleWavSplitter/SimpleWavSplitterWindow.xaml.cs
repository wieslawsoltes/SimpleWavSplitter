using System;
using System.Linq;
/*
 * SimpleWavSplitter
 * Copyright © Wiesław Šoltés 2010-2012. All Rights Reserved
 */
using System.Windows;
using System.ComponentModel;

namespace SimpleWavSplitter
{
    /// <summary>
    /// Interaction logic for SimpleWavSplitterWindow.xaml
    /// </summary>
    public partial class SimpleWavSplitterWindow : Window
    {
        /// <summary>
        /// 
        /// </summary>
        public SimpleWavSplitterWindow()
        {
            InitializeComponent();
            this.Title = "SimpleWavSplitter v0.0.2";
        }

        /// <summary>
        /// Store splitter work results stats
        /// </summary>
        public struct SplitWorkResult
        {
            public TimeSpan t;
            public WavFileHeader h;
            public long countBytes;
        }

        /// <summary>
        /// Using the BackgroundWorker to run jobs in background
        /// </summary>
        BackgroundWorker worker;
        public delegate void UpdateSplitProgressDelegate(double currentProgress);

        /// <summary>
        /// Update progress bar for WAV spliter
        /// </summary>
        /// <param name="progress"></param>
        public void UpdateSplitProgress(double currentProgress)
        {
            progress.Value = currentProgress;
        }

        /// <summary>
        /// Get WAV file header.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnGetWavHeader_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog d = new Microsoft.Win32.OpenFileDialog();
            d.Filter = "WAV Files (*.wav)|*.wav|All Files (*.*)|*.*";
            d.FilterIndex = 0;
            d.Multiselect = true;
            if (d.ShowDialog() == true)
            {
                string[] fileNames = d.FileNames;
                foreach (string fileName in fileNames)
                {
                    // parse WAV file header
                    try
                    {
                        System.IO.FileStream f = null;
                        WavFileHeader h = new WavFileHeader();

                        // create WAV file stream
                        f = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read);

                        // read WAV file header
                        h = WavFile.ReadFileHeader(f);

                        // show WAV header
                        MessageBox.Show(
                            "FileName:\t\t" + System.IO.Path.GetFileName(fileName) + "\n" +
                            "FileSize:\t\t" + f.Length.ToString() + "\n\n" +
                            h.ToString(),
                            "WAV Info",
                            MessageBoxButton.OK,
                            MessageBoxImage.None
                            );

                        // close input file
                        f.Close();
                        f.Dispose();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Split multi-channel WAV files into multiple mono WAV files
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSplitWavFiles_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog d = new Microsoft.Win32.OpenFileDialog();
            d.Filter = "WAV Files (*.wav)|*.wav|All Files (*.*)|*.*";
            d.FilterIndex = 0;
            d.Multiselect = true;
            if (d.ShowDialog() == true)
            {
                // reset window controls to defaults
                progress.Value = 0;

                // create background worker
                System.Windows.Threading.Dispatcher dispatcher = this.Dispatcher;
                worker = new BackgroundWorker();
                worker.WorkerSupportsCancellation = true;

                worker.DoWork += (s, args) =>
                {
                    string[] fileNames = (string[])args.Argument;
                    long countBytesTotal = 0;
                    System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

                    sw.Start();

                    foreach(string fileName in fileNames)
                    {
                        UpdateSplitProgressDelegate updateSplitProgress = new UpdateSplitProgressDelegate(UpdateSplitProgress);
                        dispatcher.BeginInvoke(updateSplitProgress, 0);

                        // parse WAV file header
                        try
                        {
                            System.IO.FileStream f = null;
                            WavFileHeader h = new WavFileHeader();
                            long countBytes = 0;

                            // create WAV file stream
                            f = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read);

                            // read WAV file header
                            h = WavFile.ReadFileHeader(f);
                            countBytes += h.HeaderSize;
                            countBytesTotal += h.HeaderSize;

                            //MessageBox.Show(h.ToString());

                            // create output filenames
                            string filePath = d.FileName.Remove(fileName.Length - System.IO.Path.GetFileName(fileName).Length);
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
                                    WavChannel ch = new WavChannel("Channel" + chNum, "CH" + chNum, 0);
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
                            WavFileHeader monoFileHeader = WavFile.GetMonoWavFileHeader(h);

                            // write output files header and create temp buffer for each channel
                            for (int c = 0; c < h.NumChannels; c++)
                            {
                                channelBuffer[c] = new byte[channelBufferSize];
                                outputFile[c] = new System.IO.FileStream(outputFileNames[c], System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite);

                                WavFile.WriteFileHeader(outputFile[c], monoFileHeader);
                            }

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
                                        // close input file
                                        f.Close();
                                        f.Dispose();

                                        // close output files
                                        for (int c = 0; c < h.NumChannels; c++)
                                        {
                                            outputFile[c].Close();
                                            outputFile[c].Dispose();
                                        }

                                        // cancel job
                                        args.Cancel = true;
                                        return;
                                    }

                                    // update progress bar
                                    countBytes += n;
                                    countBytesTotal += n;
                                    dispatcher.BeginInvoke(updateSplitProgress, ((double)countBytes / (double)f.Length) * 100);
                                }
                            }

                            // close input file
                            f.Close();
                            f.Dispose();

                            // close output files
                            for (int c = 0; c < h.NumChannels; c++)
                            {
                                outputFile[c].Close();
                                outputFile[c].Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }

                    sw.Stop();

                    // prepare result
                    SplitWorkResult result = new SplitWorkResult();
                    result.t = sw.Elapsed;
                    result.countBytes = countBytesTotal;

                    args.Result = result;
                };

                worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
                {
                    if (args.Cancelled == false)
                    {
                        SplitWorkResult result = (SplitWorkResult)args.Result;

                        // show statistics to user
                        MessageBox.Show(
                            "Total data bytes processed: " +
                            result.countBytes.ToString() +
                            " (" +
                            Math.Round((double)result.countBytes / (1024 * 1024), 1).ToString() +
                            " MB)" +
                            "\nTotal elapsed time: " + result.t.ToString(),
                            "Done",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        UpdateSplitProgress(0);
                    }
                };

                worker.RunWorkerAsync(d.FileNames);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (worker != null)
                worker.CancelAsync();

            progress.Value = 0;
        }
    }
}
