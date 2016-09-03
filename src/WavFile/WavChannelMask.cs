// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace WavFile
{
    /// <summary>
    /// Multi-channel WAV file mask.
    /// </summary>
    public enum WavChannelMask
    {
        /// <summary>
        /// 
        /// </summary>
        SPEAKER_FRONT_LEFT = 0x1,

        /// <summary>
        /// 
        /// </summary>
        SPEAKER_FRONT_RIGHT = 0x2,

        /// <summary>
        /// 
        /// </summary>
        SPEAKER_FRONT_CENTER = 0x4,

        /// <summary>
        /// 
        /// </summary>
        SPEAKER_LOW_FREQUENCY = 0x8,

        /// <summary>
        /// 
        /// </summary>
        SPEAKER_BACK_LEFT = 0x10,

        /// <summary>
        /// 
        /// </summary>
        SPEAKER_BACK_RIGHT = 0x20,

        /// <summary>
        /// 
        /// </summary>
        SPEAKER_FRONT_LEFT_OF_CENTER = 0x40,

        /// <summary>
        /// 
        /// </summary>
        SPEAKER_FRONT_RIGHT_OF_CENTER = 0x80,

        /// <summary>
        /// 
        /// </summary>
        SPEAKER_BACK_CENTER = 0x100,

        /// <summary>
        /// 
        /// </summary>
        SPEAKER_SIDE_LEFT = 0x200,

        /// <summary>
        /// 
        /// </summary>
        SPEAKER_SIDE_RIGHT = 0x400,

        /// <summary>
        /// 
        /// </summary>
        SPEAKER_TOP_CENTER = 0x800,

        /// <summary>
        /// 
        /// </summary>
        SPEAKER_TOP_FRONT_LEFT = 0x1000,

        /// <summary>
        /// 
        /// </summary>
        SPEAKER_TOP_FRONT_CENTER = 0x2000,

        /// <summary>
        /// 
        /// </summary>
        SPEAKER_TOP_FRONT_RIGHT = 0x4000,

        /// <summary>
        /// 
        /// </summary>
        SPEAKER_TOP_BACK_LEFT = 0x8000,

        /// <summary>
        /// 
        /// </summary>
        SPEAKER_TOP_BACK_CENTER = 0x10000,

        /// <summary>
        /// 
        /// </summary>
        SPEAKER_TOP_BACK_RIGHT = 0x20000
    }
}
