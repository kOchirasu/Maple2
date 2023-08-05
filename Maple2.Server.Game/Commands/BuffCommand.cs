using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using Maple2.Database.Storage;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Commands;

public class BuffCommand : Command {
    private const string NAME = "buff";
    private const string DESCRIPTION = "Add buff to player.";

    private const int MAX_RARITY = 6;
    private const int MAX_SOCKET = 5;

    private readonly GameSession session;
    private readonly SkillMetadataStorage skillStorage;

    public BuffCommand(GameSession session, SkillMetadataStorage skillStorage) : base(NAME, DESCRIPTION) {
        this.session = session;
        this.skillStorage = skillStorage;

        var id = new Argument<int>("id", "Id of buff to activate.");
        var level = new Option<short>(new[] {"--level", "-l"}, () => 1, "Buff level.");
        var stack = new Option<byte>(new[] {"--stack", "-s"}, () => 1, "Amount of stacks on the buff.");

        AddArgument(id);
        AddOption(level);
        AddOption(stack);
        this.SetHandler<InvocationContext, int, short, byte>(Handle, id, level, stack);
    }

    private void Handle(InvocationContext ctx, int buffId, short level, byte stack) {
        try {
            if (!skillStorage.TryGetEffect(buffId, level, out AdditionalEffectMetadata? _)) {
                ctx.Console.Error.WriteLine($"Invalid buff: {buffId}, level: {level}");
                return;
            }

            session.Player.Buffs.AddBuff(session.Player, session.Player, buffId, level);
            if (stack > 1) {
                session.Player.Buffs.Buffs[buffId].Stack(stack);
                session.Field?.Broadcast(BuffPacket.Update(session.Player.Buffs.Buffs[buffId]));
            }
            ctx.ExitCode = 0;
        } catch (SystemException ex) {
            ctx.Console.Error.WriteLine(ex.Message);
            ctx.ExitCode = 1;
        }
    }
}
