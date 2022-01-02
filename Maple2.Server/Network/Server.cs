using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ThreadState = System.Threading.ThreadState;

namespace Maple2.Server.Network;

public abstract class Server<T> : IHostedService where T : Session {
    private TcpListener listener;
    private Thread serverThread;

    private readonly CancellationTokenSource source;
    private readonly ManualResetEvent clientConnected;
    private readonly ushort port;
    private readonly PacketRouter<T> router;
    private readonly IComponentContext context;

    protected readonly ILogger logger;

    public Server(ushort port, PacketRouter<T> router, ILogger logger, IComponentContext context) {
        Trace.Assert(context != null);

        this.source = new CancellationTokenSource();
        this.clientConnected = new ManualResetEvent(false);
        this.port = port;
        this.router = router;
        this.logger = logger;
        this.context = context;
    }

    public abstract void AddSession(T session);

    private void AcceptTcpClient(IAsyncResult result) {
        var session = context.Resolve<T>();
        TcpClient client = listener.EndAcceptTcpClient(result);
        session.Init(client);
        session.OnPacket += router.OnPacket;

        AddSession(session);

        clientConnected.Set();
    }

    public Task StartAsync(CancellationToken cancellationToken) {
        listener = new TcpListener(IPAddress.Any, port);
        listener.Start();

        serverThread = new Thread(() => {
            logger.LogInformation("{Type} started on Port:{Port}", GetType().Name, port);
            while (!source.IsCancellationRequested) {
                clientConnected.Reset();
                ValueTask<TcpClient> client = listener.AcceptTcpClientAsync(source.Token);
                listener.BeginAcceptTcpClient(AcceptTcpClient, null);
                clientConnected.WaitOne();
            }
        }) {Name = $"{GetType().Name}Thread"};
        serverThread.Start();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        switch (serverThread.ThreadState) {
            case ThreadState.Unstarted:
                logger.LogInformation("{Type} has not been started", GetType().Name);
                break;
            case ThreadState.Stopped:
                logger.LogInformation("{Type} has already been stopped", GetType().Name);
                break;
            default:
                source.Cancel();
                clientConnected.Set();
                serverThread.Join();
                logger.LogInformation("{Type} was stopped", GetType().Name);
                break;
        }

        return Task.CompletedTask;
    }
}