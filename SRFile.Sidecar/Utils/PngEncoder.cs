using System;
using System.IO;
using System.IO.Compression;

namespace SRFile.Sidecar.Utils
{
    public static class PngEncoder
    {
        private static readonly byte[] PngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly uint[] CrcTable = new uint[256];

        static PngEncoder()
        {
            for (uint i = 0; i < 256; i++)
            {
                uint c = i;
                for (int k = 0; k < 8; k++)
                {
                    if ((c & 1) != 0)
                        c = 0xEDB88320 ^ (c >> 1);
                    else
                        c >>= 1;
                }
                CrcTable[i] = c;
            }
        }

        private static uint UpdateCrc(uint crc, byte[] buffer, int offset, int length)
        {
            uint c = crc ^ 0xFFFFFFFF;
            for (int i = offset; i < offset + length; i++)
            {
                c = CrcTable[(c ^ buffer[i]) & 0xFF] ^ (c >> 8);
            }
            return c ^ 0xFFFFFFFF;
        }

        private static void WriteBigEndianUint(Stream stream, uint value)
        {
            stream.WriteByte((byte)((value >> 24) & 0xFF));
            stream.WriteByte((byte)((value >> 16) & 0xFF));
            stream.WriteByte((byte)((value >> 8) & 0xFF));
            stream.WriteByte((byte)(value & 0xFF));
        }

        private static void WriteChunk(Stream stream, string type, byte[] data)
        {
            byte[] typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
            WriteBigEndianUint(stream, (uint)data.Length);
            stream.Write(typeBytes, 0, 4);
            if (data.Length > 0)
            {
                stream.Write(data, 0, data.Length);
            }

            uint crc = UpdateCrc(0, typeBytes, 0, 4);
            if (data.Length > 0)
            {
                crc = UpdateCrc(crc, data, 0, data.Length);
            }
            WriteBigEndianUint(stream, crc);
        }

        public static byte[] EncodeRgba(byte[] rgbaPixels, int width, int height)
        {
            if (rgbaPixels == null || rgbaPixels.Length < width * height * 4)
            {
                throw new ArgumentException("Pixel buffer too small or null for specified dimensions.");
            }

            using var outStream = new MemoryStream();
            outStream.Write(PngHeader, 0, PngHeader.Length);

            // 1. IHDR Chunk (13 bytes)
            byte[] ihdr = new byte[13];
            ihdr[0] = (byte)((width >> 24) & 0xFF);
            ihdr[1] = (byte)((width >> 16) & 0xFF);
            ihdr[2] = (byte)((width >> 8) & 0xFF);
            ihdr[3] = (byte)(width & 0xFF);
            ihdr[4] = (byte)((height >> 24) & 0xFF);
            ihdr[5] = (byte)((height >> 16) & 0xFF);
            ihdr[6] = (byte)((height >> 8) & 0xFF);
            ihdr[7] = (byte)(height & 0xFF);
            ihdr[8] = 8; // Bit depth: 8 bits per channel
            ihdr[9] = 6; // Color type: 6 = RGBA
            ihdr[10] = 0; // Compression: Deflate
            ihdr[11] = 0; // Filter: Adaptive
            ihdr[12] = 0; // Interlace: None
            WriteChunk(outStream, "IHDR", ihdr);

            // 2. IDAT Chunk: Scanlines with Filter type 0 (None)
            int rowBytes = width * 4;
            byte[] rawScanlines = new byte[height * (1 + rowBytes)];
            int srcOffset = 0;
            int dstOffset = 0;
            for (int y = 0; y < height; y++)
            {
                rawScanlines[dstOffset++] = 0; // Filter byte: 0 (None)
                Buffer.BlockCopy(rgbaPixels, srcOffset, rawScanlines, dstOffset, rowBytes);
                srcOffset += rowBytes;
                dstOffset += rowBytes;
            }

            byte[] compressedData;
            using (var compMs = new MemoryStream())
            {
                using (var zlib = new ZLibStream(compMs, CompressionLevel.Optimal, leaveOpen: true))
                {
                    zlib.Write(rawScanlines, 0, rawScanlines.Length);
                }
                compressedData = compMs.ToArray();
            }
            WriteChunk(outStream, "IDAT", compressedData);

            // 3. IEND Chunk
            WriteChunk(outStream, "IEND", Array.Empty<byte>());

            return outStream.ToArray();
        }
    }
}
