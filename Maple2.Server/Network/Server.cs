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
                logger.LogInformation($"{GetType().Name} started on Port:{port}");
                listener.BeginAcceptTcpClient(AcceptTcpClient, null);
                clientConnected.WaitOne();
            }
        }) {Name = $"{GetType().Name}Thread"};
        serverThread.Start();
    }

    public void Stop() {
        switch (serverThread.ThreadState) {
            case ThreadState.Unstarted:
                logger.LogInformation($"{GetType().Name} has not been started.");
                break;
            case ThreadState.Stopped:
                logger.LogInformation($"{GetType().Name} has already been stopped.");
                break;
            default:
                source.Cancel();
                clientConnected.Set();
                serverThread.Join();
                logger.LogInformation($"{GetType().Name} was stopped.");
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