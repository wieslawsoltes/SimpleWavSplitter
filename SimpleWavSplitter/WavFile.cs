/*
 * SimpleWavSplitter
 * Copyright © Wiesław Šoltés 2010-2012. All Rights Reserved
 */
using System;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SimpleWavSplitter
{
    /* http://msdn.microsoft.com/en-us/library/dd757720(v=VS.85).aspx

    typedef struct
    {
        WORD wFormatTag;
        WORD nChannels;
        DWORD nSamplesPerSec;
        DWORD nAvgBytesPerSec;
        WORD nBlockAlign;
        WORD wBitsPerSample;
        WORD cbSize;
    } WAVEFORMATEX;

    // Note:
    // cbSize is at Least 22
    // For WAVEFORMATEXTENSIBLE, cbSize must always be set to at least 22. 
    // This is the sum of the sizes of the Samples union (2), DWORD dwChannelMask (4), and GUID guidSubFormat (16). 
    // This is appended to the initial WAVEFORMATEX Format (size 18), so a WAVEFORMATPCMEX and WAVEFORMATIEEEFLOATEX structure is 64-bit aligned.
    */

    /* http://msdn.microsoft.com/en-us/library/dd757721(v=VS.85).aspx
     * http://www.microsoft.com/whdc/device/audio/multichaud.mspx

    typedef struct 
    {
        WAVEFORMATEX Format;
        union 
        {
            WORD wValidBitsPerSample;
            WORD wSamplesPerBlock;
            WORD wReserved;
        } Samples;
        DWORD dwChannelMask;
        GUID SubFormat;
    } WAVEFORMATEXTENSIBLE, *PWAVEFORMATEXTENSIBLE;

    */

    /* http://www.microsoft.com/whdc/device/audio/multichaud.mspx
     * KSMEDIA.H

        #define STATIC_KSDATAFORMAT_SUBTYPE_PCM\
            DEFINE_WAVEFORMATEX_GUID(WAVE_FORMAT_PCM)
        DEFINE_GUIDSTRUCT("00000001-0000-0010-8000-00aa00389b71", KSDATAFORMAT_SUBTYPE_PCM);
        #define KSDATAFORMAT_SUBTYPE_PCM DEFINE_GUIDNAMED(KSDATAFORMAT_SUBTYPE_PCM)

        #define STATIC_KSDATAFORMAT_SUBTYPE_IEEE_FLOAT\ 
            DEFINE_WAVEFORMATEX_GUID(WAVE_FORMAT_IEEE_FLOAT) 
        DEFINE_GUIDSTRUCT("00000003-0000-0010-8000-00aa00389b71", KSDATAFORMAT_SUBTYPE_IEEE_FLOAT); 
        #define KSDATAFORMAT_SUBTYPE_IEEE_FLOAT DEFINE_GUIDNAMED(KSDATAFORMAT_SUBTYPE_IEEE_FLOAT)
    */

    /* http://www.microsoft.com/whdc/device/audio/multichaud.mspx
        Default Channel Ordering:
        1.  Front Left - FL
        2.  Front Right - FR
        3.  Front Center - FC
        4.  Low Frequency - LF
        5.  Back Left - BL
        6.  Back Right - BR
        7.  Front Left of Center - FLC
        8.  Front Right of Center - FRC
        9.  Back Center - BC
        10. Side Left - SL
        11. Side Right - SR
        12. Top Center - TC
        13. Top Front Left - TFL
        14. Top Front Center - TFC
        15. Top Front Right - TFR
        16. Top Back Left - TBL
        17. Top Back Center - TBC
        18. Top Back Right - TBR
    */

    public enum WavChannelMask
    {
        SPEAKER_FRONT_LEFT = 0x1,
        SPEAKER_FRONT_RIGHT = 0x2,
        SPEAKER_FRONT_CENTER = 0x4,
        SPEAKER_LOW_FREQUENCY = 0x8,
        SPEAKER_BACK_LEFT = 0x10,
        SPEAKER_BACK_RIGHT = 0x20,
        SPEAKER_FRONT_LEFT_OF_CENTER = 0x40,
        SPEAKER_FRONT_RIGHT_OF_CENTER = 0x80,
        SPEAKER_BACK_CENTER = 0x100,
        SPEAKER_SIDE_LEFT = 0x200,
        SPEAKER_SIDE_RIGHT = 0x400,
        SPEAKER_TOP_CENTER = 0x800,
        SPEAKER_TOP_FRONT_LEFT = 0x1000,
        SPEAKER_TOP_FRONT_CENTER = 0x2000,
        SPEAKER_TOP_FRONT_RIGHT = 0x4000,
        SPEAKER_TOP_BACK_LEFT = 0x8000,
        SPEAKER_TOP_BACK_CENTER = 0x10000,
        SPEAKER_TOP_BACK_RIGHT = 0x20000
    }

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

    /*
    The canonical WAVE format starts with the RIFF header:

    0         4   ChunkID          Contains the letters "RIFF" in ASCII form
                                    (0x52494646 big-endian form).
    4         4   ChunkSize        36 + SubChunk2Size, or more precisely:
                                    4 + (8 + SubChunk1Size) + (8 + SubChunk2Size)
                                    This is the size of the rest of the chunk 
                                    following this number.  This is the size of the 
                                    entire file in bytes minus 8 bytes for the
                                    two fields not included in this count:
                                    ChunkID and ChunkSize.
    8         4   Format           Contains the letters "WAVE"
                                    (0x57415645 big-endian form).

    The "WAVE" format consists of two subchunks: "fmt " and "data":
    The "fmt " subchunk describes the sound data's format:

    12        4   Subchunk1ID      Contains the letters "fmt "
                                    (0x666d7420 big-endian form).
    16        4   Subchunk1Size    16 for PCM.  This is the size of the
                                    rest of the Subchunk which follows this number.
    20        2   AudioFormat      PCM = 1 (i.e. Linear quantization)
                                    Values other than 1 indicate some 
                                    form of compression.
    22        2   NumChannels      Mono = 1, Stereo = 2, etc.
    24        4   SampleRate       8000, 44100, etc.
    28        4   ByteRate         == SampleRate * NumChannels * BitsPerSample/8
    32        2   BlockAlign       == NumChannels * BitsPerSample/8
                                    The number of bytes for one sample including
                                    all channels. I wonder what happens when
                                    this number isn't an integer?
    34        2   BitsPerSample    8 bits = 8, 16 bits = 16, etc.
                2   ExtraParamSize   if PCM, then doesn't exist
                X   ExtraParams      space for extra parameters

    The "data" subchunk contains the size of the data and the actual sound:

    36        4   Subchunk2ID      Contains the letters "data"
                                    (0x64617461 big-endian form).
    40        4   Subchunk2Size    == NumSamples * NumChannels * BitsPerSample/8
                                    This is the number of bytes in the data.
                                    You can also think of this as the size
                                    of the read of the subchunk following this 
                                    number.
    44        *   Data             The actual sound data.
    */

    /// <summary>
    /// The canonical WAVE format starts with the RIFF header
    /// Based on description from:
    /// https://ccrma.stanford.edu/courses/422/projects/WaveFormat/
    /// </summary>
    public struct WavFileHeader
    {
        //
        // WAVE                             bytes=12
        //
        public UInt32 ChunkID;          //  bytes=4
        public UInt32 ChunkSize;        //  bytes=4
        public UInt32 Format;           //  bytes=4
        //
        // fmt                              bytes=24
        //
        public UInt32 Subchunk1ID;      //  bytes=4
        public UInt32 Subchunk1Size;    //  bytes=4
        public UInt16 AudioFormat;      //  bytes=2
        public UInt16 NumChannels;      //  bytes=2
        public UInt32 SampleRate;       //  bytes=4
        public UInt32 ByteRate;         //  bytes=4
        public UInt16 BlockAlign;       //  bytes=2
        public UInt16 BitsPerSample;    //  bytes=2
        //
        // extra                            bytes=2
        // if h.Subchunk1Size > 16
        //
        public UInt16 ExtraParamSize;   //  bytes=2
        //
        // extensible                       bytes=22
        //
        public UInt16 Samples;          //  bytes=2
        public UInt32 ChannelMask;      //  bytes=4
        public Guid GuidSubFormat;      //  bytes=16
        //
        // data                             bytes=8
        //
        public UInt32 Subchunk2ID;      //  bytes=4
        public UInt32 Subchunk2Size;    //  bytes=4
        //
        // info
        //
        public bool IsExtensible;
        public int HeaderSize;  // normal WAV = 44 bytes, extensible WAV = 44 + 24 = 68 bytes (without extra chunks)
        public double Duration; // duration in seconds
        public long TotalSamples;
        //
        // channel types
        //
        public static readonly IList<WavChannel> WavChannelTypes = new ReadOnlyCollection<WavChannel>
            (
            new[] 
            {
                 new WavChannel ("Mono",                    "M",   0                                                ),
                 new WavChannel ("Left",                    "L",   0                                                ),
                 new WavChannel ("Right",                   "R",   0                                                )
            });

        //
        // multi-channel types
        //
        public static readonly IList<WavChannel> WavMultiChannelTypes = new ReadOnlyCollection<WavChannel>
            (
            new[] 
            {
                 new WavChannel ("Front Left",              "FL",   WavChannelMask.SPEAKER_FRONT_LEFT               ),
                 new WavChannel ("Front Right",             "FR",   WavChannelMask.SPEAKER_FRONT_RIGHT              ),
                 new WavChannel ("Front Center",            "FC",   WavChannelMask.SPEAKER_FRONT_CENTER             ),
                 new WavChannel ("Low Frequency",           "LF",   WavChannelMask.SPEAKER_LOW_FREQUENCY            ),
                 new WavChannel ("Back Left",               "BL",   WavChannelMask.SPEAKER_BACK_LEFT                ),
                 new WavChannel ("Back Right",              "BR",   WavChannelMask.SPEAKER_BACK_RIGHT               ),
                 new WavChannel ("Front Left of Center",    "FLC",  WavChannelMask.SPEAKER_FRONT_LEFT_OF_CENTER     ),
                 new WavChannel ("Front Right of Center",   "FRC",  WavChannelMask.SPEAKER_FRONT_RIGHT_OF_CENTER    ),
                 new WavChannel ("Back Center",             "BC",   WavChannelMask.SPEAKER_BACK_CENTER              ),
                 new WavChannel ("Side Left",               "SL",   WavChannelMask.SPEAKER_SIDE_LEFT                ),
                 new WavChannel ("Side Right",              "SR",   WavChannelMask.SPEAKER_SIDE_RIGHT               ),
                 new WavChannel ("Top Center",              "TC",   WavChannelMask.SPEAKER_TOP_CENTER               ),
                 new WavChannel ("Top Front Left",          "TFL",  WavChannelMask.SPEAKER_TOP_FRONT_LEFT           ),
                 new WavChannel ("Top Front Center",        "TFC",  WavChannelMask.SPEAKER_TOP_FRONT_CENTER         ),
                 new WavChannel ("Top Front Right",         "TFR",  WavChannelMask.SPEAKER_TOP_FRONT_RIGHT          ),
                 new WavChannel ("Top Back Left",           "TBL",  WavChannelMask.SPEAKER_TOP_BACK_LEFT            ),
                 new WavChannel ("Top Back Center",         "TBC",  WavChannelMask.SPEAKER_TOP_BACK_CENTER          ),
                 new WavChannel ("Top Back Right",          "TBR",  WavChannelMask.SPEAKER_TOP_BACK_RIGHT           )
            });

        // WAVEFORMATEXTENSIBLE sub-formats
        public static readonly Guid subTypePCM = new Guid("00000001-0000-0010-8000-00aa00389b71");
        public static readonly Guid subTypeIEEE_FLOAT = new Guid("00000003-0000-0010-8000-00aa00389b71");

        // ToString()
        public override string ToString()
        {
            return String.Format(
                "[WAVE]\n" +
                "ChunkID:\t\t{0}\n" +
                "ChunkSize:\t{1}\n" +
                "Format:\t\t{2}\n" +
                "[fmt]\n" +
                "Subchunk1ID:\t{3}\n" +
                "Subchunk1Size:\t{4}\n" +
                "AudioFormat:\t{5}\n" +
                "NumChannels:\t{6}\n" +
                "SampleRate:\t{7}\n" +
                "ByteRate:\t\t{8}\n" +
                "BlockAlign:\t{9}\n" +
                "BitsPerSample:\t{10}\n" +
                "[extra]\n" +
                "ExtraParamSize:\t{11}\n" +
                "[extensible]\n"+
                "Samples:\t\t{12}\n" +
                "ChannelMask:\t{13}\n" +
                "GuidSubFormat:\t{14}\n" +
                "[data]\n" +
                "Subchunk2ID:\t{15}\n" +
                "Subchunk2Size:\t{16}\n" +
                "[info]\n" +
                "IsExtensible:\t{17}\n" +
                "HeaderSize:\t{18}\n" +
                "Duration:\t\t{19}\n" +
                "TotalSamples:\t{20}",
                Encoding.ASCII.GetString(BitConverter.GetBytes(ChunkID)),
                ChunkSize,
                Encoding.ASCII.GetString(BitConverter.GetBytes(Format)),
                Encoding.ASCII.GetString(BitConverter.GetBytes(Subchunk1ID)),
                Subchunk1Size,
                (AudioFormat == 1) ? "1 : PCM" : ((AudioFormat == (UInt16) 0xFFFE) ? "0xFFFE : WAVEFORMATEXTENSIBLE" : AudioFormat.ToString()),
                NumChannels,
                SampleRate,
                ByteRate,
                BlockAlign,
                BitsPerSample,
                ExtraParamSize,
                Samples,
                ChannelMask,
                GuidSubFormat.ToString() + " : " + ((GuidSubFormat == subTypePCM) ? "PCM" : ((GuidSubFormat == subTypeIEEE_FLOAT) ? "IEEE FLOAT" : "Unknown")),
                Encoding.ASCII.GetString(BitConverter.GetBytes(Subchunk2ID)),
                Subchunk2Size,
                IsExtensible,
                HeaderSize,
                TimeSpan.FromSeconds(Duration),
                TotalSamples);
        }
    }

    public class WavFile
    {
        /// <summary>
        /// Read WAV file header
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static WavFileHeader ReadFileHeader(System.IO.FileStream f)
        {
            WavFileHeader h = new WavFileHeader();
            h.HeaderSize = 0;

            // read WAV header
            System.IO.BinaryReader b = new System.IO.BinaryReader(f);

            // WAVE
            h.ChunkID = b.ReadUInt32();         // 0x46464952, "RIFF"
            h.ChunkSize = b.ReadUInt32();       // 36 + SubChunk2Size, 4 + (8 + SubChunk1Size) + (8 + SubChunk2Size)
            h.Format = b.ReadUInt32();          // 0x45564157, "WAVE"

            h.HeaderSize += 12;

            // fmt
            h.Subchunk1ID = b.ReadUInt32();     // 0x20746d66, "fmt "
            h.Subchunk1Size = b.ReadUInt32();   // 16 for PCM, 40 for WAVEFORMATEXTENSIBLE
            h.AudioFormat = b.ReadUInt16();     // PCM = 1, WAVEFORMATEXTENSIBLE.SubFormat = 0xFFFE
            h.NumChannels = b.ReadUInt16();     // Mono = 1, Stereo = 2, etc.
            h.SampleRate = b.ReadUInt32();      // 8000, 44100, etc.
            h.ByteRate = b.ReadUInt32();        // SampleRate * NumChannels * BitsPerSample/8
            h.BlockAlign = b.ReadUInt16();      // NumChannels * BitsPerSample/8
            h.BitsPerSample = b.ReadUInt16();   // 8 bits = 8, 16 bits = 16, etc.

            h.HeaderSize += 24;

            // read PCM data or extensible data if exists
            if (h.Subchunk1Size == 16 && h.AudioFormat == 1) // PCM
            {
                h.IsExtensible = false;

                // Note: 8-bit samples are stored as unsigned bytes, ranging from 0 to 255. 16-bit samples are stored as 2's-complement signed integers, ranging from -32768 to 32767.
                // data
                h.Subchunk2ID = b.ReadUInt32();     // 0x61746164, "data"
                h.Subchunk2Size = b.ReadUInt32();   // NumSamples * NumChannels * BitsPerSample/8

                h.HeaderSize += 8;
            }
            else if (h.Subchunk1Size > 16 && h.AudioFormat == 0xFFFE) // WAVEFORMATEXTENSIBLE
            {
                // read WAVEFORMATEXTENSIBLE
                h.ExtraParamSize = b.ReadUInt16();
                h.HeaderSize += 2;

                if (h.ExtraParamSize == 22) // if cbSize is set to 22 => WAVEFORMATEXTENSIBLE
                {
                    h.IsExtensible = true;

                    //union {
                    //    WORD wValidBitsPerSample; // bits of precision
                    //    WORD wSamplesPerBlock;    // valid if wBitsPerSample==0
                    //    WORD wReserved;           // If neither applies, set to zero.
                    //} Samples;
                    h.Samples = b.ReadUInt16();

                    // DWORD dwChannelMask; which channels are present in stream
                    h.ChannelMask = b.ReadUInt32();

                    // GUID SubFormat
                    byte[] SubFormat = b.ReadBytes(16);

                    h.HeaderSize += 22;

                    // check sub-format
                    h.GuidSubFormat = new Guid(SubFormat);
                    if (h.GuidSubFormat != WavFileHeader.subTypePCM && h.GuidSubFormat != WavFileHeader.subTypeIEEE_FLOAT)
                    {
                        throw new Exception(String.Format("Not supported WAV file type: {0}", h.GuidSubFormat));
                    }

                    // find "data" chunk
                    while (b.PeekChar() != -1)
                    {
                        UInt32 chunk = b.ReadUInt32();
                        h.HeaderSize += 4;

                        if (chunk == 0x61746164) // "data" chunk
                        {
                            h.Subchunk2ID = chunk;              // 0x61746164, "data"
                            h.Subchunk2Size = b.ReadUInt32();   // NumSamples * NumChannels * BitsPerSample/8

                            h.HeaderSize += 4;

                            break;
                        }
                        else
                        {
                            // read other non "data" chunks
                            UInt32 chunkSize = b.ReadUInt32();

                            h.HeaderSize += 4;

                            string chunkName = Encoding.ASCII.GetString(BitConverter.GetBytes(chunk));
                            byte[] chunkData = b.ReadBytes((int)chunkSize);

                            h.HeaderSize += (int)chunkSize;
                        }
                    }
                }
                else
                {
                    throw new Exception("Not supported WAV file header.");
                }
            }
            else
            {
                throw new Exception("Not supported WAV file header.");
            }

            // calculate number of total samples
            h.TotalSamples = (long)((double)h.Subchunk2Size / ((double)h.NumChannels * (double)h.BitsPerSample / 8));

            // calculate dureation in seconds
            h.Duration = (1 / (double)h.SampleRate) * (double)h.TotalSamples;

            return h;
        }

        /// <summary>
        /// Write WAV file header
        /// </summary>
        /// <param name="f"></param>
        /// <param name="h"></param>
        public static void WriteFileHeader(System.IO.FileStream f, WavFileHeader h)
        {
            // write WAV header
            System.IO.BinaryWriter b = new System.IO.BinaryWriter(f);

            // WAVE
            b.Write((UInt32)0x46464952); // 0x46464952, "RIFF"
            b.Write(h.ChunkSize);
            b.Write((UInt32)0x45564157); // 0x45564157, "WAVE"

            // fmt
            b.Write((UInt32)0x20746d66); // 0x20746d66, "fmt "
            b.Write(h.Subchunk1Size);
            b.Write(h.AudioFormat);
            b.Write(h.NumChannels);
            b.Write(h.SampleRate);
            b.Write(h.ByteRate);
            b.Write(h.BlockAlign);
            b.Write(h.BitsPerSample);

            // write PCM data or extensible data if exists
            if (h.Subchunk1Size == 16 && h.AudioFormat == 1) // PCM
            {
                b.Write((UInt32)0x61746164); // 0x61746164, "data"
                b.Write(h.Subchunk2Size);
            }
            else if (h.Subchunk1Size > 16 && h.AudioFormat == 0xFFFE) // WAVEFORMATEXTENSIBLE
            {
                // write WAVEFORMATEXTENSIBLE
                b.Write(h.ExtraParamSize);

                b.Write(h.Samples);
                b.Write(h.ChannelMask);
                b.Write(h.GuidSubFormat.ToByteArray());

                b.Write((UInt32)0x61746164); // 0x61746164, "data"
                b.Write(h.Subchunk2Size);
            }
            else
            {
                throw new Exception("Not supported WAV file header.");
            }
        }

        /// <summary>
        /// Get mono WAV file header from multi-channel WAV file
        /// </summary>
        /// <param name="h"></param>
        /// <returns></returns>
        public static WavFileHeader GetMonoWavFileHeader(WavFileHeader h)
        {
            // each mono output file has the same header
            WavFileHeader monoFileHeader = new WavFileHeader();

            // WAVE
            monoFileHeader.ChunkID = (UInt32)0x46464952; // 0x46464952, "RIFF"
            monoFileHeader.ChunkSize = 36 + (h.Subchunk2Size / h.NumChannels); // 36 + SubChunk2Size, 4 + (8 + SubChunk1Size) + (8 + SubChunk2Size)
            monoFileHeader.Format = (UInt32)0x45564157; // 0x45564157, "WAVE"

            // fmt
            monoFileHeader.Subchunk1ID = (UInt32)0x20746d66; // 0x20746d66, "fmt "
            monoFileHeader.Subchunk1Size = 16; // 16 for PCM, 40 for WAVEFORMATEXTENSIBLE
            monoFileHeader.AudioFormat = (UInt16)1; // PCM = 1, WAVEFORMATEXTENSIBLE.SubFormat = 0xFFFE
            monoFileHeader.NumChannels = (UInt16)1; // Mono = 1, Stereo = 2, etc.
            monoFileHeader.SampleRate = h.SampleRate; // 8000, 44100, etc.
            monoFileHeader.ByteRate = (UInt32)((h.SampleRate * 1 * h.BitsPerSample) / 8); // SampleRate * NumChannels * BitsPerSample/8
            monoFileHeader.BlockAlign = (UInt16)((1 * h.BitsPerSample) / 8); // NumChannels * BitsPerSample/8
            monoFileHeader.BitsPerSample = h.BitsPerSample; // 8 bits = 8, 16 bits = 16, etc.

            // extensible
            monoFileHeader.ExtraParamSize = (UInt16)0;
            monoFileHeader.ChannelMask = (UInt32)0;
            monoFileHeader.GuidSubFormat = new Guid();

            // data
            monoFileHeader.Subchunk2ID = (UInt32)0x61746164; // 0x61746164, "data"
            monoFileHeader.Subchunk2Size = (h.Subchunk2Size / h.NumChannels); // NumSamples * NumChannels * BitsPerSample/8

            // info
            monoFileHeader.IsExtensible = false;
            monoFileHeader.HeaderSize = 44;
            monoFileHeader.TotalSamples = (long)((double)monoFileHeader.Subchunk2Size / ((double)monoFileHeader.NumChannels * (double)monoFileHeader.BitsPerSample / 8));
            monoFileHeader.Duration = (1 / (double)monoFileHeader.SampleRate) * (double)monoFileHeader.TotalSamples;

            return monoFileHeader;
        }
    }
}
