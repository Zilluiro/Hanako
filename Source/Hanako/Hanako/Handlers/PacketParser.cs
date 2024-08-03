using Hanako.Models;
using Hanako.Models.Protocol;
using Microsoft.Extensions.Logging;
using System.Buffers;

namespace Hanako.Handlers;

internal class PacketParser
{
    public PacketParser(ILogger<PacketParser> logger)
    {
        _logger = logger;
    }

    public void Parse(ReadOnlySequence<byte> message)
    {
        var reader = new SequenceReader<byte>(message);
        var chunk = RtmpChunk.Build(reader);

        RtmpContext.SetLastChunk(chunk);

        // Control Messages
        if (chunk.Header.StreamId == 2)
        {

        }
    }

    private readonly ILogger<PacketParser> _logger;
}
