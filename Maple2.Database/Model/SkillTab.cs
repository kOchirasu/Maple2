using System;
using System.Collections.Generic;
using System.Linq;
using Maple2.Database.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model;

internal class SkillTab {
    public long CharacterId { get; set; }
    public long Id { get; set; }
    public string Name { get; set; }
    public IList<SkillTabEntry> Skills;

    public DateTime CreationTime { get; set; }

    public static implicit operator SkillTab(Maple2.Model.Game.SkillTab other) {
        return other == null ? null : new SkillTab {
            Id = other.Id,
            Name = other.Name,
            Skills = other.Skills.Select(entry => new SkillTabEntry {
                SkillId = entry.SkillId,
                Points = entry.Points,
            }).ToList(),
        };
    }

    public static implicit operator Maple2.Model.Game.SkillTab(SkillTab other) {
        if (other == null) {
            return null;
        }

        List<Maple2.Model.Game.SkillTab.Skill> skills = other.Skills.Select(entry =>
            new Maple2.Model.Game.SkillTab.Skill(entry.SkillId, entry.Points)).ToList();
        return new Maple2.Model.Game.SkillTab(other.Name, skills) {
            Id = other.Id,
        };
    }

    public static void Configure(EntityTypeBuilder<SkillTab> builder) {
        builder.ToTable("skill-tab");
        builder.HasKey(tab => new {tab.CharacterId, tab.Id});
        builder.HasIndex(tab => tab.CharacterId);
        builder.Property(tab => tab.Skills).HasJsonConversion().IsRequired();

        builder.HasOne<Character>()
            .WithMany()
            .HasForeignKey(tab => tab.CharacterId);

        IMutableProperty creationTime = builder.Property(club => club.CreationTime)
            .ValueGeneratedOnAdd().Metadata;
        creationTime.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
    }
}

internal class SkillTabEntry {
    public int SkillId { get; set; }
    public int Points { get; set; }
}
