using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Grpc.Core;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Game.GroupChat;
using Maple2.Model.Game.Party;
using Maple2.Model.Metadata;
using Maple2.Server.Channel.Service;
using Serilog;
using Serilog.Core;
using ChannelClient = Maple2.Server.Channel.Service.Channel.ChannelClient;

namespace Maple2.Server.World.Containers;

public class GroupChatManager : IDisposable {
    public required ChannelClientLookup ChannelClients { get; init; }
    public readonly GroupChat GroupChat;

    public GroupChatManager(GroupChat groupChat) {
        GroupChat = groupChat;
    }

    public void Dispose() {
        // there really isn't a disband for this ?
        /*Broadcast(new GroupChatRequest {
            Disband = new PartyRequest.Types.Disband { },
        });*/
    }

    public void Broadcast(GroupChatRequest request) {
        if (request.GroupChatId > 0 && request.GroupChatId != GroupChat.Id) {
            throw new InvalidOperationException($"Broadcasting {request.GroupChatCase} for incorrect group chat: {request.GroupChatId} => {GroupChat.Id}");
        }

        request.GroupChatId = GroupChat.Id;
        foreach (IGrouping<short, GroupChatMember> group in GroupChat.Members.Values.GroupBy(member => member.Info.Channel)) {
            if (!ChannelClients.TryGetClient(group.Key, out ChannelClient? client)) {
                continue;
            }

            request.ReceiverIds.Clear();
            request.ReceiverIds.AddRange(group.Select(member => member.Info.CharacterId));

            try {
                client.GroupChat(request);
            } catch { }
        }
    }

    public bool CheckForDisband() {
        if (GroupChat.Members.Count <= 1) {
            Dispose();
            return true;
        }
        return false;
    }

    public GroupChatError Invite(long requestorId, PlayerInfo player) {
        if (!GroupChat.Members.TryGetValue(requestorId, out GroupChatMember? requestor)) {
            return GroupChatError.s_err_groupchat_null_target_user;
        }
        if (GroupChat.Members.Count >= Constant.GroupChatMaxCapacity) {
            return GroupChatError.s_err_groupchat_maxjoin;
        }
        if (GroupChat.Members.ContainsKey(player.CharacterId)) {
            return GroupChatError.s_err_groupchat_add_member_target;
        }
        if (!ChannelClients.TryGetClient(player.Channel, out ChannelClient? client)) {
            return GroupChatError.s_err_groupchat_add_member_target;
        }

        try {
            var request = new GroupChatRequest {
                GroupChatId = GroupChat.Id,
                ReceiverIds = { player.CharacterId },
                Invite = new GroupChatRequest.Types.Invite {
                    SenderName = requestor.Name,
                },
            };

            GroupChatResponse response = client.GroupChat(request);
            return (GroupChatError) response.Error;
        } catch (RpcException) {
            return GroupChatError.s_err_groupchat_null_target_user;
        }
    }

    public GroupChatError Join(PlayerInfo info) {
        if (GroupChat.Members.Count >= Constant.GroupChatMaxCapacity) {
            return GroupChatError.s_err_groupchat_maxjoin;
        }
        if (GroupChat.Members.ContainsKey(info.CharacterId)) {
            return GroupChatError.s_err_groupchat_add_member_target;
        }

        GroupChatMember member = new GroupChatMember {
            Info = info.Clone(),
        };

        Broadcast(new GroupChatRequest {
            AddMember = new GroupChatRequest.Types.AddMember {
                CharacterId = member.CharacterId,
            },
        });

        GroupChat.Members.TryAdd(info.CharacterId, member);
        return GroupChatError.none;
    }

    public void Leave(long characterId) {
        if (!GroupChat.Members.TryGetValue(characterId, out GroupChatMember? member)) {
            Log.Error("Failed to remove member {CharacterId} from group chat {GroupChatId} because they were not found", characterId, GroupChat.Id);
            return;
        }

        if (CheckForDisband()) {
            return;
        }

        /*Broadcast(new GroupChatRequest {
            RemoveMember = new GroupChatRequest.Types.RemoveMember {
                CharacterId = member.CharacterId,
            },
        });*/
        GroupChat.Members.TryRemove(member.CharacterId, out _);
    }
}
