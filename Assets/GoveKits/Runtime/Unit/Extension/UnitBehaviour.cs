


using Unity.VisualScripting.YamlDotNet.Core;
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
    }
}