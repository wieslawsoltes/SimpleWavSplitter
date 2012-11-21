/*
 * WavFile
 * Copyright © Wiesław Šoltés 2010-2012. All Rights Reserved
 */

namespace WavFile
{
    #region References

    using System;

    #endregion

    #region WavChannel

    /// <summary>
    /// WAV channel information
    /// </summary>
    public struct WavChannel
    {
        private readonly string _longName;
        private readonly string _shortName;
        private readonly WavChannelMask _mask;

        public WavChannel(string longName, string shortName, WavChannelMask mask)
        {
            _longName = longName;
            _shortName = shortName;
            _mask = mask;
        }

        public string LongName { get { return _longName; } }
        public string ShortName { get { return _shortName; } }
        public WavChannelMask Mask { get { return _mask; } }
    }

    #endregion
}
