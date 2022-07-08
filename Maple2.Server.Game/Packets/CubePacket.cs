using Maple2.Model.Common;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.Packets;
using Maple2.Server.Game.Session;
using Maple2.Tools.Extensions;

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
        LiftupObject = 17,
        LiftupAttack = 18,
        UpdateProfile = 20,
        SetHomeName = 21,
        UpdatePlot = 22, // Buy/Extend/Forfeit
        SetPassword = 24,
        ConfirmVote = 25,
        KickOut = 26,
        // 27
        ArchitectScore = 28,
        SetHomeMessage = 29,
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
        SetBackground = 51,
        SetLighting = 52,
        GrantBuildPermissions = 53,
        SetCamera = 54,
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

    public static ByteWriter HoldCube(GameSession session) {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.HoldCube);
        pWriter.WriteInt(session.Player.ObjectId);
        pWriter.WriteClass<UgcItemCube>(session.HeldCube);

        return pWriter;
    }

    public static ByteWriter BuyPlot(PlotInfo plot, string plotName) {
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

    public static ByteWriter ForfeitPlot(PlotInfo plot) {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.ForfeitPlot);
        pWriter.Write<UgcMapError>(UgcMapError.s_ugcmap_ok);
        pWriter.WriteInt(plot.Number);
        pWriter.WriteInt(plot.ApartmentNumber);
        pWriter.WriteBool(false); // This might be state?

        return pWriter;
    }

    public static ByteWriter ConfirmForfeitPlot(PlotInfo plot) {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.ConfirmForfeitPlot);
        pWriter.Write<UgcMapError>(UgcMapError.s_ugcmap_ok);
        pWriter.WriteShort();
        pWriter.WriteInt(plot.MapId);
        pWriter.WriteInt(plot.Number);

        return pWriter;
    }

    public static ByteWriter ExtendPlot(PlotInfo plot) {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.ExtendPlot);
        pWriter.Write<UgcMapError>(UgcMapError.s_ugcmap_ok);
        pWriter.WriteLong(plot.OwnerId);

        return pWriter;
    }

    public static ByteWriter PlaceCube(PlotInfo plot, in Vector3B position, float rotation, UgcItemCube cube) {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.PlaceCube);
        pWriter.Write<UgcMapError>(UgcMapError.s_ugcmap_ok);
        pWriter.WriteInt();
        pWriter.WriteInt();
        pWriter.WriteInt(plot.Number);
        pWriter.WriteInt(plot.ApartmentNumber);
        pWriter.Write<Vector3B>(position);
        pWriter.WriteLong(cube.Uid); // Some uid
        pWriter.WriteClass<UgcItemCube>(cube);
        pWriter.WriteBool(false); // Unknown
        pWriter.WriteFloat(rotation);
        pWriter.WriteInt(); // Unknown
        pWriter.WriteBool(false);

        return pWriter;
    }

    public static ByteWriter RemoveCube(in Vector3B position) {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.RemoveCube);
        pWriter.Write<UgcMapError>(UgcMapError.s_ugcmap_ok);
        pWriter.WriteInt();
        pWriter.WriteInt();
        pWriter.Write<Vector3B>(position);
        pWriter.WriteBool(false);

        return pWriter;
    }

    public static ByteWriter RotateCube(in Vector3B position, float rotation) {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.RotateCube);
        pWriter.Write<UgcMapError>(UgcMapError.s_ugcmap_ok);
        pWriter.WriteInt();
        pWriter.WriteInt();
        pWriter.Write<Vector3B>(position);
        pWriter.WriteFloat(rotation);

        return pWriter;
    }

    public static ByteWriter ReplaceCube(in Vector3B position, float rotation, UgcItemCube cube) {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.ReplaceCube);
        pWriter.Write<UgcMapError>(UgcMapError.s_ugcmap_ok);
        pWriter.WriteInt();
        pWriter.WriteInt();
        pWriter.Write<Vector3B>(position);
        pWriter.WriteLong(cube.Uid); // Some uid
        pWriter.WriteClass<UgcItemCube>(cube);
        pWriter.WriteBool(false); // Unknown
        pWriter.WriteFloat(rotation);
        pWriter.WriteInt(); // Unknown

        return pWriter;
    }

    public static ByteWriter LiftupObject() {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.LiftupObject);

        return pWriter;
    }

    public static ByteWriter LiftupAttack() {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.LiftupAttack);

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

    public static ByteWriter UpdateProfile(Player player, bool load = false) {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.UpdateProfile);
        pWriter.WriteInt(player.ObjectId);
        pWriter.WriteInt(player.Home.MapId);
        pWriter.WriteInt(player.Home.PlotMapId);
        pWriter.WriteInt(player.Home.PlotNumber);
        pWriter.WriteInt(player.Home.ApartmentNumber);
        pWriter.WriteUnicodeString(player.Home.Name);
        pWriter.WriteLong(player.Home.Outdoor?.ExpiryTime ?? 0);
        pWriter.WriteLong(player.Home.Outdoor?.LastModified ?? 0);
        pWriter.WriteBool(load);

        return pWriter;
    }

    public static ByteWriter UpdatePlot(PlotInfo plot) {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.UpdatePlot);
        pWriter.WriteInt(plot.Number);
        pWriter.WriteInt(plot.ApartmentNumber);
        pWriter.Write<PlotState>(plot.State);
        pWriter.WriteLong(plot.ExpiryTime);

        return pWriter;
    }
    public static ByteWriter SetPassword() {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.SetPassword);

        return pWriter;
    }

    public static ByteWriter ConfirmVote() {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.ConfirmVote);

        return pWriter;
    }

    public static ByteWriter KickOut() {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.KickOut);

        return pWriter;
    }

    public static ByteWriter ArchitectScore() {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.ArchitectScore);

        return pWriter;
    }

    public static ByteWriter SetHomeMessage() {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.SetHomeMessage);

        return pWriter;
    }

    public static ByteWriter ReturnMap(int mapId) {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.ReturnMap);
        pWriter.WriteInt(mapId);

        return pWriter;
    }

    public static ByteWriter IncreaseArea() {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.IncreaseArea);

        return pWriter;
    }

    public static ByteWriter DecreaseArea() {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.DecreaseArea);

        return pWriter;
    }

    public static ByteWriter DesignRankReward() {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.DesignRankReward);

        return pWriter;
    }

    public static ByteWriter EnablePermission() {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.EnablePermission);

        return pWriter;
    }

    public static ByteWriter SetPermission() {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.SetPermission);

        return pWriter;
    }

    public static ByteWriter IncreaseHeight() {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.IncreaseHeight);

        return pWriter;
    }

    public static ByteWriter DecreaseHeight() {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.DecreaseHeight);

        return pWriter;
    }

    public static ByteWriter SaveHome() {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.SaveHome);

        return pWriter;
    }

    public static ByteWriter SetBackground() {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.SetBackground);

        return pWriter;
    }

    public static ByteWriter SetLighting() {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.SetLighting);

        return pWriter;
    }

    public static ByteWriter GrantBuildPermissions() {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.GrantBuildPermissions);

        return pWriter;
    }

    public static ByteWriter SetCamera() {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.SetCamera);

        return pWriter;
    }

    public static ByteWriter UpdateBuildBudget() {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.UpdateBuildBudget);

        return pWriter;
    }

    public static ByteWriter AddBuildPermission() {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.AddBuildPermission);

        return pWriter;
    }

    public static ByteWriter RemoveBuildPermission() {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.RemoveBuildPermission);

        return pWriter;
    }

    public static ByteWriter LoadBuildPermission() {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.LoadBuildPermission);

        return pWriter;
    }

    public static ByteWriter FunctionCubeError(FunctionCubeError error) {
        var pWriter = Packet.Of(SendOp.ResponseCube);
        pWriter.Write<Command>(Command.FunctionCubeError);
        pWriter.Write<FunctionCubeError>(error);

        return pWriter;
    }
}
