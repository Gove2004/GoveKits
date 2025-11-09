using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using GoveKits.Unit;

namespace GoveKits.Unit.Tests
{
    public class StatusSetComprehensiveTests
    {
        [Test]
        public void AppendAndGet_Works()
        {
            var ss = new StatusSet<string, float>();
            ss.Append(("A", new Status<float>(1f)));
            Assert.IsTrue(ss.ContainsKey("A"));
            Assert.AreEqual(1f, ss.Get("A"));
        }

        [Test]
        public void Set_InvokesListener_Synchronously()
        {
            var ss = new StatusSet<string, float>();
            ss.Append(("A", new Status<float>(1f)));
            bool invoked = false;
            ss.AddListener("A", (o, n) => invoked = true);

            ss.Set("A", 5f);
            Assert.IsTrue(invoked);
        }

        [Test]
        public void CustomGetter_ComputesFromDependency()
        {
            var ss = new StatusSet<string, float>();
            ss.Append(("X", new Status<float>(2f)));
            ss.Append(("Y", new Status<float>(0f, () => ss.Get("X") + 3f, null)));
            ss.AddDependency("Y", "X");

            Assert.AreEqual(5f, ss.Get("Y"));
        }

        [Test]
        public void RemoveKey_PreventsSet()
        {
            var ss = new StatusSet<string, float>();
            ss.Append(("A", new Status<float>(1f)));
            ss.Remove("A");
            Assert.IsFalse(ss.ContainsKey("A"));
            Assert.Throws<KeyNotFoundException>(() => ss.Set("A", 2f));
        }

        [Test]
        public void AddDependency_PreventsCycle()
        {
            var ss = new StatusSet<string, float>();
            ss.Append(("A", new Status<float>(1f)));
            ss.Append(("B", new Status<float>(2f)));
            ss.Append(("C", new Status<float>(3f)));

            ss.AddDependency("B", "A");
            ss.AddDependency("C", "B");

            Assert.Throws<InvalidOperationException>(() => ss.AddDependency("A", "C"));
        }

        [Test]
        public void Clear_RemovesListenersAndData()
        {
            var ss = new StatusSet<string, float>();
            ss.Append(("A", new Status<float>(1f)));
            bool invoked = false;
            ss.AddListener("A", (o, n) => invoked = true);

            ss.Clear();
            Assert.Throws<KeyNotFoundException>(() => ss.Set("A", 2f));
            Assert.IsFalse(invoked);
        }

        [Test]
        public void AddAndRemoveListener_Works()
        {
            var ss = new StatusSet<string, float>();
            ss.Append(("A", new Status<float>(1f)));
            bool invoked = false;
            Action<float, float> l = (o, n) => invoked = true;
            ss.AddListener("A", l);
            ss.Set("A", 4f);
            Assert.IsTrue(invoked);

            invoked = false;
            ss.RemoveListener("A", l);
            ss.Set("A", 6f);
            Assert.IsFalse(invoked);
        }

        [Test]
        public void CountAndKeysReflectAppendRemove()
        {
            var ss = new StatusSet<string, float>();
            ss.Append(("A", new Status<float>(1f)));
            ss.Append(("B", new Status<float>(2f)));
            Assert.AreEqual(2, ss.Count);
            ss.Remove("A");
            Assert.AreEqual(1, ss.Count);
            Assert.IsTrue(ss.ContainsKey("B"));
        }

        [Test]
        public void ChainedDependencies_LazyEvaluation()
        {
            var ss = new StatusSet<string, float>();
            ss.Append(("A", new Status<float>(1f)));
            ss.Append(("B", new Status<float>(0f, () => ss.Get("A") + 1f, null)));
            ss.Append(("C", new Status<float>(0f, () => ss.Get("B") + 1f, null)));
            ss.AddDependency("B", "A");
            ss.AddDependency("C", "B");

            ss.Set("A", 5f);
            Assert.AreEqual(7f, ss.Get("C"));
        }

        [Test]
        public void Stress_MultipleSetsAcrossThreads_NoExceptions()
        {
            var ss = new StatusSet<string, int>();
            ss.Append(("Count", new Status<int>(0)));

            var exceptions = new List<Exception>();
            var tasks = new List<Task>();
            int writers = 8;
            int iterations = 1000;

            for (int w = 0; w < writers; w++)
            {
                int id = w;
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        for (int i = 0; i < iterations; i++)
                        {
                            ss.Set("Count", id * iterations + i);
                            var v = ss.Get("Count"); // may be racing, but should not throw
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());

            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }

            // final value should be within one of the writers' ranges
            var final = ss.Get("Count");
            bool inRange = false;
            for (int w = 0; w < writers; w++)
            {
                int lo = w * iterations;
                int hi = lo + iterations - 1;
                if (final >= lo && final <= hi) { inRange = true; break; }
            }
            Assert.IsTrue(inRange, "Final value is not within expected ranges (concurrent writes)");
        }

        [Test]
        public void ConcurrentAddRemoveListeners_DoesNotThrow()
        {
            var ss = new StatusSet<string, int>();
            ss.Append(("A", new Status<int>(0)));

            int tasksCount = 32;
            var tasks = new List<Task>();
            var exceptions = new List<Exception>();

            for (int i = 0; i < tasksCount; i++)
            {
                int id = i;
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        Action<int, int> listener = (o, n) => { /* noop */ };
                        for (int j = 0; j < 100; j++)
                        {
                            ss.AddListener("A", listener);
                            ss.Set("A", j);
                            ss.RemoveListener("A", listener);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions) { exceptions.Add(ex); }
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());
            if (exceptions.Count > 0) throw new AggregateException(exceptions);
            Assert.Pass();
        }

        [Test]
        public void DependencyPropagation_OrderAndDirtyBehavior()
        {
            var ss = new StatusSet<string, float>();
            ss.Append(("A", new Status<float>(1f)));
            ss.Append(("B", new Status<float>(0f, () => ss.Get("A") + 1f, null)));
            ss.Append(("C", new Status<float>(0f, () => ss.Get("B") + 1f, null)));
            ss.AddDependency("B", "A");
            ss.AddDependency("C", "B");

            // initial
            Assert.AreEqual(1f, ss.Get("A"));
            Assert.AreEqual(2f, ss.Get("B"));
            Assert.AreEqual(3f, ss.Get("C"));

            ss.Set("A", 10f);
            // B and C should be lazy and updated on Get
            Assert.AreEqual(11f, ss.Get("B"));
            Assert.AreEqual(12f, ss.Get("C"));
        }

        [Test]
        public void WouldCreateCycle_DetectsCycleDeterministically()
        {
            var map = new StatusSet<string, float>();
            // B depends on A, C depends on B  (B -> A, C -> B)
            map.Append(("A", new Status<float>(1f)));
            map += ("B", new Status<float>(2f));
            map += ("C", new Status<float>(3f));
            map.AddDependency("B", "A");
            map.AddDependency("C", "B");
            Assert.Throws<InvalidOperationException>(() => map.AddDependency("A", "C"));
            Assert.Throws<InvalidOperationException>(() => map.AddDependency("A", "B"));
        }
    }
}
