using System.Collections.Generic;
using System.Numerics;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Server.Game.Manager.Field;

namespace Maple2.Server.Game.Model;

public class FieldNpc : IActor<Npc> {
    public FieldManager Field { get; }

    public int ObjectId { get; }
    public Npc Value { get; }

    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
    public Vector3 Velocity { get; set; }

    public NpcState StateData;
    public ActorState State => StateData.State;
    public ActorSubState SubState => StateData.SubState;

    public short SequenceId;
    public short SequenceCounter;

    public IReadOnlyDictionary<int, Buff> Buffs { get; }
    public Stats Stats { get; }
    public int TargetId = 0;

    public FieldNpc(FieldManager field, int objectId, Npc npc) {
        Field = field;
        ObjectId = objectId;
        Value = npc;

        StateData = new NpcState();
        Buffs = new Dictionary<int, Buff>();
        Stats = new Stats(JobCode.Newbie, npc.Metadata.Basic.Level);
        SequenceId = -1;
        SequenceCounter = 1;
    }

    public void Sync() {

    }
}
