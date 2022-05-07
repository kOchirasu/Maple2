using System;
using System.Collections.Generic;
using System.Diagnostics;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;

namespace Maple2.Server.Game.Manager;

public class SkillManager {
    public readonly JobInfo JobInfo;

    public SkillManager(Job job, SkillMetadataStorage storage, JobTable.Entry jobTable) {
        JobInfo = new JobInfo(job);

        var baseSkills = new HashSet<int>(jobTable.BaseSkills);
        foreach ((SkillRank rank, JobTable.Skill[] jobSkills) in jobTable.Skills) {
            Debug.Assert(rank is SkillRank.Basic or SkillRank.Awakening);

            foreach (JobTable.Skill skill in jobSkills) {
                if (!storage.TryGet(skill.Main, out SkillMetadata? metadata)) {
                    throw new InvalidOperationException($"Nonexistent skillId:{skill.Main}");
                }

                short baseLevel = (short) (baseSkills.Contains(skill.Main) ? 1 : 0);
                JobInfo.AddSkill(metadata.Property.Type, rank, new JobInfo.Skill(skill.Main, skill.Sub, baseLevel));
            }
        }
    }

    public void UpdateSkills(IList<(int, short)> skills) {
        foreach ((int skillId, short level) in skills) {
            JobInfo.Skill? skill = JobInfo.GetSkill(skillId);
            if (skill == null) {
                continue;
            }

            if (skill.Level == skill.BaseLevel && level > skill.BaseLevel) {
                skill.Notify = true;
            }
            skill.Level = level;
        }
    }

    public void ResetSkills(SkillRank rank = SkillRank.Both) {
        foreach (SkillType type in Enum.GetValues(typeof(SkillType))) {
            foreach (JobInfo.Skill skill in JobInfo.GetSkills(type, rank)) {
                skill.Level = skill.BaseLevel;
            }
        }
    }
}
