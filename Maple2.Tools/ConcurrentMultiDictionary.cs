using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Maple2.Tools;

public class ConcurrentMultiDictionary<TK1, TK2, TV> where TK1 : notnull where TK2 : notnull {
    private readonly ConcurrentDictionary<TK1, TV> data;
    private readonly ConcurrentDictionary<TK2, TK1> mapping;

    public ConcurrentMultiDictionary() {
        data = new ConcurrentDictionary<TK1, TV>();
        mapping = new ConcurrentDictionary<TK2, TK1>();
    }

    public int Count => data.Count;

    public TV this[TK1 key] => data[key];
    public TV this[TK2 key] => data[mapping[key]];

    public ICollection<TK1> Keys1 => data.Keys;
    public ICollection<TK2> Keys2 => mapping.Keys;
    public ICollection<TV> Values => data.Values;

    public bool TryAdd(TK1 key1, TK2 key2, TV value) {
        if (data.ContainsKey(key1) || mapping.ContainsKey(key2)) {
            return false;
        }

        return data.TryAdd(key1, value) && mapping.TryAdd(key2, key1);
    }

    public bool TryGet(TK1 key, [NotNullWhen(true)] out TV? value) {
        return data.TryGetValue(key, out value);
    }

    public bool TryGet(TK2 key2, [NotNullWhen(true)] out TV? value) {
        if (mapping.TryGetValue(key2, out TK1? key1)) {
            return data.TryGetValue(key1, out value);
        }

        value = default(TV);
        return false;
    }
}
