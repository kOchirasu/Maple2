using System;
using System.Linq;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Model.Skill;
using Maple2.Server.Game.Util;
using Maple2.Tools.Collision;

namespace Maple2.Server.Game.Model;

public class FieldSkillSource : ActorBase<SkillMetadata> {
    public int Interval { get; init; } = 1000;

    private int nextTick;

    public FieldSkillSource(FieldManager field, int objectId, SkillMetadata value) : base(field, objectId, value) {
        nextTick = Environment.TickCount;
    }

    public override void Sync() {
        if (Environment.TickCount < nextTick) {
            return;
        }

        // TODO: These are buffs? seems irrelevant to FieldSkill?
        // foreach (SkillEffectMetadata skill in Value.Data.Skills) {
        //     if (skill.Condition != null) {
        //         ConditionSkill(Field.Players.Values, skill);
        //     } else if (skill.Splash != null) {
        //         SplashSkill(skill);
        //     }
        // }

        foreach (SkillMetadataMotion motion in Value.Data.Motions) {
            foreach (SkillMetadataAttack attack in motion.Attacks) {
                Prism prism = attack.Range.GetPrism(Position, Rotation.Z);
                FieldPlayer[] players = prism.Filter(Field.Players.Values, attack.TargetCount).ToArray();
                foreach (FieldPlayer player in players) {
                    var skillAttack = new SkillAttack(this, this, attack);
                    player.ApplyAttack(skillAttack);
                }
            }
        }

        nextTick = Environment.TickCount + Interval;
    }
}
