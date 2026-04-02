using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 技能容器。
    /// </summary>
    /// <remarks>
    /// 负责技能实例的注册、移除、查询与统一执行入口。
    /// </remarks>
    public class AbilityContainer : ITagSource, IEnumerable<KeyValuePair<UnitTag, UnitAbility>>
    {
        // 存储技能的字典，以技能名称为键，技能实例为值
        private readonly Dictionary<UnitTag, UnitAbility> _abilitys = new();
        
        /// <summary>
        /// 检查是否包含指定标签的技能
        /// </summary>
        public bool HasTag(UnitTag tag) => _abilitys.ContainsKey(tag);
        
        /// <summary>
        /// 技能数量
        /// </summary>
        public int Count => _abilitys.Count;
        
        /// <summary>
        /// 获取枚举器，支持foreach遍历
        /// </summary>
        public IEnumerator<KeyValuePair<UnitTag, UnitAbility>> GetEnumerator() => _abilitys.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _abilitys.GetEnumerator();

        /// <summary>
        /// 添加技能并绑定归属 Unit。
        /// </summary>
        /// <param name="ability">技能实例。</param>
        public void AddAbility(UnitAbility ability)
        {
            if (ability == null) return;

            // 如果已有同名技能，先释放旧技能
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
        /// 获取指定标签的技能实例
        /// </summary>
        /// <typeparam name="T">期望的技能类型</typeparam>
        /// <param name="tag">技能标签</param>
        /// <returns>技能实例，如果不存在或类型不匹配则返回null</returns>
        public T GetAbility<T>(UnitTag tag) where T : UnitAbility
        {
            if (_abilitys.TryGetValue(tag, out var ability))
            {
                if (ability is T result) return result;
                LogCore.Error(nameof(AbilityContainer), $"技能 {tag} 类型不匹配，期望 {typeof(T).Name}");
            }
            return null;
        }

        /// <summary>
        /// 按名称尝试异步执行技能。
        /// </summary>
        public UniTask<bool> TryExecuteAsync(UnitTag tag, AbilityContext context, CancellationToken cancellationToken = default)
        {
            if (!_abilitys.TryGetValue(tag, out var ability))
            {
                return UniTask.FromResult(false);
            }

            return ability.TryExecuteAsync(context, cancellationToken);
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