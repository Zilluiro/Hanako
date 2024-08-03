using Hanako.Exceptions;
using Hanako.Extensions;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Threading;

namespace Hanako.Models.Protocol;

internal class RtmpMessageHeader
{
    public uint TimestampDelta { get; private set; }

    public uint Length { get; private set; }

    public uint StreamId { get; private set; }

    public RtmpMessageType MessageType { get; private set; }

    public static RtmpMessageHeader Build(SequenceReader<byte> reader, RtmpChunkHeaderType fmt, RtmpMessageHeader? previous)
    {
        var messageHeader = new RtmpMessageHeader();

        // 2 bytes = timestamp delta(3 bytes)
        if (fmt <= RtmpChunkHeaderType.Type2)
        {
            var timestampBytes = _arrayPool.Rent(3);
            if (!reader.TryReadExact(3, out var timestampSequence))
            {
                throw new NetworkException("Failed to read the timestamp.");
            }
            
            timestampSequence.CopyTo(timestampBytes);
            messageHeader.TimestampDelta = BinaryPrimitivesExtended.ReadUInt24BigEndian(timestampBytes);
        }
        else
        {
            messageHeader.TimestampDelta = previous?.TimestampDelta ?? 0;
        }

        // 7 bytes = timestamp delta(3 bytes) + length(3 bytes) + type(1 byte)
        if (fmt <= RtmpChunkHeaderType.Type1)
        {
            var messageLengthBytes = _arrayPool.Rent(3);
            if (!reader.TryReadExact(3, out var messageLengthSequence))
            {
                throw new NetworkException("Failed to read the message length.");
            }
            messageLengthSequence.CopyTo(messageLengthBytes);

            messageHeader.Length = BinaryPrimitivesExtended.ReadUInt24BigEndian(messageLengthBytes);
            var t2 = BinaryPrimitives.ReadUInt32BigEndian(messageLengthBytes);

            if (!reader.TryRead(out var messageType))
            {
                throw new NetworkException("Failed to read the message type.");
            }

            messageHeader.MessageType = (RtmpMessageType)messageType;
        }
        else
        {
            messageHeader.Length = previous?.Length ?? 0;
            messageHeader.MessageType = previous?.MessageType ?? RtmpMessageType.None;
        }

        // 11 bytes = timestamp(3 bytes) + length(3 bytes) + type(1 byte) + stream id(4 bytes)
        if (fmt == RtmpChunkHeaderType.Type0)
        {
            var messageLengthBytes = _arrayPool.Rent(4);
            if (!reader.TryReadExact(4, out var messageLengthSequence))
            {
                throw new NetworkException("Failed to read the message length.");
            }

            messageLengthSequence.CopyTo(messageLengthBytes);
            messageHeader.StreamId = BinaryPrimitives.ReadUInt32BigEndian(messageLengthBytes);
        }
        else
        {
            messageHeader.StreamId = previous?.StreamId ?? 0;
        }

        // Skip Type 3.

        return messageHeader;
    }

    private static ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;
}
