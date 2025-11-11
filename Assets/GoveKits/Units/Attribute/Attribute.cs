using System;

namespace GoveKits.Units
{
    // 属性类，支持可读写的数值型属性和只读的计算属性
    public class Attribute
    {
        public string Name { get; private set; } = string.Empty;  // 属性名称
        private float currentValue = 0f;  // 当前值
        private bool dirty = false;  // 脏标记
        private readonly Func<float> calculator = null;  // 自定义计算器
        public event Action<float, float> OnValueChanged = null;  // 数值变化事件
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

        public Attribute(string name, float initialValue)
        {
            this.Name = name;
            this.currentValue = initialValue;
        }

        // 构造函数
        public Attribute(string name, Func<float> calculator = null)
        {
            this.Name = name;
            this.calculator = calculator;
            this.dirty = true;
            
            _ = Value; // 初始化时计算一次值
        }

        // 标记为脏，表示需要重新计算
        public void MarkDirty()
        {
            dirty = true;
        }

        public void Clear()
        {
            OnValueChanged = null;
        }

        #region 运算符重载
        public static float operator +(Attribute a, float b) => a.Value + b;
        public static float operator -(Attribute a, float b) => a.Value - b;
        public static float operator *(Attribute a, float b) => a.Value * b;
        public static float operator /(Attribute a, float b) => a.Value / b;
        public static float operator +(float a, Attribute b) => a + b.Value;
        public static float operator -(float a, Attribute b) => a - b.Value;
        public static float operator *(float a, Attribute b) => a * b.Value;
        public static float operator /(float a, Attribute b) => a / b.Value;
        public static float operator +(Attribute a, Attribute b) => a.Value + b.Value;
        public static float operator -(Attribute a, Attribute b) => a.Value - b.Value;
        public static float operator *(Attribute a, Attribute b) => a.Value * b.Value;
        public static float operator /(Attribute a, Attribute b) => a.Value / b.Value;
        public static implicit operator float(Attribute a) => a.Value;
        #endregion
    }
}