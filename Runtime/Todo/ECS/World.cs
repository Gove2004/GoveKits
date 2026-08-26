
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GoveKits.Runtime.Architecture
{
    /// <summary>
    /// ECS World —— 实体-组件-系统架构的核心容器。
    /// 管理实体的创建/销毁、组件的增删查、以及 Query 的缓存与增量更新。
    /// 使用紧凑的密集/稀疏数组实现 O(1) 的组件访问。
    /// </summary>
    public sealed class World
    {
        /// <summary>实体元数据数组。</summary>
        private EntityMeta[] _entities = new EntityMeta[64];
        /// <summary>当前实体总数。</summary>
        private int _entityCount = 0;
        /// <summary>空闲链表头索引。</summary>
        private int _freeList = -1;  // 空闲链表头

        /// <summary>组件池数组，按组件类型 ID 索引。</summary>
        private IComponentPool[] _pools = new IComponentPool[32];
        /// <summary>已使用的组件池数量。</summary>
        private int _poolCount = 0;

        /// <summary>每个实体对应的原型标识数组。</summary>
        private Archetype[] _archetypes = new Archetype[64];

        /// <summary>Query 缓存字典，用于避免重复创建相同条件的查询。</summary>
        private Dictionary<ulong, Query> _queries = new Dictionary<ulong, Query>();
        /// <summary>活跃 Query 列表，用于增量更新。</summary>
        private List<Query> _queryList = new List<Query>();
        /// <summary>世界结构变化版本号，用于 Query 缓存失效检测。</summary>
        private int _structVersion = 0;  // 结构变化版本

        #region Entity

        /// <summary>
        /// 创建一个新的实体。
        /// 优先复用已销毁实体的槽位，否则在末尾分配新槽位。
        /// </summary>
        /// <returns>新创建的实体标识。</returns>
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

        /// <summary>
        /// 销毁指定实体，回收其所有组件和槽位。
        /// </summary>
        /// <param name="entity">要销毁的实体标识。</param>
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

        /// <summary>
        /// 判断实体是否存活（未销毁且代数匹配）。
        /// </summary>
        /// <param name="entity">要检查的实体标识。</param>
        /// <returns>实体存活则返回 true。</returns>
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

        /// <summary>
        /// 为实体添加指定类型的组件。
        /// 如果实体已有该组件则更新其值，否则新增并更新原型和 Query 缓存。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="entity">目标实体。</param>
        /// <param name="component">组件值（可为默认值）。</param>
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

        /// <summary>
        /// 以引用方式获取实体上指定类型的组件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="entity">目标实体。</param>
        /// <returns>组件的引用，允许直接修改。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get<T>(Entity entity) where T : struct
        {
            if (!IsAlive(entity)) ThrowDeadEntity();
            return ref GetPool<T>(ComponentType<T>.Type).Get(entity.Id);
        }

        /// <summary>
        /// 从实体上移除指定类型的组件。
        /// </summary>
        /// <typeparam name="T">要移除的组件类型。</typeparam>
        /// <param name="entity">目标实体。</param>
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

        /// <summary>
        /// 判断实体是否拥有指定类型的组件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="entity">目标实体。</param>
        /// <returns>拥有该组件则返回 true。</returns>
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

        /// <summary>
        /// 获取或创建一个 Query，用于按原型条件筛选实体。
        /// 相同的包含/排除条件会返回缓存的 Query 实例。
        /// </summary>
        /// <param name="include">必须包含的原型。</param>
        /// <param name="exclude">必须排除的原型。</param>
        /// <returns>匹配的 Query 实例。</returns>
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

        /// <summary>
        /// 创建一个流畅 API 风格的 Query 构建器。
        /// 使用示例: world.Query.With<PlayerComponent>().Without<DeadComponent>().Build()
        /// </summary>
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

        /// <summary>
        /// 全量更新指定 Query 的实体缓存。
        /// 通常在 World 结构发生重大变化时使用。
        /// </summary>
        /// <param name="query">要更新的 Query。</param>
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

        private static void ThrowDeadEntity() => throw new InvalidOperationException("实体已死亡或不存在");
    }
}