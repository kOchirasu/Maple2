using System;
using System.Collections.Generic;
using System.Numerics;

namespace Maple2.Tools.Extensions;

public static class VectorExtensions {
    public const int BLOCK_SIZE = 150;

    public static Vector3 Align(this in Vector3 position, float interval = BLOCK_SIZE) {
        return new Vector3(
            MathF.Round(position.X / interval) * interval,
            MathF.Round(position.Y / interval) * interval,
            MathF.Floor(position.Z / interval) * interval
        );
    }

    public static Vector3 AlignRotation(this in Vector3 position, float interval = 90f) {
        return new Vector3(
            MathF.Round(position.X / interval) * interval,
            MathF.Round(position.Y / interval) * interval,
            MathF.Round(position.Z / interval) * interval
        );
    }

    public static Vector3 Offset(this in Vector3 position, float distance, in Vector3 rotation) {
        return position + new Vector3(0, distance, 0).Rotate(rotation);
    }

    public static Vector3 Offset(this in Vector3 position, in Vector3 offset, in Vector3 rotation) {
        return position + offset.Rotate(rotation);
    }

    // Rotates a vector by specified euler angles.
    public static Vector3 Rotate(this in Vector3 magnitude, in Vector3 eulerAngle) {
        // Extra MathF.PI on X/Z are because Y-axis is mirrored.
        var rotation = Quaternion.CreateFromYawPitchRoll(
            MathF.PI - eulerAngle.X * MathF.PI / 180,
            eulerAngle.Y * MathF.PI / 180,
            MathF.PI - eulerAngle.Z * MathF.PI / 180
        );

        return Vector3.Transform(magnitude, rotation);
    }

    // Computes the rotation angle from src(x, y) to dst(x, y).
    public static float Angle2D(this in Vector3 src, in Vector3 dst) {
        return (float) Math.Atan2(dst.Y - src.Y, dst.X - src.X) * (180 / MathF.PI) + 90f;
    }

    // Clamps the length of a vector between min and max.
    public static Vector3 ClampLength(this in Vector3 length, float min, float max) {
        float magnitude = length.Length();
        if (magnitude < min) {
            return min * Vector3.Normalize(length);
        }
        if (magnitude > max) {
            return max * Vector3.Normalize(length);
        }

        return length;
    }

    // Returns all blocks within range in a 2D grid around position
    // position should be aligned here
    public static IEnumerable<Vector3> BlocksInRange(this Vector3 position, int range) {
        int xBase = (int) position.X / BLOCK_SIZE;
        int yBase = (int) position.Y / BLOCK_SIZE;
        int xMin = xBase - range;
        int xMax = xBase + range;
        for (int x = xMin; x <= xMax; x++) {
            int xMag = Math.Abs(x - xBase); // X-Magnitude
            int yMin = yBase - range + xMag;
            int yMax = yBase + range - xMag;
            for (int y = yMin; y <= yMax; y++) {
                yield return new Vector3(x * BLOCK_SIZE, y * BLOCK_SIZE, position.Z);
            }
        }
    }
}
