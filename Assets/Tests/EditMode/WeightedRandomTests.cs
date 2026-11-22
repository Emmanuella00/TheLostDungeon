using NUnit.Framework;
using TheLostDungeon.Algorithms;

namespace TheLostDungeon.Tests
{
    /// <summary>Covers "Algorithm correctness" for the loot table's weighted random selection.</summary>
    public class WeightedRandomTests
    {
        // Credits 60, Health 30, Rare 10  ->  [0, 0.6) [0.6, 0.9) [0.9, 1]
        private static readonly float[] Weights = { 60f, 30f, 10f };

        [TestCase(0f, 0)]
        [TestCase(0.59f, 0)]
        [TestCase(0.61f, 1)]
        [TestCase(0.89f, 1)]
        [TestCase(0.95f, 2)]
        [TestCase(1f, 2)]
        public void PicksEntryWhoseCumulativeRangeContainsTheRoll(float roll, int expected)
        {
            Assert.AreEqual(expected, WeightedRandom.PickIndex(Weights, roll));
        }

        [Test]
        public void ZeroWeightEntries_AreNeverPicked()
        {
            float[] weights = { 0f, 5f, 0f };

            for (float roll = 0f; roll <= 1f; roll += 0.05f)
            {
                Assert.AreEqual(1, WeightedRandom.PickIndex(weights, roll));
            }
        }

        [Test]
        public void EmptyOrAllZero_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, WeightedRandom.PickIndex(new float[0], 0.5f));
            Assert.AreEqual(-1, WeightedRandom.PickIndex(new[] { 0f, 0f }, 0.5f));
        }
    }
}
