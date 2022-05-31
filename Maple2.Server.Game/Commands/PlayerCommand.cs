using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using Maple2.Server.Core.Constants;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Commands;

public class PlayerCommand : Command {
    private const string NAME = "player";
    private const string DESCRIPTION = "Player management.";

    public PlayerCommand(GameSession session) : base(NAME, DESCRIPTION) {
        AddCommand(new LevelCommand(session));
    }

    private class LevelCommand : Command {
        private readonly GameSession session;

        public LevelCommand(GameSession session) : base("level", "Set player level.") {
            this.session = session;

            var level = new Argument<short>("level", "Level of the player.");

            AddArgument(level);
            this.SetHandler<InvocationContext, short>(Handle, level);
        }

        private void Handle(InvocationContext ctx, short level) {
            try {
                if (level is < 1 or > Constant.characterMaxLevel) {
                    ctx.Console.Error.WriteLine($"Invalid level: {level}");
                    return;
                }

                session.Player.Value.Character.Level = level;
                session.Field?.Multicast(LevelUpPacket.LevelUp(session.Player));
                ctx.ExitCode = 0;
            } catch (SystemException ex) {
                ctx.Console.Error.WriteLine(ex.Message);
                ctx.ExitCode = 1;
            }
        }
    }
}
