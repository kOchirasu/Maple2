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

    public GroupChat? GroupChat;

    private readonly CancellationTokenSource tokenSource;

    private readonly ILogger logger = Log.Logger.ForContext<GroupChatManager>();

    public GroupChatManager(WorldClient world, GameSession session) {
        this.session = session;
        this.world = world;
        tokenSource = new CancellationTokenSource();

        GroupChatInfoResponse response = session.World.GroupChatInfo(new GroupChatInfoRequest());
        if (response.GroupChat != null) {
            SetGroupChat(response.GroupChat);
        }
    }

    public void Dispose() {
        session.Dispose();
        tokenSource.Dispose();

        if (GroupChat != null) {
            foreach (GroupChatMember member in GroupChat.Members.Values) {
                member.Dispose();
            }
        }
    }

    public void Load() {
        if (GroupChat == null) {
            return;
        }
        session.Send(GroupChatPacket.Load(GroupChat));
    }

    public bool SetGroupChat(GroupChatInfo info) {
        if (GroupChat != null) {
            return false;
        }

        GroupChatMember[] members = info.Members.Select(member => {
            if (!session.PlayerInfo.GetOrFetch(member.CharacterId, out PlayerInfo? playerInfo)) {
                return null;
            }

            var result = new GroupChatMember {
                Info = playerInfo.Clone(),
            };
            return result;
        }).WhereNotNull().ToArray();

        var groupChat = new GroupChat(info.Id);
        foreach (GroupChatMember member in members) {
            if (groupChat.Members.TryAdd(member.CharacterId, member)) {
                BeginListen(member);
            }
        }

        GroupChat = groupChat;

        session.Send(GroupChatPacket.Load(GroupChat));
        session.Send(GroupChatPacket.Create(GroupChat.Id));
        return true;
    }

    public bool AddMember(GroupChatMember sender, GroupChatMember receiver) {
        if (GroupChat == null) {
            return false;
        }
        if (!GroupChat.Members.TryAdd(receiver.CharacterId, receiver)) {
            return false;
        }

        BeginListen(receiver);
        session.Send(GroupChatPacket.Join(sender.Name, receiver.Name, GroupChat.Id));
        return true;
    }

    public bool RemoveMember(long characterId, bool isSelf) {
        if (GroupChat == null) {
            return false;
        }
        if (!GroupChat.Members.TryRemove(characterId, out GroupChatMember? member)) {
            return false;
        }
        EndListen(member);

        session.Send(GroupChatPacket.LeaveNotice(member.Name, GroupChat.Id));
        return true;
    }


    public bool Disband() {
        if (GroupChat == null) {
            return false;
        }

        foreach (GroupChatMember member in GroupChat.Members.Values) {
            EndListen(member);
        }

        GroupChat = null;
        return true;
    }

    public GroupChatMember? GetMember(string name) {
        return GroupChat?.Members.Values.FirstOrDefault(member => member.Name == name);
    }

    public GroupChatMember? GetMember(long characterId) {
        if (GroupChat?.Members.TryGetValue(characterId, out GroupChatMember? member) == true) {
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

    private bool SyncUpdate(CancellationToken cancel, long characterId, UpdateField type, IPlayerInfo info) {
        if (cancel.IsCancellationRequested || GroupChat == null || !GroupChat.Members.TryGetValue(characterId, out GroupChatMember? member)) {
            return true;
        }

        bool wasOnline = member.Info.Online;
        member.Info.Update(type, info);

        //session.Send(PartyPacket.Update(member));

        if (member.Info.Online != wasOnline) {
            session.Send(member.Info.Online
                ? GroupChatPacket.LoginNotice(member.Name, GroupChat.Id)
                : GroupChatPacket.LogoutNotice(member.Name, GroupChat.Id));
        }
        return false;
    }
    #endregion
}
