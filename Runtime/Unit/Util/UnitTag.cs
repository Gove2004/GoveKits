using System;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 框架下基于字符串语义的检索额。
    /// <para>
    /// 相比直接使用 string，<see cref="UnitTag"/> 在实例化时预计算并缓存哈希値，
    /// 从而大幅减少字典查找和比较时的计算开销。
    /// </para>
    /// </summary>
    public readonly struct UnitTag : IEquatable<UnitTag>
    {
        /// <summary>
        /// 空 Tag，语义等同于"无标签"。
        /// 建议仅用于占位，不作为业务有效标签。
        /// </summary>
        public static readonly UnitTag None = new UnitTag(string.Empty);

        /// <summary>构造时预计算并缓存的哈希値，用于字典快速索引。</summary>
        private readonly int _hash;
        /// <summary>原始字符串名称。</summary>
        private readonly string _name;

        /// <summary>
        /// 创建一个 <see cref="UnitTag"/>。
        /// </summary>
        /// <param name="name">Tag 名称。为 null 或空字符串时与 <see cref="None"/> 语义相同。</param>
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


        #region 核心：隐式转换与操作符

        /// <summary>允许直接用字符串字面量赋値给 <see cref="UnitTag"/>。</summary>
        public static implicit operator UnitTag(string name) => new UnitTag(name);
        /// <summary>允许将 <see cref="UnitTag"/> 隐式转换回字符串。</summary>
        public static implicit operator string(UnitTag tag) => tag._name;
        /// <summary>比较两个 Tag 是否相等（先哈希，再比名称）。</summary>
        public static bool operator ==(UnitTag a, UnitTag b) => a.Equals(b);
        /// <summary>比较两个 Tag 是否不等。</summary>
        public static bool operator !=(UnitTag a, UnitTag b) => !a.Equals(b);

        #endregion

        #region 核心：字典性能优化 (IEquatable)

        /// <summary>强类型等値比较，避免装笱开销。</summary>
        public bool Equals(UnitTag other) => _hash == other._hash && _name == other._name;
        /// <summary>兼容 object 等値比较。</summary>
        public override bool Equals(object obj) => obj is UnitTag other && Equals(other);
        /// <summary>返回构造时预计算的哈希値。</summary>
        public override int GetHashCode() => _hash;
        /// <summary>返回 Tag 的原始字符串名称。</summary>
        public override string ToString() => _name ?? "None";

        #endregion
    }
}