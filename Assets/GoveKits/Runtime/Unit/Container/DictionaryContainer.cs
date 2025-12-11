
using System;
using System.Collections;
using System.Collections.Generic;

namespace GoveKits.Unit
{
    /// <summary>
    /// 通用字典容器基类
    /// - 以 <see cref="GameTag"/> 作为 Key，T 为 Value（例如属性/标记/能力）
    /// - 提供 Add/Remove/Clear/查询等基础操作，并暴露事件通知（OnItemAdded/OnItemRemoved）
    /// - 子类可重写 Add/Remove/Update 来实现自定义行为（如 Mark 的堆叠逻辑）
    /// </summary>
    public abstract class DictionaryContainer<T> : IEnumerable<KeyValuePair<GameTag, T>>, IGameTagSource
    {
        // 核心存储：Key 变成了 GameTag
        protected readonly Dictionary<GameTag, T> _items = new Dictionary<GameTag, T>();

        public event Action<T> OnItemAdded;
        public event Action<T> OnItemRemoved;

        #region 核心虚方法

        /// <summary>
        /// 添加项 (默认行为是：不存在则添加，存在则覆盖/报错)
        /// <para>注意：MarkContainer 需要重写此方法以支持堆叠</para>
        /// </summary>
        public virtual void Add(GameTag key, T item)
        {
            // 如果你希望覆盖旧值：
            _items[key] = item;

            OnItemAdded?.Invoke(item);
        }

        public virtual bool Remove(GameTag key)
        {
            if (_items.TryGetValue(key, out var item))
            {
                _items.Remove(key);
                OnItemRemoved?.Invoke(item);
                return true;
            }
            return false;
        }

        public virtual void Clear()
        {
            // 触发移除事件（可选，看需求是否需要通知清空）
            // foreach(var item in _items.Values) OnItemRemoved?.Invoke(item);
            
            _items.Clear();
        }

        #endregion

        #region 查询与索引

        public virtual T this[GameTag key]
        {
            get => _items[key];
            set => Add(key, value); 
        }

        public bool MatchQuery(TagQuery query) => query.Match(this);

        public bool TryGet(GameTag key, out T item) => _items.TryGetValue(key, out item);
        
        public bool HasTag(GameTag key) => _items.ContainsKey(key);

        public int Count => _items.Count;

        public Dictionary<GameTag, T>.KeyCollection Keys => _items.Keys;
        
        public Dictionary<GameTag, T>.ValueCollection Values => _items.Values;

        #endregion

        #region IEnumerable 实现

        public IEnumerator<KeyValuePair<GameTag, T>> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        #endregion
    }
}