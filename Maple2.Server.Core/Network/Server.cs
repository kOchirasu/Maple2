using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Core.Network;

public abstract class Server<T> : BackgroundService where T : Session {
    private enum ServerState {
        Unstarted,
        Running,
        Stopped
    }

    private readonly ushort port;
    private readonly PacketRouter<T> router;
    private readonly IComponentContext context;

    private ServerState state = ServerState.Unstarted;

    protected readonly ILogger logger;

    protected Server(ushort port, PacketRouter<T> router, ILogger logger, IComponentContext context) {
        this.port = port;
        this.router = router;
        this.logger = logger;
        this.context = context ?? throw new ArgumentException("null context provided");
    }

    protected abstract void AddSession(T session);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        state = ServerState.Running;

        logger.LogInformation("{Type} started on Port:{Port}", GetType().Name, port);
        await using CancellationTokenRegistration registry = stoppingToken.Register(() => listener.Stop());
        while (!stoppingToken.IsCancellationRequested) {
            TcpClient client = await listener.AcceptTcpClientAsync(stoppingToken);

            var session = context.Resolve<T>();
            session.Init(client);
            session.OnPacket += router.OnPacket;

            AddSession(session);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken) {
        switch (state) {
            case ServerState.Unstarted:
                logger.LogInformation("{Type} has not been started", GetType().Name);
                break;
            case ServerState.Running:
                state = ServerState.Stopped;
                logger.LogInformation("{Type} was stopped", GetType().Name);
                break;
            case ServerState.Stopped:
                logger.LogInformation("{Type} has already been stopped", GetType().Name);
                break;
        }

        return Task.CompletedTask;
    }
}
