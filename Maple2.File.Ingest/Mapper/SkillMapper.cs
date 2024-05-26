﻿using System.Diagnostics;
using Maple2.File.IO;
using Maple2.File.Parser;
using Maple2.File.Parser.Xml.Skill;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;

namespace Maple2.File.Ingest.Mapper;

public class SkillMapper : TypeMapper<StoredSkillMetadata> {
    private readonly SkillParser parser;

    public SkillMapper(M2dReader xmlReader) {
        parser = new SkillParser(xmlReader);
    }

    protected override IEnumerable<StoredSkillMetadata> Map() {
        foreach ((int id, string name, SkillData data) in parser.Parse()) {
            if (data.basic == null) continue; // Old_JobChange_01
            Debug.Assert(data.basic.kinds.groupIDs.Length <= 1);

            // Note: 90000775 has cubeMagicPathID="2147483647" which should be cubeMagicPathID="9000073111"
            Dictionary<short, SkillMetadataLevel> levels = data.level.ToDictionary(
                level => level.value,
                level => new SkillMetadataLevel(
                    Condition: level.beginCondition.Convert(),
                    Change: level.changeSkill?.Convert(),
                    Consume: new SkillMetadataConsume(
                        Meso: level.consume.money,
                        UseItem: level.consume.useItem,
                        HpRate: level.consume.hpRate,
                        Stat: level.consume.stat.ToDictionary()),
                    Detect: new SkillMetadataDetect(
                        IncludeCaster: level.detectProperty?.includeCaster == 1,
                        Distance: level.detectProperty?.distance ?? 0),
                    Recovery: new SkillMetadataRecovery(
                        SpValue: level.recoveryProperty.spValue,
                        SpRate: level.recoveryProperty.spRate),
                    Skills: level.conditionSkill.Select(skill => skill.Convert()).ToArray(),
                    Motions: level.motion.Select(motion => new SkillMetadataMotion(
                        new SkillMetadataMotionProperty(
                            SequenceName: motion.motionProperty.sequenceName,
                            SequenceSpeed: motion.motionProperty.sequenceSpeed
                        ),
                        Attacks: motion.attack.Select(attack => new SkillMetadataAttack(
                            Point: attack.point,
                            PointGroup: attack.pointGroupID,
                            TargetCount: attack.targetCount,
                            MagicPathId: attack.magicPathID,
                            CubeMagicPathId: attack.cubeMagicPathID == int.MaxValue ? 9000073111 : attack.cubeMagicPathID,
                            HitImmuneBreak: attack.hitImmuneBreak,
                            BrokenOffence: attack.brokenOffence,
                            CompulsionTypes: attack.compulsionType.Select(type => (CompulsionType) type).ToArray(),
                            Pet: attack.petTamingProperty != null ? new SkillMetadataPet(
                                TamingGroup: attack.petTamingProperty.tamingGroup,
                                TrapLevel: attack.petTamingProperty.trapLevel,
                                TamingPoint: attack.petTamingProperty.tamingPoint,
                                ForcedTaming: attack.petTamingProperty.forcedTaming
                            ) : null,
                            Range: Convert(attack.rangeProperty),
                            Arrow: new SkillMetadataArrow(
                                Overlap: attack.arrowProperty.overlap,
                                Explosion: attack.arrowProperty.explosion,
                                NonTarget: (SkillEntity) attack.arrowProperty.nonTarget,
                                BounceType: attack.arrowProperty.bounceType,
                                BounceCount: attack.arrowProperty.bounceCount,
                                BounceRadius: attack.arrowProperty.bounceRadius,
                                BounceOverlap: attack.arrowProperty.bounceType > 0 && attack.arrowProperty.bounceOverlap,
                                Collision: attack.arrowProperty.collision,
                                CollisionAdd: attack.arrowProperty.collisionAdd),
                            Damage: new SkillMetadataDamage(
                                Count: attack.damageProperty.count,
                                Rate: attack.damageProperty.rate,
                                HitSpeed: attack.damageProperty.hitSpeedRate,
                                HitDelay: attack.damageProperty.hitPauseTime,
                                IsConstDamage: attack.damageProperty.isConstDamageValue != 0,
                                Value: attack.damageProperty.value,
                                DamageByTargetMaxHp: attack.damageProperty.damageByTargetMaxHP,
                                SuperArmorBreak: attack.damageProperty.superArmorBreak,
                                Push: attack.damageProperty.push > 0 ? new SkillMetadataPush(
                                    Type: attack.damageProperty.push,
                                    EaseType: attack.damageProperty.pushEaseType,
                                    ApplyField: attack.damageProperty.pushApplyField,
                                    Distance: attack.damageProperty.pushdistance,
                                    UpDistance: attack.damageProperty.pushUpDistance,
                                    Down: attack.damageProperty.pushDown,
                                    Fall: attack.damageProperty.pushFall,
                                    Duration: attack.damageProperty.pushduration,
                                    Probability: attack.damageProperty.pushprob,
                                    Priority: attack.damageProperty.pushPriority,
                                    PriorityHitImmune: attack.damageProperty.pushPriorityHitImmune
                                ) : null
                            ),
                            Skills: attack.conditionSkill.Select(skill => skill.Convert()).ToArray()
                        )).ToArray()
                    )).ToArray()
                ));

            yield return new StoredSkillMetadata(
                Id: id,
                Name: name,
                Property: new SkillMetadataProperty(
                    Type: (SkillType) data.basic.kinds.type,
                    SubType: (SkillSubType) data.basic.kinds.subType,
                    RangeType: (RangeType) data.basic.kinds.rangeType,
                    AttackType: (AttackType) data.basic.ui.attackType,
                    Element: (Element) data.basic.kinds.element,
                    ContinueSkill: data.basic.kinds.continueSkill,
                    SpRecoverySkill: data.basic.kinds.spRecoverySkill,
                    ImmediateActive: data.basic.kinds.immediateActive,
                    UnrideOnHit: data.basic.kinds.unrideOnHit,
                    UnrideOnUse: data.basic.kinds.unrideOnUse,
                    ReleaseObjectWeapon: data.basic.kinds.releaseObjectWeapon,
                    SkillGroup: data.basic.kinds.groupIDs.FirstOrDefault(),
                    MaxLevel: levels.Keys.Max()),
                State: new SkillMetadataState(
                    SuperArmor: data.basic.stateAttr.superArmor),
                Levels: levels
            );
        }
    }

    private static SkillMetadataRange Convert(RegionSkill region) {
        return new SkillMetadataRange(
            Type: region.rangeType switch {
                "box" => SkillRegion.Box,
                "cylinder" => SkillRegion.Cylinder,
                "frustum" => SkillRegion.Frustum,
                "hole_cylinder" => SkillRegion.HoleCylinder,
                "1200" => SkillRegion.None, // skill/60/60012051.xml
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
