using System;
using System.Numerics;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Server.Game.Packets;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Trigger;

public partial class TriggerContext {
    public void ChangeBackground(string dds) {
        Field.SetBackground(dds);
    }

    public void EnableLocalCamera(bool enable) {
        Field.AddFieldProperty(new FieldPropertyLocalCamera{Enabled = enable});
    }

    public void LockMyPc(bool isLock) {
        if (isLock) {
            Field.AddFieldProperty(new FieldPropertyLockPlayer());
        } else {
            Field.RemoveFieldProperty(FieldProperty.LockPlayer);
        }
    }

    public void SetAmbientLight(Vector3 color) {
        Field.AddFieldProperty(new FieldPropertyAmbientLight{Color = color});
    }

    public void SetDirectionalLight(Vector3 diffuseColor, Vector3 specularColor) {
        Field.AddFieldProperty(new FieldPropertyDirectionalLight {
            DiffuseColor = diffuseColor,
            SpecularColor = specularColor,
        });
    }

    public void SetGravity(float gravity) {
        Field.AddFieldProperty(new FieldPropertyGravity(gravity));
    }

    public void SetPhotoStudio(bool enable) {
        Field.AddFieldProperty(new FieldPropertyPhotoStudio{Enabled = enable});
    }

    public void UserTagSymbol(string symbol1, string symbol2) {
        Broadcast(FieldPropertyPacket.Add(new FieldPropertyUserTagSymbol{Symbol1 = symbol1, Symbol2 = symbol2}));
    }

    public void VisibleMyPc(bool visible) {
        if (visible) {
            Field.RemoveFieldProperty(FieldProperty.HidePlayer);
        } else {
            Field.AddFieldProperty(new FieldPropertyHidePlayer());
        }
    }

    public void Weather(Maple2.Trigger.Enum.WeatherType weatherType) {
        Field.AddFieldProperty(new FieldPropertyWeather{WeatherType = (WeatherType) weatherType});
    }

    public void SightRange(bool enable, byte range, int rangeZ, byte border) {
        // range seems to be some ID? (3)
        if (enable) {
            Field.AddFieldProperty(new FieldPropertySightRange {
                Range = rangeZ,
                Opacity = border,
            });
        } else {
            Field.RemoveFieldProperty(FieldProperty.SightRange);
        }
    }

    public void SetPvpZone(int boxId, byte arg2, int duration, int additionalEffectId, byte arg5, int[]? boxIds) {
        logger.Debug("Unimplemented SetPvpZone({boxId}, {arg2}, {duration}, {additionalEffectId}, {arg5}, {boxIds})",
            boxId, arg2, duration, additionalEffectId, arg5, boxIds);
    }

    public void SetTimeScale(bool enable, float startScale, float endScale, float duration, byte interpolator) {
        // TODO: Does this need to be persisted on the field?
        Broadcast(FieldPropertyPacket.TimeScale(enable, startScale, endScale, duration, interpolator));
    }

    public void SetLocalCamera(int cameraId, bool enable) {
        Broadcast(CameraPacket.Local(cameraId, enable));
    }

    public void CameraReset(float interpolationTime) {
        Broadcast(CameraPacket.Interpolate(interpolationTime));
    }

    public void CameraSelect(int triggerId, bool enable) {
        if (!Objects.Cameras.TryGetValue(triggerId, out TriggerObjectCamera? camera)) {
            return;
        }
        if (camera.Visible == enable) {
            return;
        }

        camera.Visible = enable;
        Broadcast(TriggerPacket.Update(camera));
    }

    public void CameraSelectPath(int[] pathIds, bool returnView) {
        Broadcast(TriggerPacket.CameraStart(pathIds, returnView));
    }

    #region Entities
    public void SetActor(int triggerId, bool visible, string initialSequence, bool arg4, bool arg5) { }

    public void SetAgent(int[] triggerIds, bool visible) { }

    public void SetBreakable(int[] triggerIds, bool visible) { }

    public void SetCube(int[] triggerIds, bool visible, byte randomCount) {
        int count = triggerIds.Length;
        if (randomCount > 0 && randomCount < triggerIds.Length) {
            count = randomCount;
            Random.Shared.Shuffle(triggerIds);
        }

        for (int i = 0; i < count; i++) {
            if (!Objects.Cubes.TryGetValue(triggerIds[i], out TriggerObjectCube? cube)) {
                continue;
            }
            if (cube.Visible == visible) {
                continue;
            }

            cube.Visible = visible;
            Broadcast(TriggerPacket.Update(cube));
        }
    }

    public void SetEffect(int[] triggerIds, bool visible, int arg3, byte arg4) {
        foreach (int triggerId in triggerIds) {
            if (!Objects.Effects.TryGetValue(triggerId, out TriggerObjectEffect? effect)) {
                continue;
            }
            if (effect.Visible == visible) {
                continue;
            }

            effect.Visible = visible;
            Broadcast(TriggerPacket.Update(effect));
        }
    }

    public void SetInteractObject(int[] triggerIds, byte state, bool arg4, bool arg3) { }

    public void SetLadder(int triggerId, bool visible, bool animationEffect, int animationDelay) {
        if (!Objects.Ladders.TryGetValue(triggerId, out TriggerObjectLadder? ladder)) {
            return;
        }

        ladder.Visible = visible;
        ladder.Animate = animationEffect;
        ladder.Delay = animationDelay;
        Broadcast(TriggerPacket.Update(ladder));
    }

    public void SetMesh(int[] triggerIds, bool visible, int arg3, int delay, float scale) {
        UpdateMesh(triggerIds, visible, arg3, delay, scale);

    }

    // examples: arg3=200, arg4=3
    public void SetMeshAnimation(int[] triggerIds, bool visible, byte arg3, byte arg4) {
        foreach (int triggerId in triggerIds) {
            if (!Objects.Meshes.TryGetValue(triggerId, out TriggerObjectMesh? mesh)) {
                continue;
            }
            if (mesh.Visible == visible) {
                continue;
            }

            mesh.Visible = visible;
            Broadcast(TriggerPacket.Update(mesh));
        }
    }

    public void SetPortal(int triggerId, bool visible, bool enabled, bool minimapVisible, bool arg5) {

    }

    public void SetRandomMesh(int[] triggerIds, bool visible, int meshCount, int arg4, int delay) {
        int count = triggerIds.Length;
        if (meshCount > 0 && meshCount < triggerIds.Length) {
            count = meshCount;
            Random.Shared.Shuffle(triggerIds);
        }

        UpdateMesh(new ArraySegment<int>(triggerIds, 0, count), visible, arg4, delay);
    }

    private void UpdateMesh(ArraySegment<int> triggerIds, bool visible, int fade, int delay, float scale = 0) {
        foreach (int triggerId in triggerIds) {
            if (!Objects.Meshes.TryGetValue(triggerId, out TriggerObjectMesh? mesh)) {
                continue;
            }
            if (mesh.Visible == visible) {
                continue;
            }

            if (delay > 0) {
                Events.Schedule(() => UpdateSetMesh(mesh), delay);
            } else {
                UpdateSetMesh(mesh);
            }
        }

        void UpdateSetMesh(TriggerObjectMesh mesh) {
            mesh.Visible = visible;
            mesh.Fade = fade;
            if (scale != 0) {
                mesh.Scale = scale;
            }
            Broadcast(TriggerPacket.Update(mesh));
            // TODO: Should Fade be reset after sending packet?
        }
    }

    public void SetRope(int triggerId, bool visible, bool animationEffect, int animationDelay) {
        if (!Objects.Ropes.TryGetValue(triggerId, out TriggerObjectRope? rope)) {
            return;
        }

        rope.Visible = visible;
        rope.Animate = animationEffect;
        rope.Delay = animationDelay;
        Broadcast(TriggerPacket.Update(rope));
    }

    public void SetSkill(int[] triggerIds, bool enabled) { }

    public void SetSound(int triggerId, bool enabled) {
        if (!Objects.Sounds.TryGetValue(triggerId, out TriggerObjectSound? sound)) {
            return;
        }

        sound.Visible = enabled;
        Broadcast(TriggerPacket.Update(sound));
    }

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
