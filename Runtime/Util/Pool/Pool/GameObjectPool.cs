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
    }


    /// <summary>
    /// Unity GameObject 对象池，基于 Stack 实现。
    /// 支持预热、容量上限控制，回收时自动递归调用子物体上所有 IPoolable.OnRecycle。
    /// </summary>
    public class GameObjectPool : IPool, IPool<GameObject>
    {
        /// <summary>池的预制体模板，实例化新对象时使用。</summary>
        private readonly GameObject prefab;
        /// <summary>缓存的未激活对象栈。</summary>
        private readonly Stack<GameObject> stack = new();

        /// <summary>池中当前缓存的激活对象数量。</summary>
        public int Count => stack.Count;
        /// <summary>对象池的最大容量，超出容量的对象在归还时将被销毁。</summary>
        public int Capacity { get; private set; }

        public GameObjectPool(GameObject prefab, int maxSize)
        {
            this.prefab = prefab;
            Capacity = maxSize;
        }

        /// <summary>
        /// 预热池，预先实例化并缓存指定数量的对象。
        /// 新实例的SetActive 设为 false 以节省性能。
        /// </summary>
        /// <param name="count">要预热的对象数量</param>
        public void Warmup(int count)
        {
            for (int i = 0; i < count && stack.Count < Capacity; i++)
                Return(Get());
        }

        /// <summary>
        /// 清空池中所有缓存对象并立即销毁它们。
        /// </summary>
        public void Clear()
        {
            while (stack.Count > 0)
            {
                var obj = stack.Pop();
                obj.SetActive(false);
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
                    obj.SetActive(true);
                    return obj;
                }
            }

            var newObj = GameObject.Instantiate(prefab);

            var record = newObj.GetComponent<PoolRecord>();
            if (record == null) record = newObj.AddComponent<PoolRecord>();
            record.SourcePool = this;

            return newObj;
        }

        /// <summary>
        /// 归还一个 GameObject 到池中。
        /// 归还时自动递归调用子物体上所有 IPoolable.OnRecycle。
        /// 超出容量时将对象销毁。
        /// </summary>
        /// <param name="item">要归还的 GameObject</param>
        public void Return(GameObject item)
        {
            if (item == null) return;

            if (stack.Count < Capacity)
            {
                RecycleGameObject(item);
                item.SetActive(false);
                stack.Push(item);
            }
            else
            {
                RecycleGameObject(item);
                GameObject.Destroy(item);
            }
        }

        private void RecycleGameObject(GameObject obj)
        {
            var children = obj.GetComponentsInChildren<IPoolable>();
            foreach (var p in children) p.OnRecycle();
        }
    }
}