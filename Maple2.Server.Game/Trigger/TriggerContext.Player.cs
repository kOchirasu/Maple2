﻿using System.Numerics;
using Maple2.Model;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;

namespace Maple2.Server.Game.Trigger;

public partial class TriggerContext {
    public void EnableSpawnPointPc(int spawnPointId, bool enabled) {
        ErrorLog("[EnableSpawnPointPc] spawnPointId:{Type}, enabled:{Enabled}", spawnPointId, enabled);
    }

    public void GiveExp(int boxId, float expRate, bool arg3) {
        ErrorLog("[GiveExp] boxId:{BoxId}, expRate:{ExpRate}", boxId, expRate);
    }

    public void GiveRewardContent(int rewardId) {
        ErrorLog("[GiveRewardContent] rewardId:{RewardId}", rewardId);
    }

    public void KickMusicAudience(int targetBoxId, int targetPortalId) {
        DebugLog("[KickMusicAudience] targetBoxId:{BoxId}, targetPortalId:{PortalId}", targetBoxId, targetPortalId);
        if (!Field.TryGetPortal(targetPortalId, out FieldPortal? portal)) {
            return;
        }

        foreach (FieldPlayer player in PlayersInBox(targetBoxId)) {
            player.Session.Send(PortalPacket.MoveByPortal(player, portal));
        }
    }

    public void MoveRandomUser(int mapId, int portalId, int boxId, int count) {
        DebugLog("[MoveRandomUser] mapId:{MapId}, portalId:{PortalId}, boxId:{BoxId}, count:{Count}", mapId, portalId, boxId, count);
        FieldPlayer[] players = PlayersInBox(boxId).ToArray();
        Random.Shared.Shuffle(players);

        for (int i = 0; i < count; i++) {
            FieldPlayer player = players[i];
            if (mapId == 0) {
                if (portalId == 0) {
                    player.Session.ReturnField();
                    return;
                }

                if (!Field.TryGetPortal(portalId, out FieldPortal? portal)) {
                    return;
                }
                player.Session.Send(PortalPacket.MoveByPortal(player, portal));
                return;
            }

            player.Session.Send(player.Session.PrepareField(mapId, portalId)
                ? FieldEnterPacket.Request(player)
                : FieldEnterPacket.Error(MigrationError.s_move_err_default));
        }
    }

    public void MoveToPortal(int userTagId, int portalId, int boxId) {
        DebugLog("[MoveToPortal] userTagId:{TagId}, portalId:{PortalId}, boxId:{BoxId}", userTagId, portalId, boxId);
        if (!Field.TryGetPortal(portalId, out FieldPortal? portal)) {
            return;
        }

        foreach (FieldPlayer player in PlayersInBox(boxId)) {
            if (userTagId <= 0 || player.TagId == userTagId) {
                player.Session.Send(PortalPacket.MoveByPortal(player, portal));
            }
        }
    }

    public void MoveUser(int mapId, int portalId, int boxId) {
        DebugLog("[MoveUser] mapId:{MapId}, portalId:{PortalId}, boxId:{BoxId}", mapId, portalId, boxId);
        if (mapId == 0) {
            foreach (FieldPlayer player in PlayersInBox(boxId)) {
                player.Session.ReturnField();
            }
            return;
        }

        if (mapId == Field.MapId) {
            if (!Field.TryGetPortal(portalId, out FieldPortal? portal)) {
                return;
            }
            foreach (FieldPlayer player in PlayersInBox(boxId)) {
                player.Session.Send(PortalPacket.MoveByPortal(player, portal));
            }
            return;
        }

        foreach (FieldPlayer player in PlayersInBox(boxId)) {
            player.Session.Send(player.Session.PrepareField(mapId, portalId)
                ? FieldEnterPacket.Request(player)
                : FieldEnterPacket.Error(MigrationError.s_move_err_default));
        }
    }

    public void MoveUserPath(string path) {
        ErrorLog("[MoveUserPath] path:{Path}", path);
    }

    public void MoveUserToBox(int boxId, int portalId) {
        DebugLog("[MoveUserToBox] boxId:{BoxId}, portalId:{PortalId}", boxId, portalId);
        if (!Field.TryGetPortal(portalId, out FieldPortal? portal)) {
            return;
        }

        foreach (FieldPlayer player in PlayersNotInBox(boxId)) {
            player.Session.Send(PortalPacket.MoveByPortal(player, portal));
        }
    }

    public void MoveUserToPos(Vector3 position, Vector3 rotation) {
        DebugLog("[MoveUserToPos] position:{Position}, rotation:{Rotation}", position, rotation);
        foreach (FieldPlayer player in Field.Players.Values) {
            Broadcast(PortalPacket.MoveByPortal(player, position, rotation));
        }
    }

    public void PatrolConditionUser(string patrolName, int patrolIndex, int additionalEffectId) {
        ErrorLog("[PatrolConditionUser] patrolName:{Name}, patrolIndex:{Index}, additionalEffectId:{EffectId}", patrolName, patrolIndex, additionalEffectId);
    }

    public void SetAchievement(int triggerId, string type, string code) {
        DebugLog("[SetAchievement] type:{Type}, code:{Code}, triggerId:{TriggerId}", type, code, triggerId);

        type = string.IsNullOrWhiteSpace(type) ? "trigger" : type;
        if (!Enum.TryParse<ConditionType>(type, out ConditionType conditionType)) {
            conditionType = ConditionType.unknown;
        }

        foreach (FieldPlayer player in PlayersInBox(triggerId)) {
            player.Session.ConditionUpdate(conditionType, codeString: code);
        }
    }

    public void SetPcEmotionLoop(string sequenceName, float duration, bool arg3) {
        DebugLog("[SetPcEmotionLoop] sequenceName:{SequenceName}, duration:{Duration}, arg3:{Arg3}", sequenceName, duration, arg3);
        Broadcast(TriggerPacket.UiEmotionLoop(sequenceName, (int) duration, arg3));
    }

    public void SetPcEmotionSequence(string[] sequenceNames) {
        DebugLog("[SetPcEmotionSequence] sequenceNames:{SequenceNames}", string.Join(", ", sequenceNames));
        Broadcast(TriggerPacket.UiEmotionSequence(sequenceNames));
    }

    public void SetPcRotation(Vector3 rotation) {
        DebugLog("[SetPcRotation] rotation:{Rotation}", rotation);
        Broadcast(TriggerPacket.UiPlayerRotation(rotation));
    }

    public void SetQuestAccept(int questId) {
        ErrorLog("[SetQuestAccept] questId:{QuestId}", questId);
    }

    public void SetQuestComplete(int questId) {
        ErrorLog("[SetQuestComplete] questId:{QuestId}", questId);
    }

    public void TalkNpc(int spawnId) {
        ErrorLog("[TalkNpc] spawnId:{SpawnId}", spawnId);
    }

    public void AddUserValue(string key, int value) {
        WarnLog("[AddUserValue] key:{Key}, value:{Value}", key, value);
        if (Field.UserValues.TryGetValue(key, out int userValue)) {
            Field.UserValues[key] = userValue + value;
        }
    }

    public void SetUserValue(int triggerId, string key, int value) {
        WarnLog("[SetUserValue] key:{Key}, value:{Value}, triggerId:{TriggerId}", key, value, triggerId);
        Field.UserValues[key] = value;
    }

    public void FaceEmotion(int spawnId, string emotionName) {
        DebugLog("[FaceEmotion] spawnId:{SpawnId}, emotionName:{Emotion}", spawnId, emotionName);
        if (spawnId == 0) {
            FieldPlayer? firstPlayer = Field.Players.Values.FirstOrDefault();
            if (firstPlayer == null) {
                return;
            }
            Field.Broadcast(TriggerPacket.UiFaceEmotion(firstPlayer.ObjectId, emotionName));
            return;
        }

        foreach (var npc in Field.Npcs.Values.Where(npc => npc.SpawnPointId == spawnId)) {
            Field.Broadcast(TriggerPacket.UiFaceEmotion(npc.ObjectId, emotionName));
        }
    }

    public void SetState(int triggerId, dynamic[] states, bool randomize) {
        ErrorLog("[SetState] triggerId:{TriggerId}, states:{States}, randomize:{Randomize}", triggerId, string.Join(", ", states), randomize);
    }

    #region Conditions
    public bool CheckAnyUserAdditionalEffect(int boxId, int additionalEffectId, int level) {
        DebugLog("[CheckAnyUserAdditionalEffect] boxId:{BoxId}, additionalEffectId:{EffectId}, level:{Level}", boxId, additionalEffectId, level);
        foreach (FieldPlayer player in PlayersInBox(boxId)) {
            if (player.Buffs.Buffs.TryGetValue(additionalEffectId, out Buff? buff) && buff.Level == level) {
                return true;
            }
        }

        return false;
    }

    public bool CheckSameUserTag(int boxId) {
        ErrorLog("[CheckSameUserTag] boxId:{BoxId}", boxId);
        return false;
    }

    public bool QuestUserDetected(int[] boxIds, int[] questIds, int[] questStates, int jobCode) {
        DebugLog("[QuestUserDetected] boxIds:{BoxIds}, questIds:{QuestIds}, questStates:{QuestStates}, jobCode:{JobCode}",
            string.Join(", ", boxIds), string.Join(", ", questIds), string.Join(", ", questStates), (JobCode) jobCode);

        foreach (FieldPlayer player in PlayersInBox(boxIds)) {
            foreach (int questId in questIds) {
                if (!player.Session.Quest.TryGetQuest(questId, out Quest? quest)) {
                    continue;
                }

                switch (questStates[0]) {
                    case 1: // Started
                        if (quest.State == QuestState.Started) {
                            return true;
                        }
                        break;
                    case 2: // Started and Can Complete
                        if (quest.State == QuestState.Started && player.Session.Quest.CanComplete(quest)) {
                            return true;
                        }
                        break;
                    case 3: // Completed
                        if (quest.State == QuestState.Completed) {
                            return true;
                        }
                        break;
                }
            }
        }

        return false;
    }

    public bool UserDetected(int[] boxIds, int jobCode) {
        DebugLog("[UserDetected] boxIds:{BoxIds}, jobCode:{JobCode}", string.Join(", ", boxIds), (JobCode) jobCode);
        IEnumerable<TriggerBox> boxes = boxIds
            .Select(boxId => Objects.Boxes.GetValueOrDefault(boxId))
            .Where(box => box != null)!;

        if (jobCode != 0) {
            return Field.Players.Values
                .Any(player => player.Value.Character.Job.Code() == (JobCode) jobCode && boxes.Any(box => box.Contains(player.Position)));
        }

        return Field.Players.Values.Any(player => boxes.Any(box => box.Contains(player.Position)));
    }

    public bool WaitSecondsUserValue(string key, string desc) {
        return false;
    }
    #endregion

    private IEnumerable<FieldPlayer> PlayersInBox(params int[] boxIds) {
        if (boxIds.Length == 0 || boxIds[0] == 0) {
            return Field.Players.Values;
        }

        IEnumerable<TriggerBox> boxes = boxIds
            .Select(boxId => Objects.Boxes.GetValueOrDefault(boxId))
            .Where(box => box != null)!;

        return Field.Players.Values.Where(player => boxes.Any(box => box.Contains(player.Position)));
    }

    private IEnumerable<FieldPlayer> PlayersNotInBox(int boxId) {
        if (!Objects.Boxes.TryGetValue(boxId, out TriggerBox? box)) {
            return Array.Empty<FieldPlayer>();
        }

        return Field.Players.Values.Where(player => !box.Contains(player.Position));
    }
}
