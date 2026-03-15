using System.Collections.Generic;
using GoveKits.Runtime.Core.Pool;
using NUnit.Framework;
using UnityEngine;

namespace GoveKits.Tests.Pool
{
    public class PoolCoreTests
    {
        private readonly List<GameObject> _createdPrefabs = new();

        [SetUp]
        public void SetUp()
        {
            PoolCore.ClearAll();
            CSharpPoolProbe.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            PoolCore.ClearAll();

            foreach (var prefab in _createdPrefabs)
            {
                if (prefab != null)
                {
                    Object.DestroyImmediate(prefab);
                }
            }
            _createdPrefabs.Clear();

            foreach (var probe in Object.FindObjectsOfType<GameObjectPoolProbe>(true))
            {
                if (probe != null)
                {
                    Object.DestroyImmediate(probe.gameObject);
                }
            }
        }

        [Test]
        public void CSharpPool_Create_WarmupCreatesExpectedInstances()
        {
            PoolCore.Create<CSharpPoolProbe>(count: 3, maxSize: 8);

            Assert.AreEqual(3, CSharpPoolProbe.CreatedCount);
        }

        [Test]
        public void CSharpPool_GetReturn_ReusesSameInstance()
        {
            var item = PoolCore.Get<CSharpPoolProbe>();
            var firstId = item.InstanceId;

            PoolCore.Return(item);
            var again = PoolCore.Get<CSharpPoolProbe>();

            Assert.AreEqual(firstId, again.InstanceId);
            Assert.AreEqual(2, again.GetCount);
            Assert.AreEqual(1, again.ReturnCount);
        }

        [Test]
        public void GameObjectPool_GetReturn_TogglesActiveAndInvokesCallbacks()
        {
            var prefab = CreatePoolablePrefab();
            var pool = PoolCore.Create(prefab);
            var instance = PoolCore.Get(prefab);
            var probe = instance.GetComponent<GameObjectPoolProbe>();

            Assert.IsNotNull(probe);
            Assert.IsTrue(instance.activeSelf);
            Assert.AreEqual(1, probe.GetCount);

            // Use direct pool return to avoid relying on PoolRecord lookup behavior.
            pool.Return(instance);

            Assert.IsFalse(instance.activeSelf);
            Assert.AreEqual(1, probe.ReturnCount);
        }

        [Test]
        public void GameObjectPool_ReturnNonPooledObject_ThrowsArgumentException()
        {
            var externalObj = new GameObject("ExternalObject");

            Assert.Throws<System.ArgumentException>(() => PoolCore.Return(externalObj));

            Object.DestroyImmediate(externalObj);
        }

        private GameObject CreatePoolablePrefab()
        {
            var prefab = new GameObject("PoolablePrefab");
            prefab.AddComponent<PoolRecord>();
            prefab.AddComponent<GameObjectPoolProbe>();
            _createdPrefabs.Add(prefab);
            return prefab;
        }

        private sealed class CSharpPoolProbe : IPoolable
        {
            public static int CreatedCount { get; private set; }

            public int InstanceId { get; }
            public int GetCount { get; private set; }
            public int ReturnCount { get; private set; }

            public CSharpPoolProbe()
            {
                CreatedCount++;
                InstanceId = CreatedCount;
            }

            public static void Reset()
            {
                CreatedCount = 0;
            }

            public void OnGetFromPool()
            {
                GetCount++;
            }

            public void OnReturnToPool()
            {
                ReturnCount++;
            }
        }

        private sealed class GameObjectPoolProbe : MonoBehaviour, IPoolable
        {
            public int GetCount { get; private set; }
            public int ReturnCount { get; private set; }

            public void OnGetFromPool()
            {
                GetCount++;
            }

            public void OnReturnToPool()
            {
                ReturnCount++;
            }
        }
    }
}
