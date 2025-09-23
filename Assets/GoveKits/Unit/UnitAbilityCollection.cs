using System.Collections.Generic;


namespace GoveKits.Unit
{
    /// <summary>
    /// 能力属性集，键值对存储，可动态扩展
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="V"></typeparam>
    public class UnitAbilityCollection<K, V> where V : System.Delegate
    {
        // 所有能力属性
        private readonly Dictionary<K, V> _abilities = new();

        /// <summary>
        /// 检查是否存在指定能力
        /// </summary>
        public bool HasAbility(K key) => _abilities.ContainsKey(key);

        /// <summary>
        /// 设置能力，有就覆盖，没有就新增
        /// </summary>
        public void SetAbility(K key, V ability)
        {
            _abilities[key] = ability;
        }

        /// <summary>
        /// 移除能力
        /// </summary>
        public void RemoveAbility(K key)
        {
            if (_abilities.ContainsKey(key))
            {
                _abilities.Remove(key);
            }
        }

        /// <summary>
        /// 执行能力
        /// </summary>
        public void InvokeAbility(K key, params object[] args)
        {
            if (_abilities.ContainsKey(key))
            {
                _abilities[key].DynamicInvoke(args);
            }
        }
    }
}