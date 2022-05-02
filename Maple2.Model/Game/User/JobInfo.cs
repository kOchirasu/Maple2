using System.Collections.Generic;
using System.Linq;
using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Model.Game;

public class JobInfo : IByteSerializable {
    public readonly Job Job;

    public readonly IReadOnlyDictionary<int, Skill> Skills;

    public readonly (IReadOnlyList<Skill> Active, IReadOnlyList<Skill> Passive) BasicSkills;
    public readonly (IReadOnlyList<Skill> Active, IReadOnlyList<Skill> Passive) AwakeningSkills;

    public JobInfo(Job job, (List<Skill>, List<Skill>) basicSkills, (List<Skill>, List<Skill>) awakeningSkills) {
        Job = job;
        BasicSkills = basicSkills;
        AwakeningSkills = awakeningSkills;

        // Index from SkillId to Skill
        var index = new Dictionary<int, Skill>();
        foreach (Skill skill in BasicSkills.Active) {
            index[skill.Id] = skill;
        }
        foreach (Skill skill in BasicSkills.Passive) {
            index[skill.Id] = skill;
        }
        foreach (Skill skill in AwakeningSkills.Active) {
            index[skill.Id] = skill;
        }
        foreach (Skill skill in AwakeningSkills.Passive) {
            index[skill.Id] = skill;
        }
        Skills = index;
    }

    public void WriteTo(IByteWriter writer) {
        int activeCount = BasicSkills.Active.Sum(skill => skill.Count);
        activeCount += AwakeningSkills.Active.Sum(skill => skill.Count);
        int passiveCount = BasicSkills.Passive.Sum(skill => skill.Count);
        passiveCount += AwakeningSkills.Passive.Sum(skill => skill.Count);

        writer.WriteInt((int) Job);
        writer.WriteByte(1); // Count
        writer.WriteInt((int) Job.Code());
        writer.WriteByte((byte) activeCount);
        foreach (Skill skill in BasicSkills.Active) {
            writer.WriteClass<Skill>(skill);
        }
        foreach (Skill skill in AwakeningSkills.Active) {
            writer.WriteClass<Skill>(skill);
        }
        writer.WriteByte((byte) passiveCount);
        foreach (Skill skill in BasicSkills.Passive) {
            writer.WriteClass<Skill>(skill);
        }
        foreach (Skill skill in AwakeningSkills.Passive) {
            writer.WriteClass<Skill>(skill);
        }

        writer.WriteByte(); // SkillType.Special
        writer.WriteByte(); // SkillType.Consumable
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
