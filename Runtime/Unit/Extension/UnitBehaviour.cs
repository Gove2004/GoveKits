using GoveKits.Runtime.Util;
using UnityEngine;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 基于 MonoBehaviour 的实体表现层载体。
    /// 涵盖模型展示、动画播放，以及底层的四大容器。
    /// 所有游戏实体的技能系统均继承此类实现。
    /// </summary>
    public abstract class UnitBehaviour : MonoBehaviour, IUnit
    {
        /// <summary>属性容器</summary>
        public AttributeContainer Attributes { get; protected set; }
        /// <summary>标记容器</summary>
        public MarkContainer Marks { get; protected set; }
        /// <summary>技能容器</summary>
        public AbilityContainer Abilities { get; protected set; }
        /// <summary>反应容器</summary>
        public ReactionContainer Reactions { get; protected set; }

        /// <summary>
        /// MonoBehavior 生命周期：初始化全部四大容器。
        /// </summary>
        protected virtual void Awake()
        {
            this.InitAllContainers();
        }

        /// <summary>初始化属性容器（可被子类覆写以自定义）</summary>
        public virtual void InitAttributes() => Attributes = new AttributeContainer(this);
        /// <summary>初始化标记容器（可被子类覆写以自定义）</summary>
        public virtual void InitMarks() => Marks = new MarkContainer(this);
        /// <summary>初始化技能容器（可被子类覆写以自定义）</summary>
        public virtual void InitAbilities() => Abilities = new AbilityContainer(this);
        /// <summary>初始化反应容器（可被子类覆写以自定义）</summary>
        public virtual void InitReactions() => Reactions = new ReactionContainer(this);

        /// <summary>
        /// MonoBehavior 生命周期：每帧驱动标记容器的 Tick 逻辑（容器未初始化时安全跳过）。
        /// </summary>
        protected virtual void Update()
        {
            this.UpdateUnit(Time.deltaTime);
        }

        /// <summary>清理全部容器状态</summary>
        public void Clear()
        {
            this.ClearAllContainers();
        }


        /// <summary>
        /// MonoBehavior 生命周期：销毁时清理全部容器。
        /// </summary>
        protected virtual void OnDestroy()
        {
            this.Clear();
        }
    }
}
