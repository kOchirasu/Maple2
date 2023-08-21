using System.Collections.Concurrent;

namespace Maple2.Server.Game.Util;

public static class NestedDictionaryUtil {
    public static void Remove<TKey, TValue>(
        ConcurrentDictionary<TKey, ConcurrentDictionary<int, TValue>> nestedDictionary,
        int id)
        where TKey : notnull {
        foreach (ConcurrentDictionary<int, TValue> innerDictionary in nestedDictionary.Values) {
            innerDictionary.TryRemove(id, out _);
        }
    }
}
