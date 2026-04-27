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
            // SimpleParticleBurst on the prefab handles its own emission in
            // Awake — just instantiate and let it self-destruct.
            Instantiate(deathBurstPrefab, e.Position, Quaternion.identity);
        }
    }
}
