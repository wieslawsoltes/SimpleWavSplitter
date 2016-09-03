// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using WavFile;

namespace SimpleWavSplitter
{
    public class SplitProgress : IProgress
    {
        public void Update(double value)
        {
            string text = string.Format("\rProgress: {0:0.0}%", value);
            System.Console.Write(text);
        }
    }
}
