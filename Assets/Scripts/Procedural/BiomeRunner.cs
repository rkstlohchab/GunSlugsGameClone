using System.Collections.Generic;
using GunSlugsClone.Core;
using GunSlugsClone.Enemies;
using UnityEngine;

namespace GunSlugsClone.Procedural
{
    public sealed class BiomeRunner : MonoBehaviour
    {
        [SerializeField] private BiomeConfig biome;
        [SerializeField] private Transform roomsRoot;
        [SerializeField] private Transform playersRoot;

        private GeneratedLevel _level;
        private readonly Dictionary<GeneratedRoom, RoomTemplate> _instances = new();
        private GeneratedRoom _currentRoom;

        public BiomeConfig Biome => biome;
        public GeneratedLevel Level => _level;

        public GeneratedLevel Build(int seed)
        {
            _level = LevelGenerator.Generate(biome, seed);
            foreach (var r in _level.Rooms)
            {
                var prefab = r.Template != null ? r.Template.gameObject : null;
                if (prefab == null) continue;
                var go = Instantiate(prefab, GridToWorld(r.GridPosition, r.Template.Size), Quaternion.identity, roomsRoot);
                _instances[r] = go.GetComponent<RoomTemplate>();
                go.SetActive(false);
            }
            EnterRoom(_level.Start);
            return _level;
        }

        private static Vector3 GridToWorld(Vector2 gridPos, Vector2 size)
            => new(gridPos.x * (size.x + 2f), gridPos.y * (size.y + 2f), 0f);

        public void EnterRoom(GeneratedRoom room)
        {
            if (room == null || !_instances.TryGetValue(room, out var inst)) return;
            if (_currentRoom != null && _instances.TryGetValue(_currentRoom, out var prev))
                prev.gameObject.SetActive(false);
            inst.gameObject.SetActive(true);
            _currentRoom = room;
            EventBus.Publish(new RoomEnteredEvent(_level.Rooms.IndexOf(room), room.IsBoss));
            SpawnEnemiesForCurrentRoom();
        }

        private void SpawnEnemiesForCurrentRoom()
        {
            if (_currentRoom == null) return;
            if (!_instances.TryGetValue(_currentRoom, out var inst)) return;
            if (_currentRoom.IsBoss && biome.BossPrefab != null)
            {
                var bossSpawn = inst.EnemySpawns.Count > 0 ? inst.EnemySpawns[0] : inst.transform;
                Instantiate(biome.BossPrefab, bossSpawn.position, Quaternion.identity, inst.transform);
                return;
            }

            foreach (var spawn in inst.EnemySpawns)
            {
                if (spawn == null) continue;
                var data = (biome.EnemyWeights != null && biome.EnemyWeights.Count == biome.EnemyPool.Count)
                    ? new DeterministicRng(_level.Seed ^ spawn.GetHashCode()).WeightedPick(biome.EnemyPool, biome.EnemyWeights)
                    : biome.EnemyPool[Random.Range(0, biome.EnemyPool.Count)];
                if (data == null || data.Prefab == null) continue;
                var enemy = Instantiate(data.Prefab, spawn.position, Quaternion.identity, inst.transform);
                if (enemy.TryGetComponent<EnemyBase>(out var eb) && playersRoot != null && playersRoot.childCount > 0)
                    eb.SetTarget(playersRoot.GetChild(0));
            }
        }

        public Vector3 GetSpawnPoint()
        {
            if (_currentRoom != null && _instances.TryGetValue(_currentRoom, out var inst) && inst.PlayerSpawn != null)
                return inst.PlayerSpawn.position;
            return Vector3.zero;
        }
    }
}
