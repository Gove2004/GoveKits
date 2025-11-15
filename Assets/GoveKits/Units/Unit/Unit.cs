

using System.Collections.Generic;
using UnityEngine;

namespace GoveKits.Units
{
    public interface IUnit
    {
        string Name { get; }
        AttributeContainer Attributes { get; }
        BuffContainer buffs { get; }
        AbilityContainer Abilities { get; }
        // BuffContainer Buffs { get; }
    }


    public abstract class Unit : IUnit
    {
        public string Name { get; } = "Unit";
        public AttributeContainer Attributes { get; } = new AttributeContainer();
        public BuffContainer buffs { get; } = new BuffContainer();
        public AbilityContainer Abilities { get; } = new AbilityContainer();
        // public BuffContainer Buffs { get; } = new BuffContainer();
    }


    public class UnitComponent : MonoBehaviour, IUnit
    {
        public string Name => "UnitComponent";
        public AttributeContainer Attributes { get; } = new AttributeContainer();
        public BuffContainer buffs { get; } = new BuffContainer();
        public AbilityContainer Abilities { get; } = new AbilityContainer();
        // public BuffContainer Buffs { get; } = new BuffContainer();

        private void Awake()
        {

            // Buffs = new BuffContainer();
        }

        private void OnDestroy()
        {
            Attributes.Clear();
            buffs.Clear();
            Abilities.Clear();
            // Buffs = null;
        }

        
    }
}