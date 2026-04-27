using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GunSlugsClone.Core
{
    // Spawns enemies in escalating waves at the room's spawn anchors. Subscribes
    // to EnemyKilledEvent to know when a wave is fully cleared, then waits a
    // beat and spawns the next one. When all configured waves are done it just
    // stops — a victory screen / boss room can take over later.
    public sealed class WaveSpawner : MonoBehaviour
    {
        [Serializable]
        public struct WaveConfig
        {
            public int gruntCount;
            public int chargerCount;
            public int flyerCount;
            public int bossCount;
        }

        [SerializeField] private GameObject gruntPrefab;
        [SerializeField] private GameObject chargerPrefab;
        [SerializeField] private GameObject flyerPrefab;
        [SerializeField] private GameObject bossPrefab;
        [SerializeField] private List<Transform> spawnAnchors = new();
        [SerializeField] private List<WaveConfig> waves = new();
        [SerializeField] private float startDelay = 0.6f;
        [SerializeField] private float delayBetweenWaves = 1.5f;

        [Header("Spawn Safety")]
        [SerializeField] private Transform playerTransform;
        [SerializeField, Min(0f)] private float minSpawnDistance = 5f;

        private int _currentWave = -1;
        private int _aliveCount;
        private bool _waitingForNext;
        private Action<EnemyKilledEvent> _onEnemyKilled;

        public int CurrentWave => _currentWave + 1; // 1-indexed for display
        public int TotalWaves => waves.Count;

        private void OnEnable()
        {
            _onEnemyKilled = OnEnemyKilled;
            EventBus.Subscribe(_onEnemyKilled);
        }

        private void OnDisable()
        {
            if (_onEnemyKilled != null)
            {
                EventBus.Unsubscribe(_onEnemyKilled);
                _onEnemyKilled = null;
            }
        }

        private void Start() => StartCoroutine(SpawnAfter(startDelay));

        private IEnumerator SpawnAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            SpawnNextWave();
        }

        private void SpawnNextWave()
        {
            _currentWave++;
            if (_currentWave >= waves.Count)
            {
                Debug.Log($"[WaveSpawner] All {waves.Count} waves complete.");
                EventBus.Publish(new AllWavesClearedEvent(waves.Count));
                return;
            }

            if (spawnAnchors.Count == 0)
            {
                Debug.LogWarning("[WaveSpawner] No spawn anchors configured — wave skipped.");
                return;
            }

            var wave = waves[_currentWave];
            _aliveCount = 0;
            var anchorIdx = 0;

            anchorIdx = SpawnBatch(gruntPrefab,   wave.gruntCount,   anchorIdx);
            anchorIdx = SpawnBatch(chargerPrefab, wave.chargerCount, anchorIdx);
            anchorIdx = SpawnBatch(flyerPrefab,   wave.flyerCount,   anchorIdx);
            anchorIdx = SpawnBatch(bossPrefab,    wave.bossCount,    anchorIdx);

            Debug.Log($"[WaveSpawner] Wave {CurrentWave}/{TotalWaves}: {wave.gruntCount}G + {wave.chargerCount}C + {wave.flyerCount}F + {wave.bossCount}B.");
        }

        private int SpawnBatch(GameObject prefab, int count, int anchorIdx)
        {
            if (prefab == null || count <= 0) return anchorIdx;
            for (var i = 0; i < count; i++)
            {
                var anchor = PickAnchor(anchorIdx);
                anchorIdx++;
                if (anchor == null) continue;
                Instantiate(prefab, anchor.position, Quaternion.identity);
                _aliveCount++;
            }
            return anchorIdx;
        }

        // Skip anchors that sit within minSpawnDistance of the player, so an
        // enemy never materialises on the player's head. If every anchor is
        // too close (e.g. player is standing in the middle of the only room
        // we have anchors in), fall back to the anchor that's farthest away
        // rather than any anchor at the start index.
        private Transform PickAnchor(int startIdx)
        {
            if (spawnAnchors.Count == 0) return null;
            if (playerTransform == null) return spawnAnchors[startIdx % spawnAnchors.Count];

            for (var attempt = 0; attempt < spawnAnchors.Count; attempt++)
            {
                var anchor = spawnAnchors[(startIdx + attempt) % spawnAnchors.Count];
                if (anchor == null) continue;
                var sqrDist = ((Vector2)(anchor.position - playerTransform.position)).sqrMagnitude;
                if (sqrDist >= minSpawnDistance * minSpawnDistance) return anchor;
            }

            Transform farthest = null;
            var bestSqr = -1f;
            for (var i = 0; i < spawnAnchors.Count; i++)
            {
                var a = spawnAnchors[i];
                if (a == null) continue;
                var sqr = ((Vector2)(a.position - playerTransform.position)).sqrMagnitude;
                if (sqr > bestSqr) { bestSqr = sqr; farthest = a; }
            }
            return farthest;
        }

        private void OnEnemyKilled(EnemyKilledEvent _)
        {
            _aliveCount--;
            if (_aliveCount > 0 || _waitingForNext) return;
            _waitingForNext = true;
            StartCoroutine(QueueNextWave());
        }

        private IEnumerator QueueNextWave()
        {
            yield return new WaitForSeconds(delayBetweenWaves);
            _waitingForNext = false;
            SpawnNextWave();
        }
    }
}
