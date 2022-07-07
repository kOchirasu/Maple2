using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;

namespace Maple2.Server.Game.Packets;

public static class CubePacket {
    private enum Command : byte {
        Unknown0 = 0,
        HoldCube = 1,
        BuyPlot = 2,
        ConfirmBuyPlot = 4,
        ForfeitPlot = 5,
        ConfirmForfeitPlot = 7,
        ExtendPlot = 9,
        // 9, 24, 40, 41 = SetPassword?
        PlaceCube = 10, // Also PurchaseCube?
        RemoveCube = 12,
        RotateCube = 14,
        ReplaceCube = 15,
        PickupLiftable = 17,
        AttackLiftable = 18,
        LoadHome = 20,
        SetHomeName = 21,
        UpdatePlot = 22, // Buy/Extend/Forfeit
        ConfirmVote = 25,
        KickOut = 26,
        // 27
        ArchitectScore = 28,
        HomeGreeting = 29,
        // 32
        // 33
        ReturnMap = 34,
        // 36
        IncreaseArea = 37,
        DecreaseArea = 38,
        DesignRankReward = 39,
        EnablePermission = 42,
        SetPermission = 43,
        IncreaseHeight = 44,
        DecreaseHeight = 45,
        SaveHome = 46,
        // 50
        ChangeBackground = 51,
        ChangeLighting = 52,
        GrantBuildPermissions = 53,
        ChangeCamera = 54,
        // 55
        UpdateBuildBudget = 56,
        AddBuildPermission = 57,
        RemoveBuildPermission = 58,
        LoadBuildPermission = 59,
        // Blueprint stuff:
        // 60, 61, 62, 63, 64, 67, 69
        FunctionCubeError = 71,
    }

    // A lot of ops contain this logic, we are just reusing Command.BuyPlot
    public static ByteWriter Error(UgcMapError error) {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.BuyPlot);
        pWriter.Write<UgcMapError>(error);

        return pWriter;
    }

    public static ByteWriter BuyPlot(Plot plot, string plotName) {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.BuyPlot);
        pWriter.Write<UgcMapError>(UgcMapError.s_ugcmap_ok);
        pWriter.WriteInt(plot.Number);
        pWriter.WriteInt(plot.ApartmentNumber);
        pWriter.WriteUnicodeString(plotName);
        pWriter.WriteLong(plot.ExpiryTime);
        pWriter.WriteLong(plot.OwnerId);

        return pWriter;
    }

    public static ByteWriter ConfirmBuyPlot() {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.ConfirmBuyPlot);

        return pWriter;
    }

    public static ByteWriter ForfeitPlot(Plot plot) {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.ForfeitPlot);
        pWriter.Write<UgcMapError>(UgcMapError.s_ugcmap_ok);
        pWriter.WriteInt(plot.Number);
        pWriter.WriteInt(plot.ApartmentNumber);
        pWriter.WriteBool(false); // This might be state?

        return pWriter;
    }

    public static ByteWriter ConfirmForfeitPlot(Plot plot) {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.ConfirmForfeitPlot);
        pWriter.Write<UgcMapError>(UgcMapError.s_ugcmap_ok);
        pWriter.WriteShort();
        pWriter.WriteInt(plot.MapId);
        pWriter.WriteInt(plot.Number);

        return pWriter;
    }

    public static ByteWriter ExtendPlot(Plot plot) {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.ExtendPlot);
        pWriter.Write<UgcMapError>(UgcMapError.s_ugcmap_ok);
        pWriter.WriteLong(plot.OwnerId);

        return pWriter;
    }

    public static ByteWriter SetHomeName(Home home) {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.SetHomeName);
        pWriter.Write<UgcMapError>(UgcMapError.s_ugcmap_ok);
        pWriter.WriteBool(false);
        pWriter.WriteLong(home.AccountId);
        pWriter.WriteInt(home.PlotNumber);
        pWriter.WriteInt(home.ApartmentNumber);
        pWriter.WriteUnicodeString(home.Name);

        return pWriter;
    }

    public static ByteWriter UpdateHomeProfile(Player player, bool load = false) {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.LoadHome);
        pWriter.WriteInt(player.ObjectId);
        pWriter.WriteInt(player.Home.MapId);
        pWriter.WriteInt(player.Home.PlotMapId);
        pWriter.WriteInt(player.Home.PlotNumber);
        pWriter.WriteInt(player.Home.ApartmentNumber);
        pWriter.WriteUnicodeString(player.Home.Name);
        pWriter.WriteLong(player.Home.Plot?.ExpiryTime ?? 0);
        pWriter.WriteLong(player.Home.Plot?.LastModified ?? 0);
        pWriter.WriteBool(load);

        return pWriter;
    }

    public static ByteWriter UpdatePlot(Plot plot) {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.UpdatePlot);
        pWriter.WriteInt(plot.Number);
        pWriter.WriteInt(plot.ApartmentNumber);
        pWriter.Write<PlotState>(plot.State);
        pWriter.WriteLong(plot.ExpiryTime);

        return pWriter;
    }

    public static ByteWriter ReturnMap(int mapId) {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.ReturnMap);
        pWriter.WriteInt(mapId);

        return pWriter;
    }
}
