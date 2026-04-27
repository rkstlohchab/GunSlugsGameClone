using System;
using UnityEngine;

namespace GunSlugsClone.Core
{
    // Smoothed 2D follow for the main camera. Lerps toward the target's position
    // every LateUpdate so the camera moves after the player has finished its
    // physics step. Also adds a small screen shake when the player takes damage
    // (subscribes to PlayerDamagedEvent on the EventBus).
    // Cinemachine 3 takes over later when we need confiner-per-room (M2) and
    // co-op group framing (M5).
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -10f);
        [SerializeField, Min(0f)] private float damping = 6f;
        [SerializeField] private bool snapOnFirstFrame = true;
        [SerializeField] private float minY = -1f; // floor always visible
        [SerializeField] private float maxY = 100f;

        [Header("Screen Shake")]
        [SerializeField] private bool shakeOnPlayerDamage = true;
        [SerializeField, Min(0f)] private float damageShakeIntensity = 0.25f;
        [SerializeField, Min(0f)] private float damageShakeDuration = 0.2f;

        private bool _hasSnapped;
        private float _shakeTimer;
        private float _shakeMagnitude;
        private float _shakeMaxDuration;
        private Action<PlayerDamagedEvent> _onPlayerDamaged;

        public void SetTarget(Transform t) => target = t;

        public void Shake(float intensity, float duration)
        {
            _shakeMagnitude = Mathf.Max(_shakeMagnitude, intensity);
            _shakeMaxDuration = Mathf.Max(_shakeMaxDuration, duration);
            _shakeTimer = Mathf.Max(_shakeTimer, duration);
        }

        private void OnEnable()
        {
            if (!shakeOnPlayerDamage) return;
            _onPlayerDamaged = _ => Shake(damageShakeIntensity, damageShakeDuration);
            EventBus.Subscribe(_onPlayerDamaged);
        }

        private void OnDisable()
        {
            if (_onPlayerDamaged != null)
            {
                EventBus.Unsubscribe(_onPlayerDamaged);
                _onPlayerDamaged = null;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;
            var desired = target.position + offset;
            // Clamp Y so the floor stays in view even when the player jumps
            // very high or falls — combined with the screen-shake offset
            // that runs after this lerp.
            desired.y = Mathf.Clamp(desired.y, minY, maxY);

            if (snapOnFirstFrame && !_hasSnapped)
            {
                transform.position = desired;
                _hasSnapped = true;
                return;
            }

            transform.position = Vector3.Lerp(transform.position, desired, damping * Time.deltaTime);

            if (_shakeTimer > 0f)
            {
                _shakeTimer -= Time.deltaTime;
                var falloff = _shakeMaxDuration > 0f ? Mathf.Clamp01(_shakeTimer / _shakeMaxDuration) : 0f;
                var jitter = UnityEngine.Random.insideUnitCircle * (_shakeMagnitude * falloff);
                transform.position += new Vector3(jitter.x, jitter.y, 0f);
            }
        }
    }
}
