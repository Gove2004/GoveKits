
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 行为接口。
    /// 包含 Unit 的核心行为方法，如应用效果、执行技能等。
    /// </summary>
    public abstract class UnitBehaviour : MonoBehaviour, IUnit
    {
        public AttributeContainer Attributes { get; } = new();
        public MarkContainer Marks { get; } = new();
        public AbilityContainer Abilities { get; } = new();
        public ReactionContainer Reactions { get; } = new();

        public abstract void InitAbilities();
        public abstract void InitAttributes();
        public abstract void InitMarks();
        public abstract void InitReactions();

        protected virtual void Awake()
        {
            InitAttributes();
            InitMarks();
            InitAbilities();
            InitReactions();
        }

        protected virtual void Update()
        {
            Marks.UpdateMarks(Time.deltaTime);
        }

        protected virtual void OnDestroy()
        {
            Attributes.Clear();
            Marks.Clear();
            Abilities.Clear();
            Reactions.Clear();
        }


        /// <summary>
        /// 获取属性值的快捷方法。
        /// </summary>
        /// <param name="attributeTag">属性标签。</param>
        /// <returns>当前属性值。</returns>
        public float Value(UnitTag attributeTag)
            => Attributes.GetValue(attributeTag);

        /// <summary>
        /// 尝试异步执行技能（由当前 Unit 作为 Source）。
        /// </summary>
        /// <param name="abilityTag">技能标签。</param>
        /// <param name="context">执行上下文。</param>
        /// <returns>是否执行成功。</returns>
        public UniTask<bool> Use(UnitTag abilityTag, UnitContext context)
            => Abilities.TryExecuteAsync(abilityTag, context);

        /// <summary>
        /// 对当前 Unit 应用一个即时效果。
        /// </summary>
        /// <param name="effect">要执行的效果对象。</param>
        public void Apply(UnitEffect effect)
            => effect.Apply(this);
    }
}