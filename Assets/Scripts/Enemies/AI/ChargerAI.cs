using UnityEngine;

namespace GunSlugsClone.Enemies.AI
{
    // Charger behaviour: approach → telegraph (brief pause + flash) → dash burst
    // toward player → recovery cooldown. Reads MoveSpeed/AggroRange/AttackRange
    // from EnemyData like the rest, but layers a state machine on top so the
    // charger reads as a deliberate threat instead of a faster grunt.
    public sealed class ChargerAI : EnemyBase
    {
        private enum State { Approach, Telegraph, Dash, Recover }

        [SerializeField] private float telegraphSeconds = 0.45f;
        [SerializeField] private float dashSeconds = 0.55f;
        [SerializeField] private float dashSpeedMul = 2.6f;
        [SerializeField] private float recoverySeconds = 0.7f;
        [SerializeField] private Color telegraphColor = new Color(1f, 0.85f, 0.25f, 1f);

        private State _state = State.Approach;
        private float _stateTimer;
        private float _dashDir;
        private Color _origColor;
        private bool _origColorCaptured;

        private void FixedUpdate()
        {
            if (Health <= 0) return;
            if (Target == null) return;

            CaptureOrigColor();

            var distToTarget = Vector2.Distance(transform.position, Target.position);
            var aggro = distToTarget <= data.AggroRange;
            var velocity = Rb.linearVelocity;
            _stateTimer -= Time.fixedDeltaTime;

            switch (_state)
            {
                case State.Approach:
                    if (!aggro) { velocity.x = 0f; break; }
                    var dir = Mathf.Sign(Target.position.x - transform.position.x);
                    velocity.x = dir * data.MoveSpeed;
                    FacePlayer(dir);
                    if (distToTarget <= data.AttackRange + 1.5f)
                    {
                        EnterTelegraph();
                    }
                    break;

                case State.Telegraph:
                    velocity.x = 0f;
                    if (_stateTimer <= 0f) EnterDash();
                    break;

                case State.Dash:
                    velocity.x = _dashDir * data.MoveSpeed * dashSpeedMul;
                    if (_stateTimer <= 0f) EnterRecovery();
                    break;

                case State.Recover:
                    velocity.x = 0f;
                    if (_stateTimer <= 0f) _state = State.Approach;
                    break;
            }

            Rb.linearVelocity = velocity;
        }

        private void EnterTelegraph()
        {
            _state = State.Telegraph;
            _stateTimer = telegraphSeconds;
            _dashDir = Mathf.Sign(Target.position.x - transform.position.x);
            if (_dashDir == 0f) _dashDir = 1f;
            if (flashRenderer != null) flashRenderer.color = telegraphColor;
        }

        private void EnterDash()
        {
            _state = State.Dash;
            _stateTimer = dashSeconds;
            if (flashRenderer != null && _origColorCaptured) flashRenderer.color = _origColor;
        }

        private void EnterRecovery()
        {
            _state = State.Recover;
            _stateTimer = recoverySeconds;
        }

        private void FacePlayer(float dir)
        {
            if (dir == 0f) return;
            var s = transform.localScale; s.x = Mathf.Abs(s.x) * dir; transform.localScale = s;
        }

        private void CaptureOrigColor()
        {
            if (_origColorCaptured || flashRenderer == null) return;
            _origColor = flashRenderer.color;
            _origColorCaptured = true;
        }

        private void OnCollisionStay2D(Collision2D col)
        {
            if (col.collider.TryGetComponent<Player.PlayerHealth>(out var ph))
                ph.TakeDamage(data.ContactDamage);
        }
    }
}
