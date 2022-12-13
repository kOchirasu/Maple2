using Maple2.Model.Enum;

namespace Maple2.Server.Game.Scripting.Npc;

public class NpcScript {
    private readonly NpcScriptContext context;
    private readonly dynamic script;

    public NpcScript(NpcScriptContext context, dynamic script) {
        this.context = context;
        this.script = script;
    }

    public bool Begin() {
        if (context.TalkType.HasFlag(NpcTalkType.Select)) {
            return context.Respond(script.select());
        }

        return context.Respond(script.first());
    }

    public bool Continue(int pick) {
        int nextState = script.execute(context.State, context.Index, pick);
        if (nextState < 0) {
            nextState = context.NextState(pick);
        }

        return context.Continue(nextState);
    }
}
