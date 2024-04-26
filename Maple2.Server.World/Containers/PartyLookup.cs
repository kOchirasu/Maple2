﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Maple2.Model.Error;
using Maple2.Model.Game;
using Maple2.Model.Game.Party;

namespace Maple2.Server.World.Containers;

public class PartyLookup : IDisposable {
    private readonly ChannelClientLookup channelClients;
    private readonly PlayerInfoLookup playerLookup;

    private readonly ConcurrentDictionary<int, PartyManager> parties;
    private int nextPartyId = 1;

    public PartyLookup(ChannelClientLookup channelClients, PlayerInfoLookup playerLookup) {
        this.channelClients = channelClients;
        this.playerLookup = playerLookup;

        parties = new ConcurrentDictionary<int, PartyManager>();
    }

    public void Dispose() {
        foreach (PartyManager manager in parties.Values) {
            manager.Dispose();
        }
    }

    public bool TryGet(int partyId, [NotNullWhen(true)] out PartyManager? party) {
        return parties.TryGetValue(partyId, out party);
    }

    public bool TryGetByCharacter(long characterId, [NotNullWhen(true)] out PartyManager? party) {
        party = null;
        foreach (PartyManager manager in parties.Values) {
            if (manager.Party.Members.TryGetValue(characterId, out PartyMember? member)) {
                party = manager;
                return true;
            }
        }
        return false;
    }

    public PartyError Create(long leaderId, out int partyId) {
        partyId = nextPartyId++;

        PlayerInfo? leaderInfo = playerLookup.GetPlayerInfo(leaderId);
        if (leaderInfo == null) {
            return PartyError.s_party_err_not_found;
        }

        Party party = new Party(partyId, leaderInfo.AccountId, leaderInfo.CharacterId, leaderInfo.Name);
        PartyManager manager = new PartyManager(party) {
            ChannelClients = channelClients,
        };

        if (!parties.TryAdd(partyId, manager)) {
            return PartyError.s_party_err_not_found;
        }

        return manager.Join(leaderInfo);
    }

    public PartyError Disband(int partyId) {
        if (!TryGet(partyId, out PartyManager? manager)) {
            return PartyError.s_party_err_not_found;
        }

        if (!parties.TryRemove(partyId, out manager)) {
            // Failed to remove party after validating.
            return PartyError.s_party_err_not_found;
        }
        manager.Dispose();

        return PartyError.none;
    }
}
