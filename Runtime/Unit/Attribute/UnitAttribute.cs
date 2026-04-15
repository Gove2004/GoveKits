using System.Collections.Generic;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 单个属性的数据存储块。
    /// 
    /// 核心功能：
    /// 1. 存储属性的基础值（BaseValue）
    /// 2. 存储属性的当前值（CurrentValue，受修改器影响）
    /// 3. 维护作用在该属性上的所有修改器列表
    /// 
    /// 设计说明：
    /// - 此类是 AttributeContainer 的内部数据结构，对外不直接暴露。
    /// - Modifiers 列表存储所有持续性/永久性修改器。
    /// 
    /// 数据流转公式：
    /// (BaseValue + Additive) * Multiplicative = CurrentValue (或 Override 强制覆盖)
    /// </summary>
    public class UnitAttribute
    {
        /// <summary>
        /// 基础值。
        /// 属性的原始数值，通常由角色基础属性或配置表决定。
        /// </summary>
        public float BaseValue;
        
        /// <summary>
        /// 当前值。
        /// 经过所有修改器计算后的最终数值，供业务逻辑实际使用。
        /// </summary>
        public float CurrentValue;
        
        /// <summary>
        /// 修改器列表。
        /// 存储当前作用在该属性上的所有生效修改器。
        /// </summary>
        public readonly List<AttributeModifier> Modifiers = new();
    }
}