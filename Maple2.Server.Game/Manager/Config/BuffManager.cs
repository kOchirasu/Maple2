using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Model.Skill;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Util;
using Serilog;

namespace Maple2.Server.Game.Manager.Config;

public class BuffManager : IUpdatable {
    #region ObjectId
    private int idCounter;

    /// <summary>
    /// Generates an ObjectId unique to this specific manager instance.
    /// </summary>
    /// <returns>Returns a local ObjectId</returns>
    private int NextLocalId() => Interlocked.Increment(ref idCounter);
    #endregion
    private readonly IActor actor;
    // TODO: Change this to support multiple buffs of the same id, different casters. Possibly also different levels?
    public ConcurrentDictionary<int, Buff> Buffs { get; } = new();
    public ConcurrentDictionary<InvokeEffectType, ConcurrentDictionary<int, InvokeRecord>> Invokes { get; } = new();
    public ConcurrentDictionary<CompulsionEventType, ConcurrentDictionary<int, AdditionalEffectMetadataStatus.CompulsionEvent>> Compulsions { get; } = new();
    private Dictionary<BasicAttribute, float> Resistances { get; } = new();
    public ReflectRecord? Reflect;
    private readonly ILogger logger = Log.ForContext<BuffManager>();

    public BuffManager(IActor actor) {
        this.actor = actor;
    }

    public void Initialize() {
        // Load buffs that are not broadcasted to the field
        if (actor is FieldPlayer player) {
            player.Session.Config.Skill.UpdatePassiveBuffs(false);
            foreach (Item item in player.Session.Item.Equips.Gear.Values) {
                foreach (ItemMetadataAdditionalEffect buff in item.Metadata.AdditionalEffects) {
                    AddBuff(actor, actor, buff.Id, buff.Level, false);
                }

                // Check for buffs on gemstones
                if (item.Socket?.Sockets is not null && item.Socket.Sockets.Length > 0) {
                    foreach (ItemGemstone? gemstone in item.Socket?.Sockets!) {
                        if (gemstone == null || !player.Session.ItemMetadata.TryGet(gemstone.ItemId, out ItemMetadata? metadata)) {
                            continue;
                        }

                        foreach (ItemMetadataAdditionalEffect buff in metadata.AdditionalEffects) {
                            AddBuff(actor, actor, buff.Id, buff.Level, false);
                        }
                    }
                }
            }

            foreach (Item item in player.Session.Item.Equips.Badge.Values) {
                foreach (ItemMetadataAdditionalEffect buff in item.Metadata.AdditionalEffects) {
                    AddBuff(actor, actor, buff.Id, buff.Level, false);
                }
            }
        }
    }

    public void LoadFieldBuffs() {
        // Lapenshards
        // Game Events
        // Prestige
        AddMapBuffs();
        if (actor is FieldPlayer player) {
            player.Session.Config.RefreshPremiumClubBuffs();
        }
    }

    public void AddBuff(IActor caster, IActor owner, int id, short level, bool notifyField = true) {
        if (!owner.Field.SkillMetadata.TryGetEffect(id, level, out AdditionalEffectMetadata? additionalEffect)) {
            logger.Error("Invalid buff: {SkillId},{Level}", id, level);
            return;
        }

        // Check if immune to any present buffs
        if (!CheckImmunity(id, additionalEffect.Property.Category)) {
            return;
        }

        // TODO: Implement AdditionalEffectMetadata.CasterIndividualBuff.
        // If true, each caster will have their own buff added to the same Actor.
        if (Buffs.TryGetValue(id, out Buff? existing)) {
            if (level > existing.Level) {

            }
            if (!existing.Stack()) {
                return;
            }
            if (notifyField) {
                owner.Field.Broadcast(BuffPacket.Update(existing));
            }
            return;
        }

        // Remove existing buff if it's in the same group
        if (additionalEffect.Property.Group > 0) {
            existing = Buffs.Values.FirstOrDefault(buff => buff.Metadata.Property.Group == additionalEffect.Property.Group);
            if (existing != null) {
                existing.Disable();
                owner.Field.Broadcast(BuffPacket.Remove(existing));
            }
        }


        var buff = new Buff(owner.Field, additionalEffect, NextLocalId(), caster, owner);
        if (!Buffs.TryAdd(id, buff)) {
            Buffs[id].Stack();
            owner.Field.Broadcast(BuffPacket.Update(buff));
            return;
        }

        SetReflect(buff);
        SetInvoke(buff);
        SetCompulsionEvent(buff);
        SetShield(buff);
        SetUpdates(buff);

        // refresh stats if needed
        if (buff.Metadata.Status.Values.Any() || buff.Metadata.Status.Rates.Any() || buff.Metadata.Status.SpecialValues.Any() || buff.Metadata.Status.SpecialRates.Any()) {
            if (actor is FieldPlayer player) {
                player.Session.Stats.Refresh2();
            }
        }

        // Add resistances
        foreach ((BasicAttribute attribute, float value) in additionalEffect.Status.Resistances) {
            Resistances.TryGetValue(attribute, out float existingValue);
            Resistances[attribute] = existingValue + value;
        }

        if (!additionalEffect.Condition.Check(caster, owner, actor)) {
            buff.Disable();
        }

        logger.Information("{Id} AddBuff to {ObjectId}: {SkillId},{Level} for {Tick}ms", buff.ObjectId, owner.ObjectId, id, level, buff.EndTick - buff.StartTick);
        if (notifyField) {
            owner.Field.Broadcast(BuffPacket.Add(buff));
        }
    }

    private void SetReflect(Buff buff) {
        if (buff.Metadata.Reflect.EffectId == 0 || !actor.Field.SkillMetadata.TryGetEffect(buff.Metadata.Reflect.EffectId, buff.Metadata.Reflect.EffectLevel,
                out AdditionalEffectMetadata? _)) {
            return;
        }

        // Does this get overwritten if a new reflect is applied?
        var record = new ReflectRecord(buff.Id, buff.Metadata.Reflect);
        Reflect = record;
    }


    private void SetInvoke(Buff buff) {
        if (buff.Metadata.InvokeEffect == null) {
            return;
        }

        for (int i = 0; i < buff.Metadata.InvokeEffect.Types.Length; i++) {
            var record = new InvokeRecord(buff.Id, buff.Metadata.InvokeEffect) {
                Value = buff.Metadata.InvokeEffect.Values[i],
                Rate = buff.Metadata.InvokeEffect.Rates[i],
            };

            // if exists, replace the record to support differentiating buff levels.
            if (Invokes.TryGetValue(buff.Metadata.InvokeEffect.Types[i], out ConcurrentDictionary<int, InvokeRecord>? nestedInvokeDic)) {
                NestedDictionaryUtil.Remove(Invokes, buff.Id);

                if (!nestedInvokeDic.TryAdd(buff.Id, record)) {
                    logger.Error("Could not add invoke record from {Id} to {Object}", buff.Id, actor.ObjectId);
                }
                continue;
            }

            if (!Invokes.TryAdd(buff.Metadata.InvokeEffect.Types[i], new ConcurrentDictionary<int, InvokeRecord> {
                    [buff.Id] = record
                })) {
                logger.Error("Could not add invoke record {Type} to {Object}", buff.Metadata.InvokeEffect.Types[i], actor.ObjectId);
            }
        }
    }

    private void SetCompulsionEvent(Buff buff) {
        if (buff.Metadata.Status.Compulsion == null) {
            return;
        }

        CompulsionEventType eventType = buff.Metadata.Status.Compulsion.Type;
        if (Compulsions.TryGetValue(eventType, out ConcurrentDictionary<int, AdditionalEffectMetadataStatus.CompulsionEvent>? nestedCompulsionDic)) {
            NestedDictionaryUtil.Remove(Compulsions, buff.Id);

            if (!nestedCompulsionDic.TryAdd(buff.Id, buff.Metadata.Status.Compulsion)) {
                logger.Error("Could not add compulsion event from {Id} to {Object}", buff.Id, actor.ObjectId);
            }
            return;
        }

        if (!Compulsions.TryAdd(eventType, new ConcurrentDictionary<int, AdditionalEffectMetadataStatus.CompulsionEvent> {
                [buff.Id] = buff.Metadata.Status.Compulsion
            })) {
            logger.Error("Could not add compulsion event {Type} to {Object}", eventType, actor.ObjectId);
        }
    }

    public float TotalCompulsionRate(CompulsionEventType type, int skillId = 0) {
        if (!Compulsions.TryGetValue(type, out ConcurrentDictionary<int, AdditionalEffectMetadataStatus.CompulsionEvent>? nestedCompulsionDic)) return 0;
        return skillId == 0 ? nestedCompulsionDic.Values.Sum(compulsion => compulsion.Rate) :
            nestedCompulsionDic.Values.Where(compulsion => compulsion.SkillIds.Contains(skillId)).Sum(compulsion => compulsion.Rate);
    }

    public float GetResistance(BasicAttribute attribute) {
        if (Resistances.TryGetValue(attribute, out float value)) {
            return value;
        }
        return 0;
    }

    public (int, float) GetInvokeValues(InvokeEffectType invokeType, int skillId, params int[] skillGroup) {
        float value = 0;
        float rate = 0f;
        if (Invokes.TryGetValue(invokeType, out ConcurrentDictionary<int, InvokeRecord>? nestedInvokeDic)) {
            foreach (InvokeRecord invoke in nestedInvokeDic.Values.Where(record => record.Metadata.SkillId == skillId || skillGroup.Contains(record.Metadata.SkillGroupId))) {
                value += invoke.Value;
                rate += invoke.Rate;
            }
        }
        return ((int, float)) (value, rate);
    }

    private void SetShield(Buff buff) {
        if (buff.Metadata.Shield != null) {
            if (buff.Metadata.Shield.HpValue > 0) {
                buff.ShieldHealth = buff.Metadata.Shield.HpValue;
            } else if (buff.Metadata.Shield.HpByTargetMaxHp > 0f) {
                buff.ShieldHealth = (long) (actor.Stats[BasicAttribute.Health].Total * buff.Metadata.Shield.HpByTargetMaxHp);
            }
        }
    }

    private void SetUpdates(Buff buff) {
        if (buff.Metadata.Update.Cancel != null) {
            CancelBuffs(buff, buff.Metadata.Update.Cancel);
        }

        // Reset skill cooldowns
        if (buff.Metadata.Update.ResetCooldown.Length > 0 && actor is FieldPlayer player) {
            foreach (int skillId in buff.Metadata.Update.ResetCooldown) {
                player.Session.Config.SetSkillCooldown(skillId);
            }
        }
    }

    private void CancelBuffs(Buff buff, AdditionalEffectMetadataUpdate.CancelEffect cancel) {
        foreach (int cancelId in cancel.Ids) {
            if (Buffs.TryGetValue(cancelId, out Buff? cancelBuff)) {
                if (cancel.CheckSameCaster) {
                    if (cancelBuff.Caster != buff.Caster) {
                        continue;
                    }
                }
                Remove(cancelId);
            }
        }

        foreach (BuffCategory category in cancel.Categories) {
            foreach (Buff cancelCategoryBuff in Buffs.Values) {
                if (cancelCategoryBuff.Metadata.Property.Category == category) {
                    Remove(cancelCategoryBuff.Id);
                }
            }
        }
    }

    public virtual void Update(long tickCount) {
        foreach (Buff buff in Buffs.Values) {
            buff.Update(tickCount);
        }
    }

    public void LeaveField() {
        foreach (MapEntranceBuff buff in actor.Field.Metadata.EntranceBuffs) {
            Remove(buff.Id);
        }
        foreach (Buff buff in Buffs.Values) {
            if (buff.Metadata.Property.RemoveOnLeaveField) {
                Remove(buff.Id);
            }
        }
    }

    private void AddMapBuffs() {
        foreach (MapEntranceBuff buff in actor.Field.Metadata.EntranceBuffs) {
            AddBuff(actor, actor, buff.Id, buff.Level);
        }

        if (actor.Field.Metadata.Property.Type == MapType.Pvp) {
            foreach (Buff buff in Buffs.Values) {
                if (buff.Metadata.Property.RemoveOnPvpZone) {
                    Remove(buff.Id);
                }

                if (!buff.Metadata.Property.KeepOnEnterPvpZone) {
                    Remove(buff.Id);
                }
            }
        }

        if (actor.Field.Metadata.Property.Region == MapRegion.ShadowWorld) {
            AddBuff(actor, actor, Constant.shadowWorldBuffHpUp, 1);
            AddBuff(actor, actor, Constant.shadowWorldBuffMoveProtect, 1);
        }
    }

    public void AddItemBuffs(Item item) {
        foreach (ItemMetadataAdditionalEffect buff in item.Metadata.AdditionalEffects) {
            AddBuff(actor, actor, buff.Id, buff.Level);
        }
    }

    public void RemoveItemBuffs(Item item) {
        foreach (ItemMetadataAdditionalEffect buff in item.Metadata.AdditionalEffects) {
            Remove(buff.Id);
        }
    }

    public bool Remove(int id) {
        //TODO: Check if buff is removable/should be removed
        if (!Buffs.Remove(id, out Buff? buff)) {
            return false;
        }

        if (Reflect?.SourceBuffId == id) {
            Reflect = null;
        }

        foreach ((BasicAttribute attribute, float value) in buff.Metadata.Status.Resistances) {
            Resistances[attribute] = Math.Min(0, Resistances[attribute] - value);
        }

        foreach ((InvokeEffectType _, ConcurrentDictionary<int, InvokeRecord> invokeRecords) in Invokes) {
            if (invokeRecords.ContainsKey(id)) {
                invokeRecords.Remove(id, out _);
            }
        }

        foreach ((CompulsionEventType _, ConcurrentDictionary<int, AdditionalEffectMetadataStatus.CompulsionEvent> compulsionEvents) in Compulsions) {
            if (compulsionEvents.ContainsKey(id)) {
                compulsionEvents.Remove(id, out _);
            }
        }

        if (buff.Metadata.Status.Values.Any() || buff.Metadata.Status.Rates.Any() || buff.Metadata.Status.SpecialValues.Any() || buff.Metadata.Status.SpecialRates.Any()) {
            if (actor is FieldPlayer player) {
                player.Session.Stats.Refresh2();
            }
        }

        actor.Field.Broadcast(BuffPacket.Remove(buff));
        return true;
    }

    public void OnDeath() {
        foreach (Buff buff in Buffs.Values) {
            if (!buff.Metadata.Property.KeepOnDeath) {
                Remove(buff.Id);
            }
        }
    }

    private bool CheckImmunity(int newBuffId, BuffCategory category) {
        foreach ((int id, Buff buff) in Buffs) {
            if (buff.Metadata.Update.ImmuneIds.Contains(newBuffId) || buff.Metadata.Update.ImmuneCategories.Contains(category)) {
                return false;
            }
        }
        return true;
    }
}
