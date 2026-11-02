using System;
using NUnit.Framework;
using SlotGame.Core.Random;

namespace SlotGame.Tests
{
    public class WeightedRandomPickerTests
    {
        [Test]
        public void Distribution_FollowsTheWeights()
        {
            var picker = new WeightedRandomPicker<string>(new[] { "common", "rare", "epic" }, new[] { 6, 3, 1 });
            var random = new SystemRandomProvider(42);
            var counts = new int[3];
            const int rolls = 200000;

            for (int i = 0; i < rolls; i++)
            {
                counts[picker.PickIndex(random)]++;
            }

            // Expected 60% / 30% / 10%. Allow one percentage point either way.
            Assert.AreEqual(0.6, counts[0] / (double)rolls, 0.01);
            Assert.AreEqual(0.3, counts[1] / (double)rolls, 0.01);
            Assert.AreEqual(0.1, counts[2] / (double)rolls, 0.01);
        }

        [Test]
        public void ZeroWeightItem_IsNeverPicked()
        {
            var picker = new WeightedRandomPicker<int>(new[] { 10, 20, 30 }, new[] { 1, 0, 1 });
            var random = new SystemRandomProvider(7);

            for (int i = 0; i < 10000; i++)
            {
                Assert.AreNotEqual(20, picker.Pick(random));
            }
        }

        [Test]
        public void RollMapsToTheRightBucket()
        {
            // Weights 5, 3, 2 give cumulative totals 5, 8, 10.
            var picker = new WeightedRandomPicker<char>(new[] { 'a', 'b', 'c' }, new[] { 5, 3, 2 });

            Assert.AreEqual('a', picker.Pick(new ScriptedRandomProvider(0)));
            Assert.AreEqual('a', picker.Pick(new ScriptedRandomProvider(4)));
            Assert.AreEqual('b', picker.Pick(new ScriptedRandomProvider(5)));
            Assert.AreEqual('b', picker.Pick(new ScriptedRandomProvider(7)));
            Assert.AreEqual('c', picker.Pick(new ScriptedRandomProvider(8)));
            Assert.AreEqual('c', picker.Pick(new ScriptedRandomProvider(9)));
        }

        [Test]
        public void SameSeed_GivesSameSequence()
        {
            var picker = new WeightedRandomPicker<int>(new[] { 0, 1, 2, 3 }, new[] { 1, 2, 3, 4 });
            var first = new SystemRandomProvider(99);
            var second = new SystemRandomProvider(99);

            for (int i = 0; i < 1000; i++)
            {
                Assert.AreEqual(picker.PickIndex(first), picker.PickIndex(second));
            }
        }

        [Test]
        public void InvalidWeights_Throw()
        {
            Assert.Throws<ArgumentException>(() => new WeightedRandomPicker<int>(new[] { 1, 2 }, new[] { 0, 0 }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new WeightedRandomPicker<int>(new[] { 1 }, new[] { -1 }));
            Assert.Throws<ArgumentException>(() => new WeightedRandomPicker<int>(new[] { 1, 2 }, new[] { 1 }));
        }
    }
}
