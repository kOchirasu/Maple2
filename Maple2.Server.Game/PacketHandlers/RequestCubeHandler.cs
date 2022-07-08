using Maple2.Model.Common;
using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Tools.Extensions;

namespace Maple2.Server.Game.PacketHandlers;

public class RequestCubeHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.RequestCube;

    private enum Command : byte {
        BuyPlot = 2,
        ForfeitPlot = 6,
        ExtendPlot = 9,
        PlaceCube = 10,
        RemoveCube = 12,
        RotateCube = 14,
        ReplaceCube = 15,
        LiftupObject = 17,
        LiftupAttack = 18,
        SetHomeName = 21,
        SetPasscode = 24,
        VoteHome = 25,
        SetHomeMessage = 29,
        ClearCubes = 31,
        LoadUnknown = 35,
        IncreaseArea = 37,
        DecreaseArea = 38,
        DesignRankReward = 40,
        EnablePermission = 42,
        SetPermission = 43,
        IncreaseHeight = 44,
        DecreaseHeight = 45,
        SaveHome = 46,
        LoadHome = 47,
        ConfirmLoadHome = 48,
        KickOut = 49,
        SetBackground = 51,
        SetLighting = 52,
        SetCamera = 54,
        SaveBlueprint = 64,
        LoadBlueprint = 65,
    }

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.BuyPlot:
                HandleBuyPlot(session, packet);
                return;
            case Command.ForfeitPlot:
                HandleForfeitPlot(session);
                break;
            case Command.ExtendPlot:
                HandleExtendPlot(session);
                break;
            case Command.PlaceCube:
                HandlePlaceCube(session, packet);
                break;
            case Command.RemoveCube:
                HandleRemoveCube(session, packet);
                break;
            case Command.RotateCube:
                HandleRotateCube(session, packet);
                break;
            case Command.ReplaceCube:
                HandleReplaceCube(session, packet);
                break;
            case Command.LiftupObject:
                HandleLiftupObject(session, packet);
                break;
            case Command.LiftupAttack:
                HandleLiftupAttack(session);
                break;
            case Command.SetHomeName:
                HandleSetHomeName(session, packet);
                break;
            case Command.SetPasscode:
                HandleSetPasscode(session, packet);
                break;
            case Command.VoteHome:
                HandleVoteHome(session);
                break;
            case Command.SetHomeMessage:
                HandleSetHomeMessage(session, packet);
                break;
            case Command.ClearCubes:
                HandleClearCubes(session);
                break;
            case Command.LoadUnknown:
                HandleLoadUnknown(session, packet);
                break;
            case Command.IncreaseArea:
                HandleIncreaseArea(session);
                break;
            case Command.DecreaseArea:
                HandleDecreaseArea(session);
                break;
            case Command.DesignRankReward:
                HandleDesignRankReward(session);
                break;
            case Command.EnablePermission:
                HandleEnablePermission(session, packet);
                break;
            case Command.SetPermission:
                HandleSetPermission(session, packet);
                break;
            case Command.IncreaseHeight:
                HandleIncreaseHeight(session);
                break;
            case Command.DecreaseHeight:
                HandleDecreaseHeight(session);
                break;
            case Command.SaveHome:
                HandleSaveHome(session, packet);
                break;
            case Command.LoadHome:
                HandleLoadHome(session, packet);
                break;
            case Command.ConfirmLoadHome:
                HandleConfirmLoadHome(session, packet);
                break;
            case Command.KickOut:
                HandleKickOut(session);
                break;
            case Command.SetBackground:
                HandleSetBackground(session, packet);
                break;
            case Command.SetLighting:
                HandleSetLighting(session, packet);
                break;
            case Command.SetCamera:
                HandleSetCamera(session, packet);
                break;
            case Command.SaveBlueprint:
                HandleSaveBlueprint(session, packet);
                break;
            case Command.LoadBlueprint:
                HandleLoadBlueprint(session, packet);
                break;
        }
    }

    private void HandleBuyPlot(GameSession session, IByteReader packet) {
        int plotNumber = packet.ReadInt();
        packet.ReadInt(); // ApartmentNumber?

        if (session.Housing.BuyPlot(plotNumber)) {
            session.Send(CubePacket.ConfirmBuyPlot());
        }
    }

    private void HandleForfeitPlot(GameSession session) {
        PlotInfo? plot = session.Housing.ForfeitPlot();
        if (plot != null) {
            session.Send(CubePacket.ConfirmForfeitPlot(plot));
        }
    }

    private void HandleExtendPlot(GameSession session) {
        session.Housing.ExtendPlot();
    }

    private void HandlePlaceCube(GameSession session, IByteReader packet) {
        var position = packet.Read<Vector3B>();
        var cubeItem = packet.ReadClass<UgcItemCube>();
        float rotation = packet.ReadFloat();
    }

    private void HandleRemoveCube(GameSession session, IByteReader packet) {
        var position = packet.Read<Vector3B>();
    }

    private void HandleRotateCube(GameSession session, IByteReader packet) {
        var position = packet.Read<Vector3B>();
        bool clockwise = packet.ReadBool();
    }

    private void HandleReplaceCube(GameSession session, IByteReader packet) {
        var position = packet.Read<Vector3B>();
        var cubeItem = packet.ReadClass<UgcItemCube>();
        float rotation = packet.ReadFloat();
    }

    private void HandleLiftupObject(GameSession session, IByteReader packet) {
        var position = packet.Read<Vector3B>();
    }

    private void HandleLiftupAttack(GameSession session) { }

    private void HandleSetHomeName(GameSession session, IByteReader packet) {
        string name = packet.ReadUnicodeString();
        session.Housing.SetName(name);
    }

    private void HandleSetPasscode(GameSession session, IByteReader packet) {
        bool hasPasscode = packet.ReadBool();
        string passcode = string.Empty;
        if (hasPasscode) {
            passcode = packet.ReadUnicodeString();
        }

        session.Housing.SetPasscode(passcode);
    }

    private void HandleVoteHome(GameSession session) { }

    private void HandleSetHomeMessage(GameSession session, IByteReader packet) {
        string message = packet.ReadUnicodeString();
        session.Housing.SetHomeMessage(message);
    }

    private void HandleClearCubes(GameSession session) { }

    private void HandleLoadUnknown(GameSession session, IByteReader packet) {
        int slot = packet.ReadInt();
    }

    private void HandleIncreaseArea(GameSession session) {
        int area = session.Player.Value.Home.Area + 1;
        if (session.Player.Value.Home.SetArea(area) && session.Housing.SaveHome()) {
            session.Field?.Multicast(CubePacket.IncreaseArea((byte) area));
        }
    }

    private void HandleDecreaseArea(GameSession session) {
        int area = session.Player.Value.Home.Area - 1;
        if (session.Player.Value.Home.SetArea(area) && session.Housing.SaveHome()) {
            session.Field?.Multicast(CubePacket.DecreaseArea((byte) area));
        }
    }

    private void HandleDesignRankReward(GameSession session) { }

    private void HandleEnablePermission(GameSession session, IByteReader packet) {
        var permission = packet.Read<HomePermission>();
        bool enabled = packet.ReadBool();

        if (enabled) {
            session.Player.Value.Home.Permissions[permission] = HomePermissionSetting.None;
        } else {
            session.Player.Value.Home.Permissions.Remove(permission);
        }

        session.Field?.Multicast(CubePacket.EnablePermission(permission, enabled));
    }

    private void HandleSetPermission(GameSession session, IByteReader packet) {
        var permission = packet.Read<HomePermission>();
        var setting = packet.Read<HomePermissionSetting>();

        if (session.Player.Value.Home.Permissions.ContainsKey(permission)) {
            session.Player.Value.Home.Permissions[permission] = setting;
        } else {
            setting = HomePermissionSetting.None;
        }

        session.Field?.Multicast(CubePacket.SetPermission(permission, setting));
    }

    private void HandleIncreaseHeight(GameSession session) {
        int height = session.Player.Value.Home.Height + 1;
        if (session.Player.Value.Home.SetHeight(height) && session.Housing.SaveHome()) {
            session.Field?.Multicast(CubePacket.IncreaseHeight((byte) height));
        }
    }

    private void HandleDecreaseHeight(GameSession session) {
        int height = session.Player.Value.Home.Height - 1;
        if (session.Player.Value.Home.SetHeight(height) && session.Housing.SaveHome()) {
            session.Field?.Multicast(CubePacket.DecreaseHeight((byte) height));
        }
    }

    private void HandleSaveHome(GameSession session, IByteReader packet) {
        int slot = packet.ReadInt();
        string name = packet.ReadUnicodeString();
    }

    private void HandleLoadHome(GameSession session, IByteReader packet) {
        int slot = packet.ReadInt();
    }

    private void HandleConfirmLoadHome(GameSession session, IByteReader packet) {
        int slot = packet.ReadInt();
    }

    private void HandleKickOut(GameSession session) { }

    private void HandleSetBackground(GameSession session, IByteReader packet) {
        var background = packet.Read<HomeBackground>();
        if (session.Player.Value.Home.SetBackground(background)) {
            session.Field?.Multicast(CubePacket.SetBackground(background));
        }
    }

    private void HandleSetLighting(GameSession session, IByteReader packet) {
        var lighting = packet.Read<HomeLighting>();
        if (session.Player.Value.Home.SetLighting(lighting)) {
            session.Field?.Multicast(CubePacket.SetLighting(lighting));
        }
    }

    private void HandleSetCamera(GameSession session, IByteReader packet) {
        var camera = packet.Read<HomeCamera>();
        if (session.Player.Value.Home.SetCamera(camera)) {
            session.Field?.Multicast(CubePacket.SetCamera(camera));
        }
    }

    private void HandleSaveBlueprint(GameSession session, IByteReader packet) {
        int slot = packet.ReadInt();
        string name = packet.ReadUnicodeString();
    }

    private void HandleLoadBlueprint(GameSession session, IByteReader packet) {
        int slot = packet.ReadInt();
    }
}
