using System;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Trigger;
using Maple2.Trigger;

namespace Maple2.Server.Game.Model;

public class FieldTrigger : FieldEntity<TriggerModel> {
    private static readonly TriggerLoader TriggerLoader = new();

    private readonly TriggerContext context;

    private long nextTick;
    private TriggerState state;
    private TriggerState? nextState;

    public FieldTrigger(FieldManager field, int objectId, TriggerModel value) : base(field, objectId, value) {
        if (!TriggerLoader.TryGetTrigger(Field.Metadata.XBlock, Value.Name.ToLower(), out Func<ITriggerContext, TriggerState>? initialState)) {
            throw new ArgumentException($"Invalid trigger for {Field.Metadata.XBlock}, {Value.Name.ToLower()}");
        }

        context = new TriggerContext(this);
        state = initialState(context);
        nextTick = Environment.TickCount64;
    }

    public override void Sync() {
        context.Events.InvokeAll();

        if (Environment.TickCount64 < nextTick) {
            return;
        }

        nextTick += Constant.OnEnterTriggerDefaultTick;
        if (nextState != null) {
            state.OnExit();
            state = nextState;
            context.StartTick = Environment.TickCount64;
            state.OnEnter();
            nextState = null;
        }

        nextState = state.Execute();
    }
}
