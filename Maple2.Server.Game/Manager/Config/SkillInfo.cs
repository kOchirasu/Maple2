using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Maple2.Database.Storage;
using Maple2.Model;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Manager.Config;

public class SkillInfo : IByteSerializable {
    public const int SKILL_TYPES = 4;
    public const int SKILL_RANKS = 2;

    private readonly JobTable jobTable;
    private readonly SkillMetadataStorage skillMetadata;

    public Job Job { get; private set; }
    public readonly IDictionary<int, Skill>[,] Skills;

    public SkillInfo(JobTable jobTable, SkillMetadataStorage skillMetadata, Job job, SkillTab? skillTab) {
        this.jobTable = jobTable;
        this.skillMetadata = skillMetadata;

        Skills = new IDictionary<int, Skill>[SKILL_TYPES,SKILL_RANKS];
        for (int i = 0; i < SKILL_TYPES; i++) {
            for (int j = 0; j < SKILL_RANKS; j++) {
                Skills[i, j] = new Dictionary<int, Skill>();
            }
        }

        SetJob(job);
        SetTab(skillTab);

        // Disable all notifications when loading.
        foreach (IDictionary<int, Skill> dict in Skills) {
            foreach (Skill skill in dict.Values) {
                skill.Notify = false;
            }
        }
    }

    public void SetJob(Job job) {
        // If both jobs shared the same JobCode, we do not need to update Skills.
        if (Job.Code() != job.Code()) {
            foreach (IDictionary<int, Skill> dict in Skills) {
                dict.Clear();
            }

            if (!jobTable.Entries.TryGetValue(job.Code(), out JobTable.Entry? jobTableEntry)) {
                throw new ArgumentException($"No Table found for job: {job} => {job.Code()}");
            }

            var baseSkills = new HashSet<int>(jobTableEntry.BaseSkills);
            foreach ((SkillRank rank, JobTable.Skill[] jobSkills) in jobTableEntry.Skills) {
                Debug.Assert(rank is SkillRank.Basic or SkillRank.Awakening);

                foreach (JobTable.Skill skillData in jobSkills) {
                    if (!skillMetadata.TryGet(skillData.Main, out SkillMetadata? metadata)) {
                        throw new InvalidOperationException($"Nonexistent skillId:{skillData.Main}");
                    }

                    short baseLevel = (short) (baseSkills.Contains(skillData.Main) ? 1 : 0);
                    var skill = new Skill(skillData.Main, skillData.Sub, baseLevel);
                    Skills[(int) metadata.Property.Type, (int) rank].Add(skill.Id, skill);
                }
            }
        }

        Job = job;
    }

    public void SetTab(SkillTab? skillTab) {
        var skillMap = new Dictionary<int, (int, int)>();
        if (skillTab != null) {
            foreach ((int skillId, int points) in skillTab.Skills) {
                skillMap[skillId] = (skillId, points);
            }
        }

        foreach (IDictionary<int, Skill> dict in Skills) {
            foreach (Skill skill in dict.Values) {
                if (skillMap.TryGetValue(skill.Id, out (int SkillId, int Points) tabSkill)) {
                    skill.SetLevel((short) (skill.BaseLevel + tabSkill.Points));
                } else {
                    skill.Reset();
                }
            }
        }
    }

    public IEnumerable<Skill> GetSkills(SkillType type, SkillRank rank) {
        if (rank is SkillRank.Basic or SkillRank.Both) {
            foreach (Skill skill in Skills[(int) type, (int) SkillRank.Basic].Values) {
                yield return skill;
            }
        }

        if (rank is SkillRank.Awakening or SkillRank.Both) {
            foreach (Skill skill in Skills[(int) type, (int) SkillRank.Awakening].Values) {
                yield return skill;
            }
        }
    }

    public Skill? GetSkill(int skillId, SkillRank rank = SkillRank.Both) {
        for (int i = 0; i < SKILL_TYPES; i++) {
            if (rank is SkillRank.Basic or SkillRank.Both) {
                if (Skills[i, (int) SkillRank.Basic].TryGetValue(skillId, out Skill? skill)) {
                    return skill;
                }
            }

            if (rank is SkillRank.Awakening or SkillRank.Both) {
                if (Skills[i, (int) SkillRank.Awakening].TryGetValue(skillId, out Skill? skill)) {
                    return skill;
                }
            }
        }

        return null;
    }

    public void WriteTo(IByteWriter writer) {
        writer.WriteInt((int) Job);
        writer.WriteByte(1); // Count

        writer.WriteInt((int) Job.Code());
        for (int i = 0; i < SKILL_TYPES; i++) {
            int count = 0;
            for (int j = 0; j < SKILL_RANKS; j++) {
                count += Skills[i, j].Values.Sum(skill => skill.Count);
            }
            writer.WriteByte((byte) count);

            for (int j = 0; j < SKILL_RANKS; j++) {
                foreach (Skill skill in Skills[i, j].Values) {
                    writer.WriteClass<Skill>(skill);
                }
            }
        }
    }

    public class Skill : IByteSerializable {
        public readonly int Id;
        public readonly int[] SubIds;
        public readonly short BaseLevel;
        public short Level { get; private set; }
        public bool Notify;

        public int Count => SubIds.Length + 1;


        public Skill(int id, int[] subIds, short baseLevel) {
            Id = id;
            SubIds = subIds;
            BaseLevel = baseLevel;
            Level = baseLevel;
        }

        public void Reset() {
            Level = BaseLevel;
            Notify = false;
        }

        public void SetLevel(short level) {
            if (BaseLevel == 0 && Level == 0 && level > 0) {
                Notify = true;
            }

            Level = Math.Max(BaseLevel, level);
        }

        public void WriteTo(IByteWriter writer) {
            writer.WriteBool(Notify);
            writer.WriteBool(Level > 0);
            writer.WriteInt(Id);
            writer.WriteInt(Math.Max((int)Level, 1));
            writer.WriteByte();

            Notify = false;

            foreach (int subId in SubIds) {
                writer.WriteBool(false);
                writer.WriteBool(Level > 0);
                writer.WriteInt(subId);
                writer.WriteInt(Math.Max((int)Level, 1));
                writer.WriteByte();
            }
        }
    }
}
