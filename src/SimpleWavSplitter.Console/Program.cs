// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Linq;
using WavFile;

namespace SimpleWavSplitter.Console
{
    /// <summary>
    /// Wav file splitter console program.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            string fileName = string.Empty;
            string outputPath = string.Empty;

            if (args.Count() == 1)
            {
                fileName = args[0];

                outputPath = fileName.Remove(fileName.Length - System.IO.Path.GetFileName(fileName).Length);
            }
            else if (args.Count() == 2)
            {
                fileName = args[0];

                outputPath = args[1];
            }
            else
            {
                System.Console.WriteLine("SimpleWavSplitter.Console v0.1.0.0");
                System.Console.WriteLine("");
                System.Console.WriteLine("Usage:");
                System.Console.WriteLine("SimpleWavSplitter.Console <*.wav> [<OutputPath>]");

                System.Environment.Exit(-1);
            }

            long bytesTotal = 0;
            var splitter = new WavFileSplitter(value => System.Console.Write(string.Format("\rProgress: {0:0.0}%", value)));

            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // split multi-channel WAV file into single channel WAV files
                bytesTotal = splitter.SplitWavFile(fileName, outputPath, System.Threading.CancellationToken.None);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Error: {0}", ex.Message);
                System.Environment.Exit(-1);
            }

            System.Console.WriteLine("");

            // show slit stats
            sw.Stop();

            string stat1 = string.Format("Data bytes processed: {0} ({1} MB)",
                bytesTotal,
                Math.Round((double)bytesTotal / (1024 * 1024), 1));

            System.Console.WriteLine(stat1);

            string stat2 = string.Format("Elapsed time: {0}",
                sw.Elapsed);

            System.Console.WriteLine(stat2);

            // exit
            System.Environment.Exit(0);
        }
    }
}
