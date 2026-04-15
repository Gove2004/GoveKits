namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 技能冷却时间拦截规则。
    /// </summary>
    /// <remarks>
    /// 巧妙复用状态系统：通过给施法者挂载一个带 Duration 的隐形 CDMark，来拦截技能重发。
    /// </remarks>
    public class CDRule : AbilityRule
    {
        public UnitTag CDTag { get; }
        public float Duration { get; }

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
            // 利用数据驱动注册中心创建标记实例
            var cdMark = UnitCore.CreateMark(CDTag, stack: 1, duration: Duration);
            if (cdMark != null)
            {
                // 使用对象池极速应用特效，绝不产生 GC 垃圾
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
        public override UnitTag Name { get => _name; protected set => _name = value; }

        public CDMark() { }

        // 此方法允许底层框架动态构建各种技能的不同名称冷却
        public CDMark SetName(UnitTag name)
        {
            _name = name;
            return this;
        }
    }
}