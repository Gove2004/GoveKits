using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 表示能够提供标签查询源的接口。
    /// 例如：单位的 MarkContainer 可以实现此接口以支持 TagQuery 的匹配。
    /// </summary>
    public interface IUnitTagSource
    {
        /// <summary>
        /// 检查是否包含指定标签。
        /// </summary>
        bool HasTag(UnitTag tag);
    }


    #region 核心查询基类

    /// <summary>
    /// 标签匹配查询基类。通过组合节点可以构造复杂的匹配逻辑（与/或/非/自定义）。
    /// 支持隐式从 string 或 UnitTag 的转换，以及常用运算符重载以便于表达式式编写查询条件。
    /// </summary>
    public abstract class TagQuery
    {
        /// <summary>
        /// 判断给定的标签源是否满足此查询条件。
        /// </summary>
        public abstract bool Match(IUnitTagSource container);

        #region 运算符重载 & 隐式转换 (语法糖)

        // 允许写法: TagQuery q = "Stunned";
        public static implicit operator TagQuery(string tagName)
            => new HasTag(tagName);

        // 允许写法: TagQuery q = unitTag;
        public static implicit operator TagQuery(UnitTag tag)
            => new HasTag(tag);

        // 允许写法: !query
        public static TagQuery operator !(TagQuery query)
            => new NotTag(query);

        // 允许写法: query1 & query2
        public static TagQuery operator &(TagQuery left, TagQuery right)
            => new AllTag(left, right);

        // 允许写法: query1 | query2
        public static TagQuery operator |(TagQuery left, TagQuery right)
            => new AnyTag(left, right);

        #endregion

        #region 静态构建方法 (工厂)

        /// <summary>
        /// 使用自定义条件构建查询节点。
        /// </summary>
        /// <param name="func">匹配函数。</param>
        /// <returns>自定义条件查询节点。</returns>
        public static TagQuery Custom(Func<IUnitTagSource, bool> func) => new ConditionTag(func);

        /// <summary>
        /// 构建“全部满足”查询。
        /// </summary>
        /// <param name="queries">子查询列表。</param>
        /// <returns>AND 查询节点。</returns>
        public static TagQuery All(params TagQuery[] queries) => new AllTag(queries);

        /// <summary>
        /// 构建“任一满足”查询。
        /// </summary>
        /// <param name="queries">子查询列表。</param>
        /// <returns>OR 查询节点。</returns>
        public static TagQuery Any(params TagQuery[] queries) => new AnyTag(queries);

        /// <summary>
        /// 构建“取反”查询。
        /// </summary>
        /// <param name="query">子查询。</param>
        /// <returns>NOT 查询节点。</returns>
        public static TagQuery Not(TagQuery query) => new NotTag(query);

        #endregion
    }

    #endregion

    #region 具体节点实现

    /// <summary>
    /// 基础节点：检查是否拥有某个 Tag
    /// </summary>
    public class HasTag : TagQuery
    {
        /// <summary>
        /// 目标标签。
        /// </summary>
        public readonly UnitTag Tag;

        /// <summary>
        /// 创建 HasTag 查询节点。
        /// </summary>
        /// <param name="tag">要匹配的标签。</param>
        public HasTag(UnitTag tag)
        {
            Tag = tag;
        }

        public override bool Match(IUnitTagSource container)
        {
            return container != null && container.HasTag(Tag);
        }

        public override string ToString() => $"Has({Tag})";
    }

    /// <summary>
    /// 逻辑非节点 (NOT)
    /// </summary>
    public class NotTag : TagQuery
    {
        private readonly TagQuery _query;

        /// <summary>
        /// 创建 NOT 查询节点。
        /// </summary>
        /// <param name="query">被取反的子查询。</param>
        public NotTag(TagQuery query)
        {
            _query = query ?? throw new ArgumentNullException(nameof(query));
        }

        public override bool Match(IUnitTagSource container)
        {
            return !_query.Match(container);
        }

        public override string ToString() => $"!({_query})";
    }

    /// <summary>
    /// 逻辑与节点 (AND)
    /// </summary>
    public class AllTag : TagQuery
    {
        private readonly TagQuery[] _queries;

        /// <summary>
        /// 创建 AND 查询节点。
        /// </summary>
        /// <param name="queries">子查询列表。</param>
        public AllTag(params TagQuery[] queries)
        {
            _queries = queries ?? Array.Empty<TagQuery>();
        }

        public override bool Match(IUnitTagSource container)
        {
            for (int i = 0; i < _queries.Length; i++)
            {
                if (_queries[i] == null)
                {
                    continue;
                }

                if (!_queries[i].Match(container))
                {
                    return false;
                }
            }

            return true;
        }

        public override string ToString() => $"({string.Join(" & ", (IEnumerable<TagQuery>)_queries)})";
    }

    /// <summary>
    /// 逻辑或节点 (OR)
    /// </summary>
    public class AnyTag : TagQuery
    {
        private readonly TagQuery[] _queries;

        /// <summary>
        /// 创建 OR 查询节点。
        /// </summary>
        /// <param name="queries">子查询列表。</param>
        public AnyTag(params TagQuery[] queries)
        {
            _queries = queries ?? Array.Empty<TagQuery>();
        }

        public override bool Match(IUnitTagSource container)
        {
            for (int i = 0; i < _queries.Length; i++)
            {
                if (_queries[i] == null)
                {
                    continue;
                }

                if (_queries[i].Match(container))
                {
                    return true;
                }
            }

            return false;
        }

        public override string ToString() => $"({string.Join(" | ", (IEnumerable<TagQuery>)_queries)})";
    }

    /// <summary>
    /// 自定义条件节点 (Func)
    /// </summary>
    public class ConditionTag : TagQuery
    {
        private readonly Func<IUnitTagSource, bool> _func;

        /// <summary>
        /// 创建自定义条件查询节点。
        /// </summary>
        /// <param name="func">匹配函数。</param>
        public ConditionTag(Func<IUnitTagSource, bool> func)
        {
            _func = func ?? throw new ArgumentNullException(nameof(func));
        }

        public override bool Match(IUnitTagSource container)
        {
            return _func(container);
        }
    }

    #endregion
}