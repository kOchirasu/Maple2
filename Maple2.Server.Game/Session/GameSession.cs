using System;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Server.Core.Network;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Microsoft.Extensions.Logging;

namespace Maple2.Server.Game.Session;

public sealed class GameSession : Core.Network.Session {
    protected override PatchType Type => PatchType.Ignore;
    
    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global, UnusedAutoPropertyAccessor.Global
    public GameStorage GameStorage { private get; init; }
    public FieldManager.Factory FieldFactory { private get; init; }
    // ReSharper restore All
    #endregion
    
    public FieldManager Field { get; set; }
    public FieldPlayer Player { get; private set; }

    public GameSession(ILogger<GameSession> logger) : base(logger) { }

    public bool EnterServer(long accountId, long characterId) {
        using GameStorage.Request db = GameStorage.Context();
        Player player = db.LoadPlayer(accountId, characterId);
        if (player == null) {
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

        using (GameStorage.Request db = GameStorage.Context()) {
            db.GetEquips(player.Character.Id, EquipTab.Gear, EquipTab.Outfit, EquipTab.Badge);
        }

        return true;
    }
}
