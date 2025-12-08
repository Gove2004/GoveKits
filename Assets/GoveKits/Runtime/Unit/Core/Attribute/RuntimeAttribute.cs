using System;
using UnityEngine;

namespace GoveKits.Unit
{
    /// <summary>
    /// 资源型属性 (CurrentHP, CurrentMP)
    /// <para>逻辑：Current 永远被钳制在 [0, Max] 之间</para>
    /// </summary>
    public class RuntimeAttribute : GameAttribute, IDisposable
    {
        private float _current;
        private readonly GameAttribute _maxAttr; // 依赖的上限属性

        /// <summary>
        /// 构造资源型属性并关联上限属性（Max）。
        /// <para>会自动把当前值初始化为上限并订阅上限属性的变化以保持响应式。</para>
        /// </summary>
        public RuntimeAttribute(GameTag tag, GameAttribute maxAttr) : base(tag)
        {
            _maxAttr = maxAttr ?? throw new ArgumentNullException(nameof(maxAttr));
            _current = Max; // 初始满状态

            // 监听上限变化 (响应式核心)
            _maxAttr.OnValueChanged += OnMaxChanged;
        }

        /// <summary>当前属性的值（例如当前 HP）。</summary>
        public override float Value => _current;
        
        // 辅助读取
        /// <summary>依赖的上限属性的当前值（Max）</summary>
        public float Max => _maxAttr.Value;
        /// <summary>当前值与上限的比率（0~1）</summary>
        public float Ratio => Max > 0 ? Mathf.Clamp01(_current / Max) : 0f;
        /// <summary>是否已满（Current >= Max）</summary>
        public bool IsFull => _current >= Max;

        // --- 写操作 (直接修改数值) ---

        /// <summary>
        /// 对当前数值应用变化（可为负表示扣除/伤害）。
        /// </summary>
        /// <param name="delta">变化量（负数为减少）</param>
        public void ApplyChange(float delta)
        {
            float old = _current;
            _current = Mathf.Clamp(_current + delta, 0, Max);
            NotifyChange(old, _current);
        }
        /// <summary>把当前值设为上限（补满）</summary>
        public void SetToMax() => ApplyChange(Max - _current);

        // --- 内部响应逻辑 ---

        private void OnMaxChanged(float oldMax, float newMax)
        {
            // 如果上限变小导致溢出，截断当前值
            if (_current > newMax)
            {
                float old = _current;
                _current = newMax;
                NotifyChange(old, _current);
            }
            // 扩展：如果需要 MOBA 风格的“百分比保持”，可以在这里写逻辑
        }

        /// <summary>释放资源并取消订阅上限变化（Dispose 模式）。</summary>
        public void Dispose()
        {
            if (_maxAttr != null) 
                _maxAttr.OnValueChanged -= OnMaxChanged;
        }
    }
}