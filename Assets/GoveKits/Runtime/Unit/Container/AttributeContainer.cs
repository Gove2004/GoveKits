using System;
using System.Collections.Generic;

namespace GoveKits.Unit
{
    /// <summary>
    /// 属性容器
    /// <para>核心职责：管理单位的所有数值属性（计算型 Stat 和 资源型 Runtime）。</para>
    /// <para>特性：</para>
    /// <para>1. 提供工厂方法创建属性，自动处理依赖关系。</para>
    /// <para>2. 封装了属性的读写操作，区分 Modifier 修改和数值直接变更。</para>
    /// <para>3. 实现了资源清理，防止事件监听导致的内存泄漏。</para>
    /// </summary>
    public class AttributeContainer : DictionaryContainer<GameAttribute>
    {
        private readonly IGameUnit _owner;
        public AttributeContainer(IGameUnit owner) => _owner = owner;
        
        #region 工厂方法

        /// <summary>
        /// 创建并添加一个计算型属性 (StateAttribute)
        /// <para>适用：MaxHP, Attack, Defense 等基于公式计算的属性。</para>
        /// </summary>
        /// <param name="tag">属性标签</param>
        /// <param name="baseVal">基础数值</param>
        /// <param name="minLimit">数值下限 (默认0，防御力等可能允许负数)</param>
        /// <returns>创建的属性实例</returns>
        public StateAttribute AddState(GameTag tag, float baseVal, float minLimit = 0)
        {
            var attr = new StateAttribute(tag, baseVal, minLimit);
            base.Add(tag, attr); 
            return attr;
        }

        /// <summary>
        /// 创建并添加一个资源型属性 (RuntimeAttribute)
        /// <para>适用：HP, MP, Stamina 等需要在运行时消耗和恢复的属性。</para>
        /// </summary>
        /// <param name="tag">属性标签</param>
        /// <param name="maxTag">依赖的上限属性标签 (必须已存在)</param>
        /// <returns>创建的属性实例</returns>
        /// <exception cref="Exception">如果依赖的上限属性不存在则抛出异常</exception>
        public RuntimeAttribute AddRuntime(GameTag tag, GameTag maxTag)
        {
            if (!_items.TryGetValue(maxTag, out var maxAttr))
                throw new Exception($"[AttributeContainer] 创建 {tag} 失败：找不到依赖的上限属性 {maxTag}");

            var attr = new RuntimeAttribute(tag, maxAttr);
            base.Add(tag, attr);
            return attr;
        }

        #endregion

        #region 读写操作封装

        /// <summary>
        /// 快捷获取属性的当前最终值
        /// </summary>
        /// <param name="tag">属性标签</param>
        /// <returns>属性值，如果不存在则返回 0</returns>
        public float GetValue(GameTag tag, float defaultValue = 0f)
        {
            return TryGet(tag, out var attr) ? attr.Value : defaultValue;
        }
        
        /// <summary>
        /// [仅资源型] 应用数值变化
        /// <para>适用：造成伤害、治疗、消耗魔法等。</para>
        /// </summary>
        /// <param name="tag">属性标签 (如 HP)</param>
        /// <param name="delta">变化量 (负数为消耗/伤害，正数为恢复)</param>
        public void ApplyRuntimeChange(GameTag tag, float delta)
        {
            if (TryGet(tag, out var attr) && attr is RuntimeAttribute runtime)
            {
                runtime.ApplyChange(delta);
            }
        }

        /// <summary>
        /// [仅计算型] 添加状态修改器
        /// <para>适用：施加 Buff、装备物品、被动技能加成。</para>
        /// </summary>
        /// <param name="tag">属性标签 (如 Attack)</param>
        /// <param name="modifier">修改器详细信息</param>
        public void ApplyStateModifier(GameTag tag, GameModifier modifier)
        {
            if (TryGet(tag, out var attr) && attr is StateAttribute state)
            {
                state.AddModifier(modifier);
            }
        }

        /// <summary>
        /// [仅计算型] 移除特定的状态修改器
        /// </summary>
        public void RemoveStateModifier(GameTag tag, GameModifier modifier)
        {
            if (TryGet(tag, out var attr) && attr is StateAttribute state)
            {
                state.RemoveModifier(modifier);
            }
        }

        /// <summary>
        /// [仅计算型] 按来源移除单个属性下的修改器
        /// <para>适用：Buff 结束时，移除该 Buff 对特定属性的影响。</para>
        /// </summary>
        /// <param name="tag">属性标签</param>
        /// <param name="source">来源对象 (如 Buff 实例)</param>
        public void RemoveStateModifiersBySource(GameTag tag, object source)
        {
            if (TryGet(tag, out var attr) && attr is StateAttribute state)
            {
                state.RemoveBySource(source);
            }
        }

        /// <summary>
        /// [补充] 从所有属性中移除指定来源的修改器
        /// <para>适用：脱下装备或清除 Buff 时，不知道它具体影响了哪些属性，直接一键清理。</para>
        /// </summary>
        /// <param name="source">来源对象</param>
        public void RemoveModifiersFromAllAttributes(object source)
        {
            foreach (var attr in _items.Values)
            {
                if (attr is StateAttribute state)
                {
                    state.RemoveBySource(source);
                }
            }
        }

        #endregion

        #region 资源清理 (核心补充)

        /// <summary>
        /// 重写移除逻辑：确保 RuntimeAttribute 正确断开对 MaxAttribute 的事件监听
        /// </summary>
        public override bool Remove(GameTag key)
        {
            if (_items.TryGetValue(key, out var item))
            {
                // 如果是可销毁的（如 RuntimeAttribute），必须调用 Dispose
                if (item is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            return base.Remove(key);
        }

        /// <summary>
        /// 重写清空逻辑：销毁所有属性的连接，防止内存泄漏
        /// </summary>
        public override void Clear()
        {
            foreach (var item in _items.Values)
            {
                if (item is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            base.Clear();
        }

        #endregion
    }
}