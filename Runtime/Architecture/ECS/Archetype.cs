using System;
using System.Runtime.CompilerServices;

namespace GoveKits.Runtime.Architecture
{
    // 原型ID：基于组件组合的唯一标识
    public readonly struct Archetype : IEquatable<Archetype>
    {
        public readonly ulong Bits0;  // 支持128种组件
        public readonly ulong Bits1;

        public Archetype(ulong b0 = 0, ulong b1 = 0)
        {
            Bits0 = b0;
            Bits1 = b1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(Archetype other) => (Bits0 & other.Bits0) == other.Bits0 && (Bits1 & other.Bits1) == other.Bits1;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasAny(Archetype other) => (Bits0 & other.Bits0) != 0 || (Bits1 & other.Bits1) != 0;

        public bool Equals(Archetype other) => Bits0 == other.Bits0 && Bits1 == other.Bits1;
        public override int GetHashCode() => (int)(Bits0 ^ (Bits1 >> 32));
        
        public static Archetype operator |(Archetype a, ComponentType t) => t.Id < 64 
            ? new Archetype(a.Bits0 | (1UL << t.Id), a.Bits1) 
            : new Archetype(a.Bits0, a.Bits1 | (1UL << (t.Id - 64)));
        
        public static Archetype operator &(Archetype a, Archetype b) => new Archetype(a.Bits0 & b.Bits0, a.Bits1 & b.Bits1);
        public static Archetype operator ~(Archetype a) => new Archetype(~a.Bits0, ~a.Bits1);
    }
}