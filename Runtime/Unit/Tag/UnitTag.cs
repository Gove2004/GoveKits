using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 框架下的 string。
    /// 采用静态池化策略：相同字符串返回同一实例（String Pool），ReferenceEquals 为 true。
    /// 作为 class，引用相等天然成立，无需缓存 hash。
    /// </summary>
    public sealed class UnitTag : IEquatable<UnitTag>
    {
        /// <summary>空 Tag，语义等同于"无标签"，用于占位</summary>
        public static readonly UnitTag None = new UnitTag(string.Empty);

        /// <summary>null Tag（等价于 None）</summary>
        public static UnitTag Null => None;

        private readonly string _name;

        /// <summary>池化实例数量（调试用）</summary>
        public static int PoolSize => _pool.Count;

        private static readonly Dictionary<string, UnitTag> _pool = new Dictionary<string, UnitTag>(256);

        /// <summary>标签字符串内容</summary>
        public string Name => _name;

        /// <summary>创建一个 UnitTag（自动池化，相同字符串返回同一实例）</summary>
        public UnitTag(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                _name = string.Empty;
                return;
            }

            lock (_pool)
            {
                if (_pool.TryGetValue(name, out var cached))
                {
                    _name = cached._name;
                    return;
                }

                _name = name;
                _pool[name] = this;
            }
        }

        /// <summary>隐式从 string 转换为 UnitTag</summary>
        public static implicit operator UnitTag(string name) => new UnitTag(name);

        /// <summary>隐式从 UnitTag 转换为 string</summary>
        public static implicit operator string(UnitTag tag) => tag?._name ?? string.Empty;

        /// <summary>相等比较运算符</summary>
        public static bool operator ==(UnitTag a, UnitTag b) => ReferenceEquals(a, b);

        /// <summary>不等比较运算符</summary>
        public static bool operator !=(UnitTag a, UnitTag b) => !ReferenceEquals(a, b);

        /// <summary>与另一个 UnitTag 引用相等</summary>
        public bool Equals(UnitTag other) => ReferenceEquals(this, other);

        /// <summary>与另一个对象引用相等</summary>
        public override bool Equals(object obj) => ReferenceEquals(this, obj);

        /// <summary>哈希码委托给底层 string</summary>
        public override int GetHashCode() => _name?.GetHashCode() ?? 0;

        /// <summary>返回标签字符串表示</summary>
        public override string ToString() => _name ?? "None";
    }
}
