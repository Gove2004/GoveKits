using System.Collections.Generic;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 可完美序列化（如 JSON / MessagePack）的 Unit 纯数据结构
    /// </summary>
    [System.Serializable]
    public class UnitArchiveData
    {
        // 1. 属性数据 (标签 -> 基础值)
        public Dictionary<string, float> Attributes = new();
        
        // 2. 标记数据 (带运行态进度)
        public List<MarkArchiveData> Marks = new();
        
        // 3. 技能和反应 (无状态定义)
        public List<string> Abilities = new();
        public List<string> Reactions = new();
    }

    [System.Serializable]
    public class MarkArchiveData
    {
        public string Tag;
        public int Stack;
        public float Duration;
        public float Timer; // 记录冷却/剩余时间进度
    }

    /// <summary>
    /// 负责将 Unit 运行时实例与静态数据进行相互转换的工具
    /// </summary>
    public static class UnitSerializer
    {
        /// <summary>
        /// 提取 Unit 的全部状态作为存档数据
        /// </summary>
        public static UnitArchiveData Extract(IUnit unit)
        {
            var data = new UnitArchiveData();

            // 提取属性基础值
            foreach (var kvp in unit.Attributes)
                data.Attributes[kvp.Key] = kvp.Value.BaseValue;

            // 提取正在生效的标记
            foreach (var kvp in unit.Marks)
            {
                var mark = kvp.Value;
                data.Marks.Add(new MarkArchiveData
                {
                    Tag = mark.Name,
                    Stack = mark.Stack,
                    Duration = mark.Duration,
                    Timer = mark.Timer
                });
            }

            // 提取拥有的能力标签
            foreach (var kvp in unit.Abilities) data.Abilities.Add(kvp.Key);
            foreach (var kvp in unit.Reactions) data.Reactions.Add(kvp.Key);

            return data;
        }

        /// <summary>
        /// 从存档/配置数据重建整个 Unit (Data-Driven)
        /// </summary>
        public static void Restore(IUnit unit, UnitArchiveData data)
        {
            unit.Clear(); // 清理旧数据

            // 1. 恢复属性
            foreach (var kvp in data.Attributes)
                unit.Attributes.Add(kvp.Key, kvp.Value);

            // 2. 恢复技能和反应 (基于注册中心工厂)
            foreach (var abilityTag in data.Abilities)
                unit.Abilities.AddAbility(UnitCore.CreateAbility(abilityTag));

            foreach (var reactionTag in data.Reactions)
                unit.Reactions.AddReaction(UnitCore.CreateReaction(reactionTag));

            // 3. 恢复标记及计时器状态
            foreach (var markData in data.Marks)
            {
                var mark = UnitCore.CreateMark(markData.Tag, markData.Stack, markData.Duration);
                if (mark != null)
                {
                    mark.RestoreTimer(markData.Timer); // 恢复读秒
                    unit.Marks.AddMark(mark);
                }
            }
        }
    }
}