using System;
using System.Collections.Generic;


namespace GoveKits.Attribute
{
    /// <summary>
    /// 属性类
    /// </summary>
    /// <typeparam name="T">一般为int和float</typeparam>
    public class Attribute<T> where T : struct
    {
        public T baseValue { get; set; } // 基础值
        public T cacheValue { get; set; } // 当前值
        private bool isDirty = true; // 脏标记
        private List<AttributeModifier<T>> modifiers; // 修正器列表
        private List<Attribute<T>> denpendents; // 依赖的属性列表
        public Action<T> OnValueChanged; // 值变化时的回调

        public Attribute(T baseValue)
        {
            this.baseValue = baseValue;
            this.cacheValue = baseValue;
            modifiers = new List<AttributeModifier<T>>();
        }

        /// <summary>
        /// 当前值，自动计算
        /// </summary>
        public T Value
        {
            get
            {
                if (isDirty)
                {
                    RecalculateValue();
                    isDirty = false;
                }
                return cacheValue;
            }
            private set
            {
                cacheValue = value;
            }
        }

        /// <summary>
        /// 添加修正器
        /// </summary>
        /// <param name="modifier"></param>
        public void AddModifier(AttributeModifier<T> modifier)
        {
            modifiers.Add(modifier);
            isDirty = true;
        }

        /// <summary>
        /// 移除修正器
        /// </summary>
        /// <param name="modifier"></param>
        public void RemoveModifier(AttributeModifier<T> modifier)
        {
            if (modifiers.Contains(modifier))
            {
                modifiers.Remove(modifier);
                isDirty = true;
            }
        }


        /// <summary>
        /// 重新计算当前值
        /// </summary>
        private void RecalculateValue()
        {
            cacheValue = baseValue;
            modifiers.Sort((a, b) => b.priority.CompareTo(a.priority)); // 按优先级排序
            foreach (var modifier in modifiers)
            {
                cacheValue = modifier.Apply(cacheValue);
            }
            OnValueChanged?.Invoke(cacheValue);  // 触发值变化回调
        }
    }
    
}