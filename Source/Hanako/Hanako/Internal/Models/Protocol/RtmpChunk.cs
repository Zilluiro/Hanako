namespace Hanako.Internal.Models.Protocol;

internal class RtmpChunk
{
    public RtmpChunk(RtmpChunkHeader header, RtmpMessageHeader messageHeader, byte[] payload)
    {
        Header = header;
        MessageHeader = messageHeader;
        Payload = payload;
    }

    public RtmpChunkHeader Header { get; init; }

    public RtmpMessageHeader MessageHeader { get; init; }

    public byte[] Payload { get; init; }
}