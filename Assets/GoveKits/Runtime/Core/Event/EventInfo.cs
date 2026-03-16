using System;

namespace GoveKits.Runtime.Core.Event
{
    #region Event Info

    /// <summary>
    /// 所有事件数据的基类。
    /// </summary>
    /// <remarks>
    /// 事件对象来自对象池：发布完成后会被回收到池中并在下次复用。
    /// 派生类应在 <see cref="OnRecycle"/> 中清理自身字段，避免脏数据跨帧残留。
    /// </remarks>
    public abstract class EventInfo : Pool.IPoolable
    {
        /// <summary>
        /// 是否中断后续监听器调用。
        /// </summary>
        /// <remarks>
        /// 任意监听器将其设为 true 后，当前事件分发会立即停止。
        /// </remarks>
        public bool IsBreak = false;

        /// <summary>
        /// 事件对象回收时调用。
        /// </summary>
        /// <remarks>
        /// 在这里重置派生事件类中的全部可变状态。
        /// </remarks>
        public abstract void OnRecycle();
    }

    #endregion

    #region Event Listener

    /// <summary>
    /// 强类型事件监听器接口。
    /// </summary>
    /// <typeparam name="T">事件类型。</typeparam>
    public interface IEventListener<T> where T : EventInfo
    {
        /// <summary>
        /// 监听器优先级，数值越大越先执行。
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 处理事件回调。
        /// </summary>
        /// <param name="eventInfo">事件实例。</param>
        void OnEvent(T eventInfo);
    }


    /// <summary>
    /// 监听器基类，提供默认优先级实现。
    /// </summary>
    /// <typeparam name="T">事件类型。</typeparam>
    public abstract class EventListener<T> : IEventListener<T> where T : EventInfo
    {
        public virtual int Priority => 0;
        public abstract void OnEvent(T eventInfo);
    }

    /// <summary>
    /// 通过委托快速创建的监听器。
    /// </summary>
    /// <typeparam name="T">事件类型。</typeparam>
    public class ActionEventListener<T> : EventListener<T> where T : EventInfo
    {
        private readonly Action<T> _action;
        private readonly int _priority;
        public override int Priority => _priority;

        public ActionEventListener(Action<T> action, int priority = 0)
        {
            _action = action;
            _priority = priority;
        }

        public override void OnEvent(T eventInfo) => _action?.Invoke(eventInfo);
    }

    #endregion

    #region Dispose Action

    /// <summary>
    /// 轻量级反注册句柄。
    /// </summary>
    public class DisposeAction : System.IDisposable
    {
        private readonly System.Action _disposeAction;

        public DisposeAction(System.Action disposeAction)
        {
            _disposeAction = disposeAction;
        }

        public void Dispose()
        {
            _disposeAction?.Invoke();
        }
    }

    #endregion
}