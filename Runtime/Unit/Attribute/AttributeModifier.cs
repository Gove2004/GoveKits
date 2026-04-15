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
    /// </summary>
    public abstract class ModifierSource
    {
    }

    /// <summary>
    /// 属性修改器 (0 GC Struct)。
    /// </summary>
    public readonly struct AttributeModifier
    {
        /// <summary>修改器类型，决定参与哪一步计算环节。</summary>
        public readonly ModifierType Type;
        
        /// <summary>修改器数值（固定值、小数值或覆盖目标值）。</summary>
        public readonly float Value;
        
        /// <summary>修改器溯源标记。</summary>
        public readonly ModifierSource Source;

        /// <summary>
        /// 创建属性修改器实例。
        /// </summary>
        /// <param name="type">加成类型（加法/乘法/覆盖）。</param>
        /// <param name="value">加成数值。</param>
        /// <param name="source">来源对象（如特定的 Buff 实例）。</param>
        public AttributeModifier(ModifierType type, float value, ModifierSource source = null)
        {
            Type = type;
            Value = value;
            Source = source;
        }
    }
}