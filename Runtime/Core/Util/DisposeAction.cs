using System;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 轻量级 disposable action。
    /// 包装一个无参 action，Dispose 时执行。
    /// 用于 Subscribe 返回取消订阅凭证等场景，避免分配 lambda 闭包。
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