using System.Collections.Generic;
using NUnit.Framework;
using TheLostDungeon.Algorithms;
using UnityEngine;

namespace TheLostDungeon.Tests
{
    /// <summary>Covers "Algorithm correctness" for the recursive barrel chain reaction.</summary>
    public class ChainReactionTests
    {
        [Test]
        public void BarrelsInARow_AllExplode_InOrder()
        {
            // A - B - C, each 3m apart. A only reaches B, B reaches C: a chain, not one big blast.
            var barrels = new List<Vector3> { new Vector3(0, 0, 0), new Vector3(3, 0, 0), new Vector3(6, 0, 0) };

            List<int> order = ChainReaction.FindChain(0, barrels, chainRadius: 3.5f);

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, order);
        }

        [Test]
        public void EachBarrelExplodesExactlyOnce_EvenWhenAllAreNeighbours()
        {
            // Three barrels all in range of each other: without the "already exploded" base case
            // the recursion would loop forever.
            var barrels = new List<Vector3> { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1) };

            List<int> order = ChainReaction.FindChain(1, barrels, chainRadius: 5f);

            Assert.AreEqual(3, order.Count);
            CollectionAssert.AllItemsAreUnique(order);
        }

        [Test]
        public void BarrelOutOfRange_DoesNotExplode()
        {
            var barrels = new List<Vector3> { new Vector3(0, 0, 0), new Vector3(2, 0, 0), new Vector3(20, 0, 0) };

            List<int> order = ChainReaction.FindChain(0, barrels, chainRadius: 3f);

            CollectionAssert.AreEqual(new[] { 0, 1 }, order);
        }

        [Test]
        public void MaxDepth_LimitsHowFarTheChainGoes()
        {
            var barrels = new List<Vector3>();
            for (int i = 0; i < 6; i++) barrels.Add(new Vector3(i * 2f, 0, 0));

            List<int> order = ChainReaction.FindChain(0, barrels, chainRadius: 2.5f, maxDepth: 2);

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, order, "Depth 0, 1 and 2 only");
        }

        [Test]
        public void InvalidStart_ReturnsEmpty()
        {
            var barrels = new List<Vector3> { Vector3.zero };
            Assert.AreEqual(0, ChainReaction.FindChain(5, barrels, 3f).Count);
        }
    }
}
