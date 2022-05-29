using System;
using System.Collections.Generic;
using System.Diagnostics;
using Maple2.Database.Storage;
using Maple2.Model;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Game.Session;

namespace Maple2.Server.Game.Manager;

public class SkillManager {
    public readonly JobInfo JobInfo;

    public SkillManager(GameSession session) {
        Player player = session.Player;
        JobTable.Entry jobTableEntry = session.TableMetadata.JobTable.Entries[player.Character.Job.Code()];
        JobInfo = new JobInfo(player.Character.Job);

        var baseSkills = new HashSet<int>(jobTableEntry.BaseSkills);
        foreach ((SkillRank rank, JobTable.Skill[] jobSkills) in jobTableEntry.Skills) {
            Debug.Assert(rank is SkillRank.Basic or SkillRank.Awakening);

            foreach (JobTable.Skill skill in jobSkills) {
                if (!session.SkillMetadata.TryGet(skill.Main, out SkillMetadata? metadata)) {
                    throw new InvalidOperationException($"Nonexistent skillId:{skill.Main}");
                }

                short baseLevel = (short) (baseSkills.Contains(skill.Main) ? 1 : 0);
                JobInfo.AddSkill(metadata.Property.Type, rank, new JobInfo.Skill(skill.Main, skill.Sub, baseLevel));
            }
        }
    }

    public void UpdateSkill(int skillId, short level, bool enabled) {
        JobInfo.Skill? skill = JobInfo.GetSkill(skillId);
        if (skill == null) {
            return;
        }

        // Level must be set to 0 if not enabled since there is a placeholder value of 1.
        if (!enabled) {
            skill.Level = 0;
            return;
        }

        if (skill.Level == skill.BaseLevel && level > skill.BaseLevel) {
            skill.Notify = true;
        }

        skill.Level = level;
    }

    public void ResetSkills(SkillRank rank = SkillRank.Both) {
        foreach (SkillType type in Enum.GetValues(typeof(SkillType))) {
            foreach (JobInfo.Skill skill in JobInfo.GetSkills(type, rank)) {
                skill.Level = skill.BaseLevel;
            }
        }
    }
}
