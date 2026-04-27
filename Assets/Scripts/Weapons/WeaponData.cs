using UnityEngine;

namespace GunSlugsClone.Weapons
{
    public enum FireMode { SemiAuto, Auto, Burst, Charged, Beam, Melee }

    [CreateAssetMenu(menuName = "GunSlugs/Weapon Data", fileName = "weapon_")]
    public sealed class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        public string Id = "weapon_unset";
        public string DisplayName = "Unset";
        public Sprite Icon;

        [Header("Behaviour")]
        public FireMode Mode = FireMode.SemiAuto;
        [Min(0)] public int Damage = 10;
        [Min(0.01f)] public float FireRate = 4f;             // shots per second
        [Min(0)] public int ProjectilesPerShot = 1;          // shotgun spread
        [Min(0f)] public float SpreadDegrees = 0f;
        [Min(0f)] public float ProjectileSpeed = 18f;
        [Min(0f)] public float ProjectileLifetime = 1.5f;
        [Min(0f)] public float Knockback = 2f;
        [Min(0f)] public float Recoil = 0.05f;

        [Header("Ammo")]
        public bool Infinite = false;
        [Min(0)] public int MagazineSize = 12;
        [Min(0f)] public float ReloadSeconds = 1.2f;

        [Header("Burst")]
        [Min(1)] public int BurstCount = 3;
        [Min(0f)] public float BurstInterval = 0.05f;

        [Header("Melee")]
        [Min(0f)] public float MeleeRange = 1.6f;

        [Header("Visual / Audio")]
        public GameObject ProjectilePrefab;
        public GameObject MuzzleFlashPrefab;
        public AudioClip FireSfx;
        public AudioClip ReloadSfx;

        public float ShotsPerSecond => FireRate;
        public float SecondsBetweenShots => 1f / Mathf.Max(0.01f, FireRate);
        public float DamagePerSecond => Damage * ProjectilesPerShot * FireRate;
    }
}
