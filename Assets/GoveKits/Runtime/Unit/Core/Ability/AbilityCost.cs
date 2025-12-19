using System.Collections.Generic;

namespace GoveKits.Unit
{
    /// <summary>
    /// 能力消耗描述：管理能力所需的各类资源（MP、HP、Stamina 等）。
    /// <para>支持检查资源充足性（<see cref="Check"/>）和消耗支付（<see cref="Pay"/>）。</para>
    /// <para>适用于复杂消耗体系（多种资源、动态调整等）。</para>
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