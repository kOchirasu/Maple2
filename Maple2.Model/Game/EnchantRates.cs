using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game;

// We store this data as ints, but write to packet as float.
// Using ints for computation avoids rounding errors.
public class EnchantRates : IByteSerializable {
    public int Success;
    public int Unknown1;
    public int Unknown2;
    public int Fodder;
    public int Charge;

    public int Total => Success + Unknown1 + Unknown2 + Fodder + Charge;

    public void Clear() {
        Success = 0;
        Unknown1 = 0;
        Unknown2 = 0;
        Fodder = 0;
        Charge = 0;
    }

    public void WriteTo(IByteWriter writer) {
        writer.WriteFloat(Success);
        writer.WriteFloat(Unknown1);
        writer.WriteFloat(Unknown2);
        writer.WriteFloat(Fodder);
        writer.WriteFloat(Charge);
    }
}
