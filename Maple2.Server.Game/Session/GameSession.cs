using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using Autofac;
using Grpc.Core;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Network;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Commands;
using Maple2.Server.Game.Manager;
using Maple2.Server.Game.Manager.Config;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Manager.Items;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.World.Service;
using Maple2.Tools.Scheduler;
using WorldClient = Maple2.Server.World.Service.World.WorldClient;

namespace Maple2.Server.Game.Session;

public sealed partial class GameSession : Core.Network.Session {
    protected override PatchType Type => PatchType.Ignore;
    public const int FIELD_KEY = 0x1234;

    private bool disposed;
    private readonly GameServer server;

    public readonly CommandRouter CommandHandler;
    public readonly EventQueue Scheduler;

    public long AccountId { get; private set; }
    public long CharacterId { get; private set; }
    public Guid MachineId { get; private set; }
    public int Channel => server.Channel;

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public GameStorage GameStorage { get; init; } = null!;
    public WorldClient World { get; init; } = null!;
    public ItemMetadataStorage ItemMetadata { get; init; } = null!;
    public SkillMetadataStorage SkillMetadata { get; init; } = null!;
    public TableMetadataStorage TableMetadata { get; init; } = null!;
    public MapMetadataStorage MapMetadata { get; init; } = null!;
    public FieldManager.Factory FieldFactory { private get; init; } = null!;
    // ReSharper restore All
    #endregion

    public ConfigManager Config { get; set; }
    public MailManager Mail { get; set; }
    public BuddyManager Buddy { get; set; }
    public ItemManager Item { get; set; }
    public HousingManager Housing { get; set; }
    public CurrencyManager Currency { get; set; }
    public StatsManager Stats { get; set; }
    public FieldManager? Field { get; set; }
    public FieldPlayer Player { get; private set; }

    public GameSession(TcpClient tcpClient, GameServer server, IComponentContext context) : base(tcpClient) {
        this.server = server;
        State = SessionState.Moving;
        CommandHandler = context.Resolve<CommandRouter>(new NamedParameter("session", this));
        Scheduler = new EventQueue();
        Scheduler.ScheduleRepeated(() => Send(TimeSyncPacket.Request()), 1000);

        OnLoop += Scheduler.InvokeAll;
    }

    public bool FindSession(long characterId, [NotNullWhen(true)] out GameSession? other) {
        return server.GetSession(characterId, out other);
    }

    public bool EnterServer(long accountId, long characterId, Guid machineId) {
        AccountId = accountId;
        CharacterId = characterId;
        MachineId = machineId;

        State = SessionState.Moving;
        server.OnConnected(this);

        using GameStorage.Request db = GameStorage.Context();
        db.BeginTransaction();
        int objectId = FieldManager.NextGlobalId();
        Player? player = db.LoadPlayer(AccountId, CharacterId, objectId);
        if (player == null) {
            Logger.Warning("Failed to load player from database: {AccountId}, {CharacterId}", AccountId, CharacterId);
            Send(MigrationPacket.MoveResult(MigrationError.s_move_err_default));
            return false;
        }
        db.Commit();

        player.Character.Channel = (short) Channel;
        Player = new FieldPlayer(this, player);
        Currency = new CurrencyManager(this);
        Stats = new StatsManager(this);
        Housing = new HousingManager(this);
        Mail = new MailManager(this);

        Config = new ConfigManager(db, this);
        Buddy = new BuddyManager(db, this);
        Item = new ItemManager(db, this);

        if (!PrepareField(player.Character.MapId)) {
            Send(MigrationPacket.MoveResult(MigrationError.s_move_err_default));
            return false;
        }

        //session.Send(Packet.Of(SendOp.REQUEST_SYSTEM_INFO));
        Send(MigrationPacket.MoveResult(MigrationError.ok));

        // Survival
        // MeretMarket
        // UserConditionEvent
        // PCBangBonus
        Buddy.Load();

        Send(TimeSyncPacket.Reset(DateTimeOffset.UtcNow));
        Send(TimeSyncPacket.Set(DateTimeOffset.UtcNow));

        Send(StatsPacket.Init(Player));
        // Quest

        Send(RequestPacket.TickSync(Environment.TickCount));

        try {
            ChannelsResponse response = World.Channels(new ChannelsRequest());
            Send(ChannelPacket.Dynamic(response.Channels));
        } catch (RpcException ex) {
            Logger.Warning(ex, "Failed to populate channels");
        }
        Send(ServerEnterPacket.Request(Player));

        // Ugc
        // Cash
        // Gvg
        // Pvp
        Send(StateSyncPacket.SyncNumber(0));
        // SyncWorld
        // Prestige
        Item.Inventory.Load();
        Item.Furnishing.Load();
        // Quest
        // Achieve
        // MaidCraftItem
        // UserMaid
        // UserEnv
        // Fishing
        Pet?.Load();
        Send(PetPacket.LoadCollection(Player.Value.Unlock.Pets));
        // LegionBattle
        // CharacterAbility
        Config.LoadKeyTable();
        // GuideRecord
        // DailyWonder*
        Send(GameEventPacket.Load(server.GetGameEvents()));
        // BannerList
        // RoomDungeon
        // FieldEntrance
        // InGameRank
        Send(FieldEnterPacket.Request(Player));
        // HomeCommand
        // ResponseCube
        // Mentor
        Config.LoadChatStickers();
        // Mail
        Mail.Notify(true);
        // BypassKey
        // AH
        Config.LoadWardrobe();

        // Online Notifications


        return true;
    }

    private void LeaveField() {
        Array.Clear(ItemLockStaging);
        Array.Clear(DismantleStaging);
        DismantleOpened = false;
        Trade?.Dispose();
        Storage?.Dispose();
        Pet?.Dispose();
        Instrument = null;
        GuideObject = null;
        HeldCube = null;
        HeldLiftup = null;
        ActiveSkills.Clear();
        NpcScript = null;

        if (Field != null) {
            Scheduler.Stop();
            Field.RemovePlayer(Player.ObjectId, out _);
        }
    }

    public bool PrepareField(int mapId, int portalId = -1, in Vector3 position = default, in Vector3 rotation = default) {
        FieldManager? newField = FieldFactory.Get(mapId);
        if (newField == null) {
            return false;
        }

        State = SessionState.Moving;
        LeaveField();

        Field = newField;
        Player = Field.SpawnPlayer(this, Player, portalId, position, rotation);
        Config.Skill.UpdatePassiveBuffs();

        return true;
    }

    public bool EnterField() {
        if (Field == null) {
            return false;
        }

        Player.Value.Unlock.Maps.Add(Player.Value.Character.MapId);

        Field.OnAddPlayer(Player);
        Scheduler.Start();
        State = SessionState.Connected;

        Send(StatsPacket.Init(Player));
        Field.Broadcast(StatsPacket.Update(Player), Player.Session);

        var pWriter = Packet.Of(SendOp.UserState);
        pWriter.WriteInt(Player.ObjectId);
        pWriter.Write<ActorState>(ActorState.Fall);
        Send(pWriter);

        Send(EmotePacket.Load(Player.Value.Unlock.Emotes.Select(id => new Emote(id)).ToList()));
        Config.LoadMacros();

        Send(CubePacket.UpdateProfile(Player, true));
        Send(CubePacket.ReturnMap(Player.Value.Character.ReturnMapId));
        Send(RevivalPacket.Count(0)); // TODO: Consumed daily revivals?
        Send(RevivalPacket.Confirm(Player));
        Config.LoadStatAttributes();

        return true;
    }

    public void ReturnField() {
        Character character = Player.Value.Character;
        int mapId = character.ReturnMapId;
        Vector3 position = character.ReturnPosition;

        if (!MapMetadata.TryGet(mapId, out _)) {
            mapId = Constant.DefaultReturnMapId;
            position = default;
        }

        character.ReturnMapId = 0;
        character.ReturnPosition = default;

        // If returning to a map, pass in the spawn point.
        Send(PrepareField(mapId, position: position)
            ? FieldEnterPacket.Request(Player)
            : FieldEnterPacket.Error(MigrationError.s_move_err_default));
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
        Player.Rotation = new Vector3(0, 0, stateSync.Rotation / 10f);
        Player.State = stateSync.State;
        Player.SubState = stateSync.SubState;

        if (stateSync.SyncNumber != int.MaxValue) {
            Player.LastGroundPosition = stateSync.Position;
        }

        Field?.EnsurePlayerPosition(Player);
    }

    #region Dispose
    ~GameSession() => Dispose(false);

    protected override void Dispose(bool disposing) {
        if (disposed) return;
        disposed = true;

        try {
            Scheduler.Stop();
            server.OnDisconnected(this);
            LeaveField();
            State = SessionState.Disconnected;
            Complete();
        } finally {
#if !DEBUG
            if (Player.Value.Character.ReturnMapId != 0) {
                Player.Value.Character.MapId = Player.Value.Character.ReturnMapId;
            }
#endif

            using (GameStorage.Request db = GameStorage.Context()) {
                db.BeginTransaction();
                db.SavePlayer(Player);
                Config.Save(db);
                Item.Save(db);
                Housing.Save(db);
            }

            base.Dispose(disposing);
        }
    }
    #endregion
}
