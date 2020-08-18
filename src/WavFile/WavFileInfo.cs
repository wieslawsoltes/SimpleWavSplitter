using System;
using System.IO;
using System.Text;

namespace WavFile
{
    /// <summary>
    /// Read/Write WAV file header information.
    /// </summary>
    public static class WavFileInfo
    {
        /// <summary>
        /// Read WAV file header.
        /// </summary>
        /// <param name="fs">The file stream.</param>
        /// <returns>The new instance of the <see cref="WavFileHeader"/> struct.</returns>
        public static WavFileHeader ReadFileHeader(FileStream fs)
        {
            var h = new WavFileHeader()
            {
                HeaderSize = 0
            };

            // Read WAV header.
            var b = new BinaryReader(fs);

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

            // Read PCM data or extensible data if exists.
            if (h.Subchunk1Size == 16 && h.AudioFormat == 1)
            {
                // PCM
                h.IsExtensible = false;

                // Find "data" chunk.
                while (b.PeekChar() != -1)
                {
                    UInt32 chunk = b.ReadUInt32();
                    h.HeaderSize += 4;

                    if (chunk == 0x61746164)
                    {
                        // Note: 8-bit samples are stored as unsigned bytes, ranging from 0 to 255. 16-bit samples are stored as 2's-complement signed integers, ranging from -32768 to 32767.
                        // "data" chunk
                        h.Subchunk2ID = chunk;              // 0x61746164, "data"
                        h.Subchunk2Size = b.ReadUInt32();   // NumSamples * NumChannels * BitsPerSample/8

                        h.HeaderSize += 4;

                        break;
                    }
                    else
                    {
                        // Read other non "data" chunks.
                        UInt32 chunkSize = b.ReadUInt32();

                        h.HeaderSize += 4;

                        string chunkName = Encoding.ASCII.GetString(BitConverter.GetBytes(chunk));
                        byte[] chunkData = b.ReadBytes((int)chunkSize);

                        h.HeaderSize += (int)chunkSize;
                    }
                }
            }
            else if (h.Subchunk1Size > 16 && h.AudioFormat == 0xFFFE)
            {
                // read WAVEFORMATEXTENSIBLE
                h.ExtraParamSize = b.ReadUInt16();
                h.HeaderSize += 2;

                if (h.ExtraParamSize == 22)
                {
                    // if cbSize is set to 22 => WAVEFORMATEXTENSIBLE
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
                    byte[] subFormat = b.ReadBytes(16);

                    h.HeaderSize += 22;

                    // Check sub-format.
                    h.GuidSubFormat = new Guid(subFormat);
                    if (h.GuidSubFormat != WavFileHeader.SubTypePCM && h.GuidSubFormat != WavFileHeader.SubTypeIEEE_FLOAT)
                    {
                        throw new Exception(String.Format("Not supported WAV file type: {0}", h.GuidSubFormat));
                    }

                    // Find "data" chunk.
                    while (b.PeekChar() != -1)
                    {
                        UInt32 chunk = b.ReadUInt32();
                        h.HeaderSize += 4;

                        if (chunk == 0x61746164)
                        {
                            // "data" chunk
                            h.Subchunk2ID = chunk;              // 0x61746164, "data"
                            h.Subchunk2Size = b.ReadUInt32();   // NumSamples * NumChannels * BitsPerSample/8

                            h.HeaderSize += 4;

                            break;
                        }
                        else
                        {
                            // Read other non "data" chunks.
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

            // Calculate number of total samples.
            h.TotalSamples = (long)((double)h.Subchunk2Size / ((double)h.NumChannels * (double)h.BitsPerSample / 8));

            // Calculate duration in seconds.
            h.Duration = (1 / (double)h.SampleRate) * (double)h.TotalSamples;

            return h;
        }

        /// <summary>
        /// Write WAV file header.
        /// </summary>
        /// <param name="fs">The file stream.</param>
        /// <param name="h">The wav file header.</param>
        public static void WriteFileHeader(FileStream fs, WavFileHeader h)
        {
            // Write WAV header.
            var b = new BinaryWriter(fs);

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

            // Write PCM data or extensible data if exists.
            if (h.Subchunk1Size == 16 && h.AudioFormat == 1)
            {
                // PCM
                b.Write((UInt32)0x61746164); // 0x61746164, "data"
                b.Write(h.Subchunk2Size);
            }
            else if (h.Subchunk1Size > 16 && h.AudioFormat == 0xFFFE)
            {
                // Write WAVEFORMATEXTENSIBLE
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
        /// Get mono WAV file header from multi-channel WAV file.
        /// </summary>
        /// <param name="h">The wav file header.</param>
        /// <returns>The new instance of the <see cref="WavFileHeader"/> struct.</returns>
        public static WavFileHeader GetMonoWavFileHeader(WavFileHeader h)
        {
            // Each mono output file has the same header.
            var mh = new WavFileHeader();

            // WAVE
            mh.ChunkID = (UInt32)0x46464952; // 0x46464952, "RIFF"
            mh.ChunkSize = 36 + (h.Subchunk2Size / h.NumChannels); // 36 + SubChunk2Size, 4 + (8 + SubChunk1Size) + (8 + SubChunk2Size)
            mh.Format = (UInt32)0x45564157; // 0x45564157, "WAVE"

            // fmt
            mh.Subchunk1ID = (UInt32)0x20746d66; // 0x20746d66, "fmt "
            mh.Subchunk1Size = 16; // 16 for PCM, 40 for WAVEFORMATEXTENSIBLE
            mh.AudioFormat = (UInt16)1; // PCM = 1, WAVEFORMATEXTENSIBLE.SubFormat = 0xFFFE
            mh.NumChannels = (UInt16)1; // Mono = 1, Stereo = 2, etc.
            mh.SampleRate = h.SampleRate; // 8000, 44100, etc.
            mh.ByteRate = (UInt32)((h.SampleRate * 1 * h.BitsPerSample) / 8); // SampleRate * NumChannels * BitsPerSample/8
            mh.BlockAlign = (UInt16)((1 * h.BitsPerSample) / 8); // NumChannels * BitsPerSample/8
            mh.BitsPerSample = h.BitsPerSample; // 8 bits = 8, 16 bits = 16, etc.

            // extensible
            mh.ExtraParamSize = (UInt16)0;
            mh.ChannelMask = (UInt32)0;
            mh.GuidSubFormat = new Guid();

            // data
            mh.Subchunk2ID = (UInt32)0x61746164; // 0x61746164, "data"
            mh.Subchunk2Size = h.Subchunk2Size / h.NumChannels; // NumSamples * NumChannels * BitsPerSample/8

            // info
            mh.IsExtensible = false;
            mh.HeaderSize = 44;
            mh.TotalSamples = (long)((double)mh.Subchunk2Size / ((double)mh.NumChannels * (double)mh.BitsPerSample / 8));
            mh.Duration = (1 / (double)mh.SampleRate) * (double)mh.TotalSamples;

            return mh;
        }
    }
}
