namespace Hanako.Models;

internal class RtmpPacket
{
    public RtmpPacket(byte[] data, int length)
    {
        Data = data;
        Length = length;
    }

    public byte[] Data { get; init; }

    public int Length { get; init; }
}
