using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Maple2.Server.World.Containers;

public class PlayerChannelLookup : IEnumerable<KeyValuePair<long, int>> {
    private readonly ConcurrentDictionary<long, int> lookup;

    public PlayerChannelLookup() {
        lookup = new ConcurrentDictionary<long, int>();
    }

    public int Count => lookup.Count;

    public int this[long characterId] {
        set {
            if (characterId == 0) {
                return;
            }

            lookup[characterId] = value;
        }
    }

    public bool Lookup(long characterId, out int channel) => lookup.TryGetValue(characterId, out channel);

    public bool Remove(long characterId) => lookup.TryRemove(characterId, out _);

    public bool Remove(long characterId, out int channel) => lookup.TryRemove(characterId, out channel);

    public IEnumerator<KeyValuePair<long, int>> GetEnumerator() => lookup.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => lookup.GetEnumerator();
}
