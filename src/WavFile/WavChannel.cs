// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace WavFile
{
    /// <summary>
    /// WAV channel information.
    /// </summary>
    public struct WavChannel
    {
        private readonly string _longName;
        private readonly string _shortName;
        private readonly WavChannelMask _mask;

        /// <summary>
        /// 
        /// </summary>
        public string LongName { get { return _longName; } }

        /// <summary>
        /// 
        /// </summary>
        public string ShortName { get { return _shortName; } }

        /// <summary>
        /// 
        /// </summary>
        public WavChannelMask Mask { get { return _mask; } }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="longName"></param>
        /// <param name="shortName"></param>
        /// <param name="mask"></param>
        public WavChannel(string longName, string shortName, WavChannelMask mask)
        {
            _longName = longName;
            _shortName = shortName;
            _mask = mask;
        }
    }
}
