using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Manager.Config;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Model.Skill;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Server.Game.Util;
using Maple2.Tools;
using Maple2.Tools.Collision;
using Maple2.Tools.Extensions;
using Serilog;

namespace Maple2.Server.Game.Manager.Field;

public partial class FieldManager {
    // Actors
    internal readonly ConcurrentDictionary<int, FieldPlayer> Players = new();
    internal readonly ConcurrentDictionary<int, FieldNpc> Npcs = new();
    internal readonly ConcurrentDictionary<int, FieldNpc> Mobs = new();

    // Entities
    private readonly ConcurrentDictionary<string, FieldBreakable> fieldBreakables = new();
    private readonly ConcurrentDictionary<string, FieldLiftable> fieldLiftables = new();
    private readonly ConcurrentDictionary<int, FieldItem> fieldItems = new();
    private readonly ConcurrentDictionary<int, FieldMobSpawn> fieldMobSpawns = new();
    private readonly ConcurrentDictionary<int, FieldSkill> fieldSkills = new();

    // Objects
    private readonly ConcurrentDictionary<int, FieldObject<Portal>> fieldPortals = new();

    private string? background;
    private readonly ConcurrentDictionary<FieldProperty, IFieldProperty> fieldProperties = new();

    #region Spawn
    public FieldPlayer SpawnPlayer(GameSession session, Player player, int portalId = -1, in Vector3 position = default, in Vector3 rotation = default) {
        // TODO: Not sure what the difference is between instance ids.
        player.Character.MapId = MapId;
        player.Character.InstanceMapId = MapId;
        player.Character.InstanceId = InstanceId;

        var fieldPlayer = new FieldPlayer(session, player) {
            Position = position,
            Rotation = rotation,
        };

        // Use Portal if needed.
        if (fieldPlayer.Position == default && entities.Portals.TryGetValue(portalId, out Portal? portal)) {
            fieldPlayer.Position = portal.Position.Offset(portal.FrontOffset, portal.Rotation);
            fieldPlayer.Rotation = portal.Rotation;
        }

        // Use SpawnPoint if needed.
        if (fieldPlayer.Position == default) {
            SpawnPointPC? spawn = entities.PlayerSpawns.Values.FirstOrDefault(spawn => spawn.Enable);
            if (spawn != null) {
                fieldPlayer.Position = spawn.Position + new Vector3(0, 0, 25);
                fieldPlayer.Rotation = spawn.Rotation;
            }
        }

        if (Metadata.Property.RevivalReturnId != 0) {
            player.Character.ReviveMapId = Metadata.Property.RevivalReturnId;
        }
        if (Metadata.Property.EnterReturnId != 0) {
            player.Character.ReturnMapId = Metadata.Property.EnterReturnId;
        }

        return fieldPlayer;
    }

    public FieldNpc SpawnNpc(NpcMetadata npc, Vector3 position, Vector3 rotation, FieldMobSpawn? owner = null) {
        var fieldNpc = new FieldNpc(this, NextLocalId(), new Npc(npc)) {
            Owner = owner,
            Position = position,
            Rotation = rotation,
        };

        if (npc.Basic.Friendly > 0) {
            Npcs[fieldNpc.ObjectId] = fieldNpc;
        } else {
            Mobs[fieldNpc.ObjectId] = fieldNpc;
        }

        return fieldNpc;
    }

    public FieldObject<Portal> SpawnPortal(Portal portal, Vector3 position = default, Vector3 rotation = default) {
        var fieldPortal = new FieldObject<Portal>(NextLocalId(), portal) {
            Position = position != default ? position : portal.Position,
            Rotation = rotation != default ? rotation : portal.Rotation,
        };
        fieldPortals[fieldPortal.ObjectId] = fieldPortal;

        return fieldPortal;
    }

    public FieldItem SpawnItem(IActor owner, Item item) {
        var fieldItem = new FieldItem(this, NextLocalId(), item) {
            Owner = owner,
            Position = owner.Position,
            Rotation = owner.Rotation,
        };
        fieldItems[fieldItem.ObjectId] = fieldItem;

        return fieldItem;
    }

    public FieldBreakable? AddBreakable(string entityId, BreakableActor breakable) {
        if (fieldBreakables.ContainsKey(entityId)) {
            return null;
        }

        var fieldBreakable = new FieldBreakable(this, NextLocalId(), entityId, breakable) {
            Position = breakable.Position,
            Rotation = breakable.Rotation,
        };

        fieldBreakables.TryAdd(entityId, fieldBreakable);
        if (breakable.Id != 0) {
            triggerBreakable.TryAdd(breakable.Id, fieldBreakable);
        }
        return fieldBreakable;
    }

    public FieldLiftable? AddLiftable(string entityId, Liftable liftable) {
        if (fieldLiftables.ContainsKey(entityId)) {
            return null;
        }

        var fieldLiftable = new FieldLiftable(this, NextLocalId(), entityId, liftable) {
            Position = liftable.Position,
            Rotation = liftable.Rotation,
        };

        fieldLiftables[entityId] = fieldLiftable;
        return fieldLiftable;
    }

    public FieldMobSpawn? AddMobSpawn(MapMetadataSpawn metadata, Ms2RegionSpawn regionSpawn, ICollection<int> npcIds) {
        var spawnNpcs = new WeightedSet<NpcMetadata>();
        foreach (int npcId in npcIds) {
            if (!NpcMetadata.TryGet(npcId, out NpcMetadata? npc)) {
                continue;
            }
            if (npc.Basic.Difficulty < metadata.MinDifficulty || npc.Basic.Difficulty > metadata.MaxDifficulty) {
                continue;
            }

            int spawnWeight = Lua.CalcNpcSpawnWeight(npc.Basic.MainTags.Length, npc.Basic.SubTags.Length, npc.Basic.RareDegree, npc.Basic.Difficulty);
            spawnNpcs.Add(npc, spawnWeight);
        }

        if (spawnNpcs.Count <= 0) {
            logger.Warning("No valid Npcs found from: {NpcIds}", string.Join(",", npcIds));
            return null;
        }

        var fieldMobSpawn = new FieldMobSpawn(this, NextLocalId(), metadata, spawnNpcs) {
            Position = regionSpawn.Position,
            Rotation = regionSpawn.UseRotAsSpawnDir ? regionSpawn.Rotation : default,
        };

        fieldMobSpawns[fieldMobSpawn.ObjectId] = fieldMobSpawn;
        return fieldMobSpawn;
    }

    public void AddSkill(SkillMetadata metadata, int interval, in Vector3 position, in Vector3 rotation = default) {
        var fieldSkill = new FieldSkill(this, NextLocalId(), fieldActor, metadata, interval, position) {
            Position = position,
            Rotation = rotation,
        };

        fieldSkills[fieldSkill.ObjectId] = fieldSkill;
        Broadcast(RegionSkillPacket.Add(fieldSkill));
    }

    public void AddSkill(IActor caster, SkillEffectMetadata effect, Vector3[] points, in Vector3 position, in Vector3 rotation = default) {
        Debug.Assert(effect.Splash != null, "Cannot add non-splash skill to field");

        foreach (SkillEffectMetadata.Skill skill in effect.Skills) {
            if (!SkillMetadata.TryGet(skill.Id, skill.Level, out SkillMetadata? metadata)) {
                continue;
            }

            int fireCount = effect.FireCount > 0 ? effect.FireCount : -1;
            var fieldSkill = new FieldSkill(this, NextLocalId(), caster, metadata, fireCount, effect.Splash, points) {
                Position = position,
                Rotation = rotation,
            };

            fieldSkills[fieldSkill.ObjectId] = fieldSkill;
            Broadcast(RegionSkillPacket.Add(fieldSkill));
        }
    }

    public void AddSkill(SkillRecord record) {
        SkillMetadataAttack attack = record.Attack;
        if (!TableMetadata.MagicPathTable.Entries.TryGetValue(attack.CubeMagicPathId, out IReadOnlyList<MagicPath>? cubeMagicPaths)) {
            logger.Error("No CubeMagicPath found for {CubeMagicPath})", attack.CubeMagicPathId);
            return;
        }

        Vector3[] cubePoints;
        if (attack.CubeMagicPathId != 0) {
            // TODO: If this is a CubeMagicPath, we always align the height. Ideally we can detect the floor instead.
            record.Position = record.Position.AlignHeight();
            cubePoints = new Vector3[cubeMagicPaths.Count];
            for (int i = 0; i < cubeMagicPaths.Count; i++) {
                MagicPath magicPath = cubeMagicPaths[i];
                Vector3 rotation = default;
                if (magicPath.Rotate) {
                    rotation = record.Rotation;
                }

                Vector3 position = record.Position + magicPath.FireOffset.Rotate(rotation);
                cubePoints[i] = magicPath.IgnoreAdjust ? position : position.Align();
            }
        } else {
            cubePoints = new[] {record.Position};
        }

        // Condition-Skills are expected to be handled separately.
        foreach (SkillEffectMetadata effect in attack.Skills.Where(effect => effect.Splash != null)) {
            if (effect.Splash == null) {
                logger.Fatal("Invalid Splash-Skill being handled: {Effect}", effect);
                continue;
            }

            AddSkill(record.Caster, effect, cubePoints, record.Position, record.Rotation);
        }
    }

    public IEnumerable<IActor> GetTargets(Prism[] prisms, SkillEntity entity, int limit) {
        switch (entity) {
            case SkillEntity.Owner:
            case SkillEntity.Attacker:
            case SkillEntity.RegionBuff:
            case SkillEntity.RegionDebuff:
                return prisms.Filter(Players.Values, limit);
            case SkillEntity.Target:
                return prisms.Filter(Mobs.Values, limit);
            default:
                Log.Debug("Unhandled SkillEntity:{Entity}", entity);
                return Array.Empty<IActor>();
        }
    }

    public void RemoveSkill(int objectId) {
        if (fieldSkills.Remove(objectId, out _)) {
            Broadcast(RegionSkillPacket.Remove(objectId));
        }
    }
    #endregion

    public void AddFieldProperty(IFieldProperty fieldProperty) {
        fieldProperties[fieldProperty.Type] = fieldProperty;
        Broadcast(FieldPropertyPacket.Add(fieldProperty));
    }

    public void RemoveFieldProperty(FieldProperty fieldProperty) {
        fieldProperties.Remove(fieldProperty, out _);
        Broadcast(FieldPropertyPacket.Remove(fieldProperty));
    }

    public void SetBackground(string ddsPath) {
        background = ddsPath;
        Broadcast(FieldPropertyPacket.Background(background));
    }

    #region Player Managed
    // GuideObject is not added to the field, it will be managed by |GameSession.State|
    public FieldGuideObject SpawnGuideObject(IActor<Player> owner, IGuideObject guideObject) {
        var fieldGuideObject = new FieldGuideObject(this, NextLocalId(), guideObject) {
            CharacterId = owner.Value.Character.Id,
            Position = owner.Position,
            // rotation?
        };

        return fieldGuideObject;
    }

    public FieldInstrument SpawnInstrument(IActor<Player> owner, InstrumentMetadata instrument) {
        var fieldInstrument = new FieldInstrument(this, NextLocalId(), instrument) {
            OwnerId = owner.ObjectId,
            Position = owner.Position + new Vector3(0, 0, 1),
            Rotation = owner.Rotation,
        };

        return fieldInstrument;
    }
    #endregion

    #region Remove
    public bool RemovePlayer(int objectId, [NotNullWhen(true)] out FieldPlayer? fieldPlayer) {
        if (Players.TryRemove(objectId, out fieldPlayer)) {
            CommitPlot(fieldPlayer.Session);
            Broadcast(FieldPacket.RemovePlayer(objectId));
            Broadcast(ProxyObjectPacket.RemovePlayer(objectId));
            return true;
        }

        return false;
    }

    public bool PickupItem(FieldPlayer looter, int objectId, [NotNullWhen(true)] out Item? item) {
        if (!fieldItems.TryRemove(objectId, out FieldItem? fieldItem)) {
            item = null;
            return false;
        }

        item = fieldItem.Value;
        fieldItem.Pickup(looter);
        Broadcast(FieldPacket.RemoveItem(objectId));
        return true;
    }

    public bool RemoveLiftable(string entityId) {
        if (!fieldLiftables.TryRemove(entityId, out FieldLiftable? fieldLiftable)) {
            return false;
        }

        Broadcast(CubePacket.RemoveCube(fieldLiftable.ObjectId, fieldLiftable.Position));
        Broadcast(LiftablePacket.Remove(entityId));
        return true;
    }

    public bool RemoveNpc(int objectId) {
        if (!Mobs.TryRemove(objectId, out _) && !Npcs.TryRemove(objectId, out _)) {
            return false;
        }

        Broadcast(FieldPacket.RemoveNpc(objectId));
        Broadcast(ProxyObjectPacket.RemoveNpc(objectId));
        return true;
    }
    #endregion

    #region Events
    public void OnAddPlayer(FieldPlayer added) {
        Players[added.ObjectId] = added;
        // LOAD:
        added.Session.Send(LiftablePacket.Update(fieldLiftables.Values));
        added.Session.Send(BreakablePacket.Update(fieldBreakables.Values));
        // InteractObject
        foreach (FieldPlayer fieldPlayer in Players.Values) {
            added.Session.Send(FieldPacket.AddPlayer(fieldPlayer.Session));
        }
        Broadcast(FieldPacket.AddPlayer(added.Session), added.Session);
        Broadcast(ProxyObjectPacket.AddPlayer(added), added.Session);
        foreach (FieldItem fieldItem in fieldItems.Values) {
            added.Session.Send(FieldPacket.DropItem(fieldItem));
        }
        foreach (FieldNpc fieldNpc in Npcs.Values.Concat(Mobs.Values)) {
            added.Session.Send(FieldPacket.AddNpc(fieldNpc));
        }
        // FieldAddPet
        foreach (FieldObject<Portal> fieldPortal in fieldPortals.Values) {
            added.Session.Send(PortalPacket.Add(fieldPortal));
        }
        // ProxyGameObj
        foreach (FieldPlayer fieldPlayer in Players.Values) {
            added.Session.Send(ProxyObjectPacket.AddPlayer(fieldPlayer));
        }
        foreach (FieldNpc fieldNpc in Npcs.Values.Concat(Mobs.Values)) {
            added.Session.Send(ProxyObjectPacket.AddNpc(fieldNpc));
        }
        foreach (FieldSkill skillSource in fieldSkills.Values) {
            added.Session.Send(RegionSkillPacket.Add(skillSource));
        }

        added.Session.Send(TriggerPacket.Load(TriggerObjects));

        if (background != null) {
            added.Session.Send(FieldPropertyPacket.Background(background));
        }
        added.Session.Send(FieldPropertyPacket.Load(fieldProperties.Values));
    }
    #endregion Events
}
