using System;

namespace Hanako;

internal class Constants
{
    internal class RTMP
    {
        public const int Port = 1935;

        public const int MaxClients = 1;
        public static readonly TimeSpan ClientTimeout = TimeSpan.FromMilliseconds(500);
        public const ushort HandshakePacketSize = 1536;

        public const byte Version = 0x3;
    }
}
