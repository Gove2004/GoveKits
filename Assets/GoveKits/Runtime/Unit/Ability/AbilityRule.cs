

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 技能规则抽象基类。
    /// </summary>
    /// <remarks>
    /// 规则通常用于技能执行前的检查与执行时的提交（如冷却、资源消耗、状态锁定）。
    /// </remarks>
    public abstract class AbilityRule
    {
        /// <summary>
        /// 执行前检查。
        /// </summary>
        /// <param name="context">技能执行上下文。</param>
        /// <returns>满足执行条件返回 true，否则返回 false。</returns>
        public abstract bool Check(UnitContext context);

        /// <summary>
        /// 提交规则副作用。
        /// </summary>
        /// <param name="context">技能执行上下文。</param>
        public abstract void Commit(UnitContext context);
    }
}