using GunSlugsClone.Core;
using GunSlugsClone.Weapons;
using UnityEngine;

namespace GunSlugsClone.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class EnemyBase : MonoBehaviour, IDamageable
    {
        [SerializeField] protected EnemyData data;
        [SerializeField] protected SpriteRenderer flashRenderer;
        [SerializeField] protected Color flashColor = new Color(1f, 0.35f, 0.35f, 1f);

        protected int Health;
        protected Rigidbody2D Rb;
        protected Transform Target;
        protected float AttackCdTimer;
        public EnemyData Data => data;

        protected virtual void Awake()
        {
            Rb = GetComponent<Rigidbody2D>();
            Health = data != null ? data.MaxHealth : 10;
        }

        protected virtual void Start()
        {
            // Target is non-serialized so explicit SetTarget calls don't survive
            // a scene save. Fall back to finding the Player at scene start so
            // enemies authored at edit time still aggro on Play.
            if (Target == null)
            {
                var pc = FindFirstObjectByType<GunSlugsClone.Player.PlayerController>();
                if (pc != null) Target = pc.transform;
            }
        }

        public void SetTarget(Transform t) => Target = t;

        public void ApplyDamage(int amount, Vector2 knockback)
        {
            if (Health <= 0 || amount <= 0) return;
            Health -= amount;
            if (knockback.sqrMagnitude > 0f) Rb.AddForce(knockback, ForceMode2D.Impulse);
            if (data != null && data.HitSfx != null)
                AudioManager.Instance?.PlaySfx(data.HitSfx, transform.position);
            if (flashRenderer != null) StartCoroutine(HitFlash());
            if (Health <= 0) Die();
        }

        protected virtual void Die()
        {
            if (data != null)
            {
                if (data.DeathSfx != null) AudioManager.Instance?.PlaySfx(data.DeathSfx, transform.position);
                EventBus.Publish(new EnemyKilledEvent(data.Id, data.ScoreOnKill, transform.position));
                DropLoot();
            }
            Destroy(gameObject);
        }

        protected virtual void DropLoot()
        {
            // Hook for room/biome systems to listen on EnemyKilledEvent and spawn pickups.
            // Concrete drop spawning lives in a LootService (M3) so balancing is centralised.
        }

        private System.Collections.IEnumerator HitFlash()
        {
            var orig = flashRenderer.color;
            flashRenderer.color = flashColor;
            yield return new WaitForSeconds(0.05f);
            flashRenderer.color = orig;
        }
    }
}
