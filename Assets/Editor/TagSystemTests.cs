using System;
using System.Collections.Generic;
using System.Linq;
using GoveKits.Units;
using NUnit.Framework;

namespace GoveKits.Tests
{
    public class TagSystemTests
    {

        [SetUp]
        public void SetUp()
        {
            // No global registry; tests use unique tag names to avoid collisions.
        }

        [Test]
        public void GameplayTag_Equality_And_ImplicitConversion()
        {
            var a = new GameplayTag("Test.Simple");
            GameplayTag b = "Test.Simple"; // implicit conversion

            Assert.AreEqual(a, b);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
            Assert.AreEqual("Test.Simple", a.Name);
        }

        [Test]
        public void GameplayTagContainer_AddRemove_HasTag_Behavior_And_Events()
        {
            var container = new GameplayTagContainer();
            var name = $"Damage.{Guid.NewGuid()}";
            bool added = false, removed = false;

            container.OnTagAdded += (t) => { if (t.Name.Contains("Damage")) added = true; };
            container.OnTagRemoved += (t) => { if (t.Name.Contains("Damage")) removed = true; };

            // Add by string
            Assert.IsTrue(container.AddTag(name));
            Assert.IsTrue(added);
            Assert.AreEqual(1, container.Count);

            var tag = new GameplayTag(name);
            Assert.IsTrue(container.HasTag(tag));
            Assert.IsTrue(container.HasTag(tag.Name));

            // Remove
            Assert.IsTrue(container.RemoveTag(tag));
            Assert.IsTrue(removed);
            Assert.AreEqual(0, container.Count);
        }

        [Test]
        public void TagQuery_Has_All_Any_None_AtLeast_Work()
        {
            var container = new GameplayTagContainer();
            container.AddTag("A");
            container.AddTag("B");
            container.AddTag("C");

            // Has
            var qA = T.Has("A");
            Assert.IsTrue(qA.Matches(container));

            // All
            var all = T.All("A", "B");
            Assert.IsTrue(all.Matches(container));
            var allFalse = T.All("A", "Z");
            Assert.IsFalse(allFalse.Matches(container));

            // Any
            var any = T.Any("Z", "B");
            Assert.IsTrue(any.Matches(container));
            var anyFalse = T.Any("X", "Y");
            Assert.IsFalse(anyFalse.Matches(container));

            // None
            var none = T.None(T.Has("Z"));
            Assert.IsTrue(none.Matches(container));
            var noneFalse = T.None(T.Has("A"));
            Assert.IsFalse(noneFalse.Matches(container));

            // AtLeast
            var atLeast2 = T.AtLeast(2, "A", "B", "Z");
            Assert.IsTrue(atLeast2.Matches(container));
            var atLeast3 = T.AtLeast(3, "A", "B", "Z");
            Assert.IsFalse(atLeast3.Matches(container));
        }

        [Test]
        public void T_Builder_Allows_Mixed_String_And_Query_Params()
        {
            var container = new GameplayTagContainer();
            container.AddTag("X");
            container.AddTag("Y");

            var subQuery = T.Has("Y");
            var combined = T.All("X", subQuery);
            Assert.IsTrue(combined.Matches(container));
        }
    }
}
