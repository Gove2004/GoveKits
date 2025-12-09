using System.Collections.Generic;


namespace GoveKits.Unit
{
    public class TagMark : GameMark
    {
        // 仅作为标签存在的 Mark，不具有任何额外逻辑
        public TagMark(GameTag tag) : base(tag, Infinite, 1) { }
    }


    /// <summary>
    /// 状态容器
    /// <para>负责管理 Mark 的生命周期、堆叠逻辑和 Tag 查询。</para>
    /// </summary>
    public class MarkContainer : DictionaryContainer<GameMark>
    {
        // 容器必须知道它的主人是谁，以便传给 Mark
        private readonly IGameUnit _owner;
        public MarkContainer(IGameUnit owner) => _owner = owner;


        public void Add(GameTag tag)
        {
            Add(tag, new TagMark(tag));
        }

        /// <summary>
        /// 添加 Mark (核心重写)
        /// <para>处理 覆盖 vs 堆叠 的逻辑</para>
        /// </summary>
        public override void Add(GameTag key, GameMark newMark)
        {
            // 1. 检查是否存在同类 Mark
            if (_items.TryGetValue(key, out var existingMark))
            {
                // [堆叠逻辑]
                // 调用旧 Mark 的 OnStack，传入新 Mark 作为参数
                existingMark.OnStack(newMark);
            }
            else
            {
                // [新增逻辑]
                // 1. 调用基类 Add 存入字典
                base.Add(key, newMark);
                
                // 2. 触发 Mark 的生命周期 OnApply
                // 注意：这里需要传入 Source，通常 newMark 创建时并没有 Source 信息
                // 如果需要 Source，建议 Add 方法多传一个参数，或者在 newMark 创建时赋值
                newMark.OnApply(_owner, newMark.Source); 
            }
        }

        /// <summary>
        /// 移除 Mark (核心重写)
        /// <para>处理 OnRemove 回调</para>
        /// </summary>
        public override bool Remove(GameTag key)
        {
            if (_items.TryGetValue(key, out var mark))
            {
                // 先触发回调，清理属性修饰器等
                mark.OnRemove();
                // 再从字典移除
                return base.Remove(key);
            }
            return false;
        }

        /// <summary>
        /// 每帧更新 (驱动所有 Mark 的 Tick)
        /// </summary>
        public override void Update(float delta)
        {
            // 收集过期列表 (不能在遍历时修改字典)
            List<GameTag> toRemove = null;

            foreach (var kvp in _items)
            {
                var mark = kvp.Value;
                
                // 驱动 Mark 逻辑
                mark.OnTick(delta);

                // 检查过期
                if (mark.IsExpired)
                {
                    if (toRemove == null) toRemove = new List<GameTag>();
                    toRemove.Add(kvp.Key);
                }
            }

            // 统一清理
            if (toRemove != null)
            {
                foreach (var key in toRemove)
                {
                    Remove(key);
                }
            }
        }

        #region 辅助查询

        /// <summary>
        /// 获取某 Tag 的层数 (如果没有则返回 0)
        /// </summary>
        public int GetStack(GameTag tag)
        {
            return TryGet(tag, out var mark) ? mark.CurrentStack : 0;
        }

        /// <summary>
        /// 获取某 Tag 的剩余时间
        /// </summary>
        public float GetRemainingTime(GameTag tag)
        {
            return TryGet(tag, out var mark) ? mark.Duration : 0f;
        }

        /// <summary>
        /// 获取某 Tag 的总时间进度 (0~1)
        /// </summary>
        public float GetTimeProgress(GameTag tag)
        {
            if (TryGet(tag, out var mark) && mark.MaxDuration > 0)
            {
                return 1f - (mark.Duration / mark.MaxDuration);
            }
            return 0f;
        }

        #endregion
    }
}