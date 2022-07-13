using Maple2.Model.Metadata;

namespace Maple2.Server.Game.Model.Skill;

public class SkillAttack {
    public readonly IActor Caster;
    public readonly IActor Owner;

    private readonly SkillMetadataAttack attack;
    public SkillMetadataDamage Damage => attack.Damage;
    public SkillEffectMetadata[] Effects => attack.Skills;

    public SkillAttack(IActor caster, IActor owner, SkillMetadataAttack attack) {
        Caster = caster;
        Owner = owner;

        this.attack = attack;
    }
}
