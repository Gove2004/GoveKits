using System;

namespace GoveKits.Unit
{
    /// <summary>
    /// 委托反应器（Lambda 适配器）：允许直接使用 Lambda 表达式创建反应。
    /// <para>将 Func/Action 委托适配到 <see cref="GameReaction{T}"/> 继承体系中。</para>
    /// <para>适用于临时、一次性反应或简单处理逻辑。</para>
    /// </summary>
    public sealed class DelegateReaction<T> : GameReaction<T> where T : GameEffect
    {
        private readonly Action<T> _callback;

        public DelegateReaction(GameTag name, IGameUnit owner, Action<T> callback, int priority = 0) 
            : base(name, owner, priority)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        protected override void OnExecute(T effect)
        {
            // 直接转发给委托
            _callback.Invoke(effect);
        }
    }
}