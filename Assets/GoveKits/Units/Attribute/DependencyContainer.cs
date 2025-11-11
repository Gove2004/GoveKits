


using System.Collections.Generic;
using System.Linq;

namespace GoveKits.Units
{
    public class DependencyContainer<T>
    {
        private readonly Dictionary<T, List<T>> _dependencies = new(); // key -> 依赖的键列表
        private readonly Dictionary<T, List<T>> _dependents = new();   // key -> 被哪些键依赖

        // 添加依赖关系
        public void AddDependency(T key, T dependency)
        {
            if (HasCircularDependency(key, dependency))
            {
                throw new System.InvalidOperationException($"[DependencyContainer] 循环依赖 {key} -> {dependency}");
            }
            // 添加到依赖关系
            if (!_dependencies.TryGetValue(key, out var dependencies))
            {
                dependencies = new List<T>();
                _dependencies[key] = dependencies;
            }
            if (!dependencies.Contains(dependency))
                dependencies.Add(dependency);
            // 添加到被依赖关系
            if (!_dependents.TryGetValue(dependency, out var dependents))
            {
                dependents = new List<T>();
                _dependents[dependency] = dependents;
            }
            if (!dependents.Contains(key))
                dependents.Add(key);
        }

        // 获取影响列表
        public IReadOnlyList<T> GetDependents(T key) 
        {
            if (_dependents.TryGetValue(key, out var dependents))
            {
                return dependents.AsReadOnly();
            }
            return new List<T>().AsReadOnly();
        }

        // 修正后的循环依赖检测
        private bool HasCircularDependency(T key, T dependency)
        {
            var visited = new HashSet<T> { key };
            return CheckDependencyPath(dependency, key, new HashSet<T>(visited));
        }
        private bool CheckDependencyPath(T current, T target, HashSet<T> visited)
        {
            if (current.Equals(target)) return true;
            if (!visited.Add(current)) return false;
            
            if (_dependencies.TryGetValue(current, out var dependencies))
            {
                foreach (var dep in dependencies)
                {
                    if (CheckDependencyPath(dep, target, visited))
                        return true;
                }
            }
            visited.Remove(current);
            return false;
        }
        
        // 清空所有依赖关系
        public void Clear()
        {
            _dependencies.Clear();
            _dependents.Clear();
        }
    }
}