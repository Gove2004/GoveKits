using System;
using System.Collections.Generic;
using UnityEngine;


namespace GoveKits.Attribute
{
    /// <summary>
    /// 属性容器类
    /// </summary>
    public class AttributeContainer
    {
        private Dictionary<string, Attribute> attributes; // 属性字典

        public AttributeContainer()
        {
            attributes = new Dictionary<string, Attribute>();
        }

        /// <summary>
        /// 添加属性
        /// </summary>
        public void AddAttribute(string key, float baseValue)
        {
            if (!attributes.ContainsKey(key))
            {
                attributes[key] = new Attribute(baseValue);
            }
            else
            {
                Debug.LogWarning($"[AttributeContainer] 属性 {key} 已存在，无法添加");
            }
        }

        /// <summary>
        /// 添加或覆盖属性，如果存在则设置 Base 并标记为脏
        /// </summary>
        public void AddOrSetAttribute(string key, float baseValue)
        {
            if (!attributes.ContainsKey(key))
            {
                attributes[key] = new Attribute(baseValue);
            }
            else
            {
                attributes[key].Base = baseValue;
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
        public Attribute GetAttribute(string key)
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
        /// 安全获取属性，不存在时返回 null
        /// </summary>
        public Attribute TryGetAttribute(string key)
        {
            if (attributes.TryGetValue(key, out var attribute))
            {
                return attribute;
            }
            return null;
        }

        /// <summary>
        /// 直接获取属性值（会触发延迟计算）。不存在抛出 KeyNotFoundException
        /// </summary>
        public float GetValue(string key)
        {
            return GetAttribute(key).Value;
        }

        /// <summary>
        /// 如果属性存在，设置基础值并标记为脏；不存在抛出异常
        /// </summary>
        public void SetBase(string key, float baseValue)
        {
            GetAttribute(key).Base = baseValue;
        }

        /// <summary>
        /// 尝试给属性添加一个修正器，返回是否成功
        /// </summary>
        public bool AddModifierToAttribute(string key, AttributeModifier modifier)
        {
            var attr = TryGetAttribute(key);
            if (attr == null) return false;
            attr.AddModifier(modifier);
            return true;
        }

        /// <summary>
        /// 尝试从属性移除一个修正器，返回是否成功
        /// </summary>
        public bool RemoveModifierFromAttribute(string key, AttributeModifier modifier)
        {
            var attr = TryGetAttribute(key);
            if (attr == null) return false;
            attr.RemoveModifier(modifier);
            return true;
        }

        /// <summary>
        /// 清空指定属性的所有修正器
        /// </summary>
        public bool ClearModifiers(string key)
        {
            var attr = TryGetAttribute(key);
            if (attr == null) return false;
            attr.RemoveAllModifiers();
            return true;
        }

        /// <summary>
        /// 返回容器内所有属性键名（副本）
        /// </summary>
        public List<string> GetAllKeys()
        {
            return new List<string>(attributes.Keys);
        }

        /// <summary>
        /// 检查属性是否存在
        /// </summary>
        public bool HasAttribute(string key) => attributes.ContainsKey(key);
    }
}