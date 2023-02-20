namespace Maple2.Server.Game.Model.Action;

public class DelayAction : NpcAction {
    public DelayAction(FieldNpc npc, short sequenceId, float duration) : base(npc, sequenceId, duration) { }
}
