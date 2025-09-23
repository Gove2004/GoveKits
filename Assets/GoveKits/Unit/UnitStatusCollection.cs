using System;
using System.Collections.Generic;


namespace GoveKits.Unit
{
    /// <summary>
    /// 状态属性集，键值对存储，可动态扩展
    /// 支持属性变更事件、属性依赖关系、数值计算管道
    /// K为属性名(一般为string，也可以是自定义Type)，V为属性值(一般为float)
    /// </summary>
    public class UnitStatusCollection<K, V> where V : struct
    {
        // 所有数值属性
        private readonly Dictionary<K, V> _status = new();
        // 数值变更监听器
        private readonly Dictionary<K, List<Action<V, V>>> _changeListeners = new();
        // 数值计算管道，实现Buff动态修改属性值
        private readonly Dictionary<K, List<Func<V, V>>> _calculationPipelines = new();
        // 属性依赖关系，某属性变化时，自动更新依赖它的属性
        private readonly Dictionary<K, List<K>> _dependencies = new();

        /// <summary>
        /// 扩展状态属性（从父状态继承）
        /// </summary>
        public void ExtendStatus(UnitStatusCollection<K, V> parentStatus)
        {
            foreach (var kvp in parentStatus._status)
            {
                if (!_status.ContainsKey(kvp.Key))
                {
                    _status.Add(kvp.Key, kvp.Value);
                }
            }

            // 继承依赖关系
            foreach (var dependency in parentStatus._dependencies)
            {
                if (!_dependencies.ContainsKey(dependency.Key))
                {
                    _dependencies[dependency.Key] = new List<K>(dependency.Value);
                }
            }
        }

        /// <summary>
        /// 检查是否存在指定属性
        /// </summary>
        public bool HasStatus(K key) => _status.ContainsKey(key);

        /// <summary>
        /// 设置状态属性值, 有就覆盖, 没有就新增
        /// </summary>
        public void SetStatus(K key, V value)
        {
            V oldValue = default;
            bool hasOldValue = _status.TryGetValue(key, out oldValue);

            // 如果值没有变化，则不做任何操作
            if (hasOldValue && EqualityComparer<V>.Default.Equals(oldValue, value))
                return;

            // 更新值
            if (hasOldValue)
            {
                _status[key] = value;
            }
            else
            {
                _status.Add(key, value);
            }

            // 触发变更事件
            NotifyStatusChanged(key, oldValue, value);

            // 处理依赖属性
            UpdateDependentProperties(key);
        }

        /// <summary>
        /// 获取状态属性值（带默认值）
        /// </summary>
        public V GetStatus(K key, V defaultValue = default)
        {
            return _status.TryGetValue(key, out var value) ? value : defaultValue;
        }

        /// <summary>
        /// 获取计算后的状态属性值（通过计算管道）
        /// </summary>
        public V GetCalculatedStatus(K key, V defaultValue = default)
        {
            if (!_status.TryGetValue(key, out var value))
                return defaultValue;

            // 应用计算管道
            if (_calculationPipelines.TryGetValue(key, out var pipeline))
            {
                foreach (var func in pipeline)
                {
                    value = func(value);
                }
            }

            return value;
        }

        /// <summary>
        /// 移除状态属性
        /// </summary>
        public void RemoveStatus(K key)
        {
            if (_status.ContainsKey(key))
            {
                var oldValue = _status[key];
                _status.Remove(key);

                // 触发变更事件（值为移除）
                NotifyStatusChanged(key, oldValue, default);

                // 处理依赖属性
                UpdateDependentProperties(key);
            }
        }

        /// <summary>
        /// 添加属性变更监听器
        /// </summary>
        public void AddChangeListener(K key, Action<V, V> listener)
        {
            if (!_changeListeners.TryGetValue(key, out var listeners))
            {
                listeners = new List<Action<V, V>>();
                _changeListeners[key] = listeners;
            }
            listeners.Add(listener);
        }

        /// <summary>
        /// 移除属性变更监听器
        /// </summary>
        public void RemoveChangeListener(K key, Action<V, V> listener)
        {
            if (_changeListeners.TryGetValue(key, out var listeners))
            {
                listeners.Remove(listener);
            }
        }

        /// <summary>
        /// 添加计算管道（用于动态修改属性值）
        /// </summary>
        public void AddCalculation(K key, Func<V, V> calculation)
        {
            if (!_calculationPipelines.TryGetValue(key, out var pipeline))
            {
                pipeline = new List<Func<V, V>>();
                _calculationPipelines[key] = pipeline;
            }
            pipeline.Add(calculation);

            // 触发变更事件（因为计算管道改变了实际值）
            if (_status.TryGetValue(key, out var currentValue))
            {
                NotifyStatusChanged(key, currentValue, currentValue);
            }
        }

        /// <summary>
        /// 移除计算管道
        /// </summary>
        public void RemoveCalculation(K key, Func<V, V> calculation)
        {
            if (_calculationPipelines.TryGetValue(key, out var pipeline))
            {
                pipeline.Remove(calculation);

                // 触发变更事件
                if (_status.TryGetValue(key, out var currentValue))
                {
                    NotifyStatusChanged(key, currentValue, currentValue);
                }
            }
        }

        /// <summary>
        /// 添加属性依赖关系（当依赖属性变化时，重新计算目标属性）
        /// </summary>
        public void AddDependency(K targetKey, K dependencyKey)
        {
            if (!_dependencies.TryGetValue(dependencyKey, out var dependencies))
            {
                dependencies = new List<K>();
                _dependencies[dependencyKey] = dependencies;
            }

            if (!dependencies.Contains(targetKey))
            {
                dependencies.Add(targetKey);
            }
        }

        /// <summary>
        /// 移除属性依赖关系
        /// </summary>
        public void RemoveDependency(K targetKey, K dependencyKey)
        {
            if (_dependencies.TryGetValue(dependencyKey, out var dependencies))
            {
                dependencies.Remove(targetKey);
            }
        }

        private void NotifyStatusChanged(K key, V oldValue, V newValue)
        {
            if (_changeListeners.TryGetValue(key, out var listeners))
            {
                foreach (var listener in listeners.ToArray()) // 使用ToArray防止在遍历时修改集合
                {
                    listener(oldValue, newValue);
                }
            }
        }

        private void UpdateDependentProperties(K changedKey)
        {
            if (_dependencies.TryGetValue(changedKey, out var dependentKeys))
            {
                foreach (var targetKey in dependentKeys.ToArray()) // 使用ToArray防止在遍历时修改集合
                {
                    // 重新计算依赖属性
                    if (_status.TryGetValue(targetKey, out var currentValue))
                    {
                        // 这里只是触发重新计算通知，实际值可能没有变化
                        NotifyStatusChanged(targetKey, currentValue, currentValue);
                    }
                }
            }
        }
    }
}







