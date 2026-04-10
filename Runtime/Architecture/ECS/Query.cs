using System;
using System.Runtime.CompilerServices;

namespace GoveKits.Runtime.Architecture
{
    public sealed class Query
    {
        private readonly Archetype _include;
        private readonly Archetype _exclude;
        private readonly World _world;
        
        // 缓存匹配的实体列表（延迟更新）
        private int[] _entities = new int[64];
        private int _count = 0;
        private int _version = 0;  // 缓存版本
        private int _worldVersion = 0; // World结构版本

        internal Query(World world, Archetype include, Archetype exclude)
        {
            _world = world;
            _include = include;
            _exclude = exclude;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Matches(Archetype archetype) => archetype.Has(_include) && !archetype.HasAny(_exclude);

        // 遍历（自动处理缓存失效）
        public Enumerator GetEnumerator() => new Enumerator(this);

        public struct Enumerator
        {
            private readonly Query _query;
            private readonly int[] _entities;
            private readonly int _count;
            private int _index;

            public Enumerator(Query query)
            {
                _query = query;
                _query.EnsureUpdated();
                _entities = query._entities;
                _count = query._count;
                _index = -1;
            }

            public Entity Current => new Entity(_entities[_index], _query._world.GetGen(_entities[_index]));
            public bool MoveNext() => ++_index < _count;
        }

        internal void AddEntity(int entityId)
        {
            if (_count >= _entities.Length) Array.Resize(ref _entities, _entities.Length * 2);
            _entities[_count++] = entityId;
        }

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

        internal void Clear() => _count = 0;

        private void EnsureUpdated()
        {
            if (_version != _worldVersion)
            {
                _world.UpdateQuery(this);
                _worldVersion = _version;
            }
        }

        internal void IncrementVersion() => _version++;
    }

    // QueryBuilder - 流畅API
    public ref struct QueryBuilder
    {
        private World _world;
        private Archetype _include;
        private Archetype _exclude;

        public QueryBuilder(World world)
        {
            _world = world;
            _include = default;
            _exclude = default;
        }

        public QueryBuilder With<T>() where T : struct
        {
            _include |= ComponentType<T>.Type;
            return this;
        }

        public QueryBuilder Without<T>() where T : struct
        {
            _exclude |= ComponentType<T>.Type;
            return this;
        }

        public Query Build() => _world.GetQuery(_include, _exclude);
    }
}