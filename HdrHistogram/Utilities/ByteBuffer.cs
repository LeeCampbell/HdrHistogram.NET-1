/*
 * Written by Matt Warren, and released to the public domain,
 * as explained at
 * http://creativecommons.org/publicdomain/zero/1.0/
 *
 * This is a .NET port of the original Java version, which was written by
 * Gil Tene as described in
 * https://github.com/HdrHistogram/HdrHistogram
 */

using System;
using System.Buffers.Binary;
#if NETSTANDARD2_0
using System.Net;
#endif

namespace HdrHistogram.Utilities
{
    // See http://stackoverflow.com/questions/1261543/equivalent-of-javas-bytebuffer-puttype-in-c-sharp
    // and http://stackoverflow.com/questions/18040012/what-is-the-equivalent-of-javas-bytebuffer-wrap-in-c
    // and http://stackoverflow.com/questions/1261543/equivalent-of-javas-bytebuffer-puttype-in-c-sharp
    // Java version http://docs.oracle.com/javase/7/docs/api/java/nio/ByteBuffer.html
    /// <summary>
    /// A byte buffer that tracks position and allows reads and writes of 32 and 64 bit integer values.
    /// </summary>
    public sealed class ByteBuffer
    {
        private readonly byte[] _internalBuffer;

        /// <summary>
        /// Creates a <see cref="ByteBuffer"/> with a specified capacity in bytes.
        /// </summary>
        /// <param name="bufferCapacity">The capacity of the buffer in bytes</param>
        /// <returns>A newly created <see cref="ByteBuffer"/>.</returns>
        public static ByteBuffer Allocate(int bufferCapacity)
        {
            return new ByteBuffer(bufferCapacity);
        }

        /// <summary>
        /// Creates a <see cref="ByteBuffer"/> loaded with the provided byte array.
        /// </summary>
        /// <param name="source">The source byte array to load the buffer with.</param>
        /// <returns>A newly created <see cref="ByteBuffer"/>.</returns>
        public static ByteBuffer Allocate(byte[] source)
        {
            var buffer = new ByteBuffer(source.Length);
            Buffer.BlockCopy(source, 0, buffer._internalBuffer, buffer.Position, source.Length);
            return buffer;
        }

        private ByteBuffer(int bufferCapacity)
        {
            _internalBuffer = new byte[bufferCapacity];
            Position = 0;
        }

        /// <summary>
        /// The buffer's current position in the underlying byte array
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// Returns the capacity of the <see cref="ByteBuffer"/>
        /// </summary>
        /// <returns>The length of the internal byte array.</returns>
        public int Capacity()
        {
            return _internalBuffer.Length;
        }

        /// <summary>
        /// The remaining capacity.
        /// </summary>
        /// <returns>The number of bytes between the current position and the underlying byte array length.</returns>
        public int Remaining()
        {
            return Capacity() - Position;
        }

        /// <summary>
        /// Reads from the provided <see cref="System.IO.Stream"/>, into the buffer.
        /// </summary>
        /// <param name="source">The source stream to read from.</param>
        /// <param name="length">The number of bytes to read.</param>
        /// <returns>The number of bytes read.</returns>
        public int ReadFrom(System.IO.Stream source, int length)
        {
            int totalRead = 0;
            while (totalRead < length)
            {
                int bytesRead = source.Read(_internalBuffer, Position + totalRead, length - totalRead);
                if (bytesRead == 0) break;
                totalRead += bytesRead;
            }
            return totalRead;
        }

        /// <summary>
        /// Gets the current byte and advances the position by one.
        /// </summary>
        /// <returns>The byte at the current position.</returns>
        public byte Get()
        {
            return _internalBuffer[Position++];
        }

        /// <summary>
        /// Gets the 16 bit integer (<seealso cref="short"/>) at the current position, and then advances by two.
        /// </summary>
        /// <returns>The value of the <see cref="short"/> at the current position.</returns>
        public short GetShort()
        {
#if NETSTANDARD2_0
            var shortValue = IPAddress.HostToNetworkOrder(BitConverter.ToInt16(_internalBuffer, Position));
#else
            var shortValue = BinaryPrimitives.ReadInt16BigEndian(new ReadOnlySpan<byte>(_internalBuffer, Position, sizeof(short)));
#endif
            Position += sizeof(short);
            return shortValue;
        }

        /// <summary>
        /// Gets the 32 bit integer (<seealso cref="int"/>) at the current position, and then advances by four.
        /// </summary>
        /// <returns>The value of the <see cref="int"/> at the current position.</returns>
        public int GetInt()
        {
#if NETSTANDARD2_0
            var intValue = IPAddress.HostToNetworkOrder(BitConverter.ToInt32(_internalBuffer, Position));
#else
            var intValue = BinaryPrimitives.ReadInt32BigEndian(new ReadOnlySpan<byte>(_internalBuffer, Position, sizeof(int)));
#endif
            Position += sizeof(int);
            return intValue;
        }

        /// <summary>
        /// Gets the 64 bit integer (<seealso cref="long"/>) at the current position, and then advances by eight.
        /// </summary>
        /// <returns>The value of the <see cref="long"/> at the current position.</returns>
        public long GetLong()
        {
#if NETSTANDARD2_0
            var longValue = IPAddress.HostToNetworkOrder(BitConverter.ToInt64(_internalBuffer, Position));
#else
            var longValue = BinaryPrimitives.ReadInt64BigEndian(new ReadOnlySpan<byte>(_internalBuffer, Position, sizeof(long)));
#endif
            Position += sizeof(long);
            return longValue;
        }

        /// <summary>
        /// Gets the double floating point number (<seealso cref="double"/>) at the current position, and then advances by eight.
        /// </summary>
        /// <returns>The value of the <see cref="double"/> at the current position.</returns>
        public double GetDouble()
        {
#if NETSTANDARD2_0
            var doubleValue = BitConverter.Int64BitsToDouble(ReadInt64BigEndian(_internalBuffer, Position));
#else
            var doubleValue = BinaryPrimitives.ReadDoubleBigEndian(new ReadOnlySpan<byte>(_internalBuffer, Position, sizeof(double)));
#endif
            Position += sizeof(double);
            return doubleValue;
        }

#if NETSTANDARD2_0
        private static long ReadInt64BigEndian(byte[] buffer, int startIndex)
        {
            long ret = 0;
            for (int i = 0; i < 8; i++)
            {
                ret = unchecked((ret << 8) | buffer[startIndex + i]);
            }
            return ret;
        }
#endif

        /// <summary>
        /// Writes a byte value to the current position, and advances the position by one.
        /// </summary>
        /// <param name="value">The byte value to write.</param>
        public void Put(byte value)
        {
            _internalBuffer[Position++] = value;
        }

        /// <summary>
        /// Sets the bytes at the current position to the value of the passed value, and advances the position.
        /// </summary>
        /// <param name="value">The value to set the current position to.</param>
        public void PutInt(int value)
        {
#if NETSTANDARD2_0
            var intAsBytes = BitConverter.GetBytes(IPAddress.NetworkToHostOrder(value));
            Array.Copy(intAsBytes, 0, _internalBuffer, Position, intAsBytes.Length);
            Position += intAsBytes.Length;
#else
            BinaryPrimitives.WriteInt32BigEndian(new Span<byte>(_internalBuffer, Position, sizeof(int)), value);
            Position += sizeof(int);
#endif
        }

        /// <summary>
        /// Sets the bytes at the provided position to the value of the passed value, and does not advance the position.
        /// </summary>
        /// <param name="index">The position to set the value at.</param>
        /// <param name="value">The value to set.</param>
        /// <remarks>
        /// This can be useful for writing a value into an earlier placeholder e.g. a header property for storing the body length.
        /// </remarks>
        public void PutInt(int index, int value)
        {
#if NETSTANDARD2_0
            var intAsBytes = BitConverter.GetBytes(IPAddress.NetworkToHostOrder(value));
            Array.Copy(intAsBytes, 0, _internalBuffer, index, intAsBytes.Length);
#else
            BinaryPrimitives.WriteInt32BigEndian(new Span<byte>(_internalBuffer, index, sizeof(int)), value);
#endif
            // We don't increment the Position as this is an explicit write.
        }

        /// <summary>
        /// Sets the bytes at the current position to the value of the passed value, and advances the position.
        /// </summary>
        /// <param name="value">The value to set the current position to.</param>
        public void PutLong(long value)
        {
#if NETSTANDARD2_0
            var longAsBytes = BitConverter.GetBytes(IPAddress.NetworkToHostOrder(value));
            Array.Copy(longAsBytes, 0, _internalBuffer, Position, longAsBytes.Length);
            Position += longAsBytes.Length;
#else
            BinaryPrimitives.WriteInt64BigEndian(new Span<byte>(_internalBuffer, Position, sizeof(long)), value);
            Position += sizeof(long);
#endif
        }

        /// <summary>
        /// Sets the bytes at the current position to the value of the passed value, and advances the position.
        /// </summary>
        /// <param name="value">The value to set the current position to.</param>
        public void PutDouble(double value)
        {
#if NETSTANDARD2_0
            var doubleAsBytes = BitConverter.GetBytes(value);
            Array.Reverse(doubleAsBytes);
            Array.Copy(doubleAsBytes, 0, _internalBuffer, Position, doubleAsBytes.Length);
            Position += doubleAsBytes.Length;
#else
            BinaryPrimitives.WriteDoubleBigEndian(new Span<byte>(_internalBuffer, Position, sizeof(double)), value);
            Position += sizeof(double);
#endif
        }

        /// <summary>
        /// Gets a copy of the internal byte array.
        /// </summary>
        /// <returns>The a copy of the internal byte array.</returns>
        internal byte[] ToArray()
        {
            var copy = new byte[_internalBuffer.Length];
            Array.Copy(_internalBuffer, copy, _internalBuffer.Length);
            return copy;
        }

        internal void BlockCopy(Array src, int srcOffset, int dstOffset, int count)
        {
            Buffer.BlockCopy(src: src, srcOffset: srcOffset, dst: _internalBuffer, dstOffset: dstOffset, count: count);
            Position += count;
        }

        internal void BlockGet(Array target, int targetOffset, int sourceOffset, int count)
        {
            Buffer.BlockCopy(src: _internalBuffer, srcOffset: sourceOffset, dst: target, dstOffset: targetOffset, count: count);
        }
    }
}
