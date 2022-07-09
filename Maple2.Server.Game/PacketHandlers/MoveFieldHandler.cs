using System.Numerics;
using Maple2.Database.Storage;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using static Maple2.Model.Error.MigrationError;

namespace Maple2.Server.Game.PacketHandlers;

public class MoveFieldHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.RequestMoveField;

    private enum Command : byte {
        Portal = 0,
        LeaveDungeon = 1,
        VisitHome = 2,
        Return = 3,
        DecorPlanner = 4,
        BlueprintDesigner = 5,
        ModelHome = 6,
    }

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public MapMetadataStorage MapMetadata { get; init; } = null!;
    // ReSharper restore All
    #endregion

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();

        switch (command) {
            case Command.Portal:
                HandlePortal(session, packet);
                return;
            case Command.VisitHome: // s_action_privilege_portal
                HandleVisitHome(session, packet);
                return;
            case Command.LeaveDungeon:
            case Command.Return:
                HandleReturn(session);
                return;
            case Command.DecorPlanner: // s_tutorial_designHome_limit
                HandleDecorPlanner(session);
                return;
            case Command.BlueprintDesigner: // s_tutorial_blueprint_limit
                HandleBlueprintDesigner(session);
                return;
            case Command.ModelHome: // s_meratmarket_ask_move_to_modelhouse
                HandleModelHome(session);
                return;
        }
    }

    private void HandlePortal(GameSession session, IByteReader packet) {
        if (session.Field == null) return;

        int mapId = packet.ReadInt();
        if (mapId != session.Field.MapId) {
            return;
        }

        int portalId = packet.ReadInt();
        packet.ReadInt();
        packet.ReadInt();
        packet.ReadUnicodeString();
        packet.ReadUnicodeString();

        session.Field.UsePortal(session, portalId);
    }

    private void HandleVisitHome(GameSession session, IByteReader packet) {

    }

    private void HandleReturn(GameSession session) {
        session.ReturnField();
    }

    private void HandleDecorPlanner(GameSession session) {

    }

    private void HandleBlueprintDesigner(GameSession session) {

    }

    private void HandleModelHome(GameSession session) {

    }
}
