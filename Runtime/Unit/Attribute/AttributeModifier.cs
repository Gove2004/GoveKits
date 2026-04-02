namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 属性修改器加成类型
    /// </summary>
    public enum ModifierType
    {
        /// <summary>
        /// 加法修改
        /// 直接加减固定数值
        /// </summary>
        Additive,
        
        /// <summary>
        /// 乘法修改
        /// 按百分比增减，存储为小数形式
        /// </summary>
        Multiplicative,
        
        /// <summary>
        /// 覆盖修改
        /// 强制设置属性为指定值，忽略其他修改器
        /// </summary>
        Override
    }

    /// <summary>
    /// 属性修改器来源
    /// </summary>
    public abstract class ModifierSource
    {
    }

    /// <summary>
    /// 属性修改器 (0 GC Struct)
    /// </summary>
    public readonly struct AttributeModifier
    {
        /// <summary>
        /// 修改器类型
        /// 决定计算方式（加法/乘法/覆盖）
        /// </summary>
        public readonly ModifierType Type;
        
        /// <summary>
        /// 修改器数值
        /// 
        /// 数值说明：
        /// - Additive：固定数值（如 +50）
        /// - Multiplicative：小数形式（如 +20% 存储为 0.2）
        /// - Override：目标值（如 强行设为 100）
        /// </summary>
        public readonly float Value;
        
        /// <summary>
        /// 修改器来源
        /// </summary>
        public readonly ModifierSource Source;

        /// <summary>
        /// 创建属性修改器
        /// </summary>
        /// <param name="type">修改器类型（加法/乘法/覆盖）</param>
        /// <param name="value">修改器数值</param>
        /// <param name="source">修改器来源（用于精准移除）</param>
        public AttributeModifier(ModifierType type, float value, ModifierSource source = null)
        {
            Type = type;
            Value = value;
            Source = source;
        }
    }
}