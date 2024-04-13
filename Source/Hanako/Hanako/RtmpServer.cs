using Hanako.Exceptions;
using Hanako.Extensions;
using Hanako.Handlers;
using Hanako.Internal.Models;
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
    public RtmpServer(ILogger<RtmpServer> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        _tcpListener = new TcpListener(IPAddress.Any, Constants.RTMP.Port);
        _clients = new ConcurrentDictionary<Guid, RtmpClient>();
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
            using var netstream = connection.GetStream();

            // Perform the handshake.
            //
            var handshaker = _serviceProvider.GetValidatedService<Handshaker>();
            await handshaker.DoHandshakeAsync(new RtmpOpenConnection(client.Identifier, netstream), cancellationToken);


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

    private int CurrentThreadID => Thread.CurrentThread.ManagedThreadId;

    private readonly ILogger<RtmpServer> _logger;
    private readonly IServiceProvider _serviceProvider;

    private readonly ConcurrentDictionary<Guid, RtmpClient> _clients;
    private int _concurrentThreads;
    private readonly TcpListener _tcpListener;
}
