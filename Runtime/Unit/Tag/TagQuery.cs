using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 标签匹配树抽象基类（类似行为树的条件节点）。
    /// 通过将多个简单的 TagQuery 进行组合（与、或、非），可以构造出极度复杂的技能前置释放条件。
    /// </summary>
    /// <example>
    /// TagQuery condition = !TagQuery.Has("Buff_Immune") & (TagQuery.Has("Debuff_Poison") | TagQuery.Has("Debuff_Bleed"));
    /// </example>
    public abstract class TagQuery
    {
        /// <summary>针对传入的标签源，执行这棵条件判断树</summary>
        public abstract bool Match(ITagSource container);

        #region 运算符重载

        /// <summary>隐式转换为 HasTag 查询</summary>
        public static implicit operator TagQuery(string tagName) => new HasTag(tagName);

        /// <summary>隐式转换为 HasTag 查询</summary>
        public static implicit operator TagQuery(UnitTag tag) => new HasTag(tag);

        /// <summary>逻辑非运算，生成取反查询树</summary>
        public static TagQuery operator !(TagQuery query) => new NotTag(query);

        /// <summary>逻辑与运算，生成全部满足查询树</summary>
        public static TagQuery operator &(TagQuery left, TagQuery right) => new AllTag(left, right);

        /// <summary>逻辑或运算，生成任一满足查询树</summary>
        public static TagQuery operator |(TagQuery left, TagQuery right) => new AnyTag(left, right);

        #endregion

        #region 静态组合工厂方法

        /// <summary>创建一个标签存在性查询</summary>
        public static TagQuery Has(UnitTag tag) => new HasTag(tag);

        /// <summary>创建一个自定义条件查询</summary>
        public static TagQuery Custom(Func<ITagSource, bool> func) => new ConditionTag(func);

        /// <summary>创建一个"全部满足"查询（AND 组合）</summary>
        public static TagQuery All(params TagQuery[] queries) => new AllTag(queries);

        /// <summary>创建一个"任一满足"查询（OR 组合）</summary>
        public static TagQuery Any(params TagQuery[] queries) => new AnyTag(queries);

        /// <summary>创建一个取反查询（NOT）</summary>
        public static TagQuery Not(TagQuery query) => new NotTag(query);

        #endregion
    }

    #region 具体树节点

    /// <summary>标签存在性查询节点</summary>
    public class HasTag : TagQuery
    {
        /// <summary>要查询的标签</summary>
        public readonly UnitTag Tag;
        public HasTag(UnitTag tag) { Tag = tag; }

        /// <summary>检查标签源是否包含指定标签</summary>
        public override bool Match(ITagSource container) => container != null && container.HasTag(Tag);

        /// <summary>返回可读的查询表达式</summary>
        public override string ToString() => $"({Tag})";
    }

    /// <summary>标签取反查询节点</summary>
    public class NotTag : TagQuery
    {
        private readonly TagQuery _query;
        public NotTag(TagQuery query) { _query = query ?? throw new ArgumentNullException(nameof(query)); }

        /// <summary>返回子查询的取反结果</summary>
        public override bool Match(ITagSource container) => !_query.Match(container);

        /// <summary>返回可读的查询表达式</summary>
        public override string ToString() => $"!({_query})";
    }

    /// <summary>全部满足查询节点（AND 组合）</summary>
    public class AllTag : TagQuery
    {
        private readonly TagQuery[] _queries;
        public AllTag(params TagQuery[] queries)
        {
            var valid = new List<TagQuery>();
            if (queries != null) foreach (var q in queries) if (q != null) valid.Add(q);
            _queries = valid.ToArray();
        }

        /// <summary>所有子查询都必须返回 true 才匹配</summary>
        public override bool Match(ITagSource container)
        {
            for (int i = 0; i < _queries.Length; i++)
            {
                if (!_queries[i].Match(container)) return false;
            }
            return true;
        }

        /// <summary>返回可读的查询表达式</summary>
        public override string ToString() => $"({string.Join(" & ", (IEnumerable<TagQuery>)_queries)})";
    }

    /// <summary>任一满足查询节点（OR 组合）</summary>
    public class AnyTag : TagQuery
    {
        private readonly TagQuery[] _queries;
        public AnyTag(params TagQuery[] queries)
        {
            var valid = new List<TagQuery>();
            if (queries != null) foreach (var q in queries) if (q != null) valid.Add(q);
            _queries = valid.ToArray();
        }

        /// <summary>任意一个子查询返回 true 即匹配</summary>
        public override bool Match(ITagSource container)
        {
            for (int i = 0; i < _queries.Length; i++)
            {
                if (_queries[i].Match(container)) return true;
            }
            return false;
        }

        /// <summary>返回可读的查询表达式</summary>
        public override string ToString() => $"({string.Join(" | ", (IEnumerable<TagQuery>)_queries)})";
    }

    /// <summary>自定义条件查询节点</summary>
    public class ConditionTag : TagQuery
    {
        private readonly Func<ITagSource, bool> _func;
        public ConditionTag(Func<ITagSource, bool> func) { _func = func ?? throw new ArgumentNullException(nameof(func)); }

        /// <summary>执行自定义条件函数</summary>
        public override bool Match(ITagSource container) => _func(container);
    }

    #endregion
}
