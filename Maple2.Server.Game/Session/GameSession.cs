﻿using System;
using System.Collections;
using System.Collections.Generic;
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
using Maple2.Model.Game.Event;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Network;
using Maple2.Server.Core.Packets;
using Maple2.Server.Core.Sync;
using Maple2.Server.Game.Commands;
using Maple2.Server.Game.Manager;
using Maple2.Server.Game.Manager.Config;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Manager.Items;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Model.Skill;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Util;
using Maple2.Server.Game.Util.Sync;
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
    public string PlayerName => Player.Value.Character.Name;
    public Guid MachineId { get; private set; }
    public int Channel => server.Channel;

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public required GameStorage GameStorage { get; init; }
    public required WorldClient World { get; init; }
    public required ItemMetadataStorage ItemMetadata { get; init; }
    public required SkillMetadataStorage SkillMetadata { get; init; }
    public required TableMetadataStorage TableMetadata { get; init; }
    public required MapMetadataStorage MapMetadata { get; init; }
    public required FieldManager.Factory FieldFactory { private get; init; }
    public required Lua.Lua Lua { private get; init; }
    public required ItemStatsCalculator ItemStatsCalc { private get; init; }
    public required PlayerInfoStorage PlayerInfo { get; init; }
    // ReSharper restore All
    #endregion

    public ConfigManager Config { get; set; }
    public MailManager Mail { get; set; }
    public GuildManager Guild { get; set; }
    public BuddyManager Buddy { get; set; }
    public ItemManager Item { get; set; }
    public HousingManager Housing { get; set; }
    public CurrencyManager Currency { get; set; }
    public MasteryManager Mastery { get; set; }
    public StatsManager Stats => Player.Stats;
    public ItemEnchantManager ItemEnchant { get; set; }
    public ItemBoxManager ItemBox { get; set; }
    public BeautyManager Beauty { get; set; }
    public GameEventUserValueManager GameEventUserValue { get; set; }
    public ExperienceManager Exp { get; set; }
    public FieldManager? Field { get; set; }
    public FieldPlayer Player { get; private set; }

    public GameSession(TcpClient tcpClient, GameServer server, IComponentContext context) : base(tcpClient) {
        this.server = server;
        State = SessionState.ChangeMap;
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

        State = SessionState.ChangeMap;
        server.OnConnected(this);

        using GameStorage.Request db = GameStorage.Context();
        db.BeginTransaction();
        int objectId = FieldManager.NextGlobalId();
        Player? player = db.LoadPlayer(AccountId, CharacterId, objectId, (short) Channel);
        if (player == null) {
            Logger.Warning("Failed to load player from database: {AccountId}, {CharacterId}", AccountId, CharacterId);
            Send(MigrationPacket.MoveResult(MigrationError.s_move_err_default));
            return false;
        }
        db.Commit();

        Player = new FieldPlayer(this, player);
        Currency = new CurrencyManager(this);
        Mastery = new MasteryManager(this);
        Housing = new HousingManager(this);
        Mail = new MailManager(this);
        ItemEnchant = new ItemEnchantManager(this, Lua);
        ItemBox = new ItemBoxManager(this);
        Beauty = new BeautyManager(this);
        GameEventUserValue = new GameEventUserValueManager(this);
        Exp = new ExperienceManager(this, Lua);

        Guild = new GuildManager(this);
        Config = new ConfigManager(db, this);
        Buddy = new BuddyManager(db, this);
        Item = new ItemManager(db, this, ItemStatsCalc);

        if (!PrepareField(player.Character.MapId)) {
            Send(MigrationPacket.MoveResult(MigrationError.s_move_err_default));
            return false;
        }

        var playerUpdate = new PlayerUpdateRequest {
            AccountId = accountId,
            CharacterId = characterId,
        };
        playerUpdate.SetFields(UpdateField.All, player);
        PlayerInfo.SendUpdate(playerUpdate);

        //session.Send(Packet.Of(SendOp.REQUEST_SYSTEM_INFO));
        Send(MigrationPacket.MoveResult(MigrationError.ok));

        // Survival
        // MeretMarket
        // UserConditionEvent
        // PCBangBonus
        Guild.Load();
        // Club
        Buddy.Load();

        Send(TimeSyncPacket.Reset(DateTimeOffset.UtcNow));
        Send(TimeSyncPacket.Set(DateTimeOffset.UtcNow));

        Send(StatsPacket.Init(Player));

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
        // Load Quests
        Send(QuestPacket.StartLoad(0));
        // Send(QuestPacket.LoadSkyFortressMissions(Array.Empty<int>()));
        // Send(QuestPacket.LoadKritiasMissions(Array.Empty<int>()));
        Send(QuestPacket.LoadQuestStates(player.Unlock.Quests.Values));
        // Send(QuestPacket.LoadQuests(Array.Empty<int>()));
        // Achieve
        // MaidCraftItem
        // UserMaid
        // UserEnv
        Send(UserEnvPacket.LoadTitles(Player.Value.Unlock.Titles));
        Send(UserEnvPacket.LoadClaimedRewards(Player.Value.Unlock.MasteryRewardsClaimed));
        Send(FishingPacket.LoadAlbum(Player.Value.Unlock.FishAlbum.Values));
        Pet?.Load();
        Send(PetPacket.LoadCollection(Player.Value.Unlock.Pets));
        // LegionBattle
        // CharacterAbility
        Config.LoadKeyTable();
        // GuideRecord
        // DailyWonder*
        GameEventUserValue.Load();
        Send(GameEventPacket.Load(db.GetEvents()));
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

    public bool PrepareField(int mapId, int portalId = -1, long ownerId = 0, in Vector3 position = default, in Vector3 rotation = default) {
        // If entering home without instanceKey set, default to own home.
        if (mapId == Player.Value.Home.Indoor.MapId && ownerId == 0) {
            ownerId = AccountId;
        }

        FieldManager? newField = FieldFactory.Get(mapId, ownerId);
        if (newField == null) {
            return false;
        }

        State = SessionState.ChangeMap;
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

        PlayerInfo.SendUpdate(new PlayerUpdateRequest {
            AccountId = AccountId,
            CharacterId = CharacterId,
            MapId = Field.MapId,
            Async = true,
        });

        Send(StatsPacket.Init(Player));
        Field.Broadcast(StatsPacket.Update(Player), Player.Session);

        var pWriter = Packet.Of(SendOp.UserState);
        pWriter.WriteInt(Player.ObjectId);
        pWriter.Write<ActorState>(ActorState.Fall);
        Send(pWriter);

        Send(EmotePacket.Load(Player.Value.Unlock.Emotes.Select(id => new Emote(id)).ToList()));
        Config.LoadMacros();
        Config.LoadSkillCooldowns();

        Send(CubePacket.UpdateProfile(Player, true));
        Send(CubePacket.ReturnMap(Player.Value.Character.ReturnMapId));
        Config.LoadLapenshard();
        Send(RevivalPacket.Count(0)); // TODO: Consumed daily revivals?
        Send(RevivalPacket.Confirm(Player));
        Config.LoadStatAttributes();
        Player.Buffs.LoadFieldBuffs();
        Send(PremiumCubPacket.Activate(Player.ObjectId, Player.Value.Account.PremiumTime));
        Send(PremiumCubPacket.LoadItems(Player.Value.Account.PremiumRewardsClaimed));
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

    public GameEvent? FindEvent<T>() where T : GameEventInfo {
        return server.FindEvent<T>();
    }
    
    public void ChannelBroadcast(ByteWriter packet) {
        server.Broadcast(packet);
    }

    public IDictionary<int, SkillCooldown> GetSkillCooldowns() {
        return server.FindPlayerCooldowns(CharacterId);
    }

    public void CacheSkillCooldowns() {
        server.StorePlayerCooldowns(CharacterId, Config.GetCurrentSkillCooldowns());
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

        if (State == SessionState.Connected) {
            PlayerInfo.SendUpdate(new PlayerUpdateRequest {
                AccountId = AccountId,
                CharacterId = CharacterId,
                MapId = 0,
                Channel = 0,
                Async = true,
            });
        }

        try {
            Scheduler.Stop();
            server.OnDisconnected(this);
            LeaveField();
            Player.Value.Character.Channel = 0;
            Player.Value.Account.Online = false;
            State = SessionState.Disconnected;
            Complete();
        } finally {
#if !DEBUG
            if (Player.Value.Character.ReturnMapId != 0) {
                Player.Value.Character.MapId = Player.Value.Character.ReturnMapId;
            }
#endif
            Guild.Dispose();
            Buddy.Dispose();
            CacheSkillCooldowns();
            using (GameStorage.Request db = GameStorage.Context()) {
                db.BeginTransaction();
                db.SavePlayer(Player);
                Config.Save(db);
                Item.Save(db);
                Housing.Save(db);
                GameEventUserValue.Save(db);
            }

            base.Dispose(disposing);
        }
    }
    #endregion
}
