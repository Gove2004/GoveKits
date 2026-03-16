using GoveKits.Runtime.Core.Event;
using UnityEngine;

namespace GoveKits.Test.Event
{
    public class EventTestBusIsolation : MonoBehaviour
    {
        public string busA = "main";
        public string busB = "combat";

        private DisposeAction _subA;
        private DisposeAction _subB;
        private int _receivedA;
        private int _receivedB;

        private void OnEnable()
        {
            _subA = EventCore.Subscribe<DamageEvent>(e =>
            {
                _receivedA++;
                Debug.Log($"[EventTest] BusA({busA}) recv: {e.Amount} from {e.Source}");
            }, busName: busA);

            _subB = EventCore.Subscribe<DamageEvent>(e =>
            {
                _receivedB++;
                Debug.Log($"[EventTest] BusB({busB}) recv: {e.Amount} from {e.Source}");
            }, busName: busB);
        }

        private void OnDisable()
        {
            _subA?.Dispose();
            _subA = null;

            _subB?.Dispose();
            _subB = null;
        }

        public void RunBusIsolationTest()
        {
            _receivedA = 0;
            _receivedB = 0;

            EventCore.Publish<DamageEvent>(e =>
            {
                e.Amount = 1;
                e.Source = "BusAOnly";
            }, busName: busA);

            EventCore.Publish<DamageEvent>(e =>
            {
                e.Amount = 2;
                e.Source = "BusBOnly";
            }, busName: busB);

            if (_receivedA != 1 || _receivedB != 1)
            {
                Debug.LogError($"[EventTest] Bus isolation failed. recvA={_receivedA}, recvB={_receivedB}");
            }
            else
            {
                Debug.Log("[EventTest] Bus isolation passed.");
            }
        }
    }
}
