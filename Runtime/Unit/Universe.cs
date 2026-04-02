


using System.Threading;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Unit
{
    public class Universe : CSharpSingleton<Universe>, IUnit
    {
        public AttributeContainer Attributes { get; protected set; }
        public MarkContainer Marks { get; protected set; }
        public AbilityContainer Abilities { get; protected set; }
        public ReactionContainer Reactions { get; protected set; }

        public void InitAbilities()
        {
            // 添加技能
            Abilities = new AbilityContainer();
        }

        public void InitAttributes()
        {
            // 添加属性
            Attributes = new AttributeContainer();  
        }

        public void InitMarks()
        {
            // 添加标记
            Marks = new MarkContainer();
        }

        public void InitReactions()
        {
            // 添加反应
            Reactions = new ReactionContainer();
        }


        protected override void Init()
        {
            base.Init();
            InitAttributes();
            InitAbilities();
            InitMarks();
            InitReactions();
        }

        protected override void Uninit()
        {
            base.Uninit();
            Clear();
        }
        

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
}