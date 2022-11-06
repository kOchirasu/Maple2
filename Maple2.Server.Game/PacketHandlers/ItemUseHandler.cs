using System;
using System.Collections.Generic;
using Maple2.Database.Extensions;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Server.Game.Util;

namespace Maple2.Server.Game.PacketHandlers;

public class ItemUseHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.RequestItemUse;

    public override void Handle(GameSession session, IByteReader packet) {
        long itemUid = packet.ReadLong();
        string unknown = packet.ReadUnicodeString();

        Item? item = session.Item.Inventory.Get(itemUid);
        if (item == null) {
            Logger.Warning("RequestItemUse for invalid item:{ItemUid}", itemUid);
            return;
        }

        switch (item.Metadata.Function?.Type) {
            case ItemFunction.StoryBook:
                HandleStoryBook(session, item);
                break;
            case ItemFunction.ChatEmoticonAdd:
                HandleChatSticker(session, item);
                break;
            default:
                Logger.Warning("Unhandled item function: {Name}", item.Metadata.Function?.Type);
                return;
        }
    }

    private static void HandleStoryBook(GameSession session, Item item) {
        if (!int.TryParse(item.Metadata.Function?.Parameters, out int storyBookId)) {
            return;
        }
        
        session.Send(StoryBookPacket.Load(storyBookId));
    }

    private static void HandleChatSticker(GameSession session, Item item) {
        Dictionary<string, string> parameters = XmlParseUtil.GetParameters(item.Metadata?.Function?.Parameters);

        if (!parameters.ContainsKey("id")) {
            session.Send(ChatStickerPacket.Error(ChatStickerError.s_msg_chat_emoticon_add_failed));
            return;
        }

        int stickerSetId = int.Parse(parameters["id"]);

        int duration = 0;
        if (parameters.TryGetValue(parameters["id"], out string? durationString)) {
            int.TryParse(durationString, out duration);
        }
        
        if (session.Player.Value.Unlock.StickerSets.ContainsKey(stickerSetId)) {
            if (session.Player.Value.Unlock.StickerSets[stickerSetId] == long.MaxValue) {
                session.Send(ChatStickerPacket.Error(ChatStickerError.s_msg_chat_emoticon_add_failed_already_exist));
                return;
            }

            // does not expire
            if (duration == 0) {
                session.Player.Value.Unlock.StickerSets[stickerSetId] = long.MaxValue;
            } else {
                session.Player.Value.Unlock.StickerSets[stickerSetId] = Math.Min(session.Player.Value.Unlock.StickerSets[stickerSetId] + duration, long.MaxValue);
            }
        } else {
            if (duration == 0) {
                session.Player.Value.Unlock.StickerSets[stickerSetId] = long.MaxValue;
            } else {
                session.Player.Value.Unlock.StickerSets[stickerSetId] = duration + DateTime.Now.ToEpochSeconds();
            }
        }

        if (!session.Item.Inventory.Consume(item.Uid, 1)) {
            return;
        }
        
        session.Send(ChatStickerPacket.Add(item, new(stickerSetId, session.Player.Value.Unlock.StickerSets[stickerSetId])));
    }
}
