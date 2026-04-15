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
    /// 负责技能实例的注册、移除、查询与统一异步执行入口。
    /// 容器接管了所有技能实例的 Owner 依赖注入权。
    /// </remarks>
    public class AbilityContainer : ITagSource, IEnumerable<KeyValuePair<UnitTag, UnitAbility>>
    {
        public IUnit Owner { get; }
        private readonly Dictionary<UnitTag, UnitAbility> _abilitys = new();

        public int Count => _abilitys.Count;

        /// <summary>构造函数，绑定宿主单位</summary>
        public AbilityContainer(IUnit owner)
        {
            Owner = owner;
        }
        
        public bool HasTag(UnitTag tag) => _abilitys.ContainsKey(tag);
        
        public IEnumerator<KeyValuePair<UnitTag, UnitAbility>> GetEnumerator() => _abilitys.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _abilitys.GetEnumerator();

        /// <summary>
        /// 添加技能并自动为其注入宿主。
        /// 若已存在同名技能，旧技能将被自动销毁替换。
        /// </summary>
        public void AddAbility(UnitAbility ability)
        {
            if (ability == null) return;

            if (_abilitys.TryGetValue(ability.Name, out var oldAbility))
            {
                oldAbility.Dispose();
            }

            // 【核心注入机制】所有能力在此刻才真正知晓它们的宿主是谁
            ability.Init(Owner);
            _abilitys[ability.Name] = ability;
        }

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
        /// 强类型获取指定标签的技能实例。
        /// </summary>
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
        /// 由容器代理转发的技能异步执行入口。
        /// 外部直接调用 `Unit.Use(tag, ctx)` 即可启动整个技能流水线。
        /// </summary>
        public UniTask<bool> TryExecuteAsync(UnitTag tag, AbilityContext context, CancellationToken cancellationToken = default)
        {
            if (!_abilitys.TryGetValue(tag, out var ability))
            {
                return UniTask.FromResult(false);
            }

            return ability.TryExecuteAsync(context, cancellationToken);
        }

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