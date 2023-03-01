using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Maple2.Database.Extensions;
using Maple2.Model.Game;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maple2.Database.Model.Event;

internal class GameEvent {
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime BeginTime { get; set; }
    public DateTime EndTime { get; set; }
    public GameEventInfo EventInfo { get; set; }

    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator GameEvent?(Maple2.Model.Game.Event.GameEvent? other) {
        return other == null ? null : new GameEvent {
            Id = other.Id,
            Name = other.Name,
            BeginTime = other.BeginTime.FromEpochSeconds(),
            EndTime = other.EndTime.FromEpochSeconds(),
            EventInfo = other.EventInfo,
        };
    }

    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator Maple2.Model.Game.Event.GameEvent?(GameEvent? other) {
        return other == null ? null : new Maple2.Model.Game.Event.GameEvent {
            Id = other.Id,
            Name = other.Name,
            BeginTime = other.BeginTime.ToEpochSeconds(),
            EndTime = other.EndTime.ToEpochSeconds(),
            EventInfo = other.EventInfo,
        };
    }

    public static void Configure(EntityTypeBuilder<GameEvent> builder) {
        builder.ToTable("game-event");
        builder.HasKey(@event => @event.Id);
        builder.Property(@event => @event.EventInfo).HasJsonConversion().IsRequired();
    }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "!")]
[JsonDerivedType(typeof(TrafficOptimizer), typeDiscriminator: nameof(TrafficOptimizer))]
[JsonDerivedType(typeof(EventFieldPopup), typeDiscriminator: nameof(EventFieldPopup))]
[JsonDerivedType(typeof(StringBoard), typeDiscriminator: nameof(StringBoard))]
[JsonDerivedType(typeof(StringBoardLink), typeDiscriminator: nameof(StringBoardLink))]
[JsonDerivedType(typeof(MeratMarketNotice), typeDiscriminator: nameof(MeratMarketNotice))]
[JsonDerivedType(typeof(SaleChat), typeDiscriminator: nameof(SaleChat))]
[JsonDerivedType(typeof(BlueMarble), typeDiscriminator: nameof(BlueMarble))]
[JsonDerivedType(typeof(AttendGift), typeDiscriminator: nameof(AttendGift))]
internal abstract class GameEventInfo {
    public int Id { get; set; }
    public string Name { get; set; }

    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator GameEventInfo?(Maple2.Model.Game.Event.GameEventInfo? other) {
        return other switch {
            Maple2.Model.Game.Event.TrafficOptimizer trafficOptimizer => new TrafficOptimizer {
                Id = trafficOptimizer.Id,
                Name = trafficOptimizer.Name,
                GuideObjectSyncInterval = trafficOptimizer.GuideObjectSyncInterval,
                RideSyncInterval = trafficOptimizer.RideSyncInterval,
                UserSyncInterval = trafficOptimizer.UserSyncInterval,
                LinearMovementInterval = trafficOptimizer.LinearMovementInterval,
            },
            Maple2.Model.Game.Event.EventFieldPopup fieldPopup => new EventFieldPopup {
                Id = fieldPopup.Id,
                Name = fieldPopup.Name,
                MapId = fieldPopup.MapId,
            },
            Maple2.Model.Game.Event.StringBoard stringBoard => new StringBoard {
                Id = stringBoard.Id,
                Name = stringBoard.Name,
                StringId = stringBoard.StringId,
                String = stringBoard.String,
            },
            Maple2.Model.Game.Event.StringBoardLink stringBoardLink => new StringBoardLink {
                Id = stringBoardLink.Id,
                Name = stringBoardLink.Name,
                Url = stringBoardLink.Url,
            },
            Maple2.Model.Game.Event.MeratMarketNotice meratMarketNotice => new MeratMarketNotice {
                Id = meratMarketNotice.Id,
                Name = meratMarketNotice.Name,
                Message = meratMarketNotice.Message,
            },
            Maple2.Model.Game.Event.SaleChat saleChat => new SaleChat {
                Id = saleChat.Id,
                Name = saleChat.Name,
                WorldChatDiscount = saleChat.WorldChatDiscount,
                ChannelChatDiscount = saleChat.ChannelChatDiscount,
            },
            Maple2.Model.Game.Event.BlueMarble blueMarble => new BlueMarble {
                Id = blueMarble.Id,
                Name = blueMarble.Name,
                Entries = blueMarble.Entries,
                Tiles = blueMarble.Tiles,
            },
            Maple2.Model.Game.Event.AttendGift attendGift => new AttendGift {
                Id = attendGift.Id,
                Name = attendGift.Name,
                AttendanceName = attendGift.AttendanceName,
                Rewards = new Dictionary<int, RewardItem>(attendGift.Rewards),
                DisableClaimButton = attendGift.DisableClaimButton,
                SkipDayCost = attendGift.SkipDayCost,
                SkipDayCurrencyType = attendGift.SkipDayCurrencyType,
                SkipDaysAllowed = attendGift.SkipDaysAllowed,
                TimeRequired = attendGift.TimeRequired,
                Url = attendGift.Url,
                BeginTime = attendGift.BeginTime,
                EndTime = attendGift.EndTime,
            },
            _ => null,
        };
    }

    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator Maple2.Model.Game.Event.GameEventInfo?(GameEventInfo? other) {
        return other switch {
            TrafficOptimizer trafficOptimizer => new Maple2.Model.Game.Event.TrafficOptimizer {
                Id = trafficOptimizer.Id,
                Name = trafficOptimizer.Name,
                GuideObjectSyncInterval = trafficOptimizer.GuideObjectSyncInterval,
                RideSyncInterval = trafficOptimizer.RideSyncInterval,
                UserSyncInterval = trafficOptimizer.UserSyncInterval,
                LinearMovementInterval = trafficOptimizer.LinearMovementInterval,
            },
            EventFieldPopup fieldPopup => new Maple2.Model.Game.Event.EventFieldPopup {
                Id = fieldPopup.Id,
                Name = fieldPopup.Name,
                MapId = fieldPopup.MapId,
            },
            StringBoard stringBoard => new Maple2.Model.Game.Event.StringBoard {
                Id = stringBoard.Id,
                Name = stringBoard.Name,
                StringId = stringBoard.StringId,
                String = stringBoard.String,
            },
            StringBoardLink stringBoardLink => new Maple2.Model.Game.Event.StringBoardLink {
                Id = stringBoardLink.Id,
                Name = stringBoardLink.Name,
                Url = stringBoardLink.Url,
            },
            MeratMarketNotice meratMarketNotice => new Maple2.Model.Game.Event.MeratMarketNotice {
                Id = meratMarketNotice.Id,
                Name = meratMarketNotice.Name,
                Message = meratMarketNotice.Message,
            },
            SaleChat saleChat => new Maple2.Model.Game.Event.SaleChat {
                Id = saleChat.Id,
                Name = saleChat.Name,
                WorldChatDiscount = saleChat.WorldChatDiscount,
                ChannelChatDiscount = saleChat.ChannelChatDiscount,
            },
            BlueMarble blueMarble => new Maple2.Model.Game.Event.BlueMarble {
                Id = blueMarble.Id,
                Name = blueMarble.Name,
                Entries = blueMarble.Entries,
                Tiles = blueMarble.Tiles,
            },
            AttendGift attendGift => new Maple2.Model.Game.Event.AttendGift {
                Id = attendGift.Id,
                Name = attendGift.Name,
                AttendanceName = attendGift.AttendanceName,
                BeginTime = attendGift.BeginTime,
                EndTime = attendGift.EndTime,
                Rewards = new Dictionary<int, RewardItem>(attendGift.Rewards),
                DisableClaimButton = attendGift.DisableClaimButton,
                SkipDayCost = attendGift.SkipDayCost,
                SkipDayCurrencyType = attendGift.SkipDayCurrencyType,
                SkipDaysAllowed = attendGift.SkipDaysAllowed,
                TimeRequired = attendGift.TimeRequired,
                Url = attendGift.Url,
            },
            _ => null,
        };
    }
}
