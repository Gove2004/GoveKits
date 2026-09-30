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

        /// <summary>驱动单位的 Tick 逻辑（如 Mark 计时器），容器未初始化时安全跳过</summary>
        public static void UpdateUnit(this IUnit unit, float deltaTime)
        {
            if (unit.Marks != null)
                unit.Marks.UpdateMarks(deltaTime);
        }

        /// <summary>初始化全部四大容器（依次调用四个 InitXxx，宿主可覆写单个 Init 定制容器）</summary>
        public static void InitAllContainers(this IUnit unit)
        {
            unit.InitAttributes();
            unit.InitMarks();
            unit.InitAbilities();
            unit.InitReactions();
        }

        /// <summary>清理全部容器状态（依次调用四大容器的 Clear，未初始化的容器安全跳过）</summary>
        public static void ClearAllContainers(this IUnit unit)
        {
            unit.Attributes?.Clear();
            unit.Marks?.Clear();
            unit.Abilities?.Clear();
            unit.Reactions?.Clear();
        }

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
