using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Demo
{
	using GoveKits.Units;

	// 带冷却与护盾交互的伤害能力
	public class DamageAbility : BaseAbility
	{
		private readonly float damage;
		private readonly int cooldownSeconds;

		public DamageAbility(string name, float damage, int cooldownSeconds = 1) : base(name)
        {
            this.damage = damage;
			this.CooldownTime = cooldownSeconds;
		}


		// 执行伤害：优先消耗护盾
		public override async UniTask Execute(UnitContext context)
		{
			await UniTask.Yield();
			var target = context.Target;
			if (target == null)
			{
				Debug.LogWarning($"[伤害] {Name}: 目标为空");
				return;
			}

			var attrs = target.Attributes;
			const string hpKey = "HP";
			const string shieldKey = "Shield";
			if (!attrs.Has(hpKey)) attrs.Add(hpKey, 0f);
			if (!attrs.Has(shieldKey)) attrs.Add(shieldKey, 0f);

			attrs.TryGetValue(shieldKey, out var shield);
			attrs.TryGetValue(hpKey, out var hp);

			float remaining = damage;
			if (shield > 0f)
			{
				float used = Math.Min(shield, remaining);
				shield -= used;
				remaining -= used;
				attrs.SetValue(shieldKey, shield);
			}
			if (remaining > 0f)
			{
				float newHp = Math.Max(0f, hp - remaining);
				attrs.SetValue(hpKey, newHp);
				Debug.Log($"[伤害] {GetName(context.Source)} -> {GetName(context.Target)} 造成 {damage} 点伤害（护盾吸收后实际扣除 {remaining}），HP: {hp} -> {newHp} 护盾: {shield}");
			}
			else
			{
				Debug.Log($"[伤害] {GetName(context.Source)} -> {GetName(context.Target)} 造成 {damage} 点伤害，全部被护盾吸收，护盾剩余: {shield}");
			}
		}


		private string GetName(IUnit unit)
		{
			if (unit is UnityEngine.Component c) return c.gameObject.name;
			return unit?.Name ?? "未知单位";
		}
	}


	// 带冷却的治疗能力
	public class HealAbility : BaseAbility
	{
		private readonly float heal;

		public HealAbility(string name, float heal, int cooldownSeconds = 2) : base(name)
		{
			this.heal = heal;
			this.CooldownTime = cooldownSeconds;
		}

		public override async UniTask<bool> Condition(UnitContext context)
		{
			await UniTask.Yield();
			if (context?.Source == null) return false;
			var attrs = context.Source.Attributes;
			string cdKey = $"CD_{Name}";
			if (attrs.Has(cdKey) && attrs.TryGetValue(cdKey, out var cd) && cd > 0f)
			{
				Debug.Log($"[能力] {GetName(context.Source)} 的 {Name} 冷却中：{cd} 秒");
				return false;
			}
			return true;
		}

		public override async UniTask Execute(UnitContext context)
		{
			await UniTask.Yield();
			var target = context.Target;
			if (target == null)
			{
				Debug.LogWarning($"[治疗] {Name}: 目标为空");
				return;
			}
			var attrs = target.Attributes;
			const string key = "HP";
			if (!attrs.Has(key)) attrs.Add(key, 0f);
			if (attrs.TryGetValue(key, out var hp))
			{
				float newHp = hp + heal;
				attrs.SetValue(key, newHp);
				Debug.Log($"[治疗] {GetName(context.Source)} -> {GetName(context.Target)} 回复 {heal} 点生命，HP: {hp} -> {newHp}");
			}
		}


		private string GetName(IUnit unit)
		{
			if (unit is UnityEngine.Component c) return c.gameObject.name;
			return unit?.Name ?? "未知单位";
		}
	}
}

