using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using GoveKits.Unit;

namespace GoveKits.Unit.Tests
{
    public class StatusSetUnityTests
    {
        [UnityTest]
        public IEnumerator Unity_AppendAndGet_WorksAcrossFrame()
        {
            var ss = new StatusSet<string, float>();
            ss.Append(("A", new Status<float>(1f)));
            yield return null;
            Assert.IsTrue(ss.ContainsKey("A"));
            Assert.AreEqual(1f, ss.Get("A"));
        }

        [UnityTest]
        public IEnumerator Unity_Set_InvokesListenerImmediately()
        {
            var ss = new StatusSet<string, float>();
            ss.Append(("A", new Status<float>(1f)));
            bool invoked = false;
            ss.AddListener("A", (o, n) => invoked = true);

            ss.Set("A", 5f);
            // listener fires synchronously in current impl
            Assert.IsTrue(invoked);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Unity_CustomGetter_ComputesFromDependency()
        {
            var ss = new StatusSet<string, float>();
            ss.Append(("X", new Status<float>(2f)));
            ss.Append(("Y", new Status<float>(0f, () => ss.Get("X") + 3f, null)));
            ss.AddDependency("Y", "X");

            yield return null;
            Assert.AreEqual(5f, ss.Get("Y"));
        }

        [UnityTest]
        public IEnumerator Unity_RemoveKey_PreventsSet()
        {
            var ss = new StatusSet<string, float>();
            ss.Append(("A", new Status<float>(1f)));
            ss.Remove("A");
            yield return null;
            Assert.IsFalse(ss.ContainsKey("A"));
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => ss.Set("A", 2f));
        }

        [UnityTest]
        public IEnumerator Unity_AddDependency_PreventsCycle()
        {
            var ss = new StatusSet<string, float>();
            ss.Append(("A", new Status<float>(1f)));
            ss.Append(("B", new Status<float>(2f)));
            ss.Append(("C", new Status<float>(3f)));

            // B depends on A, C depends on B
            ss.AddDependency("B", "A");
            ss.AddDependency("C", "B");

            // Adding C depends on A would create A->B->C->A cycle
            Assert.Throws<System.InvalidOperationException>(() => ss.AddDependency("A", "C"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Unity_Clear_RemovesListeners()
        {
            var ss = new StatusSet<string, float>();
            ss.Append(("A", new Status<float>(1f)));
            bool invoked = false;
            ss.AddListener("A", (o, n) => invoked = true);

            ss.Clear();
            yield return null;
            // after Clear, setting previously existing key will throw
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => ss.Set("A", 2f));
            Assert.IsFalse(invoked);
        }

        [UnityTest]
        public IEnumerator Unity_AddAndRemoveListener_CoroutineStyle()
        {
            var ss = new StatusSet<string, float>();
            ss.Append(("A", new Status<float>(1f)));
            bool invoked = false;
            System.Action<float, float> l = (o, n) => invoked = true;
            ss.AddListener("A", l);
            ss.Set("A", 4f);
            Assert.IsTrue(invoked);

            invoked = false;
            ss.RemoveListener("A", l);
            ss.Set("A", 6f);
            Assert.IsFalse(invoked);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Unity_CountAndKeysReflectAppendRemove()
        {
            var ss = new StatusSet<string, float>();
            ss.Append(("A", new Status<float>(1f)));
            ss.Append(("B", new Status<float>(2f)));
            yield return null;
            Assert.AreEqual(2, ss.Count);
            ss.Remove("A");
            yield return null;
            Assert.AreEqual(1, ss.Count);
            Assert.IsTrue(ss.ContainsKey("B"));
        }

        [UnityTest]
        public IEnumerator Unity_ChainedDependencies_LazyEvaluation()
        {
            var ss = new StatusSet<string, float>();
            ss.Append(("A", new Status<float>(1f)));
            ss.Append(("B", new Status<float>(0f, () => ss.Get("A") + 1f, null)));
            ss.Append(("C", new Status<float>(0f, () => ss.Get("B") + 1f, null)));
            ss.AddDependency("B", "A");
            ss.AddDependency("C", "B");

            ss.Set("A", 5f);
            // listeners are lazy; computing C via Get should reflect change
            Assert.AreEqual(7f, ss.Get("C"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Unity_Stress_MultipleSetsAcrossFrames()
        {
            var ss = new StatusSet<string, float>();
            ss.Append(("Count", new Status<float>(0f)));
            for (int i = 0; i < 20; i++)
            {
                ss.Set("Count", i);
                yield return null;
                Assert.AreEqual(i, ss.Get("Count"));
            }
        }
    }
}



