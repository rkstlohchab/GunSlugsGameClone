using UnityEngine;

namespace GunSlugsClone.Enemies.AI
{
    public sealed class GroundGruntAI : EnemyBase
    {
        [SerializeField] private float patrolDistance = 4f;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private Transform edgeCheck;

        private Vector2 _patrolOrigin;
        private int _patrolDir = 1;
        private bool _aggro;

        protected override void Awake()
        {
            base.Awake();
            _patrolOrigin = transform.position;
        }

        private void FixedUpdate()
        {
            if (Health <= 0) return;
            var hasTarget = Target != null;
            var distToTarget = hasTarget ? Vector2.Distance(transform.position, Target.position) : float.MaxValue;
            _aggro = hasTarget && distToTarget <= data.AggroRange;

            var velocity = Rb.linearVelocity;
            if (_aggro)
            {
                var dir = Mathf.Sign(Target.position.x - transform.position.x);
                velocity.x = dir * data.MoveSpeed * 1.2f;
                if (distToTarget <= data.AttackRange) TryAttack();
            }
            else
            {
                if (Mathf.Abs(transform.position.x - _patrolOrigin.x) > patrolDistance) _patrolDir = -_patrolDir;
                if (edgeCheck != null && !Physics2D.OverlapCircle(edgeCheck.position, 0.1f, groundMask)) _patrolDir = -_patrolDir;
                velocity.x = _patrolDir * data.MoveSpeed;
            }
            Rb.linearVelocity = velocity;
            if (Mathf.Abs(velocity.x) > 0.01f)
            {
                var s = transform.localScale; s.x = Mathf.Abs(s.x) * Mathf.Sign(velocity.x); transform.localScale = s;
            }
        }

        private void TryAttack()
        {
            if (AttackCdTimer > 0f) { AttackCdTimer -= Time.fixedDeltaTime; return; }
            AttackCdTimer = data.AttackCooldown;
            // Contact-damage handled in OnCollisionEnter2D below; ranged variants would override.
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            if (col.collider.TryGetComponent<Player.PlayerHealth>(out var ph))
                ph.TakeDamage(data.ContactDamage);
        }
    }
}
