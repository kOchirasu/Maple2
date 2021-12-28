using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Maple2.Model.Common;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4)]
public readonly record struct Vector3B(sbyte X, sbyte Y, sbyte Z);


[StructLayout(LayoutKind.Sequential, Pack = 2, Size = 6)]
public readonly record struct Vector3S(short X, short Y, short Z) {
    public static implicit operator Vector3S(Vector3 vector) {
        return new Vector3S(
            (short) MathF.Round(vector.X),
            (short) MathF.Round(vector.Y),
            (short) MathF.Round(vector.Z)
        );
    }

    public static implicit operator Vector3(Vector3S vector) {
        return new Vector3(vector.X, vector.Y, vector.Z);
    }

    public static Vector3S operator +(in Vector3S a, in Vector3S b) =>
        new Vector3S((short) (a.X + b.X), (short) (a.Y + b.Y), (short) (a.Z + b.Z));
    public static Vector3S operator -(in Vector3S a, in Vector3S b) =>
        new Vector3S((short) (a.X - b.X), (short) (a.Y - b.Y), (short) (a.Z - b.Z));
    public static Vector3S operator *(in Vector3S a, in Vector3S b) =>
        new Vector3S((short) (a.X * b.X), (short) (a.Y * b.Y), (short) (a.Z * b.Z));
    public static Vector3S operator /(in Vector3S a, in Vector3S b) =>
        new Vector3S((short) (a.X / b.X), (short) (a.Y / b.Y), (short) (a.Z / b.Z));

    public override string ToString() => $"<{X}, {Y}, {Z}>";
}