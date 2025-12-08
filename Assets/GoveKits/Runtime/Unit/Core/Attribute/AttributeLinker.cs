using System;


namespace GoveKits.Unit
{
    public static class AttributeLinker
    {
        /// <summary>
        /// 建立单向链接：Source 变化时，自动更新 Target 的 Modifier
        /// <para>例如：Stamina (Source) -> MaxHP (Target)</para>
        /// </summary>
        /// <param name="source">源属性 (Stamina)</param>
        /// <param name="target">目标属性 (MaxHP)</param>
        /// <param name="convertFunc">转换公式 (val => val * 3)</param>
        public static void Link(StateAttribute source, StateAttribute target, Func<float, float> convertFunc)
        {
            // 1. 定义更新逻辑
            void OnSourceChanged(float oldVal, float newVal)
            {
                // 先移除旧的 modifier (利用 source 对象进行匹配移除)
                target.RemoveBySource(source);

                // 计算新的加成值
                float bonus = convertFunc(newVal);

                // 创建新 modifier (类型通常是 Flat)
                // 注意：Source 字段填的是 source 属性对象，这样方便后续查找移除
                var mod = new GameModifier(ModifierType.Flat, bonus, source);
                
                target.AddModifier(mod);
            }

            // 2. 立即执行一次，初始化数值
            OnSourceChanged(0, source.Value);

            // 3. 订阅事件 (当体力变化时，自动更新血量)
            source.OnValueChanged += OnSourceChanged;
        }
    }
}