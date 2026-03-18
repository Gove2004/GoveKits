

namespace GoveKits.Runtime.Unit
{
    public abstract class UnitAbility
    {
        public abstract UnitTag Name { get; }

        public IUnit Owner { get; private set; }

        public void Initialize(IUnit owner)
        {
            Owner = owner;
            OnInitialize();
        }

        protected virtual void OnInitialize() { }

        public abstract void Execute(UnitContext context);
    }

}