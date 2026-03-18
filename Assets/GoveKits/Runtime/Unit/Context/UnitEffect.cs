using GoveKits.Runtime.Core.Pool;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 即时效果基类。
    /// </summary>
    /// <remarks>
    /// 典型用途：伤害、治疗、回蓝、清除状态等一次性生效逻辑。
    /// 生效后会自动回收到对象池。
    /// </remarks>
    public abstract class UnitEffect : IPoolable
    {
        /// <summary>
        /// 执行效果并自动回池。
        /// </summary>
        /// <param name="target">效果目标 Unit。</param>
        internal void Apply(IUnit target)
        {
            OnApply(target);
            PoolCore.Return(this);
        }

        /// <summary>
        /// 实际效果逻辑。
        /// </summary>
        /// <param name="target">效果目标 Unit。</param>
        public abstract void OnApply(IUnit target);

        /// <summary>
        /// 回池前的重置逻辑。
        /// </summary>
        /// <remarks>
        /// 在这里清理临时状态，避免复用对象时脏数据泄漏。
        /// </remarks>
        public abstract void OnRecycle();
    }
}