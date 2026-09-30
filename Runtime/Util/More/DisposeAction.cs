using System;

namespace GoveKits.Runtime.Util
{
    /// <summary>
    /// 轻量级 disposable action。
    /// 包装一个无参 action，Dispose 时执行。
    /// 用于 Subscribe 返回取消订阅凭证等场景。
    /// 注意：readonly struct 只节省字段复制的开销，并不能消除装箱——
    /// 当它以 IDisposable 接口形式返回或存储时（如 EventBus.Subscribe 的返回值）
    /// 仍会发生一次装箱；订阅方传入的 lambda 亦会产生一次闭包分配，均为可接受开销。
    /// </summary>
    public readonly struct DisposeAction : IDisposable
    {
        private readonly Action _action;

        public DisposeAction(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            _action?.Invoke();
        }
    }
}