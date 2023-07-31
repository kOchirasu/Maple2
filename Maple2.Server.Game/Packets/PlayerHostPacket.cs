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

    public static ByteWriter AdBalloonWindow(InteractBillBoardObject interact) {
        var pWriter = Packet.Of(SendOp.PlayerHost);
        pWriter.Write<Command>(Command.AdBalloonWindow);
        pWriter.WriteLong(interact.Owner.AccountId);
        pWriter.WriteLong(interact.Owner.Id);
        pWriter.WriteUnicodeString(interact.Owner.Picture);
        pWriter.WriteUnicodeString(interact.Owner.Name);
        pWriter.WriteShort(interact.Owner.Level);
        pWriter.Write(interact.Owner.Job.Code());
        pWriter.WriteShort();
        pWriter.WriteUnicodeString(interact.Title);
        pWriter.WriteUnicodeString(interact.Description);
        pWriter.WriteBool(interact.PublicHouse);
        pWriter.WriteLong(interact.CreationTime);
        pWriter.WriteLong(interact.ExpirationTime);
        pWriter.WriteLong();

        return pWriter;
    }
}
