


using System.Collections.Generic;

public class Table<T>
{
    private readonly Dictionary<int, T> items = new Dictionary<int, T>();
    private int nextId = 0;

    public int Add(T item)
    {
        int id = nextId++;
        items[id] = item;
        return id;
    }

    public bool Remove(int id)
    {
        return items.Remove(id);
    }

    public bool TryGet(int id, out T item)
    {
        return items.TryGetValue(id, out item);
    }

    public void Clear()
    {
        items.Clear();
        nextId = 0;
    }

    public IEnumerable<T> GetAllItems()
    {
        return items.Values;
    }
}