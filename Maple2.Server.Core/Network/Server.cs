using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Maple2.Server.Core.Network;

public abstract class Server<T> : BackgroundService where T : Session {
    private enum ServerState {
        Unstarted,
        Running,
        Stopped,
    }

    private readonly ushort port;
    private readonly PacketRouter<T> router;
    private readonly IComponentContext context;

    private ServerState state = ServerState.Unstarted;

    protected readonly ILogger Logger = Log.Logger.ForContext<T>();

    protected Server(ushort port, PacketRouter<T> router, IComponentContext context) {
        this.port = port;
        this.router = router;
        this.context = context ?? throw new ArgumentException("null context provided");
    }

    public abstract void OnConnected(T session);
    public abstract void OnDisconnected(T session);
    protected abstract void AddSession(T session);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        state = ServerState.Running;

        Logger.Information("{Type} started on Port:{Port}", GetType().Name, port);
        await using CancellationTokenRegistration registry = stoppingToken.Register(() => listener.Stop());
        while (!stoppingToken.IsCancellationRequested) {
            TcpClient client = await listener.AcceptTcpClientAsync(stoppingToken);

            var session = context.Resolve<T>(new NamedParameter("tcpClient", client));
            session.OnPacket += router.OnPacket;

            AddSession(session);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken) {
        switch (state) {
            case ServerState.Unstarted:
                Logger.Information("{Type} has not been started", GetType().Name);
                break;
            case ServerState.Running:
                state = ServerState.Stopped;
                Logger.Information("{Type} was stopped", GetType().Name);
                break;
            case ServerState.Stopped:
                Logger.Information("{Type} has already been stopped", GetType().Name);
                break;
        }

        return Task.CompletedTask;
    }
}
