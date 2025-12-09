using UnityEngine;

namespace GoveKits.Unit
{
    /// <summary>
    /// Unity 组件式的 Unit 封装：把 IGameUnit 的生命周期绑定到 MonoBehaviour。
    /// - 在 Start 中自动创建四个容器（Attributes/Marks/Abilities/Reactions）。
    /// - 在 Update 中把 deltaTime 转发给各容器的 Update（例如 Mark 的 Tick）。
    /// - 在 OnDestroy 中负责清理容器以释放资源与取消订阅。
    /// </summary>
    public abstract class UnitBehaviour : MonoBehaviour, IGameUnit
    {
        public AttributeContainer Attributes { get; protected set; }
        public MarkContainer Marks { get; protected set; }
        public AbilityContainer Abilities { get; protected set; }
        public ReactionContainer Reactions { get; protected set; }

        public virtual void InitializeAttributes()
        {
            Attributes = new AttributeContainer(this);
        }

        public virtual void InitializeMarks()
        {
            Marks = new MarkContainer(this);
        }

        public virtual void InitializeAbilities()
        {
            Abilities = new AbilityContainer(this);
        }

        public virtual void InitializeReactions()
        {
            Reactions = new ReactionContainer(this);
        }


        public virtual void Start()
        {
            // 默认初始化顺序：先属性 -> 标记 -> 能力 -> 反应
            InitializeAttributes();
            InitializeMarks();
            InitializeAbilities();
            InitializeReactions();
        }

        public virtual void Update()
        {
            // 把 Unity 的帧时间转发给容器，容器根据需要处理 Tick
            Attributes.Update(Time.deltaTime);
            Marks.Update(Time.deltaTime);
            Abilities.Update(Time.deltaTime);
            Reactions.Update(Time.deltaTime);
        }

        public virtual void OnDestroy()
        {
            // 清理资源，确保事件订阅被解除
            Attributes.Clear();
            Marks.Clear();
            Abilities.Clear();
            Reactions.Clear();
        }
    }
}