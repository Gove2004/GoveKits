using GoveKits.Runtime.Core.Event;

namespace GoveKits.Test.Event
{
    public class DamageEvent : EventInfo
    {
        public int Amount;
        public string Source;

        public override void OnRecycle()
        {
            Amount = 0;
            Source = null;
        }
    }

    public class HealEvent : EventInfo
    {
        public int Amount;
        public string Source;

        public override void OnRecycle()
        {
            Amount = 0;
            Source = null;
        }
    }
}
