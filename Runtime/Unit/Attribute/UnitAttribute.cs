using System.Collections.Generic;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 单个属性的数据存储块
    /// 
    /// 核心功能：
    /// 1. 存储属性的基础值（BaseValue）
    /// 2. 存储属性的当前值（CurrentValue，受修改器影响）
    /// 3. 维护作用在该属性上的所有修改器列表
    /// 
    /// 设计说明：
    /// - 该类是 AttributeContainer 的内部数据结构
    /// - 不直接对外暴露，由容器统一管理
    /// - Modifiers 列表存储所有持续性/永久性修改器
    /// 
    /// 数据流转：
    /// BaseValue + Modifiers → 计算管线 → CurrentValue
    /// 
    /// 使用示例：
    /// var attr = new UnitAttribute();
    /// attr.BaseValue = 100;
    /// attr.Modifiers.Add(new AttributeModifier(ModifierType.Additive, 50, buffSource));
    /// // CurrentValue 由 AttributeContainer.UpdateCurrentValue 计算
    /// </summary>
    public class UnitAttribute
    {
        /// <summary>
        /// 基础值
        /// 属性的原始数值，不受修改器影响
        /// 通常由角色等级、基础属性等决定
        /// </summary>
        public float BaseValue;
        
        /// <summary>
        /// 当前值
        /// 经过所有修改器计算后的最终数值
        /// 游戏逻辑中实际使用的值
        /// </summary>
        public float CurrentValue;
        
        /// <summary>
        /// 修改器列表
        /// 存储当前作用在该属性上的所有修改器
        /// </summary>
        public List<AttributeModifier> Modifiers = new List<AttributeModifier>();
    }
}