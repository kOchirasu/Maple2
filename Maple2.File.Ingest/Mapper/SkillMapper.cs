using System.Diagnostics;
using Maple2.File.IO;
using Maple2.File.Parser;
using Maple2.File.Parser.Xml.Common;
using Maple2.File.Parser.Xml.Skill;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;

namespace Maple2.File.Ingest.Mapper;

public class SkillMapper : TypeMapper<SkillMetadata> {
    private readonly SkillParser parser;

    public SkillMapper(M2dReader xmlReader) {
        parser = new SkillParser(xmlReader);
    }

    protected override IEnumerable<SkillMetadata> Map() {
        foreach ((int id, string name, SkillData data) in parser.Parse()) {
            if (data.basic == null) continue; // Old_JobChange_01
            Debug.Assert(data.basic.kinds.groupIDs.Length <= 1);

            // Note: 90000775 has cubeMagicPathID="2147483647" which should be cubeMagicPathID="9000073111"
            Dictionary<short, SkillMetadataLevel> levels = data.level.ToDictionary(
                level => level.value,
                level => new SkillMetadataLevel(
                    Consume: new SkillMetadataConsume(
                        Meso: level.consume.money,
                        UseItem: level.consume.useItem,
                        HpRate: level.consume.hpRate,
                        Stat: level.consume.stat.ToDictionary()),
                    Recovery: new SkillMetadataRecovery(
                        SpValue: level.recoveryProperty.spValue,
                        SpRate: level.recoveryProperty.spRate),
                    Skills: level.conditionSkill.Select(Convert).ToList(),
                    Motions: level.motion.Select(motion => new SkillMetadataMotion(
                        Attacks: motion.attack.Select(attack => new SkillMetadataAttack(
                            Point: attack.point,
                            PointGroup: attack.pointGroupID,
                            TargetCount: attack.targetCount,
                            MagicPathId: attack.magicPathID,
                            CubeMagicPathId: attack.cubeMagicPathID,
                            Pet: attack.petTamingProperty != null ? new SkillMetadataPet(
                                TamingGroup: attack.petTamingProperty.tamingGroup,
                                TrapLevel: attack.petTamingProperty.trapLevel,
                                TamingPoint: attack.petTamingProperty.tamingPoint,
                                ForcedTaming: attack.petTamingProperty.forcedTaming
                            ) : null,
                            Range: Convert(attack.rangeProperty),
                            Damage: new SkillMetadataDamage(
                                Count: attack.damageProperty.count,
                                Rate: attack.damageProperty.rate,
                                HitSpeed: attack.damageProperty.hitSpeedRate,
                                HitDelay: attack.damageProperty.hitPauseTime,
                                IsConstDamage: attack.damageProperty.isConstDamageValue != 0,
                                Value: attack.damageProperty.value,
                                DamageByTargetMaxHp: attack.damageProperty.damageByTargetMaxHP
                            )
                        )).ToList()
                    )).ToList()
                ));

            yield return new SkillMetadata(
                Id: id,
                Name: name,
                Property: new SkillMetadataProperty(
                    Type: (SkillType) data.basic.kinds.type,
                    SubType: (SkillSubType) data.basic.kinds.subType,
                    RangeType: (RangeType) data.basic.kinds.rangeType,
                    Element: (Element) data.basic.kinds.element,
                    ContinueSkill: data.basic.kinds.continueSkill,
                    SpRecoverySkill: data.basic.kinds.spRecoverySkill,
                    ImmediateActive: data.basic.kinds.immediateActive,
                    UnrideOnHit: data.basic.kinds.unrideOnHit,
                    UnrideOnUse: data.basic.kinds.unrideOnUse,
                    ReleaseObjectWeapon: data.basic.kinds.releaseObjectWeapon,
                    SkillGroup: data.basic.kinds.groupIDs.FirstOrDefault()),
                State: new SkillMetadataState(),
                Levels: levels
            );
        }
    }

    private static SkillMetadataSkill Convert(TriggerSkill trigger) {
        return new SkillMetadataSkill(
            Splash: trigger.splash != 0,
            RandomCast: trigger.randomCast != 0,
            Skills: trigger.skillID.Zip(trigger.level, (skillId, level) =>
                new SkillMetadataSkill.Skill(skillId, (short) level)).ToArray(),
            Target: (SkillEntity) trigger.skillTarget,
            Owner: (SkillEntity) trigger.skillOwner,
            Delay: trigger.delay,
            RemoveDelay: trigger.removeDelay,
            Interval: trigger.interval,
            FireCount: trigger.fireCount,
            ImmediateActive: trigger.immediateActive != 0,
            NonTargetActive: trigger.nonTargetActive != 0
        );
    }

    private static SkillMetadataRegion Convert(RegionSkill region) {
        return new SkillMetadataRegion(
            Type: region.rangeType switch {
                "box" => SkillRegion.Box,
                "cylinder" => SkillRegion.Cylinder,
                "frustum" => SkillRegion.Frustum,
                "hole_cylinder" => SkillRegion.HoleCylinder,
                _ => SkillRegion.None,
            },
            Distance: region.distance,
            Height: region.height,
            Width: region.height,
            EndWidth: region.endWidth,
            RotateZDegree: region.rangeZRotateDegree,
            RangeAdd: region.rangeAdd,
            RangeOffset: region.rangeOffset,
            IncludeCaster: (SkillEntity) region.includeCaster,
            ApplyTarget: (SkillEntity) region.applyTarget,
            CastTarget: (SkillEntity) region.castTarget
        );
    }
}
