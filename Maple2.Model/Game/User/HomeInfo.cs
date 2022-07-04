using System;
using System.Collections.Generic;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game;

public class HomeInfo : IByteSerializable {
    private const byte HOME_PERMISSION_COUNT = 9;

    public int MapId { get; private set; }
    public int PlotMapId { get; private set; }
    public int PlotId { get; private set; }
    public int ApartmentNumber { get; private set; }
    public PlotState State { get; private set; }
    public int WeeklyArchitectScore { get; private set; }
    public int ArchitectScore { get; private set; }

    public byte Area { get; private set; }
    public byte Height { get; private set; }

    // Interior Settings
    public byte Background { get; private set; }
    public byte Lighting { get; private set; }
    public byte Camera { get; private set; }

    public string Name { get; private set; }
    public string Greeting { get; private set; }
    public long ExpiryTime { get; private set; }
    public long UpdateTime { get; private set; }

    public readonly IDictionary<HomePermission, HomePermissionSetting> Permissions;

    public HomeInfo(int mapId, int plotMapId, int plotId, int apartmentNumber, PlotState state, int weeklyArchitectScore, int architectScore, byte area,
                    byte height, byte background, byte lighting, byte camera, string? name, string? greeting, long expiryTime, long updateTime,
                    IDictionary<HomePermission, HomePermissionSetting>? permissions) {
        MapId = mapId != 0 ? mapId : Constant.DefaultHomeMapId;
        PlotMapId = plotMapId;
        PlotId = plotId;
        ApartmentNumber = apartmentNumber;
        State = state;
        WeeklyArchitectScore = weeklyArchitectScore;
        ArchitectScore = architectScore;
        Area = Math.Clamp(area, Constant.MinHomeArea, Constant.MaxHomeArea);
        Height = Math.Clamp(height, Constant.MinHomeHeight, Constant.MaxHomeHeight);
        Background = background;
        Lighting = lighting;
        Camera = camera;
        Name = name ?? "Unknown";
        Greeting = greeting ?? "Thanks for visiting. Come back soon!";
        ExpiryTime = expiryTime != 0 ? expiryTime : DateTimeOffset.MaxValue.ToUnixTimeSeconds();
        UpdateTime = updateTime != 0 ? updateTime : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Permissions = permissions ?? new Dictionary<HomePermission, HomePermissionSetting>();
    }

    public void WriteTo(IByteWriter writer) {
        writer.WriteUnicodeString(Name);
        writer.WriteUnicodeString(Greeting);
        writer.WriteByte();
        writer.WriteInt(WeeklyArchitectScore);
        writer.WriteInt(ArchitectScore);
        writer.WriteInt(PlotId);
        writer.WriteInt(PlotMapId);
        writer.Write<PlotState>(State);
        writer.WriteByte(Area);
        writer.WriteByte(Height);
        writer.WriteByte(Background);
        writer.WriteByte(Lighting);
        writer.WriteByte(Camera);

        writer.WriteByte(HOME_PERMISSION_COUNT);
        for (byte i = 0; i < HOME_PERMISSION_COUNT; i++) {
            bool enabled = Permissions.TryGetValue((HomePermission) i, out HomePermissionSetting setting);
            writer.WriteBool(enabled);
            if (enabled) {
                writer.Write<HomePermissionSetting>(setting);
            }
        }
    }
}
