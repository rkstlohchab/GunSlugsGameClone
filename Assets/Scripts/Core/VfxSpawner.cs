using System;
using UnityEngine;

namespace GunSlugsClone.Core
{
    // Subscribes to gameplay events and spawns matching one-shot VFX prefabs.
    // ParticleSystem prefabs should be configured with stopAction = Destroy so
    // they clean themselves up; this script just instantiates and moves on.
    public sealed class VfxSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject deathBurstPrefab;

        private Action<EnemyKilledEvent> _onEnemyKilled;

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

        private void OnEnemyKilled(EnemyKilledEvent e)
        {
            if (deathBurstPrefab == null) return;
            var go = Instantiate(deathBurstPrefab, e.Position, Quaternion.identity);
            // Belt-and-suspenders: even with main.playOnAwake = true on the
            // prefab, Instantiate sometimes lands in a frame where the
            // ParticleSystem hasn't had its Awake fire — explicitly Play.
            if (go.TryGetComponent<ParticleSystem>(out var ps)) ps.Play();
        }
    }
}
