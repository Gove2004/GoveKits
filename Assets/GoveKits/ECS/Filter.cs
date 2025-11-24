using System;
using System.Collections.Generic;

namespace GoveKits.ECS
{
    public class Filter
    {
        // 缓存符合条件的实体 ID
        private readonly HashSet<int> _entities = new HashSet<int>();
        private readonly Type[] _includeTypes;
        private readonly Type[] _excludeTypes;
        private World _world;

        // 提供给 System 遍历的迭代器
        public IEnumerable<Entity> Entities
        {
            get
            {
                foreach (var id in _entities)
                {
                    // 再次校验实体存活（安全网）
                    if (_world.IsAlive(id)) 
                        yield return _world.GetEntity(id);
                }
            }
        }

        public Filter(World world, Type[] include, Type[] exclude)
        {
            _world = world;
            _includeTypes = include ?? Array.Empty<Type>();
            _excludeTypes = exclude ?? Array.Empty<Type>();
        }

        // 当实体的组件发生变化时，重新检查该实体是否符合 Filter
        internal void TryUpdateEntity(int entityId)
        {
            bool match = true;

            // 检查包含
            foreach (var type in _includeTypes)
            {
                if (!_world.HasComponent(entityId, type))
                {
                    match = false;
                    break;
                }
            }

            // 检查排除
            if (match)
            {
                foreach (var type in _excludeTypes)
                {
                    if (_world.HasComponent(entityId, type))
                    {
                        match = false;
                        break;
                    }
                }
            }

            if (match) _entities.Add(entityId);
            else _entities.Remove(entityId);
        }

        internal void RemoveEntity(int entityId)
        {
            _entities.Remove(entityId);
        }
    }
}