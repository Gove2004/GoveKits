using System.Collections.Generic;
using GoveKits.Runtime.Core.Pool;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 即时效果基类。
    /// </summary>
    /// <remarks>
    /// 典型用途：伤害、治疗、回蓝、清除状态等一次性生效逻辑。
    /// 生效后会自动回收到对象池。
    /// </remarks>
    public abstract class UnitEffect : IPoolable
    {
        /// <summary>
        /// 执行效果并在结束后自动回池。
        /// </summary>
        /// <param name="target">效果目标 Unit。</param>
        internal void Apply(IUnit target)
        {
            try
            {
                OnApply(target);
            }
            finally
            {
                OnDestroy();
            }
        }

        /// <summary>
        /// 实际效果逻辑。
        /// </summary>
        /// <param name="target">效果目标 Unit。</param>
        public abstract void OnApply(IUnit target);

        /// <summary>
        /// 释放效果资源并回池。
        /// </summary>
        /// <remarks>
        /// 默认实现会将当前效果对象归还到 PoolCore。
        /// 如需改为延迟回收，可在子类中重写该方法。
        /// </remarks>
        public virtual void OnDestroy() => PoolCore.Return(this);

        /// <summary>
        /// 回池前的重置逻辑。
        /// </summary>
        /// <remarks>
        /// 在这里清理临时状态，避免复用对象时脏数据泄漏。
        /// </remarks>
        public abstract void OnRecycle();
    }

    /// <summary>
    /// 带自动工厂能力的 UnitEffect 基类。
    /// </summary>
    /// <typeparam name="TEffect">具体效果类型。</typeparam>
    public abstract class UnitEffect<TEffect> : UnitEffect where TEffect : UnitEffect<TEffect>, new()
    {
        /// <summary>
        /// 从对象池创建当前效果类型。
        /// </summary>
        /// <remarks>
        /// 子类可直接通过 XxxEffect.Get() 获取对象，
        /// 再通过 Set(...) 注入参数，减少 new 分配。
        /// </remarks>
        public static TEffect Get() => PoolCore.Get<TEffect>();
    }

    /// <summary>
    /// 组合效果：一次性执行多个 UnitEffect。
    /// </summary>
    public class MoreEffect : UnitEffect<MoreEffect>
    {
        private readonly List<UnitEffect> _effects = new();

        public MoreEffect()
        {
        }

        public MoreEffect(params UnitEffect[] effects)
        {
            _effects.AddRange(effects);
        }

        public MoreEffect Set(params UnitEffect[] effects)
        {
            _effects.Clear();
            _effects.AddRange(effects);
            return this;
        }

        public MoreEffect Add(UnitEffect effect)
        {
            if (effect != null)
            {
                _effects.Add(effect);
            }
            return this;
        }

        public override void OnApply(IUnit target)
        {
            foreach (var effect in _effects)
            {
                effect.Apply(target);
            }
        }

        public override void OnRecycle()
        {
            _effects.Clear();
        }
    }
}