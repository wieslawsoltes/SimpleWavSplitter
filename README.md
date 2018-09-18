# SimpleWavSplitter

[![Gitter](https://badges.gitter.im/wieslawsoltes/SimpleWavSplitter.svg)](https://gitter.im/wieslawsoltes/SimpleWavSplitter?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

[![Build status](https://ci.appveyor.com/api/projects/status/pfooqgyo9uwrj44o/branch/master?svg=true)](https://ci.appveyor.com/project/wieslawsoltes/simplewavsplitter/branch/master)
[![Build Status](https://travis-ci.org/wieslawsoltes/SimpleWavSplitter.svg?branch=master)](https://travis-ci.org/wieslawsoltes/SimpleWavSplitter)
[![CircleCI](https://circleci.com/gh/wieslawsoltes/SimpleWavSplitter/tree/master.svg?style=svg)](https://circleci.com/gh/wieslawsoltes/SimpleWavSplitter/tree/master)
[![Build Status](https://dev.azure.com/wieslawsoltes/SimpleWavSplitter/_apis/build/status/wieslawsoltes.SimpleWavSplitter)](https://dev.azure.com/wieslawsoltes/SimpleWavSplitter/_build/latest?definitionId=1)

[![NuGet](https://img.shields.io/nuget/v/WavFile.svg)](https://www.nuget.org/packages/WavFile)
[![MyGet](https://img.shields.io/myget/simplewavsplitter-nightly/vpre/WavFile.svg?label=myget)](https://www.myget.org/gallery/simplewavsplitter-nightly) 

[![CodeFactor](https://www.codefactor.io/repository/github/wieslawsoltes/simplewavsplitter/badge/master)](https://www.codefactor.io/repository/github/wieslawsoltes/simplewavsplitter/overview/master)

Split multi-channel WAV files into single channel WAV files.

* To run program please install .NET Framework Version 4.5
* To build program use Microsoft Visual C# 2017.
* Download are available at: https://github.com/wieslawsoltes/SimpleWavSplitter

### Download

| Platforn                               | Type        | Version       | Download                                                                                                                                                                  |
|----------------------------------------|-------------|---------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Windows (GUI .NET4.5/WPF)              | Portable    | 0.3.4         | [SimpleWavSplitter.Wpf-Release-0.3.4.zip](https://github.com/wieslawsoltes/SimpleWavSplitter/releases/download/0.3.4/SimpleWavSplitter.Wpf-Release-0.3.4.zip)             |
| Windows/Linux/OSX (GUI mono/Avalonia)  | Portable    | 0.3.4         | [SimpleWavSplitter.Avalonia-Release-0.3.4.zip](https://github.com/wieslawsoltes/SimpleWavSplitter/releases/download/0.3.4/SimpleWavSplitter.Avalonia-Release-0.3.4.zip)   |
| Windows/Linux/OSX (Console mono/WPF)   | Portable    | 0.3.4         | [SimpleWavSplitter.Console-Release-0.3.4.zip](https://github.com/wieslawsoltes/SimpleWavSplitter/releases/download/0.3.4/SimpleWavSplitter.Console-Release-0.3.4.zip)     |

### Mirrors

[![Softpedia Download Mirror](http://www.softpedia.com/_img/softpedia_100_free.png)](http://www.softpedia.com/get/Multimedia/Audio/Audio-Editors-Recorders/SimpleWavSplitter.shtml)

## NuGet

SimpleWavSplitter is delivered as a NuGet package.

You can find the package on [NuGet](https://www.nuget.org/packages/WavFile/) or by using nightly build feed:
* Add `https://www.myget.org/F/simplewavsplitter-nightly/api/v2` to your package sources
* Update your package using `WavFile` feed

You can install the package like this:

`Install-Package WavFile -Pre`

### Package Dependencies

Does not require any external dependencies.

### Package Sources

* https://api.nuget.org/v3/index.json
* https://www.myget.org/F/simplewavsplitter-nightly/api/v2

## Resources

* [GitHub source code repository.](https://github.com/wieslawsoltes/SimpleWavSplitter)

## Using SimpleWavSplitter

Please check first the sample apps and the [helper class](https://github.com/wieslawsoltes/SimpleWavSplitter/blob/master/src/WavFile/SimpleWavFileSplitter.cs).
* SimpleWavSplitter.Console
* SimpleWavSplitter.Wpf
* SimpleWavSplitter.Avalonia

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
                WriteLine(
                    string.Format(
                        "SimpleWavSplitterConsole v{0}.{1}.{2}", 
                        v.Major, v.Minor, v.Build));
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
                        bytesTotal, Round((double)bytesTotal / (1024 * 1024), 1)));
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
    var h = WavFileInfo.ReadFileHeader(fs);
    Write(
        string.Format(
            "FileName:\t\t{0}\nFileSize:\t\t{1}\n{2}", 
            Path.GetFileName(fileName), fs.Length, h));
}
```

## License

SimpleWavSplitter is licensed under the [MIT license](LICENSE.TXT).
