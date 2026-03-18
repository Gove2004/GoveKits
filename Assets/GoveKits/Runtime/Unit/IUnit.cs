


using Cysharp.Threading.Tasks;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 容器类型枚举。
    /// </summary>
    public enum UnitContainerType
    {
        /// <summary>
        /// 属性容器（AttributeContainer）。
        /// </summary>
        Attribute = 1,

        /// <summary>
        /// 标记容器（MarkContainer）。
        /// </summary>
        Mark = 1 << 1,

        /// <summary>
        /// 技能容器（AbilityContainer）。
        /// </summary>
        Ability = 1 << 2,

        /// <summary>
        /// 反应容器（ReactionContainer）。
        /// </summary>
        Reaction = 1 << 3
    }

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
        float Value(UnitTag attributeTag) => Attributes.GetValue(attributeTag); 

        /// <summary>
        /// 尝试异步执行技能（由当前 Unit 作为 Source）。
        /// </summary>
        /// <param name="abilityTag">技能标签。</param>
        /// <param name="target">技能目标，可为空。</param>
        UniTask<bool> Use(UnitTag abilityTag, UnitContext context) => Abilities.TryExecuteAsync(abilityTag, context);
        
        /// <summary>
        /// 对当前 Unit 应用一个即时效果。
        /// </summary>
        /// <param name="effect">要执行的效果对象。</param>
        void Apply(UnitEffect effect) => effect.Apply(this);
    }

    /// <summary>
    /// Unit 抽象基类。
    /// </summary>
    /// <remarks>
    /// 基类负责创建四类核心容器，具体初始化逻辑由派生类实现。
    /// </remarks>
    public abstract class UnitBase : IUnit
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
        /// 构造并初始化基础容器。
        /// </summary>
        protected UnitBase()
        {
            Attributes = new AttributeContainer();
            Marks = new MarkContainer();
            Abilities = new AbilityContainer();
            Reactions = new ReactionContainer();
        }

        /// <summary>
        /// 初始化属性数据。
        /// </summary>
        public abstract void InitAttributes();

        /// <summary>
        /// 初始化标记数据。
        /// </summary>
        public abstract void InitMarks();

        /// <summary>
        /// 初始化技能数据。
        /// </summary>
        public abstract void InitAbilities();

        /// <summary>
        /// 初始化反应数据。
        /// </summary>
        public abstract void InitReactions();
    }
}