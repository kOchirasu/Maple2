using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Util;
using Serilog;

namespace Maple2.Server.Game.Manager.Config;

public class BuffManager : IUpdatable {
    #region ObjectId
    private int idCounter;

    /// <summary>
    /// Generates an ObjectId unique to this specific actor instance.
    /// </summary>
    /// <returns>Returns a local ObjectId</returns>
    private int NextLocalId() => Interlocked.Increment(ref idCounter);
    #endregion
    private readonly IActor actor;
    // TODO: Change this to support multiple buffs of the same id, different casters. Possibly also different levels?
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
    }

    public void AddBuff(IActor caster, IActor owner, int id, short level, bool notifyField = true) {
        if (!owner.Field.SkillMetadata.TryGetEffect(id, level, out AdditionalEffectMetadata? additionalEffect)) {
            logger.Error("Invalid buff: {SkillId},{Level}", id, level);
            return;
        }

        // Check if immune to any present buffs
        if (!CheckImmunity(additionalEffect.Update)) {
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

        if (!additionalEffect.Condition.Check(caster, owner, actor)) {
            buff.Disable();
        }

        logger.Information("{Id} AddBuff to {ObjectId}: {SkillId},{Level} for {Tick}ms", buff.ObjectId, owner.ObjectId, id, level, buff.EndTick - buff.StartTick);
        // Logger.Information("> {Data}", additionalEffect.Property);
        if (notifyField) {
            owner.Field.Broadcast(BuffPacket.Add(buff));
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
        if (!Buffs.TryGetValue(id, out Buff? buff)) {
            return false;
        }

        //TODO: Check if buff is removable/should be removed
        Buffs.Remove(id, out _);
        actor.Field.Broadcast(BuffPacket.Remove(buff));
        return true;

    }

    public void RemoveAll() {
        Buffs.Clear();
        Initialize();
        LoadFieldBuffs();
    }

    public void OnDeath() {
        foreach (Buff buff in Buffs.Values) {
            if (!buff.Metadata.Property.KeepOnDeath) {
                Remove(buff.Id);
            }
        }
    }

    private bool CheckImmunity(AdditionalEffectMetadataUpdate metadataUpdate) {
        if (metadataUpdate.ImmuneIds.Any(buffId => Buffs.ContainsKey(buffId))) {
            return false;
        }

        if (metadataUpdate.ImmuneCategories.Any(category => Buffs.Values.Any(buff => buff.Metadata.Property.Category == category)))
            foreach (BuffCategory category in metadataUpdate.ImmuneCategories) {
                if (Buffs.Values.Any(buff => buff.Metadata.Property.Category == category)) {
                    return false;
                }
            }

        return true;
    }
}
