using System.Runtime.InteropServices;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;

namespace Maple2.Model.Game;

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 12)]
public struct FishCatch {
    public int Id { get; }
    public int Exp { get; set; }
    public CaughtFishType Type { get; }
    public short Unknown { get; }

    public FishCatch(FishTable.Entry fishEntry, CaughtFishType type) {
        Id = fishEntry.Id;
        switch (type) {
            case CaughtFishType.Default:
                Exp = fishEntry.Rarity;
                break;
            case CaughtFishType.FirstKind:
            case CaughtFishType.Prize:
                Exp = fishEntry.Rarity * Constant.FishingMasteryIncreaseFactor;
                break;
        }
        Type = type;
        Unknown = 1;
    }
}
