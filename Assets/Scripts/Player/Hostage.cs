using GunSlugsClone.Core;
using UnityEngine;

namespace GunSlugsClone.Player
{
    // Touchable rescue NPC. Player overlap → publishes HostageRescuedEvent
    // (LootSpawner / VfxSpawner / GameOverScreen pick that up for score, VFX,
    // and HUD count) then destroys self. Visual placeholder uses a Kenney
    // character sprite tinted for distinction; later we'll add a 'cell' graphic
    // and a small 'rescued' run-to-exit animation.
    [RequireComponent(typeof(Collider2D))]
    public sealed class Hostage : MonoBehaviour
    {
        [SerializeField] private int scoreReward = 50;

        private bool _rescued;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_rescued) return;
            if (!other.TryGetComponent<PlayerHealth>(out _)) return;
            _rescued = true;
            EventBus.Publish(new HostageRescuedEvent(scoreReward, transform.position));
            Destroy(gameObject);
        }
    }
}
