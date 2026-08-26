using System;
using System.Runtime.CompilerServices;

namespace GoveKits.Runtime.Architecture
{
    /// <summary>
    /// ECS 实体标识，使用 ID + 代数（Generation）机制防止悬空引用。
    /// 实体被销毁后代数递增，旧引用因代数不匹配而失效。
    /// 支持多 World 隔离。
    /// </summary>
    public readonly struct Entity : IEquatable<Entity>
    {
        /// <summary>实体在 World 中的索引 ID。</summary>
        public readonly int Id;        // 索引
        /// <summary>实体代数，每次重建时递增，用于检测悬空引用。</summary>
        public readonly ushort Gen;    // 代数（版本）
        /// <summary>所属 World 的标识，用于多 World 支持。</summary>
        public readonly ushort World;  // 多World支持

        /// <summary>
        /// 创建实体标识。
        /// </summary>
        /// <param name="id">实体索引。</param>
        /// <param name="gen">实体代数。</param>
        /// <param name="world">所属 World 标识。</param>
        public Entity(int id, ushort gen, ushort world = 0)
        {
            Id = id;
            Gen = gen;
            World = world;
        }

        /// <summary>表示空实体的静态只读实例。</summary>
        public static Entity Null => new Entity(-1, 0);
        /// <summary>判断当前实体是否为空。</summary>
        public bool IsNull => Id < 0;

        /// <summary>判断两个实体标识是否指向同一个有效的实体。</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Entity other) => Id == other.Id && Gen == other.Gen && World == other.World;

        /// <summary>与对象进行比较。</summary>
        public override bool Equals(object obj) => obj is Entity e && Equals(e);
        /// <summary>返回实体标识的哈希码。</summary>
        public override int GetHashCode() => Id ^ (Gen << 16) ^ (World << 24);
        /// <summary>返回实体的字符串表示，格式为 E{Id}:{Gen}。</summary>
        public override string ToString() => $"E{Id}:{Gen}";

        /// <summary>判断两个实体标识是否相等。</summary>
        public static bool operator ==(Entity a, Entity b) => a.Equals(b);
        /// <summary>判断两个实体标识是否不相等。</summary>
        public static bool operator !=(Entity a, Entity b) => !a.Equals(b);
    }

    /// <summary>
    /// 实体的内部元数据，紧凑存储在 World 中。
    /// 包含代数、存活标志和空闲链表指针。
    /// </summary>
    // Entity元数据（紧凑存储）
    internal struct EntityMeta
    {
        /// <summary>当前代数，每次实体重建时递增。</summary>
        public ushort Gen;      // 当前代数
        /// <summary>标记位，FLAG_ALIVE 表示实体处于存活状态。</summary>
        public ushort Flags;    // 标记位
        /// <summary>空闲链表中的下一个空闲实体索引。</summary>
        public int NextFree;    // 空闲链表下一个

        /// <summary>存活标志常量。</summary>
        public const ushort FLAG_ALIVE = 1;

        /// <summary>判断实体是否存活。</summary>
        public bool IsAlive => (Flags & FLAG_ALIVE) != 0;
    }
}