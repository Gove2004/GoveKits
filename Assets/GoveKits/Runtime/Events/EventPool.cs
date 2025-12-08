using System.Collections.Generic;

namespace GoveKits.Events
{
    /// <summary>
    /// 内部事件对象池。
    /// <para>自动管理事件对象的分配与回收，实现零 GC (Zero Allocation) 事件发布。</para>
    /// </summary>
    public static class EventPool<T> where T : EventInfo, new()
    {
        // 使用 Queue 比 Stack 稍微符合直觉一点，但在池化场景下两者性能差异可忽略
        private static readonly Queue<T> _pool = new Queue<T>();

        /// <summary>
        /// 从池中获取一个事件对象，如果池为空则新建。
        /// </summary>
        public static T Get()
        {
            return _pool.Count > 0 ? _pool.Dequeue() : new T();
        }
        
        /// <summary>
        /// 将事件对象归还到池中，并重置状态。
        /// </summary>
        public static void Return(T item)
        {
            if (item == null) return;
            
            item.Reset();          // 用户定义的重置
            item.IsStopped = false; // 框架级的重置
            _pool.Enqueue(item);
        }
    }  
}