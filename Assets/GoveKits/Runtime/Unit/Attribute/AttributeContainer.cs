using System.Collections.Generic;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 属性容器。
    /// 统一管理 StateAttribute / RuntimeAttribute 的创建、查询和修改。
    /// </summary>
    public class AttributeContainer : IUnitTagSource
    {
        private readonly Dictionary<UnitTag, UnitAttribute> _attributes = new();

        /// <summary>
        /// 判断容器中是否存在指定标签对应的属性。
        /// 该实现用于 TagQuery 的 HasTag 匹配。
        /// </summary>
        /// <param name="tag">待检查标签。</param>
        /// <returns>存在则返回 true，否则返回 false。</returns>
        public bool HasTag(UnitTag tag) => _attributes.ContainsKey(tag);


        /// <summary>
        /// 获取指定属性的当前值，不存在时返回 0。
        /// </summary>
        public float GetValue(UnitTag name)
        {
            if (_attributes.TryGetValue(name, out var attr))
            {
                return attr.Value;
            }
            return 0f;
        }

        /// <summary>
        /// 清空容器，并释放全部属性对象上的事件订阅。
        /// </summary>
        public void Clear()
        {
            foreach (var attr in _attributes.Values)
            {
                attr.Dispose();
            }
            _attributes.Clear();
        }

        #region State Attribute

        /// <summary>
        /// 创建并注册一个状态属性。
        /// </summary>
        /// <param name="name">属性名。</param>
        /// <param name="baseValue">基础值。</param>
        /// <param name="minLimit">最小限制。</param>
        /// <param name="maxLimit">最大限制。</param>
        /// <returns>创建后的状态属性。</returns>
        public StateAttribute AddState(UnitTag name, float baseValue = 0f, float minLimit = 0f, float maxLimit = float.MaxValue)
        {
            var attr = new StateAttribute(name, baseValue, minLimit, maxLimit);
            _attributes[name] = attr;
            return attr;
        }

        /// <summary>
        /// 直接注册已有状态属性实例。
        /// </summary>
        /// <param name="template">要注册的状态属性。</param>
        /// <returns>注册后的实例（即 template 本身）。</returns>
        public StateAttribute AddState(StateAttribute template)
        {
            _attributes[template.Name] = template;
            return template;
        }

        /// <summary>
        /// 获取状态属性。
        /// </summary>
        /// <param name="name">属性名。</param>
        /// <returns>存在则返回状态属性，否则返回 null。</returns>
        public StateAttribute GetState(UnitTag name)
        {
            if (_attributes.TryGetValue(name, out var attr) && attr is StateAttribute stateAttr)
            {
                return stateAttr;
            }
            return null;
        }

        /// <summary>
        /// 对指定状态属性应用修改器。
        /// </summary>
        /// <param name="name">状态属性名。</param>
        /// <param name="modifier">修改器。</param>
        public void ApplyModifier(UnitTag name, AttributeModifier modifier)
        {
            var stateAttr = GetState(name);
            if (stateAttr != null)
            {
                stateAttr.AddModifier(modifier);
            }
        }

        /// <summary>
        /// 从指定状态属性移除修改器。
        /// </summary>
        /// <param name="name">状态属性名。</param>
        /// <param name="modifier">待移除修改器。</param>
        public void RemoveModifier(UnitTag name, AttributeModifier modifier)
        {
            var stateAttr = GetState(name);
            if (stateAttr != null)
            {
                stateAttr.RemoveModifier(modifier);
            }
        }

        #endregion

        #region Runtime Attribute

        /// <summary>
        /// 创建并注册运行时属性。
        /// </summary>
        /// <param name="name">运行时属性名。</param>
        /// <param name="sourceAttr">对应上限状态属性。</param>
        /// <returns>创建后的运行时属性。</returns>
        public RuntimeAttribute AddRuntime(UnitTag name, StateAttribute sourceAttr)
        {
            var attr = new RuntimeAttribute(name, sourceAttr);
            _attributes[name] = attr;
            return attr;
        }

        /// <summary>
        /// 获取运行时属性。
        /// </summary>
        /// <param name="name">属性名。</param>
        /// <returns>存在则返回运行时属性，否则返回 null。</returns>
        public RuntimeAttribute GetRuntime(UnitTag name)
        {
            if (_attributes.TryGetValue(name, out var attr) && attr is RuntimeAttribute runtimeAttr)
            {
                return runtimeAttr;
            }
            return null;
        }

        /// <summary>
        /// 对指定运行时属性应用增量变化。
        /// </summary>
        /// <param name="name">运行时属性名。</param>
        /// <param name="delta">变化量（正数恢复，负数扣减）。</param>
        public void ApplyChange(UnitTag name, float delta)
        {
            var runtimeAttr = GetRuntime(name);
            if (runtimeAttr != null)
            {
                runtimeAttr.Change(delta);
            }
        }

        #endregion
    }
}