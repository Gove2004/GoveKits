
namespace GoveKits.Unit
{
    /// <summary>
    /// 反应容器：管理单位的 IGameReaction 注册与激活状态
    /// - 添加时若容器处于激活状态会自动 Activate 反应
    /// - Remove/Clear 时会自动 Deactivate
    /// - 可通过 SetActive 控制整组反应的启用/禁用（例如单位死亡时禁用所有反应）
    /// </summary>
    public class ReactionContainer : DictionaryContainer<IGameReaction>
    {
        private readonly IGameUnit _owner;

        /// <summary>
        /// 构造一个新的 <see cref="ReactionContainer"/> 并记录所属单位。
        /// </summary>
        public ReactionContainer(IGameUnit owner) => _owner = owner;
        
        private bool _isActive = true;

        /// <summary>
        /// 添加一个反应项并在容器处于激活状态时立即调用其 <see cref="IGameReaction.Activate"/>。
        /// </summary>
        public override void Add(GameTag key, IGameReaction item)
        {
            base.Add(key, item);
            if (_isActive) item.Activate();
        }

        /// <summary>
        /// 移除指定 Key 的反应项，并在移除前取消激活该反应。
        /// </summary>
        public override bool Remove(GameTag key)
        {
            if (TryGet(key, out var item))
            {
                item.Deactivate();
            }
            return base.Remove(key);
        }

        /// <summary>
        /// 清空容器，先取消激活所有反应然后清理集合。
        /// </summary>
        public override void Clear()
        {
            foreach (var item in Values) item.Deactivate();
            base.Clear();
        }

        /// <summary>
        /// 设置容器激活状态（例如单位死亡时可以 SetActive(false) 来停用全部反应）。
        /// </summary>
        /// <param name="active">是否激活</param>
        public void SetActive(bool active)
        {
            if (_isActive == active) return;
            _isActive = active;
            
            foreach (var item in Values)
            {
                if (active) item.Activate();
                else item.Deactivate();
            }
        }
    }
}