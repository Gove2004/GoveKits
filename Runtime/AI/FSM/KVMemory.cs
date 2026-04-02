using System.Collections.Generic;

namespace GoveKits.Runtime.AI
{
    /// <summary>
    /// 基于键值对的通用记忆体实现
    /// 
    /// 核心功能：
    /// 1. 提供泛型键值对存储
    /// 2. 支持任意类型的数据存取
    /// 3. 作为 IAIMemory 接口的默认实现
    /// </summary>
    public class KVMemory : IAIMemory
    {
        /// <summary>内部数据存储容器</summary>
        private readonly Dictionary<string, object> _data = new();

        /// <summary>初始化记忆系统 - 清空数据</summary>
        public void Init() => _data.Clear();
        
        /// <summary>清理记忆系统 - 清空数据</summary>
        public void UnInit() => _data.Clear();

        /// <summary>
        /// 写入记忆数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">记忆键名</param>
        /// <param name="value">记忆值</param>
        public void Set<T>(string key, T value) => _data[key] = value;

        /// <summary>
        /// 读取记忆数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">记忆键名</param>
        /// <returns>记忆值，不存在则返回 default(T)</returns>
        public T Get<T>(string key)
        {
            if (_data.TryGetValue(key, out var val)) return (T)val;
            return default;
        }
    }
}