using Maple2.Model.Enum;
using Maple2.Model.Game;
using Maple2.PacketLib.Tools;
using Maple2.Server.Core.Constants;
using Maple2.Server.Core.PacketHandlers;
using Maple2.Server.Game.Session;
using Maple2.Tools.Extensions;
using WorldClient = Maple2.Server.World.Service.World.WorldClient;

namespace Maple2.Server.Game.PacketHandlers;

public class GuildHandler : PacketHandler<GameSession> {
    public override RecvOp OpCode => RecvOp.Guild;

    private enum Command : byte {
        Create = 1,
        Disband = 2,
        Invite = 3,
        RespondInvite = 4,
        Leave = 7,
        Expel = 8,
        UpdateMemberRank = 10,
        UpdateMemberMessage = 13,
        CheckIn = 15,
        UpdateLeader = 61,
        UpdateNotice = 62,
        UpdateEmblem = 63,
        IncreaseCapacity = 64,
        UpdateRank = 65,
        UpdateFocus = 66,
        SendMail = 69,
        SendApplication = 80,
        CancelApplication = 81,
        RespondApplication = 82,
        Unknown83 = 83,
        ListApplications = 84,
        SearchGuilds = 85,
        SearchGuildName = 86,
        UseBuff = 88,
        UsePersonalBuff = 89,
        UpgradeBuff = 90,
        StartArcade = 96,
        EnterArcade = 97,
        UpgradeHouseRank = 98,
        UpgradeHouseTheme = 99,
        EnterHouse = 100,
        SendGift = 106,
        UpdateGiftLog = 109,
        Donate = 110,
        UpgradeNpc = 111,
        CreateGuildEvent = 112,
        StartGuildEvent = 113,
        JoinGuildEvent = 117,
    }

    #region Autofac Autowired
    // ReSharper disable MemberCanBePrivate.Global
    public required WorldClient World { private get; init; }
    // ReSharper restore All
    #endregion

    public override void Handle(GameSession session, IByteReader packet) {
        var command = packet.Read<Command>();
        switch (command) {
            case Command.Create:
            HandleCreate(session, packet);
            return;
        case Command.Disband:
            HandleDisband(session);
            return;
        case Command.Invite:
            HandleInvite(session, packet);
            return;
        case Command.RespondInvite:
            HandleRespondInvite(session, packet);
            return;
        case Command.Leave:
            HandleLeave(session);
            return;
        case Command.Expel:
            HandleExpel(session, packet);
            return;
        case Command.UpdateMemberRank:
            HandleUpdateMemberRank(session, packet);
            return;
        case Command.UpdateMemberMessage:
            HandleUpdateMemberMessage(session, packet);
            return;
        case Command.CheckIn:
            HandleCheckIn(session);
            return;
        case Command.UpdateLeader:
            HandleUpdateLeader(session, packet);
            return;
        case Command.UpdateNotice:
            HandleUpdateNotice(session, packet);
            return;
        case Command.UpdateEmblem:
            HandleUpdateEmblem(session, packet);
            return;
        case Command.IncreaseCapacity:
            HandleIncreaseCapacity(session);
            return;
        case Command.UpdateRank:
            HandleUpdateRank(session, packet);
            return;
        case Command.UpdateFocus:
            HandleUpdateFocus(session, packet);
            return;
        case Command.SendMail:
            HandleSendMail(session, packet);
            return;
        case Command.SendApplication:
            HandleSendApplication(session, packet);
            return;
        case Command.CancelApplication:
            HandleCancelApplication(session, packet);
            return;
        case Command.RespondApplication:
            HandleRespondApplication(session, packet);
            return;
        case Command.ListApplications:
            HandleListApplications(session);
            return;
        case Command.SearchGuilds:
            HandleSearchGuilds(session, packet);
            return;
        case Command.SearchGuildName:
            HandleSearchGuildName(session, packet);
            return;
        case Command.UseBuff:
            HandleUseBuff(session, packet);
            return;
        case Command.UsePersonalBuff:
            HandleUsePersonalBuff(session, packet);
            return;
        case Command.UpgradeBuff:
            HandleUpgradeBuff(session, packet);
            return;
        case Command.StartArcade:
            HandleStartArcade(session, packet);
            return;
        case Command.EnterArcade:
            HandleEnterArcade(session, packet);
            return;
        case Command.UpgradeHouseRank:
            HandleUpgradeHouseRank(session, packet);
            return;
        case Command.UpgradeHouseTheme:
            HandleUpgradeHouseTheme(session, packet);
            return;
        case Command.EnterHouse:
            HandleEnterHouse(session);
            return;
        case Command.SendGift:
            HandleSendGift(session, packet);
            return;
        case Command.UpdateGiftLog:
            HandleUpdateGiftLog(session);
            return;
        case Command.Donate:
            HandleDonate(session, packet);
            return;
        case Command.UpgradeNpc:
            HandleUpgradeNpc(session, packet);
            return;
        case Command.CreateGuildEvent:
            HandleCreateGuildEvent(session);
            return;
        case Command.StartGuildEvent:
            HandleStartGuildEvent(session);
            return;
        case Command.JoinGuildEvent:
            HandleJoinGuildEvent(session);
            return;
        }
    }

    private void HandleCreate(GameSession session, IByteReader packet) {
        string guildName = packet.ReadUnicodeString();
    }

    private void HandleDisband(GameSession session) {

    }

    private void HandleInvite(GameSession session, IByteReader packet) {
        string playerName = packet.ReadUnicodeString();
    }

    private void HandleRespondInvite(GameSession session, IByteReader packet) {
        var invite = packet.ReadClass<GuildInvite>();
        bool accepted = packet.ReadBool();
    }

    private void HandleLeave(GameSession session) {

    }

    private void HandleExpel(GameSession session, IByteReader packet) {
        string playerName = packet.ReadUnicodeString();
    }

    private void HandleUpdateMemberRank(GameSession session, IByteReader packet) {
        string playerName = packet.ReadUnicodeString();
        byte rankId = packet.ReadByte();
    }

    private void HandleUpdateMemberMessage(GameSession session, IByteReader packet) {
        string message = packet.ReadUnicodeString();
    }

    private void HandleCheckIn(GameSession session) {

    }

    private void HandleUpdateLeader(GameSession session, IByteReader packet) {
        string leaderName = packet.ReadUnicodeString();
    }

    private void HandleUpdateNotice(GameSession session, IByteReader packet) {
        packet.ReadBool();
        string notice = packet.ReadUnicodeString();
    }

    private void HandleUpdateEmblem(GameSession session, IByteReader packet) {
        string emblem = packet.ReadUnicodeString();
    }

    private void HandleIncreaseCapacity(GameSession session) {

    }

    private void HandleUpdateRank(GameSession session, IByteReader packet) {
        packet.ReadByte();
        var rank = packet.ReadClass<GuildRank>();
    }

    private void HandleUpdateFocus(GameSession session, IByteReader packet) {
        packet.ReadByte();
        var focus = packet.Read<GuildFocus>();
    }

    private void HandleSendMail(GameSession session, IByteReader packet) {
        string title = packet.ReadUnicodeString();
        string content = packet.ReadUnicodeString();
    }

    private void HandleSendApplication(GameSession session, IByteReader packet) {
        long guildId = packet.ReadLong();
    }

    private void HandleCancelApplication(GameSession session, IByteReader packet) {
        long applicationId = packet.ReadLong();
    }

    private void HandleRespondApplication(GameSession session, IByteReader packet) {
        long applicationId = packet.ReadLong();
        bool accepted = packet.ReadBool();
    }

    private void HandleListApplications(GameSession session) {

    }

    private void HandleSearchGuilds(GameSession session, IByteReader packet) {
        var focus = packet.Read<GuildFocus>();
        packet.ReadInt(); // 1
    }

    private void HandleSearchGuildName(GameSession session, IByteReader packet) {
        string guildName = packet.ReadUnicodeString();
    }

    private void HandleUseBuff(GameSession session, IByteReader packet) {
        int buffId = packet.ReadInt();
    }

    private void HandleUsePersonalBuff(GameSession session, IByteReader packet) {
        int buffId = packet.ReadInt();
    }

    private void HandleUpgradeBuff(GameSession session, IByteReader packet) {
        int buffId = packet.ReadInt();
    }

    private void HandleStartArcade(GameSession session, IByteReader packet) {
        int index = packet.ReadInt();
    }

    private void HandleEnterArcade(GameSession session, IByteReader packet) {
        int index = packet.ReadInt();
    }

    private void HandleUpgradeHouseRank(GameSession session, IByteReader packet) {
        int rank = packet.ReadInt();
    }

    private void HandleUpgradeHouseTheme(GameSession session, IByteReader packet) {
        int theme = packet.ReadInt();
    }

    private void HandleEnterHouse(GameSession session) {

    }

    private void HandleSendGift(GameSession session, IByteReader packet) {
        var gift = packet.Read<RewardItem>();
        string playerName = packet.ReadUnicodeString();
    }

    private void HandleUpdateGiftLog(GameSession session) {

    }

    private void HandleDonate(GameSession session, IByteReader packet) {
        int count = packet.ReadInt();
    }

    private void HandleUpgradeNpc(GameSession session, IByteReader packet) {
        var type = packet.Read<GuildNpcType>();
    }

    private void HandleCreateGuildEvent(GameSession session) {

    }

    private void HandleStartGuildEvent(GameSession session) {

    }

    private void HandleJoinGuildEvent(GameSession session) {

    }
}
