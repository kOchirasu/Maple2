using System.Numerics;

namespace Maple2.File.Ingest.Utils;

public static class Vector3ParseUtil {
    public static Vector3 ParseVector3(string vectorString) {
        float[] coords = vectorString.Split(",").Select(float.Parse).ToArray();
        return coords.Length != 3 ? Vector3.Zero : new Vector3(coords[0], coords[1], coords[2]);
    }
}
