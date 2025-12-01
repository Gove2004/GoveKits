


using System;
using System.Collections.Generic;

namespace GoveKits.Aether
{
        // ==========================================================
    // 2. AetherSource: 虚空之源 (全局共享池)
    // ==========================================================
    /// <summary>
    /// 负责无中生有（实例化）和万物归一（回收）。
    /// 所有位面共享同一个底层的对象池，以减少总体内存开销。
    /// </summary>
    public static class AetherSource
    {
        private static class Pool<T> where T : AetherInfo, new()
        {
            private static readonly Queue<T> _stack = new Queue<T>();

            public static T Spawn() => _stack.Count > 0 ? _stack.Dequeue() : new T();
            
            public static void Recycle(T item)
            {
                item.Reset();
                _stack.Enqueue(item);
            }
        }

        /// <summary>
        /// [核心] 注入：从源汲取以太，注入指定位面，流动结束后回收。
        /// </summary>
        /// <typeparam name="T">以太类型</typeparam>
        /// <param name="plane">目标位面</param>
        /// <param name="manifestation">以太表现形式的回调, 用于初始化以太</param>
        public static void Infuse<T>(AetherPlane plane, Action<T> manifestation) where T : AetherInfo, new()
        {
            var aether = Pool<T>.Spawn();
            try
            {
                manifestation(aether);
                plane.PumpInternal(aether); // 泵入位面管道
            }
            finally
            {
                Pool<T>.Recycle(aether);
            }
        }
    }
}