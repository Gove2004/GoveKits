using System.Collections.Generic;
using UnityEngine;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// Unity GameObject 对象池实现类
    /// </summary>
    public class GameObjectPool : IPool, IPool<GameObject>
    {
        private GameObject _prefab;
        
        private readonly Stack<GameObject> _stack = new();
        
        public int CachedCount => _stack.Count;
        
        public int MaxSize { get; private set; }

        public GameObjectPool(GameObject prefab, int maxSize)
        {
            _prefab = prefab;
            MaxSize = maxSize;
        }

        public void Warmup(int count)
        {
            for (int i = 0; i < count && _stack.Count < MaxSize; i++)
            {
                Return(Get());
            }
        }

        public void Clear()
        {
            while (_stack.Count > 0)
            {
                var obj = _stack.Pop();
                obj.SetActive(false);
                GameObject.Destroy(obj);
            }
        }

        public GameObject Get()
        {
            while (_stack.Count > 0)
            {
                var obj = _stack.Pop();
                if (obj != null)
                {
                    obj.SetActive(true);
                    return obj;
                }
            }

            var newObj = GameObject.Instantiate(_prefab);
            
            var record = newObj.GetComponent<PoolRecord>();
            if (record == null) record = newObj.AddComponent<PoolRecord>();
            record.SourcePool = this;
            
            return newObj;
        }

        public void Return(GameObject item)
        {
            if (item == null) return;
            
            if (_stack.Count < MaxSize)
            {
                RecycleGameObject(item);
                item.SetActive(false);
                _stack.Push(item);
            }
            else
            {
                RecycleGameObject(item);
                GameObject.Destroy(item);
            }
        }
        
        private static readonly List<IPoolable> _tempPoolables = new List<IPoolable>();
        
        private static void RecycleGameObject(GameObject obj)
        {
            obj.GetComponentsInChildren<IPoolable>(true, _tempPoolables);
            
            foreach (var p in _tempPoolables) p.OnRecycle();
            
            _tempPoolables.Clear();
        }
    }

    /// <summary>
    /// 池记录组件
    /// </summary>
    public class PoolRecord : MonoBehaviour
    {
        public GameObjectPool SourcePool { get; set; }
    }
}