using Hanako.Exceptions;
using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Hanako.Models.Protocol;

internal class RtmpChunk
{
    private RtmpChunk(RtmpChunkHeader chunkHeader, RtmpMessageHeader messageHeader, Memory<byte> payload)
    {
        Header = chunkHeader;
        MessageHeader = messageHeader;
        Payload = payload;
    }

    public RtmpChunkHeader Header { get; init; }

    public RtmpMessageHeader MessageHeader { get; init; }

    public Memory<byte> Payload { get; init; }

    public static RtmpChunk Build(SequenceReader<byte> reader)
    {
        var chunkHeader = RtmpChunkHeader.BuildHeader(reader);
        var messageHeader = RtmpMessageHeader.Build(reader, chunkHeader.Type, RtmpContext.LastChunk?.MessageHeader);

        var buffer = _arrayPool.Rent(RtmpContext.PackageSize);
        if (!reader.TryReadExact(RtmpContext.PackageSize, out var chunkSequence))
        {
            throw new NetworkException("Failed to read a chunk");
        }

        chunkSequence.CopyTo(buffer);

        return new RtmpChunk(chunkHeader, messageHeader, buffer);
    }

    private static ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;
}