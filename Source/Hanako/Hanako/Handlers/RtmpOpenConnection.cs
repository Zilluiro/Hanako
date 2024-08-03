using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Hanako.Handlers.Interfaces;
using Hanako.Models;
using Pipelines.Sockets.Unofficial;

namespace Hanako.Handlers
{
    internal class RtmpOpenConnection : IPacketReaderWriter
    {
        public RtmpOpenConnection(RtmpClient client, NetworkStream networkStream)
        {
            Client = client;

            DuplexPipe = SocketConnection.Create(Client.TcpClient.Client);
        }

        public RtmpClient Client { get; init; }

        private IDuplexPipe DuplexPipe { get; init; } 

        private ReadOnlySequence<byte>? LastBuffer { get; set; }

        public async ValueTask<ReadOnlySequence<byte>> ReadAsync(CancellationToken cancellationToken)
        {
            if (LastBuffer.HasValue)
            {
                DuplexPipe.Input.AdvanceTo(LastBuffer.Value.End);
            }

            var result = await DuplexPipe.Input.ReadAtLeastAsync(RtmpContext.PackageSize, cancellationToken);

            var correctedBuffer = result.Buffer.Slice(0, RtmpContext.PackageSize);
            LastBuffer = correctedBuffer;

            return correctedBuffer;
        }

        public async ValueTask WriteAsync(RtmpPacket packet, CancellationToken cancellationToken)
        {
            var toSend = new ReadOnlyMemory<byte>(packet.Data, 0, packet.Length);
            var _ = await DuplexPipe.Output.WriteAsync(toSend, cancellationToken);
            await DuplexPipe.Output.FlushAsync(cancellationToken);

            return;
        }
    }
}
