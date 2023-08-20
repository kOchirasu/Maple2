using Maple2.File.IO;
using Maple2.File.Parser;
using Maple2.File.Parser.Enum;
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
                    Condition: new TrophyMetadataCondition(
                        Type: (TrophyConditionType) grade.condition.type,
                        Codes: GetCodes(grade.condition.code),
                        Value: grade.condition.value,
                        Target: GetCodes(grade.condition.target)),
                    Reward: grade.reward.type == AchieveRewardType.unknown ? null : new TrophyMetadataReward(
                        Type: (TrophyRewardType) grade.reward.type,
                        Code: grade.reward.code,
                        Value: grade.reward.value,
                        Rank: grade.reward.rank)));
            }

            TrophyCategory category = TrophyCategory.Life;
            string[] tags = data.categoryTag;
            if (data.categoryTag.Length > 0) {
                // skip the first in the array and use it as the trophy category
                category = GetTrophyCategory(tags[0]);
                tags = tags.Skip(1).ToArray();

            }

            yield return new TrophyMetadata(
                Id: id,
                Name: name,
                AccountWide: data.account,
                Category: category,
                CategoryTags: tags,
                Grades: grades);
        }
    }

    private static TrophyCategory GetTrophyCategory(string tag) {
        return tag switch {
            "combat" => TrophyCategory.Combat,
            "adventure" => TrophyCategory.Adventure,
            "living" => TrophyCategory.Life,
            _ => TrophyCategory.Life,
        };
    }

    private TrophyMetadataCondition.Code? GetCodes(string[] codes) {
        if (codes.Length == 0) {
            return null;
        }
        if (codes.Length > 1) {
            List<int> integers = new();
            List<string> strings = new();
            foreach (string code in codes) {
                if (!int.TryParse(code, out int intCode)) {
                    strings.Add(code);
                }
                else {
                    integers.Add(intCode);
                }
            }

            return new TrophyMetadataCondition.Code(
                Strings: strings.Count == 0 ? null : strings.ToArray(),
                Integers: integers.Count == 0 ? null : integers.ToArray(),
                Range: null);
        }

        string[] split = codes[0].Split('-');
        if (split.Length > 1) {
            return new TrophyMetadataCondition.Code(
                Strings: null,
                Integers: null,
                Range: new TrophyMetadataCondition.Range<int>(int.Parse(split[0]), int.Parse(split[1])));
        }

        if (!int.TryParse(codes[0], out int integerResult)) {
            return new TrophyMetadataCondition.Code(
                Strings: new[] {codes[0]},
                Range: null,
                Integers: null);
        }
        return new TrophyMetadataCondition.Code(
            Strings: null,
            Range: null,
            Integers: new[] {integerResult});
    }
}
