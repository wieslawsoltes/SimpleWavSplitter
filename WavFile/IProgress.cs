// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace WavFile
{
    #region References

    using System;

    #endregion

    #region IProgress

    /// <summary>
    /// Interface used to report background task progress to UI
    /// </summary>
    public interface IProgress
    {
        /// <summary>
        /// Update current progress
        /// </summary>
        /// <param name="value">Progress value</param>
        void Update(double value);
    }

    #endregion
}
