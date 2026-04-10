using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace GoveKits.Runtime.Architecture
{
    // 组件类型ID，避免运行时Type.GetHashCode
    public readonly struct ComponentType : IEquatable<ComponentType>
    {
        public readonly int Id;
        public static int Counter = 0;
        
        public ComponentType(int id) => Id = id;
        public bool Equals(ComponentType other) => Id == other.Id;
        public override int GetHashCode() => Id;
        public static implicit operator int(ComponentType t) => t.Id;
    }

    public static class ComponentType<T> where T : struct
    {
        public static readonly ComponentType Type = new ComponentType(Interlocked.Increment(ref ComponentType.Counter));
    }

    // 组件池接口
    internal interface IComponentPool
    {
        void Remove(int entityId);
        bool Has(int entityId);
    }

    // 优化版组件池：使用位图+数组，支持O(1)遍历
    internal sealed class ComponentPool<T> : IComponentPool where T : struct
    {
        // 存储：紧凑数组
        private T[] _dense = new T[64];
        // 稀疏数组：EntityId -> DenseIndex
        private int[] _sparse = new int[64];
        // 反向映射
        private int[] _denseToEntity = new int[64];
        private int _count = 0;

        public ComponentPool()
        {
            Array.Fill(_sparse, -1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int entityId, in T component)
        {
            if (Has(entityId))
            {
                _dense[_sparse[entityId]] = component;
                return;
            }

            EnsureCapacity(entityId);
            
            int index = _count++;
            _dense[index] = component;
            _sparse[entityId] = index;
            _denseToEntity[index] = entityId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(int entityId)
        {
            if (!Has(entityId)) ThrowNotFound(entityId);
            return ref _dense[_sparse[entityId]];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityId)
        {
            return (uint)entityId < (uint)_sparse.Length && _sparse[entityId] != -1;
        }

        public void Remove(int entityId)
        {
            if (!Has(entityId)) return;

            int denseIndex = _sparse[entityId];
            int lastIndex = --_count;
            
            // Swap-back
            if (denseIndex != lastIndex)
            {
                _dense[denseIndex] = _dense[lastIndex];
                int lastEntity = _denseToEntity[lastIndex];
                _denseToEntity[denseIndex] = lastEntity;
                _sparse[lastEntity] = denseIndex;
            }

            _sparse[entityId] = -1;
            _dense[lastIndex] = default;
        }

        // 批量遍历支持：直接暴露dense数组
        public ReadOnlySpan<T> GetAllComponents() => _dense.AsSpan(0, _count);
        public ReadOnlySpan<int> GetAllEntities() => _denseToEntity.AsSpan(0, _count);

        private void EnsureCapacity(int entityId)
        {
            if (entityId >= _sparse.Length)
            {
                int newSize = Math.Max(entityId + 1, _sparse.Length * 2);
                Array.Resize(ref _sparse, newSize);
                _sparse.AsSpan(_sparse.Length / 2).Fill(-1);
            }
            if (_count >= _dense.Length)
            {
                int newSize = _dense.Length * 2;
                Array.Resize(ref _dense, newSize);
                Array.Resize(ref _denseToEntity, newSize);
            }
        }

        private void ThrowNotFound(int entityId) => 
            throw new InvalidOperationException($"Entity {entityId} has no {typeof(T).Name}");
    }
}