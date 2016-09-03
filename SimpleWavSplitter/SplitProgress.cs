// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Windows.Controls;
using System.Windows.Threading;
using WavFile;

namespace SimpleWavSplitter
{
    public class SplitProgress : IProgress
    {
        public ProgressBar ProgressBar { get; private set; }
        public Dispatcher Dispatcher { get; private set; }

        public SplitProgress(ProgressBar progressBar, Dispatcher dispatcher)
        {
            this.ProgressBar = progressBar;
            this.Dispatcher = dispatcher;
        }

        public void Update(double value)
        {
            Dispatcher.Invoke((Action)delegate ()
            {
                this.ProgressBar.Value = value;
            });
        }
    }
}
