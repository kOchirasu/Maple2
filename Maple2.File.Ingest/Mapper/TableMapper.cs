using Maple2.File.IO;
using Maple2.File.Parser;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;

namespace Maple2.File.Ingest.Mapper; 

public class TableMapper : TypeMapper<TableMetadata> {
    private readonly M2dReader xmlReader;
    
    public TableMapper(M2dReader xmlReader) {
        this.xmlReader = xmlReader;
    }
    
    protected override IEnumerable<TableMetadata> Map() {
        yield return new TableMetadata {Name = "job.xml", Table = ParseJobTable()};
    }

    private JobTable ParseJobTable() {
        var parser = new JobTableParser(xmlReader);
        var results = new Dictionary<JobCode, JobTable.Entry>();
        foreach (File.Parser.Xml.Table.JobTable data in parser.Parse()) {
            var skills = new Dictionary<SkillRank, JobTable.Skill[]>();
            skills[SkillRank.Basic] = data.skills.skill
                .Where(skill => skill.subJobCode <= data.code) // This is not actually correct, but works.
                .Select(skill => new JobTable.Skill(skill.main, skill.sub, skill.maxLevel, skill.quickSlotPriority))
                .ToArray();
            skills[SkillRank.Awakening] = data.skills.skill
                .Where(skill => skill.subJobCode > data.code) // This is not actually correct, but works.
                .Select(skill => new JobTable.Skill(skill.main, skill.sub, skill.maxLevel, skill.quickSlotPriority))
                .ToArray();

            results[(JobCode) data.code] = new JobTable.Entry(
                Tutorial:new JobTable.Tutorial(
                    StartField:data.startField,
                    SkipField:data.tutorialSkipField.Length > 0 ? data.tutorialSkipField[0] : 0,
                    SkipItem:data.tutorialSkipItem,
                    OpenMaps:data.tutorialClearOpenMaps,
                    OpenTaxis:data.tutorialClearOpenTaxis,
                    StartItem:data.startInvenItem.item.Select(item => 
                        new JobTable.Item(item.itemID, item.grade, item.count)).ToArray(),
                    Reward:data.reward.item.Select(item => 
                        new JobTable.Item(item.itemID, item.grade, 1)).ToArray()),
                Skills:skills,
                BaseSkills:data.learn.SelectMany(learn => learn.skill)
                    .SelectMany(skill => skill.sub.Append(skill.id)).OrderBy(id => id).ToArray()
            );
        }
        
        return new JobTable(results);
    }
}
