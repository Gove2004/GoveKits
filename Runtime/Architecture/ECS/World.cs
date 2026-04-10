
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GoveKits.Runtime.Architecture
{
    public sealed class World
    {
        // 实体存储
        private EntityMeta[] _entities = new EntityMeta[64];
        private int _entityCount = 0;
        private int _freeList = -1;  // 空闲链表头

        // 组件存储：TypeId -> Pool
        private IComponentPool[] _pools = new IComponentPool[32];
        private int _poolCount = 0;

        // 原型追踪：EntityId -> Archetype
        private Archetype[] _archetypes = new Archetype[64];

        // Query缓存
        private Dictionary<ulong, Query> _queries = new Dictionary<ulong, Query>();
        private List<Query> _queryList = new List<Query>();
        private int _structVersion = 0;  // 结构变化版本

        #region Entity

        public Entity CreateEntity()
        {
            int id;
            ushort gen;

            if (_freeList != -1)
            {
                id = _freeList;
                ref var meta = ref _entities[id];
                _freeList = meta.NextFree;
                gen = ++meta.Gen;  // 代数增加
                meta.Flags = EntityMeta.FLAG_ALIVE;
            }
            else
            {
                if (_entityCount >= _entities.Length)
                {
                    int newSize = _entities.Length * 2;
                    Array.Resize(ref _entities, newSize);
                    Array.Resize(ref _archetypes, newSize);
                }
                id = _entityCount++;
                ref var meta = ref _entities[id];
                meta.Gen = 1;
                meta.Flags = EntityMeta.FLAG_ALIVE;
                gen = 1;
            }

            _archetypes[id] = default;
            return new Entity(id, gen);
        }

        public void DestroyEntity(Entity entity)
        {
            if (!IsAlive(entity)) return;

            int id = entity.Id;
            ref var meta = ref _entities[id];

            // 移除所有组件
            var arch = _archetypes[id];
            for (int i = 0; i < 128; i++)
            {
                if (HasBit(arch, i))
                {
                    _pools[i]?.Remove(id);
                }
            }

            // 从所有Query中移除
            foreach (var query in _queryList)
            {
                query.RemoveEntity(id);
            }

            // 回收实体
            meta.Flags = 0;
            meta.NextFree = _freeList;
            _freeList = id;
            _archetypes[id] = default;
            _structVersion++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAlive(Entity entity)
        {
            if ((uint)entity.Id >= (uint)_entityCount) return false;
            return _entities[entity.Id].Gen == entity.Gen && _entities[entity.Id].IsAlive;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ushort GetGen(int id) => _entities[id].Gen;

        #endregion

        #region Component

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add<T>(Entity entity, in T component = default) where T : struct
        {
            if (!IsAlive(entity)) ThrowDeadEntity();
            
            int id = entity.Id;
            var type = ComponentType<T>.Type;
            var pool = GetPool<T>(type);
            
            pool.Add(id, component);
            
            // 更新原型
            var oldArch = _archetypes[id];
            var newArch = oldArch | type;
            _archetypes[id] = newArch;
            
            // 增量更新Query（而非全量扫描）
            UpdateQueriesForEntity(id, oldArch, newArch);
            _structVersion++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get<T>(Entity entity) where T : struct
        {
            if (!IsAlive(entity)) ThrowDeadEntity();
            return ref GetPool<T>(ComponentType<T>.Type).Get(entity.Id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove<T>(Entity entity) where T : struct
        {
            if (!IsAlive(entity)) return;
            
            int id = entity.Id;
            var type = ComponentType<T>.Type;
            
            GetPool<T>(type).Remove(id);
            
            var oldArch = _archetypes[id];
            var newArch = oldArch & ~new Archetype(type.Id < 64 ? 1UL << type.Id : 0, type.Id >= 64 ? 1UL << (type.Id - 64) : 0);
            _archetypes[id] = newArch;
            
            UpdateQueriesForEntity(id, oldArch, newArch);
            _structVersion++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has<T>(Entity entity) where T : struct
        {
            if (!IsAlive(entity)) return false;
            var type = ComponentType<T>.Type;
            return HasBit(_archetypes[entity.Id], type.Id);
        }

        private ComponentPool<T> GetPool<T>(ComponentType type) where T : struct
        {
            int id = type.Id;
            if (id >= _pools.Length) Array.Resize(ref _pools, Math.Max(id + 1, _pools.Length * 2));
            
            if (_pools[id] == null)
            {
                _pools[id] = new ComponentPool<T>();
                if (id >= _poolCount) _poolCount = id + 1;
            }
            return (ComponentPool<T>)_pools[id];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasBit(Archetype arch, int bit) => bit < 64 
            ? (arch.Bits0 & (1UL << bit)) != 0 
            : (arch.Bits1 & (1UL << (bit - 64))) != 0;

        #endregion

        #region Query

        internal Query GetQuery(Archetype include, Archetype exclude)
        {
            // 使用哈希缓存Query
            ulong key = include.Bits0 ^ (include.Bits1 << 1) ^ (exclude.Bits0 << 2) ^ (exclude.Bits1 << 3);
            
            if (!_queries.TryGetValue(key, out var query))
            {
                query = new Query(this, include, exclude);
                _queries[key] = query;
                _queryList.Add(query);
                
                // 初始化：扫描现有实体
                for (int i = 0; i < _entityCount; i++)
                {
                    if (_entities[i].IsAlive && query.Matches(_archetypes[i]))
                    {
                        query.AddEntity(i);
                    }
                }
            }
            return query;
        }

        public QueryBuilder Query => new QueryBuilder(this);

        // 增量更新：只检查变化的实体
        private void UpdateQueriesForEntity(int entityId, Archetype oldArch, Archetype newArch)
        {
            foreach (var query in _queryList)
            {
                bool wasMatch = query.Matches(oldArch);
                bool isMatch = query.Matches(newArch);
                
                if (!wasMatch && isMatch) query.AddEntity(entityId);
                else if (wasMatch && !isMatch) query.RemoveEntity(entityId);
            }
        }

        internal void UpdateQuery(Query query)
        {
            query.Clear();
            for (int i = 0; i < _entityCount; i++)
            {
                if (_entities[i].IsAlive && query.Matches(_archetypes[i]))
                {
                    query.AddEntity(i);
                }
            }
        }

        #endregion

        private static void ThrowDeadEntity() => throw new InvalidOperationException("Entity is not alive");
    }
}