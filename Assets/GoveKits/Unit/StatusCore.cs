using System;
using System.Collections.Generic;
using System.Linq;

namespace GoveKits.Unit
{
    /// <summary>
    /// 单个状态封装。保存基础值、可选的自定义 getter/setter、缓存与脏标记，并提供变更事件。Lazy 版本。
    /// - Setter: Func<V,V> 接受欲写入的值并返回实际存储的基础值（便于做截断/校验等）。可为空。
    /// - Getter: Func<V> 计算最终值。可为空，默认返回基础值。
    /// - Dirty Flag: 指示当前值是否已被修改但尚未更新。
    /// </summary>
    /// <typeparam name="V">状态值的类型</typeparam>
    public class Status<V>
    {
        // 基础存储值与计算缓存
        private V _baseValue;
        private V _cachedValue;

        // 可选自定义 setter/getter
        public readonly Func<V, V> setter;
        public readonly Func<V> getter;

        // 脏标识
        private bool _dirty;

        /// <summary>
        /// 获取或设置脏标记。当值为true时表示状态已被修改但尚未重新评估。
        /// </summary>
        public bool dirty
        {
            get { return _dirty; }
            set { _dirty = value; }
        }

        /// <summary>
        /// 状态值变更事件。参数为(oldValue, newValue)
        /// </summary>
        public event Action<V, V> onValueChanged;

        /// <summary>
        /// 初始化状态实例
        /// </summary>
        /// <param name="value">初始基础值</param>
        /// <param name="getter">可选的自定义getter函数，用于计算最终值</param>
        /// <param name="setter">可选的自定义setter函数，用于处理值的设置</param>
        public Status(V value, Func<V> getter = null, Func<V, V> setter = null)
        {
            _baseValue = value;
            this.getter = getter;
            this.setter = setter;
            _cachedValue = EvaluateInternal();
            _dirty = false;
        }

        /// <summary>
        /// 内部评估方法，重新计算当前值并清除脏标记
        /// </summary>
        /// <returns>计算后的最终值</returns>
        private V EvaluateInternal()
        {
            if (getter != null)
            {
                var result = getter();
                _cachedValue = result;
                _dirty = false;
                return result;
            }
            _cachedValue = _baseValue;
            _dirty = false;
            return _cachedValue;
        }

        /// <summary>
        /// 获取或设置状态的最终值。设置时会触发变更事件检查。
        /// </summary>
        public V Value
        {
            get
            {
                return _dirty ? EvaluateInternal() : _cachedValue;
            }
            set
            {
                V oldFinal = _dirty ? EvaluateInternal() : _cachedValue;
                
                // 应用自定义 setter
                var storedValue = setter != null ? setter(value) : value;
                _baseValue = storedValue;
                
                _dirty = true;
                V newFinal = EvaluateInternal();

                // 记录是否需要触发事件
                if (!EqualityComparer<V>.Default.Equals(oldFinal, newFinal))
                {
                    onValueChanged?.Invoke(oldFinal, newFinal);; // 复制事件委托引用
                }
            }
        }

        /// <summary>
        /// 读取基础存储值（仅供内部使用）
        /// </summary>
        /// <returns>基础存储值</returns>
        internal V ReadBaseValue()
        {
            return _baseValue;
        }

        /// <summary>
        /// 强制标记为脏状态，下次读取时会重新评估
        /// </summary>
        internal void MarkDirty()
        {
            _dirty = true;
        }

        /// <summary>
        /// 清理状态，重置所有值和事件
        /// </summary>
        public void Clear()
        {
            _baseValue = default(V);
            _cachedValue = default(V);
            _dirty = false;
            onValueChanged = null;
        }
    }

    /// <summary>
    /// 通用状态集合，支持依赖关系和惰性评估。
    /// 提供键值对形式的状态管理，支持状态间的依赖关系自动传播变更。
    /// </summary>
    /// <typeparam name="K">状态键的类型</typeparam>
    /// <typeparam name="V">状态值的类型</typeparam>
    public class StatusSet<K, V>
    {
        private readonly Dictionary<K, Status<V>> _status = new Dictionary<K, Status<V>>();
        private readonly DependencyMap<K> _dependencyMap = new DependencyMap<K>();

        /// <summary>
        /// 索引器，通过键获取或设置状态值
        /// </summary>
        /// <param name="key">状态键</param>
        /// <returns>状态值</returns>
        public V this[K key]
        {
            get => Get(key);
            set => Set(key, value);
        }

        /// <summary>
        /// 添加状态的重载运算符
        /// </summary>
        public static StatusSet<K, V> operator +(StatusSet<K, V> set, (K key, Status<V> status) item)
            => set.Append(item);

        /// <summary>
        /// 移除状态的重载运算符
        /// </summary>
        public static StatusSet<K, V> operator -(StatusSet<K, V> set, K key)
            => set.Remove(key);

        /// <summary>
        /// 获取所有状态键的集合
        /// </summary>
        public IEnumerable<K> Keys => _status.Keys.ToList();

        /// <summary>
        /// 获取状态集合中键值对的数量
        /// </summary>
        public int Count => _status.Count;

        /// <summary>
        /// 添加新状态到集合中（键已存在时抛出异常）
        /// </summary>
        /// <param name="item">包含键和状态实例的元组</param>
        /// <returns>当前StatusSet实例（支持链式调用）</returns>
        /// <exception cref="InvalidOperationException">当键已存在时抛出</exception>
        public StatusSet<K, V> Append((K key, Status<V> status) item)
        {
            if (_status.ContainsKey(item.key))
                throw new InvalidOperationException($"[StatusSet] Key already exists: {item.key}");

            var status = item.status;
            status.onValueChanged += (oldVal, newVal) => OnStatusValueChanged(item.key, oldVal, newVal);
            _status.Add(item.key, status);

            return this;
        }

        /// <summary>
        /// 从集合中移除指定状态（如果存在依赖则抛出异常）
        /// </summary>
        /// <param name="key">要移除的状态键</param>
        /// <returns>当前StatusSet实例（支持链式调用）</returns>
        /// <exception cref="InvalidOperationException">当存在依赖关系时抛出</exception>
        public StatusSet<K, V> Remove(K key)
        {
            if (!_status.TryGetValue(key, out var status))
                return this;

            // 检查是否有其他键依赖于此键
            var dependents = _dependencyMap.GetDependents(key);
            if (dependents.Count > 0)
            {
                throw new InvalidOperationException(
                    $"[StatusSet] Cannot remove key {key} because it is depended on by: {string.Join(", ", dependents)}");
            }

            // 移除依赖关系
            _dependencyMap.RemoveKey(key);

            // 清理状态
            status.Clear();
            _status.Remove(key);

            return this;
        }

        /// <summary>
        /// 获取指定键的状态值（键不存在时返回默认值）
        /// </summary>
        /// <param name="key">状态键</param>
        /// <param name="defaultValue">键不存在时返回的默认值</param>
        /// <returns>状态值或默认值</returns>
        public V Get(K key, V defaultValue = default(V))
        {
            return _status.TryGetValue(key, out var status) ? status.Value : defaultValue;
        }

        /// <summary>
        /// 设置指定键的状态值（键不存在时抛出异常）
        /// </summary>
        /// <param name="key">状态键</param>
        /// <param name="value">要设置的值</param>
        /// <exception cref="KeyNotFoundException">当键不存在时抛出</exception>
        public void Set(K key, V value)
        {
            if (!_status.TryGetValue(key, out var status))
                throw new KeyNotFoundException($"[StatusSet] Key not found: {key}");

            status.Value = value;
        }

        /// <summary>
        /// 检查集合中是否包含指定键
        /// </summary>
        /// <param name="key">要检查的键</param>
        /// <returns>如果包含则返回true，否则返回false</returns>
        public bool ContainsKey(K key)
        {
            return _status.ContainsKey(key);
        }

        /// <summary>
        /// 为指定状态添加值变更监听器
        /// </summary>
        /// <param name="key">状态键</param>
        /// <param name="listener">变更监听器委托</param>
        /// <returns>添加的监听器（便于后续移除）</returns>
        /// <exception cref="ArgumentNullException">当listener为null时抛出</exception>
        /// <exception cref="KeyNotFoundException">当键不存在时抛出</exception>
        public Action<V, V> AddListener(K key, Action<V, V> listener)
        {
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));

            if (!_status.TryGetValue(key, out var status))
                throw new KeyNotFoundException($"[StatusSet] Key not found: {key}");

            status.onValueChanged += listener;
            return listener;
        }

        /// <summary>
        /// 移除指定状态的变更监听器
        /// </summary>
        /// <param name="key">状态键</param>
        /// <param name="listener">要移除的监听器委托</param>
        public void RemoveListener(K key, Action<V, V> listener)
        {
            if (listener == null) return;

            if (_status.TryGetValue(key, out var status))
            {
                status.onValueChanged -= listener;
            }
        }

        /// <summary>
        /// 添加依赖关系，指定dependentKey依赖于dependsOnKey
        /// </summary>
        /// <param name="dependentKey">依赖方键</param>
        /// <param name="dependsOnKey">被依赖方键</param>
        /// <exception cref="KeyNotFoundException">当任一键不存在时抛出</exception>
        /// <exception cref="ArgumentException">当尝试添加自依赖时抛出</exception>
        /// <exception cref="InvalidOperationException">当检测到循环依赖时抛出</exception>
        public void AddDependency(K dependentKey, K dependsOnKey)
        {
            // 验证键存在
            if (!_status.ContainsKey(dependentKey))
                throw new KeyNotFoundException($"[StatusSet] Dependent key not found: {dependentKey}");
            if (!_status.ContainsKey(dependsOnKey))
                throw new KeyNotFoundException($"[StatusSet] Depends-on key not found: {dependsOnKey}");

            // 检查自依赖
            if (EqualityComparer<K>.Default.Equals(dependentKey, dependsOnKey))
                throw new ArgumentException($"[StatusSet] Self-dependency is not allowed: {dependentKey}");

            // 检查循环依赖
            if (_dependencyMap.WouldCreateCycle(dependentKey, dependsOnKey))
                throw new InvalidOperationException($"[StatusSet] Circular dependency detected: {dependentKey} -> {dependsOnKey}");

            _dependencyMap.AddDependency(dependentKey, dependsOnKey);
        }

        /// <summary>
        /// 移除依赖关系
        /// </summary>
        /// <param name="dependentKey">依赖方键</param>
        /// <param name="dependsOnKey">被依赖方键</param>
        public void RemoveDependency(K dependentKey, K dependsOnKey)
        {
            _dependencyMap.RemoveDependency(dependentKey, dependsOnKey);
        }

        /// <summary>
        /// 批量添加依赖关系
        /// </summary>
        /// <param name="dependentKey">依赖方键</param>
        /// <param name="dependsOnKeys">被依赖方键集合</param>
        public void AddDependencies(K dependentKey, IEnumerable<K> dependsOnKeys)
        {
            foreach (var dependsOnKey in dependsOnKeys)
            {
                AddDependency(dependentKey, dependsOnKey);
            }
        }

        /// <summary>
        /// 获取指定键的所有直接依赖项
        /// </summary>
        /// <param name="key">要查询的键</param>
        /// <returns>依赖项列表</returns>
        public IReadOnlyList<K> GetDependencies(K key)
        {
            return _dependencyMap.GetDependencies(key);
        }

        /// <summary>
        /// 获取所有直接依赖于此键的键
        /// </summary>
        /// <param name="key">要查询的键</param>
        /// <returns>依赖此键的键列表</returns>
        public IReadOnlyList<K> GetDependents(K key)
        {
            return _dependencyMap.GetDependents(key);
        }

        /// <summary>
        /// 清空所有状态和依赖关系
        /// </summary>
        public void Clear()
        {
            foreach (var status in _status.Values)
            {
                status.Clear();
            }
            _status.Clear();
            _dependencyMap.Clear();
        }

        /// <summary>
        /// 状态值变更时的处理函数，通知所有依赖项
        /// </summary>
        /// <param name="changedKey">发生变更的键</param>
        /// <param name="oldValue">旧值</param>
        /// <param name="newValue">新值</param>
        private void OnStatusValueChanged(K changedKey, V oldValue, V newValue)
        {
            NotifyDependents(changedKey);
        }

        /// <summary>
        /// 通知所有依赖项标记为脏状态
        /// </summary>
        /// <param name="changedKey">发生变更的键</param>
        private void NotifyDependents(K changedKey)
        {
            var dependents = _dependencyMap.GetDependents(changedKey);
            if (dependents.Count == 0) return;

            var visited = new HashSet<K>();
            var queue = new Queue<K>(dependents);

            while (queue.Count > 0)
            {
                var dependentKey = queue.Dequeue();

                if (visited.Contains(dependentKey)) continue;
                visited.Add(dependentKey);

                if (_status.TryGetValue(dependentKey, out var status))
                {
                    // 标记为脏，触发重新评估
                    status.MarkDirty();

                    // 获取此依赖项的下游依赖
                    var furtherDependents = _dependencyMap.GetDependents(dependentKey);
                    foreach (var furtherKey in furtherDependents.Where(k => !visited.Contains(k)))
                    {
                        queue.Enqueue(furtherKey);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 依赖关系映射表，管理键之间的依赖关系
    /// 提供依赖检测、循环依赖预防等功能
    /// </summary>
    /// <typeparam name="K">键的类型</typeparam>
    internal class DependencyMap<K>
    {
        private readonly Dictionary<K, List<K>> _dependencies = new Dictionary<K, List<K>>(); // key -> 依赖的键列表
        private readonly Dictionary<K, List<K>> _dependents = new Dictionary<K, List<K>>();   // key -> 被哪些键依赖

        /// <summary>
        /// 添加依赖关系
        /// </summary>
        /// <param name="dependentKey">依赖方键</param>
        /// <param name="dependsOnKey">被依赖方键</param>
        public void AddDependency(K dependentKey, K dependsOnKey)
        {
            // 添加到依赖关系
            if (!_dependencies.TryGetValue(dependentKey, out var dependencies))
            {
                dependencies = new List<K>();
                _dependencies[dependentKey] = dependencies;
            }

            if (!dependencies.Contains(dependsOnKey))
                dependencies.Add(dependsOnKey);

            // 添加到被依赖关系
            if (!_dependents.TryGetValue(dependsOnKey, out var dependents))
            {
                dependents = new List<K>();
                _dependents[dependsOnKey] = dependents;
            }

            if (!dependents.Contains(dependentKey))
                dependents.Add(dependentKey);
        }

        /// <summary>
        /// 移除指定的依赖关系
        /// </summary>
        /// <param name="dependentKey">依赖方键</param>
        /// <param name="dependsOnKey">被依赖方键</param>
        public void RemoveDependency(K dependentKey, K dependsOnKey)
        {
            // 从依赖关系中移除
            if (_dependencies.TryGetValue(dependentKey, out var dependencies))
            {
                dependencies.Remove(dependsOnKey);
                if (dependencies.Count == 0)
                    _dependencies.Remove(dependentKey);
            }

            // 从被依赖关系中移除
            if (_dependents.TryGetValue(dependsOnKey, out var dependents))
            {
                dependents.Remove(dependentKey);
                if (dependents.Count == 0)
                    _dependents.Remove(dependsOnKey);
            }
        }

        /// <summary>
        /// 移除指定键的所有依赖关系（包括作为依赖方和被依赖方）
        /// </summary>
        /// <param name="key">要移除的键</param>
        public void RemoveKey(K key)
        {
            // 移除该键的依赖关系
            if (_dependencies.TryGetValue(key, out var dependencies))
            {
                foreach (var dep in dependencies)
                {
                    if (_dependents.TryGetValue(dep, out var dependents))
                    {
                        dependents.Remove(key);
                        if (dependents.Count == 0)
                            _dependents.Remove(dep);
                    }
                }
                _dependencies.Remove(key);
            }

            // 移除其他键对该键的依赖
            if (_dependents.TryGetValue(key, out var keyDependents))
            {
                foreach (var dependent in keyDependents.ToList()) // ToList 避免修改集合
                {
                    RemoveDependency(dependent, key);
                }
            }
        }

        /// <summary>
        /// 检查添加依赖关系是否会创建循环依赖
        /// </summary>
        /// <param name="startKey">起始键（依赖方）</param>
        /// <param name="targetKey">目标键（被依赖方）</param>
        /// <returns>如果会创建循环依赖则返回true，否则返回false</returns>
        public bool WouldCreateCycle(K startKey, K targetKey)
        {
            var visited = new HashSet<K>();
            var stack = new Stack<K>();
            stack.Push(targetKey);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (EqualityComparer<K>.Default.Equals(current, startKey))
                    return true;

                if (visited.Contains(current))
                    continue;

                visited.Add(current);

                if (_dependencies.TryGetValue(current, out var dependencies))
                {
                    foreach (var dep in dependencies)
                    {
                        if (!visited.Contains(dep))
                            stack.Push(dep);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 获取指定键的所有直接依赖项
        /// </summary>
        /// <param name="key">要查询的键</param>
        /// <returns>依赖项列表的只读视图</returns>
        public IReadOnlyList<K> GetDependencies(K key)
        {
            return _dependencies.TryGetValue(key, out var dependencies)
                ? new List<K>(dependencies).AsReadOnly()
                : new List<K>().AsReadOnly();
        }

        /// <summary>
        /// 获取所有直接依赖于此键的键
        /// </summary>
        /// <param name="key">要查询的键</param>
        /// <returns>依赖此键的键列表的只读视图</returns>
        public IReadOnlyList<K> GetDependents(K key)
        {
            return _dependents.TryGetValue(key, out var dependents)
                ? new List<K>(dependents).AsReadOnly()
                : new List<K>().AsReadOnly();
        }

        /// <summary>
        /// 检查映射表中是否包含指定键
        /// </summary>
        /// <param name="key">要检查的键</param>
        /// <returns>如果包含则返回true，否则返回false</returns>
        public bool ContainsKey(K key)
        {
            return _dependencies.ContainsKey(key);
        }

        /// <summary>
        /// 清空所有依赖关系
        /// </summary>
        public void Clear()
        {
            _dependencies.Clear();
            _dependents.Clear();
        }

        /// <summary>
        /// 添加依赖关系的运算符重载
        /// </summary>
        public static DependencyMap<K> operator +(DependencyMap<K> map, (K key, K dependsOn) item)
        {
            map.AddDependency(item.key, item.dependsOn);
            return map;
        }

        /// <summary>
        /// 移除依赖关系的运算符重载
        /// </summary>
        public static DependencyMap<K> operator -(DependencyMap<K> map, (K key, K dependsOn) item)
        {
            map.RemoveDependency(item.key, item.dependsOn);
            return map;
        }
    }
}