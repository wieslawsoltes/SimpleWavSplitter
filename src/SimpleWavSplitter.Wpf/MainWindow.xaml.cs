// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using WavFile;

namespace SimpleWavSplitter.Wpf
{
    /// <summary>
    /// Main window.
    /// </summary>
    public partial class MainWindow : Window
    {
        private SimpleWavFileSplitter _wavFileSplitter;

        /// <summary>
        /// Initializes the new instance of <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            _wavFileSplitter = new SimpleWavFileSplitter();

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Title = string.Format("SimpleWavSplitter v{0}.{1}.{2}", version.Major, version.Minor, version.Build);

            btnBrowseOutputPath.Click += (sender, e) => GetOutputPath();
            btnGetWavHeader.Click += (sender, e) => GetWavHeader();
            btnSplitWavFiles.Click += async (sender, e) => await SplitWavFiles();
            btnCancel.Click += async (sender, e) => await _wavFileSplitter.CancelSplitWavFiles(
                value => Dispatcher.Invoke(() => progress.Value = value));
        }

        private void GetOutputPath()
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog();
            string text = textOutputPath.Text;
            if (text.Length > 0)
            {
                dlg.SelectedPath = textOutputPath.Text;
            }

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textOutputPath.Text = dlg.SelectedPath;
            }
        }

        private void GetWavHeader()
        {
            var dlg = new OpenFileDialog()
            {
                Filter = "WAV Files (*.wav)|*.wav|All Files (*.*)|*.*",
                FilterIndex = 0,
                Multiselect = true
            };

            if (dlg.ShowDialog() == true)
            {
                _wavFileSplitter.GetWavHeader(dlg.FileNames, text => textOutput.Text = text);
            }
        }

        private async Task SplitWavFiles()
        {
            var dlg = new OpenFileDialog()
            {
                Filter = "WAV Files (*.wav)|*.wav|All Files (*.*)|*.*",
                FilterIndex = 0,
                Multiselect = true
            };

            if (dlg.ShowDialog() == true)
            {
                await _wavFileSplitter.SplitWavFiles(
                    dlg.FileNames,
                    textOutputPath.Text,
                    value => Dispatcher.Invoke(() => progress.Value = value),
                    text => Dispatcher.Invoke(() => textOutput.Text = text));
            }
        }
    }
}
