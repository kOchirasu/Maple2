﻿using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.PacketHandlers;

public class InteractObjectHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.InteractObject;

    private enum Command : byte {
        Start = 11,
        End = 12,
    }

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.Start:
                HandleStart(session, packet);
                return;
            case Command.End:
                HandleEnd(session, packet);
                return;
        }
    }

    private void HandleStart(GameSession session, IByteReader packet) {
        string entityId = packet.ReadString();

        if (session.Field?.TryGetInteract(entityId, out FieldInteract? interact) != true) {
            return;
        }
    }

    private void HandleEnd(GameSession session, IByteReader packet) {
        string entityId = packet.ReadString();

        if (session.Field?.TryGetInteract(entityId, out FieldInteract? interact) == true && interact.React()) {
            switch (interact.Value.Type) {
                case InteractType.Mesh:
                    session.Send(InteractObjectPacket.Interact(interact));
                    break;
                case InteractType.Telescope:
                    session.Send(InteractObjectPacket.Interact(interact));
                    session.Send(InteractObjectPacket.Result(InteractResult.s_interact_find_new_telescope, interact));
                    if (!session.Player.Value.Unlock.InteractedObjects.Contains(interact.Object.Id)) {
                        session.Trophy.Update(TrophyConditionType.interact_object, codeLong: interact.Object.Id);
                    }
                    break;
                case InteractType.Ui:
                    session.Send(InteractObjectPacket.Interact(interact));
                    break;
                case InteractType.Web:
                case InteractType.DisplayImage:
                case InteractType.Gathering:
                case InteractType.GuildPoster:
                case InteractType.BillBoard: // AdBalloon
                    session.Send(PlayerHostPacket.AdBalloonWindow((interact.Object as InteractBillBoardObject)!));
                    break;
                case InteractType.WatchTower:
                    break;
            }
        }
    }
}
