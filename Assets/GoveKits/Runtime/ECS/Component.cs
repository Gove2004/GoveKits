using System;
using System.Collections.Generic;

namespace GoveKits.ECS
{
    // 只是一个标记基类，实际数据建议直接用 POCO 类或 struct
    public class Component { }

    // 组件池接口，用于 World 统一管理
    internal interface IComponentPool
    {
        bool Has(int entityId);
        void Remove(int entityId);
        void OnEntityDestroyed(int entityId);
    }

    // 泛型组件池：稀疏集实现
    internal class ComponentPool<T> : IComponentPool
    {
        private T[] _dense = new T[128];      // 紧凑数组：存真实数据
        private int[] _sparse = new int[128]; // 稀疏数组：EntityID -> DenseIndex
        private int[] _denseToEntity = new int[128]; // 反向映射：DenseIndex -> EntityID
        private int _count = 0;

        public ComponentPool()
        {
            Array.Fill(_sparse, -1);
        }

        public void Add(int entityId, T component)
        {
            if (Has(entityId))
            {
                _dense[_sparse[entityId]] = component; // 覆盖
                return;
            }

            // 扩容
            if (entityId >= _sparse.Length)
            {
                int newSize = Math.Max(entityId + 1, _sparse.Length * 2);
                Array.Resize(ref _sparse, newSize);
                for (int i = _sparse.Length / 2; i < newSize; i++) _sparse[i] = -1;
            }
            if (_count >= _dense.Length)
            {
                int newSize = _dense.Length * 2;
                Array.Resize(ref _dense, newSize);
                Array.Resize(ref _denseToEntity, newSize);
            }

            _dense[_count] = component;
            _sparse[entityId] = _count;
            _denseToEntity[_count] = entityId;
            _count++;
        }

        public T Get(int entityId)
        {
            if (!Has(entityId)) throw new Exception($"Entity {entityId} does not have component {typeof(T).Name}");
            return _dense[_sparse[entityId]];
        }

        public bool Has(int entityId)
        {
            return entityId < _sparse.Length && _sparse[entityId] != -1;
        }

        public void Remove(int entityId)
        {
            if (!Has(entityId)) return;

            int currentDenseIndex = _sparse[entityId];
            int lastDenseIndex = _count - 1;

            // Swap Remove：将最后一个元素移动到被删除的位置，保持数组紧凑
            T lastComponent = _dense[lastDenseIndex];
            int lastEntityId = _denseToEntity[lastDenseIndex];

            _dense[currentDenseIndex] = lastComponent;
            _denseToEntity[currentDenseIndex] = lastEntityId;
            _sparse[lastEntityId] = currentDenseIndex;

            // 清理
            _sparse[entityId] = -1;
            _dense[lastDenseIndex] = default;
            _count--;
        }

        public void OnEntityDestroyed(int entityId) => Remove(entityId);
    }
}