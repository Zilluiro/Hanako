using System;
using System.Drawing;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Hanako.Internal.Models
{
    internal class RtmpOpenConnection
    {
        public RtmpOpenConnection(Guid identifier, NetworkStream networkStream)
        {
            Identifier = identifier;
            NetworkStream = networkStream;
        }

        public Guid Identifier { get; init; }

        public NetworkStream NetworkStream { get; init; }

        public async Task ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var bytes  = await NetworkStream.ReadAsync(buffer, offset, count, cancellationToken);
            Console.WriteLine(bytes);
        }

        public async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await NetworkStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public Task WritePacketAsync(RtmpPacket packet, CancellationToken cancellationToken)
        {
            return WriteAsync(packet.Data, 0, packet.Length, cancellationToken);
        }
    }
}
