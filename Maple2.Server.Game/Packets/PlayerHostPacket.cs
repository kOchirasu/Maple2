using Maple2.Model;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;

namespace Maple2.Server.Game.Packets;

public static class PlayerHostPacket {
    private enum Command : byte {
        UseHongBao = 0,
        HongBaoGiftNotice = 2,
        StartMiniGame = 3,
        MiniGameRewardNotice = 4,
        MiniGameRewardReceive = 5,
        AdBalloonWindow = 6,
    }

    public static ByteWriter AdBalloonWindow(InteractBillBoardObject billboard) {
        var pWriter = Packet.Of(SendOp.PlayerHost);
        pWriter.Write<Command>(Command.AdBalloonWindow);
        pWriter.WriteLong(billboard.Owner.AccountId);
        pWriter.WriteLong(billboard.Owner.Id);
        pWriter.WriteUnicodeString(billboard.Owner.Picture);
        pWriter.WriteUnicodeString(billboard.Owner.Name);
        pWriter.WriteShort(billboard.Owner.Level);
        pWriter.Write(billboard.Owner.Job.Code());
        pWriter.WriteShort();
        pWriter.WriteUnicodeString(billboard.Title);
        pWriter.WriteUnicodeString(billboard.Description);
        pWriter.WriteBool(billboard.PublicHouse);
        pWriter.WriteLong(billboard.CreationTime);
        pWriter.WriteLong(billboard.ExpirationTime);
        pWriter.WriteLong();

        return pWriter;
    }
}
