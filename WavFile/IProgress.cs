/*
 * WavFile
 * Copyright © Wiesław Šoltés 2010-2012. All Rights Reserved
 */

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
