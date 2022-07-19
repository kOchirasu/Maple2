using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Maple2.Model;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Trigger;

public partial class TriggerContext {
    public void EnableSpawnPointPc(int spawnPointId, bool isEnable) { }

    public void GiveRewardContent(int rewardId) { }

    public void KickMusicAudience(int targetBoxId, int targetPortalId) {
        if (!Field.TryGetPortal(targetPortalId, out Portal? portal)) {
            return;
        }

        foreach (FieldPlayer player in PlayersInBox(targetBoxId)) {
            player.Session.Send(PortalPacket.MoveByPortal(player, portal));
        }
    }

    public void MoveRandomUser(int mapId, int portalId, int boxId, int count) {
        FieldPlayer[] players = PlayersInBox(boxId).ToArray();
        Random.Shared.Shuffle(players);

        for (int i = 0; i < count; i++) {
            FieldPlayer player = players[i];
            if (mapId == 0) {
                if (portalId == 0) {
                    player.Session.ReturnField();
                    return;
                }

                if (!Field.TryGetPortal(portalId, out Portal? portal)) {
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
        if (!Field.TryGetPortal(portalId, out Portal? portal)) {
            return;
        }

        foreach (FieldPlayer player in PlayersInBox(boxId)) {
            if (userTagId <= 0 || player.TagId == userTagId) {
                player.Session.Send(PortalPacket.MoveByPortal(player, portal));
            }
        }
    }

    public void MoveUser(int mapId, int portalId, int boxId) {
        if (mapId == 0) {
            if (portalId == 0) {
                foreach (FieldPlayer player in PlayersInBox(boxId)) {
                    player.Session.ReturnField();
                }
                return;
            }

            if (!Field.TryGetPortal(portalId, out Portal? portal)) {
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

    public void MoveUserPath(string path) { }

    public void MoveUserToBox(int boxId, int portalId) {
        if (!Field.TryGetPortal(portalId, out Portal? portal)) {
            return;
        }

        foreach (FieldPlayer player in PlayersNotInBox(boxId)) {
            player.Session.Send(PortalPacket.MoveByPortal(player, portal));
        }
    }

    public void MoveUserToPos(Vector3 position, Vector3 rotation) {
        foreach (FieldPlayer player in Field.Players.Values) {
            Broadcast(PortalPacket.MoveByPortal(player, position, rotation));
        }
    }

    public void PatrolConditionUser(string patrolName, byte patrolIndex, int additionalEffectId) { }

    public void SetAchievement(string type, string code, int triggerId) { }

    public void SetPcEmotionLoop(string sequenceName, float duration, bool arg3) {
        Broadcast(TriggerPacket.UiEmotionLoop(sequenceName, (int) duration, arg3));
    }

    public void SetPcEmotionSequence(string[] sequenceNames) {
        Broadcast(TriggerPacket.UiEmotionSequence(sequenceNames));
    }

    public void SetPcRotation(Vector3 rotation) {
        Broadcast(TriggerPacket.UiPlayerRotation(rotation));
    }

    public void SetQuestAccept(int questId) { }

    public void SetQuestComplete(int questId) { }

    public void TalkNpc(int spawnId) { }

    public void AddUserValue(string key, int value) { }

    public void SetUserValue(string key, int value, int triggerId) { }

    public void FaceEmotion(int spawnId, string emotionName) { }

    public void SetState(byte arg1, string[] arg2, bool arg3) { }

    #region Conditions
    public bool CheckAnyUserAdditionalEffect(int boxId, int additionalEffectId, short level) {
        return false;
    }

    public bool CheckSameUserTag(int boxId) {
        return false;
    }

    public bool QuestUserDetected(int[] boxIds, int[] questIds, byte[] questStates, byte jobCode) {
        return false;
    }

    public bool UserDetected(int[] boxIds, byte jobCode) {
        IEnumerable<TriggerBox> boxes = boxIds
            .Select(boxId => Objects.Boxes.GetValueOrDefault(boxId))
            .Where(box => box != null)!;

        if (jobCode != 0) {
            return Field.Players.Values
                .Any(player => player.Value.Character.Job.Code() == (JobCode) jobCode && boxes.Any(box => box.Contains(player.Position)));
        }

        return Field.Players.Values.Any(player => boxes.Any(box => box.Contains(player.Position)));
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
