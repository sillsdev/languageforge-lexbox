using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace LexCore.Utils;

/// <summary>
/// a simple (small) dictionary that uses weak references for the values.
/// Whenever you try to get a value out of the dictionary, it will check if the weak reference is still alive.
/// If it is, it will return the value. If it isn't, it will remove the kvp from the dictionary and return false.
/// It will also cleanup dead weak references when you try to get a value out of the dictionary or when you add a new kvp.
/// </summary>
public class ConcurrentWeakDictionary<TKey, TValue>
    where TKey : class
    where TValue : class
{
    private readonly ConcurrentDictionary<TKey, WeakReference<TValue>> _lookup = new();

    public void Add(TKey key, TValue value)
    {
        var added = _lookup.TryAdd(key, new WeakReference<TValue>(value));
        if (!added) throw new ArgumentException("Key already exists" + key);
        Cull();
    }

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        var value = _lookup.GetOrAdd(key, static (key, factory) => new WeakReference<TValue>(factory(key)), valueFactory);
        if (value.TryGetTarget(out var target)) return target;
        _lookup.TryRemove(key, out _);
        Cull();
        return GetOrAdd(key, valueFactory);
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        value = default!;
        if (_lookup.TryGetValue(key, out var weakReference))
        {
            if (weakReference.TryGetTarget(out value))
            {
                return true;
            }
            _lookup.TryRemove(key, out _);
        }

        return false;
    }

    private void Cull()
    {
        foreach (var (key, weakReference) in _lookup.ToArray())
        {
            if (!weakReference.TryGetTarget(out _))
            {
                _lookup.TryRemove(key, out _);
            }
        }
    }
}
