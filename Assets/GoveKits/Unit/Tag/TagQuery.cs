using System;
using System.Collections.Generic;
using System.Linq;

namespace GoveKits.Units
{
    // 标签查询接口，实现可组合的标签查询条件
    public interface ITagQuery
    {
        bool Matches(GameplayTagContainer container);
    }

    // 基础标签条件
    public class HasTag : ITagQuery
    {
        private readonly string _tagName;

        public HasTag(string tagName)
        {
            _tagName = tagName ?? throw new ArgumentNullException(nameof(tagName));
        }

        public bool Matches(GameplayTagContainer container)
        {
            return container.HasTag(_tagName);
        }
    }

    // 接受一个Func<bool, string>用于生成动态条件
    public class Condition : ITagQuery
    {
        private readonly Func<string, bool> _func;
        private readonly string _tagName;

        public Condition(Func<string, bool> func, string tagName)
        {
            _func = func ?? throw new ArgumentNullException(nameof(func));
            _tagName = tagName ?? throw new ArgumentNullException(nameof(tagName));
        }

        public bool Matches(GameplayTagContainer container) => _func(_tagName);
    }

    // 所有条件都必须满足
    public class All : ITagQuery
    {
        private readonly List<ITagQuery> _conditions;

        public All(params ITagQuery[] conditions)
        {
            _conditions = conditions?.ToList() ?? throw new ArgumentNullException(nameof(conditions));
        }

        public bool Matches(GameplayTagContainer container)
        {
            return _conditions.All(cond => cond.Matches(container));
        }
    }

    // 任意一个条件满足即可
    public class Any : ITagQuery
    {
        private readonly List<ITagQuery> _conditions;

        public Any(params ITagQuery[] conditions)
        {
            _conditions = conditions?.ToList() ?? throw new ArgumentNullException(nameof(conditions));
        }

        public bool Matches(GameplayTagContainer container)
        {
            return _conditions.Any(cond => cond.Matches(container));
        }
    }

    // 条件不满足
    public class None : ITagQuery
    {
        private readonly ITagQuery _condition;

        public None(ITagQuery condition)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        public bool Matches(GameplayTagContainer container)
        {
            return !_condition.Matches(container);
        }
    }

    // 扩展查询条件
    public class AtLeast : ITagQuery
    {
        private readonly int _count;
        private readonly List<ITagQuery> _conditions;

        public AtLeast(int count, params ITagQuery[] conditions)
        {
            _count = count;
            _conditions = conditions?.ToList() ?? throw new ArgumentNullException(nameof(conditions));
        }

        public bool Matches(GameplayTagContainer container)
        {
            int matchCount = _conditions.Count(cond => cond.Matches(container));
            return matchCount >= _count;
        }
    }


    // 流畅的构建器类 - 简化版本
    public static class T
    {
        // 基础条件（使用隐式转换）
        public static ITagQuery Has(string tag) => new HasTag(tag);
        public static ITagQuery Condition(Func<string, bool> func, string tagName)
            => new Condition(func, tagName);

        // 组合条件（支持字符串和ITagQuery混合参数）
        public static ITagQuery All(params object[] conditions) => new All(ConvertToQueries(conditions));
        public static ITagQuery Any(params object[] conditions) => new Any(ConvertToQueries(conditions));
        public static ITagQuery None(object condition) => new None(ConvertToQuery(condition));
        public static ITagQuery AtLeast(int count, params object[] conditions) => new AtLeast(count, ConvertToQueries(conditions));
        
        private static ITagQuery[] ConvertToQueries(object[] conditions)
        {
            return conditions.Select(ConvertToQuery).ToArray();
        }
        
        private static ITagQuery ConvertToQuery(object condition)
        {
            return condition switch
            {
                ITagQuery query => query,
                string tag => new HasTag(tag),
                _ => throw new ArgumentException($"不支持的类型: {condition.GetType()}")
            };
        }
    }
}