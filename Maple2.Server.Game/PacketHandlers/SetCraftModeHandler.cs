using System.Diagnostics;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.PacketHandlers;

public class SetCraftModeHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.RequestSetCraftMode;

    private enum Command : byte {
        Stop = 0,
        Start = 1,
    }

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.Stop:
                HandleStop(session, packet);
                return;
            case Command.Start:
                HandleStart(session, packet);
                return;
        }
    }

    private void HandleStop(GameSession session, IByteReader packet) {
        bool isStart = packet.ReadBool();
        Debug.Assert(!isStart);

        if (session.Field == null || session.GuideObject == null) {
            return;
        }

        session.Field.Broadcast(SetCraftModePacket.Stop(session.Player.ObjectId));
        session.Field.Broadcast(GuideObjectPacket.Remove(session.GuideObject));
        session.GuideObject = null;
        session.HeldCube = null;
    }

    private void HandleStart(GameSession session, IByteReader packet) {
        bool isStart = packet.ReadBool();
        Debug.Assert(isStart);

        if (session.Field == null || session.GuideObject != null) {
            return;
        }

        var cubeItem = packet.ReadClass<HeldCube>();
        if (cubeItem.ItemId != Constant.ConstructionCubeItemId || cubeItem.Template != null) {
            return;
        }

        session.GuideObject = session.Field.SpawnGuideObject(session.Player, new ConstructionGuideObject());
        session.HeldCube = cubeItem;

        session.Field.Broadcast(GuideObjectPacket.Create(session.GuideObject));
        session.Field.Broadcast(SetCraftModePacket.Home(session.Player.ObjectId, cubeItem));
    }
}
