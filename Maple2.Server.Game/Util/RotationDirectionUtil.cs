using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Xml;

namespace Maple2.Server.Game.Util;

public static class RotationDirectionUtil {
    public const int SOUTH_EAST = 0;
    public const int NORTH_EAST = 90;
    public const int NORTH_WEST = 180;
    public const int SOUTH_WEST = 270;

    public static int GetClosestDirection(Vector3 rotation)
    {
        int[] directions = new int[4]
        {
            SOUTH_EAST, NORTH_EAST, NORTH_WEST, SOUTH_WEST
        };

        return directions.MinBy(x => Math.Abs(x - rotation.Z));
    }
}
