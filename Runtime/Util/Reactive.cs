using System;
using System.Collections.Generic;
using System.Linq;


namespace GoveKits.Runtime.Util
{
    /// <summary>
    /// 反应式变量工厂：快速创建 Int/Float/String/Bool 反应式引用。
    /// </summary>
    public static class Reactive
    {
        /// <summary>创建一个整数反应式引用。</summary>
        /// <param name="value">初始值。</param>
        /// <returns>新的 IntRef 实例。</returns>
        public static IntRef Int(int value) => new IntRef(value);
        
        /// <summary>创建一个浮点数反应式引用。</summary>
        /// <param name="value">初始值。</param>
        /// <returns>新的 FloatRef 实例。</returns>
        public static FloatRef Float(float value) => new FloatRef(value);
        
        /// <summary>创建一个字符串反应式引用。</summary>
        /// <param name="value">初始值。</param>
        /// <returns>新的 StringRef 实例。</returns>
        public static StringRef String(string value) => new StringRef(value);
        
        /// <summary>创建一个布尔反应式引用。</summary>
        /// <param name="value">初始值。</param>
        /// <returns>新的 BoolRef 实例。</returns>
        public static BoolRef Bool(bool value) => new BoolRef(value);
    }

    /// <summary>
    /// 反应式变量基类：值改变时自动通知监听器，支持计算属性、依赖管理、防重入保护。
    /// 反应式变量可监视（Watch）其值变化，也可组合成依赖关系形成计算属性。
    /// </summary>
    /// <typeparam name="T">变量值的数据类型。</typeparam>
    public abstract class Ref<T> where T : IEquatable<T>
    {
        private T _value;  // 存储实际值
        private readonly Func<T> _computer;  // 计算属性的计算函数
        private readonly List<Action> _listeners = new List<Action>();  // 监听器列表
        private readonly HashSet<Ref<T>> _impacts = new HashSet<Ref<T>>();  // 影响的计算属性列表

        // 防重入/并发修改保护
        private bool _isNotifying;
        private readonly HashSet<Action> _pendingRemove = new HashSet<Action>();

        /// <summary>
        /// 属性值（单值或计算得其）。
        /// 获取时返回 _value 或计算函数结果；设置时更新 _value 并通知所有监听器。
        /// 计算属性（由 _computer 函数驱动）无法直接设置。
        /// </summary>
        public T Value
        {
            get
            {
                return _computer != null ? _computer() : _value;
            }
            set
            {
                if (_value?.Equals(value) == true) return;
                if (_computer != null)
                    throw new InvalidOperationException("[Ref] 计算属性不能被设置");
                // 设置新值并通知监听器
                _value = value;
                Notify();
            }
        }

        protected Ref(T value) => _value = value;
        protected Ref(Func<T> computer) => _computer = computer;

        /// <summary>
        /// 通知所有监听器，并级联通知依赖的计算属性。
        /// 防重入：若已在通知过程中，则返回以避免无限递归。
        /// 监听器在回调中的 Unwatch 调用延迟生效。
        /// </summary>
        private void Notify()
        {
            // 防重入：避免循环依赖导致无限递归
            if (_isNotifying) return;
            _isNotifying = true;
            try
            {
                // 遍历时不复制数组，允许监听器在回调中取消订阅（延迟生效）
                for (int i = 0; i < _listeners.Count; i++)
                {
                    var l = _listeners[i];
                    if (l == null) continue;
                    if (_pendingRemove.Contains(l)) continue; // 已标记移除的跳过
                    l.Invoke();
                }

                // 采用队列式传播依赖，避免深递归
                if (_impacts.Count > 0)
                {
                    // 使用临时列表快照引用（仅引用，不分配新 Action）
                    foreach (var dep in _impacts)
                    {
                        dep?.Notify();
                    }
                }
            }
            finally
            {
                _isNotifying = false;
                // 应用延迟移除
                if (_pendingRemove.Count > 0)
                {
                    foreach (var a in _pendingRemove)
                        _listeners.Remove(a);
                    _pendingRemove.Clear();
                }
            }
        }

        /// <summary>
        /// 于值改变时监听回调（执行一个监听器）。
        /// </summary>
        /// <param name="action">变化回调。</param>
        /// <returns>返回取消监听的作用。</returns>
        public Action Watch(Action action)
        {
            _listeners.Add(action);
            return () => Unwatch(action);
        }
        
        /// <summary>
        /// 取消监听。
        /// </summary>
        /// <param name="action">变化回调。</param>
        public void Unwatch(Action action)
        {
            if (_isNotifying)
            {
                _pendingRemove.Add(action);
            }
            else
            {
                _listeners.Remove(action);
            }
        }

        public Ref<T> DependOn(params Ref<T>[] others)
        {
            foreach (var other in others)
                other._impacts.Add(this);
            return this;
        }

        public override string ToString() => Value?.ToString() ?? "null";
    }

    public class IntRef : Ref<int>
    {
        public IntRef(int value) : base(value) { }
        public IntRef(Func<int> computer) : base(computer) { }

        // 运算符重载...
        public static IntRef operator +(IntRef a, IntRef b) => (IntRef)new IntRef(() => a.Value + b.Value).DependOn(a, b);
        public static IntRef operator -(IntRef a, IntRef b) => (IntRef)new IntRef(() => a.Value - b.Value).DependOn(a, b);
        public static IntRef operator *(IntRef a, IntRef b) => (IntRef)new IntRef(() => a.Value * b.Value).DependOn(a, b);
        public static IntRef operator /(IntRef a, IntRef b) => (IntRef)new IntRef(() => a.Value / b.Value).DependOn(a, b);
        public static IntRef operator %(IntRef a, IntRef b) => (IntRef)new IntRef(() => a.Value % b.Value).DependOn(a, b);
        public static IntRef operator +(IntRef a, int b) => (IntRef)new IntRef(() => a.Value + b).DependOn(a);
        public static IntRef operator -(IntRef a, int b) => (IntRef)new IntRef(() => a.Value - b).DependOn(a);
        public static IntRef operator *(IntRef a, int b) => (IntRef)new IntRef(() => a.Value * b).DependOn(a);
        public static IntRef operator /(IntRef a, int b) => (IntRef)new IntRef(() => a.Value / b).DependOn(a);
        public static IntRef operator %(IntRef a, int b) => (IntRef)new IntRef(() => a.Value % b).DependOn(a);
        public static IntRef operator +(int a, IntRef b) => (IntRef)new IntRef(() => a + b.Value).DependOn(b);
        public static IntRef operator -(int a, IntRef b) => (IntRef)new IntRef(() => a - b.Value).DependOn(b);
        public static IntRef operator *(int a, IntRef b) => (IntRef)new IntRef(() => a * b.Value).DependOn(b);
        public static IntRef operator /(int a, IntRef b) => (IntRef)new IntRef(() => a / b.Value).DependOn(b);
        public static IntRef operator %(int a, IntRef b) => (IntRef)new IntRef(() => a % b.Value).DependOn(b);
    }

    public class FloatRef : Ref<float>
    {
        public FloatRef(float value) : base(value) { }
        public FloatRef(Func<float> computer) : base(computer) { }

        // 重写设置逻辑，处理浮点精度
        public new float Value
        {
            get => base.Value;
            set
            {
                // 更合理的阈值，避免频繁微抖动导致的通知
                if (Math.Abs(base.Value - value) <= 1e-5f) return;
                base.Value = value;
            }
        }

        // 运算符重载...
        public static FloatRef operator +(FloatRef a, FloatRef b) => (FloatRef)new FloatRef(() => a.Value + b.Value).DependOn(a, b);
        public static FloatRef operator -(FloatRef a, FloatRef b) => (FloatRef)new FloatRef(() => a.Value - b.Value).DependOn(a, b);
        public static FloatRef operator *(FloatRef a, FloatRef b) => (FloatRef)new FloatRef(() => a.Value * b.Value).DependOn(a, b);
        public static FloatRef operator /(FloatRef a, FloatRef b) => (FloatRef)new FloatRef(() => a.Value / b.Value).DependOn(a, b);
        public static FloatRef operator +(FloatRef a, float b) => (FloatRef)new FloatRef(() => a.Value + b).DependOn(a);
        public static FloatRef operator -(FloatRef a, float b) => (FloatRef)new FloatRef(() => a.Value - b).DependOn(a);
        public static FloatRef operator *(FloatRef a, float b) => (FloatRef)new FloatRef(() => a.Value * b).DependOn(a);
        public static FloatRef operator /(FloatRef a, float b) => (FloatRef)new FloatRef(() => a.Value / b).DependOn(a);
        public static FloatRef operator +(float a, FloatRef b) => (FloatRef)new FloatRef(() => a + b.Value).DependOn(b);
        public static FloatRef operator -(float a, FloatRef b) => (FloatRef)new FloatRef(() => a - b.Value).DependOn(b);
        public static FloatRef operator *(float a, FloatRef b) => (FloatRef)new FloatRef(() => a * b.Value).DependOn(b);
        public static FloatRef operator /(float a, FloatRef b) => (FloatRef)new FloatRef(() => a / b.Value).DependOn(b);
    }

    public class StringRef : Ref<string>
    {
        public StringRef(string value) : base(value) { }
        public StringRef(Func<string> computer) : base(computer) { }

        // 运算符重载...
        public static StringRef operator +(StringRef a, StringRef b) => (StringRef)new StringRef(() => a.Value + b.Value).DependOn(a, b);
        public static StringRef operator +(StringRef a, string b) => (StringRef)new StringRef(() => a.Value + b).DependOn(a);
        public static StringRef operator +(string a, StringRef b) => (StringRef)new StringRef(() => a + b.Value).DependOn(b);
    }

    public class BoolRef : Ref<bool>
    {
        public BoolRef(bool value) : base(value) { }
        public BoolRef(Func<bool> computer) : base(computer) { }

        // 运算符重载...
        public static BoolRef operator !(BoolRef a) => (BoolRef)new BoolRef(() => !a.Value).DependOn(a);
        public static BoolRef operator &(BoolRef a, BoolRef b) => (BoolRef)new BoolRef(() => a.Value & b.Value).DependOn(a, b);
        public static BoolRef operator |(BoolRef a, BoolRef b) => (BoolRef)new BoolRef(() => a.Value | b.Value).DependOn(a, b);
        public static BoolRef operator ^(BoolRef a, BoolRef b) => (BoolRef)new BoolRef(() => a.Value ^ b.Value).DependOn(a, b);
    }
}