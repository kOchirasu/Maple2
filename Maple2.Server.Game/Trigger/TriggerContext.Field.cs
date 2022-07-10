using System.Numerics;
using Maple2.Trigger.Enum;

namespace Maple2.Server.Game.Trigger;

public partial class TriggerContext {
    #region FieldProperty
    public void ChangeBackground(string dds) { }

    public void EnableLocalCamera(bool enable) { }

    public void LockMyPc(bool isLock) { }

    public void SetAmbientLight(Vector3 color) { }

    public void SetDirectionalLight(Vector3 diffuseColor, Vector3 specularColor) { }

    public void SetGravity(float gravity) { }

    public void SetPhotoStudio(bool enable) { }

    public void SetPvpZone(int boxId, byte arg2, int duration, int additionalEffectId, byte arg5, int[]? boxIds) { }

    public void SightRange(bool enable, byte range, int rangeZ, byte border) { }

    public void UserTagSymbol(string symbol1, string symbol2) { }

    public void VisibleMyPc(bool visible) { }

    public void Weather(WeatherType weatherType) { }
    #endregion

    public void SetTimeScale(bool enable, float startScale, float endScale, float duration, byte interpolator) { }

    public void SetLocalCamera(int cameraId, bool enable) { }

    public void CameraReset(float interpolationTime) { }

    public void CameraSelect(int triggerId, bool enable) { }

    public void CameraSelectPath(int[] pathIds, bool returnView) { }

    #region Entities
    public void SetActor(int triggerId, bool visible, string initialSequence, bool arg4, bool arg5) { }

    public void SetAgent(int[] triggerIds, bool visible) { }

    public void SetBreakable(int[] triggerIds, bool visible) { }

    public void SetCube(int[] triggerIds, bool visible, byte randomCount) { }

    public void SetEffect(int[] triggerIds, bool visible, int arg3, byte arg4) { }

    public void SetInteractObject(int[] triggerIds, byte state, bool arg4, bool arg3) { }

    public void SetLadder(int triggerId, bool visible, bool animationEffect, int animationDelay) { }

    public void SetMesh(int[] triggerIds, bool visible, int arg3, int delay, float arg5) { }

    public void SetMeshAnimation(int[] triggerIds, bool visible, byte arg3, byte arg4) { }

    public void SetPortal(int portalId, bool visible, bool enabled, bool minimapVisible, bool arg5) { }

    public void SetRandomMesh(int[] triggerIds, bool visible, int meshCount, int arg4, int delay) { }

    public void SetRope(int triggerId, bool visible, bool animationEffect, int animationDelay) { }

    public void SetSkill(int[] triggerIds, bool enabled) { }

    public void SetSound(int triggerId, bool enabled) { }

    public void SetVisibleBreakableObject(int[] triggerIds, bool visible) { }

    public void AddBuff(int[] boxIds, int skillId, short level, bool arg4, bool arg5, string feature) { }

    public void RemoveBuff(int boxId, int skillId, bool arg3) { }

    public void CreateItem(int[] spawnIds, int triggerId, int itemId, int arg5) { }

    public void SpawnItemRange(int[] rangeId, int randomPickCount) { }

    public void StartCombineSpawn(int[] groupId, bool isStart) { }

    public void SetTimer(string timerId, int seconds, bool clearAtZero, bool display, int arg5, string arg6) { }

    public void ResetTimer(string timerId) { }

    public void RoomExpire() { }

    public void FieldWarEnd(bool isClear) { }
    #endregion

    #region Conditions
    public bool DetectLiftableObject(int[] boxIds, int itemId) {
        return false;
    }

    public bool ObjectInteracted(int[] interactIds, byte arg2) {
        return false;
    }

    public bool PvpZoneEnded(int boxId) {
        return false;
    }

    public bool TimeExpired(string timerId) {
        // Consider timer expired if non-existent
        return false;
    }
    #endregion
}
