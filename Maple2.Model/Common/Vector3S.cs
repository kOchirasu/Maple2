using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Maple2.Model.Common; 

[ProtoContract, StructLayout(LayoutKind.Sequential, Pack = 2, Size = 6)]
public readonly struct Vector3S {
    public readonly short X;
    public readonly short Y;
    public readonly short Z;

    public Vector3S(short x, short y, short z) {
        this.X = x;
        this.Y = y;
        this.Z = z;
    }

    public static implicit operator Vector3S(Vector3 vector) {
        return new Vector3S((short) vector.X, (short) vector.Y, (short) vector.Z);
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

public class ProtoContractAttribute : Attribute { }