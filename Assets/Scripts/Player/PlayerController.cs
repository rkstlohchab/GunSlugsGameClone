using GunSlugsClone.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GunSlugsClone.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerHealth), typeof(WeaponHolder))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private int playerIndex = 0;
        public int PlayerIndex => playerIndex;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float airControl = 0.85f;
        [SerializeField] private float jumpVelocity = 12f;
        [SerializeField] private float coyoteTime = 0.10f;
        [SerializeField] private float jumpBuffer = 0.10f;
        [SerializeField] private float gravityScale = 3.5f;
        [SerializeField] private float fallGravityMultiplier = 1.5f;
        [SerializeField] private float lowJumpMultiplier = 2.0f;
        [SerializeField] private int extraJumps = 0;

        [Header("Dash")]
        [SerializeField] private bool dashEnabled = true;
        [SerializeField] private float dashSpeed = 18f;
        [SerializeField] private float dashDuration = 0.18f;
        [SerializeField] private float dashCooldown = 0.7f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.15f;
        [SerializeField] private LayerMask groundMask = ~0;

        [Header("Audio")]
        [SerializeField] private AudioClip jumpSfx;

        private Rigidbody2D _rb;
        private PlayerHealth _health;
        private WeaponHolder _weapon;
        private Vector2 _moveInput;
        private Vector2 _aimInput;
        private bool _jumpHeld;
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private int _jumpsRemaining;
        private bool _isGrounded;
        private float _dashTimer;
        private float _dashCdTimer;
        private Vector2 _dashDir;
        private int _facing = 1;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = gravityScale;
            _health = GetComponent<PlayerHealth>();
            _weapon = GetComponent<WeaponHolder>();
        }

        private void OnEnable() => EventBus.Publish(new PlayerSpawnedEvent(playerIndex));

        // Input System SendMessages convention: PlayerInput broadcasts On<ActionName>(InputValue)
        // when its Behavior is set to "Send Messages". Keeps wiring zero-click in the Editor.
        public void OnMove(InputValue v) => _moveInput = v.Get<Vector2>();
        public void OnAim(InputValue v) => _aimInput = v.Get<Vector2>();

        public void OnJump(InputValue v)
        {
            if (v.isPressed) _jumpBufferTimer = jumpBuffer;
            _jumpHeld = v.isPressed;
        }

        public void OnFire(InputValue v) => _weapon.SetTriggerHeld(v.isPressed);

        public void OnDash(InputValue v)
        {
            if (!v.isPressed || !dashEnabled || _dashCdTimer > 0f) return;
            _dashDir = _moveInput.sqrMagnitude > 0.01f ? _moveInput.normalized : new Vector2(_facing, 0);
            _dashTimer = dashDuration;
            _dashCdTimer = dashCooldown;
            _health.SetInvulnerable(dashDuration);
        }

        public void OnSwapWeapon(InputValue v)
        {
            if (v.isPressed) _weapon.SwapToNext();
        }

        public void OnPause(InputValue v)
        {
            if (!v.isPressed) return;
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (gm.State == GameState.Playing) gm.Pause();
            else if (gm.State == GameState.Paused) gm.Resume();
        }

        private void Update()
        {
            if (_dashTimer > 0f) _dashTimer -= Time.deltaTime;
            if (_dashCdTimer > 0f) _dashCdTimer -= Time.deltaTime;
            if (_jumpBufferTimer > 0f) _jumpBufferTimer -= Time.deltaTime;

            UpdateGround();
            HandleJump();
            UpdateFacing();
            FeedAimToWeapon();
        }

        private void FixedUpdate()
        {
            if (_dashTimer > 0f)
            {
                _rb.linearVelocity = _dashDir * dashSpeed;
                return;
            }

            var control = _isGrounded ? 1f : airControl;
            var targetX = _moveInput.x * moveSpeed * control;
            var v = _rb.linearVelocity;
            v.x = Mathf.MoveTowards(v.x, targetX, moveSpeed * 12f * Time.fixedDeltaTime);

            if (v.y < 0f) _rb.gravityScale = gravityScale * fallGravityMultiplier;
            else if (v.y > 0f && !_jumpHeld) _rb.gravityScale = gravityScale * lowJumpMultiplier;
            else _rb.gravityScale = gravityScale;

            _rb.linearVelocity = v;
        }

        private void UpdateGround()
        {
            var wasGrounded = _isGrounded;
            _isGrounded = ProbeGround();
            if (_isGrounded)
            {
                _coyoteTimer = coyoteTime;
                _jumpsRemaining = extraJumps;
            }
            else if (wasGrounded)
            {
                _coyoteTimer = coyoteTime;
            }
            else if (_coyoteTimer > 0f) _coyoteTimer -= Time.deltaTime;
        }

        private static readonly Collider2D[] _groundProbeBuffer = new Collider2D[8];

        private bool ProbeGround()
        {
            if (groundCheck == null) return false;
            // OverlapCircle alone returns the player's own collider when the ground-check
            // point sits inside it (radius >= 0.15 with bottom edge at -0.5 overlaps
            // the player's BoxCollider2D), so _isGrounded would stay true mid-air and
            // the player could jump forever. Filter out self + children explicitly.
            var count = Physics2D.OverlapCircleNonAlloc(groundCheck.position, groundCheckRadius, _groundProbeBuffer, groundMask);
            for (var i = 0; i < count; i++)
            {
                var hit = _groundProbeBuffer[i];
                if (hit == null) continue;
                if (hit.transform == transform) continue;
                if (hit.transform.IsChildOf(transform)) continue;
                return true;
            }
            return false;
        }

        private void HandleJump()
        {
            if (_jumpBufferTimer <= 0f) return;
            if (_coyoteTimer > 0f)
            {
                Jump();
                _coyoteTimer = 0f;
                _jumpBufferTimer = 0f;
            }
            else if (_jumpsRemaining > 0)
            {
                Jump();
                _jumpsRemaining--;
                _jumpBufferTimer = 0f;
            }
        }

        private void Jump()
        {
            var v = _rb.linearVelocity;
            v.y = jumpVelocity;
            _rb.linearVelocity = v;
            if (jumpSfx != null) AudioManager.Instance?.PlaySfx(jumpSfx, transform.position);
        }

        private void UpdateFacing()
        {
            if (Mathf.Abs(_moveInput.x) > 0.1f) _facing = _moveInput.x > 0 ? 1 : -1;
            var s = transform.localScale;
            s.x = Mathf.Abs(s.x) * _facing;
            transform.localScale = s;
        }

        private void FeedAimToWeapon()
        {
            Vector2 aim;
            // Mouse position arrives in screen pixels (huge magnitude). Convert to a
            // world direction relative to the player. Gamepad sticks already deliver
            // a unit-vector direction (magnitude <= 1) so we can use them as-is.
            if (_aimInput.sqrMagnitude > 4f && Camera.main != null)
            {
                var screen = new Vector3(_aimInput.x, _aimInput.y, -Camera.main.transform.position.z);
                var world = Camera.main.ScreenToWorldPoint(screen);
                var delta = (Vector2)(world - transform.position);
                aim = delta.sqrMagnitude > 0.0001f ? delta.normalized : new Vector2(_facing, 0);
            }
            else if (_aimInput.sqrMagnitude > 0.01f)
            {
                aim = _aimInput.normalized;
            }
            else
            {
                aim = new Vector2(_facing, 0);
            }
            _weapon.SetAimDirection(aim);
        }

        public void ApplyCharacterModifiers(float speedMul, float fireRateMul, float damageMul, int extraJumpCount, bool dash)
        {
            moveSpeed *= speedMul;
            extraJumps = extraJumpCount;
            dashEnabled = dash;
            _weapon.ApplyMultipliers(fireRateMul, damageMul);
        }
    }
}
