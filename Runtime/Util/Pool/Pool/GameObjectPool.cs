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

        public static Transform GetOrCreate()
        {
            if (_root != null)
                return _root.transform;

            _root = GameObject.Find(RootName);
            if (_root == null)
            {
                _root = new GameObject(RootName);
                Object.DontDestroyOnLoad(_root);
            }

            return _root.transform;
        }

        public static void Destroy()
        {
            if (_root != null)
                GameObject.Destroy(_root);
            _root = null;
        }
    }

    /// <summary>
    /// Unity GameObject 对象池，支持预热、容量上限控制，回收时自动递归调用子物体上所有 IPoolable.OnRecycle。
    /// 缓存实例统一挂到 DontDestroyOnLoad 容器下，场景切换不会丢失。
    /// 通过 PoolCore 创建与使用（按预制体单例），通常无需直接实例化。
    /// </summary>
    public class GameObjectPool : IPool, IPool<GameObject>
    {
        // 回收时收集 IPoolable 的共享缓冲，避免每次 Return 产生数组分配
        private static readonly List<IPoolable> s_poolableBuffer = new();

        private readonly GameObject prefab;
        private readonly Stack<GameObject> stack = new();

        /// <summary>池中当前缓存的对象数量。</summary>
        public int Count => stack.Count;
        /// <summary>对象池的最大容量，超出容量的对象在归还时将被销毁。</summary>
        public int Capacity { get; private set; }

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
        /// 从池中获取一个可用的 GameObject 实例。
        /// 池中有缓存对象时取出并激活；池空时根据 prefab 创建新实例。
        /// </summary>
        /// <returns>可用的 GameObject 实例</returns>
        public GameObject Get()
        {
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
            s_poolableBuffer.Clear();
            obj.GetComponentsInChildren(true, s_poolableBuffer);
            foreach (var p in s_poolableBuffer) p.OnRecycle();
        }
    }
}
