using System;
using System.Collections.Generic;

namespace GoveKits.Units
{
    // 属性类，支持可读写的数值型属性和只读的计算属性
    public class Attribute
    {
        public string Name { get; private set; } = string.Empty;  // 属性名称
        private float currentValue = 0f;  // 当前值
        private bool dirty = false;  // 脏标记
        private readonly Func<float> calculator = null;  // 自定义计算器
        private event Action<float, float> OnValueChanged = null;  // 数值变化事件
        public bool IsComputed => calculator != null; // 是否为只读属性
        public float Value  // 属性当前值，通过自定义的计算器获取
        {
            get
            {
                if (IsComputed)
                {
                    if (dirty)
                    {
                        float oldValue = currentValue;
                        currentValue = calculator();
                        if (Math.Abs(currentValue - oldValue) > float.Epsilon)
                        {
                            OnValueChanged?.Invoke(oldValue, currentValue);
                        }
                        dirty = false;
                        return currentValue;  // 返回计算后的值
                    }
                    return currentValue;  // 不脏，返回缓存的值
                }
                return currentValue;  // 非计算属性，直接返回当前值
            }
            set
            {
                if (IsComputed)
                {
                    throw new InvalidOperationException($"[Attribute] 计算属性 {Name} 不可写");
                }
                float oldValue = currentValue;
                if (Math.Abs(value - currentValue) > float.Epsilon)
                {
                    currentValue = value;
                    // 如果是计算属性，设置值后应该标记为不脏
                    dirty = false;
                    OnValueChanged?.Invoke(oldValue, value);
                }
            }
        }

        // 构造函数
        public Attribute(string name, float initialValue)
        {
            this.Name = name;
            this.currentValue = initialValue;
        }
        private Attribute(string name, Func<float> calculator = null)
        {
            this.Name = name;
            this.calculator = calculator;
            this.dirty = true;

            _ = Value; // 初始化时计算一次值
        }


        /// <summary>
        /// 转换属性名称
        /// </summary>
        public Attribute As(string newName)
        {
            this.Name = newName;
            return this;
        } 


        // 订阅数值变化事件，返回一个取消订阅的操作
        public Action Subscribe(Action<float, float> handler)
        {
            OnValueChanged += handler;
            return () => Unsubscribe(handler);
        }
        // 取消订阅数值变化事件
        public void Unsubscribe(Action<float, float> handler)
        {
            OnValueChanged -= handler;
        }


        // 标记为脏，表示需要重新计算
        public void MarkDirty()
        {
            dirty = true;
        }

        #region 依赖管理
        // 订阅依赖属性的变化并在自身标记为脏
        // 我们保存订阅的委托以便在 Clear 或移除依赖时能够退订，防止内存泄漏
        private readonly List<Attribute> _dependencies = new List<Attribute>();
        private readonly Dictionary<Attribute, Action<float, float>> _dependencyHandlers
            = new Dictionary<Attribute, Action<float, float>>();
        public Attribute DependOn(params Attribute[] attributes)
        {
            if (attributes == null) return this;
            foreach (var attribute in attributes)
            {
                if (attribute == null) continue;
                if (AttributeExtensions.HasCircularDependency(this, attribute))
                {
                    throw new InvalidOperationException($"[Attribute] 依赖属性 {attribute.Name} 会引起循环依赖");
                }
                if (_dependencyHandlers.ContainsKey(attribute)) continue;
                // 订阅依赖属性的变化事件
                Action<float, float> handler = (oldValue, newValue) => MarkDirty();
                _dependencyHandlers[attribute] = handler;
                attribute.OnValueChanged += handler;
                _dependencies.Add(attribute);
            }
            MarkDirty();
            return this;
        }
        public IReadOnlyList<Attribute> GetDependencies()
        {
            return _dependencies.AsReadOnly();
        }


        // 清除所有事件监听器
        public void Clear()
        {
            // 退订我们在其他属性上注册的回调，防止其他属性继续持有对我们的引用
            if (_dependencyHandlers != null)
            {
                foreach (var kv in _dependencyHandlers)
                {
                    try
                    {
                        kv.Key.OnValueChanged -= kv.Value;
                    }
                    catch { }
                }
                _dependencyHandlers.Clear();
            }
            // 清空依赖列表
            _dependencies.Clear();
            // 清空本属性的订阅者
            OnValueChanged = null;
        }
        #endregion

        #region 运算符重载
        public static Attribute operator +(Attribute a)
            => a;
        public static Attribute operator -(Attribute a)
            => new Attribute($"(-{a.Name})", () => -a.Value).DependOn(a);
        public static Attribute operator +(Attribute a, float b)
            => new Attribute($"({a.Name}+{b})", () => a.Value + b).DependOn(a);
        public static Attribute operator -(Attribute a, float b)
            => new Attribute($"({a.Name}-{b})", () => a.Value - b).DependOn(a);
        public static Attribute operator *(Attribute a, float b)
            => new Attribute($"({a.Name}*{b})", () => a.Value * b).DependOn(a);
        public static Attribute operator /(Attribute a, float b)
            => new Attribute($"({a.Name}/{b})", () => a.Value / b).DependOn(a);
        public static Attribute operator +(float a, Attribute b)
            => new Attribute($"({a}+{b.Name})", () => a + b.Value).DependOn(b);
        public static Attribute operator -(float a, Attribute b)
            => new Attribute($"({a}-{b.Name})", () => a - b.Value).DependOn(b);
        public static Attribute operator *(float a, Attribute b)
            => new Attribute($"({a}*{b.Name})", () => a * b.Value).DependOn(b);
        public static Attribute operator /(float a, Attribute b)
            => new Attribute($"({a}/{b.Name})", () => a / b.Value).DependOn(b);
        public static Attribute operator +(Attribute a, Attribute b)
            => new Attribute($"({a.Name}+{b.Name})", () => a.Value + b.Value).DependOn(a, b);
        public static Attribute operator -(Attribute a, Attribute b)
            => new Attribute($"({a.Name}-{b.Name})", () => a.Value - b.Value).DependOn(a, b);
        public static Attribute operator *(Attribute a, Attribute b)
            => new Attribute($"({a.Name}*{b.Name})", () => a.Value * b.Value).DependOn(a, b);
        public static Attribute operator /(Attribute a, Attribute b)
            => new Attribute($"({a.Name}/{b.Name})", () => a.Value / b.Value).DependOn(a, b);
        #endregion


        // 扩展运算符重载
        public static Attribute Power(Attribute attribute, float exponent)
            => new Attribute($"({attribute.Name}^{exponent})", () => MathF.Pow(attribute.Value, exponent)).DependOn(attribute);
        public static Attribute Sqrt(Attribute attribute)
            => new Attribute($"sqrt({attribute.Name})", () => MathF.Sqrt(attribute.Value)).DependOn(attribute);
        public static Attribute Abs(Attribute attribute)
            => new Attribute($"abs({attribute.Name})", () => MathF.Abs(attribute.Value)).DependOn(attribute);
        public static Attribute Sin(Attribute attribute)
            => new Attribute($"sin({attribute.Name})", () => MathF.Sin(attribute.Value)).DependOn(attribute);
        public static Attribute Cos(Attribute attribute)
            => new Attribute($"cos({attribute.Name})", () => MathF.Cos(attribute.Value)).DependOn(attribute);
        public static Attribute Tan(Attribute attribute)
            => new Attribute($"tan({attribute.Name})", () => MathF.Tan(attribute.Value)).DependOn(attribute);
        public static Attribute Exp(Attribute attribute)
            => new Attribute($"exp({attribute.Name})", () => MathF.Exp(attribute.Value)).DependOn(attribute);
        public static Attribute Log(Attribute attribute)
            => new Attribute($"log({attribute.Name})", () => MathF.Log(attribute.Value)).DependOn(attribute);


    }



    public static class AttributeExtensions
    {
        // 循环依赖检测扩展方法
        public static bool HasCircularDependency(this Attribute attribute, Attribute dependency)
        {
            var visited = new HashSet<Attribute> { attribute };
            return CheckDependencyPath(dependency, attribute, new HashSet<Attribute>(visited));
        }
        private static bool CheckDependencyPath(Attribute current, Attribute target, HashSet<Attribute> visited)
        {
            if (current.Equals(target)) return true;
            if (!visited.Add(current)) return false;

            foreach (var dep in current.GetDependencies())
            {
                if (CheckDependencyPath(dep, target, visited))
                    return true;
            }
            visited.Remove(current);
            return false;
        }
    }
}