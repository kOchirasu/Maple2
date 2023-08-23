using Maple2.File.IO;
using Maple2.File.Parser;
using Maple2.File.Parser.Enum;
using Maple2.File.Parser.Xml.Achieve;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;

namespace Maple2.File.Ingest.Mapper;

public class AchievementMapper : TypeMapper<AchievementMetadata> {
    private readonly AchieveParser parser;

    public AchievementMapper(M2dReader xmlReader) {
        parser = new AchieveParser(xmlReader);
    }

    protected override IEnumerable<AchievementMetadata> Map() {
        foreach ((int id, string name, AchieveData data) in parser.Parse()) {
            var grades = new Dictionary<int, AchievementMetadataGrade>();
            foreach (Grade grade in data.grade) {
                grades.Add(grade.value, new AchievementMetadataGrade(
                    Grade: grade.value,
                    Condition: new AchievementMetadataCondition(
                        Type: (AchievementConditionType) grade.condition.type,
                        Codes: GetCodes(grade.condition.code),
                        Value: grade.condition.value,
                        Target: GetCodes(grade.condition.target)),
                    Reward: grade.reward.type == AchieveRewardType.unknown ? null : new AchievementMetadataReward(
                        Type: (AchievementRewardType) grade.reward.type,
                        Code: grade.reward.code,
                        Value: grade.reward.value,
                        Rank: grade.reward.rank)));
            }

            AchievementCategory category = AchievementCategory.Life;
            string[] tags = data.categoryTag;
            if (data.categoryTag.Length > 0) {
                // skip the first in the array and use it as the trophy category
                category = GetTrophyCategory(tags[0]);
                tags = tags.Skip(1).ToArray();
            }

            yield return new AchievementMetadata(
                Id: id,
                Name: name,
                AccountWide: data.account,
                Category: category,
                CategoryTags: tags,
                Grades: grades);
        }
    }

    private static AchievementCategory GetTrophyCategory(string tag) {
        return tag switch {
            "combat" => AchievementCategory.Combat,
            "adventure" => AchievementCategory.Adventure,
            "living" => AchievementCategory.Life,
            _ => AchievementCategory.Life,
        };
    }

    private AchievementMetadataCondition.Parameters? GetCodes(string[] codes) {
        if (codes.Length == 0) {
            return null;
        }
        if (codes.Length > 1) {
            List<int> integers = new();
            List<string> strings = new();
            foreach (string code in codes) {
                if (!int.TryParse(code, out int intCode)) {
                    strings.Add(code);
                } else {
                    integers.Add(intCode);
                }
            }

            return new AchievementMetadataCondition.Parameters(
                Strings: strings.Count == 0 ? null : strings.ToArray(),
                Integers: integers.Count == 0 ? null : integers.ToArray(),
                Range: null);
        }

        string[] split = codes[0].Split('-');
        if (split.Length > 1) {
            return new AchievementMetadataCondition.Parameters(
                Range: new AchievementMetadataCondition.Range<int>(int.Parse(split[0]), int.Parse(split[1])));
        }

        if (!int.TryParse(codes[0], out int integerResult)) {
            return new AchievementMetadataCondition.Parameters(
                Strings: new[] {codes[0]});
        }
        return new AchievementMetadataCondition.Parameters(
            Integers: new[] {integerResult});
    }
}
