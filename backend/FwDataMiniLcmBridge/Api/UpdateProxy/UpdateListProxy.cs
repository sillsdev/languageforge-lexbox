using System.Collections;

namespace FwDataMiniLcmBridge.Api.UpdateProxy;

public class UpdateListProxy<T>(
    Action<T> add,
    Action<T> remove,
    Func<int, T> getAt) : IList<T>
{
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

    public void Clear()
    {
        throw new NotImplementedException();
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

    public int Count => throw new NotImplementedException();

    public bool IsReadOnly => false;

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