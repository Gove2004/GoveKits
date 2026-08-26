

namespace GoveKits.Runtime.Architecture
{
    /// <summary>
    /// ECS 系统基类，封装与世界绑定的生命周期管理。
    /// 子类继承后实现 OnInit、OnUpdate 和 OnDestroy 来定义系统行为。
    /// </summary>
    public abstract class System
    {
        /// <summary>系统所属的 World 实例。</summary>
        protected World World { get; private set; }

        /// <summary>
        /// 将系统与指定的 World 绑定。
        /// </summary>
        /// <param name="world">要绑定的 World。</param>
        public void Bind(World world) => World = world;

        /// <summary>系统初始化回调，在绑定 World 后调用。</summary>
        public virtual void OnInit() { }

        /// <summary>
        /// 系统每帧更新回调。
        /// </summary>
        /// <param name="dt">时间增量。</param>
        public virtual void OnUpdate(float dt) { }

        /// <summary>系统销毁回调，用于清理资源。</summary>
        public virtual void OnDestroy() { }
    }

    /// <summary>
    /// 带自动 Query 构建的系统基类。
    /// 子类只需实现 BuildQuery 定义查询条件，系统会在 OnInit 时自动创建 Query。
    /// </summary>
    // 辅助：自动创建Query的System基类
    public abstract class QuerySystem : System
    {
        private Query _query;

        /// <summary>
        /// 系统初始化：自动构建 Query 并调用 OnSystemInit。
        /// </summary>
        public sealed override void OnInit()
        {
            _query = BuildQuery(World.Query);
            OnSystemInit();
        }

        /// <summary>
        /// 子类实现此方法来定义查询条件。
        /// </summary>
        /// <param name="builder">Query 构建器。</param>
        /// <returns>编译好的 Query。</returns>
        protected abstract Query BuildQuery(QueryBuilder builder);

        /// <summary>
        /// 在 Query 构建完成后调用的额外初始化回调。
        /// </summary>
        protected virtual void OnSystemInit() { }

        /// <summary>系统初始化后构建的 Query 实例。</summary>
        protected Query Query => _query;
    }
}