using UnityEngine;
using UnityEngine.Pool;

namespace GoveKits.Pools
{
    /// <summary>
    /// 【内部组件】附加到池化 GameObject 上的辅助组件。
    /// 它的唯一作用是“记住”这个 GameObject 实例属于哪个具体的池。
    /// 当我们调用 Pool.Recycle(gameObject) 时，正是通过这个组件来找到正确的池进行回收。
    /// </summary>
    [DisallowMultipleComponent]
    internal class PoolRecord : MonoBehaviour
    {
        /// <summary>
        /// 指向创建此对象的那个对象池的引用。
        /// </summary>
        public IObjectPool<GameObject> Pool { get; set; }
    }
}