


using System.Collections.Generic;

namespace GoveKits.Runtime.Unit
{
    public class AbilityContainer : IUnitTagSource
    {
        private readonly Dictionary<UnitTag, UnitAbility> _abilitys = new();
        public bool HasTag(UnitTag tag) => _abilitys.ContainsKey(tag);
    }
}