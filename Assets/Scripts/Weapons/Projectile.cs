using UnityEngine;

namespace GunSlugsClone.Weapons
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private GameObject impactFxPrefab;

        private Rigidbody2D _rb;
        private int _damage;
        private float _life;
        private float _knockback;

        public void Configure(Vector2 velocity, int damage, float lifetime, float knockback)
        {
            _rb ??= GetComponent<Rigidbody2D>();
            _rb.linearVelocity = velocity;
            _damage = damage;
            _life = lifetime;
            _knockback = knockback;
        }

        private void Awake() => _rb = GetComponent<Rigidbody2D>();

        private void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f) Destroy(gameObject);
        }

        private void OnCollisionEnter2D(Collision2D col) => HandleHit(col.collider, col.GetContact(0).point);
        private void OnTriggerEnter2D(Collider2D other) => HandleHit(other, transform.position);

        private void HandleHit(Collider2D col, Vector3 point)
        {
            if (((1 << col.gameObject.layer) & hitMask) == 0) return;
            if (col.TryGetComponent<IDamageable>(out var d))
            {
                var dir = (Vector2)(col.transform.position - transform.position);
                d.ApplyDamage(_damage, dir.sqrMagnitude > 0 ? dir.normalized * _knockback : Vector2.zero);
            }
            if (impactFxPrefab != null) Instantiate(impactFxPrefab, point, Quaternion.identity);
            Destroy(gameObject);
        }
    }

    public interface IDamageable
    {
        void ApplyDamage(int amount, Vector2 knockback);
    }
}
