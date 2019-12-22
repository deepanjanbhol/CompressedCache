using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace CompressedCache
{
    /// <summary>
    /// Compress using GZIP.
    /// </summary>
    public class GzipCompression
    {
        /// <summary>
        /// Decompress input string.
        /// </summary>
        /// <param name="compressed">Input compressed byte array.</param>
        /// <returns>Decompressed string.</returns>
        public static string Decompress(byte[] compressed)
        {
            byte[] decompressed = DecompressToBytes(compressed);
            return Encoding.UTF8.GetString(decompressed);
        }

        /// <summary>
        /// Compress input string.
        /// </summary>
        /// <param name="input">Input string.</param>
        /// <returns>compressed byte array.</returns>
        public static byte[] Compress(string input)
        {
            byte[] encoded = Encoding.UTF8.GetBytes(input);
            byte[] compressed = CompressToBytes(encoded);
            return compressed;
        }

        /// <summary>
        /// Decompress input byte array
        /// </summary>
        /// <param name="input">Input byte array</param>
        /// <returns>decompressed byte array</returns>
        public static byte[] DecompressToBytes(byte[] input)
        {
            using (var source = new MemoryStream(input))
            {
                byte[] lengthBytes = new byte[4];
                source.Read(lengthBytes, 0, 4);

                var length = BitConverter.ToInt32(lengthBytes, 0);
                using (var decompressionStream = new GZipStream(source, CompressionMode.Decompress))
                {
                    var result = new byte[length];
                    decompressionStream.Read(result, 0, length);
                    return result;
                }
            }
        }

        /// <summary>
        /// Compress input byte array
        /// </summary>
        /// <param name="input">Input byte array</param>
        /// <returns>Compressed byte array</returns>
        public static byte[] CompressToBytes(byte[] input)
        {
            using (var result = new MemoryStream())
            {
                var lengthBytes = BitConverter.GetBytes(input.Length);
                result.Write(lengthBytes, 0, 4);

                using (var compressionStream = new GZipStream(result, CompressionMode.Compress))
                {
                    compressionStream.Write(input, 0, input.Length);
                    compressionStream.Flush();
                }

                return result.ToArray();
            }
        }
    }
}
