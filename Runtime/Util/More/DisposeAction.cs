using System;

namespace GoveKits.Runtime.Util
{
    /// <summary>
    /// 轻量级 disposable action。
    /// 包装一个无参 action，Dispose 时执行。
    /// 用于 Subscribe 返回取消订阅凭证等场景；以 readonly struct 返回避免凭证本身装箱
    /// （订阅方传入的 lambda 仍会产生一次闭包分配，属正常开销）。
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