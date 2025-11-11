using System;
using System.Collections.Generic;

namespace GoveKits.Units
{
    public class AttributeContainer
    {
        private readonly Dictionary<string, Attribute> _attributes = new();
        private readonly DependencyContainer<string> _dependencyContainer = new();

        public bool Has(string key)
        {
            return _attributes.ContainsKey(key);
        }

        public void AddAttribute(string key, float initialValue)
        {
            if (_attributes.ContainsKey(key))
            {
                throw new InvalidOperationException($"[AttributeContainer] 已存在属性 {key}");
            }
            _attributes[key] = new Attribute(key, initialValue);
        }

        public void AddAttribute(string key, Func<float> calculator, List<string> dependsOn = null)
        {
            if (_attributes.ContainsKey(key))
            {
                throw new InvalidOperationException($"[AttributeContainer] 已存在属性 {key}");
            }

            // 先检查依赖属性是否存在
            if (dependsOn != null)
            {
                foreach (var dependKey in dependsOn)
                {
                    if (!_attributes.ContainsKey(dependKey))
                    {
                        throw new KeyNotFoundException($"[AttributeContainer] 未知属性 {dependKey}");
                    }
                }
            }

            _attributes[key] = new Attribute(key, calculator);

            // 添加依赖关系
            if (dependsOn != null)
            {
                foreach (var dependKey in dependsOn)
                {
                    _dependencyContainer.AddDependency(key, dependKey);

                    // 订阅依赖属性变化事件
                    _attributes[dependKey].OnValueChanged += (oldValue, newValue) =>
                    {
                        _attributes[key].MarkDirty();
                    };
                }
            }
        }

        public bool TryGetAttribute(string key, out Attribute attribute)
        {
            return _attributes.TryGetValue(key, out attribute);
        }

        public float GetValue(string key)
        {
            if (!_attributes.ContainsKey(key))
                throw new KeyNotFoundException($"[AttributeContainer] 未知属性 {key}");
            return _attributes[key].Value;
        }

        public bool GetValue(string key, out float value)
        {
            if (_attributes.TryGetValue(key, out var attribute))
            {
                value = attribute.Value;
                return true;
            }
            value = default;
            return false;
        }

        public float SetValue(string key, float value)
        {
            if (!_attributes.ContainsKey(key))
                throw new KeyNotFoundException($"[AttributeContainer] 未知属性 {key}");

            var attribute = _attributes[key];
            if (attribute.IsComputed)
                throw new InvalidOperationException($"[AttributeContainer] 属性 {key} 是只读的计算属性");

            attribute.Value = value;
            return value;
        }

        public void Clear()
        {
            foreach (var kvp in _attributes)
            {
                kvp.Value.Clear();
            }
            _attributes.Clear();
            _dependencyContainer.Clear();
        }

        // public void BatchSetValues(Dictionary<string, float> values)
        // {
        //     foreach (var kvp in values)
        //     {
        //         if (_attributes.TryGetValue(kvp.Key, out var attribute) && !attribute.IsReadOnly)
        //         {
        //             attribute.Value = kvp.Value;
        //         }
        //     }
        // }

        // public Dictionary<string, float> GetSnapshot()
        // {
        //     var snapshot = new Dictionary<string, float>();
        //     foreach (var kvp in _attributes)
        //     {
        //         snapshot[kvp.Key] = kvp.Value.Value;
        //     }
        //     return snapshot;
        // }

        // public IReadOnlyList<string> GetDependents(string key)
        // {
        //     return _dependencyContainer.GetDependents(key);
        // }

        // 添加监听器，返回取消监听的操作
        public Action AddListener(string key, Action<float, float> listener)
        {
            if (!_attributes.ContainsKey(key))
                throw new KeyNotFoundException($"[AttributeContainer] 未知属性 {key}");

            _attributes[key].OnValueChanged += listener;
            return () => RemoveListener(key, listener);
        }

        // 移除监听器
        public void RemoveListener(string key, Action<float, float> listener)
        {
            if (!_attributes.ContainsKey(key))
                throw new KeyNotFoundException($"[AttributeContainer] 未知属性 {key}");

            _attributes[key].OnValueChanged -= listener;
        }

    }
}