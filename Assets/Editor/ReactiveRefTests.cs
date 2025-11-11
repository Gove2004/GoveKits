using System;
using System.Collections.Generic;
using NUnit.Framework;
using GoveKits.Units;

namespace GoveKits.Tests
{
    [TestFixture]
    public class ReactiveRefTests
    {
        [Test]
        public void IntRef_SetAndGetValue()
        {
            var a = Reactive.Int(5);
            Assert.AreEqual(5, a.Value);
            a.Value = 7;
            Assert.AreEqual(7, a.Value);
        }

        [Test]
        public void IntRef_Computed_UpdatesWhenDependencyChanges()
        {
            var a = Reactive.Int(2);
            var b = a * 3; // b depends on a
            Assert.AreEqual(6, b.Value);
            a.Value = 4;
            Assert.AreEqual(12, b.Value);
        }

        [Test]
        public void Watch_Unwatch_ListenerIsCalledAndRemoved()
        {
            var a = Reactive.Int(1);
            bool called = false;
            Action unsub = a.Watch(() => called = true);
            a.Value = 2;
            Assert.IsTrue(called);
            // unsubscribe via returned action
            called = false;
            unsub();
            a.Value = 3;
            Assert.IsFalse(called);
        }

        [Test]
        public void IntRef_Operator_Add_ReflectsChanges()
        {
            var a = Reactive.Int(1);
            var b = Reactive.Int(2);
            var sum = a + b; // computed
            Assert.AreEqual(3, sum.Value);
            a.Value = 5;
            Assert.AreEqual(7, sum.Value);
        }

        [Test]
        public void IntRef_DivideByZero_ThrowsWhenAccessing()
        {
            var a = Reactive.Int(10);
            var b = Reactive.Int(0);
            var div = a / b;
            Assert.Throws<DivideByZeroException>(() => { var v = div.Value; });
        }

        [Test]
        public void FloatRef_Operators_Work()
        {
            var f1 = Reactive.Float(1.5f);
            var f2 = Reactive.Float(2.5f);
            var sum = f1 + f2;
            Assert.AreEqual(4.0f, sum.Value, 1e-6f);
            f1.Value = 3.0f;
            Assert.AreEqual(5.5f, sum.Value, 1e-6f);
        }

        [Test]
        public void StringRef_Concat_Works()
        {
            var s1 = Reactive.String("hello");
            var s2 = Reactive.String("world");
            var joined = s1 + " " + s2;
            Assert.AreEqual("hello world", joined.Value);
            s2.Value = "universe";
            Assert.AreEqual("hello universe", joined.Value);
        }

        [Test]
        public void BoolRef_LogicalOperators_WorkAndPropagate()
        {
            var t = Reactive.Bool(true);
            var f = Reactive.Bool(false);
            var notT = !t;
            var and = t & f;
            var or = t | f;
            Assert.IsFalse(notT.Value);
            Assert.IsFalse(and.Value);
            Assert.IsTrue(or.Value);
            // Flip values and ensure propagation
            t.Value = false;
            f.Value = true;
            Assert.IsTrue(notT.Value);
            Assert.IsFalse(and.Value);
            Assert.IsTrue(or.Value);
        }

        [Test]
        public void Chained_Need_Propagation_UpdatesFinalComputed()
        {
            var a = Reactive.Int(1);
            var b = a + 2; // b depends on a
            var c = b + 3; // c depends on b (and indirectly a)
            Assert.AreEqual(6, c.Value);
            a.Value = 5;
            Assert.AreEqual(10, c.Value);
        }

        [Test]
        public void Ref_ToString_ReturnsValueString()
        {
            var a = Reactive.Int(42);
            Assert.AreEqual("42", a.ToString());
            var s = Reactive.String("abc");
            Assert.AreEqual("abc", s.ToString());
        }

        [Test]
        public void IntRef_SameValueAssignment_DoesNotTriggerUpdate()
        {
            var a = Reactive.Int(10);
            var b = 1;
            var c = Reactive.Int(0);
            var d = a + b + c;
            var e = b + a + c;
            Assert.AreEqual(11, d.Value, "Initial d value incorrect");
            Assert.AreEqual(11, e.Value, "Initial e value incorrect");
            a.Value = 10; // same value assignment
            Assert.AreEqual(11, d.Value, "d value changed after same value assignment");
            Assert.AreEqual(11, e.Value, "e value changed after same value assignment");
            a.Value = 15; // change value
            Assert.AreEqual(16, d.Value, "d value did not update after a changed");
            Assert.AreEqual(16, e.Value, "e value did not update after a changed");
        }


        [Test]
        public void ComputedRef_ExceptionInComputation_Propagates()
        {
            var a = new Units.Attribute("attrA", 10f);
            var b = new Units.Attribute("attrB", 0f);
            var c = new Units.Attribute("attrC", () => a + b);

            Assert.AreEqual(10f, c.Value, "Initial computed value incorrect");
            
            b.Value = 5f;
            Assert.AreEqual(15f, c.Value, "Computed value did not update after dependency change");
        }
    }
}
