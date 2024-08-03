using Hanako.Handlers;
using Hanako.Handlers.Interfaces;
using Hanako.Helpers;
using Hanako.Models.Protocol;
using System;
using System.Threading;

namespace Hanako.Models;

internal class RtmpContext
{
    public static void Initialize(RtmpOpenConnection rtmpConnection)
    {
        s_state = new AsyncLocal<RtmpContextState> { Value = new RtmpContextState(rtmpConnection) };
    }

    public static RtmpChunk? LastChunk => CurrentState.LastChunk;

    public static RtmpChunk SetLastChunk(RtmpChunk chunk) => CurrentState.LastChunk = chunk;

    public static Guid ClientId => CurrentState.RtmpOpenConnection.Client.Identifier;

    public static IPacketReaderWriter ReaderWriter => CurrentState.RtmpOpenConnection;

    public static RtmpConnectionState SetConnectionState(RtmpConnectionState state)
    {
        if (state <= CurrentState.RtmpConnectionState)
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        return CurrentState.RtmpConnectionState = state;
    }

    public static int PackageSize
    {
        get
        {
            return CurrentState.RtmpConnectionState switch
            {
                RtmpConnectionState.Handshake_C0C1 => Constants.RTMP.HandshakePacketSize + 1,
                RtmpConnectionState.Handshake_C2 => Constants.RTMP.HandshakePacketSize,
                RtmpConnectionState.PostHandshake => GetCustomPackageSize(),

                _ => throw new ArgumentOutOfRangeException($"Value {CurrentState.RtmpConnectionState} isn't supported"),
            };

            int GetCustomPackageSize()
            {
                if (CurrentState.CustomPackageSize != default)
                {
                    return CurrentState.CustomPackageSize;
                }

                return Constants.RTMP.DefaultChunkSize;
            }
        }
    }

    private static RtmpContextState CurrentState => Guard.IsNotNull(s_state?.Value);

    private static AsyncLocal<RtmpContextState>? s_state;

    private class RtmpContextState
    {
        public RtmpContextState(RtmpOpenConnection rtmpConnection)
        {
            RtmpOpenConnection = rtmpConnection;
        }

        public RtmpOpenConnection RtmpOpenConnection { get; init; }

        public RtmpChunk? LastChunk { get; set; }

        public int CustomPackageSize { get; set; }

        public RtmpConnectionState RtmpConnectionState { get; set; }
    }

    internal enum RtmpConnectionState
    {
        None,
        Handshake_C0C1,
        Handshake_C2,
        PostHandshake
    }
}