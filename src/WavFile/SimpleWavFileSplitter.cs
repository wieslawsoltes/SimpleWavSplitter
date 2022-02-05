using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WavFile;

/// <summary>
/// Split multi-channel WAV files into single channel WAV files.
/// </summary>
public class SimpleWavFileSplitter
{
    private CancellationTokenSource? _tokenSource;

    /// <summary>
    /// Get WAV file header.
    /// </summary>
    /// <param name="fileNames">The file names.</param>
    /// <param name="setOutput">Set output string action.</param>
    public void GetWavHeader(string[] fileNames, Action<string> setOutput)
    {
        var sb = new StringBuilder();
        int totalFiles = 0;
        foreach (string fileName in fileNames)
        {
            try
            {
                using (var fs = File.OpenRead(fileName))
                {
                    var h = WavFileInfo.ReadFileHeader(fs);
                    if (totalFiles > 0)
                    {
                        sb.Append("\n\n");
                    }
                    sb.Append(
                        string.Format(
                            "FileName:\t\t{0}\nFileSize:\t\t{1}\n{2}",
                            Path.GetFileName(fileName), fs.Length.ToString(), h.ToString()));
                    totalFiles++;
                }
            }
            catch (Exception ex)
            {
                string text = string.Format("Error: {0}\n", ex.Message);
                sb.Append(text);
                setOutput(sb.ToString());
            }
        }
        setOutput(sb.ToString());
    }

    /// <summary>
    /// Split WAV files.
    /// </summary>
    /// <param name="files">The file names.</param>
    /// <param name="path">The output path.</param>
    /// <param name="setProgress">Set progress value action.</param>
    /// <param name="setOutput">Set output string action.</param>
    /// <returns></returns>
    public async Task SplitWavFiles(string[] files, string path, Action<double> setProgress, Action<string> setOutput)
    {
        setProgress(0);
        _tokenSource = new CancellationTokenSource();
        CancellationToken ct = _tokenSource.Token;

        var sw = Stopwatch.StartNew();
        var sb = new StringBuilder();

        sb.Append(string.Format("Files to split: {0}\n", files.Count()));
        setOutput(sb.ToString());

        if (path.EndsWith("\\") == false && path.Length > 0)
        {
            path += "\\";
        }

        long totalBytesProcessed = await Task<long>.Factory.StartNew(() =>
        {
            ct.ThrowIfCancellationRequested();
            long countBytesTotal = 0;

            var splitter = new WavFileSplitter(setProgress);

            foreach (string fileName in files)
            {
                try
                {
                    string outputPath = path.Length > 0 ?
                        path :
                        fileName.Remove(fileName.Length - Path.GetFileName(fileName).Length);

                    sb.Append(
                        string.Format(
                            "Split file: {0}\n",
                            Path.GetFileName(fileName)));

                    setOutput(sb.ToString());

                    countBytesTotal += splitter.SplitWavFile(fileName, outputPath, ct);
                }
                catch (Exception ex)
                {
                    sb.Append(string.Format("Error: {0}\n", ex.Message));

                    setOutput(sb.ToString());

                    return countBytesTotal;
                }
            }
            return countBytesTotal;
        }, ct);

        sw.Stop();
        if (_tokenSource.IsCancellationRequested == false)
        {
            string text = string.Format(
                "Done.\nData bytes processed: {0} ({1} MB)\nElapsed time: {2}\n",
                totalBytesProcessed,
                Math.Round((double)totalBytesProcessed / (1024 * 1024), 1),
                sw.Elapsed);
            sb.Append(text);
            setOutput(sb.ToString());
        }
    }

    /// <summary>
    /// Cancel WAV file split.
    /// </summary>
    /// <param name="setProgress">Set progress value action.</param>
    /// <returns>The cancellation task.</returns>
    public async Task CancelSplitWavFiles(Action<double> setProgress)
    {
        if (_tokenSource != null)
        {
            await Task.Factory.StartNew(() => _tokenSource.Cancel());
        }
        setProgress(0);
    }
}