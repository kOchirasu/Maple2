using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Grpc.Core;
using Maple2.Model.Game;
using Maple2.Model.Game.Event;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Network;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Session;
using Maple2.Server.World.Service;
using WorldClient = Maple2.Server.World.Service.World.WorldClient;

namespace Maple2.Server.Game;

public class GameServer : Server<GameSession> {
    private readonly object mutex = new();
    private readonly WorldClient world;
    private readonly FieldManager.Factory fieldFactory;
    private readonly HashSet<GameSession> connectingSessions;
    private readonly Dictionary<long, GameSession> sessions;
    private readonly List<GameEvent> gameEvents;

    public int Channel { get; private set; }

    public GameServer(WorldClient world, FieldManager.Factory fieldFactory, PacketRouter<GameSession> router, IComponentContext context)
            : base(Target.GamePort, router, context) {
        this.world = world;
        this.fieldFactory = fieldFactory;
        connectingSessions = new HashSet<GameSession>();
        sessions = new Dictionary<long, GameSession>();
        gameEvents = new List<GameEvent> {new TrafficOptimizer()};
    }

    public override void OnConnected(GameSession session) {
        lock (mutex) {
            connectingSessions.Remove(session);
            sessions[session.CharacterId] = session;
        }
    }

    public override void OnDisconnected(GameSession session) {
        lock (mutex) {
            connectingSessions.Remove(session);
            sessions.Remove(session.CharacterId);
        }
    }

    public bool GetSession(long characterId, [NotNullWhen(true)] out GameSession? session) {
        lock (mutex) {
            return sessions.TryGetValue(characterId, out session);
        }
    }

    protected override void AddSession(GameSession session) {
        lock (mutex) {
            connectingSessions.Add(session);
        }

        Logger.Information("Game client connecting: {Session}", session);
        session.Start();
    }

    public IList<GameEvent> GetGameEvents() {
        return gameEvents;
    }

    protected override Task ExecuteAsync(CancellationToken cancellationToken) {
        try {
            RegisterResponse response = world.Register(new RegisterRequest {
                IpAddress = Target.GameIp.ToString(),
                Port = Port,
            }, cancellationToken: cancellationToken);

            Channel = response.Channel;
        } catch (RpcException ex) {
            Logger.Fatal(ex, "Failed to register GameServer instance with WorldServer");
            throw;
        }

        return base.ExecuteAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken) {
        lock (mutex) {
            foreach (GameSession session in connectingSessions) {
                session.Send(NoticePacket.Disconnect(new InterfaceText("GameServer Maintenance")));
                session.Dispose();
            }
            foreach (GameSession session in sessions.Values) {
                session.Send(NoticePacket.Disconnect(new InterfaceText("GameServer Maintenance")));
                session.Dispose();
            }
            fieldFactory.Dispose();
        }

        world.Unregister(new UnregisterRequest{Channel = Channel}, cancellationToken: cancellationToken);
        return base.StopAsync(cancellationToken);
    }
}
