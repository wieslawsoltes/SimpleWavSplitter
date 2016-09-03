# SimpleWavSplitter

Split multi-channel WAV files into single channel WAV files.

* To run program please install .NET Framework Version 4.5
* To build program use Microsoft Visual C# 2015.
* Download are available at: https://github.com/wieslawsoltes/SimpleWavSplitter

## Examples

### Split Wav Files

```C#
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
                var title = string.Format("SimpleWavSplitterConsole v{0}.{1}.{2}", v.Major, v.Minor, v.Build);
                WriteLine(title);
                Write(Environment.NewLine);
                WriteLine("Usage:");
                WriteLine("SimpleWavSplitter.Console <file.wav> [<OutputPath>]");
                Environment.Exit(-1);
            }

            try
            {
                long bytesTotal = 0;
                var splitter = new WavFileSplitter(value => Write(string.Format("\rProgress: {0:0.0}%", value)));
                var sw = Stopwatch.StartNew();
                bytesTotal = splitter.SplitWavFile(fileName, outputPath, CancellationToken.None);
                sw.Stop();
                Write(Environment.NewLine);
                WriteLine(string.Format("Data bytes processed: {0} ({1} MB)", bytesTotal, Round((double)bytesTotal / (1024 * 1024), 1)));
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
```

### Get Wav Header

```C#
using System.IO;
using static System.Console;

string fileName = "test.wav";

using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
{
    var h = WavFileInfo.ReadFileHeader(f);
    Write(string.Format(
        "FileName:\t\t{0}\nFileSize:\t\t{1}\n{2}", 
        Path.GetFileName(fileName), 
        f.Length.ToString(), 
        h.ToString()));
}
```

## License

SimpleWavSplitter is licensed under the [MIT license](LICENSE.TXT).
