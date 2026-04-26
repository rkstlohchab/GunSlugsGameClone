using GunSlugsClone.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace GunSlugsClone.Tests
{
    public class WeaponDataTests
    {
        [Test]
        public void DamagePerSecond_ScalesWithRateProjectilesAndDamage()
        {
            var w = ScriptableObject.CreateInstance<WeaponData>();
            w.Damage = 10;
            w.FireRate = 4f;
            w.ProjectilesPerShot = 1;
            Assert.AreEqual(40f, w.DamagePerSecond, 0.001f);

            w.ProjectilesPerShot = 3; // shotgun
            Assert.AreEqual(120f, w.DamagePerSecond, 0.001f);

            w.FireRate = 10f;
            Assert.AreEqual(300f, w.DamagePerSecond, 0.001f);

            Object.DestroyImmediate(w);
        }

        [Test]
        public void SecondsBetweenShots_IsInverseOfFireRate()
        {
            var w = ScriptableObject.CreateInstance<WeaponData>();
            w.FireRate = 5f;
            Assert.AreEqual(0.2f, w.SecondsBetweenShots, 0.001f);
            Object.DestroyImmediate(w);
        }

        [Test]
        public void SecondsBetweenShots_HandlesZeroFireRateSafely()
        {
            var w = ScriptableObject.CreateInstance<WeaponData>();
            w.FireRate = 0f;
            // Should clamp to a sane minimum, not divide by zero.
            Assert.IsFalse(float.IsInfinity(w.SecondsBetweenShots));
            Object.DestroyImmediate(w);
        }
    }
}
