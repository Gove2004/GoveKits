using System.Collections.Generic;
using GoveKits.Events; // 引用你的事件系统命名空间

namespace GoveKits.Unit
{
    /// <summary>
    /// 游戏效果事件基类。
    /// <para>作为事件总线的消息载体，承载一次交互的上下文（来源、目标、标签等）。</para>
    /// <para>继承自 <see cref="EventInfo"/>，支持对象池复用：使用完请通过事件系统回收或手动 Reset。</para>
    /// </summary>
    public abstract class GameEffect : EventInfo
    {
        // --- 核心元数据 ---
        /// <summary>
        /// 效果的来源（施加者/发起单位），可用于 Reaction 的自动过滤。
        /// </summary>
        public IGameUnit Source; // 施法者

        /// <summary>
        /// 效果的目标（承受者）。Reaction 通常会基于 Target 来决定是否响应。
        /// </summary>
        public IGameUnit Target; // 承受者

        // --- 上下文标签 (Context Tags) ---
        // 用于标记这次交互的特性 (e.g., "Critical", "Fire", "Physical")
        private HashSet<GameTag> _tags;

        #region Tag 操作 (用于 Reaction 筛选)

        /// <summary>
        /// 为本次效果添加一个上下文标签（用于 Reaction/Filter）。
        /// </summary>
        /// <param name="tag">要添加的标签。</param>
        public void AddTag(GameTag tag)
        {
            if (_tags == null) _tags = new HashSet<GameTag>();
            _tags.Add(tag);
        }

        /// <summary>
        /// 检查效果是否包含指定标签。
        /// </summary>
        public bool HasTag(GameTag tag) => _tags != null && _tags.Contains(tag);

        #endregion

        #region EventInfo 重写 (对象池复用)

        /// <summary>
        /// 重置对象以供对象池复用：清空来源、目标和标签集合。
        /// </summary>
        public override void OnRecycle()
        {
            Source = null;
            Target = null;
            _tags?.Clear();
        }

        #endregion
    }
}