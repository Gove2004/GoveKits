using System;
using System.Collections.Generic;

namespace GoveKits.Unit
{
    /// <summary>
    /// 状态属性：计算型属性（MaxHP、攻击力、防御力等），支持修改器系统。
    /// <para>计算公式：(Base + Sum(Flat)) × (1 + Sum(PercentAdd)) × Prod(1 + PercentMult)，若存在 Override 修改器则覆盖结果。</para>
    /// <para>支持修改器的动态添加/移除、按来源批量移除、缓存优化（延迟计算）。</para>
    /// <para>适用于复杂的属性系统（Buff、装备、技能等多源修改）。</para>
    /// </summary>
    public class StateAttribute : GameAttribute
    {
        private float _baseValue;
        
        // 修改器列表
        private readonly List<GameModifier> _modifiers = new List<GameModifier>();
        
        // 缓存机制
        private float _cachedValue;
        private bool _isDirty = true;
        
        // 逻辑下限 (如移速不能 < 0)
        private readonly float _minLimit;

        /// <summary>
        /// 构造 StateAttribute
        /// </summary>
        /// <param name="tag">属性标签</param>
        /// <param name="baseValue">基础值</param>
        /// <param name="minLimit">值下限（防止出现非法负值）</param>
        public StateAttribute(GameTag tag, float baseValue, float minLimit = 0f)
            : base(tag)
        {
            _baseValue = baseValue;
            _minLimit = minLimit;
            // 构造时不计算，等到首次访问 Value 时懒加载计算
        }

        public override float Value
        {
            get
            {
                if (_isDirty) Recalculate();
                return _cachedValue;
            }

        }

        // --- 写操作 (只能通过 Base 或 Modifier 修改) ---

        /// <summary>
        /// 设置基础值并标记为脏（触发重算与通知）
        /// </summary>
        public void SetBase(float val)
        {
            if (Math.Abs(_baseValue - val) > 1e-5f)
            {
                // 读取旧值（会触发懒计算 if needed）
                float old = Value;

                // 更新基值并立即重算、通知。
                // 直接调用 Recalculate 而不是 SetDirty，因为 SetDirty
                // 在属性已为脏时会早退，导致未触发通知。
                _baseValue = val;
                Recalculate();
                NotifyChange(old, _cachedValue);
            }
        }

        /// <summary>
        /// 添加一个修改器（如来自 Buff、装备、技能等），并标记为脏
        /// </summary>
        public void AddModifier(GameModifier mod)
        {
            _modifiers.Add(mod);
            SetDirty();
        }

        /// <summary>
        /// 移除单个修改器（若存在则标记为脏）
        /// </summary>
        public void RemoveModifier(GameModifier mod)
        {
            if (_modifiers.Remove(mod)) SetDirty();
        }

        /// <summary>
        /// 根据来源移除所有修改器（例如移除某个 Buff 导致的所有 modifier）
        /// </summary>
        public void RemoveBySource(object source)
        {
            if (_modifiers.RemoveAll(m => m.Source == source) > 0) SetDirty();
        }

        // --- 核心计算 ---

        /// <summary>
        /// 将属性标记为脏并触发一次重算/通知（仅当当前为干净状态时）
        /// <para>这里的策略是：避免重复触发重算；若已经脏则不重复计算。</para>
        /// </summary>
        private void SetDirty()
        {
            if (_isDirty) return;
            _isDirty = true;

            // 立即重算并通知，保证依赖链（如 Vital）能立刻响应
            float old = _cachedValue;
            Recalculate();
            NotifyChange(old, _cachedValue);
        }

        private void Recalculate()
        {
            float sumFlat = 0f;
            float sumPercentAdd = 0f;
            float prodPercentMult = 1f;
            
            bool hasOverride = false;
            float overrideValue = 0f;

            // O(N) 一次遍历完成分类
            for (int i = 0; i < _modifiers.Count; i++)
            {
                var mod = _modifiers[i];
                switch (mod.Type)
                {
                    case ModifierType.Flat:        sumFlat += mod.Value; break;
                    case ModifierType.PercentAdd:  sumPercentAdd += mod.Value; break;
                    case ModifierType.PercentMult: prodPercentMult *= (1f + mod.Value); break;
                    case ModifierType.Override:
                        hasOverride = true;
                        overrideValue = mod.Value; // 取最后一个覆写
                        break;
                }
            }

            if (hasOverride)
            {
                _cachedValue = overrideValue;
            }
            else
            {
                // 标准 RPG 公式
                float val = (_baseValue + sumFlat) * (1f + sumPercentAdd) * prodPercentMult;
                _cachedValue = Math.Max(val, _minLimit);
            }

            _isDirty = false;
        }
    }
}