using System;
using System.Runtime.CompilerServices;

namespace GoveKits.Runtime.Architecture
{
    public readonly struct Entity : IEquatable<Entity>
    {
        public readonly int Id;        // 索引
        public readonly ushort Gen;    // 代数（版本）
        public readonly ushort World;  // 多World支持

        public Entity(int id, ushort gen, ushort world = 0)
        {
            Id = id;
            Gen = gen;
            World = world;
        }

        public static Entity Null => new Entity(-1, 0);
        public bool IsNull => Id < 0;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Entity other) => Id == other.Id && Gen == other.Gen && World == other.World;
        
        public override bool Equals(object obj) => obj is Entity e && Equals(e);
        public override int GetHashCode() => Id ^ (Gen << 16) ^ (World << 24);
        public override string ToString() => $"E{Id}:{Gen}";
        
        public static bool operator ==(Entity a, Entity b) => a.Equals(b);
        public static bool operator !=(Entity a, Entity b) => !a.Equals(b);
    }

    // Entity元数据（紧凑存储）
    internal struct EntityMeta
    {
        public ushort Gen;      // 当前代数
        public ushort Flags;    // 标记位
        public int NextFree;    // 空闲链表下一个
        
        public const ushort FLAG_ALIVE = 1;
        
        public bool IsAlive => (Flags & FLAG_ALIVE) != 0;
    }
}