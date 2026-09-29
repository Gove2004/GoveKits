using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// IUnit 扩展方法，封装常用的快捷操作，避免接口过于臃肿。
    /// </summary>
    public static class IUnitExtensions
    {
        public static Func<float, float> DefaultValueFunc = x => x;
        /// <summary>
        /// 获取属性当前值并通过 func 限制/转换，默认值为 0。
        /// </summary>
        /// <example>unit.GetValue("HP", x => x < 100 ? x : 100)</example>
        public static float GetValue(this IUnit unit, string attributeName, Func<float, float> func = null)
            => unit.Attributes.GetValue(new UnitTag(attributeName), func ?? DefaultValueFunc);

        /// <summary>驱动单位的 Tick 逻辑（如 Mark 计时器）</summary>
        public static void UpdateUnit(this IUnit unit, float deltaTime)
            => unit.Marks.UpdateMarks(deltaTime);

        /// <summary>尝试异步执行自身拥有的技能</summary>
        public static UniTask<bool> UseAbility(this IUnit unit, UnitTag abilityTag, AbilityContext context, CancellationToken cancellationToken = default)
            => unit.Abilities.TryExecuteAsync(abilityTag, context, cancellationToken);

        /// <summary>启用或禁用某个反应</summary>
        public static void EnableReaction(this IUnit unit, UnitTag reactionTag, bool enable)
            => unit.Reactions.Enable(reactionTag, enable);

        /// <summary>对当前 Unit 应用一个即时效果</summary>
        public static void ApplyEffect(this IUnit unit, UnitEffect effect)
            => effect.Apply(unit);
    }
}
