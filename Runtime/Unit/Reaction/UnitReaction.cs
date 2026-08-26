using System;
using System.Collections.Generic;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 被动反应基类。
    /// Reaction 是 Intent 的处理者：读取 Intent + Attribute + Mark，产出 Effect。
    /// Reaction 不再监听全局事件，只响应投递到 Unit 的 Intent。
    /// </summary>
    public abstract class UnitReaction : IDisposable
    {
        /// <summary>反应的唯一标识（通常与监听的事件或业务逻辑相关）</summary>
        public abstract UnitTag Name { get; }

        /// <summary>
        /// 决定在处理 Intent 时的调用先后顺序。值越大，优先级越高。
        /// 高优先级反应先于低优先级反应执行。
        /// </summary>
        public virtual int Priority => 0;

        /// <summary>
        /// 此 Reaction 能处理的 Intent 类型 Tag 列表。
        /// 默认只处理 Name 相同的 Intent。子类重写以指定更多类型。
        /// </summary>
        public virtual UnitTag[] CanHandleTypes => new[] { Name };

        /// <summary>反应挂载的宿主单位（由 Container 注入）</summary>
        public IUnit Owner { get; private set; }

        /// <summary>反应是否处于激活监听状态</summary>
        public bool IsActive { get; protected set; }

        /// <summary>无参构造，满足反序列化工厂要求</summary>
        public UnitReaction() { }

        /// <summary>
        /// 由 ReactionContainer 在挂载瞬间调用，注入宿主引用。
        /// </summary>
        internal void Init(IUnit owner)
        {
            Owner = owner;
        }

        /// <summary>激活反应，开始处理 Intent</summary>
        public virtual void Activate()
        {
            IsActive = true;
        }

        /// <summary>停用反应，停止处理 Intent</summary>
        public virtual void Deactivate()
        {
            IsActive = false;
        }

        /// <summary>
        /// 检查此 Reaction 是否能够处理给定的 Intent。
        /// 默认按 CanHandleTypes 数组匹配 intent.Type，子类可重写自定义过滤。
        /// </summary>
        public virtual bool CanHandle(UnitIntent intent)
        {
            var types = CanHandleTypes;
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] == intent.Type) return true;
            }
            return false;
        }

        /// <summary>
        /// 处理 Intent，产出 Effect。
        /// 将生成的 Effect 追加到 effects 列表中，由 ReactionContainer 统一 Apply。
        /// </summary>
        public abstract void Handle(UnitIntent intent, IList<UnitEffect> effects);

        /// <summary>释放反应资源，彻底断开与宿主的联系</summary>
        public virtual void Dispose()
        {
            Deactivate();
            Owner = null;
        }
    }
}
