﻿using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PathEngine;
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
    internal readonly ConcurrentDictionary<int, FieldPet> Pets = new();

    // Entities
    private readonly ConcurrentDictionary<string, FieldBreakable> fieldBreakables = new();
    private readonly ConcurrentDictionary<string, FieldLiftable> fieldLiftables = new();
    private readonly ConcurrentDictionary<string, FieldInteract> fieldInteracts = new();
    private readonly ConcurrentDictionary<string, FieldInteract> fieldAdBalloons = new();
    private readonly ConcurrentDictionary<int, FieldItem> fieldItems = new();
    private readonly ConcurrentDictionary<int, FieldMobSpawn> fieldMobSpawns = new();
    private readonly ConcurrentDictionary<int, FieldSkill> fieldSkills = new();
    private readonly ConcurrentDictionary<int, FieldPortal> fieldPortals = new();

    private string? background;
    private readonly ConcurrentDictionary<FieldProperty, IFieldProperty> fieldProperties = new();

    public RoomTimer? RoomTimer { get; private set; }


    #region Spawn
    public FieldPlayer SpawnPlayer(GameSession session, Player player, int portalId = -1, in Vector3 position = default, in Vector3 rotation = default) {
        // TODO: Not sure what the difference is between instance ids.
        player.Character.MapId = MapId;
        player.Character.InstanceMapId = MapId;
        player.Character.InstanceId = InstanceId;

        var fieldPlayer = new FieldPlayer(session, player, NpcMetadata) {
            Position = position,
            Rotation = rotation,
        };

        // Use Portal if needed.
        if (fieldPlayer.Position == default && Entities.Portals.TryGetValue(portalId, out Portal? portal)) {
            fieldPlayer.Position = portal.Position.Offset(portal.FrontOffset, portal.Rotation);
            fieldPlayer.Rotation = portal.Rotation;
        }

        // Use SpawnPoint if needed.
        if (fieldPlayer.Position == default) {
            SpawnPointPC? spawn = Entities.PlayerSpawns.Values.FirstOrDefault(spawn => spawn.Enable);
            if (spawn != null) {
                fieldPlayer.Position = spawn.Position + new Vector3(0, 0, 25);
                fieldPlayer.Rotation = spawn.Rotation;
            }
        }

        if (Metadata.Property.RevivalReturnId != 0) {
            player.Character.ReviveMapId = Metadata.Property.RevivalReturnId;
        }

        if (Metadata.Property.EnterReturnId != 0 &&
            Metadata.Property.Type != MapType.Arcade &&
            Metadata.Property.Type != MapType.PocketRealm &&
            Metadata.Property.Type != MapType.Dungeon &&
            Metadata.Property.Type != MapType.Event &&
            Metadata.Property.Type != MapType.Event2) {
            player.Character.ReturnMapId = Metadata.Property.EnterReturnId;
        }

        return fieldPlayer;
    }

    public FieldNpc? SpawnNpc(NpcMetadata npc, Vector3 position, Vector3 rotation, FieldMobSpawn? owner = null, SpawnPointNPC? spawnPointNpc = null) {
        Agent? agent = Navigation.AddAgent(npc, position);

        AnimationMetadata? animation = NpcMetadata.GetAnimation(npc.Model);
        Vector3 spawnPosition = position;
        if (agent is not null) {
            spawnPosition = Navigation.FromPosition(agent.getPosition());
        }
        var fieldNpc = new FieldNpc(this, NextLocalId(), agent, new Npc(npc, animation), spawnPointNpc?.PatrolData) {
            Owner = owner,
            Position = spawnPosition,
            Rotation = rotation,
            Origin = owner?.Position ?? spawnPosition,
            SpawnPointId = owner?.Value.Id ?? NextGlobalId(),
        };

        if (npc.Basic.Friendly > 0) {
            Npcs[fieldNpc.ObjectId] = fieldNpc;
        } else {
            Mobs[fieldNpc.ObjectId] = fieldNpc;
        }

        return fieldNpc;
    }

    private FieldNpc? SpawnNpc(NpcMetadata npc, SpawnPointNPC spawnPointNpc) {
        return SpawnNpc(npc, spawnPointNpc.Position, spawnPointNpc.Rotation, null, spawnPointNpc);
    }

    public FieldPet? SpawnPet(Item pet, Vector3 position, Vector3 rotation, FieldMobSpawn? owner = null, FieldPlayer? player = null) {
        if (!NpcMetadata.TryGet(pet.Metadata.Property.PetId, out NpcMetadata? npc)) {
            return null;
        }

        Agent? agent = Navigation.AddAgent(npc, position);
        if (agent == null) {
            return null;
        }

        // We use GlobalId if there is an owner because players can move between maps.
        int objectId = player != null ? NextGlobalId() : NextLocalId();
        AnimationMetadata? animation = NpcMetadata.GetAnimation(npc.Model);
        Vector3 spawnPosition = Navigation.FromPosition(agent.getPosition());
        var fieldPet = new FieldPet(this, objectId, agent, new Npc(npc, animation), pet, player) {
            Owner = owner,
            Position = Navigation.FromPosition(agent.getPosition()),
            Rotation = rotation,
            Origin = owner?.Position ?? spawnPosition,
        };
        Pets[fieldPet.ObjectId] = fieldPet;

        return fieldPet;
    }

    public FieldPortal SpawnPortal(Portal portal, Vector3 position = default, Vector3 rotation = default) {
        var fieldPortal = new FieldPortal(this, NextLocalId(), portal) {
            Position = position != default ? position : portal.Position,
            Rotation = rotation != default ? rotation : portal.Rotation,
        };
        fieldPortals[fieldPortal.ObjectId] = fieldPortal;

        return fieldPortal;
    }

    public FieldPortal SpawnPortal(Portal portal, int instanceId, Vector3 position = default, Vector3 rotation = default) {
        var fieldPortal = new FieldPortal(this, NextLocalId(), portal) {
            Position = position != default ? position : portal.Position,
            Rotation = rotation != default ? rotation : portal.Rotation,
            InstanceId = instanceId,
        };
        fieldPortals[fieldPortal.ObjectId] = fieldPortal;

        return fieldPortal;
    }

    public FieldItem SpawnItem(IActor owner, Item item) {
        var fieldItem = new FieldItem(this, NextLocalId(), item) {
            Owner = owner,
            Position = owner.Position,
            Rotation = owner.Rotation,
            Type = DropType.Player,
        };
        fieldItems[fieldItem.ObjectId] = fieldItem;

        return fieldItem;
    }

    public FieldItem SpawnItem(Vector3 position, Vector3 rotation, Item item, long characterId = 0, bool fixedPosition = false) {
        var fieldItem = new FieldItem(this, NextLocalId(), item) {
            Position = position,
            Rotation = rotation,
            FixedPosition = fixedPosition,
            ReceiverId = characterId,
            Type = characterId > 0 ? DropType.Default : DropType.Player,
        };
        fieldItems[fieldItem.ObjectId] = fieldItem;

        return fieldItem;
    }

    public FieldItem SpawnItem(IFieldEntity owner, Vector3 position, Vector3 rotation, Item item, long characterId) {
        var fieldItem = new FieldItem(this, NextLocalId(), item) {
            Owner = owner,
            Position = position,
            Rotation = rotation,
            ReceiverId = characterId,
            Type = characterId > 0 ? DropType.Default : DropType.Player,
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

        if (!fieldLiftables.TryAdd(entityId, fieldLiftable)) {
            return null;
        }
        return fieldLiftable;
    }

    public FieldInteract? AddInteract(string entityId, InteractObject interact) {
        if (!TableMetadata.InteractObjectTable.Entries.TryGetValue(interact.InteractId, out InteractObjectMetadata? metadata)) {
            return null;
        }
        IInteractObject interactObject = interact switch {
            Ms2InteractMesh mesh => new InteractMeshObject(entityId, mesh),
            Ms2Telescope telescope => new InteractTelescopeObject(entityId, telescope),
            Ms2SimpleUiObject ui => new InteractUiObject(entityId, ui),
            Ms2InteractDisplay display when metadata.Type == InteractType.DisplayImage => new InteractDisplayImage(entityId, display),
            Ms2InteractDisplay poster when metadata.Type == InteractType.GuildPoster => new InteractGuildPosterObject(entityId, poster),
            Ms2InteractActor actor => new InteractGatheringObject(entityId, actor),
            _ => throw new ArgumentException($"Unsupported Type: {metadata.Type}"),
        };

        return AddInteract(interact, interactObject);
    }

    public FieldInteract? AddInteract(InteractObject interactData, IInteractObject interactObject, InteractObjectMetadata? metadata = null) {
        if (metadata == null && !TableMetadata.InteractObjectTable.Entries.TryGetValue(interactData.InteractId, out metadata)) {
            return null;
        }

        var fieldInteract = new FieldInteract(this, NextLocalId(), interactObject.EntityId, metadata, interactObject) {
            Position = interactData.Position,
            Rotation = interactData.Rotation,
        };

        //TODO: Add treasure chests
        switch (interactObject) {
            case InteractBillBoardObject billboard:
                fieldAdBalloons[billboard.EntityId] = fieldInteract;
                break;
            default:
                fieldInteracts[interactObject.EntityId] = fieldInteract;
                break;
        }

        return fieldInteract;
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

        var spawnPets = new WeightedSet<ItemMetadata>();
        foreach ((NpcMetadata npc, int weight) in spawnNpcs) {
            if (!metadata.PetIds.TryGetValue(npc.Id, out int petId)) {
                continue;
            }

            if (!ItemMetadata.TryGetPet(petId, out ItemMetadata? pet)) {
                continue;
            }

            spawnPets.Add(pet, weight);
        }

        if (spawnNpcs.Count <= 0) {
            logger.Warning("No valid Npcs found from: {NpcIds}", string.Join(",", npcIds));
            return null;
        }

        var fieldMobSpawn = new FieldMobSpawn(this, NextLocalId(), metadata, spawnNpcs, spawnPets) {
            Position = regionSpawn.Position,
            Rotation = regionSpawn.UseRotAsSpawnDir ? regionSpawn.Rotation : default,
        };

        fieldMobSpawns[fieldMobSpawn.ObjectId] = fieldMobSpawn;
        return fieldMobSpawn;
    }

    public void AddSkill(SkillMetadata metadata, int interval, in Vector3 position, in Vector3 rotation = default) {
        var fieldSkill = new FieldSkill(this, NextLocalId(), FieldActor, metadata, interval, position) {
            Position = position,
            Rotation = rotation,
        };

        fieldSkills[fieldSkill.ObjectId] = fieldSkill;
        Broadcast(RegionSkillPacket.Add(fieldSkill));
    }

    public void AddSkill(IActor caster, SkillEffectMetadata effect, Vector3[] points, in Vector3 rotation = default) {
        Debug.Assert(effect.Splash != null, "Cannot add non-splash skill to field");

        foreach (SkillEffectMetadata.Skill skill in effect.Skills) {
            if (!SkillMetadata.TryGet(skill.Id, skill.Level, out SkillMetadata? metadata)) {
                continue;
            }

            int fireCount = effect.FireCount > 0 ? effect.FireCount : -1;
            var fieldSkill = new FieldSkill(this, NextLocalId(), caster, metadata, fireCount, effect.Splash, points) {
                Position = points[0],
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
            cubePoints = [record.Position];
        }

        // Condition-Skills are expected to be handled separately.
        foreach (SkillEffectMetadata effect in attack.Skills.Where(effect => effect.Splash != null)) {
            if (effect.Splash == null) {
                logger.Fatal("Invalid Splash-Skill being handled: {Effect}", effect);
                continue;
            }

            AddSkill(record.Caster, effect, cubePoints, record.Rotation);
        }
    }

    public IEnumerable<IActor> GetTargets(Prism[] prisms, SkillEntity entity, int limit, ICollection<IActor>? ignore = null) {
        switch (entity) {
            case SkillEntity.Owner:
            case SkillEntity.Attacker:
            case SkillEntity.RegionBuff:
            case SkillEntity.RegionDebuff:
                return prisms.Filter(Players.Values, limit, ignore);
            case SkillEntity.Target:
                return prisms.Filter(Mobs.Values, limit, ignore);
            case SkillEntity.RegionPet:
                return prisms.Filter(Pets.Values.Where(pet => pet.OwnerId == 0), limit, ignore);
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

    private void SetBonusMapPortal(IList<MapMetadata> bonusMaps, Ms2RegionSpawn spawn) {
        // Spawn a hat within a random range of 5 min to 8 hours
        int delay = Random.Shared.Next(1, 97) * (int) TimeSpan.FromMinutes(5).TotalMilliseconds;
        MapMetadata bonusMapMetadata = bonusMaps[Random.Shared.Next(bonusMaps.Count)];
        FieldManager? bonusMap = FieldFactory.Create(bonusMapMetadata.Id);
        Console.WriteLine($"Creating bonus map {bonusMapMetadata.Id} at {spawn.Position} in {delay} ms.");
        if (bonusMap == null) {
            return;
        }
        bonusMap.SetRoomTimer(RoomTimerType.Clock, 90000);
        var portal = new Portal(NextLocalId(), bonusMapMetadata.Id, -1, PortalType.Event, PortalActionType.Interact, spawn.Position, spawn.Rotation,
            new Vector3(200, 200, 250), 0, true, false, true);
        FieldPortal fieldPortal = SpawnPortal(portal, bonusMap.InstanceId);
        fieldPortal.Model = Metadata.Property.Continent switch {
            Continent.VictoriaIsland => "Eff_event_portal_A01",
            Continent.KarkarIsland => "Eff_kr_sandswirl_01",
            Continent.ShadowWorld => "Eff_uw_potral_A01",
            Continent.Kritias => "Eff_ks_magichole_portal_A01",
            _ => "Eff_event_portal_A01",
        };
        fieldPortal.EndTick = (int) (Environment.TickCount64 + TimeSpan.FromSeconds(30).TotalMilliseconds);
        Broadcast(PortalPacket.Add(fieldPortal));
        Scheduler.Schedule(() => SetBonusMapPortal(bonusMaps, spawn), delay);
    }

    private void SetRoomTimer(RoomTimerType type, int duration) {
        RoomTimer = new RoomTimer(this, type, duration);
    }

    #region Player Managed
    // GuideObject is not added to the field, it will be managed by |GameSession.State|
    public FieldGuideObject SpawnGuideObject(IActor<Player> owner, IGuideObject guideObject, Vector3 position = default) {
        if (position == default) {
            position = owner.Position;
        }
        var fieldGuideObject = new FieldGuideObject(this, NextLocalId(), guideObject) {
            CharacterId = owner.Value.Character.Id,
            Position = position,
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

    public FieldPortal SpawnEventPortal(FieldPlayer player, int fieldId, int portalDurationTick, string password) {
        var portal = new Portal(NextLocalId(), fieldId, -1, PortalType.Event, PortalActionType.Interact, player.Position, player.Rotation, new Vector3(200, 200, 250), 0, true, false, true);
        FieldPortal fieldPortal = SpawnPortal(portal);
        fieldPortal.Model = "Eff_Com_Portal_E";
        fieldPortal.Password = password;
        fieldPortal.OwnerName = player.Value.Character.Name;
        fieldPortal.EndTick = (int) (Environment.TickCount64 + portalDurationTick);
        Broadcast(PortalPacket.Add(fieldPortal));
        return fieldPortal;
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

    public bool RemoveItem(int objectId) {
        if (!fieldItems.TryRemove(objectId, out _)) {
            return false;
        }

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

    public bool RemoveInteract(string entityId) {
        if (fieldInteracts.TryRemove(entityId, out FieldInteract? fieldInteract)) {
            return false;
        }

        Broadcast(InteractObjectPacket.Remove(entityId));
        return true;
    }

    public bool RemoveNpc(int objectId, int removeDelay = 0) {
        if (!Mobs.TryRemove(objectId, out FieldNpc? npc) && !Npcs.TryRemove(objectId, out npc)) {
            return false;
        }

        Scheduler.Schedule(() => {
            Broadcast(FieldPacket.RemoveNpc(objectId));
            Broadcast(ProxyObjectPacket.RemoveNpc(objectId));
            npc.Dispose();
        }, removeDelay);
        return true;
    }

    public bool RemovePet(int objectId, int removeDelay = 0) {
        if (!Pets.TryRemove(objectId, out FieldPet? pet)) {
            return false;
        }

        Scheduler.Schedule(() => {
            Broadcast(FieldPacket.RemovePet(objectId));
            Broadcast(ProxyObjectPacket.RemovePet(objectId));
            pet.Dispose();
        }, removeDelay);
        return true;
    }

    public bool RemovePortal(int objectId) {
        if (!fieldPortals.TryRemove(objectId, out FieldPortal? portal)) {
            return false;
        }

        Broadcast(PortalPacket.Remove(portal.Value.Id));
        return true;
    }
    #endregion

    #region Events
    public void OnAddPlayer(FieldPlayer added) {
        Players[added.ObjectId] = added;
        // LOAD:
        added.Session.Send(LiftablePacket.Update(fieldLiftables.Values));
        added.Session.Send(BreakablePacket.Update(fieldBreakables.Values));
        added.Session.Send(InteractObjectPacket.Load(fieldInteracts.Values));
        foreach (FieldInteract fieldInteract in fieldAdBalloons.Values) {
            added.Session.Send(InteractObjectPacket.Add(fieldInteract.Object));
        }
        foreach (FieldPlayer fieldPlayer in Players.Values) {
            added.Session.Send(FieldPacket.AddPlayer(fieldPlayer.Session));
            if (fieldPlayer.Session.GuideObject != null) {
                added.Session.Send(GuideObjectPacket.Create(fieldPlayer.Session.GuideObject));
            }
            switch (fieldPlayer.Session.HeldCube) {
                case PlotCube plotCube:
                    added.Session.Send(SetCraftModePacket.Plot(fieldPlayer.ObjectId, plotCube));
                    break;
                case LiftableCube liftableCube:
                    added.Session.Send(SetCraftModePacket.Liftable(fieldPlayer.ObjectId, liftableCube));
                    break;
            }
        }
        Broadcast(FieldPacket.AddPlayer(added.Session), added.Session);
        Broadcast(ProxyObjectPacket.AddPlayer(added), added.Session);
        foreach (FieldItem fieldItem in fieldItems.Values) {
            added.Session.Send(FieldPacket.DropItem(fieldItem));
        }
        foreach (FieldNpc fieldNpc in Npcs.Values.Concat(Mobs.Values)) {
            added.Session.Send(FieldPacket.AddNpc(fieldNpc));
        }
        foreach (FieldPet fieldPet in Pets.Values) {
            added.Session.Send(FieldPacket.AddPet(fieldPet));
        }
        foreach (FieldPortal fieldPortal in fieldPortals.Values) {
            added.Session.Send(PortalPacket.Add(fieldPortal));
        }
        // ProxyGameObj
        foreach (FieldPlayer fieldPlayer in Players.Values) {
            added.Session.Send(ProxyObjectPacket.AddPlayer(fieldPlayer));
        }
        foreach (FieldNpc fieldNpc in Npcs.Values.Concat(Mobs.Values)) {
            added.Session.Send(ProxyObjectPacket.AddNpc(fieldNpc));
        }
        foreach (FieldPet fieldPet in Pets.Values) {
            added.Session.Send(ProxyObjectPacket.AddPet(fieldPet));
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
