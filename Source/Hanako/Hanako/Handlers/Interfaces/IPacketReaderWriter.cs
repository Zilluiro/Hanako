using Hanako.Models;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Hanako.Handlers.Interfaces
{
    internal interface IPacketReaderWriter
    {
        ValueTask<ReadOnlySequence<byte>> ReadAsync(CancellationToken cancellationToken);

        ValueTask WriteAsync(RtmpPacket packet, CancellationToken cancellationToken);
    }
}
