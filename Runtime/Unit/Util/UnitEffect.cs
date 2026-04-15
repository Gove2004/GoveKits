using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 效果基类（非泛型公共抽象层）。
    /// <para>基于命令模式设计，代表对单位（IUnit）施加的一次性或持续性动作载体。</para>
    /// </summary>
    public abstract class UnitEffect : IPoolable
    {
        /// <summary>
        /// 应用效果并【自动回收入池】。
        /// 强烈推荐用于战斗逻辑中的一次性伤害、临时 Buff 添加等场景。
        /// </summary>
        public abstract void Apply<TUnit>(TUnit target) where TUnit : IUnit;

        /// <summary>
        /// 应用效果且【不进行回收】。
        /// 适用于将 Effect 作为成员变量长期持有，并反复执行（如定期恢复血量的内部缓存对象）。
        /// </summary>
        public abstract void ApplyWithoutPool<TUnit>(TUnit target) where TUnit : IUnit;

        /// <summary>
        /// 对象池回收时调用的清理钩子。
        /// 必须在此处清空所有目标引用及状态缓存，防止对象复用时发生脏数据泄露。
        /// </summary>
        public abstract void OnRecycle();
    }

    /// <summary>
    /// 泛型 Unit 效果基类 (CRTP 奇异递归模板模式)。
    /// <para>职责：支持流畅的链式赋值接口 (Fluent API)，并提供极其严格的 0GC 安全对象池管控。</para>
    /// </summary>
    /// <example>
    /// DamageEffect.Create()
    ///     .SetDamage(100)
    ///     .Apply(enemy); 
    /// </example>
    public abstract class UnitEffect<TEffect> : UnitEffect where TEffect : UnitEffect<TEffect>, new()
    {
        /// <summary>
        /// 从全局对象池中取出一个 Effect 实例准备赋值。
        /// <para>警告：取得实例后必须确保执行了 <see cref="Apply"/> 方法，否则会造成内存池流失。</para>
        /// </summary>
        public static TEffect Create()
        {
            return PoolCore.Get<TEffect>();
        }

        /// <summary>
        /// 核心业务入口：在此处编写如扣血、施加标记等具体业务代码。
        /// </summary>
        public abstract void OnApply<TUnit>(TUnit target) where TUnit : IUnit;

        /// <summary>
        /// 自动回收机制封装。采用 try-finally 结构提供崩溃保护。
        /// </summary>
        public override void Apply<TUnit>(TUnit target)
        {
            try
            {
                OnApply(target);
            }
            finally
            {
                // 即使核心逻辑发生报错异常，也会强制将其塞回对象池。
                PoolCore.Return((TEffect)this);
            }
        }

        public override void ApplyWithoutPool<TUnit>(TUnit target)
        {
            OnApply(target);
        }
    }
}