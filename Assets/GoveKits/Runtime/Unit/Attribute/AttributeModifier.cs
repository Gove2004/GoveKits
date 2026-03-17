


namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 修改器类型。
    /// </summary>
    public enum ModifierType
    {
        Additive,
        Multiplicative,
        Override
    }


    /// <summary>
    /// 修改器来源基类（例如装备、Buff、技能）。
    /// 用于追踪来源以及做批量移除。
    /// </summary>
    public abstract class ModifierSource
    {
        
    }


    /// <summary>
    /// 属性修改器。
    /// </summary>
    public struct AttributeModifier
    {
        /// <summary>来源，例如装备、Buff、技能等。</summary>
        public readonly ModifierSource Source { get; }
        /// <summary>加成类型。</summary>
        public readonly ModifierType Type { get; }
        /// <summary>加成数值。</summary>
        public readonly float Value { get; }

        /// <summary>
        /// 创建一个属性修改器。
        /// </summary>
        /// <param name="type">修改器类型。</param>
        /// <param name="value">修改器数值。</param>
        /// <param name="source">修改器来源，可为空。</param>
        public AttributeModifier(ModifierType type, float value, ModifierSource source = null)
        {
            Type = type;
            Value = value;
            Source = source;
        }

        /// <summary>
        /// 将当前修改器应用到输入值。
        /// </summary>
        public float Apply(float baseValue)
        {
            return Type switch
            {
                ModifierType.Additive => baseValue + Value,
                ModifierType.Multiplicative => baseValue * (1 + Value),
                ModifierType.Override => Value,
                _ => baseValue
            };
        }
    }


    
}