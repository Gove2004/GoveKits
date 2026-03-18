


using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 技能容器。
    /// </summary>
    /// <remarks>
    /// 负责技能实例的注册、移除、查询与统一执行入口。
    /// </remarks>
    public class AbilityContainer : IUnitTagSource, IEnumerable<KeyValuePair<UnitTag, UnitAbility>>
    {
        private readonly Dictionary<UnitTag, UnitAbility> _abilitys = new();
        public bool HasTag(UnitTag tag) => _abilitys.ContainsKey(tag);
        public int Count => _abilitys.Count;
        public IEnumerator<KeyValuePair<UnitTag, UnitAbility>> GetEnumerator() => _abilitys.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _abilitys.GetEnumerator();

        /// <summary>
        /// 添加技能并绑定归属 Unit。
        /// </summary>
        /// <param name="ability">技能实例。</param>
        public void AddAbility(UnitAbility ability)
        {
            if (ability == null) return;

            if (_abilitys.TryGetValue(ability.Name, out var oldAbility))
            {
                oldAbility.Dispose();
            }

            _abilitys[ability.Name] = ability;
        }

        /// <summary>
        /// 移除并释放技能。
        /// </summary>
        public bool RemoveAbility(UnitTag tag)
        {
            if (_abilitys.TryGetValue(tag, out var ability))
            {
                ability.Dispose();
                _abilitys.Remove(tag);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取技能实例。
        /// </summary>
        public T GetAbility<T>(UnitTag tag) where T : UnitAbility
        {
            return _abilitys.TryGetValue(tag, out var ability) ? ability as T : null;
        }

        /// <summary>
        /// 按名称尝试异步执行技能。
        /// </summary>
        public UniTask<bool> TryExecuteAsync(UnitTag tag, UnitContext context)
        {
            if (!_abilitys.TryGetValue(tag, out var ability))
            {
                return UniTask.FromResult(false);
            }

            return ability.TryExecuteAsync(context);
        }

        /// <summary>
        /// 清空并释放全部技能。
        /// </summary>
        public void Clear()
        {
            foreach (var ability in _abilitys.Values)
            {
                ability.Dispose();
            }

            _abilitys.Clear();
        }
    }
}