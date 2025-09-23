
using System.Collections.Generic;


namespace GoveKits.Unit
{
    /// <summary>
    /// 动作属性集，键值对存储，可动态扩展
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="V"></typeparam>
    public class UnitActionCollection<K, V> where V : System.Delegate
    {
        // 所有动作属性
        private readonly Dictionary<K, List<V>> _actions = new();

        /// <summary>
        /// 检查是否存在指定动作
        /// </summary>
        public bool HasAction(K key) => _actions.ContainsKey(key);

        /// <summary>
        /// 添加动作
        /// </summary>
        public void AddAction(K key, V action)
        {
            if (!_actions.ContainsKey(key))
            {
                _actions[key] = new List<V>();
            }
            _actions[key].Add(action);
        }

        /// <summary>
        /// 移除动作
        /// </summary>
        public void RemoveAction(K key, V action)
        {
            if (_actions.ContainsKey(key))
            {
                _actions[key].Remove(action);
                if (_actions[key].Count == 0)
                {
                    _actions.Remove(key);
                }
            }
        }

        /// <summary>
        /// 执行动作
        /// </summary>
        public void InvokeAction(K key, params object[] args)
        {
            if (_actions.ContainsKey(key))
            {
                foreach (var action in _actions[key])
                {
                    action.DynamicInvoke(args);
                }
            }
        }
    }

}