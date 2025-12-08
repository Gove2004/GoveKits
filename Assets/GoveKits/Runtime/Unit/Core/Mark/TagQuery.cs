using System;
using System.Collections.Generic;

namespace GoveKits.Unit
{
    /// <summary>
    /// 表示能够提供标签查询源的接口。
    /// <para>例如：单位的 MarkContainer 可以实现此接口以支持 <see cref="TagQuery"/> 的匹配。</para>
    /// </summary>
    public interface IGameTagSource
    {
        /// <summary>
        /// 检查是否包含指定标签。
        /// </summary>
        bool HasTag(GameTag tag);
    }


    #region 核心查询基类

    /// <summary>
    /// 标签匹配查询基类。通过组合节点可以构造复杂的匹配逻辑（与/或/非/自定义）。
    /// <para>支持隐式从字符串或 <see cref="GameTag"/> 的转换，以及常用运算符重载以便于表达式式编写查询条件。</para>
    /// </summary>
    public abstract class TagQuery
    {
        // 核心匹配逻辑
        /// <summary>
        /// 判断给定的标签源是否满足此查询条件。
        /// </summary>
        public abstract bool Match(IGameTagSource container);

        #region 运算符重载 & 隐式转换 (语法糖)

        // 1. 隐式将 string 转换为 HasTag 查询
        // 允许写法: GameQuery q = "Stunned";
        public static implicit operator TagQuery(string tagName) 
            => new HasTag(tagName);

        // 2. 隐式将 GameTag 转换为 HasTag 查询
        public static implicit operator TagQuery(GameTag tag) 
            => new HasTag(tag);

        // 3. 重载 ! (逻辑非 / 禁止)
        // 允许写法: !query
        public static TagQuery operator !(TagQuery query) 
            => new NotTag(query);

        // 4. 重载 & (逻辑与 / 必须同时满足)
        // 允许写法: query1 & query2
        public static TagQuery operator &(TagQuery left, TagQuery right) 
            => new AllTag(left, right);

        // 5. 重载 | (逻辑或 / 满足其一即可)
        // 允许写法: query1 | query2
        public static TagQuery operator |(TagQuery left, TagQuery right) 
            => new AnyTag(left, right);

        #endregion

        #region 静态构建方法 (工厂)
        
        // 允许自定义复杂逻辑，例如检查层数
        public static TagQuery Custom(Func<IGameTagSource, bool> func) => new ConditionTag(func);
        
        public static TagQuery All(params TagQuery[] queries) => new AllTag(queries);
        public static TagQuery Any(params TagQuery[] queries) => new AnyTag(queries);
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
        public readonly GameTag Tag;

        public HasTag(GameTag tag)
        {
            Tag = tag;
        }

        public override bool Match(IGameTagSource container)
        {
            // 极速查找：利用 GameTag 的 Int ID 和 Dictionary 的 O(1)
            return container.HasTag(Tag);
        }

        public override string ToString() => $"Has({Tag})";
    }

    /// <summary>
    /// 逻辑非节点 (NOT)
    /// </summary>
    public class NotTag : TagQuery
    {
        private readonly TagQuery _query;

        public NotTag(TagQuery query)
        {
            _query = query ?? throw new ArgumentNullException(nameof(query));
        }

        public override bool Match(IGameTagSource container)
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

        public AllTag(params TagQuery[] queries)
        {
            _queries = queries ?? Array.Empty<TagQuery>();
        }

        public override bool Match(IGameTagSource container)
        {
            // 优化：使用 for 循环代替 LINQ 的 All，避免 Delegate 分配
            for (int i = 0; i < _queries.Length; i++)
            {
                if (!_queries[i].Match(container)) return false;
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

        public AnyTag(params TagQuery[] queries)
        {
            _queries = queries ?? Array.Empty<TagQuery>();
        }

        public override bool Match(IGameTagSource container)
        {
            // 优化：使用 for 循环代替 LINQ 的 Any
            for (int i = 0; i < _queries.Length; i++)
            {
                if (_queries[i].Match(container)) return true;
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
        // 优化：直接传入容器接口，而不是 string[]
        // 这样可以避免 ToArray() 的内存分配，并支持检查 Stacks 等高级功能
        private readonly Func<IGameTagSource, bool> _func;

        public ConditionTag(Func<IGameTagSource, bool> func)
        {
            _func = func ?? throw new ArgumentNullException(nameof(func));
        }

        public override bool Match(IGameTagSource container)
        {
            return _func(container);
        }
    }

    #endregion
}