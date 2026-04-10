using System;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 效果基类（非泛型层）。
    /// <para>基于命令模式 (Command Pattern) 设计，表示对单位施加的一次性或持续性状态变更。</para>
    /// </summary>
    public abstract class UnitEffect : IPoolable
    {
        /// <summary>
        /// 应用效果并【自动回收】（推荐用于临时/即时效果）。
        /// 执行完毕后，该 Effect 实例将被自动放回对象池。
        /// </summary>
        /// <param name="target">效果作用的目标单位。</param>
        public abstract void Apply<TUnit>(TUnit target) where TUnit : IUnit;

        /// <summary>
        /// 应用效果且【不进行回收】（推荐用于长期缓存/复用的效果）。
        /// 适用于通过 new 关键字创建，并缓存在字段中反复执行的持久化 Effect。
        /// </summary>
        /// <param name="target">效果作用的目标单位。</param>
        public abstract void ApplyWithoutPool<TUnit>(TUnit target) where TUnit : IUnit;

        /// <summary>
        /// 对象池回收时的重置逻辑。
        /// <para>子类必须在此处清空所有引用的目标、恢复数值默认值，防止脏数据污染下一次使用。</para>
        /// </summary>
        public abstract void OnRecycle();
    }

    /// <summary>
    /// 泛型 Unit 效果基类 (基于 CRTP 奇异递归模板模式)。
    /// <para>职责：提供流式接口 (Fluent API) 支持，并封装严格的 0GC 对象池生命周期管理。</para>
    /// </summary>
    /// <example>
    /// // 最佳实践：即用即抛（0 GC）
    /// DamageEffect.Create()
    ///     .SetDamage(100)
    ///     .SetCritical(true)
    ///     .Apply(enemy); 
    /// </example>
    /// <typeparam name="TEffect">子类自身的具体类型</typeparam>
    public abstract class UnitEffect<TEffect> : UnitEffect where TEffect : UnitEffect<TEffect>, new()
    {
        /// <summary>
        /// 从对象池中获取一个 Effect 实例。
        /// <para>注意：通过此方法获取的实例，必须调用 <see cref="Apply{TUnit}"/> 以确保被正确回收。</para>
        /// </summary>
        /// <returns>初始化后的具体 Effect 实例。</returns>
        public static TEffect Create()
        {
            return CoreLocator.Pool.Get<TEffect>();
        }

        /// <summary>
        /// 执行效果逻辑，并在执行结束后【强制回收】至对象池。
        /// <para>采用 try-finally 结构，确保即使业务逻辑抛出异常，对象也能安全入池，防止内存泄漏。</para>
        /// </summary>
        /// <param name="target">目标单位。</param>
        public override void Apply<TUnit>(TUnit target)
        {
            try
            {
                OnApply<TUnit>(target);
            }
            finally
            {
                // 强转回 TEffect 并交还给全局对象池
                CoreLocator.Pool.Return<TEffect>((TEffect)this);
            }
        }

        /// <summary>
        /// 执行效果逻辑，但【跳过】对象池回收流程。
        /// <para>常用于 CDRule 或长期运行的 Buff 内部，手动 new 出来的 Effect 实例。</para>
        /// </summary>
        /// <param name="target">目标单位。</param>
        public override void ApplyWithoutPool<TUnit>(TUnit target)
        {
            OnApply<TUnit>(target);
        }
        
        /// <summary>
        /// 核心业务逻辑实现入口。
        /// <para>子类应在此处编写具体的数值修改、状态添加等逻辑（如：扣血、加减速）。</para>
        /// </summary>
        /// <param name="target">目标单位。</param>
        public abstract void OnApply<TUnit>(TUnit target) where TUnit : IUnit;
    }
}