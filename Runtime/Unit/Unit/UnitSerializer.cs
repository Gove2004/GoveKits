using System;
using System.Collections.Generic;
using GoveKits.Runtime.Util;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 可完美序列化（如 JSON）的 Unit 纯数据结构。
    /// 用于存档、网络同步或外部配置驱动 Unit 的初始化。
    /// </summary>
    [System.Serializable]
    public class UnitArchiveData
    {
        /// <summary>属性数据（标签 -> 属性存档：基值 + 修改器列表）</summary>
        public Dictionary<string, AttributeArchiveData> Attributes = new();

        /// <summary>标记数据（带运行态进度）</summary>
        public List<MarkArchiveData> Marks = new();

        /// <summary>技能标签列表（无状态定义）</summary>
        public List<string> Abilities = new();

        /// <summary>反应标签列表（无状态定义）</summary>
        public List<string> Reactions = new();
    }

    /// <summary>
    /// 属性存档数据。只保存基值与修改器数值，读档恢复基值后重挂修改器，加成不会被固化为基值。
    /// ModifierSource 为运行时对象引用无法直接序列化，按 SourceTypeName 还原空壳实例。
    /// </summary>
    [System.Serializable]
    public class AttributeArchiveData
    {
        /// <summary>属性基值（Modifier 加成不入基值）</summary>
        public float BaseValue;

        /// <summary>修改器列表（类型 + 数值）</summary>
        public List<ModifierArchiveData> Modifiers = new();
    }

    /// <summary>单个修改器的存档数据</summary>
    [System.Serializable]
    public class ModifierArchiveData
    {
        /// <summary>修改器类型枚举名（如 "Add"）</summary>
        public string TypeName;

        /// <summary>修改器数值</summary>
        public float Value;

        /// <summary>Source 的运行时类型全名（读档还原空壳实例用，原引用无法序列化）</summary>
        public string SourceTypeName;
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
        /// <summary>周期触发间隔（秒），0 表示非周期标记（TickMark 专用）</summary>
        public float TickInterval;
        /// <summary>周期计时器已流逝时间（用于精确恢复 tick 进度，TickMark 专用）</summary>
        public float TickTimer;
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
            {
                var attr = kvp.Value;
                var attrData = new AttributeArchiveData { BaseValue = attr.BaseValue };
                foreach (var mod in attr.Modifiers)
                {
                    attrData.Modifiers.Add(new ModifierArchiveData
                    {
                        // 以枚举名存档，避免枚举成员重排导致 int 数值错位
                        TypeName = mod.Type.ToString(),
                        Value = mod.Value,
                        SourceTypeName = mod.Source?.GetType().AssemblyQualifiedName
                    });
                }
                data.Attributes[kvp.Key] = attrData;
            }

            foreach (var kvp in unit.Marks)
            {
                var mark = kvp.Value;
                var markData = new MarkArchiveData
                {
                    Tag = mark.Name.ToString(),
                    Stack = mark.Stack,
                    Duration = mark.Duration,
                    Timer = mark.Timer
                };

                // 周期标记补存 tick 间隔与进度，读档后可完整还原触发节奏
                if (mark is TickMark tickMark)
                {
                    markData.TickInterval = tickMark.TickInterval;
                    markData.TickTimer = tickMark.TickTimer;
                }

                data.Marks.Add(markData);
            }

            foreach (var kvp in unit.Abilities) data.Abilities.Add(kvp.Key.ToString());
            foreach (var kvp in unit.Reactions) data.Reactions.Add(kvp.Key.ToString());

            return data;
        }

        /// <summary>从存档/配置数据重建整个 Unit（Data-Driven）</summary>
        public static void Restore(IUnit unit, UnitArchiveData data)
        {
            unit.Clear();

            // 1. 恢复属性：恢复基值后重挂修改器，重新走重算管线得出当前值
            foreach (var kvp in data.Attributes)
            {
                var attrData = kvp.Value;
                unit.Attributes.Add(kvp.Key, attrData.BaseValue);

                // 修改器按类型 + 数值还原；Source 引用无法序列化，按存档类型名还原空壳实例，
                // 使 RemoveModifier 的"按类型回退"能命中读档恢复的修改器（否则变成无法移除的永久 buff）
                foreach (var modData in attrData.Modifiers)
                {
                    // 类型按枚举名解析，无法识别（拼错/枚举成员已删）时跳过并警告，不做静默强转
                    if (!Enum.TryParse(modData.TypeName, true, out ModifierType modType))
                    {
                        LogCore.Warning(nameof(UnitSerializer),
                            $"修改器类型无法识别({modData.TypeName})，该修改器已跳过: {kvp.Key}");
                        continue;
                    }

                    ModifierSource source = null;
                    if (!string.IsNullOrEmpty(modData.SourceTypeName))
                    {
                        var sourceType = Type.GetType(modData.SourceTypeName);
                        if (sourceType != null)
                        {
                            try { source = Activator.CreateInstance(sourceType) as ModifierSource; }
                            catch (Exception e)
                            {
                                LogCore.Warning(nameof(UnitSerializer),
                                    $"修改器 Source 还原失败({modData.SourceTypeName}): {e.Message}");
                            }
                        }
                        if (source == null)
                            LogCore.Warning(nameof(UnitSerializer),
                                $"修改器 Source 无法还原({modData.SourceTypeName})，读档后该修改器将无法按来源移除");
                    }

                    unit.Attributes.AddModifier(kvp.Key,
                        new AttributeModifier(modType, modData.Value, source));
                }
            }

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
                var mark = UnitCore.CreateMark(markTag);

                // 未注册的标记工厂返回 null，跳过（CreateMark 内部已 LogCore.Error），避免 NRE 中断读档
                if (mark == null) continue;

                mark.SetStack(markData.Stack).SetDuration(markData.Duration);

                // 周期标记还原触发间隔（工厂重建的实例若未 SetInterval，以存档值为准）
                if (mark is TickMark tickMark && markData.TickInterval > 0f)
                    tickMark.SetInterval(markData.TickInterval);

                unit.Marks.AddMark(mark);

                // 必须在 AddMark 之后恢复进度：AddMark 触发的 OnApply 会清零 Timer 与 _tickTimer
                mark.RestoreTimer(markData.Timer);
                (mark as TickMark)?.RestoreTickTimer(markData.TickTimer);
            }
        }
    }
}
