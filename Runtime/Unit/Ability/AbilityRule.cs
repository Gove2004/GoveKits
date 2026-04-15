namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 技能规则抽象基类。
    /// </summary>
    /// <remarks>
    /// 规则用于技能生命周期内的前置校验（如检查蓝量、冷却、眩晕状态）。
    /// 如果检查通过，技能被触发，则调用 Commit 执行具体的消耗扣减行为。
    /// </remarks>
    public abstract class AbilityRule
    {
        /// <summary>
        /// 执行前检查。
        /// </summary>
        /// <param name="context">包含施法者和目标信息的执行上下文。</param>
        /// <returns>满足执行条件返回 true，否则拒绝技能释放返回 false。</returns>
        public abstract bool Check(AbilityContext context);

        /// <summary>
        /// 提交规则产生的副作用（如真正扣除 MP、添加冷却标记）。
        /// 仅在技能确认开始释放的瞬间被触发一次。
        /// </summary>
        public abstract void Commit(AbilityContext context);
    }
}