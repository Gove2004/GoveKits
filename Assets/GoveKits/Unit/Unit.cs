

namespace GoveKits.Units
{
    public abstract class Unit
    {
        string Name { get; set; }
        AttributeContainer Attributes { get; }
        GameplayTagContainer Tags { get; }
        AbilityContainer Abilities { get; }
        // public BuffContainer Buffs { get; } = new BuffContainer();
    }
}