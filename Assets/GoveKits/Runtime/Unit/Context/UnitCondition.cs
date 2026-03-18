

using System;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 条件基类。
    /// <para>可通过 And/Or/Not 组合条件，或通过 From 委托快速构建条件。</para>
    /// </summary>
    public abstract class UnitCondition
    {
        /// <summary>
        /// 执行条件检查。
        /// </summary>
        /// <param name="context">Unit 上下文。</param>
        /// <returns>满足返回 true，否则返回 false。</returns>
        public abstract bool Check(UnitContext context);

        /// <summary>
        /// 与另一个条件做逻辑与。
        /// </summary>
        public UnitCondition And(UnitCondition other) => this & other;

        /// <summary>
        /// 与另一个条件做逻辑或。
        /// </summary>
        public UnitCondition Or(UnitCondition other) => this | other;

        /// <summary>
        /// 对当前条件做逻辑非。
        /// </summary>
        public UnitCondition Not() => !this;

        /// <summary>
        /// 通过委托快速创建一个条件。
        /// </summary>
        public static UnitCondition From(Func<UnitContext, bool> predicate) => new DelegateCondition(predicate);

        /// <summary>
        /// 重载逻辑与运算符，组合为 AndCondition。
        /// </summary>
        public static UnitCondition operator &(UnitCondition left, UnitCondition right)
        {
            return new AndCondition(left, right);
        }

        /// <summary>
        /// 重载逻辑或运算符，组合为 OrCondition。
        /// </summary>
        public static UnitCondition operator |(UnitCondition left, UnitCondition right)
        {
            return new OrCondition(left, right);
        }

        /// <summary>
        /// 重载逻辑非运算符，组合为 NotCondition。
        /// </summary>
        public static UnitCondition operator !(UnitCondition condition)
        {
            return new NotCondition(condition);
        }
    }


    /// <summary>
    /// 基于委托的条件实现。
    /// </summary>
    public sealed class DelegateCondition : UnitCondition
    {
        private readonly Func<UnitContext, bool> _predicate;

        public DelegateCondition(Func<UnitContext, bool> predicate)
        {
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        public override bool Check(UnitContext context)
        {
            return _predicate(context);
        }
    }


    /// <summary>
    /// 逻辑与条件。
    /// </summary>
    public sealed class AndCondition : UnitCondition
    {
        private readonly UnitCondition _left;
        private readonly UnitCondition _right;

        public AndCondition(UnitCondition left, UnitCondition right)
        {
            _left = left;
            _right = right;
        }

        public override bool Check(UnitContext context)
        {
            return _left != null && _right != null && _left.Check(context) && _right.Check(context);
        }
    }


    /// <summary>
    /// 逻辑或条件。
    /// </summary>
    public sealed class OrCondition : UnitCondition
    {
        private readonly UnitCondition _left;
        private readonly UnitCondition _right;

        public OrCondition(UnitCondition left, UnitCondition right)
        {
            _left = left;
            _right = right;
        }

        public override bool Check(UnitContext context)
        {
            if (_left == null && _right == null)
            {
                return false;
            }

            return (_left != null && _left.Check(context)) || (_right != null && _right.Check(context));
        }
    }


    /// <summary>
    /// 逻辑非条件。
    /// </summary>
    public sealed class NotCondition : UnitCondition
    {
        private readonly UnitCondition _inner;

        public NotCondition(UnitCondition inner)
        {
            _inner = inner;
        }

        public override bool Check(UnitContext context)
        {
            return _inner != null && !_inner.Check(context);
        }
    }

    /// <summary>
    /// 从上下文扩展参数读取值并执行判断的条件。
    /// </summary>
    /// <typeparam name="T">扩展参数类型。</typeparam>
    public sealed class ContextDataCondition<T> : UnitCondition
    {
        private readonly string _key;
        private readonly Func<T, bool> _predicate;

        public ContextDataCondition(string key, Func<T, bool> predicate)
        {
            _key = key;
            _predicate = predicate;
        }

        public override bool Check(UnitContext context)
        {
            if (context == null || string.IsNullOrEmpty(_key) || _predicate == null)
            {
                return false;
            }

            return context.TryGetData<T>(_key, out var value) && _predicate(value);
        }
    }
}