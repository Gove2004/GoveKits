using System;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 框架下基于字符串语义的检索标签。
    /// <para>
    /// 相比直接使用 string，UnitTag 在实例化时预计算并缓存哈希值，
    /// 从而大幅减少字典查找和比较时的计算开销。
    /// </para>
    /// </summary>
    public readonly struct UnitTag : IEquatable<UnitTag>
    {
        /// <summary>
        /// 空 Tag，语义等同于"无标签"，用于占位。
        /// </summary>
        public static readonly UnitTag None = new UnitTag(string.Empty);

        private readonly int _hash;
        private readonly string _name;

        /// <summary>
        /// 创建一个 UnitTag。
        /// </summary>
        /// <param name="name">Tag 名称。为 null 或空字符串时与 None 语义相同。</param>
        public UnitTag(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                _name = string.Empty;
                _hash = 0;
                return;
            }
            _name = name;
            _hash = name.GetHashCode();
        }

        #region 隐式转换与操作符

        public static implicit operator UnitTag(string name) => new UnitTag(name);
        public static implicit operator string(UnitTag tag) => tag._name;
        
        public static bool operator ==(UnitTag a, UnitTag b) => a.Equals(b);
        public static bool operator !=(UnitTag a, UnitTag b) => !a.Equals(b);

        #endregion

        #region 字典性能优化 (IEquatable)

        public bool Equals(UnitTag other) => _hash == other._hash && _name == other._name;
        public override bool Equals(object obj) => obj is UnitTag other && Equals(other);
        public override int GetHashCode() => _hash;
        public override string ToString() => _name ?? "None";

        #endregion
    }
}