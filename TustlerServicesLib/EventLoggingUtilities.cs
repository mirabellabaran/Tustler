using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TustlerServicesLib
{
    public static class EventLoggingUtilities
    {
        /// <summary>
        /// Convert a list of blocks of bytes (TaskEvent logging format) into an array of bytes (normally for writing to a file)
        /// </summary>
        /// <param name="blocks">An array of blocks where each block sequence of bytes encodes a single TaskEvent as a standalone JSON document</param>
        /// <returns>An array of bytes that encode event logging blocks as pairs of [length, block data]</length></returns>
        public static byte[] BlockArrayToByteArray(byte[][] blocks)
        {
            var arrayLength = blocks.Sum(block => block.Length) + (blocks.Length * 4);
            byte[] result = new byte[arrayLength];
            var span = new Span<byte>(result);
            var start = 0;

            foreach (var data in blocks)
            {
                var header = BitConverter.GetBytes(data.Length);
                var destination = span.Slice(start, 4);
                new Span<byte>(header).CopyTo(destination);
                start += 4;

                destination = span.Slice(start, data.Length);
                new Span<byte>(data).CopyTo(destination);
                start += data.Length;
            }

            return result;
        }

        /// <summary>
        /// Convert an array of bytes into a list of blocks of bytes (TaskEvent logging format)
        /// </summary>
        /// <param name="data">Data encoding event logging blocks (perhaps from a binary file)</param>
        /// <returns>A list of blocks where each block encodes a single TaskEvent as a standalone JSON document</returns>
        public static List<byte[]> ByteArrayToBlockArray(byte[] data)
        {
            var blocks = new List<byte[]>(30);
            var span = new ReadOnlySpan<byte>(data);
            var start = 0;

            while (start + 4 < data.Length)
            {
                var slice = span.Slice(start, 4);
                var blockLength = BitConverter.ToInt32(slice);
                start += 4;

                slice = span.Slice(start, blockLength);
                blocks.Add(slice.ToArray());
                start += blockLength;
            }

            return blocks;
        }
    }
}
