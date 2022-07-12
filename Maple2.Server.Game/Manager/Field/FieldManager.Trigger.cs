using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Model;
using Maple2.Tools;

namespace Maple2.Server.Game.Manager.Field;

public partial class FieldManager {
    public readonly TriggerCollection TriggerObjects;

    private readonly ConcurrentDictionary<string, FieldTrigger> fieldTriggers = new();
    public readonly Dictionary<string, TickTimer> Timers = new();

    public FieldTrigger? AddTrigger(TriggerModel trigger) {
        try {
            var fieldTrigger = new FieldTrigger(this, NextLocalId(), trigger) {
                Position = trigger.Position,
                Rotation = trigger.Rotation,
            };
            fieldTriggers[trigger.Name] = fieldTrigger;

            return fieldTrigger;
        } catch (ArgumentException ex) {
            logger.Warning("Invalid Trigger: {Exception}", ex.Message);
            return null;
        }
    }

    public bool TryGetTrigger(string name, [NotNullWhen(true)] out FieldTrigger? fieldTrigger) {
        return fieldTriggers.TryGetValue(name, out fieldTrigger);
    }
}
