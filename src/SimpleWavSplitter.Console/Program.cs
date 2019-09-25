// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using WavFile;
using static System.Console;
using static System.Math;

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
                outputPath = fileName.Remove(fileName.Length - Path.GetFileName(fileName).Length);
            }
            else if (args.Count() == 2)
            {
                fileName = args[0];
                outputPath = args[1];
            }
            else
            {
                var v = Assembly.GetExecutingAssembly().GetName().Version;
                WriteLine(
                    string.Format(
                        "SimpleWavSplitterConsole v{0}.{1}.{2}",
                        v?.Major, v?.Minor, v?.Build));
                Write(Environment.NewLine);
                WriteLine("Usage:");
                WriteLine("SimpleWavSplitter.Console <file.wav> [<OutputPath>]");
                Environment.Exit(-1);
            }

            try
            {
                long bytesTotal = 0;
                var splitter = new WavFileSplitter(
                    value => Write(string.Format("\rProgress: {0:0.0}%", value)));
                var sw = Stopwatch.StartNew();
                bytesTotal = splitter.SplitWavFile(fileName, outputPath, CancellationToken.None);
                sw.Stop();
                Write(Environment.NewLine);
                WriteLine(
                    string.Format(
                        "Data bytes processed: {0} ({1} MB)",
                        bytesTotal,
                        Round((double)bytesTotal / (1024 * 1024), 1)));
                WriteLine(string.Format("Elapsed time: {0}", sw.Elapsed));
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                WriteLine(ex.Message);
                WriteLine(ex.StackTrace);
                Environment.Exit(-1);
            }
        }
    }
}
