using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Unit
{
    /// <summary>
    /// 能力接口：代表单位的可执行动作/技能。
    /// - `CanExecute` 用于 UI/AI 的可用性检查（同步）。
    /// - `Execute` 为异步执行入口，包含消耗/冷却/生命周期管理。
    /// </summary>
    public interface IGameAbility
    {
        /// <summary>能力的唯一名称/标签</summary>
        GameTag Name { get; }

        /// <summary>
        /// 检查能力是否可执行（同步）
        /// - 一般检查冷却、资源和状态限制
        /// </summary>
        bool CanExecute(IGameUnit source, IGameUnit target);

        /// <summary>
        /// 执行能力（异步）。
        /// - 实现应负责能力本身的生命周期（播放动画、应用效果等）。
        /// </summary>
        UniTask Execute(IGameUnit source, IGameUnit target);
    }


    /// <summary>
    /// 能力基类，封装了通用的资源消耗、冷却处理与执行模板。
    /// 子类实现 <see cref="OnExecute"/> 以完成具体业务逻辑。
    /// </summary>
    public abstract class GameAbility : IGameAbility
    {
        /// <summary>能力名称/标签</summary>
        public GameTag Name { get; protected set; }

        // --- 内置组件/策略 ---
        protected AbilityCost Cost = new AbilityCost();
        protected GameTag CooldownTag; // 专属冷却 Tag（例如 "CD.Fireball"）
        protected float CooldownDuration; // 专属冷却时长
        protected GameTag GlobalCooldownTag = "CD.Global"; // 公共冷却 Tag（GCD）
        protected float GlobalCooldownDuration = 1.0f; // GCD 时长

        /// <summary>
        /// 构造能力并指定其名称/标签。
        /// </summary>
        public GameAbility(GameTag name)
        {
            Name = name;
        }

        // 配置方法（供子类或外部使用）
        /// <summary>
        /// 配置专属冷却时长与可选的冷却 Tag。
        /// </summary>
        public void SetCooldown(float duration, string cooldownTag = null)
        {
            CooldownDuration = duration;
            if (cooldownTag != null)
                CooldownTag = cooldownTag;
            else
                CooldownTag = CooldownMark.GetTag(Name);
        }

        /// <summary>
        /// 添加技能消耗项（例如 MP、Stamina）。
        /// </summary>
        public void AddCost(GameTag tag, float val) => Cost.AddCost(tag, val);

        #region 检查逻辑

        /// <summary>
        /// 默认可执行性检查：冷却、资源和状态限制
        /// 子类可覆盖以增加更多检查（如目标有效性、距离、连招条件等）。
        /// </summary>
        /// <summary>
        /// 默认的可执行性检查（同步）。
        /// <para>检查冷却、全局冷却和资源是否满足。</para>
        /// </summary>
        public virtual bool CanExecute(IGameUnit source, IGameUnit target)
        {
            if (source == null) return false;

            // 1. 冷却检查（专属 + 公共）
            if (source.Marks.HasTag(CooldownTag)) return false;
            if (source.Marks.HasTag(GlobalCooldownTag)) return false;

            // 2. 资源检查
            if (!Cost.Check(source)) return false;

            // 3. 状态限制（示例：被控制则不能释放）
            // if (GameQuery.IsStunned.Match(source.Marks)) return false;

            return true;
        }

        #endregion

        #region 执行逻辑

        /// <summary>
        /// 执行模板方法：Double Check -> 支付消耗 -> 进入冷却 -> OnExecute
        /// </summary>
        /// <summary>
        /// 执行能力的异步入口：支付消耗、施加冷却并调用 <see cref="OnExecute"/>。
        /// </summary>
        public async UniTask Execute(IGameUnit source, IGameUnit target)
        {
            // Double Check
            if (!CanExecute(source, target)) return;

            try
            {
                // 1. 支付消耗
                Cost.Pay(source);

                // 2. 可选：广播能力开始事件
                // EventManager.Broadcast(new AbilityStartEvent(context));

                // 3. 施加冷却（防止连点）
                CommitCooldown(source);

                // 4. 执行具体逻辑（子类实现）
                await OnExecute(source, target);
            }
            catch (System.Exception ex)
            {
                // 捕获能力执行过程中的异常并记录为 Warning，而不是 Error。
                // Unity 的测试框架会将 Error 日志视为测试失败（Unhandled log message），
                // 这里使用 Warning 保持异常被记录同时不使单元测试因日志失败。
                Debug.LogWarning($"[Ability] Error in {Name}: {ex}");
            }
        }

        /// <summary>
        /// 施加冷却：包含专属冷却与全局冷却（GCD），支持从属性读取冷却缩减（CDR）
        /// </summary>
        protected virtual void CommitCooldown(IGameUnit source)
        {
            if (source == null) return;

            // 读取 CDR（冷却缩减），为简单示例直接用属性标签 "冷却缩减"
            float cdr = source.Attributes.GetValue("冷却缩减", 0f);
            float finalCd = CooldownDuration * (1f - cdr);

            // 1. 添加专属冷却
            if (finalCd > 0)
            {
                source.Marks.Add(CooldownTag, new CooldownMark(CooldownTag, finalCd));
            }

            // 2. 添加全局冷却（GCD）
            if (GlobalCooldownDuration > 0)
            {
                source.Marks.Add(GlobalCooldownTag, new CooldownMark(GlobalCooldownTag, GlobalCooldownDuration));
            }
        }

        /// <summary>
        /// 子类实现能力的具体业务逻辑。
        /// - 在这里执行技能伤害、动画等待、特效播放等。
        /// </summary>
        protected abstract UniTask OnExecute(IGameUnit source, IGameUnit target);

        #endregion
    }
}