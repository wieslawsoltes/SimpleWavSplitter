# SimpleWavSplitter

Split multi-channel WAV files into single channel WAV files.

Copyright © Wiesław Šoltés 2010-2015. All Rights Reserved

* To contact author send e-mail to: wieslaw.soltes@gmail.com
* To run program please install .NET Framework Version 4.0, Client Profile
* To build program use Microsoft Visual C# 2010 Express or Visual Studio 2012 Express for Windows Desktop.
* Download are available at: https://github.com/wieslawsoltes/SimpleWavSplitter

## Examples

### Split Wav Files

```C#
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WavFile;

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
            System.Console.WriteLine("SimpleWavSplitter.Console");
            System.Console.WriteLine("");
            System.Console.WriteLine("Usage:");
            System.Console.WriteLine("SimpleWavSplitter.Console <*.wav> [<OutputPath>]");
            System.Environment.Exit(-1);
        }

        long bytesTotal = 0;
        var splitter = new WavFileSplitter(new SplitProgress());
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            bytesTotal = splitter.SplitWavFile(fileName, outputPath, System.Threading.CancellationToken.None);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Error: {0}", ex.Message);
            System.Environment.Exit(-1);
        }

        System.Console.WriteLine("");
        sw.Stop();
        string stat1 = string.Format("Data bytes processed: {0} ({1} MB)", bytesTotal, Math.Round((double)bytesTotal / (1024 * 1024), 1));
        System.Console.WriteLine(stat1);
        string stat2 = string.Format("Elapsed time: {0}", sw.Elapsed);
        System.Console.WriteLine(stat2);
        System.Environment.Exit(0);
    }
}
```

```C#
using System;
using WavFile;

public class SplitProgress : IProgress
{
    public void Update(double value)
    {
        string text = string.Format("\rProgress: {0:0.0}%", value);
        System.Console.Write(text);
    }
}
```

### Get Wav Header

```C#
string fileName = "test.wav";
using (System.IO.FileStream f = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
{
    var h = WavFileInfo.ReadFileHeader(f);
    string text = string.Format("FileName:\t\t{0}\nFileSize:\t\t{1}\n{2}", 
        System.IO.Path.GetFileName(fileName),
        f.Length.ToString(),
        h.ToString());
    System.Console.Write(text);
}
```

## License

SimpleWavSplitter is licensed under the [MIT license](LICENSE.TXT).
