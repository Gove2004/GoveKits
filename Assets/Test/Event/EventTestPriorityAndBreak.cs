using System.Collections.Generic;
using GoveKits.Runtime.Core.Event;
using UnityEngine;

namespace GoveKits.Test.Event
{
    public class EventTestPriorityAndBreak : MonoBehaviour
    {
        [Header("Test Options")]
        public bool breakOnFatalDamage = true;
        public int fatalDamageThreshold = 999;

        private readonly List<DisposeAction> _disposables = new();
        private readonly List<string> _executionOrder = new();

        private void OnEnable()
        {
            _disposables.Add(EventCore.Subscribe<DamageEvent>(OnHighPriorityDamage, priority: 100));
            _disposables.Add(EventCore.Subscribe<DamageEvent>(OnMediumPriorityDamage, priority: 10));
            _disposables.Add(EventCore.Subscribe<DamageEvent>(OnLowPriorityDamage, priority: -10));
            _disposables.Add(EventCore.Subscribe<HealEvent>(OnHealEvent, priority: 0));
        }

        private void OnDisable()
        {
            for (int i = 0; i < _disposables.Count; i++)
            {
                _disposables[i]?.Dispose();
            }
            _disposables.Clear();
        }

        public void RunPriorityTest()
        {
            _executionOrder.Clear();
            EventCore.Publish<DamageEvent>(e =>
            {
                e.Amount = 100;
                e.Source = "PriorityTest";
            });

            Debug.Log($"[EventTest] Priority Order => {string.Join(" -> ", _executionOrder)}");
            var expected = "P100 -> P10 -> P-10";
            var actual = string.Join(" -> ", _executionOrder);
            if (actual != expected)
            {
                Debug.LogError($"[EventTest] Priority order mismatch. Expected: {expected}, Actual: {actual}");
            }
        }

        public void RunBreakTest()
        {
            _executionOrder.Clear();
            EventCore.Publish<DamageEvent>(e =>
            {
                e.Amount = fatalDamageThreshold;
                e.Source = "BreakTest";
            });

            Debug.Log($"[EventTest] Break Order => {string.Join(" -> ", _executionOrder)}");
            if (breakOnFatalDamage && _executionOrder.Count > 1)
            {
                Debug.LogError("[EventTest] Break failed. Lower priority listeners were still invoked.");
            }
        }

        public void RunHealTest()
        {
            EventCore.Publish<HealEvent>(e =>
            {
                e.Amount = 30;
                e.Source = "HealTest";
            });
        }

        private void OnHighPriorityDamage(DamageEvent e)
        {
            _executionOrder.Add("P100");
            Debug.Log($"[EventTest] High Priority Damage: {e.Amount} from {e.Source}");
            if (breakOnFatalDamage && e.Amount >= fatalDamageThreshold)
            {
                e.IsBreak = true;
                Debug.Log("[EventTest] Set IsBreak = true");
            }
        }

        private void OnMediumPriorityDamage(DamageEvent e)
        {
            _executionOrder.Add("P10");
            Debug.Log($"[EventTest] Medium Priority Damage: {e.Amount} from {e.Source}");
        }

        private void OnLowPriorityDamage(DamageEvent e)
        {
            _executionOrder.Add("P-10");
            Debug.Log($"[EventTest] Low Priority Damage: {e.Amount} from {e.Source}");
        }

        private void OnHealEvent(HealEvent e)
        {
            Debug.Log($"[EventTest] Heal Event: {e.Amount} from {e.Source}");
        }
    }
}
