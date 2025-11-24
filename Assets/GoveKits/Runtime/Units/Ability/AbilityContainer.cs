
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GoveKits.Units
{
    // 能力容器，用于管理单位的能力
    public class AbilityContainer : DictionaryContainer<IAbility>
    {
        public override void Add(string key, IAbility ability)
        {
            if (Has(key)) return;
            _items[key] = ability;
            OnAbilityAdded?.Invoke(key, ability);
        }

        public override void Remove(string key)
        {
            _items.Remove(key);
            OnAbilityRemoved?.Invoke(key);
        }


        public override void Clear()
        {
            OnAbilityAdded = null;
            OnAbilityRemoved = null;
            base.Clear();
        }


        /// <summary>
        /// 尝试执行能力，包含完整的生命周期
        /// </summary>
        public async UniTask TryExecute(string key, UnitContext context)
        {
            await _items[key].Try(context);
        }


        #region 
        private event Action<string, IAbility> OnAbilityAdded;  // 能力添加事件
        private event Action<string> OnAbilityRemoved;  // 能力移除事件

        // 订阅能力添加事件
        public Action SubscribeAbilityAdded(System.Action<string, IAbility> listener)
        {
            OnAbilityAdded += listener;
            return () => OnAbilityAdded -= listener;
        }
        // 取消订阅能力添加事件
        public void UnsubscribeAbilityAdded(System.Action<string, IAbility> listener)
        {
            OnAbilityAdded -= listener;
        }
        // 订阅能力移除事件
        public Action SubscribeAbilityRemoved(System.Action<string> listener)
        {
            OnAbilityRemoved += listener;
            return () => OnAbilityRemoved -= listener;
        }
        // 取消订阅能力移除事件
        public void UnsubscribeAbilityRemoved(System.Action<string> listener)
        {
            OnAbilityRemoved -= listener;
        }
        #endregion
    }
}