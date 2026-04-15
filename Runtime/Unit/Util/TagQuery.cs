using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 提供标签查询匹配源的统一接口。
    /// （Unit 的 MarkContainer 会自动实现它，暴露给技能系统用于前置条件查询）。
    /// </summary>
    public interface ITagSource
    {
        bool HasTag(UnitTag tag);
    }

    #region 核心查询基类

    /// <summary>
    /// 标签匹配树的抽象基类（类似行为树的条件节点）。
    /// <para>通过将多个简单的 TagQuery 进行组合（与、或、非），可以构造出极度复杂的技能前置释放条件。</para>
    /// <example>
    /// // 示例：目标必须没有免疫标记，且必须处于(中毒或流血)状态之一
    /// TagQuery condition = !TagQuery.Has("Buff_Immune") & (TagQuery.Has("Debuff_Poison") | TagQuery.Has("Debuff_Bleed"));
    /// </example>
    /// </summary>
    public abstract class TagQuery
    {
        /// <summary>针对传入的标签源，执行这棵条件判断树。</summary>
        public abstract bool Match(ITagSource container);

        #region 运算符重载 (极客级语法糖)

        // 允许直接隐式转换：TagQuery q = "Stunned";
        public static implicit operator TagQuery(string tagName) => new HasTag(tagName);
        public static implicit operator TagQuery(UnitTag tag) => new HasTag(tag);

        // 允许直接使用布尔操作符：!query, query1 & query2, query1 | query2
        public static TagQuery operator !(TagQuery query) => new NotTag(query);
        public static TagQuery operator &(TagQuery left, TagQuery right) => new AllTag(left, right);
        public static TagQuery operator |(TagQuery left, TagQuery right) => new AnyTag(left, right);

        #endregion

        #region 静态组合工厂方法

        public static TagQuery Has(UnitTag tag) => new HasTag(tag);
        public static TagQuery Custom(Func<ITagSource, bool> func) => new ConditionTag(func);
        public static TagQuery All(params TagQuery[] queries) => new AllTag(queries);
        public static TagQuery Any(params TagQuery[] queries) => new AnyTag(queries);
        public static TagQuery Not(TagQuery query) => new NotTag(query);

        #endregion
    }

    #endregion

    #region 内部具体树节点实现

    public class HasTag : TagQuery
    {
        public readonly UnitTag Tag;
        public HasTag(UnitTag tag) { Tag = tag; }
        
        public override bool Match(ITagSource container) => container != null && container.HasTag(Tag);
        public override string ToString() => $"({Tag})";
    }

    public class NotTag : TagQuery
    {
        private readonly TagQuery _query;
        public NotTag(TagQuery query) { _query = query ?? throw new ArgumentNullException(nameof(query)); }
        
        public override bool Match(ITagSource container) => !_query.Match(container);
        public override string ToString() => $"!({_query})";
    }

    public class AllTag : TagQuery
    {
        private readonly TagQuery[] _queries;
        public AllTag(params TagQuery[] queries)
        {
            var valid = new List<TagQuery>();
            if (queries != null) foreach (var q in queries) if (q != null) valid.Add(q);
            _queries = valid.ToArray();
        }

        public override bool Match(ITagSource container)
        {
            for (int i = 0; i < _queries.Length; i++)
            {
                if (!_queries[i].Match(container)) return false; // 短路求值
            }
            return true;
        }
        public override string ToString() => $"({string.Join(" & ", (IEnumerable<TagQuery>)_queries)})";
    }

    public class AnyTag : TagQuery
    {
        private readonly TagQuery[] _queries;
        public AnyTag(params TagQuery[] queries)
        {
            var valid = new List<TagQuery>();
            if (queries != null) foreach (var q in queries) if (q != null) valid.Add(q);
            _queries = valid.ToArray();
        }

        public override bool Match(ITagSource container)
        {
            for (int i = 0; i < _queries.Length; i++)
            {
                if (_queries[i].Match(container)) return true; // 短路求值
            }
            return false;
        }
        public override string ToString() => $"({string.Join(" | ", (IEnumerable<TagQuery>)_queries)})";
    }

    public class ConditionTag : TagQuery
    {
        private readonly Func<ITagSource, bool> _func;
        public ConditionTag(Func<ITagSource, bool> func) { _func = func ?? throw new ArgumentNullException(nameof(func)); }
        
        public override bool Match(ITagSource container) => _func(container);
    }

    #endregion
}