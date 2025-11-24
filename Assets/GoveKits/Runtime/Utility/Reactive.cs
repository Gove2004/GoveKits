using System;
using System.Collections.Generic;
using System.Linq;


public static class Reactive
{
    public static IntRef Int(int value) => new IntRef(value);
    public static FloatRef Float(float value) => new FloatRef(value);
    public static StringRef String(string value) => new StringRef(value);
    public static BoolRef Bool(bool value) => new BoolRef(value);
}


public abstract class Ref<T> where T : IEquatable<T>
{
    private T _value;  // 存储实际值
    private readonly Func<T> _computer;  // 计算属性的计算函数
    private readonly List<Action> _listeners = new List<Action>();  // 监听器列表
    private readonly HashSet<Ref<T>> _impacts = new HashSet<Ref<T>>();  // 影响的计算属性列表

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


    private void Notify()
    {
        foreach (var listener in _listeners.ToArray())
            listener?.Invoke();

        foreach (var dep in _impacts.ToArray())
            dep?.Notify();
    }

    // 添加监听器，返回取消监听的操作
    public Action Watch(Action action)
    {
        _listeners.Add(action);
        return () => Unwatch(action);
    }
    public void Unwatch(Action action) => _listeners.Remove(action);

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
            if (Math.Abs(base.Value - value) < float.Epsilon) return;
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
