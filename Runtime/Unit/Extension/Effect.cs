
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

        /// <summary>
        /// 设置效果参数，便于池化对象复用。
        /// </summary>
        public AttributeChangeEffect Set(UnitTag attributeKey, float changeValue)
        {
            AttributeKey = attributeKey;
            ChangeValue = changeValue;
            return this;
        }

        public override void OnApply<TUnit>(TUnit target)
        {
            target.Attributes.ChangeBase(AttributeKey, ChangeValue);
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

        /// <summary>
        /// 设置效果参数，便于池化对象复用。
        /// </summary>
        public AttributeModifierAddEffect Set(UnitTag attributeKey, AttributeModifier modifier)
        {
            AttributeKey = attributeKey;
            Modifier = modifier;
            return this;
        }

        public override void OnApply<TUnit>(TUnit target)
        {
            target.Attributes.AddModifier(AttributeKey, Modifier);
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
        public ModifierSource Source { get; private set; }

        public AttributeModifierRemoveEffect()
        {
        }

        /// <summary>
        /// 设置效果参数，便于池化对象复用。
        /// </summary>
        public AttributeModifierRemoveEffect Set(UnitTag attributeKey, ModifierSource source)
        {
            AttributeKey = attributeKey;
            Source = source;
            return this;
        }

        public override void OnApply<TUnit>(TUnit target)
        {
            target.Attributes.RemoveModifier(AttributeKey, Source);
        }

        public override void OnRecycle()
        {
            AttributeKey = default;
            Source = default;
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

        /// <summary>
        /// 设置效果参数，便于池化对象复用。
        /// </summary>
        public MarkAddEffect Set(UnitMark mark)
        {
            Mark = mark;
            return this;
        }

        public override void OnApply<TUnit>(TUnit target)
        {
            target.Marks.AddMark(Mark);
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

        /// <summary>
        /// 设置效果参数，便于池化对象复用。
        /// </summary>
        public MarkRemoveEffect Set(UnitTag markTag)
        {
            MarkTag = markTag;
            return this;
        }

        public override void OnApply<TUnit>(TUnit target)
        {
            target.Marks.RemoveMark(MarkTag);
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

        /// <summary>
        /// 设置效果参数，便于池化对象复用。
        /// </summary>
        public AbilityAddEffect Set(UnitAbility ability)
        {
            Ability = ability;
            return this;
        }

        public override void OnApply<TUnit>(TUnit target)
        {
            target.Abilities.AddAbility(Ability);
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

        /// <summary>
        /// 设置效果参数，便于池化对象复用。
        /// </summary>
        public AbilityRemoveEffect Set(UnitTag abilityTag)
        {
            AbilityTag = abilityTag;
            return this;
        }

        public override void OnApply<TUnit>(TUnit target)
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

        /// <summary>
        /// 设置效果参数，便于池化对象复用。
        /// </summary>
        public ReactionAddEffect Set(UnitReaction reaction)
        {
            Reaction = reaction;
            return this;
        }

        public override void OnApply<TUnit>(TUnit target)
        {
            target.Reactions.AddReaction(Reaction);
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

        /// <summary>
        /// 设置效果参数，便于池化对象复用。
        /// </summary>
        public ReactionRemoveEffect Set(UnitTag reactionTag)
        {
            ReactionTag = reactionTag;
            return this;
        }

        public override void OnApply<TUnit>(TUnit target)
        {
            target.Reactions.RemoveReaction(ReactionTag);
        }

        public override void OnRecycle()
        {
            ReactionTag = default;
        }
    }
}