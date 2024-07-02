using System.Collections;

namespace FwDataMiniLcmBridge.Api.UpdateProxy;

public class UpdateListProxy<T>(
    Action<T> add,
    Action<T> remove,
    Func<int, T> getAt,
    int count) : IList<T>, IList
{
    private bool _isSynchronized;
    private bool _isFixedSize;

    public IEnumerator<T> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public void Add(T item)
    {
        add(item);
    }

    int IList.Add(object? value)
    {
        ArgumentNullException.ThrowIfNull(value);
        add((T)value);
        return 0;
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    bool IList.Contains(object? value)
    {
        throw new NotImplementedException();
    }

    int IList.IndexOf(object? value)
    {
        throw new NotImplementedException();
    }

    void IList.Insert(int index, object? value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Insert(index, (T)value);
    }

    void IList.Remove(object? value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Remove((T)value);
    }

    public bool Contains(T item)
    {
        throw new NotImplementedException();
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public bool Remove(T item)
    {
        remove(item);
        return false;
    }

    void ICollection.CopyTo(Array array, int index)
    {
        throw new NotImplementedException();
    }

    public int Count { get; } = count;

    bool ICollection.IsSynchronized => _isSynchronized;

    object ICollection.SyncRoot => throw new NotImplementedException();

    public bool IsReadOnly => false;
    object? IList.this[int index]
    {
        get => this[index];
        set => this[index] = (T?)value ?? throw new ArgumentNullException(nameof(value));
    }

    public int IndexOf(T item)
    {
        throw new NotImplementedException();
    }

    public void Insert(int index, T item)
    {
        add(item);
    }

    public void RemoveAt(int index)
    {
        Remove(getAt(index));
    }

    bool IList.IsFixedSize => _isFixedSize;

    public T this[int index]
    {
        get => getAt(index);
        set
        {
            RemoveAt(index);
            Insert(index, value);
        }
    }
}
