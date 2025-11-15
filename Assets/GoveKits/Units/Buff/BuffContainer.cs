using System;
using System.Collections.Generic;
using System.Linq;


namespace GoveKits.Units
{
    /// <summary>
    /// Buff容器类
    /// </summary>
    public class BuffContainer : DictionaryContainer<Buff>
    {
        /// <summary>
        /// 添加Buff，如果已存在则堆叠，自动调用Apply和Stack
        /// </summary>
        public override void Add(string key, Buff buff)
        {
            if (Has(key))
            {
                // 已存在则堆叠
                var existingBuff = _items[key];
                existingBuff.Stack(buff.CurrentStack);
                OnBuffStacked?.Invoke(key);
                return;
            }
            base.Add(key, buff);
            OnBuffAdded?.Invoke(key, buff);

            buff.Apply();  // 加入时自动调用Apply
        }

        /// <summary>
        /// 移除Buff，自动调用Remove
        /// </summary>
        public override void Remove(string key)
        {
            Buff buff = TryGet(key, out var b) ? b : null;
            buff?.Remove();  // 移除时自动调用Remove

            base.Remove(key);
            OnBuffRemoved?.Invoke(key);
        }

        public override void Clear()
        {
            OnBuffAdded = null;
            OnBuffRemoved = null;
            base.Clear();
        }


        #region 查询
        /// <summary>
        /// 执行查询
        /// </summary>
        public void MatchQuery(IBuffQuery query) => query.Match(this);

        public bool Any(params string[] names)
        {
            return names.Any(name => Has(name));
        }

        public bool All(params string[] names)
        {
            return names.All(name => Has(name));
        }

        public bool None(params string[] names)
        {
            return names.All(name => !Has(name));
        }

        #endregion

        #region 事件
        private event Action<string, Buff> OnBuffAdded;  // Buff添加事件
        private event Action<string> OnBuffStacked;  // Buff堆叠事件
        private event Action<string> OnBuffRemoved;  // Buff移除事件

        // 订阅Buff添加事件
        public Action SubscribeBuffAdded(Action<string, Buff> listener)
        {
            OnBuffAdded += listener;
            return () => OnBuffAdded -= listener;
        }
        // 取消订阅Buff添加事件
        public void UnsubscribeBuffAdded(Action<string, Buff> listener)
        {
            OnBuffAdded -= listener;
        }
        // 订阅Buff堆叠事件
        public Action SubscribeBuffStacked(Action<string> listener)
        {
            OnBuffStacked += listener;
            return () => OnBuffStacked -= listener;
        }
        // 取消订阅Buff堆叠事件
        public void UnsubscribeBuffStacked(Action<string> listener)
        {
            OnBuffStacked -= listener;
        }
        // 订阅Buff移除事件
        public Action SubscribeBuffRemoved(Action<string> listener)
        {
            OnBuffRemoved += listener;
            return () => OnBuffRemoved -= listener;
        }
        // 取消订阅Buff移除事件
        public void UnsubscribeBuffRemoved(Action<string> listener)
        {
            OnBuffRemoved -= listener;
        }
        #endregion
    }
}