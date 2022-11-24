using System.Text.Json.Serialization;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game;

public class FishEntry : IByteSerializable {
    
    public int Id { get; }
    public int TotalCaught { get; set; }
    public int TotalPrizeFish { get; set; }
    public int LargestSize { get; set; }

    public FishEntry(int id, int fishSize) {
        Id = id;
        LargestSize = fishSize;
    }
    
    [JsonConstructor]
    public FishEntry(int id, int totalCaught, int totalPrizeFish, int largestSize) {
        Id = id;
        TotalCaught = totalCaught;
        TotalPrizeFish = totalPrizeFish;
        LargestSize = largestSize;
    }

    public void WriteTo(IByteWriter writer) {
        writer.WriteInt(Id);
        writer.WriteInt(TotalCaught);
        writer.WriteInt(TotalPrizeFish);
        writer.WriteInt(LargestSize);
    }
}
