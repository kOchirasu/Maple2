using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Maple2.Tools.Extensions;

public static class EnumerableExtensions {
    public static IEnumerable<ImmutableList<T>> Batch<T>(this IEnumerable<T> source, int batchSize) {
        using IEnumerator<T> enumerator = source.GetEnumerator();
        while (enumerator.MoveNext()) {
            yield return YieldBatchElements(enumerator, batchSize - 1).ToImmutableList();
        }
    }

    private static IEnumerable<T> YieldBatchElements<T>(IEnumerator<T> source, int batchSize) {
        yield return source.Current;
        for (int i = 0; i < batchSize && source.MoveNext(); i++) {
            yield return source.Current;
        }
    }

    public static T ElementAtOrDefault<T>(this IList<T> list, int index, T fallback = default) {
        if (list == null) {
            throw new ArgumentNullException(nameof(list));
        }

        if (index >= 0 && index < list.Count) {
            return list[index];
        }

        return fallback;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T AsType<TE, T>(this IEnumerable<TE> enumerable, Func<IEnumerable<TE>, T> function) {
        return function.Invoke(enumerable);
    }

    public static V GetValueOrDefault<K, V>(this IDictionary<K, V> dictionary, K key, V @default = default) {
        return dictionary.TryGetValue(key, out V value) ? value : @default;
    }

    public delegate bool TryFunc<in T, TResult>(T input, out TResult value);

    public static IEnumerable<TResult> TrySelect<T, TResult>(this IEnumerable<T> source, TryFunc<T, TResult> parse) {
        foreach(T element in source) {
            if(parse(element, out TResult value )) {
                yield return value;
            }
        }
    }
}
