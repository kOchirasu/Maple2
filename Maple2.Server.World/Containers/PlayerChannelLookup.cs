using System.Collections.Concurrent;
using Serilog;

namespace Maple2.Server.World.Containers;

public class PlayerChannelLookup {
    private readonly record struct Entry(long AccountId, long CharacterId);

    private readonly ConcurrentDictionary<Entry, int> lookup;
    private readonly ConcurrentDictionary<long, Entry> accountIndex;
    private readonly ConcurrentDictionary<long, Entry> characterIndex;

    public PlayerChannelLookup() {
        lookup = new ConcurrentDictionary<Entry, int>();
        accountIndex = new ConcurrentDictionary<long, Entry>();
        characterIndex = new ConcurrentDictionary<long, Entry>();
    }

    public int Count => lookup.Count;

    public void Add(long accountId, long characterId, int channel) {
        if (accountId == 0) {
            return;
        }

        if (lookup.TryRemove(new Entry(accountId, 0), out _)) {
            accountIndex.TryRemove(accountId, out _);
        }

        var entry = new Entry(accountId, characterId);
        lookup[entry] = channel;
        accountIndex[accountId] = entry;

        // We only index characterId if specified
        if (characterId != 0) {
            characterIndex[characterId] = entry;
        }
    }

    public bool LookupAccount(long accountId, out long characterId, out int channel) {
        if (accountIndex.TryGetValue(accountId, out Entry entry)) {
            characterId = entry.CharacterId;
            return lookup.TryGetValue(entry, out channel);
        }

        characterId = 0;
        channel = 0;
        return false;
    }

    public bool LookupCharacter(long characterId, out long accountId, out int channel) {
        if (characterIndex.TryGetValue(characterId, out Entry entry)) {
            accountId = entry.AccountId;
            return lookup.TryGetValue(entry, out channel);
        }

        accountId = 0;
        channel = 0;
        return false;
    }

    public bool Remove(long accountId, long characterId) {
        if (accountId != 0 && characterId != 0) {
            Log.Error("Removing with invalid key: AccountId:{AccountId}, CharacterId:{CharacterId}", accountId, characterId);
            return false;
        }

        if (lookup.TryRemove(new Entry(accountId, characterId), out _)) {
            accountIndex.TryRemove(accountId, out _);
            characterIndex.TryRemove(characterId, out _);
            return true;
        }

        return false;
    }

    public bool RemoveByCharacter(long characterId) {
        return RemoveByCharacter(characterId, out _);
    }

    public bool RemoveByCharacter(long characterId, out int channel) {
        channel = 0;
        if (characterIndex.TryRemove(characterId, out Entry entry)) {
            return accountIndex.TryRemove(entry.AccountId, out _) && lookup.TryRemove(entry, out channel);
        }

        return false;
    }

    public bool RemoveByAccount(long accountId) {
        return RemoveByAccount(accountId, out _);
    }

    public bool RemoveByAccount(long accountId, out int channel) {
        if (accountIndex.TryRemove(accountId, out Entry entry)) {
            if (entry.CharacterId != 0) {
                characterIndex.TryRemove(entry.CharacterId, out _);
            }
            return lookup.TryRemove(entry, out channel);
        }

        channel = 0;
        return false;
    }
}
