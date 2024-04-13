namespace Hanako.Internal.Models.Protocol;

public enum RtmpChunkHeaderType
{
    /// <summary>
    /// 11 bytes = timestamp(3 bytes) + length(3 bytes) + type(1 byte) + stream id(4 bytes)
    /// </summary>
    Type0,
    /// <summary>
    /// 7 bytes = timestamp delta(3 bytes) + length(3 bytes) + type(1 byte)
    /// </summary>
    Type1,
    /// <summary>
    /// 2 bytes = timestamp delta(3 bytes)
    /// </summary>
    Type2,
    /// <summary>
    /// no header
    /// </summary>
    Type3
}
