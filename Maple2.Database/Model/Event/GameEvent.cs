using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Maple2.Database.Extensions;
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
            Maple2.Model.Game.Event.TrafficOptimizer trafficOptimizer => (TrafficOptimizer) trafficOptimizer,
            Maple2.Model.Game.Event.EventFieldPopup fieldPopup => (EventFieldPopup) fieldPopup,
            Maple2.Model.Game.Event.StringBoard stringBoard => (StringBoard) stringBoard,
            Maple2.Model.Game.Event.StringBoardLink stringBoardLink => (StringBoardLink) stringBoardLink,
            Maple2.Model.Game.Event.MeratMarketNotice meratMarketNotice => (MeratMarketNotice) meratMarketNotice,
            Maple2.Model.Game.Event.SaleChat saleChat => (SaleChat) saleChat,
            Maple2.Model.Game.Event.BlueMarble blueMarble => (BlueMarble) blueMarble,
            Maple2.Model.Game.Event.AttendGift attendGift => (AttendGift) attendGift,
            _ => null,
        };
    }

    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator Maple2.Model.Game.Event.GameEventInfo?(GameEventInfo? other) {
        return other switch {
            TrafficOptimizer trafficOptimizer => (Maple2.Model.Game.Event.TrafficOptimizer) trafficOptimizer,
            EventFieldPopup fieldPopup => (Maple2.Model.Game.Event.EventFieldPopup) fieldPopup,
            StringBoard stringBoard => (Maple2.Model.Game.Event.StringBoard) stringBoard,
            StringBoardLink stringBoardLink => (Maple2.Model.Game.Event.StringBoardLink) stringBoardLink,
            MeratMarketNotice meratMarketNotice => (Maple2.Model.Game.Event.MeratMarketNotice) meratMarketNotice,
            SaleChat saleChat => (Maple2.Model.Game.Event.SaleChat) saleChat,
            BlueMarble blueMarble => (Maple2.Model.Game.Event.BlueMarble) blueMarble,
            AttendGift attendGift => (Maple2.Model.Game.Event.AttendGift) attendGift,
            _ => null,
        };
    }
}
