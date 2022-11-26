using System.Runtime.InteropServices;

namespace Maple2.Model.Game;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 14)]
public readonly struct RewardItem {
    public readonly int ItemId;
    public readonly short Rarity;
    public readonly int Amount;
    public readonly bool Unknown1;
    public readonly bool Unknown2;
    public readonly bool Unknown3;
    public readonly bool Unknown4;

    public RewardItem(int itemId, short rarity, int amount) {
        ItemId = itemId;
        Rarity = rarity;
        Amount = amount;
    }
}
