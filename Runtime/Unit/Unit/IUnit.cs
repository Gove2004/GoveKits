
namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 核心统一接口。
    /// 定义技能系统宿主的四大容器契约，所有具备完整技能能力的实体均需实现此接口。
    /// </summary>
    public interface IUnit
    {
        /// <summary>属性容器</summary>
        AttributeContainer Attributes { get; }
        /// <summary>标记容器</summary>
        MarkContainer Marks { get; }
        /// <summary>技能容器</summary>
        AbilityContainer Abilities { get; }
        /// <summary>反应容器</summary>
        ReactionContainer Reactions { get; }

        /// <summary>初始化属性容器</summary>
        void InitAttributes();
        /// <summary>初始化标记容器</summary>
        void InitMarks();
        /// <summary>初始化技能容器</summary>
        void InitAbilities();
        /// <summary>初始化反应容器</summary>
        void InitReactions();

        /// <summary>清理当前 Unit 的全部容器数据</summary>
        void Clear();
    }
}
