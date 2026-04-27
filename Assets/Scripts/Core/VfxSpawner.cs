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
        [SerializeField] private GameObject rescueBurstPrefab;

        private Action<EnemyKilledEvent> _onEnemyKilled;
        private Action<HostageRescuedEvent> _onHostageRescued;

        private void OnEnable()
        {
            _onEnemyKilled = OnEnemyKilled;
            _onHostageRescued = OnHostageRescued;
            EventBus.Subscribe(_onEnemyKilled);
            EventBus.Subscribe(_onHostageRescued);
        }

        private void OnDisable()
        {
            if (_onEnemyKilled != null)    { EventBus.Unsubscribe(_onEnemyKilled);    _onEnemyKilled = null; }
            if (_onHostageRescued != null) { EventBus.Unsubscribe(_onHostageRescued); _onHostageRescued = null; }
        }

        private void OnEnemyKilled(EnemyKilledEvent e)
        {
            if (deathBurstPrefab == null) return;
            Instantiate(deathBurstPrefab, e.Position, Quaternion.identity);
        }

        private void OnHostageRescued(HostageRescuedEvent e)
        {
            if (rescueBurstPrefab == null) return;
            Instantiate(rescueBurstPrefab, e.Position, Quaternion.identity);
        }
    }
}
