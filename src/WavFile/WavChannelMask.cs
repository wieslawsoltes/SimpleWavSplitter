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
        /// Speaker front left channel mask.
        /// </summary>
        SPEAKER_FRONT_LEFT = 0x1,

        /// <summary>
        /// Speaker front right channel mask.
        /// </summary>
        SPEAKER_FRONT_RIGHT = 0x2,

        /// <summary>
        /// Speaker front center channel mask.
        /// </summary>
        SPEAKER_FRONT_CENTER = 0x4,

        /// <summary>
        /// Speaker low frequency channel mask.
        /// </summary>
        SPEAKER_LOW_FREQUENCY = 0x8,

        /// <summary>
        /// Speaker back left channel mask.
        /// </summary>
        SPEAKER_BACK_LEFT = 0x10,

        /// <summary>
        /// Speaker back right channel mask.
        /// </summary>
        SPEAKER_BACK_RIGHT = 0x20,

        /// <summary>
        /// Speaker front left of center channel mask.
        /// </summary>
        SPEAKER_FRONT_LEFT_OF_CENTER = 0x40,

        /// <summary>
        /// Speaker front right of center channel mask.
        /// </summary>
        SPEAKER_FRONT_RIGHT_OF_CENTER = 0x80,

        /// <summary>
        /// Speaker back center channel mask.
        /// </summary>
        SPEAKER_BACK_CENTER = 0x100,

        /// <summary>
        /// Speaker side left channel mask.
        /// </summary>
        SPEAKER_SIDE_LEFT = 0x200,

        /// <summary>
        /// Speaker side right channel mask.
        /// </summary>
        SPEAKER_SIDE_RIGHT = 0x400,

        /// <summary>
        /// Speaker top center channel mask.
        /// </summary>
        SPEAKER_TOP_CENTER = 0x800,

        /// <summary>
        /// Speaker top front left channel mask.
        /// </summary>
        SPEAKER_TOP_FRONT_LEFT = 0x1000,

        /// <summary>
        /// Speaker top front center channel mask.
        /// </summary>
        SPEAKER_TOP_FRONT_CENTER = 0x2000,

        /// <summary>
        /// Speaker top front right channel mask.
        /// </summary>
        SPEAKER_TOP_FRONT_RIGHT = 0x4000,

        /// <summary>
        /// Speaker top back left channel mask.
        /// </summary>
        SPEAKER_TOP_BACK_LEFT = 0x8000,

        /// <summary>
        /// Speaker top back center channel mask.
        /// </summary>
        SPEAKER_TOP_BACK_CENTER = 0x10000,

        /// <summary>
        /// Speaker top back right channel mask.
        /// </summary>
        SPEAKER_TOP_BACK_RIGHT = 0x20000
    }
}
