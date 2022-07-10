using System.Runtime.InteropServices;

namespace Maple2.Model.Common;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 3)]
public readonly record struct Byte3(byte X, byte Y, byte Z);
