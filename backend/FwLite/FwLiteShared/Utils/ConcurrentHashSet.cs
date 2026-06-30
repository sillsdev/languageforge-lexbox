using System.Collections;
using System.Collections.Concurrent;

public class ConcurrentHashSet<T> : IEnumerable<T> where T : notnull
{
    private readonly ConcurrentDictionary<T, byte> _dictionary = new();

    // Idempotent add: returns true if added, false if already existed
    public bool Add(T item) => _dictionary.TryAdd(item, 0);

    public bool Contains(T item) => _dictionary.ContainsKey(item);

    public bool Remove(T item) => _dictionary.TryRemove(item, out _);

    public int Count => _dictionary.Count;

    // Thread-safe iteration
    public IEnumerator<T> GetEnumerator() => _dictionary.Keys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}