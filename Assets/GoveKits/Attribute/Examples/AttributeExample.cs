using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 使用示例：演示 AttributeContainer、Attribute 与 AttributeModifier 的常见用法
// 将此脚本挂到任意 GameObject，运行时在控制台观察输出

namespace GoveKits.Attribute.Examples
{
    public class AttributeExample : MonoBehaviour
    {
        private AttributeContainer container;

        void Start()
        {
            container = new AttributeContainer();

            // 添加属性
            container.AddAttribute("Strength", 10f);
            container.AddAttribute("BaseAttack", 5f);

            // 订阅 Strength 的变化，用于驱动 Attack（通过修改 BaseAttack）
            var strAttr = container.GetAttribute("Strength");
            strAttr.onValueChanged += (newVal) =>
            {
                Debug.Log($"[Example] Strength changed -> {newVal}");
                // 假设 Attack = BaseAttack + Strength * 2
                var attackBase = container.GetAttribute("BaseAttack");
                attackBase.Base = 5f + newVal * 2f;
            };

            // 初始打印
            Debug.Log($"Initial Strength: {container.GetValue("Strength")}");
            Debug.Log($"Initial Attack (base): {container.GetValue("BaseAttack")}");

            // 给 Strength 添加一个临时加成
            var buff = new AttributeModifier(ModifierType.Add, 5f); // +5
            container.AddModifierToAttribute("Strength", buff);

            // Access Value triggers recalculation and fires event
            Debug.Log($"After buff Strength: {container.GetValue("Strength")}");
            Debug.Log($"After buff Attack (base): {container.GetValue("BaseAttack")}");

            // 添加一个乘法修正器到 Attack（例如 1.1x）
            var weaponMul = new AttributeModifier(ModifierType.Multiply, 1.1f);
            container.AddModifierToAttribute("BaseAttack", weaponMul);
            Debug.Log($"Attack after weapon multiplier: {container.GetValue("BaseAttack")}");

            // 移除 Strength 的 buff
            container.RemoveModifierFromAttribute("Strength", buff);
            Debug.Log($"After buff removed Strength: {container.GetValue("Strength")}");
            Debug.Log($"After buff removed Attack (base): {container.GetValue("BaseAttack")}");

            // 列出所有属性键
            var keys = container.GetAllKeys();
            Debug.Log("All attributes: " + string.Join(", ", keys));
        }
    }
}
