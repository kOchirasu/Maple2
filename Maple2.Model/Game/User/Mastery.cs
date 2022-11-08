using System.Runtime.InteropServices;
using Maple2.PacketLib.Tools;
using Maple2.Tools;
using Maple2.Tools.Extensions;

namespace Maple2.Model.Game; 

//[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 48)]
public class Mastery : IByteSerializable {
    private int Unknown;
    
    public int Fishing { get; set; }
    public int Instrument { get; set; }
    public int Mining { get; set; }
    public int Foraging { get; set; }
    public int Ranching { get; set; }
    public int Farming { get; set; }
    public int Smithing { get; set; }
    public int Handicrafts { get; set; }
    public int Alchemy { get; set; }
    public int Cooking { get; set; }
    public int PetTaming { get; set; }
    
    
    public void WriteTo(IByteWriter writer) {
        writer.WriteInt(Unknown);
        writer.WriteInt(Fishing);
        writer.WriteInt(Instrument);
        writer.WriteInt(Mining);
        writer.WriteInt(Foraging);
        writer.WriteInt(Ranching);
        writer.WriteInt(Farming);
        writer.WriteInt(Smithing);
        writer.WriteInt(Handicrafts);
        writer.WriteInt(Alchemy);
        writer.WriteInt(Cooking);
        writer.WriteInt(PetTaming);
    }
}
