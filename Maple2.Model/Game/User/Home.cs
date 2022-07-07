using System;
using System.Collections.Generic;
using Maple2.Model.Common;
using Maple2.Model.Enum;
using Maple2.Model.Metadata;
using Maple2.PacketLib.Tools;
using Maple2.Tools;

namespace Maple2.Model.Game;

public class Home : IByteSerializable {
    private const byte HOME_PERMISSION_COUNT = 9;

    public long AccountId { get; init; }
    public int MapId { get; init; }
    public int Number { get; init; }
    public string Name { get; set; }
    public string Message { get; set; }
    public byte Area { get; private set; }
    public byte Height { get; private set; }

    public int CurrentArchitectScore { get; set; }
    public int ArchitectScore { get; set; }

    // Interior Settings
    public byte Background { get; set; }
    public byte Lighting { get; set; }
    public byte Camera { get; set; }
    public string? Password { get; set; }
    public readonly IDictionary<HomePermission, HomePermissionSetting> Permissions;

    public IDictionary<Vector3B, (UgcItemCube Cube, float Rotation)> Cubes
        = new Dictionary<Vector3B, (UgcItemCube Cube, float Rotation)>();


    public Plot? Plot { get; set; }

    public int PlotMapId => Plot?.MapId ?? 0;
    public int PlotNumber => Plot?.Number ?? 0;
    public int ApartmentNumber => Plot?.ApartmentNumber ?? 0;
    public PlotState State => Plot?.State ?? PlotState.Open;

    public Home() {
        Name = "Unknown";
        Message = "Thanks for visiting. Come back soon!";
        Permissions = new Dictionary<HomePermission, HomePermissionSetting>();
    }

    public byte SetArea(byte area) {
        Area = Math.Clamp(area, Constant.MinHomeArea, Constant.MaxHomeArea);
        return area;
    }

    public byte SetHeight(byte height) {
        Height = Math.Clamp(height, Constant.MinHomeHeight, Constant.MaxHomeHeight);
        return height;
    }

    public void WriteTo(IByteWriter writer) {
        writer.WriteLong(AccountId);
        writer.WriteUnicodeString(Name);
        writer.WriteUnicodeString(Message);
        writer.WriteByte();
        writer.WriteInt(CurrentArchitectScore);
        writer.WriteInt(ArchitectScore);
        writer.WriteInt(PlotNumber);
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
