using System.Runtime.InteropServices;

namespace Maple2.Model.Game; 

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 12)]
public struct Trophy {
    public int Combat { get; set; }
    public int Adventure { get; set; }
    public int Lifestyle { get; set; }
}
