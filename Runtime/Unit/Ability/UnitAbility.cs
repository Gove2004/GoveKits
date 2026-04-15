using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 核心技能基类。
    /// </summary>
    /// <remarks>
    /// 标准生命周期：创建实例 -> Container 注入 Owner -> CanExecute -> TryExecuteAsync -> ExecuteAsync -> Dispose。
    /// 支持动态添加前置条件规则（Rule），实现如耗蓝、冷却等通用拦截逻辑。
    /// </remarks>
    public abstract class UnitAbility : System.IDisposable
    {
        /// <summary>
        /// 技能唯一标识（如 "Skill_Fireball"）。
        /// 强烈建议在子类中使用静态只读常量定义。
        /// </summary>
        public abstract UnitTag Name { get; }

        /// <summary>
        /// 技能归属的宿主单位（由 Container 在添加时注入）。
        /// </summary>
        public IUnit Owner { get; private set; }

        /// <summary>
        /// 标记当前技能是否正在执行阶段（防重入保护）。
        /// </summary>
        public bool IsExecuting { get; private set; }

        // 存储所有的前置检查与消耗提交规则
        private readonly List<AbilityRule> _rules = new();

        /// <summary>
        /// 无参构造函数，强制要求子类保留无参构造能力，以支持反射与序列化工场。
        /// </summary>
        public UnitAbility() { }

        /// <summary>
        /// 由 AbilityContainer 在 AddAbility 时统一调用。
        /// 将宿主依赖注入到技能中。
        /// </summary>
        /// <param name="owner">技能归属单位</param>
        internal void Init(IUnit owner)
        {
            Owner = owner;
            OnInit();
        }

        /// <summary>
        /// 供子类重写的初始化钩子。
        /// 通常在此处使用 AddRule 绑定技能的专属消耗与冷却规则。
        /// </summary>
        protected virtual void OnInit() { }

        #region 规则管理

        /// <summary>
        /// 添加一个技能执行规则（如 CD、MP 消耗）。
        /// </summary>
        /// <returns>当前技能实例，便于链式调用。</returns>
        public UnitAbility AddRule(AbilityRule rule)
        {
            if (rule != null)
            {
                _rules.Add(rule);
            }
            return this;
        }

        public bool RemoveRule(AbilityRule rule)
        {
            return rule != null && _rules.Remove(rule);
        }

        public void ClearRules()
        {
            _rules.Clear();
        }

        #endregion

        #region 执行状态机

        /// <summary>
        /// 检查技能是否允许执行。
        /// </summary>
        /// <param name="context">包含施法者与目标的执行上下文。</param>
        /// <returns>若有任意一条 Rule.Check() 不通过，或技能正在执行中，则返回 false。</returns>
        public virtual bool CanExecute(AbilityContext context)
        {
            if (IsExecuting) return false;

            // 前置条件检查，任何一个不满足都无法执行
            var rules = _rules;
            int count = rules.Count;
            for (int i = 0; i < count; i++)
            {
                var rule = rules[i];
                if (rule != null && !rule.Check(context))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 核心流程入口：尝试异步执行技能。
        /// </summary>
        /// <param name="context">执行上下文。</param>
        /// <param name="cancellationToken">异步取消令牌。</param>
        /// <returns>由于条件不足导致未能执行返回 false，成功跑完释放流返回 true。</returns>
        public async UniTask<bool> TryExecuteAsync(AbilityContext context, CancellationToken cancellationToken = default)
        {
            if (!CanExecute(context)) return false;

            IsExecuting = true;
            try
            {
                // 1. 提交前置条件（如：扣除法力值，给 Source 挂上 CD 标记）
                for (int i = 0; i < _rules.Count; i++)
                {
                    var rule = _rules[i];
                    rule?.Commit(context);
                }
                
                // 2. 将控制权移交给具体的业务子类逻辑
                await ExecuteAsync(context, cancellationToken);
                return true;
            }
            finally
            {
                // 3. 无论执行正常结束还是被 Cancel 异常打断，都必须释放执行锁
                IsExecuting = false;
            }
        }

        /// <summary>
        /// 异步执行技能的核心业务逻辑（释放表现、伤害判定等）。
        /// 子类必须实现。
        /// </summary>
        public abstract UniTask ExecuteAsync(AbilityContext context, CancellationToken cancellationToken = default);

        #endregion

        /// <summary>
        /// 释放技能资源。
        /// </summary>
        public virtual void Dispose()
        {
            Owner = null;
            IsExecuting = false;
            _rules.Clear();
        }
    }
}