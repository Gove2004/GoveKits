using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Demo
{
    using GoveKits.Units;

    // 演示：修改属性的 Buff（带中文日志）
    public class AttributeBuff : Buff
    {
        private readonly AttributeContainer attributes;
        private readonly string key;
        private readonly float delta;

        public AttributeBuff(string name, AttributeContainer attributes, string key, float delta, int currentStack = 1) : base(name, currentStack)
        {
            this.attributes = attributes;
            this.key = key;
            this.delta = delta;
        }

        public override void Apply()
        {
            base.Apply();
            if (!attributes.Has(key)) attributes.Add(key, 0f);
            if (attributes.TryGetValue(key, out var v))
            {
                attributes.SetValue(key, v + delta);
                Debug.Log($"[Buff] 应用 {Name}: {key} 增加 {delta} ({v} -> {v + delta})");
            }
        }

        public override void Stack(int count = 1)
        {
            base.Stack(count);
            if (attributes.TryGetValue(key, out var v))
            {
                attributes.SetValue(key, v + delta * count);
                Debug.Log($"[Buff] 堆叠 {Name}: {key} 增加 {delta * count}（当前层数 {CurrentStack}）");
            }
        }

        public override void Remove()
        {
            base.Remove();
            float total = delta * CurrentStack;
            if (attributes.TryGetValue(key, out var v))
            {
                attributes.SetValue(key, v - total);
                Debug.Log($"[Buff] 移除 {Name}: {key} 减少 {total} ({v} -> {v - total})");
            }
        }
    }


    // 周期性伤害（DOT）Buff
    public class DotBuff : Buff
    {
        private readonly AttributeContainer attributes;
        private readonly int ticks;
        private readonly float tickInterval;
        private readonly float tickDamage;
        private CancellationTokenSource cts;

        public DotBuff(string name, AttributeContainer attributes, float tickDamage, int ticks, float tickInterval = 1f) : base(name, 1)
        {
            this.attributes = attributes;
            this.tickDamage = tickDamage;
            this.ticks = ticks;
            this.tickInterval = tickInterval;
        }

        public override void Apply()
        {
            base.Apply();
            Debug.Log($"[Buff] 应用 DOT {Name}: 每 {tickInterval}s 造成 {tickDamage} 点，持续 {ticks} 次");
            Duration().Forget();
        }

        public override void Remove()
        {
            base.Remove();
            cts?.Cancel();
            cts?.Dispose();
            Debug.Log($"[Buff] 移除 DOT {Name}");
        }

        public override async UniTask Duration()
        {
            for (int i = 0; i < ticks; i++)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(tickInterval));
                if (attributes.TryGetValue("HP", out var hp))
                {
                    float newHp = Math.Max(0f, hp - tickDamage);
                    attributes.SetValue("HP", newHp);
                    Debug.Log($"[DOT] {Name} 第 {i + 1} 次：造成 {tickDamage} 点伤害，HP: {hp} -> {newHp}");
                }
            }
            // 完成后自动移除（由使用者决定是否真正从容器移除）
        }
    }


    // 眩晕 Buff（阻止行动）
    public class StunBuff : Buff
    {
        public StunBuff(string name, int currentStack = 1) : base(name, currentStack) { }

        public override void Apply()
        {
            base.Apply();
            Debug.Log($"[Buff] 眩晕 {Name} 应用：单位无法行动（本次示例由 DemoController 检测并跳过行动）");
        }

        public override void Remove()
        {
            base.Remove();
            Debug.Log($"[Buff] 眩晕 {Name} 移除：单位恢复行动");
        }
    }


    // 护盾 Buff：在属性 `Shield` 上加值，移除时减回
    public class ShieldBuff : Buff
    {
        private readonly AttributeContainer attributes;
        private readonly float amount;

        public ShieldBuff(string name, AttributeContainer attributes, float amount, int currentStack = 1) : base(name, currentStack)
        {
            this.attributes = attributes;
            this.amount = amount;
        }

        public override void Apply()
        {
            base.Apply();
            if (!attributes.Has("Shield")) attributes.Add("Shield", 0f);
            attributes.TryGetValue("Shield", out var v);
            attributes.SetValue("Shield", v + amount);
            Debug.Log($"[Buff] 护盾 {Name} 应用：护盾 +{amount} ({v} -> {v + amount})");
        }

        public override void Stack(int count = 1)
        {
            base.Stack(count);
            attributes.TryGetValue("Shield", out var v);
            attributes.SetValue("Shield", v + amount * count);
            Debug.Log($"[Buff] 护盾 {Name} 堆叠：护盾 +{amount * count}（当前层数 {CurrentStack}）");
        }

        public override void Remove()
        {
            base.Remove();
            attributes.TryGetValue("Shield", out var v);
            attributes.SetValue("Shield", Math.Max(0f, v - amount * CurrentStack));
            Debug.Log($"[Buff] 护盾 {Name} 移除：护盾 -{amount * CurrentStack}（剩余 {Math.Max(0f, v - amount * CurrentStack)}）");
        }
    }
}
