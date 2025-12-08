
using System;


namespace GoveKits.Unit
{
    /// <summary>
    /// 属性基类：所有具体属性（State/Runtime）都继承自此类。
    /// - 提供 Tag 标识与变更事件支持。
    /// </summary>
    public abstract class GameAttribute
    {
        /// <summary>属性标签，用于容器查找</summary>
        public GameTag Tag { get; }
        /// <summary>属性当前的最终值（只读，子类实现具体计算/存储）</summary>
        public abstract float Value { get; }
        /// <summary>
        /// 值变化回调：参数为 (oldVal, newVal)
        /// 订阅方可以在属性改变时做响应（例如更新 UI、触发依赖更新）
        /// </summary>
        public event Action<float, float> OnValueChanged;

        protected GameAttribute(GameTag tag) => Tag = tag;

        /// <summary>
        /// 在子类变更值时调用以触发事件。
        /// - 使用微小阈值避免浮点噪声导致的频繁回调。
        /// </summary>
        /// <param name="oldVal">旧值</param>
        /// <param name="newVal">新值</param>
        protected void NotifyChange(float oldVal, float newVal)
        {
            if (Math.Abs(oldVal - newVal) > 1e-5f)
                OnValueChanged?.Invoke(oldVal, newVal);
        }
    }
}