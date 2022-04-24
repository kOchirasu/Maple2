using M2dXmlGenerator;
using Maple2.File.IO;
using Maple2.File.Parser;
using Maple2.File.Parser.Xml.Npc;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;

namespace Maple2.File.Ingest.Mapper; 

public class NpcMapper : TypeMapper<NpcMetadata> {
    private readonly NpcParser parser;

    public NpcMapper(M2dReader xmlReader) {
        parser = new NpcParser(xmlReader);
    }
    
    protected override IEnumerable<NpcMetadata> Map() {
        foreach ((int id, string name, NpcData data, List<EffectDummy> _) in parser.Parse()) {
            float probDiv = data.normal.prob.Sum();
            yield return new NpcMetadata(
                Id:id,
                Name:name,
                Model:data.model.kfm,
                Stat:new NpcMetadataStat(
                    Stats:MapStats(data.stat),
                    ScaleStatRate:new [] {
                        data.stat.scaleStatRate_1, 
                        data.stat.scaleStatRate_2, 
                        data.stat.scaleStatRate_3, 
                        data.stat.scaleStatRate_4,
                    },
                    ScaleBaseTap:new [] {
                        data.stat.scaleBaseTap_1, 
                        data.stat.scaleBaseTap_2, 
                        data.stat.scaleBaseTap_3, 
                        data.stat.scaleBaseTap_4,
                    },
                    ScaleBaseDef:new [] {
                        data.stat.scaleBaseDef_1, 
                        data.stat.scaleBaseDef_2, 
                        data.stat.scaleBaseDef_3, 
                        data.stat.scaleBaseDef_4,
                    },
                    ScaleBaseSpaRate:new [] {
                        data.stat.scaleBaseSpaRate_1, 
                        data.stat.scaleBaseSpaRate_2, 
                        data.stat.scaleBaseSpaRate_3, 
                        data.stat.scaleBaseSpaRate_4,
                    }
                ),
                new NpcMetadataBasic(
                    Friendly:data.basic.friendly,
                    AttackGroup:data.basic.npcAttackGroup,
                    DefenseGroup:data.basic.npcDefenseGroup,
                    Kind:data.basic.kind,
                    ShopId:data.basic.shopId,
                    HitImmune:data.basic.hitImmune,
                    AbnormalImmune:data.basic.abnormalImmune,
                    Level:data.basic.level,
                    Class:data.basic.@class,
                    RotationDisabled:data.basic.rotationDisabled,
                    MaxSpawnCount:data.basic.maxSpawnCount,
                    GroupSpawnCount:data.basic.groupSpawnCount,
                    RareDegree:data.basic.rareDegree,
                    Difficulty:data.basic.difficulty,
                    CustomExp:data.exp.customExp
                ),
                Action:new NpcMetadataAction(
                    RotateSpeed:data.speed.rotation,
                    WalkSpeed:data.speed.walk,
                    RunSpeed:data.speed.run,
                    Actions:data.normal.action.Zip(data.normal.prob, 
                        (action, prob) => new NpcAction(action, prob/probDiv)).ToArray(),
                    MoveArea:data.normal.movearea,
                    MaidExpired:data.normal.maidExpired
                ),
                Dead:new NpcMetadataDead(
                    Time:data.dead.time,
                    Revival:data.dead.revival,
                    Count:data.dead.count,
                    LifeTime:data.dead.lifeTime,
                    ExtendRoomTime:data.dead.extendRoomTime
                )
            );
        }
    }

    private static IDictionary<StatAttribute, long> MapStats(Stat stat) {
        var stats = new Dictionary<StatAttribute, long> {
            [StatAttribute.Strength] = stat.str,
            [StatAttribute.Dexterity] = stat.dex,
            [StatAttribute.Intelligence] = stat.@int,
            [StatAttribute.Luck] = stat.luk,
            [StatAttribute.Health] = stat.hp,
            [StatAttribute.HpRegen] = stat.hp_rgp,
            [StatAttribute.HpRegenInterval] = stat.hp_inv,
            [StatAttribute.Spirit] = stat.sp,
            [StatAttribute.SpRegen] = stat.sp_rgp,
            [StatAttribute.SpRegenInterval] = stat.sp_inv,
            [StatAttribute.Stamina] = stat.ep,
            [StatAttribute.StaminaRegen] = stat.ep_rgp,
            [StatAttribute.StaminaRegenInterval] = stat.ep_inv,
            [StatAttribute.AttackSpeed] = stat.asp,
            [StatAttribute.MovementSpeed] = stat.msp,
            [StatAttribute.Accuracy] = stat.atp,
            [StatAttribute.Evasion] = stat.evp,
            [StatAttribute.CriticalRate] = stat.cap,
            [StatAttribute.CriticalDamage] = stat.cad,
            [StatAttribute.CriticalEvasion] = stat.car,
            [StatAttribute.Defense] = stat.ndd,
            [StatAttribute.PerfectGuard] = stat.abp,
            [StatAttribute.JumpHeight] = stat.jmp,
            [StatAttribute.PhysicalAtk] = stat.pap,
            [StatAttribute.MagicalAtk] = stat.map,
            [StatAttribute.PhysicalRes] = stat.par,
            [StatAttribute.MagicalRes] = stat.mar,
            [StatAttribute.MinWeaponAtk] = stat.wapmin,
            [StatAttribute.MaxWeaponAtk] = stat.wapmax,
            [StatAttribute.Damage] = stat.dmg,
            [StatAttribute.Piercing] = stat.pen,
            [StatAttribute.MountSpeed] = stat.rmsp,
            [StatAttribute.BonusAtk] = stat.bap,
            [StatAttribute.PetBonusAtk] = stat.bap_pet
        };

        if (FeatureLocaleFilter.Features.Contains("HiddenStatAdd01")) {
            stats[StatAttribute.Health] += stat.hiddenhpadd;
            stats[StatAttribute.Defense] += stat.hiddennddadd;
        }
        if (FeatureLocaleFilter.Features.Contains("HiddenStatAdd02")) {
            stats[StatAttribute.PhysicalAtk] += stat.hiddenwapadd;
            stats[StatAttribute.MagicalAtk] += stat.hiddenwapadd;
        }
        if (FeatureLocaleFilter.Features.Contains("HiddenStatAdd03")) {
            stats[StatAttribute.Health] += stat.hiddenhpadd03;
            stats[StatAttribute.Defense] += stat.hiddennddadd03;
            stats[StatAttribute.PhysicalAtk] += stat.hiddenwapadd03;
            stats[StatAttribute.MagicalAtk] += stat.hiddenwapadd03;
        }
        if (FeatureLocaleFilter.Features.Contains("HiddenStatAdd04")) {
            stats[StatAttribute.Health] += stat.hiddenhpadd04;
            stats[StatAttribute.Defense] += stat.hiddennddadd04;
            stats[StatAttribute.PhysicalAtk] += stat.hiddenwapadd04;
            stats[StatAttribute.MagicalAtk] += stat.hiddenwapadd04;
        }

        return stats;
    } 
}
