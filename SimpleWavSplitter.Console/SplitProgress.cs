// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SimpleWavSplitter
{
    #region References

    using System;
    using WavFile;

    #endregion

    #region SplitProgress

    public class SplitProgress : IProgress
    {
        #region Constructor

        public SplitProgress() { }

        #endregion

        #region IProgress

        public void Update(double value)
        {
            string text = string.Format("\rProgress: {0:0.0}%", value);
            System.Console.Write(text);
        }

        #endregion
    }

    #endregion
}
