using System.Runtime.InteropServices;

namespace Maple2.Model.Common {
    [ProtoContract, StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4)]
    public readonly struct Vector3B {
        public readonly sbyte X;
        public readonly sbyte Y;
        public readonly sbyte Z;
        private readonly sbyte Zero;

        public Vector3B(sbyte x, sbyte y, sbyte z) {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.Zero = 0;
        }

        public override string ToString() => $"CoordB({X}, {Y}, {Z})";
    }
}
