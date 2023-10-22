using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Numerics;
using Maple2.Database.Storage;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Commands;

public class CoordCommand : Command {
    private const string NAME = "coord";
    private const string DESCRIPTION = "Move to specified coordinates.";

    private readonly GameSession session;
    private readonly MapMetadataStorage mapStorage;

    public CoordCommand(GameSession session, MapMetadataStorage mapStorage) : base(NAME, DESCRIPTION) {
        this.session = session;
        this.mapStorage = mapStorage;

        var xPosition = new Argument<int>("x", "X Coordinate.");
        var yPosition = new Argument<int>("y", "Y Coordinate.");
        var zPosition = new Argument<int>("z", "Z Coordinate.");

        AddArgument(xPosition);
        AddArgument(yPosition);
        AddArgument(zPosition);
        this.SetHandler<InvocationContext, int, int, int>(Handle, xPosition, yPosition, zPosition);
    }

    private void Handle(InvocationContext ctx, int x, int y, int z) {
        try {
            Vector3 position = new(x, y, z);
            ctx.Console.Out.WriteLine($"Moving to '{position}'");
            session.Send(PortalPacket.MoveByPortal(session.Player, position, session.Player.Rotation));
            ctx.ExitCode = 0;
        } catch (SystemException ex) {
            ctx.Console.Error.WriteLine(ex.Message);
            ctx.ExitCode = 1;
        }
    }
}
