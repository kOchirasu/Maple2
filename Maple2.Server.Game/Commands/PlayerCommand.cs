using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Linq;
using Maple2.Model;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.Model.Metadata;
using Maple2.Server.Core.Constants;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Enum = Google.Protobuf.WellKnownTypes.Enum;

namespace Maple2.Server.Game.Commands;

public class PlayerCommand : Command {
    private const string NAME = "player";
    private const string DESCRIPTION = "Player management.";

    public PlayerCommand(GameSession session) : base(NAME, DESCRIPTION) {
        AddCommand(new LevelCommand(session));
        AddCommand(new JobCommand(session));
    }

    private class LevelCommand : Command {
        private readonly GameSession session;

        public LevelCommand(GameSession session) : base("level", "Set player level.") {
            this.session = session;

            var level = new Argument<short>("level", "Level of the player.");

            AddArgument(level);
            this.SetHandler<InvocationContext, short>(Handle, level);
        }

        private void Handle(InvocationContext ctx, short level) {
            try {
                if (level is < 1 or > Constant.characterMaxLevel) {
                    ctx.Console.Error.WriteLine($"Invalid level: {level}");
                    return;
                }

                session.Player.Value.Character.Level = level;
                session.Field?.Multicast(LevelUpPacket.LevelUp(session.Player));
                ctx.ExitCode = 0;
            } catch (SystemException ex) {
                ctx.Console.Error.WriteLine(ex.Message);
                ctx.ExitCode = 1;
            }
        }
    }

    private class JobCommand : Command {
        private readonly GameSession session;

        public JobCommand(GameSession session) : base("job", "Set player job.") {
            this.session = session;

            var jobCode = new Argument<JobCode>("jobcode", "JobCode of the player.");
            var awakening = new Option<bool>("awakening", "Awakening job advancement.");

            AddArgument(jobCode);
            AddOption(awakening);
            this.SetHandler<InvocationContext, JobCode, bool>(Handle, jobCode, awakening);
        }

        private void Handle(InvocationContext ctx, JobCode jobCode, bool awakening) {
            try {
                Job job = jobCode switch {
                    JobCode.Newbie => Job.Newbie,
                    JobCode.Knight => awakening ? Job.KnightII : Job.Knight,
                    JobCode.Berserker => awakening ? Job.BerserkerII : Job.Berserker,
                    JobCode.Wizard => awakening ? Job.WizardII : Job.Wizard,
                    JobCode.Priest => awakening ? Job.PriestII : Job.Priest,
                    JobCode.Ranger => awakening ? Job.RangerII : Job.Ranger,
                    JobCode.HeavyGunner => awakening ? Job.HeavyGunnerII : Job.HeavyGunner,
                    JobCode.Thief => awakening ? Job.ThiefII : Job.Thief,
                    JobCode.Assassin => awakening ? Job.AssassinII : Job.Assassin,
                    JobCode.RuneBlader => awakening ? Job.RuneBladerII : Job.RuneBlader,
                    JobCode.Striker => awakening ? Job.StrikerII : Job.Striker,
                    JobCode.SoulBinder => awakening ? Job.SoulBinderII : Job.SoulBinder,
                    _ => throw new ArgumentException($"Invalid JobCode: {jobCode}")
                };

                Job currentJob = session.Player.Value.Character.Job;
                if (currentJob.Code() != job.Code()) {
                    foreach (SkillTab skillTab in session.Config.Skill.SkillBook.SkillTabs) {
                        skillTab.Skills.Clear();
                    }
                } else if (job < currentJob) {
                    foreach (SkillTab skillTab in session.Config.Skill.SkillBook.SkillTabs) {
                        foreach (int skillId in skillTab.Skills.Keys.ToList()) {
                            if (session.Config.Skill.SkillInfo.GetSkill(skillId, SkillRank.Awakening) != null) {
                                skillTab.Skills.Remove(skillId);
                            }
                        }
                    }
                    session.Config.Skill.ResetSkills(SkillRank.Awakening);
                }

                session.Player.Value.Character.Job = job;
                session.Config.Skill.SkillInfo.SetJob(job);
                session.Field?.Multicast(JobPacket.Awakening(session.Player, session.Config.Skill.SkillInfo));
                ctx.ExitCode = 0;
            } catch (SystemException ex) {
                ctx.Console.Error.WriteLine(ex.Message);
                ctx.ExitCode = 1;
            }
        }
    }
}
