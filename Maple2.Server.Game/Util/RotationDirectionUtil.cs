using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Xml;

namespace Maple2.Server.Game.Util;

public static class RotationDirectionUtil {
    public const float SOUTH_EAST = 0;
    public const float NORTH_EAST = 90;
    public const float NORTH_WEST = 180;
    public const float SOUTH_WEST = 270;

    public static float GetClosestDirection(Vector3 rotation)
    {
        float[] directions = new float[4]
        {
            SOUTH_EAST, NORTH_EAST, NORTH_WEST, SOUTH_WEST
        };

        return directions.MinBy(x => Math.Abs(x - rotation.Z));
    }
}
