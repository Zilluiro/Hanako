using Hanako.Exceptions;
using Hanako.Internal;
using Hanako.Internal.Models;
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

        public async Task DoHandshakeAsync(RtmpOpenConnection openConnection, CancellationToken cancellationToken)
        {
            _openConnection = openConnection;

            _logger.LogInformation($"Getting c0 and c1 packets. Client: '{openConnection.Identifier}'.");
            var s0s1 = await Perform_S0_S1_Async(cancellationToken);

            _logger.LogInformation($"Sending s0 and s1 packets. Client: '{openConnection.Identifier}'.");
            await openConnection.WritePacketAsync(s0s1, cancellationToken);

            var s2 = Perform_S2();
            _logger.LogInformation($"Sending s2 validation packet. Client: '{openConnection.Identifier}'.");
            await openConnection.WritePacketAsync(s2, cancellationToken);

            _logger.LogInformation($"Getting c2 validation packet. Client: '{openConnection.Identifier}'.");
            await Validate_C2_Async(cancellationToken);

            _logger.LogInformation($"Connection established. Client: '{openConnection.Identifier}'.");
        }

        /// <summary>
        /// C0. Identifies the RTMP version requested by the client (the only valid version is 3).
        /// C1. 1536 random bytes to save.
        /// </summary>
        private async Task<RtmpPacket> Perform_S0_S1_Async(CancellationToken cancellationToken)
        {
            // C0 + C1.
            var size = Constants.RTMP.HandshakePacketSize + 1;
            var buffer = _arrayPool.Rent(size);

            await _openConnection.ReadAsync(buffer, 0, size, cancellationToken);
            var package = buffer.AsMemory();

            // Get the RTMP version.
            //
            var versionPackage = package.Slice(0, 1);
            var version = GetRtmpVersion(versionPackage);

            // Save C1 response.
            //
            var clientResponseBytes = package.Slice(1, Constants.RTMP.HandshakePacketSize);
            ClientResponseBytes = clientResponseBytes.Span.ToArray();

            // Form the response.
            //
            var response = _arrayPool.Rent(size);
            var responseSpan = response.AsMemory();

            // Version.
            response[0] = version;
            // Epoch (Actually zeroes).
            responseSpan.Slice(1, 4).Span.Fill(0);
            // Zeros.
            responseSpan.Slice(5, 4).Span.Fill(0);
            // Random bytes.
            var random = responseSpan.Slice(9, 1528);
            _random.NextBytes(random.Span);

            // Save S1 bytes.
            //
            ServerResponseBytes = responseSpan.Slice(1).Span.ToArray();

            return new RtmpPacket(response, size);
        }

        /// <summary>
        /// Sends C1.
        /// </summary>
        private RtmpPacket Perform_S2()
        {
            return new RtmpPacket(ClientResponseBytes, Constants.RTMP.HandshakePacketSize);
        }

        /// <summary>
        /// C2. Validate server's response bytes.
        /// </summary>
        private async Task<RtmpPacket> Validate_C2_Async(CancellationToken cancellationToken)
        {
            var size = Constants.RTMP.HandshakePacketSize;
            var buffer = _arrayPool.Rent(size);

            await _openConnection.ReadAsync(buffer, 0, size, cancellationToken);
            var srep = ServerResponseBytes.AsMemory().Slice(0, size);
            var crep = buffer.AsMemory().Slice(0, size);

            if (!srep.Span.SequenceEqual(crep.Span))
            {
                throw new FailedHandshakeException("C2 packet isn't valid.");
            }

            return new RtmpPacket(ClientResponseBytes, size);
        }

        private byte GetRtmpVersion(Memory<byte> versionPackage)
        {
            Guard.HasLength(versionPackage, 1);

            var version = versionPackage.Span[0];
            if (version != Constants.RTMP.Version)
            {
                _logger.LogInformation($"Client '{_openConnection.Identifier}' tries to use an unsupported version '{version}'. ");
            }

            return Constants.RTMP.Version;
        }

        // C1 storage.
        private byte[] ClientResponseBytes;
        // S1 storage.
        private byte[] ServerResponseBytes;

        private RtmpOpenConnection _openConnection;
        private readonly ILogger<Handshaker> _logger;
        private readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;
        private readonly Random _random = new Random();
    }
}
