namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 技能冷却时间拦截规则。
    /// 巧妙复用状态系统：通过给施法者挂载一个带 Duration 的隐形 CDMark，来拦截技能重发。
    /// </summary>
    public class CDRule : AbilityRule
    {
        /// <summary>冷却标记标签</summary>
        public UnitTag CDTag { get; }

        /// <summary>冷却持续时间（秒）</summary>
        public float Duration { get; }

        /// <summary>
        /// 创建冷却规则。
        /// </summary>
        /// <param name="cdTag">冷却标记标签</param>
        /// <param name="duration">冷却持续时间（秒）</param>
        public CDRule(UnitTag cdTag, float duration)
        {
            CDTag = cdTag;
            Duration = duration;
        }

        /// <summary>
        /// 如果施法者身上找不到这个特定的冷却标记，则允许施放。
        /// </summary>
        public override bool Check(AbilityContext context)
        {
            return context.Source.Marks.HasTag(CDTag) == false;
        }

        /// <summary>
        /// 技能被确认执行瞬间，从工厂生成一个专属冷却标记并强行挂载到施法者身上。
        /// </summary>
        public override void Commit(AbilityContext context)
        {
            // 必须用 CDTag 命名，否则 Check() 按 CDTag 查不到该标记，冷却会失效
            var cdMark = UnitCore.CreateMark<CDMark>().SetName(CDTag).SetStack(1).SetDuration(Duration);
            if (cdMark != null)
            {
                MarkAddEffect.Create()
                    .Set(cdMark)
                    .Apply(context.Source);
            }
        }
    }

    /// <summary>
    /// 专用的空白冷却标记实体。
    /// 它的唯一使命就是存在着，直到时间倒数完毕自然死亡。
    /// </summary>
    public class CDMark : UnitMark
    {
        private UnitTag _name;
        /// <summary>标记名称（由 CDRule 动态设置）</summary>
        public override UnitTag Name { get => _name; protected set => _name = value; }

        /// <summary>无参构造，满足反序列化工厂要求</summary>
        public CDMark() { }

        /// <summary>此方法允许底层框架动态构建各种技能的不同名称冷却</summary>
        public CDMark SetName(UnitTag name)
        {
            _name = name;
            return this;
        }
    }
}
