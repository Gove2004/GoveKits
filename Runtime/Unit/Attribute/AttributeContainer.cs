using System;
using System.Collections;
using System.Collections.Generic;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 集中式属性容器
    /// 
    /// 核心功能：
    /// 1. 统一管理所有属性的 BaseValue、Modifier 和 CurrentValue
    /// 2. 提供属性修改器添加/移除接口
    /// 3. 提供值变更拦截管线（Before/After）
    /// 4. 支持属性标签查询和遍历
    /// 
    /// 架构设计：
    /// - 标签化：使用 UnitTag 枚举标识不同属性
    /// - 管线化：值变更经过 Pre/Post 拦截
    /// - 事件化：支持值变更通知回调
    /// - 零 GC：修改器使用 struct，避免堆分配
    /// </summary>
    public class AttributeContainer : ITagSource, IEnumerable<KeyValuePair<UnitTag, UnitAttribute>>
    {
        /// <summary>
        /// 属性字典 - 按标签索引
        /// 存储所有已注册的属性及其数据
        /// </summary>
        private readonly Dictionary<UnitTag, UnitAttribute> _attributes = new();
        
        /// <summary>遍历器 - 支持 foreach 遍历所有属性</summary>
        public IEnumerator<KeyValuePair<UnitTag, UnitAttribute>> GetEnumerator() => _attributes.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _attributes.GetEnumerator();
        
        /// <summary>标签查询 - 检查是否包含指定属性</summary>
        public bool HasTag(UnitTag tag) => _attributes.ContainsKey(tag);
        
        /// <summary>属性数量 - 已注册的属性总数</summary>
        public int Count => _attributes.Count;

        /// <summary>
        /// 预变更拦截回调
        /// 1. 数值钳制：限制属性在合法范围内
        /// 2. 联动修正：根据其他属性调整当前值
        /// 3. 业务规则：实现特殊属性逻辑
        /// </summary>
        public Func<UnitTag, float, float> BeforeValueChange;
        
        /// <summary>
        /// 后变更通知回调

        /// 1. UI 更新：血条、属性面板刷新
        /// 2. 事件触发：属性达到阈值时触发效果
        /// 3. 日志记录：追踪属性变化历史
        /// </summary>
        public Action<UnitTag, float, float> AfterValueChange;

        /// <summary>
        /// 添加属性
        /// 
        /// 核心功能：
        /// 1. 创建新的 UnitAttribute 实例
        /// 2. 初始化 BaseValue 和 CurrentValue
        /// 3. 触发初始值计算（不触发事件）
        /// </summary>
        /// <param name="tag">属性标签</param>
        /// <param name="baseValue">基础值</param>
        public void Add(UnitTag tag, float baseValue)
        {
            var attribute = new UnitAttribute { BaseValue = baseValue, CurrentValue = baseValue };
            _attributes[tag] = attribute;

            UpdateCurrentValue(tag, attribute, triggerEvents: false);
        }

        /// <summary>
        /// 获取属性当前值
        /// 
        /// 返回说明：
        /// - 存在：返回 CurrentValue
        /// - 不存在：返回 0
        /// </summary>
        public float GetValue(UnitTag tag) => _attributes.TryGetValue(tag, out var d) ? d.CurrentValue : 0f;
        
        /// <summary>
        /// 获取属性基础值
        /// 
        /// 返回说明：
        /// - 存在：返回 BaseValue
        /// - 不存在：返回 0
        /// </summary>
        public float GetBaseValue(UnitTag tag) => _attributes.TryGetValue(tag, out var d) ? d.BaseValue : 0f;

        /// <summary>
        /// 修改属性基础值
        /// 适用场景：
        /// - 角色升级：基础属性增长
        /// - 装备更换：基础属性变化
        /// - Buff 效果：临时基础值改变
        /// </summary>
        /// <param name="tag">属性标签</param>
        /// <param name="deltaValue">基础值变化量（可为负）</param>
        public void ChangeBase(UnitTag tag, float deltaValue)
        {
            if (!_attributes.TryGetValue(tag, out var data)) return;

            // 1. 预计算新的基础值
            float expectedBase = data.BaseValue + deltaValue;

            // 2. Pre 拦截：交由外部业务规则钳制或修正
            if (BeforeValueChange != null)
            {
                expectedBase = BeforeValueChange.Invoke(tag, expectedBase);
            }

            // 3. 赋给合法的 BaseValue
            data.BaseValue = expectedBase;
            
            // 4. 走管线重算 CurrentValue，并触发事件
            UpdateCurrentValue(tag, data, triggerEvents: true);
        }

        /// <summary>
        /// 添加属性修改器
        /// 适用场景：
        /// - 装备穿戴：添加装备属性加成
        /// - Buff 施加：添加临时效果
        /// - 技能激活：添加技能增益
        /// </summary>
        /// <param name="tag">属性标签</param>
        /// <param name="modifier">修改器实例</param>
        public void AddModifier(UnitTag tag, AttributeModifier modifier)
        {
            if (!_attributes.TryGetValue(tag, out var data)) return;

            data.Modifiers.Add(modifier);
            UpdateCurrentValue(tag, data, triggerEvents: true);
        }

        /// <summary>
        /// 移除指定来源的所有修改器
        /// 适用场景：
        /// - 装备卸下：移除该装备的所有加成
        /// - Buff 过期：移除该 Buff 的所有效果
        /// - 技能取消：移除该技能的增益
        /// </summary>
        /// <param name="tag">属性标签</param>
        /// <param name="source">修改器来源</param>
        public void RemoveModifier(UnitTag tag, ModifierSource source)
        {
            if (!_attributes.TryGetValue(tag, out var data)) return;

            // 只有当真正移除了修改器时，才触发管线重算
            if (data.Modifiers.RemoveAll(m => m.Source == source) > 0)
            {
                UpdateCurrentValue(tag, data, triggerEvents: true);
            }
        }

        /// <summary>
        /// 强制重新计算指定属性
        /// 适用场景：
        /// - 上限改变：HealthMax 变化时强刷 CurrentHealth
        /// - 联动触发：相关属性变化时刷新
        /// - 外部修正：外部逻辑修改后同步
        /// </summary>
        /// <param name="tag">属性标签</param>
        public void ForceRecalculate(UnitTag tag)
        {
            if (_attributes.TryGetValue(tag, out var data))
            {
                UpdateCurrentValue(tag, data, triggerEvents: true);
            }
        }

        // ================= 核心重算管线 =================

        /// <summary>
        /// 内部管线：汇总公式 → Pre 拦截 (钳制) → 赋值 → Post 拦截 (联动) / Event
        /// 计算示例：
        /// BaseValue = 100
        /// Additive = +30 + 20 = +50
        /// Multiplicative = +0.2 + 0.1 = +0.3
        /// → CurrentValue = (100 + 50) * (1 + 0.3) = 195
        /// </summary>
        /// <param name="tag">属性标签</param>
        /// <param name="data">属性数据</param>
        /// <param name="triggerEvents">是否触发事件回调</param>
        private void UpdateCurrentValue(UnitTag tag, UnitAttribute data, bool triggerEvents)
        {
            float oldCurrent = data.CurrentValue;

            // 1. 汇总修改器：分别计算 Additive、Multiplicative、Override
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

            // 2. 应用计算公式
            float newCalculatedValue = hasOverride ? overrideVal : (data.BaseValue + sumAdd) * (1f + sumMult);

            // 3. Pre 拦截：交由外部业务规则钳制或修正
            if (BeforeValueChange != null)
            {
                newCalculatedValue = BeforeValueChange.Invoke(tag, newCalculatedValue);
            }

            // 4. 赋值确立
            data.CurrentValue = newCalculatedValue;

            // 5. Post 拦截与事件：仅在值发生真实改变，且允许触发事件时执行
            if (triggerEvents && Math.Abs(oldCurrent - data.CurrentValue) > 1e-5f)
            {
                AfterValueChange?.Invoke(tag, oldCurrent, data.CurrentValue);
            }
        }

        // ================= 资源清理 =================

        /// <summary>
        /// 清空所有属性数据
        /// </summary>
        public void Clear()
        {
            _attributes.Clear();
            BeforeValueChange = null;
            AfterValueChange = null;
        }
    }
}