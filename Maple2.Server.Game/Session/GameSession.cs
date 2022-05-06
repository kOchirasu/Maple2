using System;
using System.Net.Sockets;
using Maple2.Database.Storage;
using Maple2.Model;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Core.Network;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Manager;
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

    public readonly EventQueue Scheduler;

    public long AccountId { get; private set; }
    public long CharacterId { get; private set; }
    public Guid MachineId { get; private set; }

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public GameStorage GameStorage { get; init; } = null!;
    public ItemMetadataStorage ItemMetadata { get; init; } = null!;
    public SkillMetadataStorage SkillMetadata { private get; init; } = null!;
    public TableMetadataStorage TableMetadata { private get; init; } = null!;
    public FieldManager.Factory FieldFactory { private get; init; } = null!;
    // ReSharper restore All
    #endregion

    public FieldManager Field { get; set; }
    public ItemManager Item { get; set; }
    public SkillManager Skill { get; set; }
    public FieldPlayer Player { get; private set; }

    public GameSession(TcpClient tcpClient, GameServer server, ILogger<GameSession> logger) : base(tcpClient, logger) {
        this.server = server;
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
            return false;
        }
        db.Commit();

        Item = new ItemManager(db, this);

        JobTable.Entry jobTableEntry = TableMetadata.JobTable.Entries[player.Character.Job.Code()];
        Skill = new SkillManager(player.Character.Job, SkillMetadata, jobTableEntry);

        FieldManager? fieldManager = FieldFactory.Get(player.Character.MapId);
        if (fieldManager == null) {
            return false;
        }

        Field = fieldManager;
        Player = Field.SpawnPlayer(this, player);

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

        // Ugc
        // Cash
        // Gvg
        // Pvp
        // SyncNumber
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
        // KeyTable
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
            using (GameStorage.Request db = GameStorage.Context()) {
                db.BeginTransaction();
                db.SavePlayer(Player, true);
                Item.Save(db);
            }

            base.Dispose(disposing);
        }
    }
    #endregion
}
