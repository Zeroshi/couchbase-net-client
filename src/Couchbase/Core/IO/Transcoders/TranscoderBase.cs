using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Couchbase.Core.IO.Converters;
using Couchbase.Core.IO.Operations;
using Couchbase.Core.IO.Serializers;
using Couchbase.Utils;

#nullable enable

namespace Couchbase.Core.IO.Transcoders
{
    public abstract class BaseTranscoder : ITypeTranscoder
    {
        private long _mutationSentinal = 0;
        private ITypeSerializer? _serializer;

        internal void MakeImmutable()
        {
            Interlocked.Increment(ref _mutationSentinal);
        }

        public abstract Flags GetFormat<T>(T value);

        public abstract void Encode<T>(Stream stream, T value, Flags flags, OpCode opcode);

        public abstract T? Decode<T>(ReadOnlyMemory<byte> buffer, Flags flags, OpCode opcode);

        // Optimization to store a reference to the buffered serializer if it is one.
        // Will never contain a DefaultSerializer because there is no performance gain using the IBufferedTypeSerializer
        // implementations on a DefaultSerializer. DefaultSerializer implements IBufferedTypeSerializer for compatibility,
        // but for the purposes of transcoding it just adds an unnecessary layer of indirection through an additional Stream.
        private IBufferedTypeSerializer? _bufferedTypeSerializer;

        public ITypeSerializer? Serializer
        {
            get => _serializer;
            set
            {
                if (Interlocked.Read(ref _mutationSentinal) > 0)
                {
                    throw new NotSupportedException("Cannot mutate an immutable Transcoder");
                }

                _serializer = value;
                _bufferedTypeSerializer = value is IBufferedTypeSerializer bufferedTypeSerializer and not DefaultSerializer
                    ? bufferedTypeSerializer
                    : null;
            }
        }

        /// <summary>
        /// Deserializes as json.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="buffer">The buffer.</param>
        /// <returns></returns>
        public virtual T? DeserializeAsJson<T>(ReadOnlyMemory<byte> buffer)
        {
            if (Serializer == null)
            {
                ThrowHelper.ThrowInvalidOperationException("A serializer is required to transcode JSON.");
            }

            return Serializer.Deserialize<T>(buffer);
        }

        /// <summary>
        /// Serializes as json.
        /// </summary>
        /// <param name="stream">The stream to receive the encoded value.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public void SerializeAsJson(Stream stream, object? value)
        {
            if (Serializer == null)
            {
                ThrowHelper.ThrowInvalidOperationException("A serializer is required to transcode JSON.");
            }

            Serializer.Serialize(stream, value);
        }

        /// <summary>
        /// Serializes as json.
        /// </summary>
        /// <typeparam name="T">Type of value to serialize.</typeparam>
        /// <param name="stream">The stream to receive the encoded value.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public void SerializeAsJson<T>(Stream stream, T value)
        {
            if (Serializer == null)
            {
                ThrowHelper.ThrowInvalidOperationException("A serializer is required to transcode JSON.");
            }

            var bufferedTypeSerializer = _bufferedTypeSerializer;
            if (bufferedTypeSerializer is not null && stream is IBufferWriter<byte> bufferWriter)
            {
                // Use the buffered serializer if available, typically on OperationBuilder.
                bufferedTypeSerializer.Serialize(bufferWriter, value);
                return;
            }

            // For .NET Core 3.1 and later, this prefers the Serialize<T> overload.
            Serializer.Serialize(stream, value);
        }

        /// <summary>
        /// Decodes the specified buffer as string.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns></returns>
        protected string? DecodeString(ReadOnlySpan<byte> buffer)
        {
            string? result = null;
            if (buffer.Length > 0)
            {
                result = ByteConverter.ToString(buffer);
            }
            return result;
        }

        /// <summary>
        /// Decodes the specified buffer as char.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns></returns>
        protected char DecodeChar(ReadOnlySpan<byte> buffer)
        {
            char result = default(char);
            if (buffer.Length > 0)
            {
                var str = ByteConverter.ToString(buffer);
                if (str.Length == 1)
                {
                    result = str[0];
                }
                else if (str.Length > 1)
                {
                    var msg = $"Can not convert string \"{str}\" to char";
                    throw new InvalidCastException(msg);
                }
            }
            return result;
        }

        /// <summary>
        /// Decodes the binary.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns></returns>
        protected byte[] DecodeBinary(ReadOnlySpan<byte> buffer)
        {
            if (buffer.Length == 0)
            {
                return Array.Empty<byte>();
            }

            var temp = new byte[buffer.Length];
            buffer.CopyTo(temp.AsSpan());
            return temp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void WriteHelper(Stream stream, ReadOnlySpan<byte> buffer)
        {
#if SPAN_SUPPORT
            stream.Write(buffer);
#else
            if (stream is IBufferWriter<byte> bufferWriter)
            {
                // OperationBuilder implements IBufferWriter<byte> which can be used to write directly buffer
                bufferWriter.Write(buffer);
                return;
            }

            var array = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                buffer.CopyTo(array);

                stream.Write(array, 0, buffer.Length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(array);
            }
#endif
        }
    }
}


/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2021 Couchbase, Inc.
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *
 * ************************************************************/
