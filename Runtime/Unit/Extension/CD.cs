namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 冷却规则。
    /// </summary>
    /// <remarks>
    /// 通过在 Source 上挂载一个 CDMark 来表示技能冷却中。
    /// </remarks>
    public class CDRule : AbilityRule
    {
        /// <summary>
        /// 冷却标记标签。
        /// </summary>
        public UnitTag CDTag { get; }

        /// <summary>
        /// 冷却时长（秒）。
        /// </summary>
        public float duration;

        /// <summary>
        /// 创建一个冷却规则实例。
        /// </summary>
        /// <param name="cdTag">冷却标记标签。</param>
        /// <param name="duration">冷却时长（秒）。</param>
        public CDRule(UnitTag cdTag, float duration)
        {
            CDTag = cdTag;
            this.duration = duration;
        }

        /// <summary>
        /// 检查当前是否不在冷却中。
        /// </summary>
        /// <param name="context">技能执行上下文。</param>
        /// <returns>不在冷却中返回 true。</returns>
        public override bool Check(AbilityContext context)
        {
            return context.Source.Marks.HasTag(CDTag) == false;
        }

        /// <summary>
        /// 提交冷却：向 Source 添加 CDMark。
        /// </summary>
        /// <param name="context">技能执行上下文。</param>
        public override void Commit(AbilityContext context)
        {
            MarkAddEffect.Create()
                .Set(new CDMark(context.Source, CDTag, duration))
                .Apply(context.Source);
        }
    }

    /// <summary>
    /// 冷却标记。
    /// </summary>
    public class CDMark : UnitMark
    {
        /// <summary>
        /// 标记名（通常为 CD.xxx）。
        /// </summary>
        public override UnitTag Name { get; protected set; }

        /// <summary>
        /// 创建冷却标记。
        /// </summary>
        /// <param name="source">归属单位。</param>
        /// <param name="name">标记名。</param>
        /// <param name="duration">持续时间（秒）。</param>
        public CDMark(IUnit source, UnitTag name, float duration) : base(source, duration: duration)
        {
            Name = name;
        }
    }
}