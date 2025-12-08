using System.Collections.Generic;

namespace GoveKits.Unit
{
    /// <summary>
    /// 技能消耗描述
    /// <para>负责管理技能所需的资源，并提供检查和支付方法。</para>
    /// </summary>
    public class AbilityCost
    {
        // Tag -> 数值 (如 "MP": 20, "HP": 10)
        private readonly Dictionary<GameTag, float> _resourceCosts = new Dictionary<GameTag, float>();

        public void AddCost(GameTag resourceTag, float amount)
        {
            if (_resourceCosts.ContainsKey(resourceTag))
                _resourceCosts[resourceTag] += amount;
            else
                _resourceCosts[resourceTag] = amount;
        }

        /// <summary>
        /// 检查资源是否足够
        /// </summary>
        public bool Check(IGameUnit unit)
        {
            if (unit == null) return false;

            foreach (var kvp in _resourceCosts)
            {
                // 获取当前属性值
                float currentVal = unit.Attributes.GetValue(kvp.Key);
                if (currentVal < kvp.Value)
                {
                    return false; // 只要有一项不足，就返回 false
                }
            }
            return true;
        }

        /// <summary>
        /// 支付消耗 (扣除资源)
        /// </summary>
        public void Pay(IGameUnit unit)
        {
            if (unit == null) return;

            foreach (var kvp in _resourceCosts)
            {
                // 调用 RuntimeAttribute 的 ApplyChange (负数表示扣除)
                unit.Attributes.ApplyRuntimeChange(kvp.Key, -kvp.Value);
            }
        }
    }
}