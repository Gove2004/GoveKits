using System;

namespace GoveKits.Unit
{
    /// <summary>
    /// 游戏标签：字符串的高性能封装，用于标识状态、伤害类型、能力等。
    /// <para>特点：不可变值类型（struct），创建时缓存字符串的 HashCode（Id），
    /// 作为字典键时性能等同于使用 int，大幅减少运行时分配与字符串比较开销。</para>
    /// <para>支持与字符串的隐式转换，允许便捷的 API 调用（如 "Fire" 自动转 GameTag）。</para>
    /// </summary>
    public readonly struct GameTag : IEquatable<GameTag>
    {
        /// <summary>
        /// 预计算的哈希值（在构造时计算一次）。
        /// <para>用于作为字典键的高速比较与哈希，此字段为只读。</para>
        /// </summary>
        public readonly int Id;

        /// <summary>
        /// 原始字符串，仅用于调试与 ToString。不要在热路径频繁访问它以避免分配/比较负担。
        /// </summary>
        private readonly string _name;

        /// <summary>
        /// 空标签（等价于 string.Empty）。便于避免 null 检查。
        /// </summary>
        public static readonly GameTag None = new GameTag(string.Empty);

        /// <summary>
        /// 构造函数：为给定字符串创建一个 <see cref="GameTag"/>，并缓存其 HashCode。
        /// </summary>
        /// <param name="name">标签名称，可为空。</param>
        public GameTag(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                _name = string.Empty;
                Id = 0;
            }
            else
            {
                _name = name;
                // 核心：在创建时就只算一次 Hash。
                // 之后放入 Dictionary 时，直接取这个 int，无需再次遍历字符串计算 Hash。
                Id = name.GetHashCode(); 
            }
        }

        #region 核心：隐式转换与操作符

        /// <summary>
        /// 隐式从字符串转换为 <see cref="GameTag"/>。
        /// <para>允许写法：<c>GameTag tag = "Fire";</c></para>
        /// </summary>
        public static implicit operator GameTag(string name) => new GameTag(name);

        /// <summary>
        /// 隐式将 <see cref="GameTag"/> 转换为字符串（返回内部 _name）。谨慎使用以避免意外分配。
        /// </summary>
        public static implicit operator string(GameTag tag) => tag._name;

        public static bool operator ==(GameTag a, GameTag b) => a.Id == b.Id;
        public static bool operator !=(GameTag a, GameTag b) => a.Id != b.Id;

        #endregion

        #region 核心：字典性能优化 (IEquatable)

        /// <summary>
        /// 实现 <see cref="IEquatable{GameTag}"/>，用于高性能比较，避免装箱。
        /// </summary>
        public bool Equals(GameTag other)
        {
            return this.Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is GameTag other && Equals(other);
        }

        /// <summary>
        /// 返回缓存的 Hash 值（Id）。用于字典查找，性能优于每次计算字符串哈希。
        /// </summary>
        public override int GetHashCode()
        {
            return Id;
        }

        #endregion

        public override string ToString()
        {
            return string.IsNullOrEmpty(_name) ? "None" : _name;
        }
        
        /// <summary>
        /// 检查标签是否合法（非空标签的 Id 不为 0）。
        /// </summary>
        public bool IsValid => Id != 0;
    }
}