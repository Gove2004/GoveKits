

using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 技能基类。
    /// </summary>
    /// <remarks>
    /// 标准生命周期：Owner 绑定 -> CanExecute/TryExecuteAsync/ExecuteAsync -> Dispose。
    /// </remarks>
    public abstract class UnitAbility : System.IDisposable
    {
        /// <summary>
        /// 技能唯一标识。
        /// </summary>
        public abstract UnitTag Name { get; }

        /// <summary>
        /// 技能归属的 Unit。
        /// </summary>
        public IUnit Owner { get; private set; }

        /// <summary>
        /// 当前技能是否正在执行。
        /// </summary>
        public bool IsExecuting { get; private set; }

        /// <summary>
        /// 技能前置条件集合。
        /// </summary>
        private readonly List<AbilityRule> _rules = new();

        public UnitAbility(IUnit owner)
        {
            Owner = owner;
        }

        /// <summary>
        /// 检查技能是否允许执行。
        /// </summary>
        /// <param name="context">执行上下文。</param>
        public virtual bool CanExecute(UnitContext context)
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
        /// 尝试异步执行技能。
        /// </summary>
        /// <param name="context">执行上下文。</param>
        /// <returns>执行成功返回 true，否则返回 false。</returns>
        public async UniTask<bool> TryExecuteAsync(UnitContext context)
        {
            if (!CanExecute(context)) return false;

            IsExecuting = true;
            try
            {
                // 提交前置条件，若有任何提交失败则执行回滚并终止
                for (int i = 0; i < _rules.Count; i++)
                {
                    var rule = _rules[i];
                    rule?.Commit(context);
                }
                
                await ExecuteAsync(context);
                return true;
            }
            finally
            {
                IsExecuting = false;
            }
        }

        /// <summary>
        /// 异步执行技能逻辑。
        /// </summary>
        /// <param name="context">执行上下文。</param>
        public abstract UniTask ExecuteAsync(UnitContext context);

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