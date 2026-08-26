using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 全局"宇宙"对象（全局系统单例宿主）。
    /// 可用于挂载针对全局世界生效的 Buff，例如全服经验双倍、天气变化状态等。
    /// </summary>
    public class Universe : CSharpSingleton<Universe>, IUnit
    {
        /// <summary>属性容器</summary>
        public AttributeContainer Attributes { get; protected set; }
        /// <summary>标记容器</summary>
        public MarkContainer Marks { get; protected set; }
        /// <summary>技能容器</summary>
        public AbilityContainer Abilities { get; protected set; }
        /// <summary>反应容器</summary>
        public ReactionContainer Reactions { get; protected set; }

        /// <summary>初始化属性容器</summary>
        public void InitAttributes() => Attributes = new AttributeContainer(this);
        /// <summary>初始化标记容器</summary>
        public void InitMarks() => Marks = new MarkContainer(this);
        /// <summary>初始化技能容器</summary>
        public void InitAbilities() => Abilities = new AbilityContainer(this);
        /// <summary>初始化反应容器</summary>
        public void InitReactions() => Reactions = new ReactionContainer(this);

        /// <summary>
        /// 初始化全部四大容器并启动单例。
        /// </summary>
        protected override void Init()
        {
            base.Init();
            InitAttributes();
            InitMarks();
            InitAbilities();
            InitReactions();
        }

        /// <summary>
        /// 反初始化并清理全部容器数据。
        /// </summary>
        protected override void Uninit()
        {
            base.Uninit();
            this.Clear();
        }

        /// <summary>
        /// 每帧驱动标记容器的 Tick 逻辑。
        /// </summary>
        public void Update(float deltaTime)
        {
            if (Marks != null)
                this.UpdateUnit(deltaTime);
        }

        /// <summary>清理全部容器状态</summary>
        public void Clear()
        {
            Attributes?.Clear();
            Marks?.Clear();
            Abilities?.Clear();
            Reactions?.Clear();
        }

    }
}
