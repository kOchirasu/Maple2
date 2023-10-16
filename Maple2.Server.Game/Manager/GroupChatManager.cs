using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Game.GroupChat;
using Maple2.Model.Game.Party;
using Maple2.Server.Core.Sync;
using Maple2.Server.Game.Packets;
using Maple2.Server.Game.Session;
using Maple2.Server.Game.Util.Sync;
using Maple2.Server.World.Service;
using Maple2.Tools.Extensions;
using Mono.Unix.Native;
using Serilog;
using WorldClient = Maple2.Server.World.Service.World.WorldClient;

namespace Maple2.Server.Game.Manager;

public class GroupChatManager : IDisposable {
    private readonly GameSession session;
    private readonly WorldClient world;

    public GroupChat GroupChat;

    private readonly CancellationTokenSource tokenSource;

    private readonly ILogger logger = Log.Logger.ForContext<GroupChatManager>();

    public GroupChatManager(WorldClient world, GameSession session) {
        this.session = session;
        this.world = world;
        tokenSource = new CancellationTokenSource();

        GroupChatInfoResponse response = session.World.GroupChatInfo(new GroupChatInfoResponse {
            CharacterId = session.CharacterId,
        });
        if (response.Party != null) {
            SetGroupChat(response.Party);
        }
    }

    public void Dispose() {
        session.Dispose();
        tokenSource.Dispose();

        foreach((int groupChatId, GroupChat groupChat) in GroupChats) {
            foreach((long characterId, GroupChatMember member) in groupChat.Members) {
                member.Dispose();
            }
        }
    }

    public void Load() {
        foreach((int groupChatId, GroupChat groupChat) in GroupChats) {
            session.Send(GroupChatPacket.Load(groupChat));
        }
    }

    public bool SetGroupChat(PartyInfo info) {
        if
        return true;
    }

    public void RemoveParty() {
        if (Party == null) {
            return;
        }

        Party = null;
        session.Player.Value.Character.PartyId = 0;
    }

    public bool AddMember(PartyMember member) {
        if (Party == null) {
            return false;
        }
        if (!Party.Members.TryAdd(member.CharacterId, member)) {
            return false;
        }

        BeginListen(member);
        session.Send(PartyPacket.Joined(member));
        return true;
    }

    public bool RemoveMember(long characterId, bool isKick, bool isSelf) {
        if (Party == null) {
            return false;
        }
        if (!Party.Members.TryRemove(characterId, out PartyMember? member)) {
            return false;
        }
        EndListen(member);

        session.Send(isKick ? PartyPacket.Kicked(member.CharacterId) : PartyPacket.Leave(member.CharacterId, isSelf));
        return true;
    }

    public bool UpdateLeader(long newLeaderCharacterId) {
        if (Party == null) {
            return false;
        }

        PartyMember? leader = Party.Members.Values.SingleOrDefault(member => member.CharacterId == newLeaderCharacterId);
        if (leader == null) {
            logger.Error("Party {PartyId} does not have a valid leader", Party.Id);
            session.Send(PartyPacket.Error(PartyError.s_party_err_not_found));
            return false;
        }

        Party.LeaderCharacterId = leader.CharacterId;
        Party.LeaderAccountId = leader.Info.AccountId;
        Party.LeaderName = leader.Info.Name;

        session.Send(PartyPacket.NotifyUpdateLeader(newLeaderCharacterId));
        return true;
    }

    public bool Disband() {
        if (Party == null) {
            return false;
        }

        foreach (PartyMember member in Party.Members.Values) {
            EndListen(member);
        }

        session.Send(PartyPacket.Disband());
        RemoveParty();
        return true;
    }

    public GroupChatMember? GetMember(string name) {
        return Party?.Members.Values.FirstOrDefault(member => member.Name == name);
    }

    public PartyMember? GetMember(long characterId) {
        if (Party?.Members.TryGetValue(characterId, out PartyMember? member) == true) {
            return member;
        }

        return null;
    }

    #region PlayerInfo Events
    private void BeginListen(GroupChatMember member) {
        // Clean up previous token if necessary
        if (member.TokenSource != null) {
            logger.Warning("BeginListen called on Member {Id} that was already listening", member.CharacterId);
            EndListen(member);
        }

        member.TokenSource = CancellationTokenSource.CreateLinkedTokenSource(tokenSource.Token);
        CancellationToken token = member.TokenSource.Token;
        var listener = new PlayerInfoListener(UpdateField.Party, (type, info) => SyncUpdate(token, member.CharacterId, type, info));
        session.PlayerInfo.Listen(member.Info.CharacterId, listener);
    }

    private void EndListen(GroupChatMember member) {
        member.TokenSource?.Cancel();
        member.TokenSource?.Dispose();
        member.TokenSource = null;
    }

    private bool SyncUpdate(CancellationToken cancel, int groupChatId, long characterId, UpdateField type, IPlayerInfo info) {
        if (cancel.IsCancellationRequested || !GroupChats.TryGetValue(groupChatId, out GroupChat? groupChat) || !groupChat.Members.TryGetValue(characterId, out GroupChatMember? member)) {
            return true;
        }

        bool wasOnline = member.Info.Online;
        member.Info.Update(type, info);

        //session.Send(PartyPacket.Update(member));

        if (member.Info.Online != wasOnline) {
            session.Send(member.Info.Online
                ? GroupChatPacket.LoginNotice(member.Name, groupChatId)
                : GroupChatPacket.LogoutNotice(member.Name, groupChatId));
        }
        return false;
    }
    #endregion
}
