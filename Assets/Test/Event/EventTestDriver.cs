using UnityEngine;

namespace GoveKits.Test.Event
{
    public class EventTestDriver : MonoBehaviour
    {
        [Header("Bind Components")]
        public EventTestPriorityAndBreak priorityAndBreak;
        public EventTestBusIsolation busIsolation;

        [Header("Auto Run")]
        public bool runOnStart = true;

        private void Start()
        {
            if (runOnStart)
            {
                RunAll();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) priorityAndBreak?.RunPriorityTest();
            if (Input.GetKeyDown(KeyCode.Alpha2)) priorityAndBreak?.RunBreakTest();
            if (Input.GetKeyDown(KeyCode.Alpha3)) priorityAndBreak?.RunHealTest();
            if (Input.GetKeyDown(KeyCode.Alpha4)) busIsolation?.RunBusIsolationTest();
            if (Input.GetKeyDown(KeyCode.Alpha0)) RunAll();
        }

        [ContextMenu("Run All Event Tests")]
        public void RunAll()
        {
            priorityAndBreak?.RunPriorityTest();
            priorityAndBreak?.RunBreakTest();
            priorityAndBreak?.RunHealTest();
            busIsolation?.RunBusIsolationTest();
            Debug.Log("[EventTest] RunAll done.");
        }
    }
}
