using System.Numerics;
using Maple2.Trigger.Enum;

namespace Maple2.Server.Game.Trigger;

public partial class TriggerContext {
    public void WeddingBroken() { }

    public void WeddingMoveUser(WeddingEntryType type, int arg1, byte[] arg2, int arg3) { }

    public void WeddingMutualAgree(WeddingAgreeType type) { }

    public void WeddingMutualCancel(WeddingAgreeType type) { }

    public void WeddingSetUserEmotion(WeddingEntryType type, byte id) { }

    public void WeddingSetUserLookAt(WeddingEntryType type, WeddingEntryType lookAtType, bool immediate) { }

    public void WeddingSetUserRotation(WeddingEntryType type, Vector3 rotation, bool immediate) { }

    public void WeddingUserToPatrol(string patrolName, WeddingEntryType type, byte patrolIndex) { }

    public void WeddingVowComplete() { }

    #region Conditions
    public bool WeddingEntryInField(WeddingEntryType type) {
        return false;
    }

    public bool WeddingHallState(string hallState) {
        return false;
    }

    public bool? WeddingMutualAgreeResult(WeddingAgreeType type) {
        return false;
    }
    #endregion
}
