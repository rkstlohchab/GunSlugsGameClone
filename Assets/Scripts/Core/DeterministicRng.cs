using System;

namespace GunSlugsClone.Core
{
    public sealed class DeterministicRng
    {
        private uint _state;

        public DeterministicRng(int seed) => _state = unchecked((uint)seed == 0u ? 0x9E3779B9u : (uint)seed);

        // xorshift32 — fast, deterministic, fine for level gen / loot. Not for crypto.
        public uint NextUInt()
        {
            var x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            var range = (uint)(maxExclusive - minInclusive);
            return (int)(NextUInt() % range) + minInclusive;
        }

        public float NextFloat01() => (NextUInt() & 0x00FFFFFFu) / (float)0x01000000;

        public float NextFloat(float minInclusive, float maxExclusive)
            => minInclusive + NextFloat01() * (maxExclusive - minInclusive);

        public bool NextBool(float trueProbability = 0.5f) => NextFloat01() < trueProbability;

        public T Pick<T>(System.Collections.Generic.IList<T> source)
        {
            if (source == null || source.Count == 0) throw new ArgumentException("Source must be non-empty");
            return source[NextInt(0, source.Count)];
        }

        public T WeightedPick<T>(System.Collections.Generic.IList<T> items, System.Collections.Generic.IList<float> weights)
        {
            if (items == null || items.Count == 0) throw new ArgumentException("items must be non-empty");
            if (weights == null || weights.Count != items.Count) throw new ArgumentException("weights must match items");
            var total = 0f;
            for (var i = 0; i < weights.Count; i++) total += Math.Max(0f, weights[i]);
            if (total <= 0f) return Pick(items);
            var roll = NextFloat01() * total;
            var acc = 0f;
            for (var i = 0; i < items.Count; i++)
            {
                acc += Math.Max(0f, weights[i]);
                if (roll <= acc) return items[i];
            }
            return items[items.Count - 1];
        }
    }
}
