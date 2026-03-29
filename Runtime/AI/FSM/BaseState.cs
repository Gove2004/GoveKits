using Cysharp.Threading.Tasks;
using System;

namespace GoveKits.Runtime.AI.FSM
{
    /// <summary>
    /// FSM 状态基类。
    /// <para>可重写进入、每帧更新、固定帧更新和退出生命周期。</para>
    /// </summary>
    /// <typeparam name="TStateEnum">状态枚举类型。</typeparam>
    /// <typeparam name="TFSMObject">状态机持有者类型。</typeparam>
    public abstract class BaseState<TStateEnum, TFSMObject> : IDisposable
        where TStateEnum : struct, System.Enum 
        where TFSMObject : class, IFSMObject
    {
        /// <summary>
        /// 所属状态机，由 <see cref="FSM{TStateEnum, TFSMObject}.AddState"/> 自动注入。
        /// </summary>
        public FSM<TStateEnum, TFSMObject> Machine { get; internal set; }

        /// <summary>
        /// 便捷访问状态机所属对象。
        /// </summary>
        protected TFSMObject Owner => Machine.Owner;

        /// <summary>
        /// 进入状态时调用。
        /// </summary>
        public virtual UniTask OnEnter() => UniTask.CompletedTask;

        /// <summary>
        /// 每帧更新时调用。
        /// </summary>
        public virtual void OnUpdate() { }

        /// <summary>
        /// 固定帧更新时调用。
        /// </summary>
        public virtual void OnFixedUpdate() {}

        /// <summary>
        /// 退出状态时调用。
        /// </summary>
        public virtual UniTask OnExit() => UniTask.CompletedTask;

        /// <summary>
        /// 便捷切换到目标状态。
        /// <para>此方法为 fire-and-forget，不等待切换完成。</para>
        /// </summary>
        /// <param name="nextState">目标状态标签。</param>
        protected void ChangeState(TStateEnum nextState) => Machine.ChangeState(nextState).Forget();

        /// <summary>
        /// 释放状态持有的资源。
        /// <para>如状态内部订阅了事件、持有定时器或对象池句柄，请在子类重写并释放。</para>
        /// </summary>
        public virtual void Dispose()
        {
            Machine = null;
        }
    }
}