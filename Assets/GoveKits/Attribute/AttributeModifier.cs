

using System;

namespace GoveKits.Attribute
{
    /// <summary>
    /// 属性修正器接口
    /// </summary>
    /// <typeparam name="T">一般为int和float</typeparam>
    public class AttributeModifier<T> where T : struct
    {
        public readonly string name; // 修正器名称
        public readonly string description; // 修正器描述
        public readonly int priority; // 修正器优先级，数值越大优先级越高
        private readonly Func<T, T> onApply; // 应用修正器的委托

        public AttributeModifier(string name = "", string description = "", int priority = 0, Func<T, T> onApply = null)
        {
            this.name = name;
            this.description = description;
            this.priority = priority;
            this.onApply = onApply;
        }

        public virtual T Apply(T baseValue) { return onApply(baseValue); } // 应用修正器逻辑
    }

    public enum ModifierType
    {
        Add,  // 加法, 优先级1
        Multiply,  // 乘法, 优先级10
        Override  // 覆盖, 优先级100
    }

    /// <summary>
    /// 加法修正器
    /// </summary>
    /// <typeparam name="T">一般为int和float</typeparam>
    public class AddModifier<T> : AttributeModifier<T> where T : struct
    {
        private readonly T addValue; // 加法值

        public AddModifier(T addValue, string name = "", string description = "", int priority = 1)
            : base(name, description, priority, null)
        {
            this.addValue = addValue;
        }

        public override T Apply(T baseValue)
        {
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Int32:
                    int intResult = Convert.ToInt32(baseValue) + Convert.ToInt32(addValue);
                    return (T)(object)intResult;
                case TypeCode.Single:
                    float floatResult = Convert.ToSingle(baseValue) + Convert.ToSingle(addValue);
                    return (T)(object)floatResult;
                case TypeCode.Double:
                    double doubleResult = Convert.ToDouble(baseValue) + Convert.ToDouble(addValue);
                    return (T)(object)doubleResult;
                default:
                    throw new NotSupportedException($"[AddModifier] 不支持类型 {typeof(T)}");
            }
        }
    }

    /// <summary>
    /// 乘法修正器
    /// </summary>
    /// <typeparam name="T">一般为int和float</typeparam>
    public class MultiplyModifier<T> : AttributeModifier<T> where T : struct
    {
        private readonly T multiplyRate; // 乘法因子

        public MultiplyModifier(T multiplyRate, string name = "", string description = "", int priority = 10)
            : base(name, description, priority, null)
        {
            this.multiplyRate = multiplyRate;
        }

        public override T Apply(T baseValue)
        {
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Int32:
                    int intResult = (int)(Convert.ToInt32(baseValue) * Convert.ToSingle(multiplyRate));
                    return (T)(object)intResult;
                case TypeCode.Single:
                    float floatResult = Convert.ToSingle(baseValue) * Convert.ToSingle(multiplyRate);
                    return (T)(object)floatResult;
                case TypeCode.Double:
                    double doubleResult = Convert.ToDouble(baseValue) * Convert.ToDouble(multiplyRate);
                    return (T)(object)doubleResult;
                default:
                    throw new NotSupportedException($"[MultiplyModifier] 不支持类型 {typeof(T)}");
            }
        }
    }

    /// <summary>
    /// 覆盖修正器
    /// </summary>
    /// <typeparam name="T">一般为int和float</typeparam>
    public class OverrideModifier<T> : AttributeModifier<T> where T : struct
    {
        private readonly T overrideValue; // 覆盖值

        public OverrideModifier(T overrideValue, string name = "", string description = "", int priority = 100)
            : base(name, description, priority, (baseValue) => overrideValue)
        {
            this.overrideValue = overrideValue;
        }
    }

    /// <summary>
    /// 修正器工厂类
    /// </summary>
    /// <typeparam name="T">一般为int和float</typeparam>
    public static class ModifierFactory<T> where T : struct
    {
        public static AttributeModifier<T> CreateModifier(ModifierType type, T value, string name = "", string description = "")
        {
            return type switch
            {
                ModifierType.Add => new AddModifier<T>(value, name, description),
                ModifierType.Multiply => new MultiplyModifier<T>(value, name, description),
                ModifierType.Override => new OverrideModifier<T>(value, name, description),
                _ => throw new ArgumentException("[ModifierFactory] 未知的修正器类型"),
            };
        }
    }
}