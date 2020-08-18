
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
        /// Gets wav channel long name.
        /// </summary>
        public string LongName
        {
            get { return _longName; }
        }

        /// <summary>
        /// Gets wav channel short name.
        /// </summary>
        public string ShortName
        {
            get { return _shortName; }
        }

        /// <summary>
        /// Gets wav channel mask.
        /// </summary>
        public WavChannelMask Mask
        {
            get { return _mask; }
        }

        /// <summary>
        /// Initializes new instance of the <see cref="WavChannel"/> struct.
        /// </summary>
        /// <param name="longName">The wav channel long name.</param>
        /// <param name="shortName">The wav channel short name.</param>
        /// <param name="mask">The wav channel mask.</param>
        public WavChannel(string longName, string shortName, WavChannelMask mask)
        {
            _longName = longName;
            _shortName = shortName;
            _mask = mask;
        }
    }
}
