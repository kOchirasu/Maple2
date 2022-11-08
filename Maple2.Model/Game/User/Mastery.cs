using System.Runtime.InteropServices;

namespace Maple2.Model.Game; 

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 48)]
public struct Mastery {
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
}
