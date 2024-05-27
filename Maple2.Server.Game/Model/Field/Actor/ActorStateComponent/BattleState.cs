﻿using Maple2.Model.Metadata;
using Maple2.Model.Enum;
using static Maple2.Model.Metadata.AiMetadata;
using System.Numerics;

namespace Maple2.Server.Game.Model.Field.Actor.ActorStateComponent;

public class BattleState {
    private readonly FieldNpc actor;

    public NodeTargetType TargetType {
        get {
            if (targetNode is null) {
                return NodeTargetType.Near;
            }

            return targetNode.Type;
        }
    }
    public TargetNode? TargetNode {
        get => targetNode;
        set {
            targetNode = value;
            changedTargetType = false;
        }
    }
    public bool KeepBattle = false;
    public bool CanBattle = true;
    public bool InBattle { get => TargetId != 0 || KeepBattle; }
    public int TargetId { get; private set; }
    public IActor? Target { get; private set; }
    public IActor? GrabbedUser { get; set; }
    private long nextTargetSearchTick = 0;
    private bool changedTargetType = false;
    private TargetNode? targetNode = null;

    public BattleState(FieldNpc actor) {
        this.actor = actor;
    }

    private string GetTargetName() {
        if (Target is null) {
            return "null";
        }

        if (Target is FieldPlayer player) {
            return player.Value.Character.Name;
        }

        if (Target is FieldNpc npc) {
            return npc.Value.Metadata.Name ?? "npc";
        }

        return "unknown";
    }

    public void Update(long tickCount) {
        if (actor.IsDead) {
            TargetId = 0;
            Target = null;

            return;
        }

        if (TargetId == 0 && TargetNode is not null && TargetType == NodeTargetType.HasAdditional && TargetNode.NoChangeWhenNoTarget) {
            return;
        }

        if (TargetId != 0 && (!ShouldKeepTarget() || !CanBattle) && !KeepBattle) {
            actor.AppendDebugMessage($"Lost target '{GetTargetName()}'\n");

            TargetId = 0;
            Target = null;
        }

        if (CanBattle && TargetId == 0 && nextTargetSearchTick <= tickCount) {
            FindNewTarget();

            nextTargetSearchTick = tickCount + 500;

            if (TargetId != 0) {
                actor.AppendDebugMessage($"New target '{GetTargetName()}'\n");
            }
        }
    }

    public bool IsInRange(float distanceSquared, float verticalOffset, float radiusSquared, float heightUp, float heightDown) {
        return distanceSquared < radiusSquared && verticalOffset <= heightUp && verticalOffset >= heightDown;
    }

    public bool ShouldTargetActor(IActor target, float radiusSquared, float heightUp, float heightDown) {
        float dummyMatch = -1;

        return ShouldTargetActor(target, radiusSquared, heightUp, heightDown, ref dummyMatch, null);
    }

    public bool ShouldTargetActor(IActor target, float radiusSquared, float heightUp, float heightDown, ref float currentBestMatch, List<(float, IActor)>? targetCandidates) {
        Vector3 position = actor.Position;

        if (TargetNode is not null && TargetType == NodeTargetType.Near) {
            if (TargetNode.From == 0 && TargetNode.To == 0) {
                position = TargetNode.Center;
            }
        }

        float distanceSquared = (target.Position - position).LengthSquared();
        float verticalOffset = target.Position.Z - position.Z;

        if (!IsInRange(distanceSquared, verticalOffset, radiusSquared, heightUp, -heightDown)) {
            return false;
        }

        if (TargetNode is null) {
            return true;
        }

        bool canMatch;
        float from = TargetNode.From * TargetNode.From;
        float to = TargetNode.To * TargetNode.To;

        switch (TargetType) {
            case NodeTargetType.Rand:
            case NodeTargetType.RandAssociated:
                canMatch = false;
                if (TargetNode.From != 0 || TargetNode.To != 0) {
                    canMatch = distanceSquared >= from && distanceSquared <= to;
                }
                if (canMatch && targetCandidates is not null) {
                    targetCandidates.Add((distanceSquared, target));
                }
                return canMatch;

            case NodeTargetType.Near:
            case NodeTargetType.NearAssociated:
                if (currentBestMatch != -1 && distanceSquared >= currentBestMatch) {
                    return false;
                }
                canMatch = distanceSquared >= from && distanceSquared <= to;
                if (canMatch && currentBestMatch != -1) {
                    currentBestMatch = distanceSquared;
                }
                return canMatch;

            case NodeTargetType.Far:
                if (currentBestMatch != -1 && distanceSquared <= currentBestMatch) {
                    return false;
                }
                canMatch = distanceSquared >= from && distanceSquared <= to;
                if (canMatch && currentBestMatch != -1) {
                    currentBestMatch = distanceSquared;
                }
                return canMatch;

            case NodeTargetType.Mid:
                canMatch = distanceSquared >= from && distanceSquared <= to;
                if (canMatch && targetCandidates is not null) {
                    targetCandidates.Add((distanceSquared, target));
                }
                return canMatch;

            case NodeTargetType.HasAdditional:
                int friendlyType = GetTargetType();
                if (currentBestMatch != -1 && distanceSquared >= currentBestMatch) {
                    return false;
                }
                canMatch = false;
                if (TargetNode.Target != NodeAiTarget.DefaultTarget) {
                    canMatch = friendlyType switch {
                        0 => target is FieldPlayer,
                        1 => target is FieldNpc,
                        _ => false
                    };
                }
                canMatch |= target.Buffs.HasBuff(TargetNode.AdditionalId, TargetNode.AdditionalLevel, 0);
                if (canMatch && currentBestMatch != -1) {
                    currentBestMatch = distanceSquared;
                }
                return canMatch;

            case NodeTargetType.GrabbedUser:
                return target == GrabbedUser && distanceSquared >= from && distanceSquared <= to;

            default:
                break;
        }

        return true;
    }

    private bool ShouldKeepTarget() {
        if (changedTargetType) return true;
        if (TargetId == 0) return true;

        IActor? target = null;

        if (actor.Field.Players.TryGetValue(TargetId, out FieldPlayer? targetPlayer)) {
            target = targetPlayer;
        } else if (actor.Field.Npcs.TryGetValue(TargetId, out FieldNpc? targetNpc)) {
            target = targetNpc;
        }

        if (target is null) {
            return false;
        }

        float lastSightRadius = actor.Value.Metadata.Distance.CustomLastSightRadius;
        float lastSightHeightUp = actor.Value.Metadata.Distance.CustomLastSightHeightUp;
        float lastSightHeightDown = actor.Value.Metadata.Distance.CustomLastSightHeightDown;

        if (lastSightRadius == 0) {
            lastSightRadius = Constant.NpcLastSightRadius;
        }

        if (lastSightHeightUp == 0) {
            lastSightHeightUp = Constant.NpcLastSightHeightUp;
        }

        if (lastSightHeightDown == 0) {
            lastSightHeightDown = Constant.NpcLastSightHeightDown;
        }

        return ShouldTargetActor(target, lastSightRadius * lastSightRadius, lastSightHeightUp, lastSightHeightDown);
    }

    private int GetTargetType() {
        int friendlyType = actor.Value.Metadata.Basic.Friendly;

        if (friendlyType != 2 && TargetNode is not null && TargetType == NodeTargetType.HasAdditional) {
            friendlyType = TargetNode.Target switch {
                NodeAiTarget.Hostile => friendlyType == 0 ? 0 : 1, // enemies target players (0), friendlies target enemies (1)
                NodeAiTarget.Friendly => friendlyType == 1 ? 0 : 1, // enemies target enemies (1), friendlies target players (0)
                _ => friendlyType
            };
        }

        return friendlyType;
    }

    private void FindNewTarget() {
        int friendlyType = GetTargetType();
        float sightSquared = actor.Value.Metadata.Distance.Sight;
        float sightHeightUp = actor.Value.Metadata.Distance.SightHeightUp;
        float sightHeightDown = actor.Value.Metadata.Distance.SightHeightDown;

        sightSquared *= sightSquared;

        IActor? nextTarget = null;
        float nextTargetDistance = float.MaxValue;
        List<(float, IActor)>? candidates = null;

        if (TargetType == NodeTargetType.RandAssociated || TargetType == NodeTargetType.Mid) {
            candidates = new List<(float, IActor)>();
        }

        if (friendlyType == 0) {
            foreach (FieldPlayer player in actor.Field.Players.Values) {
                if (ShouldTargetActor(player, sightSquared, sightHeightUp, sightHeightDown, ref nextTargetDistance, candidates)) {
                    nextTarget = player;
                }
            }
        }

        if (friendlyType == 1) {
            foreach (FieldNpc npc in actor.Field.Npcs.Values) {
                if (npc.Value.Metadata.Basic.Friendly != 0) {
                    continue;
                }

                if (ShouldTargetActor(npc, sightSquared, sightHeightUp, sightHeightDown, ref nextTargetDistance, candidates)) {
                    nextTarget = npc;
                }
            }
        }

        if (TargetType == NodeTargetType.RandAssociated) {
            if (candidates!.Count == 0) {
                nextTarget = null;
            } else {
                nextTarget = candidates[Random.Shared.Next(0, candidates.Count)].Item2;
            }
        }

        if (TargetType == NodeTargetType.Mid) {
            if (candidates!.Count == 0) {
                nextTarget = null;
            } else {
                candidates.Sort((entry1, entry2) => entry1.Item1.CompareTo(entry2.Item1));

                nextTarget = candidates[candidates.Count / 2].Item2;
            }
        }

        TargetId = nextTarget?.ObjectId ?? 0;
        Target = nextTarget;

        return;
    }
}
