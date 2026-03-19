using System;
using System.Collections.Generic;

namespace GoveKits.ECS
{
    public class World
    {
        // 实体管理
        private List<int> _entityVersions = new List<int>();
        private Queue<int> _freeIndices = new Queue<int>();
        private int _activeCount = 0;

        // 组件池管理：Type -> Pool
        private Dictionary<Type, IComponentPool> _pools = new Dictionary<Type, IComponentPool>();

        // 过滤器管理
        private List<Filter> _filters = new List<Filter>();

        #region Entity Operations

        public Entity CreateEntity()
        {
            int id;
            if (_freeIndices.Count > 0)
            {
                id = _freeIndices.Dequeue();
            }
            else
            {
                id = _entityVersions.Count;
                _entityVersions.Add(1); // 初始版本号为1
            }

            _activeCount++;
            // 新实体默认不属于任何Filter，不需要立刻Update Filter，因为还没有组件
            return new Entity(id, _entityVersions[id]);
        }

        public void DestroyEntity(Entity entity)
        {
            if (!IsAlive(entity)) return;

            int id = entity.ID;

            // 1. 从所有池中移除组件
            foreach (var pool in _pools.Values)
            {
                pool.OnEntityDestroyed(id);
            }

            // 2. 从所有 Filter 中移除
            foreach (var filter in _filters)
            {
                filter.RemoveEntity(id);
            }

            // 3. 回收 ID
            _entityVersions[id]++; // 版本号+1，使旧Entity句柄失效
            _freeIndices.Enqueue(id);
            _activeCount--;
        }

        public bool IsAlive(Entity entity)
        {
            return IsAlive(entity.ID) && _entityVersions[entity.ID] == entity.Version;
        }
        
        // 内部辅助
        internal bool IsAlive(int id) => id >= 0 && id < _entityVersions.Count;
        
        // 仅用于 Filter 内部重建 Entity 结构
        internal Entity GetEntity(int id) => new Entity(id, _entityVersions[id]);

        #endregion

        #region Component Operations

        public void AddComponent<T>(Entity entity, T component = default)
        {
            if (!IsAlive(entity)) throw new Exception("Cannot add component to dead entity.");
            
            GetPool<T>().Add(entity.ID, component);
            
            // 组件变动，通知过滤器更新
            UpdateFilters(entity.ID);
        }

        public T GetComponent<T>(Entity entity)
        {
            if (!IsAlive(entity)) throw new Exception("Entity is dead.");
            return GetPool<T>().Get(entity.ID);
        }

        public void RemoveComponent<T>(Entity entity)
        {
            if (!IsAlive(entity)) return;
            
            GetPool<T>().Remove(entity.ID);
            
            // 组件变动，通知过滤器更新
            UpdateFilters(entity.ID);
        }
        
        public bool HasComponent<T>(Entity entity) => HasComponent(entity.ID, typeof(T));

        // 内部非泛型查询
        internal bool HasComponent(int entityId, Type type)
        {
            if (_pools.TryGetValue(type, out var pool))
            {
                return pool.Has(entityId);
            }
            return false;
        }

        private ComponentPool<T> GetPool<T>()
        {
            Type type = typeof(T);
            if (!_pools.TryGetValue(type, out var pool))
            {
                pool = new ComponentPool<T>();
                _pools.Add(type, pool);
            }
            return (ComponentPool<T>)pool;
        }

        #endregion

        #region Filter Operations

        // 获取或创建一个 Filter
        public Filter GetFilter(Type[] include, Type[] exclude = null)
        {
            // 这里简单起见直接创建新Filter，实际项目中应该根据Type签名缓存Filter实例
            // 避免重复创建相同的 Filter
            var filter = new Filter(this, include, exclude);
            
            // 初始化 Filter 数据 (全量扫描一次现存实体，稍微耗时，但在初始化System时只做一次)
            for (int i = 0; i < _entityVersions.Count; i++)
            {
                if (!_freeIndices.Contains(i)) // 简单的活跃检查
                {
                    filter.TryUpdateEntity(i);
                }
            }
            
            _filters.Add(filter);
            return filter;
        }

        private void UpdateFilters(int entityId)
        {
            foreach (var filter in _filters)
            {
                filter.TryUpdateEntity(entityId);
            }
        }

        #endregion
    }
}