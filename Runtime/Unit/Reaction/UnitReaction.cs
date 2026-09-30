using System;
using GoveKits.Runtime.Util;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 第一层：Unit 反应基类（非泛型公共抽象层）。
    /// 只提供生命周期管理与宿主注入，不关心具体监听哪种事件。
    ///
    /// 直接继承本类无意义（Activate/Deactivate 是抽象的），
    /// 业务反应一律继承 <see cref="UnitReaction{T}"/>。
    /// </summary>
    public abstract class UnitReaction : IDisposable
    {
        /// <summary>反应的唯一标识，容器以它作为字典键，同名反应会互相覆盖</summary>
        public abstract UnitTag Name { get; }

        /// <summary>
        /// 监听同一事件时的执行先后顺序，值越大越先执行。
        /// 由事件总线按此值排序。
        /// </summary>
        public abstract int Priority { get; }

        /// <summary>反应挂载的宿主单位（由 Container 注入）</summary>
        public IUnit Owner { get; private set; }

        /// <summary>当前是否已订阅事件（Activate 后为 true）</summary>
        public bool IsActive { get; protected set; }

        /// <summary>无参构造，满足反序列化工厂要求</summary>
        public UnitReaction() { }

        /// <summary>由 ReactionContainer 在挂载瞬间调用，注入宿主</summary>
        internal void Init(IUnit owner)
        {
            Owner = owner;
        }

        /// <summary>激活：向全局 EventCore 订阅事件，重复调用无副作用</summary>
        public abstract void Activate();

        /// <summary>停用：从全局 EventCore 注销订阅，重复调用无副作用</summary>
        public abstract void Deactivate();

        /// <summary>释放反应资源，彻底断开与宿主及事件总线的联系</summary>
        public virtual void Dispose()
        {
            Deactivate();
            Owner = null;
        }
    }

    /// <summary>
    /// 第二层：泛型 Unit 反应基类。
    /// 继承它并实现 <see cref="OnEvent"/>，即得到一个常驻监听某个事件类型的被动能力。
    ///
    /// 用法：
    /// <code>
    /// public class ThornsReaction : UnitReaction&lt;DamageEvent&gt;
    /// {
    ///     public override UnitTag Name => "passive_thorns";
    ///     public override int Priority => 10;                                   // 越大越先执行
    ///     public override bool OnFilter(DamageEvent e) => e.Target == Owner;    // 只处理打自己的
    ///     public override void OnEvent(DamageEvent e)
    ///         => AttributeChangeEffect.Create().Set("hp", -10f).Apply(e.Source);
    /// }
    /// </code>
    /// 订阅与注销由容器自动完成，业务代码不需要手写 Subscribe/Unsubscribe。
    /// </summary>
    /// <typeparam name="T">监听的事件类型</typeparam>
    public abstract class UnitReaction<T> : UnitReaction, IEventListener<T> where T : EventData, new()
    {
        // 订阅凭证，Deactivate 时用它注销，避免单位销毁后仍被事件总线持有
        private IDisposable _unsubscribeAction;
        // 订阅时的总线版本号：EventCore.Close 会重建总线使旧凭证失效（IsActive 仍为 true），
        // 版本不一致时 Activate 必须重新订阅，否则反应在 Close 后永久失聪且无任何警告
        private int _subscribedBusVersion;

        /// <summary>无参构造，满足反序列化工厂要求</summary>
        public UnitReaction() { }

        /// <summary>订阅全局事件总线；EventCore.Close 重建总线后再次调用会自动重新订阅</summary>
        public override void Activate()
        {
            if (IsActive && _subscribedBusVersion == EventCore.BusVersion) return;

            // 首次激活，或总线已被 Close 重建（旧凭证失效）：清理旧凭证后重新订阅
            _unsubscribeAction?.Dispose();
            _unsubscribeAction = EventCore.Subscribe<T>(this);
            _subscribedBusVersion = EventCore.BusVersion;
            IsActive = true;
        }

        /// <summary>注销全局事件订阅，重复调用无副作用</summary>
        public override void Deactivate()
        {
            if (!IsActive) return;

            _unsubscribeAction?.Dispose();
            _unsubscribeAction = null;
            IsActive = false;
        }

        /// <summary>事件到达时的业务处理入口，在此产出 Effect 修改单位状态</summary>
        public abstract void OnEvent(T eventData);

        /// <summary>
        /// 事件前置过滤，返回 false 则本次事件不进入 <see cref="OnEvent"/>，默认全放行。
        /// 典型用法：判断事件目标是不是自己的 Owner，避免响应别人的事件。
        /// </summary>
        public virtual bool OnFilter(T eventData) => true;
    }
}
