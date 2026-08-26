using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Util;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 技能容器。
    /// 负责技能实例的注册、移除、查询与统一异步执行入口。
    /// 容器接管了所有技能实例的 Owner 依赖注入。
    /// </summary>
    public class AbilityContainer : ITagSource, IEnumerable<KeyValuePair<UnitTag, UnitAbility>>
    {
        /// <summary>技能所属的宿主 Unit</summary>
        public IUnit Owner { get; }
        private readonly Dictionary<UnitTag, UnitAbility> _abilities = new();

        /// <summary>当前已注册的技能数量</summary>
        public int Count => _abilities.Count;

        /// <summary>构造函数</summary>
        public AbilityContainer(IUnit owner)
        {
            Owner = owner;
        }

        /// <summary>检查是否存在指定标签的技能</summary>
        public bool HasTag(UnitTag tag) => _abilities.ContainsKey(tag);

        public IEnumerator<KeyValuePair<UnitTag, UnitAbility>> GetEnumerator() => _abilities.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _abilities.GetEnumerator();

        /// <summary>
        /// 添加技能并自动为其注入宿主。若已存在同名技能，旧技能将被自动销毁替换。
        /// 正在执行中的旧技能会标记为待销毁而非立即释放。
        /// </summary>
        public void AddAbility(UnitAbility ability)
        {
            if (ability == null) return;

            if (_abilities.TryGetValue(ability.Name, out var oldAbility))
            {
                if (oldAbility.IsExecuting)
                {
                    LogCore.Warning(nameof(AbilityContainer), $"技能 {ability.Name} 正在执行中，标记为待销毁");
                    oldAbility.MarkForPendingDestroy();
                }
                else
                {
                    oldAbility.Dispose();
                }
            }

            ability.Init(Owner);
            _abilities[ability.Name] = ability;
        }

        /// <summary>移除指定标签的技能实例</summary>
        public bool RemoveAbility(UnitTag tag)
        {
            if (_abilities.TryGetValue(tag, out var ability))
            {
                ability.Dispose();
                _abilities.Remove(tag);
                return true;
            }
            return false;
        }

        /// <summary>强类型获取指定标签的技能实例</summary>
        public T GetAbility<T>(UnitTag tag) where T : UnitAbility
        {
            if (_abilities.TryGetValue(tag, out var ability))
            {
                if (ability is T result) return result;
                LogCore.Error(nameof(AbilityContainer), $"技能 {tag} 类型不匹配，期望 {typeof(T).Name}");
            }
            return null;
        }

        /// <summary>由容器代理转发的技能异步执行入口</summary>
        public UniTask<bool> TryExecuteAsync(UnitTag tag, AbilityContext context, System.Threading.CancellationToken cancellationToken = default)
        {
            if (!_abilities.TryGetValue(tag, out var ability))
            {
                return UniTask.FromResult(false);
            }
            return ability.TryExecuteAsync(context, cancellationToken);
        }

        /// <summary>销毁并清空所有技能实例</summary>
        public void Clear()
        {
            foreach (var ability in _abilities.Values)
            {
                ability.Dispose();
            }
            _abilities.Clear();
        }
    }
}
