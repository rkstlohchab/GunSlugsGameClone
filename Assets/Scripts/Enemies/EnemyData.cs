using UnityEngine;

namespace GunSlugsClone.Enemies
{
    public enum EnemyArchetype { Grunt, Charger, Flyer, Turret, Sniper, Exploder, Shielded, Summoner, Boss }

    [CreateAssetMenu(menuName = "GunSlugs/Enemy Data", fileName = "enemy_")]
    public sealed class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public string Id = "enemy_unset";
        public string DisplayName = "Unset";
        public EnemyArchetype Archetype = EnemyArchetype.Grunt;

        [Header("Stats")]
        [Min(1)] public int MaxHealth = 20;
        [Min(0f)] public float MoveSpeed = 2.5f;
        [Min(0f)] public float AggroRange = 8f;
        [Min(0f)] public float AttackRange = 1.2f;
        [Min(0f)] public float AttackCooldown = 1.0f;
        [Min(0)] public int ContactDamage = 1;

        [Header("Rewards")]
        [Min(0)] public int ScoreOnKill = 10;
        [Range(0f, 1f)] public float HealthDropChance = 0.05f;
        [Range(0f, 1f)] public float AmmoDropChance = 0.15f;
        [Range(0f, 1f)] public float CurrencyDropChance = 0.5f;
        [Min(0)] public int CurrencyDropAmount = 1;

        [Header("Prefab / FX")]
        public GameObject Prefab;
        public AudioClip HitSfx;
        public AudioClip DeathSfx;
    }
}
