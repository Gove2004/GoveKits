using System;

namespace GoveKits.Runtime.Unit
{
    // 常用条件示例：基于标签的条件
    public class TagQueryCondition : UnitCondition
    {
        private readonly TagQuery query;
        private readonly UnitContainerType containerType;
        public TagQueryCondition(TagQuery query, UnitContainerType containerType)
        {
            this.query = query;
            this.containerType = containerType;
        }
        public override bool Check(UnitContext context)
        {
            switch (containerType)
            {
                case UnitContainerType.Attribute:
                    return query.Match(context.Source.Attributes);
                case UnitContainerType.Mark:
                    return query.Match(context.Source.Marks);
                case UnitContainerType.Ability:
                    return query.Match(context.Source.Abilitys);
                case UnitContainerType.Reaction:
                    return query.Match(context.Source.Reactions);
                default:
                    return false;
            }
        }
    }

    // 常用条件示例：基于目标的条件
    public class TargetCondition : UnitCondition
    {
        private readonly IUnit unit;
        public TargetCondition(IUnit unit) => this.unit = unit;
        public override bool Check(UnitContext context) => context.Target == unit;
    }

    // 技能CD条件示例
    public class CDCondition : UnitCondition
    {
        private readonly UnitTag cdTag;
        public CDCondition(UnitTag cdTag) => this.cdTag = cdTag;
        public override bool Check(UnitContext context) => !context.Source.Marks.HasTag(cdTag);
    }
}