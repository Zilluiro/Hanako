using Hanako.Exceptions;
using Hanako.Handlers;
using Hanako.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Hanako;

internal sealed class RtmpServer : BackgroundService
{
    public RtmpServer(ILogger<RtmpServer> logger, Handshaker handshaker, PacketParser parser)
    {
        _logger = logger;

        _tcpListener = new TcpListener(IPAddress.Any, Constants.RTMP.Port);
        _clients = new ConcurrentDictionary<Guid, RtmpClient>();

        _handshaker = handshaker;
        _packetParser = parser;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Task.Run(() =>
        {
            _tcpListener.Start();

            while (!stoppingToken.IsCancellationRequested)
            {
                ProccessConnections(stoppingToken);
            }
        }, stoppingToken);

        return Task.CompletedTask;
    }

    private void ProccessConnections(CancellationToken cancellationToken)
    {
        while (_concurrentThreads < Constants.RTMP.MaxClients) 
        {
            Task.Run(async () =>
            {
                _logger.LogInformation($"Listening on thread '{CurrentThreadID}'.");
                var client = new RtmpClient(await _tcpListener.AcceptTcpClientAsync());
                _clients.TryAdd(client.Identifier, client);

                await ProcessClientAsync(client, cancellationToken);
            });

            TakeThread();
        }
    }

    private async Task ProcessClientAsync(RtmpClient client, CancellationToken cancellationToken)
    {
        try
        {
            using var connection = client.TcpClient;
            if (!connection.Connected)
            {
                throw new NotConnectedException();
            }

            _logger.LogInformation($"Connection established. Thread: '{CurrentThreadID}'.");
            using var netStream = connection.GetStream();
            var rtmpConnection = new RtmpOpenConnection(client, netStream);
            RtmpContext.Initialize(rtmpConnection);

            // Perform the handshake.
            //
            await _handshaker.DoHandshakeAsync(cancellationToken);

            // Begin parsing RTMP packets.
            //

            while(!cancellationToken.IsCancellationRequested)
            {
                var package = await RtmpContext.ReaderWriter.ReadAsync(cancellationToken);

                _packetParser.Parse(package);
            }
        }
        catch(Exception e)
        {
            _logger.LogError(e, $"While calling {nameof(ProcessClientAsync)}.");
        }
        finally
        {
            _logger.LogInformation($"Client disconnected. Thread: '{CurrentThreadID}'.");
            _clients.Remove(client.Identifier, out _);
            FreeThread();
        }
    }


    public void TakeThread()
    {
        Interlocked.Increment(ref _concurrentThreads);
    }

    public void FreeThread()
    {
        Interlocked.Decrement(ref _concurrentThreads);
    }

    private static int CurrentThreadID => Environment.CurrentManagedThreadId;

    private readonly ILogger<RtmpServer> _logger;
    private readonly Handshaker _handshaker;
    private readonly PacketParser _packetParser;

    private readonly ConcurrentDictionary<Guid, RtmpClient> _clients;
    private int _concurrentThreads;
    private readonly TcpListener _tcpListener;
}
