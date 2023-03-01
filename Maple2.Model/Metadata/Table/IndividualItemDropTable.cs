using System.Collections.Generic;
using Maple2.Model.Enum;

namespace Maple2.Model.Metadata;

public record IndividualItemDropTable(
    //IReadOnlyDictionary<int, Dictionary<byte, IList<IndividualItemDropTable.Entry>>> Default,
    //IReadOnlyDictionary<int, Dictionary<byte, IList<IndividualItemDropTable.Entry>>> Gacha,
    //IReadOnlyDictionary<int, Dictionary<byte, IList<IndividualItemDropTable.Entry>>> GearBox,
    //IReadOnlyDictionary<int, Dictionary<byte, IList<IndividualItemDropTable.Entry>>> Monster,
    //IReadOnlyDictionary<int, Dictionary<byte, IList<IndividualItemDropTable.Entry>>> Cash,
    //IReadOnlyDictionary<int, Dictionary<byte, IList<IndividualItemDropTable.Entry>>> Event,
    //IReadOnlyDictionary<int, Dictionary<byte, IList<IndividualItemDropTable.Entry>>> EventNpc,
    //IReadOnlyDictionary<int, Dictionary<byte, IList<IndividualItemDropTable.Entry>>> NewGacha,
    //IReadOnlyDictionary<int, Dictionary<byte, IList<IndividualItemDropTable.Entry>>> Pet,
    //IReadOnlyDictionary<int, Dictionary<byte, IList<IndividualItemDropTable.Entry>>> Quest,
    Dictionary<int, Dictionary<byte, IList<IndividualItemDropTable.Entry>>> QuestMonster): Table {

    public record Entry(
        IList<int> ItemIds,
        bool SmartGender,
        float MinCount,
        float MaxCount);
}
