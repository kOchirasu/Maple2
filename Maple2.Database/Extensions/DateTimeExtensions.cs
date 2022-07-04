using System;

namespace Maple2.Database.Extensions;

public static class DateTimeExtensions {
    public static long ToEpochSeconds(this DateTime dateTime) {
        if (dateTime <= DateTime.UnixEpoch) {
            return DateTime.UnixEpoch.Second;
        }

        return (long) (dateTime - DateTime.UnixEpoch).TotalSeconds;
    }

    public static DateTime FromEpochSeconds(this long epochSeconds) {
        return DateTime.UnixEpoch + TimeSpan.FromSeconds(epochSeconds);
    }
}
