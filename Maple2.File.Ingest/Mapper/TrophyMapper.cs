using Maple2.File.IO;
using Maple2.File.Parser;
using Maple2.File.Parser.Xml.Achieve;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;

namespace Maple2.File.Ingest.Mapper;

public class TrophyMapper : TypeMapper<TrophyMetadata> {
    private readonly AchieveParser parser;

    public TrophyMapper(M2dReader xmlReader) {
        parser = new AchieveParser(xmlReader);
    }

    protected override IEnumerable<TrophyMetadata> Map() {
        foreach ((int id, string name, AchieveData data) in parser.Parse()) {
            var grades = new Dictionary<int, TrophyMetadataGrade>();
            foreach (Grade grade in data.grade) {
                grades.Add(grade.value, new TrophyMetadataGrade(
                    new TrophyMetadataCondition(
                        Code: grade.condition.code,
                        Value: grade.condition.value,
                        Target: grade.condition.target),
                    new TrophyMetadataReward(
                        Type: (TrophyRewardType) grade.reward.type,
                        Code: grade.reward.code,
                        Value: grade.reward.value,
                        Rank: grade.reward.rank)));
            }

            TrophyCategory category = TrophyCategory.Life;
            string[] tags = data.categoryTag;
            if (data.categoryTag.Length > 0) {
                // remove the first in the array and use it as the trophy type
                category = GetTrophyType(tags[0]);
                tags = tags.Skip(1).ToArray();
                
            }
            
            yield return new TrophyMetadata(
                Id: id,
                Name: name,
                AccountWide: data.account,
                NoticePercent: data.noticePercent,
                Category: category,
                CategoryTags: tags,
                ConditionType: (TrophyConditionType) data.grade.First().condition.type, // Using the first considering all grades have the same condition type
                Grades: grades);
        }
    }

    private static TrophyCategory GetTrophyType(string tag) {
        return tag switch {
            "combat" => TrophyCategory.Combat,
            "adventure" => TrophyCategory.Adventure,
            "living" => TrophyCategory.Life,
            _ => TrophyCategory.Life,
        };
    }
}
