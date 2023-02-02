using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Maple2.Database.Extensions;
using Maple2.Model.Metadata;
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
            EventInfo = other.EventInfo switch {
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
            },
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
            _ => null,
        };
    }
    
    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator Maple2.Model.Game.Event.GameEventInfo?(GameEventInfo? other) {
        
        // switch expression of GameEventInfo type
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
            _ => null,
        };
    }
}

// TODO: Move this over to its own separate cs file

internal class TrafficOptimizer : GameEventInfo {
    public int GuideObjectSyncInterval { get; set; }
    public int RideSyncInterval { get; set; }
    public int LinearMovementInterval { get; set; }
    public int UserSyncInterval { get; set; }
    
    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator TrafficOptimizer?(Maple2.Model.Game.Event.TrafficOptimizer? other) {
        return other == null ? null : new TrafficOptimizer {
            Id = other.Id,
            Name = other.Name,
            GuideObjectSyncInterval = other.GuideObjectSyncInterval,
            RideSyncInterval = other.RideSyncInterval,
            LinearMovementInterval = other.LinearMovementInterval,
            UserSyncInterval = other.UserSyncInterval,
        };
    }
    
    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator Maple2.Model.Game.Event.TrafficOptimizer?(TrafficOptimizer? other) {
        return other == null ? null : new Maple2.Model.Game.Event.TrafficOptimizer {
            Id = other.Id,
            Name = other.Name,
            GuideObjectSyncInterval = other.GuideObjectSyncInterval,
            RideSyncInterval = other.RideSyncInterval,
            LinearMovementInterval = other.LinearMovementInterval,
            UserSyncInterval = other.UserSyncInterval,
        };
    }
}

internal class EventFieldPopup : GameEventInfo {
    public int MapId { get; set; }
    
    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator EventFieldPopup?(Maple2.Model.Game.Event.EventFieldPopup? other) {
        return other == null ? null : new EventFieldPopup {
            Id = other.Id,
            Name = other.Name,
            MapId = other.MapId,
        };
    }
    
    [return: NotNullIfNotNull(nameof(other))]
    public static implicit operator Maple2.Model.Game.Event.EventFieldPopup?(EventFieldPopup? other) {
        return other == null ? null : new Maple2.Model.Game.Event.EventFieldPopup {
            Id = other.Id,
            Name = other.Name,
            MapId = other.MapId,
        };
    }
}
