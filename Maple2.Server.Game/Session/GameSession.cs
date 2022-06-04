using System;
using System.Net.Sockets;
using System.Numerics;
using Autofac;
using Maple2.Database.Storage;
using Maple2.Model;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Core.Network;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Commands;
using Maple2.Server.Game.Manager;
using Maple2.Server.Game.Manager.Config;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Manager.Items;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Tools.Scheduler;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Game.Session;

public sealed class GameSession : Core.Network.Session, IDisposable {
    protected override PatchType Type => PatchType.Ignore;
    public const int FIELD_KEY = 0x1234;

    private bool disposed;
    private readonly GameServer server;

    public readonly CommandRouter CommandHandler;
    public readonly EventQueue Scheduler;

    public long AccountId { get; private set; }
    public long CharacterId { get; private set; }
    public Guid MachineId { get; private set; }

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public GameStorage GameStorage { get; init; } = null!;
    public ItemMetadataStorage ItemMetadata { get; init; } = null!;
    public SkillMetadataStorage SkillMetadata { get; init; } = null!;
    public TableMetadataStorage TableMetadata { get; init; } = null!;
    public FieldManager.Factory FieldFactory { private get; init; } = null!;
    // ReSharper restore All
    #endregion

    public ConfigManager Config { get; set; }
    public ItemManager Item { get; set; }
    public CurrencyManager Currency { get; set; }
    public StatsManager Stats { get; set; }
    public FieldManager? Field { get; set; }
    public FieldPlayer Player { get; private set; }

    public GameSession(TcpClient tcpClient, GameServer server, ILogger<GameSession> logger, IComponentContext context)
            : base(tcpClient, logger) {
        this.server = server;
        CommandHandler = context.Resolve<CommandRouter>(new NamedParameter("session", this));
        Scheduler = new EventQueue();
        Scheduler.ScheduleRepeated(() => Send(TimeSyncPacket.Request()), 1000);

        OnLoop += Scheduler.InvokeAll;
    }

    public bool EnterServer(long accountId, long characterId, Guid machineId) {
        AccountId = accountId;
        CharacterId = characterId;
        MachineId = machineId;

        server.OnConnected(this);

        using GameStorage.Request db = GameStorage.Context();
        db.BeginTransaction();
        Player player = db.LoadPlayer(AccountId, CharacterId);
        if (player == null) {
            Send(MigrationPacket.MoveResult(MigrationError.s_move_err_default));
            return false;
        }
        db.Commit();

        // Create a placeholder FieldPlayer
        Player = new FieldPlayer(0, this, player);
        Currency = new CurrencyManager(this);
        Stats = new StatsManager(this);

        Config = new ConfigManager(db, this);
        Item = new ItemManager(db, this);

        if (!PrepareField(player.Character.MapId)) {
            Send(MigrationPacket.MoveResult(MigrationError.s_move_err_default));
            return false;
        }

        //session.Send(Packet.Of(SendOp.REQUEST_SYSTEM_INFO));
        Send(MigrationPacket.MoveResult(MigrationError.ok));

        // Buddy
        // Survival
        // MeretMarket
        // UserConditionEvent
        // PCBangBonus
        // Buddy

        Send(TimeSyncPacket.Reset(DateTimeOffset.UtcNow));
        Send(TimeSyncPacket.Set(DateTimeOffset.UtcNow));

        Send(StatsPacket.Init(Player));
        // Quest

        Send(RequestPacket.TickSync(Environment.TickCount));

        // DynamicChannel
        Send(ServerEnterPacket.Request(Player));

        // Ugc
        // Cash
        // Gvg
        // Pvp
        Send(StateSyncPacket.SyncNumber(0));
        // SyncWorld
        // Prestige
        Item.Inventory.Load();
        // FurnishingStorage
        // FurnishingInventory
        // Quest
        // Achieve
        // MaidCraftItem
        // UserMaid
        // UserEnv
        // Fishing
        // ResponsePet
        // LegionBattle
        // CharacterAbility
        Config.LoadKeyTable();
        // GuideRecord
        // DailyWonder*
        // GameEvent
        // BannerList
        // RoomDungeon
        // FieldEntrance
        // InGameRank
        Send(FieldEnterPacket.Request(Player));
        // HomeCommand
        // ResponseCube
        // Mentor
        // ChatStamp
        // Mail
        // BypassKey
        // AH

        return true;
    }

    public bool PrepareField(int mapId, int portalId = 0, in Vector3 position = default, in Vector3 rotation = default) {
        FieldManager? newField = FieldFactory.Get(mapId);
        if (newField == null) {
            return false;
        }

        if (Field != null) {
            Scheduler.Stop();
            Field.RemovePlayer(Player.ObjectId, out _);
        }

        Field = newField;
        Player = Field.SpawnPlayer(this, Player, portalId, position, rotation);

        return true;
    }

    public bool EnterField() {
        Player.Value.Unlock.Maps.Add(Player.Value.Character.MapId);

        Field?.OnAddPlayer(Player);
        Scheduler.Start();

        Config.LoadMacros();

        Send(RevivalPacket.Count(0)); // TODO: Consumed daily revivals?
        Send(RevivalPacket.Confirm(Player));
        Config.LoadStatAttributes();

        return true;
    }

    public bool Temp() {
        // -> RequestMoveField

        // <- RequestFieldEnter
        // -> RequestLoadUgcMap
        //   <- LoadUgcMap
        //   <- LoadCubes
        // -> Ugc
        //   <- Ugc
        // -> ResponseFieldEnter


        return true;
    }

    public void OnStateSync(StateSync stateSync) {
        Player.Position = stateSync.Position;
        Player.Rotation = new Vector3(0, 0, stateSync.Rotation);
        Player.State = stateSync.State;
        Player.SubState = stateSync.SubState;
    }

    public void Log(LogLevel level, string? message, params object?[] args) {
        logger.Log(level, message, args);
    }

    #region Dispose
    ~GameSession() => Dispose(false);

    public new void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected override void Dispose(bool disposing) {
        if (disposed) return;
        disposed = true;

        try {
            Scheduler.Stop();
            server.OnDisconnected(this);
            Field?.RemovePlayer(Player.ObjectId, out FieldPlayer? _);
            Complete();
        } finally {
#if !DEBUG
            if (Player.Value.Character.ReturnMapId != 0) {
                Player.Value.Character.MapId = Player.Value.Character.ReturnMapId;
            }
#endif

            using (GameStorage.Request db = GameStorage.Context()) {
                db.BeginTransaction();
                db.SavePlayer(Player, true);
                Config.Save(db);
                Item.Save(db);
            }

            base.Dispose(disposing);
        }
    }
    #endregion
}
