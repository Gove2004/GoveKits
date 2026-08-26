using System.Collections.Generic;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 可完美序列化（如 JSON / MessagePack）的 Unit 纯数据结构。
    /// 用于存档、网络同步或外部配置驱动 Unit 的初始化。
    /// </summary>
    [System.Serializable]
    public class UnitArchiveData
    {
        /// <summary>属性数据（标签 -> 当前值）</summary>
        public Dictionary<string, float> Attributes = new();

        /// <summary>标记数据（带运行态进度）</summary>
        public List<MarkArchiveData> Marks = new();

        /// <summary>技能标签列表（无状态定义）</summary>
        public List<string> Abilities = new();

        /// <summary>反应标签列表（无状态定义）</summary>
        public List<string> Reactions = new();
    }

    /// <summary>标记存档数据，包含层数、持续时间和计时进度</summary>
    [System.Serializable]
    public class MarkArchiveData
    {
        /// <summary>标记标签名</summary>
        public string Tag;
        /// <summary>当前叠加层数</summary>
        public int Stack;
        /// <summary>总持续时间（秒），-1 表示永久</summary>
        public float Duration;
        /// <summary>已流逝时间（用于精确恢复进度）</summary>
        public float Timer;
    }

    /// <summary>
    /// 负责将 Unit 运行时实例与静态数据进行相互转换的工具。
    /// 支持存档/读档、网络同步和数据驱动初始化。
    /// </summary>
    public static class UnitSerializer
    {
        /// <summary>提取 Unit 的全部状态作为存档数据</summary>
        public static UnitArchiveData Extract(IUnit unit)
        {
            var data = new UnitArchiveData();

            foreach (var kvp in unit.Attributes)
                data.Attributes[kvp.Key] = kvp.Value.CurrentValue;

            foreach (var kvp in unit.Marks)
            {
                var mark = kvp.Value;
                data.Marks.Add(new MarkArchiveData
                {
                    Tag = mark.Name.ToString(),
                    Stack = mark.Stack,
                    Duration = mark.Duration,
                    Timer = mark.Timer
                });
            }

            foreach (var kvp in unit.Abilities) data.Abilities.Add(kvp.Key.ToString());
            foreach (var kvp in unit.Reactions) data.Reactions.Add(kvp.Key.ToString());

            return data;
        }

        /// <summary>从存档/配置数据重建整个 Unit（Data-Driven）</summary>
        public static void Restore(IUnit unit, UnitArchiveData data)
        {
            unit.Clear();

            // 1. 恢复属性
            foreach (var kvp in data.Attributes)
                unit.Attributes.Add(kvp.Key, kvp.Value);

            // 2. 恢复技能和反应
            foreach (var abilityTagStr in data.Abilities)
            {
                var abilityTag = (UnitTag)abilityTagStr;
                var ability = UnitCore.CreateAbility(abilityTag);
                if (ability != null)
                    unit.Abilities.AddAbility(ability);
            }

            foreach (var reactionTagStr in data.Reactions)
            {
                var reactionTag = (UnitTag)reactionTagStr;
                var reaction = UnitCore.CreateReaction(reactionTag);
                if (reaction != null)
                    unit.Reactions.AddReaction(reaction);
            }

            // 3. 恢复标记及计时器状态
            foreach (var markData in data.Marks)
            {
                var markTag = (UnitTag)markData.Tag;
                var mark = UnitCore.CreateMark(markTag).SetStack(markData.Stack).SetDuration(markData.Duration);
                if (mark != null)
                {
                    mark.RestoreTimer(markData.Timer);
                    unit.Marks.AddMark(mark);
                }
            }
        }
    }
}
