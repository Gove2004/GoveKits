
using System;
using System.Collections.Generic;


namespace GoveKits.Attribute
{
    /// <summary>
    /// 属性类，支持基础值和依赖值计算
    /// </summary>
    public class Attribute
    {
        private float baseValue; // 基础值
        private float cacheValue; // 当前值
        private bool isDirty; // 脏标记
        private List<AttributeModifier> modifiers; // 修正器列表
        private List<Attribute> dependents; // 依赖的属性列表
        public event Action<float> onValueChanged; // 值变化时的回调

        public Attribute(float value)
        {
            baseValue = value;
            cacheValue = value;
            isDirty = false;
            modifiers = new List<AttributeModifier>();
            dependents = new List<Attribute>();
            onValueChanged = null;
        }

        /// <summary>
        /// 基础值，可设置。设置后会标记为脏，下一次读取或强制计算时会重新计算值。
        /// </summary>
        public float Base
        {
            get => baseValue;
            set
            {
                if (baseValue != value)
                {
                    baseValue = value;
                    isDirty = true;
                }
            }
        }

        /// <summary>
        /// 当前值，自动计算
        /// </summary>
        public float Value
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
        /// 重新计算当前值
        /// </summary>
        private void RecalculateValue()
        {
            // 计算新值，但不要每次都对列表排序（在添加/移除时维护已排序状态）
            float oldValue = cacheValue;
            float newValue = baseValue;
            foreach (var modifier in modifiers)
            {
                newValue = modifier.Apply(newValue);
            }

            // 仅在实际变化时触发回调并通知依赖
            if (System.Math.Abs(oldValue - newValue) > 1e-6f)
            {
                cacheValue = newValue;
                onValueChanged?.Invoke(cacheValue);
                foreach (var dependent in dependents)
                {
                    dependent.isDirty = true;
                }
            }
        }

        /// <summary>
        /// 强制立即重新计算（如果需要）。
        /// </summary>
        public void RecalculateImmediately()
        {
            if (isDirty)
            {
                RecalculateValue();
                isDirty = false;
            }
        }

        /// <summary>
        /// 添加修正器
        /// </summary>
        /// <param name="modifier"></param>
        public void AddModifier(AttributeModifier modifier)
        {
            if (modifier == null) return;
            modifiers.Add(modifier);
            // 在修改器变动时维护有序状态，避免在每次计算时排序
            modifiers.Sort((a, b) => b.priority.CompareTo(a.priority)); // 按优先级排序, 从大到小
            isDirty = true;
        }

        /// <summary>
        /// 移除修正器
        /// </summary>
        /// <param name="modifier"></param>
        public void RemoveModifier(AttributeModifier modifier)
        {
            if (modifiers.Contains(modifier))
            {
                modifiers.Remove(modifier);
                // 保持顺序（虽然 Remove 不需要排序，但保持一致）
                modifiers.Sort((a, b) => b.priority.CompareTo(a.priority));
                isDirty = true;
            }
        }

        /// <summary>
        /// 清除所有修正器并标记为脏。
        /// </summary>
        public void RemoveAllModifiers()
        {
            if (modifiers.Count == 0) return;
            modifiers.Clear();
            isDirty = true;
        }

        /// <summary>
        /// 添加依赖属性
        /// </summary>
        /// <param name="attribute"></param>
        public void AddDependent(Attribute attribute)
        {
            if (!dependents.Contains(attribute))
            {
                dependents.Add(attribute);
            }
        }

        /// <summary>
        /// 移除依赖属性
        /// </summary>
        public void RemoveDependent(Attribute attribute)
        {
            if (dependents.Contains(attribute))
            {
                dependents.Remove(attribute);
            }
        }



    }
    



}