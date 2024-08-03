using System;
using System.Buffers;
using Hanako.Exceptions;

namespace Hanako.Models.Protocol;

internal class RtmpChunkHeader
{
    private RtmpChunkHeader(RtmpChunkHeaderType type, uint streamId)
    {
        Type = type;
        StreamId = streamId;
    }

    public RtmpChunkHeaderType Type { get; private set; }

    public uint StreamId { get; private set; }

    public static RtmpChunkHeader BuildHeader(SequenceReader<byte> reader)
    {
        if (!reader.TryRead(out var firstByte))
        {
            throw new NetworkException("Failed to read the header type.");
        }

        // FMT takes 2 bits.
        //
        var type = (RtmpChunkHeaderType)(firstByte >> 6);

        // Check if Type is present in the enum.
        //
        if (!Enum.IsDefined(type))
        {
            throw new FormatException($"{nameof(type)} is invalid. Value: '{type}'.");
        }

        var streamId = (uint)(firstByte >> 2); // mask - 0011 1111
        // SID 0, 1, 2 are reserved.
        // 2 byte form, ID is in the range 64-139.
        if (streamId == 0)
        {
            // Second byte + 64.
            if (!reader.TryRead(out var secondByte))
            {
                throw new NetworkException("Failed to read the SID.");
            }

            streamId = (uint)secondByte + 64;
        }
        // 3 byte form, ID is in the range 64-65599.
        else if (streamId == 1)
        {
            // Third byte * 256 + second byte + 64.
            if (!reader.TryRead(out var thirdByte))
            {
                throw new NetworkException("Failed to read the SID.");
            }

            streamId += (uint)thirdByte * 256;
        }
        else
        {
            throw new FormatException($"Failed to parse {nameof(RtmpChunkHeader)}.");
        }

        return new RtmpChunkHeader(type, streamId);
    }
}