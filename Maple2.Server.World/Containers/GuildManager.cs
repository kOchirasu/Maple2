using System;
using Maple2.Database.Storage;
using Maple2.Model.Enum;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Tools.Extensions;
using ChannelClient = Maple2.Server.Channel.Service.Channel.ChannelClient;

namespace Maple2.Server.World.Containers;

public class GuildManager : IDisposable {
    public required GameStorage GameStorage { get; init; }
    public required ChannelClientLookup ChannelClients { get; init; }

    public readonly Guild Guild;

    public GuildManager(Guild guild) {
        Guild = guild;
    }

    public void Dispose() {
        using GameStorage.Request db = GameStorage.Context();
        db.SaveGuild(Guild);
    }

    public GuildError Invite(long requestorId, PlayerInfo player) {
        if (!Guild.Members.TryGetValue(requestorId, out GuildMember? requestor)) {
            return GuildError.s_guild_err_null_member;
        }
        GuildRank? rank = Guild.Ranks.ElementAtOrDefault(requestor.Rank);
        if (rank == null || (rank.Permission & GuildPermission.InviteMembers) == default) {
            return GuildError.s_guild_err_no_authority;
        }
        if (Guild.Members.Count >= Guild.Capacity) {
            return GuildError.s_guild_err_full_member;
        }
        if (Guild.Members.ContainsKey(player.CharacterId)) {
            return GuildError.s_guild_err_already_exist;
        }
        if (!ChannelClients.TryGetClient(player.Channel, out ChannelClient? client)) {
            return GuildError.s_guild_err_wait_inviting;
        }

        return GuildError.s_guild_err_unknown;
    }

    public GuildError Join(PlayerInfo info) {


        return GuildError.none;
    }

    public GuildError Expel(long requestorId, long characterId) {
        if (!Guild.Members.TryGetValue(requestorId, out GuildMember? requestor)) {
            return GuildError.s_guild_err_null_member;
        }
        GuildRank? rank = Guild.Ranks.ElementAtOrDefault(requestor.Rank);
        if (rank == null || (rank.Permission & GuildPermission.InviteMembers) == default) {
            return GuildError.s_guild_err_no_authority;
        }
        if (characterId == Guild.LeaderCharacterId) {
            return GuildError.s_guild_err_expel_target_master;
        }
        if (!Guild.Members.TryGetValue(characterId, out GuildMember? member)) {
            return GuildError.s_guild_err_not_join_member;
        }
        if (member.Rank < requestor.Rank) {
            return GuildError.s_guild_err_no_authority;
        }

        using GameStorage.Request db = GameStorage.Context();
        if (!db.DeleteGuildMember(Guild.Id, characterId)) {
            return GuildError.s_guild_err_unknown;
        }

        Guild.Members.TryRemove(characterId, out _);
        return GuildError.none;
    }

    public GuildError Leave(long characterId) {
        if (characterId == Guild.LeaderCharacterId) {
            return GuildError.s_guild_err_cannot_leave_master;
        }
        if (!Guild.Members.TryGetValue(characterId, out GuildMember? member)) {
            return GuildError.s_guild_err_not_join_member;
        }

        using GameStorage.Request db = GameStorage.Context();
        if (!db.DeleteGuildMember(Guild.Id, characterId)) {
            return GuildError.s_guild_err_unknown;
        }

        Guild.Members.TryRemove(characterId, out _);
        return GuildError.none;
    }
}
