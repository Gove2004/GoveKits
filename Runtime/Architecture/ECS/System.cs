

namespace GoveKits.Runtime.Architecture
{
    public abstract class System
    {
        protected World World { get; private set; }
        
        public void Bind(World world) => World = world;
        public virtual void OnInit() { }
        public virtual void OnUpdate(float dt) { }
        public virtual void OnDestroy() { }
    }

    // 辅助：自动创建Query的System基类
    public abstract class QuerySystem : System
    {
        private Query _query;
        
        public sealed override void OnInit()
        {
            _query = BuildQuery(World.Query);
            OnSystemInit();
        }
        
        protected abstract Query BuildQuery(QueryBuilder builder);
        protected virtual void OnSystemInit() { }
        
        protected Query Query => _query;
    }
}