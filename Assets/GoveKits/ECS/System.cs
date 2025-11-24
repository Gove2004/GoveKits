using System.Collections.Generic;

namespace GoveKits.ECS
{
    public abstract class System
    {
        protected World World { get; private set; }
        
        public void Bind(World world)
        {
            World = world;
            OnInitialize();
        }

        public virtual void OnInitialize() { }
        public virtual void OnUpdate(float dt) { }
        public virtual void OnDestroy() { }
    }
    
    // 可选：系统组，用于批量管理 System
    public class SystemGroup
    {
        private readonly List<System> _systems = new();
        
        public void Add(System system, World world)
        {
            system.Bind(world);
            _systems.Add(system);
        }

        public void Update(float dt)
        {
            foreach (var sys in _systems)
            {
                sys.OnUpdate(dt);
            }
        }
        
        public void Destroy()
        {
             foreach (var sys in _systems) sys.OnDestroy();
             _systems.Clear();
        }
    }
}