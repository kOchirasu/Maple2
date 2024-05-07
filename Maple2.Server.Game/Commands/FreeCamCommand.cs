﻿using System.CommandLine;
using System.CommandLine.Invocation;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Commands;

public class FreeCamCommand : Command {
    private const string NAME = "freecam";
    private const string DESCRIPTION = "Enables a free cam user interface.";

    private readonly GameSession session;

    public FreeCamCommand(GameSession session) : base(NAME, DESCRIPTION) {
        this.session = session;
        var toggle = new Option<string>(new[] { "toggle" }, () => "on", "Enable free cam. \"on\" or \"off\"");

        AddOption(toggle);
        this.SetHandler<InvocationContext, string>(Handle, toggle);
    }

    private void Handle(InvocationContext ctx, string toggle) {
        if (toggle == "off") {
            session.Send(FieldPropertyPacket.Remove(FieldProperty.PhotoStudio));
        } else {
            session.Send(FieldPropertyPacket.Add(new FieldPropertyPhotoStudio {
                Enabled = true,
            }));
        }
    }
}
