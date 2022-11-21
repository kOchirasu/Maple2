
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game;

public class Fish : IByteSerializable {
    
    public int Id { get; }
    public int TotalCaught { get; set; }
    public int TotalPrizeFish { get; set; }
    public int LargestSize { get; set; }

    public Fish(int id, int fishSize) {
        Id = id;
        TotalCaught = 1;
        LargestSize = fishSize;
    }

    public void WriteTo(IByteWriter writer) {
        writer.WriteInt(Id);
        writer.WriteInt(TotalCaught);
        writer.WriteInt(TotalPrizeFish);
        writer.WriteInt(LargestSize);
    }
}
