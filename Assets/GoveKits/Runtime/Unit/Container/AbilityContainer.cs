


using System;
using Cysharp.Threading.Tasks;

namespace GoveKits.Unit
{
    /// <summary>
    /// 能力容器类，用于管理单位的能力。
    /// </summary>
    public class AbilityContainer : DictionaryContainer<IGameAbility>
    {
        // 容器必须知道它的主人是谁，以便传给 Ability
        private readonly IGameUnit _owner;

        /// <summary>
        /// 构造能力容器并绑定所属单位。
        /// </summary>
        public AbilityContainer(IGameUnit owner) => _owner = owner;


        /// <summary>
        /// 尝试使用指定 tag 的能力（非阻塞）。
        /// - 如果能力存在，立即以容器的 owner 作为执行者调用 Execute，并返回 true。
        /// - 如果能力不存在，返回 false（不会抛出异常）。
        /// </summary>
        public bool TryUseAbility<T>(GameTag tag, IGameUnit target) where T : IGameAbility
        {
            if (TryGet(tag, out var ability))
            {
                // 先检查是否能执行（蓝量、冷却、控制状态）
                if (ability.CanExecute(_owner, target))
                {
                    // 只有能执行，才真正运行，并告诉 AI "我行动了"
                    ability.Execute(_owner, target).Forget();
                    return true;
                }
                return false;
            }
            return false;
        }
    }
}