using System;
using Cysharp.Threading.Tasks;
using GoveKits.Unit;
using UnityEngine;

public class Player : UnitBehaviour
{
    public override void InitializeAttributes()
    {
        base.InitializeAttributes();

        Attributes.AddState("MaxHP", 100f);
        Attributes.AddState("Atk", 10f);
        Attributes.AddState("Def", 5f);
    }

    public override void InitializeMarks()
    {
        base.InitializeMarks();
        // 添加玩家特有的标记初始化逻辑

        Marks.Add("Player", new TagMark("Player"));
    }

    public override void InitializeAbilities()
    {
        base.InitializeAbilities();
        // 添加玩家特有的能力初始化逻辑
        Abilities.Add("NormalAttack", new NormalAttackAbility());
        
    }



    public override void InitializeReactions()
    {
        base.InitializeReactions();
        // 添加玩家特有的反应初始化逻辑

        Reactions.Add("OnDamage", new DelegateReaction<DamageEffect>(
            "OnDamage",
            this,
            (effect) => 
            {
                Debug.Log($"{this} 受到了 {effect.Amount} 点伤害！");
                // 处理伤害逻辑
            },
            priority: 10
        ));

        Reactions.Add("HealReaction", new HealReaction("HealReaction", this));
    }
}



public class NormalAttackAbility : GameAbility
{
    public NormalAttackAbility() : base("NormalAttack") { }

    protected override UniTask OnExecute(IGameUnit source, IGameUnit target)
    {
        Debug.Log($"{source} 对 {target} 进行了普通攻击！");
        // 处理攻击逻辑
        return UniTask.CompletedTask;
    }
}




public class HealReaction : GameReaction<DamageEffect>
{
    public HealReaction(GameTag name, IGameUnit owner, int priority = 0)
        : base(name, owner, priority)
    {
    }

    protected override void OnExecute(DamageEffect effect)
    {
        // 假设当受到伤害时，如果某个条件满足则进行治疗
        if (effect.Amount > 50) // 例如：如果伤害超过50则触发治疗
        {
            Debug.Log($"{effect.Source} 触发了治疗反应，治疗了 {effect.Amount * 0.2f} 点生命值！");
            // 处理治疗逻辑
        }
    }
}



public class DamageEffect : GameEffect
{
    public float Amount { get; set; }

    public override void OnRecycle()
    {
        Amount = 0f;
    }
}