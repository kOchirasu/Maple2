using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game.Shop;

public class RestrictedBuyData : IByteSerializable {

    public long StartTime { get; init; }
    public long EndTime { get; init; }
    public IList<BuyTimeOfDay> TimeRanges { get; init; }
    public IList<ShopBuyDay> Days { get; init; }

    public RestrictedBuyData() {
        TimeRanges = new List<BuyTimeOfDay>();
        Days = new List<ShopBuyDay>();
    }

    public void WriteTo(IByteWriter writer) {
        writer.WriteBool(StartTime > 0 && EndTime > 0);
        writer.WriteLong(StartTime);
        writer.WriteLong(EndTime);
        writer.WriteShort((short) TimeRanges.Count);

        foreach (BuyTimeOfDay time in TimeRanges) {
            writer.Write<BuyTimeOfDay>(time);
        }

        writer.WriteByte((byte) Days.Count);
        foreach (ShopBuyDay day in Days) {
            writer.Write<ShopBuyDay>(day);
        }
    }
}

[StructLayout(LayoutKind.Sequential, Size = 8)]
public readonly struct BuyTimeOfDay {
    public int StartTimeOfDay { get; }
    public int EndTimeOfDay { get; }

    [JsonConstructor]
    public BuyTimeOfDay(int startTime, int endTime) {
        StartTimeOfDay = startTime;
        EndTimeOfDay = endTime;
    }
}
