using System;
using System.Linq;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Game.Manager.Field;
using Maple2.Server.Game.Model;
using Maple2.Tools.Scheduler;
using Maple2.Trigger;
using Serilog;

namespace Maple2.Server.Game.Trigger;

public partial class TriggerContext : ITriggerContext {
    private readonly FieldTrigger owner;
    private readonly ILogger logger = Log.Logger.ForContext<TriggerContext>();

    private FieldManager Field => owner.Field;
    private TriggerCollection Objects => owner.Field.TriggerObjects;

    private float currentRandom = float.MaxValue;

    public readonly EventQueue Events;
    public long StartTick;

    public TriggerContext(FieldTrigger owner) {
        this.owner = owner;

        Events = new EventQueue();
        Events.Start();
        StartTick = Environment.TickCount64;
    }

    private void Broadcast(ByteWriter packet) => Field.Broadcast(packet);

    // Accessors
    public int GetShadowExpeditionPoints() {
        return 0;
    }

    public bool GetDungeonVariable(int id) {
        return false;
    }

    public float GetNpcDamageRate(int spawnPointId) {
        return 1.0f;
    }

    public float GetNpcHpRate(int spawnPointId) {
        return 1.0f;
    }

    public int GetDungeonId() {
        return 0;
    }

    public int GetDungeonLevel() {
        return 3;
    }

    public int GetDungeonMaxUserCount() {
        return 1;
    }

    public int GetDungeonRoundsRequired() {
        return int.MaxValue;
    }

    public int GetUserCount(int boxId, int userTagId) {
        if (!Objects.Boxes.TryGetValue(boxId, out TriggerBox? box)) {
            return 0;
        }

        if (userTagId > 0) {
            return Field.Players.Values.Count(player => player.TagId == userTagId && box.Contains(player.Position));
        }

        return Field.Players.Values.Count(player => box.Contains(player.Position));
    }

    public int GetNpcExtraData(int spawnPointId, string extraDataKey) {
        return 0;
    }

    public int GetDungeonPlayTime() {
        return 0;
    }

    // Scripts seem to just check if this is "Fail"
    public string GetDungeonState() {
        return "";
    }

    public int GetDungeonFirstUserMissionScore() {
        return 0;
    }

    public int GetScoreBoardScore() {
        return 0;
    }

    public int GetUserValue(string key) {
        return 0;
    }

    public void DebugString(string value, string feature) {
        logger.Debug("{Value} [{Feature}]", value, feature);
    }

    public void WriteLog(string logName, string @event, int triggerId, int arg4, string arg5) {
        logger.Information("{Log}: {Event}, {TriggerId}, {Arg4}, {Arg5}", logName, @event, triggerId, arg4, arg5);
    }

    #region Conditions
    public bool DayOfWeek(byte[] dayOfWeeks, string description) {
        byte day = (byte) (DateTime.UtcNow.DayOfWeek + 1);
        return dayOfWeeks.Any(dayOfWeek => day == dayOfWeek);
    }

    public bool RandomCondition(float rate, string description) {
        if (currentRandom >= 100f) {
            currentRandom = Random.Shared.NextSingle() * 100;
        }

        currentRandom -= rate;
        if (rate > 0) {
            return false;
        }

        // Reset |currentRandom|
        currentRandom = float.MaxValue;
        return true;
    }

    public bool WaitAndResetTick(int waitTick) {
        long tickNow = Environment.TickCount64;
        if (tickNow <= StartTick + waitTick) {
            return false;
        }

        StartTick = tickNow;
        return true;
    }

    public bool WaitTick(int waitTick) {
        return Environment.TickCount64 > StartTick + waitTick;
    }
    #endregion
}
