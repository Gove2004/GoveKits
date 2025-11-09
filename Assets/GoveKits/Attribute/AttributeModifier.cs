

namespace GoveKits.Attribute
{
    public enum ModifierType
    {
        Add,  // 加法, 优先级100
        Multiply,  // 乘法, 优先级10
        Override  // 覆盖, 优先级1
    }


    public class AttributeModifier
    {
        public readonly ModifierType modifierType;
        public readonly int priority;
        public readonly float value;  // 修正值, +0, x1, 覆盖值

        public AttributeModifier(ModifierType modifierType, float value, int priority = 0)
        {
            this.modifierType = modifierType;
            this.value = value;

            // 如果外部未显式指定优先级（传入 0），则采用基于类型的默认优先级；
            // 否则使用外部提供的优先级（允许覆盖）。
            if (priority != 0)
            {
                this.priority = priority;
            }
            else
            {
                switch (modifierType)
                {
                    case ModifierType.Add:
                        this.priority = 100;
                        break;
                    case ModifierType.Multiply:
                        this.priority = 10;
                        break;
                    case ModifierType.Override:
                        this.priority = 1;
                        break;
                    default:
                        this.priority = 0;
                        break;
                }
            }
        }

        // 便于在容器中比较/移除同一修正器实例或等价修正器
        public override bool Equals(object obj)
        {
            if (obj is AttributeModifier other)
            {
                return modifierType == other.modifierType
                    && priority == other.priority
                    && value == other.value;
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + modifierType.GetHashCode();
                hash = hash * 23 + priority.GetHashCode();
                hash = hash * 23 + value.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return $"AttributeModifier(Type={modifierType}, Value={value}, Priority={priority})";
        }

        public float Apply(float baseValue)
        {
            switch (modifierType)
            {
                case ModifierType.Add:
                    return baseValue + value;
                case ModifierType.Multiply:
                    return baseValue * value;
                case ModifierType.Override:
                    return value;
                default:
                    return baseValue;
            }
        }
    }
}