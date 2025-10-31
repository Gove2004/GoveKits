

using System.Collections.Generic;

namespace GoveKits.Unit
{
    /// <summary>
    /// 效果基类
    /// </summary>
    public class BaseEffect
    {
        private float duration;
        public virtual void Start() { }
        public virtual void Update(float deltaTime) { }
        public virtual void End() { }
        public virtual bool IsExpired => false;
    }

    /// <summary>
    /// 效果属性集，键值对存储，可动态扩展
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="V"></typeparam>
    public class UnitEffectCollection<K, V> where V : BaseEffect
    {
        // 所有效果属性
        private readonly Dictionary<K, List<V>> _effects = new();

        /// <summary>
        /// 检查是否存在指定效果
        /// </summary>
        public bool HasEffect(K key) => _effects.ContainsKey(key);

        /// <summary>
        /// 添加效果
        /// </summary>
        public void AddEffect(K key, V effect)
        {
            if (!_effects.ContainsKey(key))
            {
                _effects[key] = new List<V>();
            }
            _effects[key].Add(effect);
        }

        /// <summary>
        /// 移除效果
        /// </summary>
        public void RemoveEffect(K key, V effect)
        {
            if (_effects.ContainsKey(key))
            {
                _effects[key].Remove(effect);
                if (_effects[key].Count == 0)
                {
                    _effects.Remove(key);
                }
            }
        }

        /// <summary>
        /// 更新效果
        /// </summary>
        public void UpdateEffect(float deltaTime)
        {
            var keysToRemove = new List<K>();

            foreach (var kvp in _effects)
            {
                kvp.Value.RemoveAll(effect =>
                {
                    // 假设BaseEffect有一个Update方法和IsExpired属性
                    effect.Update(deltaTime);
                    return effect.IsExpired;
                });

                if (kvp.Value.Count == 0)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _effects.Remove(key);
            }
        }
    }
}