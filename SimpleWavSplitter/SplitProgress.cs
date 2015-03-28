// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SimpleWavSplitter
{
    #region References

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Windows.Controls;
    using System.Windows.Threading;
    using WavFile;

    #endregion

    #region SplitProgress

    public class SplitProgress : IProgress
    {
        #region Properties

        public ProgressBar ProgressBar { get; private set; }
        public Dispatcher Dispatcher { get; private set; }

        #endregion

        #region Constructor

        public SplitProgress(ProgressBar progressBar, Dispatcher dispatcher)
        {
            this.ProgressBar = progressBar;
            this.Dispatcher = dispatcher;
        }

        #endregion

        #region IProgress

        public void Update(double value)
        {
            Dispatcher.Invoke((Action)delegate()
            {
                this.ProgressBar.Value = value;
            });
        }

        #endregion
    }

    #endregion
}
