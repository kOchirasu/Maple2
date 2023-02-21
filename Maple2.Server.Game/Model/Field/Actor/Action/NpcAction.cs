using System;
using Maple2.Model.Enum;
using Maple2.Server.Game.Model.State;

namespace Maple2.Server.Game.Model.Action;

public abstract class NpcAction {
    public bool Completed { get; protected set; }
    protected FieldNpc Npc;
    protected int EndTick;

    // Used for chaining predetermined actions.
    public Func<NpcAction>? NextAction { get; init; }

    protected NpcAction(FieldNpc npc, short sequenceId, float duration) {
        Npc = npc;
        EndTick = Environment.TickCount + (int) (duration * 1000);

        Npc.SequenceId = sequenceId;
    }

    /// <summary>
    /// Synchronizes an Npc action.
    /// </summary>
    /// <returns>bool representing whether this action is completed</returns>
    public virtual bool Sync() {
        if (Environment.TickCount < EndTick) {
            return false;
        }

        OnCompleted();
        return true;
    }

    public virtual void OnCompleted() {
        if (Completed) {
            return;
        }

        Completed = true;
        // Force Idle on completion
        Npc.SequenceId = Npc.IdleSequence.Id;
        if (Npc.StateData.State != ActorState.Idle) {
            Npc.StateData = new NpcState();
        }
    }
}
