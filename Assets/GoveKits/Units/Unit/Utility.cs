


using System;
using System.Collections.Generic;

namespace GoveKits.Units
{
    #region 上下文类
    // 上下文类，包含来源、目标和额外信息
    public class UnitContext
    {
        public IUnit Source { get; set; }  // 来源单元
        public IUnit Target { get; set; }  // 目标单元
        private Dictionary<string, object> Extra { get; set; }  // 额外信息字典

        // 构造函数
        public UnitContext(IUnit source = null, IUnit target = null, Dictionary<string, object> extra = null)
        {
            this.Source = source;
            this.Target = target;
            this.Extra = extra;
        }

        // 设置额外信息
        public void PutExtra(string key, object value)
        {
            if (Extra == null)
            {
                Extra = new Dictionary<string, object>();
            }
            Extra[key] = value;
        }

        // 通过键获取额外信息，若不存在则返回默认值
        public T GetExtra<T>(string key, T defaultValue = default)
        {
            if (Extra != null && Extra.TryGetValue(key, out var value) && value is T tValue)
            {
                return tValue;
            }
            return defaultValue;
        }


    }
    #endregion


    #region 容器类
    // 基于 Dictionary 的简单容器实现
    public abstract class DictionaryContainer<T>
    {
        protected readonly Dictionary<string, T> _items = new Dictionary<string, T>();  // 内部字典存储
        /// <summary>
        /// 添加或更新项
        /// </summary>
        public virtual void Add(string key, T item) => _items[key] = item;
        /// <summary>
        /// 移除项
        /// </summary>
        public virtual void Remove(string key) => _items.Remove(key);
        /// <summary>
        /// 尝试获取项
        /// </summary>
        public virtual bool TryGet(string key, out T item) => _items.TryGetValue(key, out item);
        /// <summary>
        /// 检查是否包含项
        /// </summary>
        public virtual bool Has(string key) => _items.ContainsKey(key);
        /// <summary>
        /// 清空容器
        /// </summary>
        public virtual void Clear() => _items.Clear();
        /// <summary>
        /// 获取所有键
        /// </summary>
        public IEnumerable<string> Keys => _items.Keys;
        /// <summary>
        /// 获取项数量
        /// </summary>
        public int Count => _items.Count;
    }
    #endregion



}