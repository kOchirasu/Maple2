using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game;

public class SkillTab : IByteSerializable, IByteDeserializable {
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
    public readonly record struct Skill(int SkillId, int Points);

    public long Id;
    public string Name;
    public List<Skill> Skills;

    public SkillTab(string name, IEnumerable<Skill>? entries = null) {
        Name = name;
        Skills = entries?.ToList() ?? new List<Skill>();
    }

    public void AddOrUpdate(in Skill skill) {
        for (int i = 0; i < Skills.Count; i++) {
            // Update if SkillId already exists.
            if (Skills[i].SkillId == skill.SkillId) {
                Skills[i] = skill;
                return;
            }
        }

        // If not updated, add this skill.
        Skills.Add(skill);
    }

    public void WriteTo(IByteWriter writer) {
        writer.WriteLong(Id);
        writer.WriteUnicodeString(Name);

        writer.WriteInt(Skills.Count);
        foreach (Skill skill in Skills) {
            writer.Write<Skill>(skill);
        }
    }

    public void ReadFrom(IByteReader reader) {
        Id = reader.ReadLong();
        Name = reader.ReadUnicodeString();
        Skills = new List<Skill>();

        int count = reader.ReadInt();
        for (int i = 0; i < count; i++) {
            AddOrUpdate(reader.Read<Skill>());
        }
    }
}
