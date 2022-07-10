using System;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Trigger;
using Maple2.Trigger;

namespace Maple2.Server.Game.Model;

public class FieldTrigger : FieldEntity<TriggerModel> {
    private static readonly TriggerLoader TriggerLoader = new();

    private readonly TriggerContext context;

    private TriggerState state;
    private TriggerState? nextState;

    public FieldTrigger(FieldManager field, int objectId, TriggerModel value) : base(field, objectId, value) {
        if (!TriggerLoader.TryGetTrigger(Field.Metadata.XBlock, Value.Name, out Func<ITriggerContext, TriggerState>? func)) {
            throw new ArgumentException($"Invalid trigger for {Field.Metadata.XBlock}, {Value.Name}");
        }

        context = new TriggerContext(this);
        state = func(context);
    }

    public override void Sync() {
        if (Environment.TickCount < context.NextTick) return;
        context.NextTick = Environment.TickCount + Constant.OnEnterTriggerDefaultTick;

        if (nextState != null) {
            state?.OnExit();
            state = nextState;
            state.OnEnter();
            nextState = null;
        }

        nextState = state.Execute();
    }
}
