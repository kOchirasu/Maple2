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
        var skills = new Dictionary<SkillRank, (List<JobInfo.Skill>, List<JobInfo.Skill>)> {
            [SkillRank.Basic] = (new List<JobInfo.Skill>(), new List<JobInfo.Skill>()),
            [SkillRank.Awakening] = (new List<JobInfo.Skill>(), new List<JobInfo.Skill>())
        };

        var baseSkills = new HashSet<int>(jobTable.BaseSkills);
        foreach ((SkillRank rank, JobTable.Skill[] jobSkills) in jobTable.Skills) {
            Debug.Assert(rank is SkillRank.Basic or SkillRank.Awakening);

            foreach (JobTable.Skill skill in jobSkills) {
                if (!storage.TryGet(skill.Main, out SkillMetadata metadata)) {
                    throw new InvalidOperationException($"Nonexistent skillId:{skill.Main}");
                }
                Debug.Assert(metadata.Property.Type is SkillType.Active or SkillType.Passive);

                short baseLevel = (short) (baseSkills.Contains(skill.Main) ? 1 : 0);
                switch (metadata.Property.Type) {
                    case SkillType.Active:
                        skills[rank].Item1.Add(new JobInfo.Skill(skill.Main, skill.Sub, baseLevel));
                        break;
                    case SkillType.Passive:
                        skills[rank].Item2.Add(new JobInfo.Skill(skill.Main, skill.Sub, baseLevel));
                        break;
                }
            }
        }

        JobInfo = new JobInfo(job, skills[SkillRank.Basic], skills[SkillRank.Awakening]);
    }

    public void UpdateSkills(IList<(int, short)> skills) {
        foreach ((int skillId, short level) in skills) {
            if (!JobInfo.Skills.TryGetValue(skillId, out JobInfo.Skill skill)) {
                continue;
            }

            if (skill.Level == skill.BaseLevel && level > skill.BaseLevel) {
                skill.Notify = true;
            }
            skill.Level = level;
        }
    }

    public void ResetSkills(SkillRank rank = SkillRank.Both) {
        if (rank is SkillRank.Basic or SkillRank.Both) {
            foreach (JobInfo.Skill skill in JobInfo.BasicSkills.Active) {
                skill.Level = skill.BaseLevel;
            }
            foreach (JobInfo.Skill skill in JobInfo.BasicSkills.Passive) {
                skill.Level = skill.BaseLevel;
            }
        }
        if (rank is SkillRank.Awakening or SkillRank.Both) {
            foreach (JobInfo.Skill skill in JobInfo.AwakeningSkills.Active) {
                skill.Level = skill.BaseLevel;
            }
            foreach (JobInfo.Skill skill in JobInfo.AwakeningSkills.Passive) {
                skill.Level = skill.BaseLevel;
            }
        }
    }
}
