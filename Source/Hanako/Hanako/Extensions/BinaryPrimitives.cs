using System;

namespace Hanako.Extensions
{
    internal static partial class BinaryPrimitivesExtended
    {
        public static uint ReadUInt24BigEndian(Span<byte> bytes)
        {
            return (((uint)bytes[2]) << 16) | (((uint)bytes[1]) << 8) | ((uint)bytes[0]);
        }
    }
}
