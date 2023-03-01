using System.Collections.Generic;
using System.Linq;
using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Model.Game.Event;

/// <summary>
/// Attendance Event
/// </summary>
public class AttendGift : GameEventInfo {
    public string AttendanceName { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public long BeginTime { get; init; }
    public long EndTime { get; init; }
    public bool DisableClaimButton { get; init; }
    public int TimeRequired { get; init; }
    public AttendGiftCurrencyType SkipDayCurrencyType { get; init; }
    public int SkipDaysAllowed { get; init; }
    public long SkipDayCost { get; init; }
    public IList<Item> Days { get; set; }

    public AttendGift() {
        Days = new List<Item>();
    }

    public override void WriteTo(IByteWriter writer) {
        writer.WriteInt(Id);
        writer.WriteLong(BeginTime);
        writer.WriteLong(EndTime);
        writer.WriteUnicodeString(AttendanceName);
        writer.WriteString(Url);
        writer.WriteByte();
        writer.WriteBool(DisableClaimButton);
        writer.WriteInt(TimeRequired);
        writer.WriteByte();
        writer.WriteInt();
        writer.Write<AttendGiftCurrencyType>(SkipDayCurrencyType);

        if (SkipDayCurrencyType != AttendGiftCurrencyType.None) {
            writer.WriteInt(SkipDaysAllowed);
            writer.WriteLong(SkipDayCost);
            writer.WriteInt();
        }

        writer.WriteInt(Days.Count);
        foreach (Item day in Days.OrderBy(day => day.Day)) {
            writer.WriteClass<Item>(day);
        }
    }

    public class Item : IByteSerializable {
        public int Day { get; init; }
        public short ItemRarity { get; init; }
        public int ItemId { get; init; }
        public int ItemAmount { get; init; }

        public void WriteTo(IByteWriter writer) {
            writer.WriteInt(ItemId);
            writer.WriteShort(ItemRarity);
            writer.WriteInt(ItemAmount);
            writer.WriteByte();
            writer.WriteByte();
            writer.WriteByte();
            writer.WriteByte();
        }
    }
}
