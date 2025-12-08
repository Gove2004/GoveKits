using System;
using GoveKits.Events;

namespace GoveKits.Unit
{
    /// <summary>
    /// 反应器接口。
    /// <para>实现者用于控制反应的激活/关闭以及提供名称（用于调试或按标签查找）。</para>
    /// </summary>
    public interface IGameReaction
    {
        /// <summary>
        /// 反应的标签/名称。
        /// </summary>
        GameTag Name { get; }

        /// <summary>
        /// 激活反应（开始订阅事件总线）。
        /// </summary>
        void Activate();

        /// <summary>
        /// 取消激活反应（取消订阅事件总线并释放资源）。
        /// </summary>
        void Deactivate();
    }

    /// <summary>
    /// 泛型反应器。
    /// <para>T 必须是 <see cref="GameEffect"/> 或其子类。</para>
    /// <para>反应器在激活时会订阅事件总线，并在接收到匹配的事件后调用提供的回调。</para>
    /// </summary>
    /// <typeparam name="T">反应所监听的事件类型（GameEffect 子类）。</typeparam>
    public abstract class GameReaction<T> : IGameReaction where T : GameEffect
    {
        /// <summary>
        /// 反应的标签，用于标识或在日志中显示。
        /// </summary>
        public GameTag Name { get; }

        // 反应归属的单位（用于自动过滤 Target/Source）
        protected readonly IGameUnit _owner;

        // 订阅优先级，值越大越先收到事件（取决于 EventManager 的实现）
        private readonly int _priority;

        // 订阅时返回的取消订阅 token（封装为 Action，调用即可取消订阅）
        private Action _unsubscribeToken;

        /// <summary>
        /// 构造一个新的反应器实例。
        /// </summary>
        /// <param name="name">反应标签/名称（调试用）。</param>
        /// <param name="owner">拥有者单位，用于自动过滤事件（例如只响应发向自己的事件）。</param>
        /// <param name="action">事件触发时的处理函数。</param>
        /// <param name="priority">事件订阅优先级（可选）。</param>
        protected GameReaction(GameTag name, IGameUnit owner, int priority = 0)
        {
            Name = name;
            _owner = owner;
            _priority = priority;
        }


        /// <summary>
        /// 激活反应：订阅事件总线。如果已经激活则无操作。
        /// </summary>
        public void Activate()
        {
            if (_unsubscribeToken != null) return;

            // 订阅事件总线，订阅后 EventManager 返回一个取消订阅的 Action
            // 这里不需要传入 busName，通常 Units 在同一位面或由 owner 决定具体路由。
            _unsubscribeToken = EventManager.Subscribe<T>(OnEventReceived, priority: _priority);
        }

        /// <summary>
        /// 取消激活反应：如果已订阅则取消订阅并释放 token。
        /// </summary>
        public void Deactivate()
        {
            _unsubscribeToken?.Invoke();
            _unsubscribeToken = null;
        }

        /// <summary>
        /// 事件总线回调：收到事件后先做过滤，再执行用户提供的逻辑。
        /// </summary>
        /// <param name="effect">接收到的事件对象。</param>
        private void OnEventReceived(T effect)
        {
            // --- 自动过滤逻辑 ---
            // 只有当事件的目标是自己，或者事件发起者是自己时，才触发反应。
            // 这避免了不必要的处理并实现常见的“只处理针对自己的事件”语义。
            if (effect.Target != _owner && effect.Source != _owner)
                return;

            // 执行逻辑并捕获异常，防止单个反应破坏事件分发流程
            try
            {
                // 2. 派发给子类
                OnExecute(effect);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[Reaction] Error in '{Name}': {ex}");
            }
        }


        /// <summary>
        /// [必须实现] 子类核心逻辑。
        /// <para>此时 effect 已经经过了 Target/Source 过滤。</para>
        /// </summary>
        protected abstract void OnExecute(T effect);
    }
}