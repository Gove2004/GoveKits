namespace GoveKits.Runtime.Unit
{
    /// <summary>对基础属性施加数值永久变化的即时效果（如掉血、耗蓝）。</summary>
    public class AttributeChangeEffect : UnitEffect<AttributeChangeEffect>
    {
        public UnitTag AttributeKey { get; private set; }
        public float ChangeValue { get; private set; }

        public AttributeChangeEffect Set(UnitTag attributeKey, float changeValue)
        {
            AttributeKey = attributeKey;
            ChangeValue = changeValue;
            return this;
        }

        public override void OnApply<TUnit>(TUnit target) => target.Attributes.ChangeBase(AttributeKey, ChangeValue);
        public override void OnRecycle() { AttributeKey = default; ChangeValue = 0f; }
    }

    /// <summary>为属性添加持续性修改器的即时效果（如穿装备、吃增益 Buff）。</summary>
    public class AttributeModifierAddEffect : UnitEffect<AttributeModifierAddEffect>
    {
        public UnitTag AttributeKey { get; private set; }
        public AttributeModifier Modifier { get; private set; }

        public AttributeModifierAddEffect Set(UnitTag attributeKey, AttributeModifier modifier)
        {
            AttributeKey = attributeKey;
            Modifier = modifier;
            return this;
        }

        public override void OnApply<TUnit>(TUnit target) => target.Attributes.AddModifier(AttributeKey, Modifier);
        public override void OnRecycle() { AttributeKey = default; Modifier = default; }
    }

    /// <summary>移除状态修改器的即时效果。</summary>
    public class AttributeModifierRemoveEffect : UnitEffect<AttributeModifierRemoveEffect>
    {
        public UnitTag AttributeKey { get; private set; }
        public ModifierSource Source { get; private set; }

        public AttributeModifierRemoveEffect Set(UnitTag attributeKey, ModifierSource source)
        {
            AttributeKey = attributeKey;
            Source = source;
            return this;
        }

        public override void OnApply<TUnit>(TUnit target) => target.Attributes.RemoveModifier(AttributeKey, Source);
        public override void OnRecycle() { AttributeKey = default; Source = default; }
    }

    /// <summary>为单位施加状态标记的即时效果。</summary>
    public class MarkAddEffect : UnitEffect<MarkAddEffect>
    {
        public UnitMark Mark { get; private set; }

        public MarkAddEffect Set(UnitMark mark)
        {
            Mark = mark;
            return this;
        }

        public override void OnApply<TUnit>(TUnit target) => target.Marks.AddMark(Mark);
        public override void OnRecycle() { Mark = null; }
    }

    /// <summary>强制净化/移除指定状态标记的即时效果。</summary>
    public class MarkRemoveEffect : UnitEffect<MarkRemoveEffect>
    {
        public UnitTag MarkTag { get; private set; }

        public MarkRemoveEffect Set(UnitTag markTag)
        {
            MarkTag = markTag;
            return this;
        }

        public override void OnApply<TUnit>(TUnit target) => target.Marks.RemoveMark(MarkTag);
        public override void OnRecycle() { MarkTag = default; }
    }

    /// <summary>为单位挂载/赋予某项新技能的能力。</summary>
    public class AbilityAddEffect : UnitEffect<AbilityAddEffect>
    {
        public UnitAbility Ability { get; private set; }

        public AbilityAddEffect Set(UnitAbility ability)
        {
            Ability = ability;
            return this;
        }

        public override void OnApply<TUnit>(TUnit target) => target.Abilities.AddAbility(Ability);
        public override void OnRecycle() { Ability = null; }
    }

    /// <summary>褫夺/移除指定技能的能力。</summary>
    public class AbilityRemoveEffect : UnitEffect<AbilityRemoveEffect>
    {
        public UnitTag AbilityTag { get; private set; }

        public AbilityRemoveEffect Set(UnitTag abilityTag)
        {
            AbilityTag = abilityTag;
            return this;
        }

        public override void OnApply<TUnit>(TUnit target) => target.Abilities.RemoveAbility(AbilityTag);
        public override void OnRecycle() { AbilityTag = default; }
    }

    /// <summary>挂载被动监听反应的能力。</summary>
    public class ReactionAddEffect : UnitEffect<ReactionAddEffect>
    {
        public UnitReaction Reaction { get; private set; }

        public ReactionAddEffect Set(UnitReaction reaction)
        {
            Reaction = reaction;
            return this;
        }

        public override void OnApply<TUnit>(TUnit target) => target.Reactions.AddReaction(Reaction);
        public override void OnRecycle() { Reaction = null; }
    }

    /// <summary>移除指定被动监听反应的能力。</summary>
    public class ReactionRemoveEffect : UnitEffect<ReactionRemoveEffect>
    {
        public UnitTag ReactionTag { get; private set; }

        public ReactionRemoveEffect Set(UnitTag reactionTag)
        {
            ReactionTag = reactionTag;
            return this;
        }

        public override void OnApply<TUnit>(TUnit target) => target.Reactions.RemoveReaction(ReactionTag);
        public override void OnRecycle() { ReactionTag = default; }
    }
}