using GunSlugsClone.Core;
using NUnit.Framework;

namespace GunSlugsClone.Tests
{
    public class DeterministicRngTests
    {
        [Test]
        public void SameSeed_ProducesSameSequence()
        {
            var a = new DeterministicRng(42);
            var b = new DeterministicRng(42);
            for (var i = 0; i < 1000; i++)
                Assert.AreEqual(a.NextUInt(), b.NextUInt());
        }

        [Test]
        public void DifferentSeeds_DivergeWithinFewDraws()
        {
            var a = new DeterministicRng(1);
            var b = new DeterministicRng(2);
            var divergedWithin = 0;
            for (var i = 0; i < 16 && a.NextUInt() == b.NextUInt(); i++) divergedWithin = i + 1;
            Assert.Less(divergedWithin, 16, "Two different seeds should diverge within 16 draws");
        }

        [Test]
        public void NextInt_StaysInRange()
        {
            var rng = new DeterministicRng(7);
            for (var i = 0; i < 10_000; i++)
            {
                var v = rng.NextInt(-5, 5);
                Assert.GreaterOrEqual(v, -5);
                Assert.Less(v, 5);
            }
        }

        [Test]
        public void NextFloat01_StaysInUnitInterval()
        {
            var rng = new DeterministicRng(13);
            for (var i = 0; i < 10_000; i++)
            {
                var v = rng.NextFloat01();
                Assert.GreaterOrEqual(v, 0f);
                Assert.Less(v, 1f);
            }
        }

        [Test]
        public void WeightedPick_ApproximatesDistribution()
        {
            var items = new[] { "A", "B", "C" };
            var weights = new[] { 1f, 1f, 8f }; // C should win ~80% of the time
            var rng = new DeterministicRng(99);
            var counts = new System.Collections.Generic.Dictionary<string, int> { { "A", 0 }, { "B", 0 }, { "C", 0 } };
            for (var i = 0; i < 10_000; i++) counts[rng.WeightedPick(items, weights)]++;
            Assert.Greater(counts["C"], 7000);
            Assert.Less(counts["A"], 2000);
        }

        [Test]
        public void Pick_ThrowsOnEmpty()
        {
            var rng = new DeterministicRng(0);
            Assert.Throws<System.ArgumentException>(() => rng.Pick(System.Array.Empty<int>()));
        }
    }
}
