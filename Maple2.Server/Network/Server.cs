using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Autofac;
using Microsoft.Extensions.Logging;
using ThreadState = System.Threading.ThreadState;

namespace Maple2.Server.Network;

public abstract class Server<T> where T : Session {
    private TcpListener listener;
    private Thread serverThread;

    private readonly CancellationTokenSource source;
    private readonly ManualResetEvent clientConnected;
    private readonly PacketRouter<T> router;
    private readonly IComponentContext context;

    protected readonly ILogger logger;

    public Server(PacketRouter<T> router, ILogger logger, IComponentContext context) {
        Trace.Assert(context != null);

        this.source = new CancellationTokenSource();
        this.clientConnected = new ManualResetEvent(false);
        this.router = router;
        this.logger = logger;
        this.context = context;
    }

    public void Start(ushort port) {
        listener = new TcpListener(IPAddress.Any, port);
        listener.Start();

        serverThread = new Thread(() => {
            while (!source.IsCancellationRequested) {
                clientConnected.Reset();
                logger.LogInformation("{Type} started on Port:{Port}", GetType().Name, port);
                listener.BeginAcceptTcpClient(AcceptTcpClient, null);
                clientConnected.WaitOne();
            }
        }) {Name = $"{GetType().Name}Thread"};
        serverThread.Start();
    }

    public void Stop() {
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
}