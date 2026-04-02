


using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 统一接口。
    /// </summary>
    /// <remarks>
    /// 约定 Unit 需要暴露四类核心容器，并提供对应初始化入口。
    /// </remarks>
    public interface IUnit
    {
        /// <summary>
        /// 属性容器。
        /// </summary>
        public AttributeContainer Attributes { get; }

        /// <summary>
        /// 标记容器。
        /// </summary>
        public MarkContainer Marks { get; }

        /// <summary>
        /// 技能容器。
        /// </summary>
        public AbilityContainer Abilities { get; }

        /// <summary>
        /// 反应容器。
        /// </summary>
        public ReactionContainer Reactions { get; }

        /// <summary>
        /// 初始化属性数据。
        /// </summary>
        void InitAttributes();

        /// <summary>
        /// 初始化标记数据。
        /// </summary>
        void InitMarks();

        /// <summary>
        /// 初始化技能数据。
        /// </summary>
        void InitAbilities();

        /// <summary>
        /// 初始化反应数据。
        /// </summary>
        void InitReactions();

        /// <summary>
        /// 获取属性值的快捷方法。
        /// </summary>
        /// <param name="attributeTag"></param>
        /// <returns></returns>
        public float Value(UnitTag attributeTag) => Attributes.GetValue(attributeTag); 


        public void UpdateUnit(float deltaTime) => Marks.UpdateMarks(deltaTime);


        /// <summary>
        /// 尝试异步执行技能（由当前 Unit 作为 Source）。
        /// </summary>
        /// <param name="abilityTag">技能标签。</param>
        /// <param name="context">执行上下文。</param>
        public UniTask<bool> Use(UnitTag abilityTag, AbilityContext context, CancellationToken cancellationToken = default)
             => Abilities.TryExecuteAsync(abilityTag, context, cancellationToken);
        
        

        public void Enable(UnitTag reactionTag, bool enable) => Reactions.Enable(reactionTag, enable);
        
        /// <summary>
        /// 对当前 Unit 应用一个即时效果。
        /// </summary>
        /// <param name="effect">要执行的效果对象。</param>
        public void ApplyEffect(UnitEffect effect) => effect.Apply(this);

        

        /// <summary>
        /// 清理当前 Unit 的全部容器数据。
        /// </summary>
        public void Clear()
        {
            Attributes.Clear();
            Marks.Clear();
            Abilities.Clear();
            Reactions.Clear();
        }
    }

    /// <summary>
    /// Unit 抽象基类。
    /// </summary>
    /// <remarks>
    /// 基类负责创建四类核心容器，具体初始化逻辑由派生类实现。
    /// </remarks>
    public abstract class BaseUnit : IUnit
    {
        /// <summary>
        /// 属性容器实例。
        /// </summary>
        public AttributeContainer Attributes { get; private set; }

        /// <summary>
        /// 标记容器实例。
        /// </summary>
        public MarkContainer Marks { get; private set; }

        /// <summary>
        /// 技能容器实例。
        /// </summary>
        public AbilityContainer Abilities { get; private set; }

        /// <summary>
        /// 反应容器实例。
        /// </summary>
        public ReactionContainer Reactions { get; private set; }

        /// <summary>
        /// 初始化属性数据。
        /// </summary>
        public virtual void InitAttributes() => Attributes = new AttributeContainer();

        /// <summary>
        /// 初始化标记数据。
        /// </summary>
        public virtual void InitMarks() => Marks = new MarkContainer();

        /// <summary>
        /// 初始化技能数据。
        /// </summary>
        public virtual void InitAbilities() => Abilities = new AbilityContainer();

        /// <summary>
        /// 初始化反应数据。
        /// </summary>
        public virtual void InitReactions() => Reactions = new ReactionContainer();

        /// <summary>
        /// 获取属性值的快捷方法。
        /// </summary>
        /// <param name="attributeTag"></param>
        /// <returns></returns>
        public float Value(UnitTag attributeTag) => Attributes.GetValue(attributeTag); 

        /// <summary>
        /// 更新标记数据。
        /// </summary>
        /// <param name="deltaTime"></param>
        public void UpdateUnit(float deltaTime) => Marks.UpdateMarks(deltaTime);


        /// <summary>
        /// 尝试异步执行技能（由当前 Unit 作为 Source）。
        /// </summary>
        /// <param name="abilityTag">技能标签。</param>
        /// <param name="context">执行上下文。</param>
        public UniTask<bool> Use(UnitTag abilityTag, AbilityContext context, CancellationToken cancellationToken = default)
             => Abilities.TryExecuteAsync(abilityTag, context, cancellationToken);
        

        public void Enable(UnitTag reactionTag, bool enable) => Reactions.Enable(reactionTag, enable);
        
        /// <summary>
        /// 对当前 Unit 应用一个即时效果。
        /// </summary>
        /// <param name="effect">要执行的效果对象。</param>
        public void ApplyEffect(UnitEffect effect) => effect.Apply(this);

        

        /// <summary>
        /// 清理当前 Unit 的全部容器数据。
        /// </summary>
        public void Clear()
        {
            Attributes.Clear();
            Marks.Clear();
            Abilities.Clear();
            Reactions.Clear();
        }
    }
}