using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using GoveKits.Events;
using GoveKits.Unit;

// === 🔥 终极 RPG 战斗演示版 🔥 ===
namespace GoveKits.RichCombat
{
    // [规则] MonoBehaviour 类必须在文件最上方
    public class RichBattleManager : MonoBehaviour
    {
        private RichUnit Hero;
        private RichUnit Boss;

        [Header("设置")]
        public float TurnDelay = 1.0f; // 回合间隔速度

        void Start()
        {
            Debug.Log("<color=yellow>=== ⚔️ 史诗战斗模拟器启动 ⚔️ ===</color>");
            StartCoroutine(SetupAndFight());
        }

        IEnumerator SetupAndFight()
        {
            // 1. 创建角色 (名字, HP, MP, Atk, Def, Crit%, Dodge%)
            // 英雄: 高攻, 高暴击, 低防
            Hero = CreateUnit("圣骑士", 1500, 100, 80, 20, 0.25f, 0.1f);
            // Boss: 血厚, 低攻, 高防
            Boss = CreateUnit("深渊魔王", 3000, 200, 60, 50, 0.1f, 0.05f);

            yield return null; // 等待 Start 初始化

            // 2. 装配技能
            // 英雄技能
            Hero.Abilities.Add("Atk", new HeroBasicAttack()); 
            Hero.Abilities.Add("Heal", new HolyLight());
            Hero.Abilities.Add("Ult", new GrandJudgement()); // 大招带眩晕
            // 新增技能：格挡、嘲讽、斩击
            Hero.Abilities.Add("ShieldWall", new ShieldWall());
            Hero.Abilities.Add("Taunt", new TauntAbility());
            Hero.Abilities.Add("Cleave", new Cleave());

            // Boss技能
            Boss.Abilities.Add("Atk", new BossClaw());
            Boss.Abilities.Add("Fire", new HellFire()); // 带燃烧
            Boss.Abilities.Add("Drain", new VampiricTouch()); // 吸血
            // Boss 也会有斩击，增加强度感
            Boss.Abilities.Add("Cleave", new Cleave(1.1f));

            Debug.Log("✅ 双方已就位，战斗开始！");
            yield return new WaitForSeconds(1f);

            // 3. 战斗循环
            int round = 1;
            while (Hero.CurrentHP > 0 && Boss.CurrentHP > 0)
            {
                Debug.Log($"\n<color=white>--- 第 {round} 回合 ---</color>");

                // 英雄回合
                yield return ProcessTurn(Hero, Boss);
                if (Boss.CurrentHP <= 0) break;

                // Boss回合
                yield return ProcessTurn(Boss, Hero);
                if (Hero.CurrentHP <= 0) break;

                round++;
                yield return new WaitForSeconds(TurnDelay);
            }

            string winner = Hero.CurrentHP > 0 ? Hero.UnitName : Boss.UnitName;
            Debug.Log($"<color=yellow>🏆 战斗结束！胜者是: [{winner}] 🏆</color>");
        }

        IEnumerator ProcessTurn(RichUnit actor, RichUnit target)
        {
            // 1. 检查眩晕 (Skip Turn)
            if (actor.Marks.HasTag("Stun"))
            {
                Debug.Log($"🚫 <color=grey>[{actor.UnitName}] 处于眩晕状态，跳过本回合！</color>");
                yield return new WaitForSeconds(0.5f);
                yield break; 
            }


            // 2. AI 策略（优先级）：低血 -> 格挡/回血 -> 嘲讽 -> 大招 -> 斩击 -> 其他
            bool acted = false;

            // 如果血量偏低，优先格挡（盾墙）或治疗
            if (actor.CurrentHP < actor.MaxHP * 0.35f && actor.Abilities.HasTag("ShieldWall") && TryCast(actor, "ShieldWall", actor)) acted = true;
            else if (actor.CurrentHP < actor.MaxHP * 0.3f && TryCast(actor, "Heal", actor)) acted = true;

            // 如果有嘲讽技能且目标还没有被嘲讽，则尝试嘲讽
            else if (actor.Abilities.HasTag("Taunt") && !target.Marks.HasTag("Taunt") && TryCast(actor, "Taunt", target)) acted = true;

            // 主动技能优先：大招 -> 斩击 -> 其他
            else if (TryCast(actor, "Ult", target)) acted = true;
            else if (TryCast(actor, "Cleave", target)) acted = true;
            else if (TryCast(actor, "Fire", target)) acted = true;
            else if (TryCast(actor, "Drain", target)) acted = true;
            else if (TryCast(actor, "Atk", target)) acted = true;   // 普攻 (兜底)

            if (!acted) Debug.Log($"... [{actor.UnitName}] 发呆了 (无行动)");

            yield return new WaitForSeconds(0.5f);
        }

        // 辅助施法方法
        bool TryCast(RichUnit actor, string key, RichUnit target)
        {
            if (actor.Abilities.TryGet(key, out var ability))
            {
                if (ability.CanExecute(actor, target))
                {
                    ability.Execute(actor, target).Forget();
                    return true;
                }
            }
            return false;
        }

        RichUnit CreateUnit(string name, float hp, float mp, float atk, float def, float crit, float dodge)
        {
            GameObject go = new GameObject(name);
            var unit = go.AddComponent<RichUnit>();
            unit.UnitName = name;
            unit.BaseHP = hp;
            unit.BaseMP = mp;
            unit.BaseAtk = atk;
            unit.BaseDef = def;
            unit.BaseCrit = crit;
            unit.BaseDodge = dodge;
            return unit;
        }
    }

    // ================= 核心扩展类 =================

    // --- 1. 增强的战斗事件 ---
    public class RichCombatEvent : GameEffect
    {
        // 存储数值变化 (HP, MP)
        public Dictionary<GameTag, float> InstantChanges = new Dictionary<GameTag, float>();
        // 存储要施加的 Buff
        public List<GameMark> ApplyMarks = new List<GameMark>();
        
        // 战斗标志位
        public bool IsCritical; // 是否暴击
        public bool IsDodged;   // 是否闪避

        public void AddChange(GameTag tag, float value)
        {
            if (InstantChanges.ContainsKey(tag)) InstantChanges[tag] += value;
            else InstantChanges[tag] = value;
        }

        public void AddMark(GameMark mark) => ApplyMarks.Add(mark);

        public override void OnRecycle() 
        {
            base.OnRecycle();
            InstantChanges.Clear();
            ApplyMarks.Clear();
            IsCritical = false;
            IsDodged = false;
        }
    }

    // --- 2. 增强的角色单位 (包含数值计算逻辑) ---
    public class RichUnit : UnitBehaviour
    {
        public string UnitName;
        // 基础面板
        public float BaseHP, BaseMP, BaseAtk, BaseDef, BaseCrit, BaseDodge;

        // 快捷属性访问器
        public float CurrentHP => Attributes.GetValue("HP");
        public float MaxHP => Attributes.GetValue("MaxHP");

        public override void Start()
        {
            base.Start();
            
            Debug.Log($"✨ [{UnitName}] 登场 (HP:{BaseHP} Atk:{BaseAtk} Crit:{BaseCrit:P0})");
        }

        public override void InitializeAttributes()
        {
            Attributes = new AttributeContainer(this);
            // 注册所有 RPG 属性
            Attributes.AddState("MaxHP", BaseHP);
            Attributes.AddState("MaxMP", BaseMP);
            Attributes.AddState("Attack", BaseAtk);
            Attributes.AddState("Defense", BaseDef);
            Attributes.AddState("Crit", BaseCrit);   // 暴击率
            Attributes.AddState("Dodge", BaseDodge); // 闪避率

            Attributes.AddRuntime("HP", "MaxHP");
            Attributes.AddRuntime("MP", "MaxMP");
        }

        public override void InitializeReactions()
        {
            Reactions = new ReactionContainer(this);

            // === 核心伤害计算逻辑 ===
            var reaction = new DelegateReaction<RichCombatEvent>("HandleCombat", this, evt => 
            {
                // 1. 闪避判定 (仅针对受害者，且不是治疗，也不是真实伤害)
                if ((Object)evt.Target == this && evt.InstantChanges.TryGetValue("HP", out float hpDelta) && hpDelta < 0)
                {
                    if (!evt.HasTag("TrueDamage")) // 真实伤害不可闪避
                    {
                        float dodgeChance = Attributes.GetValue("Dodge");
                        if (Random.value < dodgeChance)
                        {
                            evt.IsDodged = true;
                            Debug.Log($"💨 <color=white>[{UnitName}] 灵巧地闪避了攻击！</color>");
                            return; // 直接返回，不应用伤害
                        }
                    }
                }

                // 2. 应用数值 (如果是 Source 发起的攻击，计算暴击；如果是 Target 承受攻击，计算防御)
                // 为了简化，我们在"应用阶段"统一处理
                if (evt.InstantChanges.Count > 0)
                {
                    foreach(var kv in evt.InstantChanges)
                    {
                        string tag = kv.Key;
                        float val = kv.Value;

                        // 仅处理针对自己的数值变化
                        // (注意：这里简化了逻辑，假设事件里的 HP 变化就是给 Target 的)
                        if ((Object)evt.Target == this)
                        {
                            if (tag == "HP" && val < 0) // 是伤害
                            {
                                // 优先处理护盾吸收（如果有）
                                if (Marks.TryGet("Shield", out var mk) && mk is ShieldMark sm)
                                {
                                    float incoming = -val;
                                    float absorbed = Mathf.Min(sm.AbsorbRemaining, incoming);
                                    sm.AbsorbRemaining -= absorbed;
                                    incoming -= absorbed;
                                    val = -incoming; // 剩余要作用到血量的值

                                    Debug.Log($"🛡️ <color=cyan>[{UnitName}]</color> 护盾吸收了 <b>{absorbed:F0}</b> 点伤害");

                                    if (sm.AbsorbRemaining <= 0)
                                    {
                                        Marks.Remove("Shield");
                                        Debug.Log($"💥 <color=orange>[{UnitName}] 护盾被打破！</color>");
                                    }

                                    // 如果被护盾完全吸收，则直接跳过后续防御/暴击计算
                                    if (val == 0)
                                    {
                                        Attributes.ApplyRuntimeChange(tag, val);
                                        continue;
                                    }
                                }

                                // A. 暴击判定 (由攻击者 Source 属性决定)
                                if (evt.Source != null && !evt.IsCritical) // 防止重复判定
                                {
                                    float critChance = evt.Source.Attributes.GetValue("Crit");
                                    if (Random.value < critChance)
                                    {
                                        val *= 1.5f; // 150% 爆伤
                                        evt.IsCritical = true;
                                    }
                                }

                                // B. 防御减伤 (由防御者 This 属性决定)，嘲讽会降低防御
                                if (!evt.HasTag("TrueDamage"))
                                {
                                    float def = Attributes.GetValue("Defense");
                                    if (Marks.TryGet("Taunt", out var tmk) && tmk is TauntMark tmark)
                                    {
                                        def = Mathf.Max(0f, def - tmark.DefensePenalty);
                                    }
                                    // 简单减伤公式: 实际伤害 = 伤害 * (100 / (100 + Def))
                                    float reduction = 100f / (100f + def);
                                    val *= reduction;
                                }

                                // 打印伤害日志
                                string critStr = evt.IsCritical ? " <size=14><color=yellow><b>[暴击!]</b></color></size>" : "";
                                Debug.Log($"⚔️ <color=red>[{UnitName}]</color> 受到伤害: <b>{val:F0}</b>{critStr} (剩余: {CurrentHP + val:F0})");
                            }
                            else if (tag == "HP" && val > 0) // 是治疗
                            {
                                Debug.Log($"💚 <color=green>[{UnitName}]</color> 恢复生命: <b>+{val:F0}</b>");
                            }

                            // 应用最终数值
                            Attributes.ApplyRuntimeChange(tag, val);
                        }
                    }
                }

                // 3. 应用状态 (Buff/Debuff) - 只有 Target 才会获得 Buff
                if ((Object)evt.Target == this && evt.ApplyMarks.Count > 0)
                {
                    foreach(var mark in evt.ApplyMarks)
                    {
                        Marks.Add(mark.Tag, mark);
                        string color = mark.Tag.ToString().Contains("Burn") ? "orange" : "cyan";
                        Debug.Log($"🏷️ <color={color}>[{UnitName}] 获得了状态: [{mark.Tag}]</color>");
                    }
                }

            }, EventPriority.Normal);
            
            Reactions.Add("HandleCombat", reaction);
        }

        public override string ToString() => UnitName;
    }

    // --- 3. 定义状态 (Marks) ---

    // 燃烧: 每秒扣血
    public class BurnMark : GameMark
    {
        private float _timer;
        private float _dmg;
        public BurnMark(float duration, float damage) : base("Burn", duration) { _dmg = damage; }

        public override void OnTick(float dt)
        {
            base.OnTick(dt);
            _timer += dt;
            if (_timer >= 1.0f)
            {
                _timer = 0;
                // 发布 DOT 伤害
                EventManager.Publish<RichCombatEvent>(e => {
                    e.Source = Source;
                    e.Target = Owner;
                    e.AddChange("HP", -_dmg);
                    e.AddTag("TrueDamage"); // 燃烧通常是真实伤害
                });
                Debug.Log($"🔥 [{Owner}] 燃烧扣血 {_dmg}");
            }
        }
    }

    // 眩晕: 仅仅是一个 Tag，逻辑在 BattleManager 里判断
    public class StunMark : GameMark
    {
        public StunMark(float duration) : base("Stun", duration) { }
    }

    // 回春: 每秒回血
    public class RegenMark : GameMark
    {
        private float _timer;
        private float _heal;
        public RegenMark(float duration, float heal) : base("Regen", duration) { _heal = heal; }
        public override void OnTick(float dt)
        {
            base.OnTick(dt);
            _timer += dt;
            if (_timer >= 1.0f)
            {
                _timer = 0;
                EventManager.Publish<RichCombatEvent>(e => {
                    e.Target = Owner;
                    e.AddChange("HP", _heal);
                });
            }
        }
    }

    // --- 4. 定义具体技能 ---

    // === 英雄技能 ===
    public class HeroBasicAttack : GameAbility
    {
        public HeroBasicAttack() : base("普通攻击") { SetCooldown(0); }
        protected override async UniTask OnExecute(IGameUnit source, IGameUnit target)
        {
            Debug.Log($"🗡️ [{source}] 发起普攻");
            float atk = source.Attributes.GetValue("Attack");
            
            EventManager.Publish<RichCombatEvent>(e => {
                e.Source = source;
                e.Target = target;
                e.AddChange("HP", -atk);
            });

            // 普攻回蓝 15点
            source.Attributes.ApplyRuntimeChange("MP", 15);
            Debug.Log($"💧 [{source}] 回蓝 +15");
            await UniTask.CompletedTask;
        }
    }

    public class HolyLight : GameAbility
    {
        public HolyLight() : base("圣光术") { SetCooldown(3f); AddCost("MP", 30); }
        protected override async UniTask OnExecute(IGameUnit source, IGameUnit target)
        {
            Debug.Log($"✨ [{source}] 咏唱圣光术");
            float atk = source.Attributes.GetValue("Attack");
            // 给自己回血 200% 攻击力
            EventManager.Publish<RichCombatEvent>(e => {
                e.Source = source;
                e.Target = source; // 目标是自己
                e.AddChange("HP", atk * 2.0f);
            });
            await UniTask.CompletedTask;
        }
    }

    public class GrandJudgement : GameAbility
    {
        public GrandJudgement() : base("大审判") { SetCooldown(8f); AddCost("MP", 60); }
        protected override async UniTask OnExecute(IGameUnit source, IGameUnit target)
        {
            Debug.Log($"⚡ <size=14><b>[{source}] 降下最终审判!!!</b></size>");
            float atk = source.Attributes.GetValue("Attack");

            EventManager.Publish<RichCombatEvent>(e => {
                e.Source = source;
                e.Target = target;
                e.AddChange("HP", -atk * 2.5f); // 2.5倍伤害
                e.AddMark(new StunMark(1.1f));  // 眩晕1回合(稍大于1s确保覆盖检测)
            });
            await UniTask.CompletedTask;
        }
    }

    // === Boss 技能 ===
    public class BossClaw : GameAbility
    {
        public BossClaw() : base("魔爪") { SetCooldown(0); }
        protected override async UniTask OnExecute(IGameUnit source, IGameUnit target)
        {
            Debug.Log($"🐾 [{source}] 挥动魔爪");
            float atk = source.Attributes.GetValue("Attack");
            EventManager.Publish<RichCombatEvent>(e => {
                e.Source = source;
                e.Target = target;
                e.AddChange("HP", -atk);
            });
            source.Attributes.ApplyRuntimeChange("MP", 10);
            await UniTask.CompletedTask;
        }
    }

    public class HellFire : GameAbility
    {
        public HellFire() : base("地狱火") { SetCooldown(3f); AddCost("MP", 40); }
        protected override async UniTask OnExecute(IGameUnit source, IGameUnit target)
        {
            Debug.Log($"🔥 [{source}] 喷射地狱烈焰");
            float atk = source.Attributes.GetValue("Attack");
            EventManager.Publish<RichCombatEvent>(e => {
                e.Source = source;
                e.Target = target;
                e.AddChange("HP", -atk * 1.2f);
                e.AddMark(new BurnMark(3.1f, 30f)); // 燃烧3秒，每秒30伤害
            });
            await UniTask.CompletedTask;
        }
    }

    public class VampiricTouch : GameAbility
    {
        public VampiricTouch() : base("吸血鬼之触") { SetCooldown(5f); AddCost("MP", 50); }
        protected override async UniTask OnExecute(IGameUnit source, IGameUnit target)
        {
            Debug.Log($"🩸 [{source}] 吸取生命");
            float atk = source.Attributes.GetValue("Attack");
            float dmg = atk * 1.5f;

            // 1. 造成伤害
            EventManager.Publish<RichCombatEvent>(e => {
                e.Source = source;
                e.Target = target;
                e.AddChange("HP", -dmg);
            });

            // 2. 回复自己
            EventManager.Publish<RichCombatEvent>(e => {
                e.Source = source;
                e.Target = source;
                e.AddChange("HP", dmg * 0.5f); // 吸血 50%
            });
            await UniTask.CompletedTask;
        }
    }

    // === 新增状态: 护盾（吸收伤害）和嘲讽（降低防御） ===
    public class ShieldMark : GameMark
    {
        public float AbsorbRemaining;
        public ShieldMark(float duration, float absorb) : base("Shield", duration)
        {
            AbsorbRemaining = absorb;
        }

        public override void OnApply(IGameUnit owner, IGameUnit source)
        {
            base.OnApply(owner, source);
            Debug.Log($"🛡️ [{owner}] 获得护盾，吸收 {AbsorbRemaining:F0} 点伤害，持续 {Duration:F1}s");
        }
    }

    public class TauntMark : GameMark
    {
        public float DefensePenalty;
        public TauntMark(float duration, float defPenalty) : base("Taunt", duration)
        {
            DefensePenalty = defPenalty;
        }

        public override void OnApply(IGameUnit owner, IGameUnit source)
        {
            base.OnApply(owner, source);
            Debug.Log($"😡 [{owner}] 被嘲讽，防御降低 {DefensePenalty:F0}");
        }
    }

    // === 新增技能实现 ===
    public class ShieldWall : GameAbility
    {
        public ShieldWall() : base("ShieldWall") { SetCooldown(6f); }

        protected override async UniTask OnExecute(IGameUnit source, IGameUnit target)
        {
            // 给自己施加一个短时护盾
            var shield = new ShieldMark(6f, source.Attributes.GetValue("MaxHP") * 0.2f + 100f);
            target.Marks.Add(shield.Tag, shield);
            Debug.Log($"🛡️ [{source}] 施放 盾墙，生成护盾 {shield.AbsorbRemaining:F0}");
            await UniTask.CompletedTask;
        }
    }

    public class TauntAbility : GameAbility
    {
        public TauntAbility() : base("Taunt") { SetCooldown(8f); }

        protected override async UniTask OnExecute(IGameUnit source, IGameUnit target)
        {
            // 对目标施加嘲讽（降低防御，使其更容易被击破）
            var tm = new TauntMark(4f, 20f);
            target.Marks.Add(tm.Tag, tm);
            Debug.Log($"😈 [{source}] 对 [{target}] 施加了嘲讽（降低防御）");
            await UniTask.CompletedTask;
        }
    }

    public class Cleave : GameAbility
    {
        private float _mult = 1.0f;
        public Cleave(float mult = 1.0f) : base("Cleave") { SetCooldown(2f); _mult = mult; }

        protected override async UniTask OnExecute(IGameUnit source, IGameUnit target)
        {
            float atk = source.Attributes.GetValue("Attack");

            // 主体伤害
            EventManager.Publish<RichCombatEvent>(e => {
                e.Source = source;
                e.Target = target;
                e.AddChange("HP", -atk * (1.0f + 0.2f * _mult));
                e.AddTag("Slash");
            });

            // 小范围溅射：对目标造成额外小伤害（示意）
            EventManager.Publish<RichCombatEvent>(e => {
                e.Source = source;
                e.Target = target;
                e.AddChange("HP", -atk * 0.15f);
                e.AddTag("Splash");
            });

            Debug.Log($"🔪 [{source}] 使用 斩击，对 [{target}] 造成溅射伤害");
            await UniTask.CompletedTask;
        }
    }
}