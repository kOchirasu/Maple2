﻿using System.Linq;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class SuperChatHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.SuperWorldChat;

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public required ItemMetadataStorage ItemMetadata { private get; init; }
    // ReSharper restore All
    #endregion

    private enum Command : byte {
        Select = 0,
        Deselect = 1,
    }

    public override void Handle(GameSession session, IByteReader packet) {
        var function = packet.Read<Command>();
        switch (function) {
            case Command.Select:
                HandleSelect(session, packet);
                return;
            case Command.Deselect:
                HandleDeselect(session);
                return;
        }
    }

    private static void HandleSelect(GameSession session, IByteReader packet) {
        int itemId = packet.ReadInt();

        Item? superChatItem = session.Item.Inventory.Find(itemId).First();
        if (!int.TryParse(superChatItem.Metadata.Function?.Parameters.Split(",").First(), out int superChatId)) {
            return;
        }

        session.Player.Value.Character.SuperChatId = superChatId;
        session.Send(SuperChatPacket.Select(session.Player.ObjectId, itemId));
    }

    private static void HandleDeselect(GameSession session) {
        session.Player.Value.Character.SuperChatId = 0;
        session.Send(SuperChatPacket.Deselect(session.Player.ObjectId));
    }
}
