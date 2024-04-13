using System;

namespace Hanako.Internal.Models.Protocol;

internal class RtmpChunkHeader
{
    public RtmpChunkHeaderType Type { get; set; }

    public uint StreamId { get; set; }

    public void Parse(Memory<byte> data)
    {
        Guard.HasLengthBiggerOrEqual(data, 1);
        var firstByte = data.Span[0];

        // FMT takes 2 bits.
        //
        Type = (RtmpChunkHeaderType)(firstByte >> 6);

        // Check if Type is present in the enum.
        //
        if (!Enum.IsDefined(Type))
        {
            throw new FormatException($"{nameof(Type)} is invalid. Value: '{Type}'.");
        }

        StreamId = (uint)(firstByte >> 2); // mask - 0011 1111
        // SID 0, 1, 2 are reserved.
        // 2 byte form, ID is in the range 64-139.
        if (StreamId == 0)
        {
            // Second byte + 64.
            Guard.HasLengthBiggerOrEqual(data, 2);
            var secondByte = (uint)data.Span[1];

            StreamId = secondByte + 64;
        }
        // 3 byte form, ID is in the range 64-65599.
        else if (StreamId == 1)
        {
            // Third byte * 256 + second byte + 64.
            Guard.HasLengthBiggerOrEqual(data, 2);
            var thirdByte = (uint)data.Span[1];

            StreamId += thirdByte * 256;
        }
        else
        {
            throw new FormatException($"Failed to parse {nameof(RtmpChunkHeader)}.");
        }
    }
}