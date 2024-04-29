﻿using System;
using Maple2.Model.Metadata;

namespace Maple2.Server.Game.Model.Routine;

public class AnimateRoutine : NpcRoutine {
    private TimeSpan duration;
    private readonly bool complete = true;

    public AnimateRoutine(FieldNpc npc, AnimationSequence sequence, float duration = -1f) : base(npc, sequence.Id) {
        if (duration != -1f) {
            this.duration = TimeSpan.FromMilliseconds(duration);
            complete = false;
            return;
        }
        this.duration = TimeSpan.FromSeconds(sequence.Time);
    }

    public override Result Update(TimeSpan elapsed) {
        duration -= elapsed;
        if (duration.Ticks > 0) {
            return Result.InProgress;
        }

        if (complete) {
            OnCompleted();
        }

        return Result.Success;
    }
}
