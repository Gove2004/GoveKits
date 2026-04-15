using System.Threading;
using Cysharp.Threading.Tasks;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 核心 Unit 统一接口。
    /// <para>约定 Unit 必须暴露四大核心容器，作为能力系统的宿主。</para>
    /// </summary>
    public interface IUnit
    {
        AttributeContainer Attributes { get; }
        MarkContainer Marks { get; }
        AbilityContainer Abilities { get; }
        ReactionContainer Reactions { get; }

        void InitAttributes();
        void InitMarks();
        void InitAbilities();
        void InitReactions();

        /// <summary>清理当前 Unit 的全部容器数据</summary>
        void Clear();
    }


    /// <summary>
    /// IUnit 扩展方法。
    /// 封装常用的快捷操作，避免接口过于臃肿。
    /// </summary>
    public static class IUnitExtensions
    {
        /// <summary>获取属性当前值的快捷方法</summary>
        public static float Value(this IUnit unit, UnitTag attributeTag) 
            => unit.Attributes.GetValue(attributeTag); 

        /// <summary>驱动单位的 Tick 逻辑 (如 Mark 计时器)</summary>
        public static void UpdateUnit(this IUnit unit, float deltaTime) 
            => unit.Marks.UpdateMarks(deltaTime);

        /// <summary>尝试异步执行自身拥有的技能</summary>
        public static UniTask<bool> Use(this IUnit unit, UnitTag abilityTag, AbilityContext context, CancellationToken cancellationToken = default)
            => unit.Abilities.TryExecuteAsync(abilityTag, context, cancellationToken);
        
        /// <summary>启用或禁用某个反应</summary>
        public static void EnableReaction(this IUnit unit, UnitTag reactionTag, bool enable) 
            => unit.Reactions.Enable(reactionTag, enable);
        
        /// <summary>对当前 Unit 应用一个即时效果</summary>
        public static void ApplyEffect(this IUnit unit, UnitEffect effect) 
            => effect.Apply(unit);
    }
}