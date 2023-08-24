using System;
using Maple2.Model.Enum;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game;

public class PremiumMarketPromoData : IByteSerializable {
    public string Name { get; init; }
    public long StartTime { get; init; }
    public long EndTime { get; init; }

    public PremiumMarketPromoData(string name, long startTime, long endTime) {
        Name = name;
        StartTime = startTime;
        EndTime = endTime;
    }
    
    public PremiumMarketPromoData(string name) {
        Name = name;
        StartTime = 0;
        EndTime = 0;
    }

    public void WriteTo(IByteWriter writer) {
        writer.WriteString(Name);
        writer.WriteLong(StartTime);
        writer.WriteLong(EndTime);
    }
}
