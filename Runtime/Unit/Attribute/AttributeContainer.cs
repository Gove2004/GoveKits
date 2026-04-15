using System;
using System.Collections;
using System.Collections.Generic;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 集中式属性管理容器。
    /// 
    /// 核心职责：
    /// 1. 统一管理所有属性的 BaseValue、CurrentValue 以及管线重算。
    /// 2. 提供属性修改器的增减接口。
    /// 3. 提供值变更的前置/后置拦截管线回调。
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

        /// <summary>获取属性当前值（不存在返回 0）</summary>
        public float GetValue(UnitTag tag) => _attributes.TryGetValue(tag, out var d) ? d.CurrentValue : 0f;
        
        /// <summary>获取属性基础值（不存在返回 0）</summary>
        public float GetBaseValue(UnitTag tag) => _attributes.TryGetValue(tag, out var d) ? d.BaseValue : 0f;

        #endregion

        #region 属性写入与修改操作

        /// <summary>
        /// 初始化注册一个属性及其基础值（不触发回调事件）。
        /// </summary>
        public void Add(UnitTag tag, float baseValue)
        {
            var attribute = new UnitAttribute { BaseValue = baseValue, CurrentValue = baseValue };
            _attributes[tag] = attribute;
            UpdateCurrentValue(tag, attribute, triggerEvents: false);
        }

        /// <summary>
        /// 永久性改变属性基础值（如角色升级成长、受伤扣血）。
        /// </summary>
        /// <param name="deltaValue">变化量（正增负减）</param>
        public void ChangeBase(UnitTag tag, float deltaValue)
        {
            if (!_attributes.TryGetValue(tag, out var data)) return;

            float expectedBase = data.BaseValue + deltaValue;

            // 走预处理管线钳制基础值
            if (BeforeValueChange != null)
                expectedBase = BeforeValueChange.Invoke(tag, expectedBase);

            data.BaseValue = expectedBase;
            
            // 基础值变动，触发管线重算及后置事件
            UpdateCurrentValue(tag, data, triggerEvents: true);
        }

        /// <summary>
        /// 添加动态修改器（如 Buff 增益加成）。
        /// </summary>
        public void AddModifier(UnitTag tag, AttributeModifier modifier)
        {
            if (!_attributes.TryGetValue(tag, out var data)) return;

            data.Modifiers.Add(modifier);
            UpdateCurrentValue(tag, data, triggerEvents: true);
        }

        /// <summary>
        /// 根据来源标记，移除所有相关修改器（如 Buff 结束或卸下装备）。
        /// </summary>
        public void RemoveModifier(UnitTag tag, ModifierSource source)
        {
            if (!_attributes.TryGetValue(tag, out var data)) return;

            // 仅当确实有修改器被移除时，才触发代价昂贵的管线重算
            if (data.Modifiers.RemoveAll(m => m.Source == source) > 0)
            {
                UpdateCurrentValue(tag, data, triggerEvents: true);
            }
        }

        /// <summary>
        /// 强制某属性跑一遍重算管线（适用于该属性没有直接被修改，但受其他联动属性影响的情况）。
        /// </summary>
        public void ForceRecalculate(UnitTag tag)
        {
            if (_attributes.TryGetValue(tag, out var data))
            {
                UpdateCurrentValue(tag, data, triggerEvents: true);
            }
        }

        #endregion

        #region 内部核心重算管线

        /// <summary>
        /// 执行核心计算公式并派发事件流。
        /// </summary>
        private void UpdateCurrentValue(UnitTag tag, UnitAttribute data, bool triggerEvents)
        {
            float oldCurrent = data.CurrentValue;

            // 1. 汇总所有处于激活状态的修改器
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

            // 2. 套用 GAS 经典计算公式
            float newCalculatedValue = hasOverride ? overrideVal : (data.BaseValue + sumAdd) * (1f + sumMult);

            // 3. 拦截管线：业务方二次过滤（例如限制移速不超过阈值）
            if (BeforeValueChange != null)
            {
                newCalculatedValue = BeforeValueChange.Invoke(tag, newCalculatedValue);
            }

            // 4. 最终赋值
            data.CurrentValue = newCalculatedValue;

            // 5. 事件派发：仅当值发生本质变化时通知外部
            if (triggerEvents && Math.Abs(oldCurrent - data.CurrentValue) > 1e-5f)
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