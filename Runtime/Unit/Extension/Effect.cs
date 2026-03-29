
namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 对运行时属性施加数值变化的即时效果。
    /// </summary>
    /// <remarks>
    /// 典型用途：扣血、加血、能量变化。
    /// </remarks>
    public class AttributeChangeEffect : UnitEffect<AttributeChangeEffect>
    {
        /// <summary>
        /// 目标属性标签。
        /// </summary>
        public UnitTag AttributeKey { get; private set; }

        /// <summary>
        /// 变化量（正数恢复，负数扣减）。
        /// </summary>
        public float ChangeValue { get; private set; }

        public AttributeChangeEffect()
        {
        }

        public AttributeChangeEffect(UnitTag attributeKey, float changeValue)
        {
            Set(attributeKey, changeValue);
        }

        /// <summary>
        /// 设置效果参数，便于池化对象复用。
        /// </summary>
        public AttributeChangeEffect Set(UnitTag attributeKey, float changeValue)
        {
            AttributeKey = attributeKey;
            ChangeValue = changeValue;
            return this;
        }

        public override void OnApply(IUnit target)
        {
            target.Attributes.Change(AttributeKey, ChangeValue);
        }

        public override void OnRecycle()
        {
            AttributeKey = default;
            ChangeValue = 0f;
        }
    }


    /// <summary>
    /// 为状态属性添加修改器的即时效果。
    /// </summary>
    public class AttributeModifierAddEffect : UnitEffect<AttributeModifierAddEffect>
    {
        /// <summary>
        /// 目标状态属性标签。
        /// </summary>
        public UnitTag AttributeKey { get; private set; }

        /// <summary>
        /// 要添加的修改器。
        /// </summary>
        public AttributeModifier Modifier { get; private set; }

        public AttributeModifierAddEffect()
        {
        }

        public AttributeModifierAddEffect(UnitTag attributeKey, AttributeModifier modifier)
        {
            Set(attributeKey, modifier);
        }

        /// <summary>
        /// 设置效果参数，便于池化对象复用。
        /// </summary>
        public AttributeModifierAddEffect Set(UnitTag attributeKey, AttributeModifier modifier)
        {
            AttributeKey = attributeKey;
            Modifier = modifier;
            return this;
        }

        public override void OnApply(IUnit target)
        {
            target.Attributes.ApplyModifier(AttributeKey, Modifier);
        }

        public override void OnRecycle()
        {
            AttributeKey = default;
            Modifier = default;
        }
    }

    /// <summary>
    /// 从状态属性移除修改器的即时效果。
    /// </summary>
    public class AttributeModifierRemoveEffect : UnitEffect<AttributeModifierRemoveEffect>
    {
        /// <summary>
        /// 目标状态属性标签。
        /// </summary>
        public UnitTag AttributeKey { get; private set; }

        /// <summary>
        /// 要移除的修改器。
        /// </summary>
        public AttributeModifier Modifier { get; private set; }

        public AttributeModifierRemoveEffect()
        {
        }

        public AttributeModifierRemoveEffect(UnitTag attributeKey, AttributeModifier modifier)
        {
            Set(attributeKey, modifier);
        }

        /// <summary>
        /// 设置效果参数，便于池化对象复用。
        /// </summary>
        public AttributeModifierRemoveEffect Set(UnitTag attributeKey, AttributeModifier modifier)
        {
            AttributeKey = attributeKey;
            Modifier = modifier;
            return this;
        }

        public override void OnApply(IUnit target)
        {
            target.Attributes.Modify(AttributeKey, Modifier);
        }

        public override void OnRecycle()
        {
            AttributeKey = default;
            Modifier = default;
        }
    }

    /// <summary>
    /// 为目标 Unit 添加 Mark 的即时效果。
    /// </summary>
    public class MarkAddEffect : UnitEffect<MarkAddEffect>
    {
        /// <summary>
        /// 要添加的标记实例。
        /// </summary>
        public UnitMark Mark { get; private set; }

        public MarkAddEffect()
        {
        }

        public MarkAddEffect(UnitMark mark)
        {
            Set(mark);
        }

        /// <summary>
        /// 设置效果参数，便于池化对象复用。
        /// </summary>
        public MarkAddEffect Set(UnitMark mark)
        {
            Mark = mark;
            return this;
        }

        public override void OnApply(IUnit target)
        {
            if (Mark != null)
            {
                target.Marks.AddMark(Mark);
            }
        }

        public override void OnRecycle()
        {
            Mark = null;
        }
    }

    /// <summary>
    /// 从目标 Unit 移除 Mark 的即时效果。
    /// </summary>
    public class MarkRemoveEffect : UnitEffect<MarkRemoveEffect>
    {
        /// <summary>
        /// 要移除的标记标签。
        /// </summary>
        public UnitTag MarkTag { get; private set; }

        public MarkRemoveEffect()
        {
        }

        public MarkRemoveEffect(UnitTag markTag)
        {
            Set(markTag);
        }

        public MarkRemoveEffect(UnitMark mark)
        {
            Set(mark != null ? mark.Name : default);
        }

        /// <summary>
        /// 设置效果参数，便于池化对象复用。
        /// </summary>
        public MarkRemoveEffect Set(UnitTag markTag)
        {
            MarkTag = markTag;
            return this;
        }

        public override void OnApply(IUnit target)
        {
            if (MarkTag != UnitTag.None)
            {
                target.Marks.RemoveMark(MarkTag);
            }
        }

        public override void OnRecycle()
        {
            MarkTag = default;
        }
    }

    /// <summary>
    /// 为目标 Unit 添加 Ability 的即时效果。
    /// </summary>
    public class AbilityAddEffect : UnitEffect<AbilityAddEffect>
    {
        /// <summary>
        /// 要添加的技能实例。
        /// </summary>
        public UnitAbility Ability { get; private set; }

        public AbilityAddEffect()
        {
        }

        public AbilityAddEffect(UnitAbility ability)
        {
            Set(ability);
        }

        /// <summary>
        /// 设置效果参数，便于池化对象复用。
        /// </summary>
        public AbilityAddEffect Set(UnitAbility ability)
        {
            Ability = ability;
            return this;
        }

        public override void OnApply(IUnit target)
        {
            if (Ability != null)
            {
                target.Abilities.AddAbility(Ability);
            }
        }

        public override void OnRecycle()
        {
            Ability = null;
        }
    }

    /// <summary>
    /// 从目标 Unit 移除 Ability 的即时效果。
    /// </summary>
    public class AbilityRemoveEffect : UnitEffect<AbilityRemoveEffect>
    {
        /// <summary>
        /// 要移除的技能标签。
        /// </summary>
        public UnitTag AbilityTag { get; private set; }

        public AbilityRemoveEffect()
        {
        }

        public AbilityRemoveEffect(UnitTag abilityTag)
        {
            Set(abilityTag);
        }

        /// <summary>
        /// 设置效果参数，便于池化对象复用。
        /// </summary>
        public AbilityRemoveEffect Set(UnitTag abilityTag)
        {
            AbilityTag = abilityTag;
            return this;
        }

        public override void OnApply(IUnit target)
        {
            target.Abilities.RemoveAbility(AbilityTag);
        }

        public override void OnRecycle()
        {
            AbilityTag = default;
        }
    }

    /// <summary>
    /// 为目标 Unit 添加 Reaction 的即时效果。
    /// </summary>
    public class ReactionAddEffect : UnitEffect<ReactionAddEffect>
    {
        /// <summary>
        /// 要添加的反应实例。
        /// </summary>
        public UnitReaction Reaction { get; private set; }

        public ReactionAddEffect()
        {
        }

        public ReactionAddEffect(UnitReaction reaction)
        {
            Set(reaction);
        }

        /// <summary>
        /// 设置效果参数，便于池化对象复用。
        /// </summary>
        public ReactionAddEffect Set(UnitReaction reaction)
        {
            Reaction = reaction;
            return this;
        }

        public override void OnApply(IUnit target)
        {
            if (Reaction != null)
            {
                target.Reactions.AddReaction(Reaction);
            }
        }

        public override void OnRecycle()
        {
            Reaction = null;
        }
    }

    /// <summary>
    /// 从目标 Unit 移除 Reaction 的即时效果。
    /// </summary>
    public class ReactionRemoveEffect : UnitEffect<ReactionRemoveEffect>
    {
        /// <summary>
        /// 要移除的反应标签。
        /// </summary>
        public UnitTag ReactionTag { get; private set; }

        public ReactionRemoveEffect()
        {
        }

        public ReactionRemoveEffect(UnitTag reactionTag)
        {
            Set(reactionTag);
        }

        /// <summary>
        /// 设置效果参数，便于池化对象复用。
        /// </summary>
        public ReactionRemoveEffect Set(UnitTag reactionTag)
        {
            ReactionTag = reactionTag;
            return this;
        }

        public override void OnApply(IUnit target)
        {
            target.Reactions.RemoveReaction(ReactionTag);
        }

        public override void OnRecycle()
        {
            ReactionTag = default;
        }
    }
}