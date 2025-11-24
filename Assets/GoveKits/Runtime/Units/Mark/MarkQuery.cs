using System;
using System.Collections.Generic;
using System.Linq;

namespace GoveKits.Units
{
    // 标签查询接口，实现可组合的标签查询条件
    public interface IMarkQuery
    {
        bool Match(MarkContainer container);
    }

    // 基础标签条件
    public class Has : IMarkQuery
    {
        private readonly string _MarkName;

        public Has(string MarkName)
        {
            _MarkName = MarkName ?? throw new ArgumentNullException(nameof(MarkName));
        }

        public bool Match(MarkContainer container)
        {
            return container.Has(_MarkName);
        }
    }

    // 接受一个Func<bool, string>用于生成动态条件
    public class Condition : IMarkQuery
    {
        private readonly Func<string[], bool> _func;

        public Condition(Func<string[], bool> func)
        {
            _func = func ?? throw new ArgumentNullException(nameof(func));
        }

        public bool Match(MarkContainer container) => _func(container.Keys.ToArray());
    }

    // 所有条件都必须满足
    public class All : IMarkQuery
    {
        private readonly List<IMarkQuery> _conditions;

        public All(params IMarkQuery[] conditions)
        {
            _conditions = conditions?.ToList() ?? throw new ArgumentNullException(nameof(conditions));
        }

        public bool Match(MarkContainer container)
        {
            return _conditions.All(cond => cond.Match(container));
        }
    }

    // 任意一个条件满足即可
    public class Any : IMarkQuery
    {
        private readonly List<IMarkQuery> _conditions;

        public Any(params IMarkQuery[] conditions)
        {
            _conditions = conditions?.ToList() ?? throw new ArgumentNullException(nameof(conditions));
        }

        public bool Match(MarkContainer container)
        {
            return _conditions.Any(cond => cond.Match(container));
        }
    }

    // 条件不满足
    public class None : IMarkQuery
    {
        private readonly IMarkQuery _condition;

        public None(IMarkQuery condition)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        public bool Match(MarkContainer container)
        {
            return !_condition.Match(container);
        }
    }




    // 流畅的构建器类
    public static class MarkQueryBuilder
    {
        // 基础条件（使用隐式转换）
        public static IMarkQuery Has(string MarkName) => new Has(MarkName);
        public static IMarkQuery Condition(Func<string[], bool> func) => new Condition(func);

        // 组合条件（支持字符串和IMarkQuery混合参数）
        public static IMarkQuery All(params object[] conditions) => new All(ConvertToQueries(conditions));
        public static IMarkQuery Any(params object[] conditions) => new Any(ConvertToQueries(conditions));
        public static IMarkQuery None(object condition) => new None(ConvertToQuery(condition));

        private static IMarkQuery[] ConvertToQueries(object[] conditions)
        {
            return conditions.Select(ConvertToQuery).ToArray();
        }

        private static IMarkQuery ConvertToQuery(object condition)
        {
            return condition switch
            {
                IMarkQuery query => query,
                string MarkName => new Has(MarkName),
                _ => throw new ArgumentException($"不支持的类型: {condition.GetType()}")
            };
        }
    }


    // // 示例用法
    // IMarkQuery query = MarkQueryBuilder.All(
    //      "Stunned",
    //       MarkQueryBuilder.Any(
    //          "Poisoned",
    //          "Burning"
    //       ),
    //       MarkQueryBuilder.None("Invincible")
    // );

    // var MarkContainer = new MarkContainer();
    // MarkContainer.Add("Stunned", new Mark());
    // MarkContainer.Add("Poisoned", new Mark());
    
    // bool matches = query.Match(MarkContainer);
    // Console.WriteLine($"匹配结果: {matches}"); // 输出: 匹配结果: True
}