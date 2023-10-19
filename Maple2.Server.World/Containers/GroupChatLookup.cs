using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Game.GroupChat;
using Maple2.Model.Game.Party;
using Serilog;
using Serilog.Core;

namespace Maple2.Server.World.Containers;

public class GroupChatLookup : IDisposable {
    private readonly ChannelClientLookup channelClients;
    private readonly PlayerInfoLookup playerLookup;

    private readonly ConcurrentDictionary<int, GroupChatManager> groupChats;
    private int nextGroupChatId = 1;

    public GroupChatLookup(ChannelClientLookup channelClients, PlayerInfoLookup playerLookup) {
        this.channelClients = channelClients;
        this.playerLookup = playerLookup;

        groupChats = new ConcurrentDictionary<int, GroupChatManager>();
    }

    public void Dispose() {
        foreach (GroupChatManager manager in groupChats.Values) {
            manager.Dispose();
        }
    }

    public bool TryGet(int groupChatId, [NotNullWhen(true)] out GroupChatManager? groupChat) {
        return groupChats.TryGetValue(groupChatId, out groupChat);
    }

    /*public bool TryGetByCharacter(long characterId, [NotNullWhen(true)] out PartyManager? party) {
        party = null;
        foreach (PartyManager manager in parties.Values) {
            if (manager.Party.Members.TryGetValue(characterId, out PartyMember? member)) {
                party = manager;
                return true;
            }
        }
        return false;
    }*/

    public GroupChatError Create(long requesterId, out int groupChatId) {
        groupChatId = nextGroupChatId++;

        PlayerInfo? requesterInfo = playerLookup.GetPlayerInfo(requesterId);
        if (requesterInfo == null) {
            return GroupChatError.s_err_groupchat_null_target_user;
        }

        var groupChat = new GroupChat(groupChatId);
        var manager = new GroupChatManager(groupChat) {
            ChannelClients = channelClients,
        };

        if (!groupChats.TryAdd(groupChatId, manager)) {
            return GroupChatError.s_err_groupchat_null_target_user;
        }

        return manager.Join(requesterInfo);
    }

    public void Disband(int groupChatId) {
        if (!TryGet(groupChatId, out GroupChatManager? manager)) {
            Log.Error("Failed to disband group chat {GroupChatId} because it was not found", groupChatId);
            return;
        }

        if (!groupChats.TryRemove(groupChatId, out manager)) {
            Log.Error("Unable to remove group chat {GroupChatId} in World", groupChatId);
            return;
        }
        manager.Dispose();
    }
}
