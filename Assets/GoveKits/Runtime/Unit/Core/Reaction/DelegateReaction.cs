using System;

namespace GoveKits.Unit
{
    /// <summary>
    /// [曲线救国] 委托反应器。
    /// <para>这是一个通用子类，专门用于将 Lambda 表达式适配到继承体系中。</para>
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