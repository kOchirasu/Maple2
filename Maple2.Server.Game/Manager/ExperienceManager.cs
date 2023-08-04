using System;
using System.Collections.Generic;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Model;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Manager;

public sealed class ExperienceManager {
    private readonly GameSession session;
    private long Exp {
        get => session.Player.Value.Character.Exp;
        set => session.Player.Value.Character.Exp = value;
    }
    private long RestExp {
        get => session.Player.Value.Character.RestExp;
        set => session.Player.Value.Character.RestExp = value;
    }

    private short Level {
        get => session.Player.Value.Character.Level;
        set => session.Player.Value.Character.Level = value;
    }

    //public long PrestigeExp => session.Player.Value.Account.PrestigeExp;
    private int ChainKillCount { get; set; }

    private readonly Lua.Lua lua;
    public ExperienceManager(GameSession session, Lua.Lua lua) {
        this.session = session;
        this.lua = lua;
    }

    public void ResetChainKill() => ChainKillCount = 0;

    public void OnKill(IActor npc) {
        if (npc is not FieldNpc) {
            return;
        }
        FieldNpc fieldNpc = (npc as FieldNpc)!;
        // TODO: Check if there are level requirements for Chain Kill Count to count ?
        ChainKillCount++;
        float expRate = lua.CalcKillCountBonusExpRate(ChainKillCount);

        // TODO: Using table ID 2. Need confirmation if particular maps (or dungeons) use a different table
        long expGained = fieldNpc.Value.Metadata.Basic.CustomExp;
        if (fieldNpc.Value.Metadata.Basic.CustomExp < 0) {
            if (!session.TableMetadata.ExpBaseTable.Entries.TryGetValue(2, out IReadOnlyDictionary<int, long>? table) || !table.TryGetValue(fieldNpc.Value.Metadata.Basic.Level, out expGained)) {
                return;
            }
        }
        expGained += GetRestExp((long) (expGained * expRate));
        LevelUp();
        session.Send(ExperienceUpPacket.Add(expGained, Exp, RestExp, npc.ObjectId));
    }


    private long GetRestExp(long expGained) {
        long addedRestExp = (long) (expGained * ((float) Constant.RestExpAcquireRate / 10000)); // convert int to a percentage
        if (addedRestExp > RestExp) {
            addedRestExp = RestExp;
        }

        RestExp = Math.Max(0, RestExp - addedRestExp);
        Exp += expGained;
        return addedRestExp;
    }

    public void AddExp(long expGained, ExpMessageCode expMessageCode = ExpMessageCode.s_msg_take_exp) {
        expGained += GetRestExp(expGained);
        LevelUp();
        session.Send(ExperienceUpPacket.Add(expGained, Exp, RestExp, expMessageCode));
    }

    public bool LevelUp() {
        int startLevel = Level;
        for (int level = startLevel; level < Constant.characterMaxLevel; level++) {
            if (!session.TableMetadata.NextExpTable.Entries.TryGetValue(level, out long expToNextLevel) || expToNextLevel > Exp) {
                break;
            }

            Exp -= expToNextLevel;
            Level++;
        }
        if (Level > startLevel) {
            session.Field?.Broadcast(LevelUpPacket.LevelUp(session.Player));
        }
        return startLevel != Level;
    }

}
