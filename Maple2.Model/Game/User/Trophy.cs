using System.Runtime.InteropServices;

namespace Maple2.Model.Game;

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 12)]
public struct Trophy {
    public int Combat { get; set; }
    public int Adventure { get; set; }
    public int Lifestyle { get; set; }

    public static Trophy operator +(in Trophy a, in Trophy b) {
        return new Trophy {
            Combat = a.Combat + b.Combat,
            Adventure = a.Adventure + b.Adventure,
            Lifestyle = a.Lifestyle + b.Lifestyle,
        };
    }

    public static Trophy operator -(in Trophy a, in Trophy b) {
        return new Trophy {
            Combat = a.Combat - b.Combat,
            Adventure = a.Adventure - b.Adventure,
            Lifestyle = a.Lifestyle - b.Lifestyle,
        };
    }

    public int Total => Combat + Adventure + Lifestyle;
}
