using System.Collections.Generic;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 反应链，按优先级排序后分发 Intent。
    /// 职责单一：只管排序和执行，不负责增删管理。
    /// </summary>
    public class ReactionChain
    {
        private readonly List<UnitReaction> _reactions = new();

        /// <summary>添加反应到链中</summary>
        public void Add(UnitReaction reaction) => _reactions.Add(reaction);

        /// <summary>从链中移除反应</summary>
        public void Remove(UnitReaction reaction) => _reactions.Remove(reaction);

        /// <summary>按 Priority 降序排序</summary>
        public void Sort() => _reactions.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        /// <summary>排序并刷新（增删后调用）</summary>
        public void Refresh()
        {
            Sort();
        }

        /// <summary>清空所有反应</summary>
        public void Clear() => _reactions.Clear();

        /// <summary>按优先级分发 Intent，产出 Effect</summary>
        public void Handle(UnitIntent intent, IList<UnitEffect> buffer)
        {
            int count = _reactions.Count;
            for (int i = 0; i < count; i++)
            {
                var reaction = _reactions[i];
                if (!reaction.IsActive) continue;
                if (!reaction.CanHandle(intent)) continue;

                reaction.Handle(intent, buffer);
            }
        }
    }
}
