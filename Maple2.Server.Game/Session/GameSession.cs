using System;
using System.Collections.Generic;
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

public sealed class GameSession : Core.Network.Session {
    protected override PatchType Type => PatchType.Ignore;

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global, UnusedAutoPropertyAccessor.Global
    public GameStorage GameStorage { private get; init; }
    public SkillMetadataStorage SkillMetadata { private get; init; }
    public TableMetadataStorage TableMetadata { private get; init; }
    public FieldManager.Factory FieldFactory { private get; init; }
    // ReSharper restore All
    #endregion

    public FieldManager Field { get; set; }
    public ItemManager Item { get; set; }
    public SkillManager Skill { get; set; }
    public FieldPlayer Player { get; private set; }

    public GameSession(ILogger<GameSession> logger) : base(logger) { }

    public bool EnterServer(long accountId, long characterId) {
        using GameStorage.Request db = GameStorage.Context();
        Player player = db.LoadPlayer(accountId, characterId);
        if (player == null) {
            return false;
        }

        Item = new ItemManager(this);
        foreach ((EquipTab tab, List<Item> items) in db.GetEquips(characterId, EquipTab.Gear, EquipTab.Outfit, EquipTab.Badge)) {
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

        // Stat
        // Quest

        Send(RequestPacket.TickSync(Environment.TickCount));

        // DynamicChannel

        if (!EnterField(player)) {
            return false;
        }

        Send(ServerEnterPacket.Request(Player));

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
        // RequestFieldEnter
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

    public bool EnterField(Player player) {
        Field = FieldFactory.Get(player.Character.MapId);
        Player = Field.SpawnPlayer(this, player);
        Field.OnAddPlayer(Player);

        return true;
    }

    public override void Dispose() {
        try {
            Field?.RemovePlayer(Player.ObjectId, out FieldPlayer _);
            base.Dispose();
        } finally {
            using GameStorage.Request db = GameStorage.Transaction();
            db.SavePlayer(Player);
        }
    }
}
