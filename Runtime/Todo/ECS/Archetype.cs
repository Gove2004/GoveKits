using System;
using System.Runtime.CompilerServices;

namespace GoveKits.Runtime.Architecture
{
    /// <summary>
    /// 原型（Archetype）标识，基于组件组合的位掩码。
    /// 使用两个 ulong 支持最多 128 种不同的组件类型。
    /// 用于 ECS 查询匹配和增量更新。
    /// </summary>
    public readonly struct Archetype : IEquatable<Archetype>
    {
        /// <summary>低位位掩码，支持前 64 种组件类型。</summary>
        public readonly ulong Bits0;  // 支持128种组件
        /// <summary>高位位掩码，支持后 64 种组件类型。</summary>
        public readonly ulong Bits1;

        /// <summary>
        /// 创建原型标识。
        /// </summary>
        /// <param name="b0">低位位掩码。</param>
        /// <param name="b1">高位位掩码。</param>
        public Archetype(ulong b0 = 0, ulong b1 = 0)
        {
            Bits0 = b0;
            Bits1 = b1;
        }

        /// <summary>
        /// 检查当前原型是否包含指定原型的所有组件（子集匹配）。
        /// </summary>
        /// <param name="other">要检查的子原型。</param>
        /// <returns>如果当前原型包含 other 的所有组件则返回 true。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(Archetype other) => (Bits0 & other.Bits0) == other.Bits0 && (Bits1 & other.Bits1) == other.Bits1;

        /// <summary>
        /// 检查当前原型是否与指定原型有任意组件重叠。
        /// </summary>
        /// <param name="other">要检查的重叠原型。</param>
        /// <returns>如果有至少一个共同组件则返回 true。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasAny(Archetype other) => (Bits0 & other.Bits0) != 0 || (Bits1 & other.Bits1) != 0;

        /// <summary>判断两个原型标识是否完全相同。</summary>
        public bool Equals(Archetype other) => Bits0 == other.Bits0 && Bits1 == other.Bits1;
        /// <summary>返回哈希码，基于两个位掩码的异或运算。</summary>
        public override int GetHashCode() => (int)(Bits0 ^ (Bits1 >> 32));

        /// <summary>
        /// 将组件类型添加到原型中。
        /// </summary>
        /// <param name="a">源原型。</param>
        /// <param name="t">要添加的组件类型。</param>
        /// <returns>包含新组件的新原型。</returns>
        public static Archetype operator |(Archetype a, ComponentType t) => t.Id < 64
            ? new Archetype(a.Bits0 | (1UL << t.Id), a.Bits1)
            : new Archetype(a.Bits0, a.Bits1 | (1UL << (t.Id - 64)));

        /// <summary>
        /// 计算两个原型的交集（共同组件）。
        /// </summary>
        /// <param name="a">原型 A。</param>
        /// <param name="b">原型 B。</param>
        /// <returns>两个原型共有的组件集合。</returns>
        public static Archetype operator &(Archetype a, Archetype b) => new Archetype(a.Bits0 & b.Bits0, a.Bits1 & b.Bits1);

        /// <summary>
        /// 取反操作，生成包含所有未在当前原型中出现的组件的原型。
        /// </summary>
        /// <param name="a">源原型。</param>
        /// <returns>位掩码取反后的原型。</returns>
        public static Archetype operator ~(Archetype a) => new Archetype(~a.Bits0, ~a.Bits1);
    }
}