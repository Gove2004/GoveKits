
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GoveKits.Units
{
    // 能力容器，用于管理单位的能力
    public class AbilityContainer
    {
        private readonly Dictionary<string, IAbility> _abilities = new();
        public event System.Action<string, IAbility> OnAbilityAdded;
        public event System.Action<string> OnAbilityRemoved;

        public void AddAbility(string key, IAbility ability)
        {
            if (_abilities.ContainsKey(key)) return;
            _abilities[key] = ability;
            OnAbilityAdded?.Invoke(key, ability);
        }

        public void RemoveAbility(string key)
        {
            if (!_abilities.ContainsKey(key))
            {
                throw new KeyNotFoundException($"[AbilityContainer] 未知能力 {key}");
            }
            _abilities.Remove(key);
            OnAbilityRemoved?.Invoke(key);
        }

        public bool TryGetAbility(string key, out IAbility ability) =>
            _abilities.TryGetValue(key, out ability);

        public bool Has(string key) =>
            _abilities.ContainsKey(key);

        public void Clear()
        {
            _abilities.Clear();
        }

        // 增强执行方法，包含完整的生命周期
        public async UniTask TryExecuteAbility(string key, IUnit caster, IUnit target, CancellationToken cancellationToken = default, Dictionary<string, object> parameters = null)
        {
            if (!TryGetAbility(key, out var ability))
                await UniTask.CompletedTask;

            var context = new AbilityContext(caster, target, cancellationToken, parameters);

            await ability.Try(context);
        }

        public IEnumerable<string> Keys => _abilities.Keys;

        public int Count => _abilities.Count;
    }
}