using Hanako.Exceptions;
using Hanako.Helpers;
using Hanako.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Hanako.Handlers
{
    internal class Handshaker
    {
        public Handshaker(ILogger<Handshaker> logger)
        {
            _logger = logger;
        }

        public async Task DoHandshakeAsync(CancellationToken cancellationToken)
        {
            RtmpContext.SetConnectionState(RtmpContext.RtmpConnectionState.Handshake_C0C1);
            _logger.LogInformation($"Getting c0 and c1 packets. Client: '{RtmpContext.ClientId}'.");
            var c0c1 = await RtmpContext.ReaderWriter.ReadAsync(cancellationToken);
            var s0s1 = Perform_S0_S1_S2(c0c1);

            _logger.LogInformation($"Sending s0, s1, s2 packets. Client: '{RtmpContext.ClientId}'.");
            await RtmpContext.ReaderWriter.WriteAsync(s0s1, cancellationToken);

/*            var s2 = Perform_S2();
            _logger.LogInformation($"Sending s2 validation packet. Client: '{RtmpContext.ClientId}'.");
            await RtmpContext.ReaderWriter.WriteAsync(s2, cancellationToken);*/

            RtmpContext.SetConnectionState(RtmpContext.RtmpConnectionState.Handshake_C2);
            _logger.LogInformation($"Getting c2 validation packet. Client: '{RtmpContext.ClientId}'.");
            var c2 = await RtmpContext.ReaderWriter.ReadAsync(cancellationToken);
            Validate_C2(c2);

            _logger.LogInformation($"Connection established. Client: '{RtmpContext.ClientId}'.");
            RtmpContext.SetConnectionState(RtmpContext.RtmpConnectionState.PostHandshake);
        }

        /// <summary>
        /// C0. Identifies the RTMP version requested by the client (the only valid version is 3).
        /// C1. 1536 random bytes to save.
        /// </summary>
        private RtmpPacket Perform_S0_S1_S2(ReadOnlySequence<byte> message)
        {
            Guard.HasLength(message, Constants.RTMP.HandshakePacketSize + 1);

            // Get the RTMP version.
            //
            var reader = new SequenceReader<byte>(message);
            if (!reader.TryRead(out var versionByte))
            {
                throw new NetworkException("Failed to read the version.");
            }
            var version = GetRtmpVersion(versionByte);

            // C1.
            //
            var C1Size = Constants.RTMP.HandshakePacketSize;
            var buffer = _arrayPool.Rent(C1Size);
            if (!reader.TryCopyTo(buffer.AsSpan()[C1Size..]))
            {
                throw new NetworkException("Failed to read the C1 package.");
            }
            var C1 = buffer.AsMemory();

            // Save C1 response.
            //
            var clientResponseBytes = C1.Slice(1, C1Size);
            ClientResponseBytes = clientResponseBytes.Span.ToArray();

            // Form the response.
            //
            var S0S1S2Size = Constants.RTMP.HandshakePacketSize + C1Size + 1;
            var response = _arrayPool.Rent(S0S1S2Size);
            var responseSpan = response.AsMemory();

            // Version.
            //
            response[0] = version;

            // Epoch (Actually zeroes).
            //
            responseSpan.Slice(1, 4).Span.Clear();

            // Zeros.
            //
            responseSpan.Slice(5, 4).Span.Clear();

            // Random bytes.
            //
            var random = responseSpan.Slice(9, 1528);
            _random.NextBytes(random.Span);

            var s2 = responseSpan.Slice(1528, C1Size);
            ClientResponseBytes.CopyTo(s2);

            // Save S1 bytes.
            //
            ServerResponseBytes = responseSpan[1..].Span.ToArray();

            // Sends C1.
            return new RtmpPacket(response, S0S1S2Size);
        }

        /// <summary>
        /// C2. Validate server's response bytes.
        /// </summary>
        private void Validate_C2(ReadOnlySequence<byte> message)
        {
            Guard.HasLength(message, Constants.RTMP.HandshakePacketSize);

            var reader = new SequenceReader<byte>(message);
            var size = Constants.RTMP.HandshakePacketSize;
            var buffer = _arrayPool.Rent(size);

            if (!reader.TryCopyTo(buffer.AsSpan()[..size]))
            {
                throw new NetworkException("Failed to read the C2 package.");
            }

            var srep = ServerResponseBytes.AsMemory()[..size];
            var crep = buffer.AsMemory()[..size];

            if (!srep.Span.SequenceEqual(crep.Span))
            {
                throw new FailedHandshakeException("C2 packet isn't valid.");
            }
        }

        private byte GetRtmpVersion(byte version)
        {
            if (version != Constants.RTMP.Version)
            {
                _logger.LogInformation($"Client '{RtmpContext.ClientId}' tries to use an unsupported version '{version}'. ");
            }

            return Constants.RTMP.Version;
        }

        // C1 storage.
        //
        private byte[] ClientResponseBytes;

        // S1 storage.
        //
        private byte[] ServerResponseBytes;

        private readonly ILogger<Handshaker> _logger;

        private readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;
        private readonly Random _random = new ();
    }
}
