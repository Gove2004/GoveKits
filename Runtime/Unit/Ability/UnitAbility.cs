using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Util;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 核心技能基类。
    /// 标准生命周期：创建实例 → Container 注入 Owner（触发 OnInit）→ CanExecute 规则检查 → TryExecuteAsync → ExecuteAsync → Dispose。
    ///
    /// 用法：
    /// <code>
    /// public class FireBallAbility : UnitAbility
    /// {
    ///     public override UnitTag Name => "Skill_FireBall";
    ///     protected override void OnInit() => AddRule(new CDRule("CD.FireBall", 3f));   // 3 秒冷却
    ///     public override async UniTask ExecuteAsync(AbilityContext context, CancellationToken ct)
    ///     {
    ///         float damage = Owner.Attributes.GetValue("atk") * 2f;
    ///         AttributeChangeEffect.Create().Set("hp", -damage).Apply(context.Target);  // 直接结算
    ///         EventCore.Pick&lt;DamageEvent&gt;()... 并 Publish，交给被动反应（格挡/反伤等）处理
    ///     }
    /// }
    /// await unit.Abilities.TryExecuteAsync("Skill_FireBall", new AbilityContext(caster, target));
    /// </code>
    /// 技能内置防重入锁，执行期间再次调用会直接返回 false。
    /// </summary>
    public abstract class UnitAbility : System.IDisposable
    {
        /// <summary>技能唯一标识（如 "Skill_Fireball"）。强烈建议在子类中使用静态只读常量定义</summary>
        public abstract UnitTag Name { get; }

        /// <summary>技能归属的宿主单位（由 Container 在添加时注入）</summary>
        public IUnit Owner { get; private set; }

        /// <summary>标记当前技能是否正在执行阶段（防重入保护）</summary>
        public bool IsExecuting { get; private set; }

        private readonly List<AbilityRule> _rules = new();
        private bool _pendingDestroy;
        private bool _disposed;

        /// <summary>技能是否已释放（释放后不可再执行，CanExecute 会拦截）</summary>
        public bool IsDisposed => _disposed;

        /// <summary>标记技能待销毁（正在执行中时无法安全 Dispose）</summary>
        internal void MarkForPendingDestroy() => _pendingDestroy = true;

        /// <summary>
        /// 无参构造函数，强制要求子类保留无参构造能力，以支持反射与序列化工场。
        /// </summary>
        public UnitAbility() { }

        /// <summary>
        /// 由 AbilityContainer 在 AddAbility 时统一调用，注入宿主和日志依赖。
        /// </summary>
        internal void Init(IUnit owner)
        {
            Owner = owner;
            OnInit();
        }

        /// <summary>供子类重写的初始化钩子。通常在此处使用 AddRule 绑定技能的专属消耗与冷却规则</summary>
        protected virtual void OnInit() { }

        #region 规则管理

        /// <summary>添加一条技能前置规则</summary>
        public UnitAbility AddRule(AbilityRule rule)
        {
            if (rule != null) _rules.Add(rule);
            return this;
        }

        /// <summary>移除指定规则</summary>
        public bool RemoveRule(AbilityRule rule)
            => rule != null && _rules.Remove(rule);

        /// <summary>清空所有规则</summary>
        public void ClearRules()
            => _rules.Clear();

        #endregion

        #region 执行状态机

        /// <summary>
        /// 检查技能是否允许执行。若有任意一条 Rule.Check() 不通过，或技能正在执行中，则返回 false。
        /// </summary>
        public virtual bool CanExecute(AbilityContext context)
        {
            if (_disposed || IsExecuting) return false;

            int count = _rules.Count;
            for (int i = 0; i < count; i++)
            {
                var rule = _rules[i];
                if (rule != null && !rule.Check(context))
                    return false;
            }
            return true;
        }

        /// <summary>核心流程入口：尝试异步执行技能</summary>
        public async UniTask<bool> TryExecuteAsync(AbilityContext context, CancellationToken cancellationToken = default)
        {
            // 已释放的技能直接拒绝（不依赖 CanExecute，防止子类重写绕过）
            if (_disposed || !CanExecute(context)) return false;

            IsExecuting = true;
            try
            {
                // 1. 提交前置消耗（如：扣除法力值，给 Source 挂上 CD 标记）
                int count = _rules.Count;
                for (int i = 0; i < count; i++)
                {
                    _rules[i]?.Commit(context);
                }

                // 2. 将控制权移交给具体的业务子类逻辑
                await ExecuteAsync(context, cancellationToken);
                return true;
            }
            finally
            {
                IsExecuting = false;

                // 执行期间被替换（AbilityContainer.AddAbility → MarkForPendingDestroy）时，
                // 待执行结束才真正释放，避免在技能执行过程中销毁自身。
                if (_pendingDestroy)
                {
                    _pendingDestroy = false;
                    Dispose();
                }
            }
        }

        /// <summary>
        /// 由子类实现：技能的核心业务逻辑。
        /// 典型实现是播放表现 + 直接产出效果（Effect），或发布事件（EventCore.Publish）交给被动反应处理。
        /// </summary>
        public abstract UniTask ExecuteAsync(AbilityContext context, CancellationToken cancellationToken = default);

        #endregion

        /// <summary>释放技能资源（幂等，重复调用无副作用）</summary>
        public virtual void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Owner = null;
            IsExecuting = false;
            _pendingDestroy = false;
            _rules.Clear();
        }
    }
}
