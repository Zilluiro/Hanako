using System;
using System.Net.Sockets;

namespace Hanako.Internal.Models;

internal class RtmpClient
{
    public RtmpClient(TcpClient tcpClient)
    {
        TcpClient = tcpClient;
        Identifier = Guid.NewGuid();
    }

    public TcpClient TcpClient { get; init; }

    public Guid Identifier { get; init; }
}