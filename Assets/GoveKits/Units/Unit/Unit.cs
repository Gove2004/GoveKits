

using System.Collections.Generic;
using UnityEngine;

namespace GoveKits.Units
{
    public interface IUnit
    {
        string Name { get; }
        AttributeContainer Attributes { get; }
        MarkContainer Marks { get; }
        AbilityContainer Abilities { get; }
        // MarkContainer Marks { get; }
    }


    public abstract class Unit : IUnit
    {
        public string Name { get; } = "Unit";
        public AttributeContainer Attributes { get; } = new AttributeContainer();
        public MarkContainer Marks { get; } = new MarkContainer();
        public AbilityContainer Abilities { get; } = new AbilityContainer();
        // public MarkContainer Marks { get; } = new MarkContainer();
    }


    public class UnitComponent : MonoBehaviour, IUnit
    {
        public string Name => "UnitComponent";
        public AttributeContainer Attributes { get; } = new AttributeContainer();
        public MarkContainer Marks { get; } = new MarkContainer();
        public AbilityContainer Abilities { get; } = new AbilityContainer();
        // public MarkContainer Marks { get; } = new MarkContainer();

        private void Awake()
        {

            // Marks = new MarkContainer();
        }

        private void OnDestroy()
        {
            Attributes.Clear();
            Marks.Clear();
            Abilities.Clear();
            // Marks = null;
        }

        
    }
}