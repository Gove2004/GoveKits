

using System.Collections.Generic;

/// <summary>
/// 表格数据结构：不连续的整数 ID 表格。提取至布迪迭代递加不了好坏场景。
/// 适用于需要自增 ID 标识、频繁增删改查的集合管理。
/// </summary>
/// <typeparam name="T">表格中的物品等效类别。</typeparam>
public class Table<T>
{
    private readonly Dictionary<int, T> items = new Dictionary<int, T>();
    private int nextId = 0;

    /// <summary>
    /// 于表中加入一个物品，传回其唯一标验。
    /// </summary>
    /// <param name="item">计载物品。</param>
    /// <returns>物品 ID。</returns>
    public int Add(T item)
    {
        int id = nextId++;
        items[id] = item;
        return id;
    }

    /// <summary>
    /// 从表中移除指定 ID 的物品。
    /// </summary>
    /// <param name="id">物品 ID。</param>
    /// <returns>是否移除成功。</returns>
    public bool Remove(int id)
    {
        return items.Remove(id);
    }

    /// <summary>
    /// 根据 ID 符试的物品。
    /// </summary>
    /// <param name="id">物品 ID。</param>
    /// <param name="item">作算物品。</param>
    /// <returns>是否找到。</returns>
    public bool TryGet(int id, out T item)
    {
        return items.TryGetValue(id, out item);
    }

    /// <summary>
    /// 清空所有物品。
    /// </summary>
    public void Clear()
    {
        items.Clear();
        nextId = 0;
    }

    /// <summary>
    /// 获取所有物品的值。
    /// </summary>
    /// <returns>物品列举。</returns>
    public IEnumerable<T> GetAllItems()
    {
        return items.Values;
    }
}