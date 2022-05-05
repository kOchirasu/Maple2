using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Maple2.Database.Storage;
using Maple2.Model;
using Maple2.Model.Enum;
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
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Game.Session;

public sealed class GameSession : Core.Network.Session, IDisposable {
    protected override PatchType Type => PatchType.Ignore;
    public const int FIELD_KEY = 0x1234;

    private readonly GameServer Server;

    public long AccountId { get; private set; }
    public long CharacterId { get; private set; }
    public Guid MachineId { get; private set; }

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public GameStorage GameStorage { private get; init; } = null!;
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
        Server = server;
    }

    public bool EnterServer(long accountId, long characterId, Guid machineId) {
        AccountId = accountId;
        CharacterId = characterId;
        MachineId = machineId;

        Server.OnConnected(this);

        using GameStorage.Request db = GameStorage.Context();
        Player player = db.LoadPlayer(AccountId, CharacterId);
        if (player == null) {
            return false;
        }

        Item = new ItemManager(this);
        foreach ((EquipTab tab, List<Item> items) in db.GetEquips(CharacterId, EquipTab.Gear, EquipTab.Outfit, EquipTab.Badge)) {
            foreach (Item item in items) {
                switch (tab) {
                    case EquipTab.Gear:
                        Item.Equips.Gear[item.EquipSlot] = item;
                        break;
                    case EquipTab.Outfit:
                        Item.Equips.Outfit[item.EquipSlot] = item;
                        break;
                    case EquipTab.Badge:
                        if (item.Badge != null) {
                            Item.Equips.Badge[item.Badge.Type] = item;
                        }
                        break;
                }
            }
        }

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
        Send(TimeSyncPacket.Request());

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
        // ItemInventory
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

    #region Dispose
    ~GameSession() => Dispose(false);

    public new void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public new void Dispose(bool disposing) {
        if (disposed) {
            return;
        }

        try {
            Server.OnDisconnected(this);
            Field?.RemovePlayer(Player.ObjectId, out FieldPlayer? _);
            base.Dispose(disposing);
        } finally {
            using GameStorage.Request db = GameStorage.Context();
            db.BeginTransaction();
            db.SavePlayer(Player);
        }
    }
    #endregion
}
