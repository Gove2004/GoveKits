using System;
using System.Collections.Generic;
using System.Linq;

namespace GoveKits.Units
{
    // 标签查询接口，实现可组合的标签查询条件
    public interface IBuffQuery
    {
        bool Match(BuffContainer container);
    }

    // 基础标签条件
    public class Has : IBuffQuery
    {
        private readonly string _buffName;

        public Has(string buffName)
        {
            _buffName = buffName ?? throw new ArgumentNullException(nameof(buffName));
        }

        public bool Match(BuffContainer container)
        {
            return container.Has(_buffName);
        }
    }

    // 接受一个Func<bool, string>用于生成动态条件
    public class Condition : IBuffQuery
    {
        private readonly Func<string[], bool> _func;

        public Condition(Func<string[], bool> func)
        {
            _func = func ?? throw new ArgumentNullException(nameof(func));
        }

        public bool Match(BuffContainer container) => _func(container.Keys.ToArray());
    }

    // 所有条件都必须满足
    public class All : IBuffQuery
    {
        private readonly List<IBuffQuery> _conditions;

        public All(params IBuffQuery[] conditions)
        {
            _conditions = conditions?.ToList() ?? throw new ArgumentNullException(nameof(conditions));
        }

        public bool Match(BuffContainer container)
        {
            return _conditions.All(cond => cond.Match(container));
        }
    }

    // 任意一个条件满足即可
    public class Any : IBuffQuery
    {
        private readonly List<IBuffQuery> _conditions;

        public Any(params IBuffQuery[] conditions)
        {
            _conditions = conditions?.ToList() ?? throw new ArgumentNullException(nameof(conditions));
        }

        public bool Match(BuffContainer container)
        {
            return _conditions.Any(cond => cond.Match(container));
        }
    }

    // 条件不满足
    public class None : IBuffQuery
    {
        private readonly IBuffQuery _condition;

        public None(IBuffQuery condition)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        public bool Match(BuffContainer container)
        {
            return !_condition.Match(container);
        }
    }




    // 流畅的构建器类
    public static class BuffQueryBuilder
    {
        // 基础条件（使用隐式转换）
        public static IBuffQuery Has(string buffName) => new Has(buffName);
        public static IBuffQuery Condition(Func<string[], bool> func) => new Condition(func);

        // 组合条件（支持字符串和IBuffQuery混合参数）
        public static IBuffQuery All(params object[] conditions) => new All(ConvertToQueries(conditions));
        public static IBuffQuery Any(params object[] conditions) => new Any(ConvertToQueries(conditions));
        public static IBuffQuery None(object condition) => new None(ConvertToQuery(condition));

        private static IBuffQuery[] ConvertToQueries(object[] conditions)
        {
            return conditions.Select(ConvertToQuery).ToArray();
        }

        private static IBuffQuery ConvertToQuery(object condition)
        {
            return condition switch
            {
                IBuffQuery query => query,
                string buffName => new Has(buffName),
                _ => throw new ArgumentException($"不支持的类型: {condition.GetType()}")
            };
        }
    }


    // // 示例用法
    // IBuffQuery query = BuffQueryBuilder.All(
    //      "Stunned",
    //       BuffQueryBuilder.Any(
    //          "Poisoned",
    //          "Burning"
    //       ),
    //       BuffQueryBuilder.None("Invincible")
    // );

    // var buffContainer = new BuffContainer();
    // buffContainer.Add("Stunned", new Buff());
    // buffContainer.Add("Poisoned", new Buff());
    
    // bool matches = query.Match(buffContainer);
    // Console.WriteLine($"匹配结果: {matches}"); // 输出: 匹配结果: True
}