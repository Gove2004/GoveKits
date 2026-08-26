using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 核心技能基类。
    /// 标准生命周期：创建实例 -> Container 注入 Owner -> CanExecute -> TryExecuteAsync -> GenerateIntents -> Reaction 处理 -> Effect 落地。
    /// Ability 只负责产生 Intent，不直接修改 Unit 状态。
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
            if (IsExecuting) return false;

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
            if (!CanExecute(context)) return false;

            IsExecuting = true;
            try
            {
                // 1. 提交前置消耗（如：扣除法力值，给 Source 挂上 CD 标记）
                int count = _rules.Count;
                for (int i = 0; i < count; i++)
                {
                    _rules[i]?.Commit(context);
                }

                // 2. 生成 Intent 列表，逐个投递给目标 Unit
                IReadOnlyList<UnitIntent> intents = await GenerateIntentsAsync(context, cancellationToken);
                if (intents != null)
                {
                    foreach (var intent in intents)
                    {
                        if (intent == null) continue;
                        intent.Source = context.Source;
                        intent.Target = context.Target;
                        context.Target?.HandleIntent(intent);
                    }
                }

                return true;
            }
            finally
            {
                IsExecuting = false;
            }
        }

        /// <summary>
        /// 由子类实现，返回要产出的 Intent 列表。返回 null 或空列表表示不产生任何 Intent。
        /// </summary>
        protected abstract UniTask<IReadOnlyList<UnitIntent>> GenerateIntentsAsync(AbilityContext context, CancellationToken cancellationToken = default);

        #endregion

        /// <summary>释放技能资源</summary>
        public virtual void Dispose()
        {
            Owner = null;
            IsExecuting = false;
            _pendingDestroy = false;
            _rules.Clear();
        }
    }
}
