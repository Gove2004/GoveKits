

using System;

namespace GoveKits.Unit
{
    /// <summary>
    /// 冷却标记
    /// <para>特点：不可堆叠层数，重复添加时刷新持续时间。</para>
    /// </summary>
    public class CooldownMark : GameMark
    {
        public CooldownMark(GameTag tag, float duration) 
            : base(tag, duration, maxStack: 1) // 冷却通常只有1层
        {
        }

        // 重写堆叠逻辑：取最大时间 (刷新冷却)
        public override void OnStack(GameMark newMark)
        {
            // 比如：当前还剩 2s，新冷却 5s -> 变为 5s
            // 比如：当前还剩 5s，被重置为 1s -> 保持 5s (通常冷却不会被短时间覆盖，除非是强制 Reset)
            Duration = Math.Max(Duration, newMark.Duration);
            // 这里的 MaxDuration 不需要变，通常是固定的
        }

        // 可以添加一个静态方法快速构建 Tag
        // 比如技能名为 "Fireball"，冷却 Tag 为 "CD.Fireball"
        public static GameTag GetTag(GameTag skillName) => "CD." + (string)skillName;
    }
}