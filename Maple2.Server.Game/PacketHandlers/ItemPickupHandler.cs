using Maple2.Model;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class ItemPickupHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.RequestItemPickup;

    public override void Handle(GameSession session, IByteReader packet) {
        int objectId = packet.ReadInt();
        packet.ReadByte();

        if (session.Field == null) {
            return;
        }

        // Ensure item exists.
        if (!session.Field.TryGetItem(objectId, out FieldEntity<Item>? item)) {
            return;
        }

        // Currency items are handled differently
        if (item.Value.IsCurrency()) {
            // Remove objectId from Field, make sure item still exists (multiple looters)
            if (!session.Field.PickupItem(session.Player, objectId, out item)) {
                return;
            }

            switch (item.Value.Id) {
                // Meso: 90000001, 90000002, 90000003 (See: MesoPickupHandler)
                case 90000004: // Meret
                // case 90000011: // Meret (Secondary)
                // case 90000015: // GameMeret (Secondary)
                // case 90000016: // EventMeret (Secondary)
                // case 90000020: // RedMeret
                    session.Currency.Meret += item.Value.Amount;
                    break;
                case 90000006: // ValorToken
                    session.Currency[CurrencyType.ValorToken] += item.Value.Amount;
                    break;
                case 90000008: // ExperienceOrb
                    break;
                case 90000009: // SpiritOrb
                    break;
                case 90000010: // StaminaOrb
                    break;
                case 90000013: // Rue
                    session.Currency[CurrencyType.Rue] += item.Value.Amount;
                    break;
                case 90000014: // HaviFruit
                    session.Currency[CurrencyType.HaviFruit] += item.Value.Amount;
                    break;
                case 90000017: // Treva
                    session.Currency[CurrencyType.Treva] += item.Value.Amount;
                    break;
                case 90000027: // MesoToken
                    session.Currency[CurrencyType.MesoToken] += item.Value.Amount;
                    break;
                // case 90000005: // DungeonKey
                // case 90000007: // Karma
                // case 90000012: // Unknown (BookIcon)
                // case 90000018: // ShadowFragment
                // case 90000019: // DistinctPaul
                // case 90000021: // GuildFunds
                case 90000022: // ReverseCoin
                    session.Currency[CurrencyType.ReverseCoin] += item.Value.Amount;
                    break;
                case 90000023: // MentorPoint
                    session.Currency[CurrencyType.MentorToken] += item.Value.Amount;
                    break;
                case 90000024: // MenteePoint
                    session.Currency[CurrencyType.MenteeToken] += item.Value.Amount;
                    break;
                case 90000025: // StarPoint
                    session.Currency[CurrencyType.StarPoint] += item.Value.Amount;
                    break;
                // case 90000026: // Unknown (Blank)
            }

            session.Item.Inventory.Discard(item);
            return;
        }

        lock (session.Item) {
            if (!session.Item.Inventory.CanAdd(item)) {
                return;
            }

            // Remove objectId from Field, make sure item still exists (multiple looters)
            if (!session.Field.PickupItem(session.Player, objectId, out item)) {
                return;
            }

            item.Value.Slot = -1;
            if (session.Item.Inventory.Add(item, true) && item.Value.Metadata.Limit.TransferType == 2) {
                session.Item.Bind(item);
            }
        }
    }
}
