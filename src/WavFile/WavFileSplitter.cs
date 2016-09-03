// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Linq;
using System.Threading;

namespace WavFile
{
    /// <summary>
    /// Split multi-channel WAV file into single channel WAV files
    /// </summary>
    public class WavFileSplitter
    {
        public Action<double> Progress { get; private set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public WavFileSplitter() { }

        /// <summary>
        /// Set progress handler
        /// </summary>
        /// <param name="progress"></param>
        public WavFileSplitter(Action<double> progress)
        {
            this.Progress = progress;
        }

        /// <summary>
        /// Split multi-channel WAV file into single channel WAV files
        /// </summary>
        /// <param name="fileName">Input WAV file name</param>
        /// <param name="outputPath">Output WAV files path</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns></returns>
        public long SplitWavFile(string fileName, string outputPath, CancellationToken ct)
        {
            long countBytesTotal = 0;

            // update progress
            if (Progress != null)
            {
                Progress(0.0);
            }

            // bytes counter
            long countBytes = 0;

            // create WAV file stream
            var f = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read);

            // read WAV file header
            WavFileHeader h = WavFileInfo.ReadFileHeader(f);

            countBytes += h.HeaderSize;
            countBytesTotal += h.HeaderSize;

            // print header info
            //System.Diagnostics.Debug.Print(string.Format("FileName: {0}, Header:\n{1}",
            //    fileName,
            //    h.ToString()));

            // create output filenames
            //string outputPath = fileName.Remove(fileName.Length - System.IO.Path.GetFileName(fileName).Length);
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
                outputFileNames[p] = outputPath + fileNameOnly + "." + channels[p].ShortName + ".wav";
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


                    if (ct.IsCancellationRequested)
                    {
                        cleanUp();

                        ct.ThrowIfCancellationRequested();
                    }

                    // update stats
                    countBytes += n;
                    countBytesTotal += n;

                    // update progress
                    if (Progress != null)
                    {
                        Progress(((double)countBytes / (double)f.Length) * 100);
                    }
                }
            }

            cleanUp();

            return countBytesTotal;
        }
    }
}
