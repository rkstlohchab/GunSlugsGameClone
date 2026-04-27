using UnityEngine;

namespace GunSlugsClone.Player
{
    // Touchable heart pickup. Heals the player on overlap then destroys itself.
    // The trigger collider does the contact detection; visuals + physics live
    // on the same GameObject so dropped pickups can have a small popup velocity
    // applied at spawn time and settle on the floor under gravity.
    [RequireComponent(typeof(Collider2D))]
    public sealed class HealthPickup : MonoBehaviour
    {
        [SerializeField] private int healAmount = 1;
        [SerializeField] private float lifetime = 12f;

        private void Start()
        {
            if (lifetime > 0f) Destroy(gameObject, lifetime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent<PlayerHealth>(out var ph)) return;
            ph.Heal(healAmount);
            Destroy(gameObject);
        }
    }
}
