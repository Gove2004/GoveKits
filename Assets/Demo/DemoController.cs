using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Linq;
using GoveKits.Units;

namespace GoveKits.Demo
{
    using GoveKits.Units;

    // Attach this to an empty GameObject in a Scene and press Play.
    // It will spawn two UnitComponents and run a simple turn-based loop.
    public class DemoController : MonoBehaviour
    {
        public float initialHP = 100f;
        public float attackDamage = 15f;
        public float healAmount = 8f;
        public float turnDelaySeconds = 1f;

        private UnitComponent unitA;
        private UnitComponent unitB;

        private CancellationTokenSource cts;

        private void Awake()
        {
            cts = new CancellationTokenSource();
        }

        private void OnDestroy()
        {
            cts?.Cancel();
            cts?.Dispose();
        }

        private void Start()
        {
            SetupUnits();
            _ = RunCombatLoop(cts.Token);
        }

        private void SetupUnits()
        {
            // Create two GameObjects with UnitComponent
            var goA = new GameObject("Demo_Unit_A");
            unitA = goA.AddComponent<UnitComponent>();
            var goB = new GameObject("Demo_Unit_B");
            unitB = goB.AddComponent<UnitComponent>();

            // 初始化属性
            unitA.Attributes.Add("HP", initialHP);
            unitB.Attributes.Add("HP", initialHP);

            // 添加带冷却的能力（构造时指定 秒）
            unitA.Abilities.Add("Attack", new DamageAbility("Slash", attackDamage, 1));
            unitA.Abilities.Add("Heal", new HealAbility("Bandage", healAmount, 2));

            unitB.Abilities.Add("Attack", new DamageAbility("Stab", attackDamage * 0.9f, 1));
            unitB.Abilities.Add("Heal", new HealAbility("FirstAid", healAmount * 1.2f, 3));

            // 给 unitA 添加一个增加最大生命的 Mark（示例堆叠）
            var rage = new AttributeMark("Rage", unitA.Attributes, "HP", 10f, 1);
            unitA.Marks.Add("Rage", rage);
            // 再添加一层触发堆叠
            unitA.Marks.Add("Rage", new AttributeMark("Rage", unitA.Attributes, "HP", 10f, 1));

            unitA.Attributes.TryGetValue("HP", out var afterMarkHp);
            Debug.Log($"[Demo] 完成初始化：{goA.name} HP={afterMarkHp}");
            Debug.Log($"[Demo] {goA.name} HP={initialHP}, {goB.name} HP={initialHP}");
        }

        private async UniTaskVoid RunCombatLoop(CancellationToken token)
        {
            var attacker = unitA as IUnit;
            var defender = unitB as IUnit;

            int turn = 1;
            while (!token.IsCancellationRequested)
            {
                var attackerComp = (Component)attacker;
                var defenderComp = (Component)defender;
                Debug.Log($"[演示] 回合 {turn} - {attackerComp.gameObject.name} 开始行动");

                // 如果被眩晕，跳过行动并移除眩晕（示例行为）
                if (attacker.Marks.Any("Stun"))
                {
                    Debug.Log($"[演示] {attackerComp.gameObject.name} 被眩晕，跳过本回合");
                    if (attacker.Marks.TryGet("Stun", out var stunMark))
                    {
                        stunMark.Remove();
                        attacker.Marks.Remove("Stun");
                    }
                }
                else
                {
                    // 正常尝试使用攻击
                    var ctx = new UnitContext(attacker, defender);
                    await attacker.Abilities.TryExecute("Attack", ctx);

                    // 攻击后有一定概率自我治疗
                    if (UnityEngine.Random.value < 0.2f)
                    {
                        var healCtx = new UnitContext(attacker, attacker);
                        await attacker.Abilities.TryExecute("Heal", healCtx);
                    }
                }

                // 每隔若干回合触发一些 Mark 示例：DOT / 护盾 / 眩晕
                if (turn % 3 == 0)
                {
                    // defender 对 attacker 施加 DOT
                    var burn = new DotMark("Burn", attacker.Attributes, 5f, 3, 1f);
                    attacker.Marks.Add("Burn", burn);
                    Debug.Log($"[演示] {defenderComp.gameObject.name} 对 {attackerComp.gameObject.name} 施加了 DOT (Burn)");
                }

                if (turn % 4 == 0)
                {
                    // attacker 获得护盾
                    var shield = new ShieldMark("Barrier", attacker.Attributes, 12f, 1);
                    attacker.Marks.Add("Barrier", shield);
                    Debug.Log($"[演示] {attackerComp.gameObject.name} 获得护盾 Barrier");
                }

                if (UnityEngine.Random.value < 0.1f)
                {
                    // defender 可能造成眩晕
                    var stun = new StunMark("Stun", 1);
                    attacker.Marks.Add("Stun", stun);
                    Debug.Log($"[演示] {defenderComp.gameObject.name} 触发了眩晕 {attackerComp.gameObject.name}");
                }

                // 打印当前生命与护盾状态
                attacker.Attributes.TryGetValue("HP", out var aHp);
                attacker.Attributes.TryGetValue("Shield", out var aShield);
                defender.Attributes.TryGetValue("HP", out var bHp);
                defender.Attributes.TryGetValue("Shield", out var bShield);
                Debug.Log($"[演示] 状态: {attackerComp.gameObject.name} HP={aHp} 护盾={aShield} | {defenderComp.gameObject.name} HP={bHp} 护盾={bShield}");

                // 死亡判定
                if (bHp <= 0f)
                {
                    Debug.Log($"[演示] {defenderComp.gameObject.name} 已阵亡。{attackerComp.gameObject.name} 胜利。");
                    break;
                }

                // 交换角色
                var tmp = attacker; attacker = defender; defender = tmp;

                turn++;
                await UniTask.Delay(TimeSpan.FromSeconds(turnDelaySeconds), cancellationToken: token);
            }

            
            

            var effect = BaseEffectBuilder.Sequence(
                BaseEffectBuilder.After(1f),
                BaseEffectBuilder.Immediate(
                    (ctx) => Debug.Log("[演示] 战斗循环结束。")
                )
            );
            effect.Apply(new UnitContext(null, null)).Forget();

            var otherEffect = BaseEffectBuilder.Delay(1f, 
                (ctx) => Debug.Log("[演示] 另一种延迟效果结束。")
            );
            otherEffect.Apply(new UnitContext(null, null)).Forget();
        }
    }
}
