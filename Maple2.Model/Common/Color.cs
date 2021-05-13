using System.Runtime.InteropServices;

namespace Maple2.Model.Common {
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4)]
    public readonly struct Color {
        public readonly byte Blue;
        public readonly byte Green;
        public readonly byte Red;
        public readonly byte Alpha;

        public Color(byte alpha, byte red, byte green, byte blue) {
            this.Alpha = alpha;
            this.Red = red;
            this.Green = green;
            this.Blue = blue;
        }

        public override string ToString() => $"ARGB({Alpha:X2}, {Red:X2}, {Green:X2}, {Blue:X2})";
    }

    [StructLayout(LayoutKind.Sequential, Size = 8)]
    public readonly struct SkinColor {
        public readonly Color Primary;
        public readonly Color Secondary;

        public SkinColor(Color color) {
            this.Primary = color;
            this.Secondary = color;
        }

        public override string ToString() => $"Primary:{Primary}|Secondary:{Secondary}";
    }

    [StructLayout(LayoutKind.Sequential, Size = 20)]
    public readonly struct EquipColor {
        public readonly Color Primary;
        public readonly Color Secondary;
        public readonly Color Tertiary;
        public readonly int Index;
        public readonly int Unknown;

        public EquipColor(Color color) {
            this.Primary = color;
            this.Secondary = color;
            this.Tertiary = color;
            this.Index = -1;
            this.Unknown = 0;
        }

        public EquipColor(Color primary, Color secondary, Color tertiary, int index = -1) {
            this.Primary = primary;
            this.Secondary = secondary;
            this.Tertiary = tertiary;
            this.Index = -1;
            this.Unknown = 0;
        }

        public override string ToString() =>
            $"Primary:{Primary}|Secondary:{Secondary}|Tertiary:{Tertiary}|Index:{Index}";
    }
}
