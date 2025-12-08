
namespace GoveKits.Unit
{
    /// <summary>
    /// 单位接口：对外暴露四个核心容器（Attributes/Marks/Abilities/Reactions）以及初始化入口。
    /// </summary>
    public interface IGameUnit
    {
        /// <summary>属性容器（计算型 / 运行时资源）</summary>
        AttributeContainer Attributes { get; }
        /// <summary>状态/标记容器（用于处理冷却、Buff、Debuff 等）</summary>
        MarkContainer Marks { get; }
        /// <summary>能力容器（持有该单位所有能力）</summary>
        AbilityContainer Abilities { get; }
        /// <summary>反应容器（注册并管理该单位的被动反应 / 事件处理器）</summary>
        ReactionContainer Reactions { get; }

        /// <summary>初始化并创建属性容器（State / Runtime 属性）</summary>
        void InitializeAttributes();
        /// <summary>初始化并创建标记容器</summary>
        void InitializeMarks();
        /// <summary>初始化并创建能力容器</summary>
        void InitializeAbilities();
        /// <summary>初始化并创建反应容器</summary>
        void InitializeReactions();
    }


    /// <summary>
    /// 单位抽象基类，实现了基本容器的创建与持有逻辑。
    /// 子类可重载初始化流程以进行更复杂的构造（如注册默认属性、能力等）。
    /// </summary>
    public abstract class GameUnit : IGameUnit
    {
        /// <summary>单位的属性容器（由 InitializeAttributes 创建）</summary>
        public AttributeContainer Attributes { get; protected set; }
        /// <summary>单位的标记容器（由 InitializeMarks 创建）</summary>
        public MarkContainer Marks { get; protected set; }
        /// <summary>单位的能力容器（由 InitializeAbilities 创建）</summary>
        public AbilityContainer Abilities { get; protected set; }
        /// <summary>单位的反应容器（由 InitializeReactions 创建）</summary>
        public ReactionContainer Reactions { get; protected set; }

        /// <summary>默认创建 AttributeContainer 的实现，子类可覆盖以注入自定义容器</summary>
        public virtual void InitializeAttributes()
        {
            Attributes = new AttributeContainer(this);
        }

        /// <summary>默认创建 MarkContainer 的实现，子类可覆盖</summary>
        public virtual void InitializeMarks()
        {
            Marks = new MarkContainer(this);
        }

        /// <summary>默认创建 AbilityContainer 的实现，子类可覆盖</summary>
        public virtual void InitializeAbilities()
        {
            Abilities = new AbilityContainer(this);
        }

        /// <summary>默认创建 ReactionContainer 的实现，子类可覆盖</summary>
        public virtual void InitializeReactions()
        {
            Reactions = new ReactionContainer(this);
        }
    }
}