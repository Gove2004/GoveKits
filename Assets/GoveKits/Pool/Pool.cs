using System.Collections.Generic;
using UnityEngine;


namespace GoveKits.Pool
{
    /// <summary>
    /// 单个对象池实现
    /// </summary>
    public class Pool
    {
        private GameObject _prefab;
        private Queue<GameObject> _availableObjects = new Queue<GameObject>();
        private Transform _parent;

        public Pool(GameObject prefab, int initialSize, Transform parent)
        {
            _prefab = prefab;
            _parent = parent;

            // 初始化池
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }
        }

        /// <summary>
        /// 调整池大小
        /// </summary>
        /// <param name="newSize"></param>
        public void Resize(int newSize)
        {
            int currentSize = _availableObjects.Count;

            if (newSize > currentSize)
            {
                // 增加池大小
                for (int i = 0; i < newSize - currentSize; i++)
                {
                    CreateNewObject();
                }
            }
            else if (newSize < currentSize)
            {
                // 减少池大小
                for (int i = 0; i < currentSize - newSize; i++)
                {
                    GameObject obj = _availableObjects.Dequeue();
                    Object.Destroy(obj);
                }
            }
        }

        /// <summary>
        /// 创建新对象并添加到池中
        /// </summary>
        private void CreateNewObject()
        {
            GameObject obj = Object.Instantiate(_prefab, _parent);
            obj.SetActive(false);
            _availableObjects.Enqueue(obj);
        }

        /// <summary>
        /// 从池中获取对象
        /// </summary>
        public GameObject GetObject()
        {
            // 如果没有可用对象，创建新对象
            if (_availableObjects.Count == 0)
            {
                CreateNewObject();
            }

            GameObject obj = _availableObjects.Dequeue();
            obj.SetActive(true);

            // 重置对象状态
            IPoolable poolable = obj.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnGetFromPool();
            }

            return obj;
        }

        /// <summary>
        /// 将对象返回池中
        /// </summary>
        public void ReturnObject(GameObject obj)
        {
            obj.SetActive(false);
            obj.transform.SetParent(_parent);

            // 重置对象状态
            IPoolable poolable = obj.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnReturnToPool();
            }

            _availableObjects.Enqueue(obj);
        }

        /// <summary>
        /// 清空对象池
        /// </summary>
        public void Clear()
        {
            foreach (GameObject obj in _availableObjects)
            {
                Object.Destroy(obj);
            }
            _availableObjects.Clear();
        }
        
        public override string ToString()
        {
            return $"Pool of {_prefab.name}, Available Objects: {_availableObjects.Count}";
        }
    }
}