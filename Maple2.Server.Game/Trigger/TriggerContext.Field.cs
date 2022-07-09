using System.Numerics;
using Maple2.Trigger.Enum;

namespace Maple2.Server.Game.Trigger;

public partial class TriggerContext {
    #region FieldProperty
    public void ChangeBackground(string dds) { }

    public void EnableLocalCamera(bool isEnable) { }

    public void LockMyPc(bool isLock) { }

    public void SetAmbientLight(Vector3 color, Vector3 arg2, Vector3 arg3) { }

    public void SetDirectionalLight(Vector3 diffuseColor, Vector3 specularColor) { }

    public void SetGravity(float gravity) { }

    public void SetPhotoStudio(bool isEnable) { }

    public void SetPvpZone(byte arg1, byte arg2, int arg3, int arg4, byte arg5, byte[] arg6) { }

    public void SightRange(bool enable, byte range, int rangeZ, byte border) { }

    public void UserTagSymbol(string symbol1, string symbol2) { }

    public void VisibleMyPc(bool isVisible) { }

    public void Weather(WeatherType weatherType) { }
    #endregion

    public void SetTimeScale(bool enable, float startScale, float endScale, float duration, byte interpolator) { }

    public void SetLocalCamera(int cameraId, bool enable) { }

    public void CameraReset(float interpolationTime) { }

    public void CameraSelect(int cameraId, bool enable) { }

    public void CameraSelectPath(int[] pathIds, bool returnView) { }

    #region Entities
    public void SetActor(int triggerId, bool visible, string stateName, bool arg4, bool arg5) { }

    public void SetAgent(int[] triggerIds, bool visible) { }

    public void SetBreakable(int[] triggerIds, bool visible) { }

    public void SetCube(int[] triggerIds, bool visible, byte randomCount) { }

    public void SetEffect(int[] triggerIds, bool visible, int arg3, byte arg4) { }

    public void SetInteractObject(int[] triggerIds, byte state, bool arg4, bool arg3) { }

    public void SetLadder(int triggerId, bool visible, bool animationEffect, byte animationDelay) { }

    public void SetMesh(int[] triggerIds, bool visible, int arg3, int delay, float arg5) { }

    public void SetMeshAnimation(int[] triggerIds, bool visible, byte arg3, byte arg4) { }

    public void SetPortal(int portalId, bool visible, bool enabled, bool minimapVisible, bool arg5) { }

    public void SetRandomMesh(int[] triggerIds, bool visible, byte meshCount, int arg4, int delay) { }

    public void SetRope(int triggerId, bool visible, bool animationEffect, byte animationDelay) { }

    public void SetSkill(int[] triggerIds, bool enabled) { }

    public void SetSound(int triggerId, bool enabled) { }

    public void SetVisibleBreakableObject(int[] triggerIds, bool visible) { }

    public void AddBuff(int[] boxIds, int skillId, byte skillLevel, bool arg4, bool arg5, string feature) { }

    public void RemoveBuff(int arg1, int arg2, bool arg3) { }

    public void CreateItem(int[] arg1, int arg2, int arg3, int arg5) { }

    public void SpawnItemRange(int[] rangeId, byte randomPickCount) { }

    public void StartCombineSpawn(int[] groupId, bool isStart) { }

    public void SetTimer(string id, int seconds, bool clearAtZero, bool display, int arg5, string arg6) { }

    public void ResetTimer(string id) { }

    public void RoomExpire() { }

    public void FieldWarEnd(bool isClear) { }
    #endregion

    #region Conditions
    public bool DetectLiftableObject(int[] triggerBoxIds, int itemId) {
        return false;
    }

    public bool ObjectInteracted(int[] arg1, byte arg2) {
        return false;
    }

    public bool PvpZoneEnded(byte arg1) {
        return false;
    }

    public bool TimeExpired(string timerId) {
        // Consider timer expired if non-existent
        return false;
    }
    #endregion
}
