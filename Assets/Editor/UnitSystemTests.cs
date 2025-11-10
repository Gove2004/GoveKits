using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Cysharp.Threading.Tasks;
using GoveKits.Units;

namespace GoveKits.Tests
{
    [TestFixture]
    public class UnitSystemTests
    {
        // Helper test unit
        private class TestUnit : Unit { }

        // Simple mock ability for testing
        private class MockAbility : IAbility
        {
            public bool ConditionCalled;
            public bool CostCalled;
            public bool ExecuteCalled;
            public bool CancelCalled;
            private readonly bool _conditionResult;
            private readonly bool _executeResult;
            private readonly bool _costThrows;

            public MockAbility(bool conditionResult = true, bool executeResult = true, bool costThrows = false)
            {
                _conditionResult = conditionResult;
                _executeResult = executeResult;
                _costThrows = costThrows;
            }

            public bool Execute(AbilityContext context)
            {
                ExecuteCalled = true;
                return _executeResult;
            }

            public void Cost(AbilityContext context)
            {
                CostCalled = true;
                if (_costThrows) throw new InvalidOperationException("cost failed");
            }

            public void Cancel(AbilityContext context)
            {
                CancelCalled = true;
            }

            public bool Condition(AbilityContext context)
            {
                ConditionCalled = true;
                return _conditionResult;
            }
        }

        [Test]
        public void GameplayTag_Equals_SameName()
        {
            var a = new GameplayTag("tag");
            var b = new GameplayTag("tag");
            Assert.AreEqual(a, b);
        }

        [Test]
        public void GameplayTag_ImplicitConversionFromString()
        {
            GameplayTag t = "mytag";
            Assert.AreEqual("mytag", t.ToString());
        }

        [Test]
        public void AttributeContainer_AddAndGetValue()
        {
            var c = new AttributeContainer();
            c.AddValue("hp", 10f);
            Assert.AreEqual(10f, c.GetValue("hp"));
        }

        [Test]
        public void AttributeContainer_SetValue_OnReadOnly_Throws()
        {
            var c = new AttributeContainer();
            c.AddValue("maxHp", 100f, () => 50f);
            Assert.Throws<InvalidOperationException>(() => c.SetValue("maxHp", 200f));
        }

        [Test]
        public void AttributeContainer_Has_ReturnsCorrect()
        {
            var c = new AttributeContainer();
            c.AddValue("a", 1f);
            Assert.IsTrue(c.Has("a"));
            Assert.IsFalse(c.Has("b"));
        }

        [Test]
        public void AttributeContainer_Dependency_Recalculation()
        {
            var c = new AttributeContainer();
            c.AddValue("a", 10f);
            c.AddAttribute("b", new Units.Attribute("b", 0f, () => c.GetValue("a") * 2f), new List<string> { "a" });
            c.SetValue("a", 20f);
            Assert.AreEqual(40f, c.GetValue("b"));
        }

        [Test]
        public void AttributeContainer_Clear_RemovesAttributes()
        {
            var c = new AttributeContainer();
            c.AddValue("x", 5f);
            c.Clear();
            Assert.Throws<KeyNotFoundException>(() => c.GetValue("x"));
        }

        [Test]
        public void Attribute_Operators_And_ImplicitConversion()
        {
            var a = new Units.Attribute("a", 3f);
            var b = new Units.Attribute("b", 2f);
            Assert.AreEqual(5f, a + b);
            Assert.AreEqual(1f, a - b);
            Assert.AreEqual(6f, a * b);
            Assert.AreEqual(1.5f, a / b);
            float fa = a;
            Assert.AreEqual(3f, fa);
        }

        [Test]
        public void DependencyContainer_AddAndGetDependents()
        {
            var d = new DependencyContainer<string>();
            d.AddDependency("b", "a");
            var deps = d.GetDependents("a");
            CollectionAssert.Contains(deps, "b");
        }

        [Test]
        public void DependencyContainer_CircularDependency_Throws()
        {
            var d = new DependencyContainer<string>();
            d.AddDependency("a", "b");
            Assert.Throws<InvalidOperationException>(() => d.AddDependency("b", "a"));
        }

        [Test]
        public void AbilityContainer_Add_Remove_CountAndKeys()
        {
            var u = new TestUnit();
            var container = new AbilityContainer();
            var ability = new MockAbility();
            container.AddAbility("fire", ability);
            Assert.IsTrue(container.Has("fire"));
            Assert.AreEqual(1, container.Count);
            CollectionAssert.Contains(container.Keys, "fire");
            container.RemoveAbility("fire");
            Assert.IsFalse(container.Has("fire"));
        }

        // [Test]
        // public void AbilityContainer_Add_Duplicate_Throws()
        // {
        //     var u = new TestUnit();
        //     var c = new AbilityContainer();
        //     c.AddAbility("a", new MockAbility());
        //     Assert.Throws<InvalidOperationException>(() => c.AddAbility("a", new MockAbility()));
        // }

        [Test]
        public void AbilityContainer_Remove_Nonexistent_Throws()
        {
            var u = new TestUnit();
            var c = new AbilityContainer();
            Assert.Throws<KeyNotFoundException>(() => c.RemoveAbility("no"));
        }

        [Test]
        public void AbilityContainer_ExecuteAbility_Succeeds_WhenConditionTrue()
        {
            var u = new TestUnit();
            var target = new TestUnit();
            var c = new AbilityContainer();
            var ability = new MockAbility(conditionResult: true, executeResult: true);
            c.AddAbility("go", ability);
            var result = c.ExecuteAbility("go", target, null);
            Assert.IsTrue(result);
            Assert.IsTrue(ability.ConditionCalled);
            Assert.IsTrue(ability.CostCalled);
            Assert.IsTrue(ability.ExecuteCalled);
        }

        [Test]
        public void AbilityContainer_ExecuteAbility_Fails_WhenConditionFalse()
        {
            var u = new TestUnit();
            var target = new TestUnit();
            var c = new AbilityContainer();
            var ability = new MockAbility(conditionResult: false);
            c.AddAbility("go", ability);
            var result = c.ExecuteAbility("go", target, null);
            Assert.IsFalse(result);
            Assert.IsTrue(ability.ConditionCalled);
            Assert.IsFalse(ability.CostCalled);
            Assert.IsFalse(ability.ExecuteCalled);
        }

        [Test]
        public void AbilityContainer_ExecuteAbility_Exception_CallsCancel_And_Throws()
        {
            var u = new TestUnit();
            var target = new TestUnit();
            var c = new AbilityContainer();
            var ability = new MockAbility(conditionResult: true, executeResult: true, costThrows: true);
            c.AddAbility("boom", ability);
            Assert.Throws<Exception>(() => c.ExecuteAbility("boom", target, null));
            Assert.IsTrue(ability.CostCalled);
            Assert.IsTrue(ability.CancelCalled);
        }

        [Test]
        public async Task Effect_Immediate_ExecutesAction()
        {
            var src = new TestUnit();
            var tgt = new TestUnit();
            var ctx = new EffectContext(src, tgt);
            bool called = false;
            var effect = ComposeEffect.Immediate(c => called = true);
            await effect.Apply(ctx).AsTask();
            Assert.IsTrue(called);
        }

        [Test]
        public async Task Effect_Sequence_ExecutesInOrder()
        {
            var src = new TestUnit();
            var tgt = new TestUnit();
            var ctx = new EffectContext(src, tgt);
            var list = new List<int>();
            var seq = ComposeEffect.Sequence(
                ComposeEffect.Immediate(c => list.Add(1)),
                ComposeEffect.Immediate(c => list.Add(2)),
                ComposeEffect.Immediate(c => list.Add(3))
            );
            await seq.Apply(ctx).AsTask();
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, list);
        }

        [Test]
        public async Task Effect_If_ExecutesThenOrElse()
        {
            var src = new TestUnit();
            var tgt = new TestUnit();
            var ctx = new EffectContext(src, tgt);
            bool thenCalled = false;
            bool elseCalled = false;
            var effect = ComposeEffect.If(c => true, ComposeEffect.Immediate(c => thenCalled = true), ComposeEffect.Immediate(c => elseCalled = true));
            await effect.Apply(ctx).AsTask();
            Assert.IsTrue(thenCalled);
            Assert.IsFalse(elseCalled);
        }
    }
}
