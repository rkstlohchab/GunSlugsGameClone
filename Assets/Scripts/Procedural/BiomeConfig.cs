using System.Collections.Generic;
using GunSlugsClone.Enemies;
using UnityEngine;

namespace GunSlugsClone.Procedural
{
    [CreateAssetMenu(menuName = "GunSlugs/Biome Config", fileName = "biome_")]
    public sealed class BiomeConfig : ScriptableObject
    {
        [Header("Identity")]
        public string Id = "biome_unset";
        public string DisplayName = "Unset";

        [Header("Generation")]
        [Min(3)] public int RoomsPerRun = 8;
        [Range(0f, 1f)] public float TreasureRoomChance = 0.2f;

        [Header("Content Pools")]
        public List<GameObject> RoomTemplates = new();
        public List<EnemyData> EnemyPool = new();
        public List<float> EnemyWeights = new();
        public GameObject BossPrefab;

        [Header("Look & Feel")]
        public Color AmbientTint = Color.white;
        public AudioClip Music;

        public bool IsValid =>
            RoomTemplates != null && RoomTemplates.Count > 0 &&
            EnemyPool != null && EnemyPool.Count > 0 &&
            (EnemyWeights == null || EnemyWeights.Count == 0 || EnemyWeights.Count == EnemyPool.Count);
    }
}
