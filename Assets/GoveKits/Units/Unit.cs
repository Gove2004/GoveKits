

using UnityEngine;

namespace GoveKits.Units
{
    public interface IUnit
    {
        string Name { get; }
        AttributeContainer Attributes { get; }
        GameplayTagContainer Tags { get; }
        AbilityContainer Abilities { get; }
        // BuffContainer Buffs { get; }
    }


    public abstract class Unit : IUnit
    {
        public string Name { get; } = "Unit";
        public AttributeContainer Attributes { get; } = new AttributeContainer();
        public GameplayTagContainer Tags { get; } = new GameplayTagContainer();
        public AbilityContainer Abilities { get; } = new AbilityContainer();
        // public BuffContainer Buffs { get; } = new BuffContainer();
    }


    public class UnitComponent : MonoBehaviour, IUnit
    {
        public string Name => "UnitComponent";
        public AttributeContainer Attributes { get; } = new AttributeContainer();
        public GameplayTagContainer Tags { get; } = new GameplayTagContainer();
        public AbilityContainer Abilities { get; } = new AbilityContainer();
        // public BuffContainer Buffs { get; } = new BuffContainer();

        private void Awake()
        {

            // Buffs = new BuffContainer();
        }

        private void OnDestroy()
        {
            Attributes.Clear();
            Tags.Clear();
            Abilities.Clear();
            // Buffs = null;
        }
    }
}