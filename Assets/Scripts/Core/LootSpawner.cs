using UnityEngine;

namespace GunSlugsClone.Core
{
    // Listens to EnemyKilledEvent and rolls drops at the kill site.
    // Reading the drop chance from EnemyData would be ideal but the event
    // doesn't carry the data reference; for the smoke test a hard-coded
    // chance is enough. Swap to data-driven once a LootTable is authored.
    public sealed class LootSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject healthPickupPrefab;
        [SerializeField, Range(0f, 1f)] private float healthDropChance = 0.5f;
        [SerializeField] private Vector2 popupVelocity = new Vector2(2f, 5f);

        private void OnEnable() => EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
        private void OnDisable() => EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);

        private void OnEnemyKilled(EnemyKilledEvent e)
        {
            if (healthPickupPrefab == null) return;
            if (Random.value > healthDropChance) return;
            var pickup = Instantiate(healthPickupPrefab, e.Position, Quaternion.identity);
            if (pickup.TryGetComponent<Rigidbody2D>(out var rb))
            {
                var horizontal = Random.Range(-popupVelocity.x, popupVelocity.x);
                rb.linearVelocity = new Vector2(horizontal, popupVelocity.y);
            }
        }
    }
}
