using System;
using System.Runtime.CompilerServices;

namespace GoveKits.Runtime.Architecture
{
    /// <summary>
    /// ECS 查询，按原型条件（包含/排除）筛选实体集合。
    /// 使用版本化缓存机制实现增量更新，仅在 World 结构变化时刷新。
    /// </summary>
    public sealed class Query
    {
        /// <summary>必须匹配的原型条件。</summary>
        private readonly Archetype _include;
        /// <summary>必须排除的原型条件。</summary>
        private readonly Archetype _exclude;
        /// <summary>所属的 World 实例。</summary>
        private readonly World _world;

        /// <summary>缓存的匹配实体索引数组。</summary>
        private int[] _entities = new int[64];
        /// <summary>当前缓存中的实体数量。</summary>
        private int _count = 0;
        /// <summary>缓存版本号，用于检测是否需要刷新。</summary>
        private int _version = 0;  // 缓存版本
        /// <summary>World 的结构版本号。</summary>
        private int _worldVersion = 0; // World结构版本

        /// <summary>
        /// 创建查询实例。
        /// </summary>
        /// <param name="world">所属 World。</param>
        /// <param name="include">包含条件。</param>
        /// <param name="exclude">排除条件。</param>
        internal Query(World world, Archetype include, Archetype exclude)
        {
            _world = world;
            _include = include;
            _exclude = exclude;
        }

        /// <summary>
        /// 判断给定原型是否匹配此查询的条件。
        /// </summary>
        /// <param name="archetype">要检查的原型。</param>
        /// <returns>匹配则返回 true。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Matches(Archetype archetype) => archetype.Has(_include) && !archetype.HasAny(_exclude);

        /// <summary>
        /// 获取查询结果的枚举器，自动处理缓存失效和刷新。
        /// </summary>
        /// <returns>实体枚举器。</returns>
        // 遍历（自动处理缓存失效）
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// 查询结果的枚举器结构体，支持 foreach 遍历匹配的实体。
        /// 枚举器创建时会自动确保缓存是最新的。
        /// </summary>
        public struct Enumerator
        {
            private readonly Query _query;
            private readonly int[] _entities;
            private readonly int _count;
            private int _index;

            /// <summary>
            /// 创建枚举器，会触发缓存刷新检查。
            /// </summary>
            /// <param name="query">所属查询。</param>
            public Enumerator(Query query)
            {
                _query = query;
                _query.EnsureUpdated();
                _entities = query._entities;
                _count = query._count;
                _index = -1;
            }

            /// <summary>当前迭代位置的实体。</summary>
            public Entity Current => new Entity(_entities[_index], _query._world.GetGen(_entities[_index]));
            /// <summary>移动到下一个实体，返回是否还有更多元素。</summary>
            /// <returns>还有下一个元素则返回 true。</returns>
            public bool MoveNext() => ++_index < _count;
        }

        /// <summary>
        /// 向查询缓存中添加一个实体索引。
        /// </summary>
        /// <param name="entityId">实体索引。</param>
        internal void AddEntity(int entityId)
        {
            if (_count >= _entities.Length) Array.Resize(ref _entities, _entities.Length * 2);
            _entities[_count++] = entityId;
        }

        /// <summary>
        /// 从查询缓存中移除一个实体索引。
        /// </summary>
        /// <param name="entityId">要移除的实体索引。</param>
        internal void RemoveEntity(int entityId)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_entities[i] == entityId)
                {
                    _entities[i] = _entities[--_count];
                    return;
                }
            }
        }

        /// <summary>清空查询缓存中的所有实体。</summary>
        internal void Clear() => _count = 0;

        /// <summary>
        /// 检查缓存版本与 World 版本是否一致，不一致时触发全量刷新。
        /// </summary>
        private void EnsureUpdated()
        {
            if (_version != _worldVersion)
            {
                _world.UpdateQuery(this);
                _worldVersion = _version;
            }
        }

        /// <summary>递增查询缓存的版本号。</summary>
        internal void IncrementVersion() => _version++;
    }

    /// <summary>
    /// Query 构建器，提供流畅 API 风格的查询条件构造。
    /// 使用示例: world.Query.With&lt;Position&gt;().Without&lt;Dead&gt;().Build()
    /// </summary>
    // QueryBuilder - 流畅API
    public ref struct QueryBuilder
    {
        private World _world;
        private Archetype _include;
        private Archetype _exclude;

        /// <summary>
        /// 创建 Query 构建器。
        /// </summary>
        /// <param name="world">所属 World。</param>
        public QueryBuilder(World world)
        {
            _world = world;
            _include = default;
            _exclude = default;
        }

        /// <summary>
        /// 添加包含条件：查询结果必须具有指定类型的组件。
        /// </summary>
        /// <typeparam name="T">要包含的组件类型。</typeparam>
        /// <returns>构建器实例，支持链式调用。</returns>
        public QueryBuilder With<T>() where T : struct
        {
            _include |= ComponentType<T>.Type;
            return this;
        }

        /// <summary>
        /// 添加排除条件：查询结果不能具有指定类型的组件。
        /// </summary>
        /// <typeparam name="T">要排除的组件类型。</typeparam>
        /// <returns>构建器实例，支持链式调用。</returns>
        public QueryBuilder Without<T>() where T : struct
        {
            _exclude |= ComponentType<T>.Type;
            return this;
        }

        /// <summary>
        /// 构建并返回最终的 Query 实例。
        /// </summary>
        /// <returns>编译好的 Query。</returns>
        public Query Build() => _world.GetQuery(_include, _exclude);
    }
}