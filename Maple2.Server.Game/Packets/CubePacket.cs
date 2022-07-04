using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.Packets;

public static class CubePacket {
    private enum Command : byte {
        Unknown0 = 0,
        HoldCube = 1,
        BuyPlot = 2,
        ConfirmBuyPlot = 4,
        ForfeitPlot = 5,
        ConfirmForfeitPlot = 6,
        ExtendPlot = 9,
        // 9, 24, 40, 41 = SetPassword?
        PlaceCube = 10, // Also PurchaseCube?
        RemoveCube = 12,
        RotateCube = 14,
        ReplaceCube = 15,
        PickupLiftable = 17,
        AttackLiftable = 18,
        LoadHome = 20,
        SetPlotName = 21,
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

    public static ByteWriter LoadHome(Player player, bool load = false) {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.LoadHome);
        pWriter.WriteInt(player.ObjectId);
        pWriter.WriteInt(player.Account.Home.MapId);
        pWriter.WriteInt(player.Account.Home.PlotMapId);
        pWriter.WriteInt(player.Account.Home.PlotId);
        pWriter.WriteInt(player.Account.Home.ApartmentNumber);
        pWriter.WriteUnicodeString(player.Account.Home.Name);
        pWriter.WriteLong(player.Account.Home.ExpiryTime);
        pWriter.WriteLong(player.Account.Home.UpdateTime);
        pWriter.WriteBool(load);

        return pWriter;
    }

    public static ByteWriter ReturnMap(int mapId) {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.ReturnMap);
        pWriter.WriteInt(mapId);

        return pWriter;
    }
}
