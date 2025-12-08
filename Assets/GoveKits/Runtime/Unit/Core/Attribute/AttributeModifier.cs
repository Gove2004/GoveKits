using System;

namespace GoveKits.Unit
{
    /// <summary>
    /// 修改器类型 (决定计算顺序)
    /// </summary>
    public enum ModifierType
    {
        /// <summary> 固定加值 (Base + Flat) </summary>
        Flat = 0,
        /// <summary> 百分比叠加 (* (1 + Sum)) </summary>
        PercentAdd = 1,
        /// <summary> 独立乘区 (* Mult1 * Mult2) </summary>
        PercentMult = 2,
        /// <summary> 绝对覆写 (强制设为 X) </summary>
        Override = 3
    }

    /// <summary>
    /// 属性修改器 (零GC结构体)
    /// </summary>
    public readonly struct GameModifier : IEquatable<GameModifier>
    {
        public readonly ModifierType Type;
        public readonly float Value;
        public readonly object Source; // 来源 (Buff/Equipment/Skill)

        /// <summary>
        /// 构造一个 <see cref="GameModifier"/>。
        /// </summary>
        /// <param name="type">修改器类型（Flat / PercentAdd / PercentMult / Override）</param>
        /// <param name="value">修改值</param>
        /// <param name="source">来源对象（可用于按来源移除）</param>
        public GameModifier(ModifierType type, float value, object source = null)
        {
            Type = type;
            Value = value;
            Source = source;
        }

        /// <summary>
        /// 值相等性判定（用于从列表中查找并移除）。
        /// </summary>
        public bool Equals(GameModifier other)
        {
            return Type == other.Type && 
                   Math.Abs(Value - other.Value) < 1e-5f && 
                   Source == other.Source;
        }

        /// <summary>调试友好的文本表示。</summary>
        public override string ToString() => $"[{Type} {Value}]";
    }
}