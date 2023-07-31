﻿using System.Collections.Concurrent;
using System.Threading;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Serilog;

namespace Maple2.Server.Game.Manager.Config;

public class BuffManager {
    #region ObjectId
    private int idCounter;

    /// <summary>
    /// Generates an ObjectId unique to this specific actor instance.
    /// </summary>
    /// <returns>Returns a local ObjectId</returns>
    private int NextLocalId() => Interlocked.Increment(ref idCounter);
    #endregion
    private readonly IActor actor;
    public ConcurrentDictionary<int, Buff> Buffs { get; } = new();
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
        }
    }

    public void LoadFieldBuffs() {
        // Lapenshards
        // Game Events
        AddMapBuffs();
    }

    public void AddBuff(IActor caster, IActor owner, int id, short level, bool notifyField = true) {
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

        if (!owner.Field.SkillMetadata.TryGetEffect(id, level, out AdditionalEffectMetadata? additionalEffect)) {
            logger.Error("Invalid buff: {SkillId},{Level}", id, level);
            return;
        }
        
        // if (!SkillUtils.CheckCondition(additionalEffect.Condition, caster, owner, this)) {
        //     Buff should still be added, just disabled
        //     return;
        // }

        var buff = new Buff(owner.Field, additionalEffect, NextLocalId(), caster, owner);
        if (!Buffs.TryAdd(id, buff)) {
            logger.Error("Buff already exists: {SkillId}", id);
            return;
        }

        logger.Information("{Id} AddBuff to {ObjectId}: {SkillId},{Level} for {Tick}ms", buff.ObjectId, owner.ObjectId, id, level, buff.EndTick - buff.StartTick);
        // Logger.Information("> {Data}", additionalEffect.Property);
        if (notifyField) {
            owner.Field.Broadcast(BuffPacket.Add(buff));
        }
    }

    public void Update(long tickCount) {
        foreach (Buff buff in Buffs.Values) {
            buff.Update(tickCount);
        }
    }

    public void RemoveMapBuffs() {
        foreach (MapEntranceBuff buff in actor.Field.Metadata.EntranceBuffs) {
            Remove(buff.Id);
        }
    }

    private void AddMapBuffs() {
        foreach (MapEntranceBuff buff in actor.Field.Metadata.EntranceBuffs) {
            AddBuff(actor, actor, buff.Id, buff.Level);
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
        if (!Buffs.TryGetValue(id, out Buff? buff)) {
            return false;
        }
        //TODO: Check if buff is removable/should be removed
        buff.Remove();
        return true;

    }

    public void RemoveAll() {
        foreach (Buff buff in Buffs.Values) {
            buff.Remove();
        }
    }
}
