using System;
using System.Collections;
using System.Collections.Generic;
using GoveKits.Runtime.Util;
using UnityEngine;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 集中式属性管理容器。
    /// 统一管理所有属性的 BaseValue、CurrentValue 以及管线重算。
    /// 提供属性修改器的增减接口和值变更的前置/后置拦截管线回调。
    /// </summary>
    public class AttributeContainer : ITagSource, IEnumerable<KeyValuePair<UnitTag, UnitAttribute>>
    {
        private readonly IUnit _owner;
        private readonly Dictionary<UnitTag, UnitAttribute> _attributes = new();

        #region 生命周期管线回调

        /// <summary>
        /// 预变更拦截回调（Before Change）。
        /// 职责：数值钳制（限制最大/最小值）、联动修正业务规则。
        /// 签名：Func(Tag, 预期目标值) -> 返回修正后的合法值。
        /// </summary>
        public Func<UnitTag, float, float> BeforeValueChange;

        /// <summary>
        /// 后变更通知回调（After Change）。
        /// 职责：驱动 UI 更新（血条等）、触发数值阈值事件（如血量归零致死）。
        /// 签名：Action(Tag, 旧值, 新值)。
        /// </summary>
        public Action<UnitTag, float, float> AfterValueChange;

        #endregion

        /// <summary>已注册属性数量</summary>
        public int Count => _attributes.Count;

        /// <summary>构造函数（依赖注入）</summary>
        public AttributeContainer(IUnit owner)
        {
            _owner = owner;
        }

        #region 查询与遍历接口

        public IEnumerator<KeyValuePair<UnitTag, UnitAttribute>> GetEnumerator() => _attributes.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _attributes.GetEnumerator();

        /// <summary>检查是否注册过指定属性</summary>
        public bool HasTag(UnitTag tag) => _attributes.ContainsKey(tag);

        /// <summary>获取属性当前值并通过 func 转换/限制（不存在返回 0）</summary>
        public float GetValue(UnitTag tag, Func<float, float> func)
        {
            if (!_attributes.TryGetValue(tag, out var d)) return 0f;
            return func(d.CurrentValue);
        }

        /// <summary>获取属性基础值（不存在返回 0）</summary>
        public float GetBaseValue(UnitTag tag) => _attributes.TryGetValue(tag, out var d) ? d.BaseValue : 0f;

        #endregion

        #region 属性写入与修改操作

        /// <summary>初始化注册一个属性及其基础值（不触发回调事件）</summary>
        public void Add(UnitTag tag, float baseValue)
        {
            var attribute = new UnitAttribute { BaseValue = baseValue, CurrentValue = baseValue };
            _attributes[tag] = attribute;
            UpdateCurrentValue(tag, attribute, triggerEvents: false);
        }

        /// <summary>永久性改变属性基础值（如角色升级成长、受伤扣血）</summary>
        public void ChangeBase(UnitTag tag, float deltaValue)
        {
            if (!_attributes.TryGetValue(tag, out var data)) return;

            float expectedBase = data.BaseValue + deltaValue;
            data.BaseValue = expectedBase;
            UpdateCurrentValue(tag, data, triggerEvents: true);
        }

        /// <summary>添加动态修改器（如 Buff 增益加成）</summary>
        public void AddModifier(UnitTag tag, AttributeModifier modifier)
        {
            if (!_attributes.TryGetValue(tag, out var data)) return;

            data.Modifiers.Add(modifier);
            UpdateCurrentValue(tag, data, triggerEvents: true);
        }

        /// <summary>
        /// 根据来源移除修改器（如 Buff 结束或卸下装备）。
        /// 优先按 Source 引用精确匹配；若引用未命中，则回退按 Source 的运行时类型移除该类型的所有修改器，
        /// 因此允许传入一个新 new 出来的同类型 Source（如 RemoveModifier("atk", new EquipSource())）。
        /// </summary>
        public void RemoveModifier(UnitTag tag, ModifierSource source)
        {
            if (!_attributes.TryGetValue(tag, out var data)) return;

            // 1. 引用精确匹配（精准移除单个修改器）
            int removed = data.Modifiers.RemoveAll(m => m.Source == source);

            // 2. 引用未命中且传入非空 Source 时，回退按类型匹配该来源的全部修改器
            if (removed == 0 && source != null)
                removed = data.Modifiers.RemoveAll(m => m.Source != null && m.Source.GetType() == source.GetType());

            if (removed > 0)
            {
                UpdateCurrentValue(tag, data, triggerEvents: true);
            }
        }

        /// <summary>强制某属性跑一遍重算管线（适用于该属性没有直接被修改，但受其他联动属性影响的情况）</summary>
        public void ForceRecalculate(UnitTag tag)
        {
            if (_attributes.TryGetValue(tag, out var data))
            {
                UpdateCurrentValue(tag, data, triggerEvents: true);
            }
        }

        #endregion

        #region 内部核心重算管线

        /// <summary>执行核心计算公式并派发事件流</summary>
        private void UpdateCurrentValue(UnitTag tag, UnitAttribute data, bool triggerEvents)
        {
            float oldCurrent = data.CurrentValue;

            float sumAdd = 0f, sumMult = 0f, overrideVal = 0f;
            bool hasOverride = false;

            for (int i = 0; i < data.Modifiers.Count; i++)
            {
                var mod = data.Modifiers[i];
                switch (mod.Type)
                {
                    case ModifierType.Additive: sumAdd += mod.Value; break;
                    case ModifierType.Multiplicative: sumMult += mod.Value; break;
                    case ModifierType.Override: hasOverride = true; overrideVal = mod.Value; break;
                }
            }

            float newCalculatedValue = hasOverride ? overrideVal : (data.BaseValue + sumAdd) * (1f + sumMult);

            if (BeforeValueChange != null)
            {
                newCalculatedValue = BeforeValueChange.Invoke(tag, newCalculatedValue);
            }

            data.CurrentValue = newCalculatedValue;

            if (triggerEvents && !Mathf.Approximately(oldCurrent, data.CurrentValue))
            {
                AfterValueChange?.Invoke(tag, oldCurrent, data.CurrentValue);
            }
        }

        #endregion

        /// <summary>清理重置该容器全部状态</summary>
        public void Clear()
        {
            _attributes.Clear();
            BeforeValueChange = null;
            AfterValueChange = null;
        }
    }
}
