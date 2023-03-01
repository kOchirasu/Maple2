using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Maple2.Model.Enum;
using Maple2.Model.Game;

namespace Maple2.Database.Model.Event;

internal class AttendGift : GameEventInfo {
    public string AttendanceName { get; set; }
    public string Url { get; set; }
    public long BeginTime { get; set; }
    public long EndTime { get; set; }
    public bool DisableClaimButton { get; set; }
    public int TimeRequired { get; set; }
    public AttendGiftCurrencyType SkipDayCurrencyType { get; set; }
    public int SkipDaysAllowed { get; set; }
    public long SkipDayCost { get; set; }
    public IDictionary<int, RewardItem> Rewards { get; set; }

    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator AttendGift?(Maple2.Model.Game.Event.AttendGift? other) {
        return other == null ? null : new AttendGift {
            Id = other.Id,
            AttendanceName = other.AttendanceName,
            Url = other.Url,
            BeginTime = other.BeginTime,
            EndTime = other.EndTime,
            DisableClaimButton = other.DisableClaimButton,
            TimeRequired = other.TimeRequired,
            SkipDayCurrencyType = other.SkipDayCurrencyType,
            SkipDaysAllowed = other.SkipDaysAllowed,
            SkipDayCost = other.SkipDayCost,
            Rewards = new Dictionary<int, RewardItem>(other.Rewards),
        };
    }

    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator Maple2.Model.Game.Event.AttendGift?(AttendGift? other) {
        return other == null ? null : new Maple2.Model.Game.Event.AttendGift {
            Id = other.Id,
            AttendanceName = other.AttendanceName,
            Url = other.Url,
            BeginTime = other.BeginTime,
            EndTime = other.EndTime,
            DisableClaimButton = other.DisableClaimButton,
            TimeRequired = other.TimeRequired,
            SkipDayCurrencyType = other.SkipDayCurrencyType,
            SkipDaysAllowed = other.SkipDaysAllowed,
            SkipDayCost = other.SkipDayCost,
            Rewards = new Dictionary<int, RewardItem>(other.Rewards),
        };
    }
}
