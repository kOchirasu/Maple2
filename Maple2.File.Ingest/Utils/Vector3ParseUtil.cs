using System.Numerics;

namespace Maple2.File.Ingest.Utils;

public static class Vector3ParseUtil {
    public static Vector3 ParseVector3(string vectorString) {
        // split string and int parse
        var coords = vectorString.Split(",").Select(float.Parse).ToArray();
        if (coords.Length != 3) {
            return Vector3.Zero;
        }

        return new Vector3(coords[0], coords[1], coords[2]);
    }
}
