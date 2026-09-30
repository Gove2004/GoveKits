using System.Collections.Generic;
using UnityEngine;

namespace GoveKits.Runtime.Util
{
    /// <summary>
    /// 池记录组件，挂载在池化的 GameObject 上，标记其所属的对象池实例。
    /// 用于 Return 时查找正确的池以归还对象。
    /// </summary>
    public class PoolRecord : MonoBehaviour
    {
        /// <summary>所属的对象池实例，用于归还时定位正确的池。</summary>
        public GameObjectPool SourcePool { get; set; }
        /// <summary>当前是否处于池缓存中（用于检测重复归还）。</summary>
        public bool InPool { get; set; }
    }

    /// <summary>
    /// 池化 GameObject 的统一容器（内部使用），确保缓存实例在场景切换时不被销毁。
    /// </summary>
    internal static class GameObjectPoolRoot
    {
        private const string RootName = "GoveKitsPoolRoot";
        private static GameObject _root;

        // 同帧内 Destroy 后的容器仍可被 GameObject.Find 捞到（垂死状态），
        // 记录其 InstanceID，避免把新缓存挂到即将销毁的容器下形成幽灵引用
        private static readonly HashSet<int> s_dyingRootIds = new();

        public static Transform GetOrCreate()
        {
            if (_root != null)
                return _root.transform;

            var found = GameObject.Find(RootName);
            if (found != null && !s_dyingRootIds.Contains(found.GetInstanceID()))
            {
                _root = found;
            }
            else
            {
                _root = new GameObject(RootName);
                Object.DontDestroyOnLoad(_root);
            }

            return _root.transform;
        }

        public static void Destroy()
        {
            if (_root != null)
            {
                s_dyingRootIds.Add(_root.GetInstanceID());
                GameObject.Destroy(_root);
            }
            _root = null;
        }

        // 关闭 Domain Reload 时清理静态引用，避免跨 Play 会话残留
        internal static void ResetForDomainReload()
        {
            _root = null;
            s_dyingRootIds.Clear();
        }
    }

    /// <summary>
    /// Unity GameObject 对象池，支持预热、容量上限控制，回收时自动递归调用子物体上所有 IPoolable.OnRecycle。
    /// 缓存实例统一挂到 DontDestroyOnLoad 容器下，场景切换不会丢失。
    /// 通过 PoolCore 创建与使用（按预制体单例），通常无需直接实例化。
    /// </summary>
    public class GameObjectPool : IPool, IPool<GameObject>
    {
        // 回收时收集 IPoolable 的多级缓冲：OnRecycle 内若递归归还其他池对象会重入
        // RecycleGameObject 并清空共享缓冲，导致外层遍历错乱，故按嵌套深度隔离
        private static readonly Stack<List<IPoolable>> s_bufferStack = new();

        private readonly GameObject prefab;
        private readonly Stack<GameObject> stack = new();

        /// <summary>池中当前缓存的对象数量。</summary>
        public int Count => stack.Count;
        /// <summary>对象池的最大容量，超出容量的对象在归还时将被销毁。</summary>
        public int Capacity { get; private set; }

        /// <summary>池是否已被注销（PoolCore.Clear/Close 后为 true，此后归还的对象直接销毁而非入池）。</summary>
        public bool IsDisposed { get; private set; }

        /// <summary>创建指定预制体与容量的 GameObject 对象池（通常由 PoolCore 调用）。</summary>
        public GameObjectPool(GameObject prefab, int maxSize)
        {
            this.prefab = prefab;
            Capacity = maxSize;
        }

        /// <summary>
        /// 预热池，预先实例化并缓存指定数量的对象。
        /// </summary>
        /// <param name="count">要预热的对象数量</param>
        public void Warmup(int count)
        {
            for (int i = 0; i < count && stack.Count < Capacity; i++)
                CacheInstance(CreateInstance());
        }

        /// <summary>
        /// 清空池中所有缓存对象并立即销毁它们。
        /// 仅清空缓存，池仍可继续使用；连同注销请使用 Dispose（由 PoolCore 调用）。
        /// </summary>
        public void Clear()
        {
            while (stack.Count > 0)
            {
                var obj = stack.Pop();
                if (obj != null)
                    GameObject.Destroy(obj);
            }
        }

        /// <summary>
        /// 清空缓存并将池标记为已注销（由 PoolCore.Clear/Close 调用）。
        /// 注销后归还的实例不再入池，直接走回收回调并销毁，避免写入无人引用的缓存形成幽灵缓存。
        /// </summary>
        internal void Dispose()
        {
            IsDisposed = true;
            Clear();
        }

        /// <summary>
        /// 从池中获取一个可用的 GameObject 实例。
        /// 池中有缓存对象时取出并激活；池空时根据 prefab 创建新实例。
        /// </summary>
        /// <returns>可用的 GameObject 实例</returns>
        public GameObject Get()
        {
            if (IsDisposed)
            {
                LogCore.Warning("GameObjectPool", $"{prefab.name} 的对象池已注销，本次 Get 返回的实例不再受池管理（归还时将被销毁）。");
                return CreateInstance();
            }

            while (stack.Count > 0)
            {
                var obj = stack.Pop();
                if (obj != null)
                {
                    obj.GetComponent<PoolRecord>().InPool = false;
                    obj.transform.SetParent(null);
                    obj.SetActive(true);
                    return obj;
                }
                // 缓存实例已被外部销毁（fake-null），跳过继续取下一个
            }

            return CreateInstance();
        }

        /// <summary>
        /// 归还一个 GameObject 到池中。
        /// 归还时自动递归调用子物体上所有 IPoolable.OnRecycle。
        /// 重复归还同一对象会被忽略并输出警告；超出容量时将对象销毁。
        /// </summary>
        /// <param name="item">要归还的 GameObject</param>
        public void Return(GameObject item)
        {
            if (item == null) return;

            var record = item.GetComponent<PoolRecord>();
            if (record != null && record.InPool)
            {
                LogCore.Warning("GameObjectPool", $"{prefab.name} 的实例重复归还，已忽略。");
                return;
            }

            if (IsDisposed)
            {
                // 池已注销：缓存已清空，归还对象走回收回调后直接销毁，避免形成幽灵缓存
                RecycleGameObject(item);
                GameObject.Destroy(item);
                return;
            }

            if (stack.Count < Capacity)
            {
                RecycleGameObject(item);
                CacheInstance(item);
            }
            else
            {
                RecycleGameObject(item);
                GameObject.Destroy(item);
            }
        }

        private GameObject CreateInstance()
        {
            var newObj = GameObject.Instantiate(prefab);

            var record = newObj.GetComponent<PoolRecord>();
            if (record == null) record = newObj.AddComponent<PoolRecord>();
            record.SourcePool = this;
            record.InPool = false;

            return newObj;
        }

        private void CacheInstance(GameObject item)
        {
            var record = item.GetComponent<PoolRecord>();
            if (record != null) record.InPool = true;

            item.SetActive(false);
            item.transform.SetParent(GameObjectPoolRoot.GetOrCreate(), false);
            stack.Push(item);
        }

        private void RecycleGameObject(GameObject obj)
        {
            if (!s_bufferStack.TryPop(out var buffer))
                buffer = new List<IPoolable>();

            try
            {
                buffer.Clear();
                obj.GetComponentsInChildren(true, buffer);
                for (int i = 0; i < buffer.Count; i++)
                    buffer[i].OnRecycle();
            }
            finally
            {
                s_bufferStack.Push(buffer);
            }
        }
    }
}
