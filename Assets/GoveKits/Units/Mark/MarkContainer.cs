using System;
using System.Collections.Generic;
using System.Linq;


namespace GoveKits.Units
{
    /// <summary>
    /// Mark容器类
    /// </summary>
    public class MarkContainer : DictionaryContainer<Mark>
    {
        /// <summary>
        /// 添加Mark，如果已存在则堆叠，自动调用Apply和Stack
        /// </summary>
        public override void Add(string key, Mark Mark)
        {
            if (Has(key))
            {
                // 已存在则堆叠
                var existingMark = _items[key];
                existingMark.Stack(Mark.CurrentStack);
                OnMarkStacked?.Invoke(key);
                return;
            }
            base.Add(key, Mark);
            OnMarkAdded?.Invoke(key, Mark);

            Mark.Apply();  // 加入时自动调用Apply
        }

        /// <summary>
        /// 移除Mark，自动调用Remove
        /// </summary>
        public override void Remove(string key)
        {
            Mark Mark = TryGet(key, out var b) ? b : null;
            Mark?.Remove();  // 移除时自动调用Remove

            base.Remove(key);
            OnMarkRemoved?.Invoke(key);
        }

        public override void Clear()
        {
            OnMarkAdded = null;
            OnMarkRemoved = null;
            base.Clear();
        }


        #region 查询
        /// <summary>
        /// 执行查询
        /// </summary>
        public void MatchQuery(IMarkQuery query) => query.Match(this);

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
        private event Action<string, Mark> OnMarkAdded;  // Mark添加事件
        private event Action<string> OnMarkStacked;  // Mark堆叠事件
        private event Action<string> OnMarkRemoved;  // Mark移除事件

        // 订阅Mark添加事件
        public Action SubscribeMarkAdded(Action<string, Mark> listener)
        {
            OnMarkAdded += listener;
            return () => OnMarkAdded -= listener;
        }
        // 取消订阅Mark添加事件
        public void UnsubscribeMarkAdded(Action<string, Mark> listener)
        {
            OnMarkAdded -= listener;
        }
        // 订阅Mark堆叠事件
        public Action SubscribeMarkStacked(Action<string> listener)
        {
            OnMarkStacked += listener;
            return () => OnMarkStacked -= listener;
        }
        // 取消订阅Mark堆叠事件
        public void UnsubscribeMarkStacked(Action<string> listener)
        {
            OnMarkStacked -= listener;
        }
        // 订阅Mark移除事件
        public Action SubscribeMarkRemoved(Action<string> listener)
        {
            OnMarkRemoved += listener;
            return () => OnMarkRemoved -= listener;
        }
        // 取消订阅Mark移除事件
        public void UnsubscribeMarkRemoved(Action<string> listener)
        {
            OnMarkRemoved -= listener;
        }
        #endregion
    }
}