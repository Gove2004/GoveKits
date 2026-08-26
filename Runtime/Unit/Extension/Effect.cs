namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 对基础属性施加数值永久变化的即时效果（如掉血、耗蓝）。
    /// </summary>
    public class AttributeChangeEffect : UnitEffect<AttributeChangeEffect>
    {
        /// <summary>目标属性标签</summary>
        public UnitTag AttributeKey { get; private set; }

        /// <summary>变化值（正数为增加，负数为减少）</summary>
        public float ChangeValue { get; private set; }

        /// <summary>配置效果参数</summary>
        public AttributeChangeEffect Set(UnitTag attributeKey, float changeValue)
        {
            AttributeKey = attributeKey;
            ChangeValue = changeValue;
            return this;
        }

        /// <summary>应用效果到目标属性</summary>
        public override void OnApply<TUnit>(TUnit target) => target.Attributes.ChangeBase(AttributeKey, ChangeValue);

        /// <summary>回收时重置状态</summary>
        public override void OnRecycle() { AttributeKey = default; ChangeValue = 0f; }
    }

    /// <summary>
    /// 为属性添加持续性修改器的即时效果（如穿装备、吃增益 Buff）。
    /// </summary>
    public class AttributeModifierAddEffect : UnitEffect<AttributeModifierAddEffect>
    {
        /// <summary>目标属性标签</summary>
        public UnitTag AttributeKey { get; private set; }

        /// <summary>要添加的属性修改器</summary>
        public AttributeModifier Modifier { get; private set; }

        /// <summary>配置效果参数</summary>
        public AttributeModifierAddEffect Set(UnitTag attributeKey, AttributeModifier modifier)
        {
            AttributeKey = attributeKey;
            Modifier = modifier;
            return this;
        }

        /// <summary>应用效果到目标属性</summary>
        public override void OnApply<TUnit>(TUnit target) => target.Attributes.AddModifier(AttributeKey, Modifier);

        /// <summary>回收时重置状态</summary>
        public override void OnRecycle() { AttributeKey = default; Modifier = default; }
    }

    /// <summary>
    /// 移除状态修改器的即时效果。
    /// </summary>
    public class AttributeModifierRemoveEffect : UnitEffect<AttributeModifierRemoveEffect>
    {
        /// <summary>目标属性标签</summary>
        public UnitTag AttributeKey { get; private set; }

        /// <summary>要移除的修改器来源</summary>
        public ModifierSource Source { get; private set; }

        /// <summary>配置效果参数</summary>
        public AttributeModifierRemoveEffect Set(UnitTag attributeKey, ModifierSource source)
        {
            AttributeKey = attributeKey;
            Source = source;
            return this;
        }

        /// <summary>应用效果到目标属性</summary>
        public override void OnApply<TUnit>(TUnit target) => target.Attributes.RemoveModifier(AttributeKey, Source);

        /// <summary>回收时重置状态</summary>
        public override void OnRecycle() { AttributeKey = default; Source = default; }
    }

    /// <summary>
    /// 为单位施加状态标记的即时效果。
    /// </summary>
    public class MarkAddEffect : UnitEffect<MarkAddEffect>
    {
        /// <summary>要施加的标记实例</summary>
        public UnitMark Mark { get; private set; }

        /// <summary>配置效果参数</summary>
        public MarkAddEffect Set(UnitMark mark)
        {
            Mark = mark;
            return this;
        }

        /// <summary>应用效果到目标标记容器</summary>
        public override void OnApply<TUnit>(TUnit target) => target.Marks.AddMark(Mark);

        /// <summary>回收时重置状态</summary>
        public override void OnRecycle() { Mark = null; }
    }

    /// <summary>
    /// 强制净化/移除指定状态标记的即时效果。
    /// </summary>
    public class MarkRemoveEffect : UnitEffect<MarkRemoveEffect>
    {
        /// <summary>要移除的标记标签</summary>
        public UnitTag MarkTag { get; private set; }

        /// <summary>配置效果参数</summary>
        public MarkRemoveEffect Set(UnitTag markTag)
        {
            MarkTag = markTag;
            return this;
        }

        /// <summary>应用效果到目标标记容器</summary>
        public override void OnApply<TUnit>(TUnit target) => target.Marks.RemoveMark(MarkTag);

        /// <summary>回收时重置状态</summary>
        public override void OnRecycle() { MarkTag = default; }
    }

    /// <summary>
    /// 为单位挂载/赋予某项新技能的能力。
    /// </summary>
    public class AbilityAddEffect : UnitEffect<AbilityAddEffect>
    {
        /// <summary>要添加的技能实例</summary>
        public UnitAbility Ability { get; private set; }

        /// <summary>配置效果参数</summary>
        public AbilityAddEffect Set(UnitAbility ability)
        {
            Ability = ability;
            return this;
        }

        /// <summary>应用效果到目标技能容器</summary>
        public override void OnApply<TUnit>(TUnit target) => target.Abilities.AddAbility(Ability);

        /// <summary>回收时重置状态</summary>
        public override void OnRecycle() { Ability = null; }
    }

    /// <summary>
    /// 褫夺/移除指定技能的能力。
    /// </summary>
    public class AbilityRemoveEffect : UnitEffect<AbilityRemoveEffect>
    {
        /// <summary>要移除的技能标签</summary>
        public UnitTag AbilityTag { get; private set; }

        /// <summary>配置效果参数</summary>
        public AbilityRemoveEffect Set(UnitTag abilityTag)
        {
            AbilityTag = abilityTag;
            return this;
        }

        /// <summary>应用效果到目标技能容器</summary>
        public override void OnApply<TUnit>(TUnit target) => target.Abilities.RemoveAbility(AbilityTag);

        /// <summary>回收时重置状态</summary>
        public override void OnRecycle() { AbilityTag = default; }
    }

    /// <summary>
    /// 挂载被动监听反应的能力。
    /// </summary>
    public class ReactionAddEffect : UnitEffect<ReactionAddEffect>
    {
        /// <summary>要添加的反应实例</summary>
        public UnitReaction Reaction { get; private set; }

        /// <summary>配置效果参数</summary>
        public ReactionAddEffect Set(UnitReaction reaction)
        {
            Reaction = reaction;
            return this;
        }

        /// <summary>应用效果到目标反应容器</summary>
        public override void OnApply<TUnit>(TUnit target) => target.Reactions.AddReaction(Reaction);

        /// <summary>回收时重置状态</summary>
        public override void OnRecycle() { Reaction = null; }
    }

    /// <summary>
    /// 移除指定被动监听反应的能力。
    /// </summary>
    public class ReactionRemoveEffect : UnitEffect<ReactionRemoveEffect>
    {
        /// <summary>要移除的反应标签</summary>
        public UnitTag ReactionTag { get; private set; }

        /// <summary>配置效果参数</summary>
        public ReactionRemoveEffect Set(UnitTag reactionTag)
        {
            ReactionTag = reactionTag;
            return this;
        }

        /// <summary>应用效果到目标反应容器</summary>
        public override void OnApply<TUnit>(TUnit target) => target.Reactions.RemoveReaction(ReactionTag);

        /// <summary>回收时重置状态</summary>
        public override void OnRecycle() { ReactionTag = default; }
    }
}
