using System;
using System.Collections.Generic;
using UnityEngine;


namespace GoveKits.Attribute
{
    /// <summary>
    /// 属性容器类
    /// </summary>
    public class AttributeContainer<T> where T : struct
    {
        private Dictionary<string, Attribute<T>> attributes; // 属性字典

        public AttributeContainer()
        {
            attributes = new Dictionary<string, Attribute<T>>();
        }

        /// <summary>
        /// 添加属性
        /// </summary>
        public void AddAttribute(string key, T baseValue)
        {
            if (!attributes.ContainsKey(key))
            {
                attributes[key] = new Attribute<T>(baseValue);
            }
            else
            {
                Debug.LogWarning($"[AttributeContainer] 属性 {key} 已存在，无法添加");
            }
        }

        /// <summary>
        /// 移除属性
        /// </summary>
        public void RemoveAttribute(string key)
        {
            if (attributes.ContainsKey(key))
            {
                attributes.Remove(key);
            }
            else
            {
                Debug.LogWarning($"[AttributeContainer] 属性 {key} 不存在，无法移除");
            }
        }

        /// <summary>
        /// 获取属性
        /// </summary>
        public Attribute<T> GetAttribute(string key)
        {
            if (attributes.TryGetValue(key, out var attribute))
            {
                return attribute;
            }
            else
            {
                throw new KeyNotFoundException($"[AttributeContainer] 属性 {key} 不存在");
            }
        }

        /// <summary>
        /// 检查属性是否存在
        /// </summary>
        public bool HasAttribute(string key) => attributes.ContainsKey(key);
    }
}