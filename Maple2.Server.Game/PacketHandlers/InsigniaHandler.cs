using System;
using Maple2.Database.Extensions;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class InsigniaHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.Insignia;

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public required TableMetadataStorage TableMetadata { private get; init; }
    // ReSharper restore All
    #endregion

    public override void Handle(GameSession session, IByteReader packet) {
        if (session.Field == null) {
            return;
        }

        // Check and remove any existing insignia buffs
        if (TableMetadata.InsigniaTable.Entries.TryGetValue(session.Player.Value.Character.Insignia, out InsigniaTable.Entry? oldSigniaMetadata) && oldSigniaMetadata.BuffId > 0) {
            session.Player.Buffs.Remove(oldSigniaMetadata.BuffLevel);
        }

        short insigniaId = packet.ReadShort();
        if (!TableMetadata.InsigniaTable.Entries.TryGetValue(insigniaId, out InsigniaTable.Entry? newSigniaMetadata)) {
            return;
        }

        session.Player.Value.Character.Insignia = insigniaId;

        bool display = false;
        switch (newSigniaMetadata.Type) {
            case InsigniaConditionType.title:
                if (session.Player.Value.Unlock.Titles.Contains(newSigniaMetadata.Code)) {
                    display = true;
                }
                break;
            case InsigniaConditionType.adventure_level:
                if (session.Player.Value.Account.PrestigeLevel >= 100) {
                    display = true;
                }
                break;
            case InsigniaConditionType.trophy_point:
                if (session.Player.Value.Character.AchievementInfo.Total > 1000) {
                    display = true;
                }
                break;
            case InsigniaConditionType.enchant:
                foreach ((EquipSlot slot, Item item) in session.Item.Equips.Gear) {
                    if (item is {Enchant: {Enchants: >= 12}, Rarity: > 3}) {
                        display = true;
                        break;
                    }
                }
                break;
            case InsigniaConditionType.level:
                if (session.Player.Value.Character.Level >= 50) {
                    display = true;
                }
                break;
            case InsigniaConditionType.vip:
                if (session.Player.Value.Account.PremiumTime > DateTime.UtcNow.ToEpochSeconds()) {
                    display = true;
                }
                break;
            default:
                Logger.Information("Unhandled insignia condition type: {type}", newSigniaMetadata.Type);
                break;
        }

        if (display && newSigniaMetadata.BuffId > 0) {
            session.Player.Buffs.AddBuff(session.Player, session.Player, newSigniaMetadata.BuffId, newSigniaMetadata.BuffLevel);
        }

        session.Field.Broadcast(InsigniaPacket.Update(session.Player.ObjectId, insigniaId, display));
    }
}
