using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 基于 MonoBehaviour 的实体表现层载体。
    /// 涵盖模型展示、动画播放，以及底层的四大容器。
    /// </summary>
    public abstract class UnitBehaviour : MonoBehaviour, IUnit
    {
        public AttributeContainer Attributes { get; protected set; }
        public MarkContainer Marks { get; protected set; }
        public AbilityContainer Abilities { get; protected set; }
        public ReactionContainer Reactions { get; protected set; }

        // 【重构核心】在实例化容器时，将自己 (this) 传递进去
        public virtual void InitAttributes() => Attributes = new AttributeContainer(this);
        public virtual void InitMarks() => Marks = new MarkContainer(this);
        public virtual void InitAbilities() => Abilities = new AbilityContainer(this);
        public virtual void InitReactions() => Reactions = new ReactionContainer(this);

        protected virtual void Awake()
        {
            InitAttributes();
            InitMarks();
            InitAbilities();
            InitReactions();
        }

        protected virtual void Update()
        {
            this.UpdateUnit(Time.deltaTime); // 调用扩展方法更新标记流水线
        }

        public void Clear()
        {
            Attributes.Clear();
            Marks.Clear();
            Abilities.Clear();
            Reactions.Clear();
        }

        protected virtual void OnDestroy()
        {
            this.Clear();
        }
    }
}