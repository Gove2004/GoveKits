using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace GoveKits.Runtime.Architecture
{
    /// <summary>
    /// 组件类型标识，使用预分配的整数 ID 避免运行时 Type.GetHashCode 的性能开销。
    /// 类型 ID 通过原子计数器自动分配，确保全局唯一。
    /// </summary>
    // 组件类型ID，避免运行时Type.GetHashCode
    public readonly struct ComponentType : IEquatable<ComponentType>
    {
        /// <summary>组件类型的唯一整数 ID。</summary>
        public readonly int Id;
        /// <summary>全局自增计数器，用于分配新的组件类型 ID。</summary>
        public static int Counter = 0;

        /// <summary>
        /// 从已知 ID 创建组件类型标识。
        /// </summary>
        /// <param name="id">组件类型 ID。</param>
        public ComponentType(int id) => Id = id;

        /// <summary>判断两个组件类型标识是否相同。</summary>
        public bool Equals(ComponentType other) => Id == other.Id;
        /// <summary>返回组件类型 ID 作为哈希码。</summary>
        public override int GetHashCode() => Id;
        /// <summary>隐式转换为整数 ID。</summary>
        public static implicit operator int(ComponentType t) => t.Id;
    }

    /// <summary>
    /// 泛型组件类型工厂，为每个 T 自动生成唯一的 ComponentType 实例。
    /// 使用 Interlocked 确保多线程安全地分配 ID。
    /// </summary>
    /// <typeparam name="T">组件类型，必须为结构体。</typeparam>
    public static class ComponentType<T> where T : struct
    {
        /// <summary>该泛型类型对应的全局唯一组件类型标识。</summary>
        public static readonly ComponentType Type = new ComponentType(Interlocked.Increment(ref ComponentType.Counter));
    }

    /// <summary>
    /// 组件池接口，定义实体到组件的存取操作。
    /// </summary>
    // 组件池接口
    internal interface IComponentPool
    {
        /// <summary>从池中移除指定实体的组件。</summary>
        /// <param name="entityId">实体索引。</param>
        void Remove(int entityId);

        /// <summary>判断池中是否存在指定实体的组件。</summary>
        /// <param name="entityId">实体索引。</param>
        /// <returns>存在则返回 true。</returns>
        bool Has(int entityId);
    }

    /// <summary>
    /// 高性能组件池，使用密集/稀疏数组（dense-sparse）实现 O(1) 的增删查。
    /// 删除操作采用 swap-back 策略，保持密集数组的连续性。
    /// 支持通过 ReadOnlySpan 进行零拷贝批量遍历。
    /// </summary>
    // 优化版组件池：使用位图+数组，支持O(1)遍历
    internal sealed class ComponentPool<T> : IComponentPool where T : struct
    {
        /// <summary>密集数组，存储实际的组件数据。</summary>
        private T[] _dense = new T[64];
        /// <summary>稀疏数组：实体 ID 映射到密集数组中的索引。</summary>
        private int[] _sparse = new int[64];
        /// <summary>反向映射：密集数组索引映射到实体 ID。</summary>
        private int[] _denseToEntity = new int[64];
        /// <summary>当前池中组件的数量。</summary>
        private int _count = 0;

        /// <summary>
        /// 初始化组件池，将稀疏数组填充为 -1（表示未占用）。
        /// </summary>
        public ComponentPool()
        {
            Array.Fill(_sparse, -1);
        }

        /// <summary>
        /// 向池中添加或更新指定实体的组件。
        /// 如果实体已有该组件则更新值，否则分配新槽位。
        /// </summary>
        /// <param name="entityId">实体索引。</param>
        /// <param name="component">组件值。</param>
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

        /// <summary>
        /// 以引用方式获取指定实体的组件，允许直接修改。
        /// </summary>
        /// <param name="entityId">实体索引。</param>
        /// <returns>组件的引用。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(int entityId)
        {
            if (!Has(entityId)) ThrowNotFound(entityId);
            return ref _dense[_sparse[entityId]];
        }

        /// <summary>
        /// 判断池中是否存在指定实体的组件。
        /// </summary>
        /// <param name="entityId">实体索引。</param>
        /// <returns>存在则返回 true。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityId)
        {
            return (uint)entityId < (uint)_sparse.Length && _sparse[entityId] != -1;
        }

        /// <summary>
        /// 从池中移除指定实体的组件，使用 swap-back 策略保持数组紧凑。
        /// </summary>
        /// <param name="entityId">实体索引。</param>
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

        /// <summary>
        /// 获取所有组件数据的只读 Span，支持零拷贝遍历。
        /// </summary>
        /// <returns>组件数据的只读 Span。</returns>
        // 批量遍历支持：直接暴露dense数组
        public ReadOnlySpan<T> GetAllComponents() => _dense.AsSpan(0, _count);

        /// <summary>
        /// 获取所有实体 ID 的只读 Span，与 GetAllComponents 一一对应。
        /// </summary>
        /// <returns>实体 ID 的只读 Span。</returns>
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
            throw new InvalidOperationException($"实体 {entityId} 没有组件 {typeof(T).Name}");
    }
}