


using System;
using System.Collections.Generic;
using GoveKits.Runtime.Core.Event;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 属性基类，封装一个带名称标识、支持变更通知的浮点值。
    /// </summary>
    public abstract class UnitAttribute : IDisposable
    {
        /// <summary>
        /// 属性唯一标识。
        /// </summary>
        public UnitTag Name { get; }

        /// <summary>
        /// 内部存储值。
        /// 对 StateAttribute 表示基础值；对 RuntimeAttribute 表示当前值。
        /// </summary>
        protected float _value;

        /// <summary>
        /// 属性当前值。
        /// 基类实现为直接读写字段；派生类可重写以提供缓存、约束或联动逻辑。
        /// </summary>
        public virtual float Value
        {
            get => _value;
            set
            {
                NotifyChange(_value, value);
                _value = value;
            }
        }

        /// <summary>
        /// 数值变更事件，参数依次为 oldValue/newValue。
        /// </summary>
        public event Action<float, float> OnValueChanged;

        /// <summary>
        /// 当前是否存在 Value 变更订阅者。
        /// 用于在性能敏感场景决定是否立即刷新。
        /// </summary>
        protected bool HasValueChangedSubscribers => OnValueChanged != null;

        /// <summary>
        /// 初始化属性对象。
        /// </summary>
        /// <param name="name">属性标识（例如 Hp、Atk、MoveSpeed）。</param>
        protected UnitAttribute(UnitTag name)
        {
            Name = name;
        }

        /// <summary>
        /// 在新旧值差异超过阈值时触发变更事件。
        /// </summary>
        /// <param name="oldVal">变更前值。</param>
        /// <param name="newVal">变更后值。</param>
        protected void NotifyChange(float oldVal, float newVal)
        {
            if (Math.Abs(oldVal - newVal) > 1e-5f)
                OnValueChanged?.Invoke(oldVal, newVal);
        }

        /// <summary>
        /// 释放资源并清理事件引用。
        /// </summary>
        public virtual void Dispose()
        {
            OnValueChanged = null;
        }
    }



    /// <summary>
    /// 最大生命值、攻击力等状态属性，支持通过 Modifier 进行动态调整，并在值发生变化时触发事件通知。
    /// </summary>
    public class StateAttribute : UnitAttribute
    {
        // 简化模型：不分层，所有修改器统一参与结算。
        private readonly List<AttributeModifier> _modifiers = new();

        /// <summary>
        /// 公式计算后的缓存值。
        /// </summary>
        private float _cachedValue;
        /// <summary>
        /// 脏标记。为 true 时表示缓存需要重算。
        /// </summary>
        private bool _isDirty = true;
        /// <summary>
        /// 最小限制。
        /// </summary>
        private readonly float _minLimit;
        /// <summary>
        /// 最大限制。
        /// </summary>
        private readonly float _maxLimit;

        /// <summary>
        /// 创建状态属性。
        /// </summary>
        /// <param name="name">属性标识。</param>
        /// <param name="baseValue">基础值。</param>
        /// <param name="minLimit">最小限制。</param>
        /// <param name="maxLimit">最大限制。</param>
        public StateAttribute(UnitTag name, float baseValue, float minLimit = 0f, float maxLimit = float.MaxValue) : base(name)
        {
            _value = baseValue;
            _minLimit = minLimit;
            _maxLimit = maxLimit;
            _cachedValue = Math.Clamp(baseValue, _minLimit, _maxLimit);
            _isDirty = false;
        }

        /// <summary>
        /// 状态属性最终值。
        /// 读取时按需刷新缓存；写入时更新基础值并标记为脏。
        /// </summary>
        public override float Value
        {
            get
            {
                Refresh();
                return _cachedValue;
            }
            set
            {
                throw new InvalidOperationException("StateAttribute.Value 是计算结果，不允许直接写入。请使用 BaseValue 或修改器接口。");
            }
        }

        /// <summary>
        /// 基础值。
        /// 该值会参与公式计算：final=(base+sumAdd)*(1+sumMuty)->override。
        /// </summary>
        public float BaseValue
        {
            get => _value;
            set
            {
                if (Math.Abs(_value - value) <= 1e-5f)
                {
                    return;
                }

                _value = value;
                MarkDirty();
            }
        }

        /// <summary>
        /// 添加一个修改器并返回可释放句柄。
        /// 释放句柄时会自动移除该修改器。
        /// </summary>
        /// <param name="modifier">要添加的修改器。</param>
        /// <returns>用于撤销该修改器的释放句柄。</returns>
        public DisposeAction AddModifier(AttributeModifier modifier)
        {
            _modifiers.Add(modifier);
            MarkDirty();
            return new DisposeAction(() => RemoveModifier(modifier));
        }

        /// <summary>
        /// 移除指定修改器。
        /// </summary>
        /// <param name="modifier">要移除的修改器。</param>
        public void RemoveModifier(AttributeModifier modifier)
        {
            if (_modifiers.Remove(modifier))
            {
                MarkDirty();
            }
        }

        /// <summary>
        /// 仅在脏状态时重算缓存值。
        /// </summary>
        public void Refresh()
        {
            if (!_isDirty) return;

            float oldValue = _cachedValue;
            Recalculate();

            NotifyChange(oldValue, _cachedValue);
        }

        /// <summary>
        /// 执行一次完整重算并刷新缓存。
        /// 计算顺序：先汇总加法和乘法，再应用 override 覆盖。
        /// </summary>
        private void Recalculate()
        {
            float sumAdditive = 0f;
            float sumMultiplicative = 0f;
            bool hasOverride = false;
            float overrideValue = 0f;

            for (int i = 0; i < _modifiers.Count; i++)
            {
                var mod = _modifiers[i];
                switch (mod.Type)
                {
                    case ModifierType.Additive:
                        sumAdditive += mod.Value;
                        break;
                    case ModifierType.Multiplicative:
                        sumMultiplicative += mod.Value;
                        break;
                    case ModifierType.Override:
                        hasOverride = true;
                        overrideValue = mod.Value;
                        break;
                }
            }

            // 公式：final=(base+sumAdd)*(1+sumMuty)->override
            float finalValue = (_value + sumAdditive) * (1f + sumMultiplicative);

            if (hasOverride)
            {
                finalValue = overrideValue;
            }

            _cachedValue = Math.Clamp(finalValue, _minLimit, _maxLimit);
            _isDirty = false;
        }

        /// <summary>
        /// 标记脏并在有订阅者时立即刷新，保证 UI 事件及时。
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
            
            // 如果有订阅者，立即刷新以触发事件通知；否则等到下次访问时再刷新。
            if (HasValueChangedSubscribers)
            {
                Refresh();
            }
        }

        /// <summary>
        /// 释放状态属性并清空所有修改器。
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            _modifiers.Clear();
        }
    }


    /// <summary>
    /// 生命值、法力值等运行时属性。
    /// </summary>
    /// <remarks>
    /// RuntimeAttribute 表示“当前值”，并依赖 StateAttribute 作为“上限值”。
    /// 例如：当前生命值会被最大生命值约束；当最大生命值变化时，当前值会自动重新钳制。
    /// </remarks>
    public class RuntimeAttribute : UnitAttribute
    {
        /// <summary>
        /// 作为上限来源的状态属性（如 MaxHp）。
        /// </summary>
        private readonly StateAttribute _stateAttribute;

        /// <summary>
        /// 运行时属性对应的上限值（通常来自 StateAttribute）。
        /// </summary>
        public float MaxValue => _stateAttribute.Value;

        /// <summary>
        /// 当前值占上限的比例，范围 [0, 1]。
        /// </summary>
        public float Ratio
        {
            get
            {
                float max = MaxValue;
                if (max <= 1e-5f)
                {
                    return 0f;
                }

                return _value / max;
            }
        }

        /// <summary>
        /// 创建运行时属性，并绑定一个状态属性作为上限来源。
        /// </summary>
        /// <param name="name">运行时属性标识（例如 CurrentHp）。</param>
        /// <param name="stateAttribute">对应上限属性（例如 MaxHp）。</param>
        public RuntimeAttribute(UnitTag name, StateAttribute stateAttribute) : base(name)
        {
            _stateAttribute = stateAttribute ?? throw new ArgumentNullException(nameof(stateAttribute));
            _stateAttribute.OnValueChanged += OnMaxValueChanged;
            _value = MaxValue;
        }

        /// <summary>
        /// 当前值，自动约束到 [0, MaxValue]。
        /// </summary>
        public override float Value
        {
            get => _value;
            set
            {
                float clamped = Math.Clamp(value, 0f, MaxValue);
                NotifyChange(_value, clamped);
                _value = clamped;
            }
        }

        /// <summary>
        /// 按增量修改当前值（正数恢复，负数损失）。
        /// </summary>
        public RuntimeAttribute Change(float delta, ModifierType type = ModifierType.Additive)
        {
            switch (type)
            {
                case ModifierType.Additive:
                    Value += delta;
                    break;
                case ModifierType.Multiplicative:
                    Value *= (1f + delta);
                    break;
                case ModifierType.Override:
                    Value = delta;
                    break;
            }
            return this;
        }

        /// <summary>
        /// 直接恢复到满值（等于 MaxValue）。
        /// </summary>
        public RuntimeAttribute Full()
        {
            Value = MaxValue;
            return this;
        }

        /// <summary>
        /// 直接清空（等于 0）。
        /// </summary>
        public RuntimeAttribute Clear()
        {
            Value = 0f;
            return this;
        }

        /// <summary>
        /// 解除对 StateAttribute 的监听，避免对象生命周期结束后残留事件引用。
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            _stateAttribute.OnValueChanged -= OnMaxValueChanged;
        }

        /// <summary>
        /// 上限变化时重新钳制当前值，确保 Runtime 值始终位于合法区间。
        /// </summary>
        /// <param name="oldMax">变更前上限（未使用，仅用于保留事件签名语义）。</param>
        /// <param name="newMax">变更后上限（未使用，仅用于保留事件签名语义）。</param>
        private void OnMaxValueChanged(float oldMax, float newMax)
        {
            // 上限变化后重新钳制当前值，确保 Runtime 值永远合法。
            Value = _value;
        }
    }
}