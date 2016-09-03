// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace WavFile
{
    /// <summary>
    /// Split multi-channel WAV file into single channel WAV files.
    /// </summary>
    public class WavFileSplitter
    {
        private Action<double> Progress { get; }

        /// <summary>
        /// Initializes new instance of the <see cref="WavFileSplitter"/> class.
        /// </summary>
        /// <param name="progress">The progress update action.</param>
        public WavFileSplitter(Action<double> progress)
        {
            Progress = progress;
        }

        /// <summary>
        /// Split multi-channel WAV file into single channel WAV files.
        /// </summary>
        /// <param name="fileName">Input WAV file name.</param>
        /// <param name="outputPath">Output WAV files path.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The total processed bytes.</returns>
        public long SplitWavFile(string fileName, string outputPath, CancellationToken ct)
        {
            long countBytesTotal = 0;
            long countBytes = 0;
            var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            var header = WavFileInfo.ReadFileHeader(fs);

            Progress(0.0);

            countBytes += header.HeaderSize;
            countBytesTotal += header.HeaderSize;

            string fileNameOnly = Path.GetFileNameWithoutExtension(fileName);
            string[] outputFileNames = new string[header.NumChannels];
            WavChannel[] channels = new WavChannel[header.NumChannels];
            int countChannels = 0;

            if (header.IsExtensible == false)
            {
                for (int c = 0; c < header.NumChannels; c++)
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
                    if (((uint)ch.Mask & header.ChannelMask) != 0)
                    {
                        channels[countChannels++] = ch;
                    }
                }
            }

            for (int p = 0; p < channels.Count(); p++)
            {
                outputFileNames[p] = outputPath + fileNameOnly + "." + channels[p].ShortName + ".wav";
            }

            long dataSize = header.Subchunk2Size;
            int bufferSize = (int)header.ByteRate;
            int channelBufferSize = (int)(header.ByteRate / header.NumChannels);
            byte[] buffer = new byte[bufferSize];
            byte[][] channelBuffer = new byte[header.NumChannels][];
            int copySize = header.BlockAlign / header.NumChannels;
            var outputFiles = new FileStream[header.NumChannels];
            var mh = WavFileInfo.GetMonoWavFileHeader(header);

            for (int c = 0; c < header.NumChannels; c++)
            {
                channelBuffer[c] = new byte[channelBufferSize];
                outputFiles[c] = new FileStream(outputFileNames[c], FileMode.Create, FileAccess.ReadWrite);
                WavFileInfo.WriteFileHeader(outputFiles[c], mh);
            }

            var cleanUp = new Action(() =>
            {
                fs.Close();
                fs.Dispose();

                for (int c = 0; c < header.NumChannels; c++)
                {
                    outputFiles[c].Close();
                    outputFiles[c].Dispose();
                }
            });

            for (long i = 0; i < dataSize; i += bufferSize)
            {
                int n = fs.Read(buffer, 0, bufferSize);
                if (n > 0)
                {
                    int[] count = new int[header.NumChannels];

                    for (int j = 0; j < n; j += header.BlockAlign)
                    {
                        for (int c = 0; c < header.NumChannels; c++)
                        {
                            for (int k = 0; k < copySize; k++)
                            {
                                channelBuffer[c][count[c]++] = buffer[j + (c * copySize) + k];
                            }
                        }
                    }

                    for (int c = 0; c < header.NumChannels; c++)
                    {
                        outputFiles[c].Write(channelBuffer[c], 0, count[c]);
                    }

                    if (ct.IsCancellationRequested)
                    {
                        cleanUp();
                        ct.ThrowIfCancellationRequested();
                    }

                    countBytes += n;
                    countBytesTotal += n;

                    Progress((countBytes / fs.Length) * 100.0);
                }
            }

            cleanUp();
            return countBytesTotal;
        }
    }
}
