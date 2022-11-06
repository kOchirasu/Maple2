using System.Collections.Generic;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Packets;

public static class ChatStickerPacket {
    private enum Command : byte {
        Load = 0,
        ExpiredNotify = 1,
        Add = 2,
        Use = 3,
        GroupChat = 4,
        Favorite = 5,
        Unfavorite = 6
    }

    public static ByteWriter Load(List<int> favorites, List<ChatSticker> stickers) {
        var pWriter = Packet.Of(SendOp.ChatStamp);
        pWriter.Write(Command.Load);
        pWriter.WriteShort((short) favorites.Count);
        foreach (int favorite in favorites) {
            pWriter.WriteInt(favorite);
        }
        pWriter.WriteShort((short) stickers.Count);
        foreach (ChatSticker sticker in stickers) {
            pWriter.Write(sticker);
        }

        return pWriter;
    }
    
    public static ByteWriter Add(Item item, ChatSticker sticker) {
        var pWriter = Packet.Of(SendOp.ChatStamp);
        pWriter.Write(Command.Add);
        pWriter.WriteInt(item.Id);
        pWriter.WriteInt(item.Amount);
        pWriter.Write(sticker);

        return pWriter;
    }
}
