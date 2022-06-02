using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Maple2.Model;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Server.Game.Session;
using Maple2.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Manager.Config;

public class SkillInfo : IByteSerializable {
    public const int SKILL_TYPES = 4;
    public const int SKILL_RANKS = 2;

    private readonly List<Skill> basicSkills;
    private readonly List<Skill> awakeningSkills;

    public readonly Job Job;
    public readonly Dictionary<int, Skill>[,] Skills;

    public SkillInfo(GameSession session, SkillTab skillTab) {
        Player player = session.Player;
        JobTable.Entry jobTableEntry = session.TableMetadata.JobTable.Entries[player.Character.Job.Code()];

        var baseSkills = new HashSet<int>(jobTableEntry.BaseSkills);
        basicSkills = new List<Skill>();
        awakeningSkills = new List<Skill>();
        foreach ((SkillRank rank, JobTable.Skill[] jobSkills) in jobTableEntry.Skills) {
            Debug.Assert(rank is SkillRank.Basic or SkillRank.Awakening);

            foreach (JobTable.Skill skill in jobSkills) {
                if (!session.SkillMetadata.TryGet(skill.Main, out SkillMetadata? metadata)) {
                    throw new InvalidOperationException($"Nonexistent skillId:{skill.Main}");
                }

                short baseLevel = (short) (baseSkills.Contains(skill.Main) ? 1 : 0);
                switch (rank) {
                    case SkillRank.Basic:
                        basicSkills.Add(new Skill(metadata, skill.Sub, baseLevel));
                        break;
                    case SkillRank.Awakening:
                        awakeningSkills.Add(new Skill(metadata, skill.Sub, baseLevel));
                        break;
                }
            }
        }

        Job = player.Character.Job;
        Skills = new Dictionary<int, Skill>[SKILL_TYPES,SKILL_RANKS];
        for (int i = 0; i < SKILL_TYPES; i++) {
            for (int j = 0; j < SKILL_RANKS; j++) {
                Skills[i, j] = new Dictionary<int, Skill>();
            }
        }

        SetTab(skillTab);
    }

    public void SetTab(SkillTab skillTab) {
        foreach (Dictionary<int, Skill> dict in Skills) {
            dict.Clear();
        }

        foreach (Skill skill in basicSkills) {
            Skills[(int) skill.Metadata.Property.Type, 0].Add(skill.Id, skill);
        }
        foreach (Skill skill in awakeningSkills) {
            Skills[(int) skill.Metadata.Property.Type, 1].Add(skill.Id, skill);
        }

        foreach (SkillTab.Skill tabSkill in skillTab.Skills) {
            Skill? skill = GetSkill(tabSkill.SkillId);
            if (skill != null) {
                skill.Level = (short) (skill.BaseLevel + tabSkill.Points);
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
        public readonly SkillMetadata Metadata;

        public int Id => Metadata.Id;
        public readonly int[] SubIds;
        public readonly short BaseLevel;
        public short Level;
        public bool Notify;

        public int Count => SubIds.Length + 1;

        public Skill(SkillMetadata metadata, int[] subIds, short baseLevel) {
            Metadata = metadata;
            SubIds = subIds;
            BaseLevel = baseLevel;
            Level = baseLevel;
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
