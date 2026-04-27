using UnityEngine;

namespace GunSlugsClone.Enemies.AI
{
    // Simple gravity-less hover-and-chase enemy. Patrols by gentle bobbing
    // when the player is out of aggro range; closes the distance directly
    // when aggro'd. No ground checks (it doesn't touch the ground).
    public sealed class FlyerAI : EnemyBase
    {
        [SerializeField] private float bobAmplitude = 0.6f;
        [SerializeField] private float bobFrequency = 1.5f;

        private Vector2 _patrolOrigin;
        private float _bobPhase;

        protected override void Awake()
        {
            base.Awake();
            _patrolOrigin = transform.position;
            _bobPhase = Random.value * Mathf.PI * 2f;
        }

        private void FixedUpdate()
        {
            if (Health <= 0 || data == null) return;

            var hasTarget = Target != null;
            var toTarget  = hasTarget ? (Vector2)(Target.position - transform.position) : Vector2.zero;
            var distSqr   = toTarget.sqrMagnitude;
            var aggroSqr  = data.AggroRange * data.AggroRange;

            if (hasTarget && distSqr <= aggroSqr)
            {
                if (distSqr > 0.04f)
                    Rb.linearVelocity = toTarget.normalized * data.MoveSpeed;
            }
            else
            {
                // Hover-bob around patrol origin.
                var t = Time.time * bobFrequency + _bobPhase;
                var targetY = _patrolOrigin.y + Mathf.Sin(t) * bobAmplitude;
                var pos = (Vector2)transform.position;
                Rb.linearVelocity = new Vector2(0f, (targetY - pos.y) * 2f);
            }

            // Face the player horizontally.
            if (hasTarget && Mathf.Abs(toTarget.x) > 0.1f)
            {
                var s = transform.localScale;
                s.x = Mathf.Abs(s.x) * Mathf.Sign(toTarget.x);
                transform.localScale = s;
            }
        }

        private void OnCollisionStay2D(Collision2D col)
        {
            if (col.collider.TryGetComponent<Player.PlayerHealth>(out var ph))
                ph.TakeDamage(data.ContactDamage);
        }
    }
}
