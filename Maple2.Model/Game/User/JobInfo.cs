using System.Collections.Generic;
using System.Linq;
using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Model.Game;

public class JobInfo : IByteSerializable {
    private const int SKILL_TYPES = 4;
    private const int SKILL_RANKS = 2;

    private readonly Job job;
    private readonly Dictionary<int, Skill>[,] skills;

    public JobInfo(Job job) {
        this.job = job;
        skills = new Dictionary<int, Skill>[SKILL_TYPES,SKILL_RANKS];
        for (int i = 0; i < SKILL_TYPES; i++) {
            for (int j = 0; j < SKILL_RANKS; j++) {
                skills[i, j] = new Dictionary<int, Skill>();
            }
        }
    }

    public void AddSkill(SkillType type, SkillRank rank, Skill skill) {
        if (rank is not (SkillRank.Basic or SkillRank.Both)) {
            return;
        }

        skills[(int) type, (int) rank].Add(skill.Id, skill);
    }

    public IEnumerable<Skill> GetSkills(SkillType type, SkillRank rank) {
        if (rank is SkillRank.Basic or SkillRank.Both) {
            foreach (Skill skill in skills[(int) type, (int) SkillRank.Basic].Values) {
                yield return skill;
            }
        }

        if (rank is SkillRank.Awakening or SkillRank.Both) {
            foreach (Skill skill in skills[(int) type, (int) SkillRank.Awakening].Values) {
                yield return skill;
            }
        }
    }

    public Skill? GetSkill(int skillId, SkillRank rank = SkillRank.Both) {
        for (int i = 0; i < SKILL_TYPES; i++) {
            if (rank is SkillRank.Basic or SkillRank.Both) {
                if (skills[i, (int) SkillRank.Basic].TryGetValue(skillId, out Skill? skill)) {
                    return skill;
                }
            }

            if (rank is SkillRank.Awakening or SkillRank.Both) {
                if (skills[i, (int) SkillRank.Awakening].TryGetValue(skillId, out Skill? skill)) {
                    return skill;
                }
            }
        }

        return null;
    }

    public void WriteTo(IByteWriter writer) {
        writer.WriteInt((int) job);
        writer.WriteByte(1); // Count

        writer.WriteInt((int) job.Code());
        for (int i = 0; i < SKILL_TYPES; i++) {
            int count = 0;
            for (int j = 0; j < SKILL_RANKS; j++) {
                count += skills[i, j].Values.Sum(skill => skill.Count);
            }
            writer.WriteByte((byte) count);

            for (int j = 0; j < SKILL_RANKS; j++) {
                foreach (Skill skill in skills[i, j].Values) {
                    writer.WriteClass<Skill>(skill);
                }
            }
        }
    }

    public class Skill : IByteSerializable {
        public readonly int Id;
        public readonly int[] SubIds;
        public readonly short BaseLevel;
        public short Level;
        public bool Notify;

        public int Count => SubIds.Length + 1;

        public Skill(int id, int[] subIds, short baseLevel) {
            Id = id;
            SubIds = subIds;
            BaseLevel = baseLevel;
            Level = baseLevel;
        }

        public void WriteTo(IByteWriter writer) {
            writer.WriteBool(Notify);
            writer.WriteBool(Level > 0);
            writer.WriteInt(Id);
            writer.WriteInt(Level);
            writer.WriteByte();

            Notify = false;

            foreach (int subId in SubIds) {
                writer.WriteBool(false);
                writer.WriteBool(Level > 0);
                writer.WriteInt(subId);
                writer.WriteInt(Level);
                writer.WriteByte();
            }
        }
    }
}
