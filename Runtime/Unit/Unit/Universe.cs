using System.Threading;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 全局“宇宙”对象（全局系统单例宿主）。
    /// 可以用来挂载那些针对全局世界生效的 Buff（比如：全服经验双倍、天气变化状态）。
    /// </summary>
    public class Universe : CSharpSingleton<Universe>, IUnit
    {
        public AttributeContainer Attributes { get; protected set; }
        public MarkContainer Marks { get; protected set; }
        public AbilityContainer Abilities { get; protected set; }
        public ReactionContainer Reactions { get; protected set; }

        public void InitAttributes() => Attributes = new AttributeContainer(this);
        public void InitMarks() => Marks = new MarkContainer(this);
        public void InitAbilities() => Abilities = new AbilityContainer(this);
        public void InitReactions() => Reactions = new ReactionContainer(this);

        protected override void Init()
        {
            base.Init();
            InitAttributes();
            InitMarks();
            InitAbilities();
            InitReactions();
        }

        protected override void Uninit()
        {
            base.Uninit();
            this.Clear();
        }

        public void Update(float deltaTime)
        {
            this.UpdateUnit(deltaTime);
        }
        
        public void Clear()
        {
            Attributes.Clear();
            Marks.Clear();
            Abilities.Clear();
            Reactions.Clear();
        }
    }
}