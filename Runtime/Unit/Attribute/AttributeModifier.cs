using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 属性修改器的加成类型。
    /// </summary>
    public enum ModifierType
    {
        /// <summary>加法修改：直接加减固定数值（如攻击力 +50）</summary>
        Additive,

        /// <summary>乘法修改：按百分比增减，存储为小数（如攻击力 +20% 存为 0.2）</summary>
        Multiplicative,

        /// <summary>覆盖修改：强制设置属性为指定值，优先级最高，忽略其他加减乘</summary>
        Override
    }

    /// <summary>
    /// 属性修改器来源（用于精准追踪和移除某个 Buff 或装备带来的属性修改）。
    /// 继承此类以区分不同来源（如装备增益、技能增益等）。
    /// </summary>
    public abstract class ModifierSource
    {
    }

    /// <summary>
    /// 属性修改器（值类型，零 GC 分配）。
    /// </summary>
    public readonly struct AttributeModifier
    {
        /// <summary>修改器类型，决定参与哪一步计算环节</summary>
        public readonly ModifierType Type;

        /// <summary>修改器数值（固定值、小数值或覆盖目标值）</summary>
        public readonly float Value;

        /// <summary>修改器溯源标记</summary>
        public readonly ModifierSource Source;

        /// <summary>创建属性修改器实例</summary>
        public AttributeModifier(ModifierType type, float value, ModifierSource source = null)
        {
            Type = type;
            Value = value;
            Source = source;
        }
    }
}
