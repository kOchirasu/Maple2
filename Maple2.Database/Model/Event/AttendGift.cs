using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Maple2.Model.Enum;

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
    public IList<Item> Days { get; set; }
    
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
            Days = other.Days.Select(day => new Item {
                Day = day.Day,
                ItemRarity = day.ItemRarity,
                ItemId = day.ItemId,
                ItemAmount = day.ItemAmount,
            }).ToArray(),
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
            Days = other.Days.Select(day => new Maple2.Model.Game.Event.AttendGift.Item {
                Day = day.Day,
                ItemRarity = day.ItemRarity,
                ItemId = day.ItemId,
                ItemAmount = day.ItemAmount,
            }).ToArray(),
        };
    }
    
    public class Item {
        public int Day { get; set; }
        public short ItemRarity { get; set; }
        public int ItemId { get; set; }
        public int ItemAmount { get; set; }
        
        [return: NotNullIfNotNull(nameof(other))]
        public static implicit operator Item?(Maple2.Model.Game.Event.AttendGift.Item? other) {
            return other == null ? null : new Item {
                Day = other.Day,
                ItemRarity = other.ItemRarity,
                ItemId = other.ItemId,
                ItemAmount = other.ItemAmount,
            };
        }
        
        [return: NotNullIfNotNull(nameof(other))]
        public static implicit operator Maple2.Model.Game.Event.AttendGift.Item?(Item? other) {
            return other == null ? null : new Maple2.Model.Game.Event.AttendGift.Item {
                Day = other.Day,
                ItemRarity = other.ItemRarity,
                ItemId = other.ItemId,
                ItemAmount = other.ItemAmount,
            };
        }
    }
}
