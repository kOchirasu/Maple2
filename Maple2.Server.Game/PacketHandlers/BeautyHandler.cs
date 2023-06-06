using Maple2.Database.Storage;
using Maple2.Model.Common;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Game.Shop;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.PacketHandlers;

public class BeautyHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.Beauty;

    private enum Command : byte {
        Shop = 0,
        CreateBeauty = 3,
        Unknown4 = 4, // UpdateEar?
        UpdateBeauty = 5,
        UpdateSkin = 6,
        RandomHair = 7,
        Warp = 10,
        ConfirmRandomHair = 12,
        SaveHair = 16,
        AddSlots = 17,
        DeleteHair = 18,
        AskAddSlots = 19,
        ApplySavedHair = 21,
        GearDye = 22,
        Voucher = 23,
    }

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public required NpcMetadataStorage NpcMetadata { private get; init; }
    // ReSharper restore All
    #endregion

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.Shop:
                HandleShop(session, packet);
                return;
            case Command.CreateBeauty:
                HandleCreateBeauty(session, packet);
                return;
            case Command.Unknown4:
                packet.ReadLong();
                return;
            case Command.UpdateBeauty:
                HandleUpdateBeauty(session, packet);
                return;
            case Command.UpdateSkin:
                HandleUpdateSkin(session, packet);
                return;
            case Command.RandomHair:
                HandleRandomHair(session, packet);
                return;
            case Command.Warp:
                HandleWarp(session, packet);
                return;
            case Command.ConfirmRandomHair:
                HandleConfirmRandomHair(session, packet);
                return;
            case Command.SaveHair:
                HandleSaveHair(session, packet);
                return;
            case Command.AddSlots:
                HandleAddSlots(session, packet);
                return;
            case Command.DeleteHair:
                HandleDeleteHair(session, packet);
                return;
            case Command.AskAddSlots:
                HandleAskAddSlots(session, packet);
                return;
            case Command.ApplySavedHair:
                HandleApplySavedHair(session, packet);
                return;
            case Command.GearDye:
                HandleGearDye(session, packet);
                return;
            case Command.Voucher:
                HandleVoucher(session, packet);
                return;
        }
    }

    private void HandleShop(GameSession session, IByteReader packet) {
        int npcId = packet.ReadInt();
        var shopType = (BeautyShopType) packet.ReadByte();

        if (!NpcMetadata.TryGet(npcId, out NpcMetadata? metadata)) {
            return;
        }

        using GameStorage.Request db = session.GameStorage.Context();
        BeautyShop? beautyShop = db.GetBeautyShop(metadata.Basic.ShopId);
        if (beautyShop == null) {
            // TODO: Error?
            return;
        }

        switch (beautyShop.Category) {
            case BeautyShopCategory.Dye:
                switch (beautyShop.ShopType) {
                    case BeautyShopType.Item:
                        break;
                    case BeautyShopType.Skin:
                        break;
                    default:
                        return;
                }
                break;
            case BeautyShopCategory.Save:
                break;
            case BeautyShopCategory.Special:
            case BeautyShopCategory.Standard:
                break;
            default:
                return;
        }
        /*int shopId = metadata.Basic.ShopId;
        switch (shopId) {
            case 500:
                session.Send(BeautyPacket.BeautyShop(BeautyShop.Face()));
                return;
            case 501:
                session.Send(BeautyPacket.BeautyShop(BeautyShop.Skin()));
                return;
            case 504:
                session.Send(BeautyPacket.BeautyShop(BeautyShop.Hair()));
                return;
            case 505:
                session.Send(BeautyPacket.BeautyShop(BeautyShop.Cosmetic()));
                return;
            case 506:
                session.Send(BeautyPacket.DyeShop(BeautyShop.Dye()));
                return;
            case 508:
                session.Send(BeautyPacket.BeautyShop(BeautyShop.RandomHair()));
                return;
            case 509:
                session.Send(BeautyPacket.BeautyShop(BeautyShop.SpecialHair()));
                return;
            case 510:
                session.Send(BeautyPacket.SaveShop(BeautyShop.SavedHair()));
                return;
        }*/
    }

    private void HandleCreateBeauty(GameSession session, IByteReader packet) {
        byte index = packet.ReadByte();
        bool useVoucher = packet.ReadBool();
        int shopId = packet.ReadInt();
        var appearance = packet.ReadClass<HairAppearance>();
    }

    private void HandleUpdateBeauty(GameSession session, IByteReader packet) {
        byte index = packet.ReadByte();
        bool useVoucher = packet.ReadBool();
        long uid = packet.ReadLong();
        var appearance = packet.ReadClass<HairAppearance>();
    }

    private void HandleUpdateSkin(GameSession session, IByteReader packet) {
        byte index = packet.ReadByte();
        var skinColor = packet.Read<SkinColor>();
        bool useVoucher = packet.ReadBool();
    }

    private void HandleRandomHair(GameSession session, IByteReader packet) {
        int shopId = packet.ReadInt();
        bool useVoucher = packet.ReadBool();
    }

    private void HandleWarp(GameSession session, IByteReader packet) {
        short type = packet.ReadShort();
        int mapId = type switch {
            1 => Constant.BeautyHairShopGotoFieldID,
            3 => Constant.BeautyFaceShopGotoFieldID,
            5 => Constant.BeautyColorShopGotoFieldID,
            _ => 0
        };
        int portalId = type switch {
            1 => Constant.BeautyHairShopGotoPortalID,
            3 => Constant.BeautyFaceShopGotoPortalID,
            5 => Constant.BeautyColorShopGotoPortalID,
            _ => 0
        };

        session.Send(session.PrepareField(mapId, portalId: portalId)
            ? FieldEnterPacket.Request(session.Player)
            : FieldEnterPacket.Error(MigrationError.s_move_err_default));
    }

    private void HandleConfirmRandomHair(GameSession session, IByteReader packet) {
        packet.ReadByte();
    }

    private void HandleSaveHair(GameSession session, IByteReader packet) {
        long uid = packet.ReadLong();
    }

    private void HandleAddSlots(GameSession session, IByteReader packet) {
        long uid = packet.ReadLong();
    }

    private void HandleDeleteHair(GameSession session, IByteReader packet) {
        long uid = packet.ReadLong();
        bool delete = packet.ReadBool();
    }

    private void HandleAskAddSlots(GameSession session, IByteReader packet) {

    }

    private void HandleApplySavedHair(GameSession session, IByteReader packet) {
        long uid = packet.ReadLong();
        byte index = packet.ReadByte();
    }

    private void HandleGearDye(GameSession session, IByteReader packet) {
        byte count = packet.ReadByte();
        for (byte i = 0; i < count; i++) {
            bool isValid = packet.ReadBool();
            if (!isValid) {
                continue;
            }

            packet.ReadByte();
            bool useVoucher = packet.ReadBool();
            packet.ReadByte();
            packet.ReadInt();
            packet.ReadLong();
            long itemUid = packet.ReadLong();
            int itemId = packet.ReadInt();
        }
    }

    private void HandleVoucher(GameSession session, IByteReader packet) {
        long uid = packet.ReadLong();
    }
}
