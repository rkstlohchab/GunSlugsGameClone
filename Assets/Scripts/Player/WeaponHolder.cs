using System.Collections.Generic;
using GunSlugsClone.Core;
using GunSlugsClone.Weapons;
using UnityEngine;

namespace GunSlugsClone.Player
{
    public sealed class WeaponHolder : MonoBehaviour
    {
        [SerializeField] private int playerIndex = 0;
        [SerializeField] private Transform muzzle;
        [SerializeField] private float muzzleOffset = 0.8f;
        [SerializeField] private List<WeaponData> startingLoadout = new();
        [SerializeField] private int maxCarried = 3;

        [Header("Aim Assist")]
        [SerializeField] private bool aimAssist = true;
        [SerializeField] private float aimAssistRange = 12f;

        private readonly List<WeaponBase> _weapons = new();
        private int _activeIndex;
        private bool _triggerHeld;
        private bool _triggerJustPressed;
        private Vector2 _aim = Vector2.right;
        private float _fireRateMul = 1f;
        private float _damageMul = 1f;

        public WeaponBase Active => _weapons.Count == 0 ? null : _weapons[_activeIndex];

        private void Awake()
        {
            foreach (var data in startingLoadout)
                if (data != null) Equip(data);
        }

        private void Update()
        {
            if (Active == null) return;
            var dir = ResolveAimDirection();
            var origin = transform.position + (Vector3)(dir * muzzleOffset);
            if (muzzle != null) muzzle.position = origin; // keep the Transform in sync for visuals
            Active.UpdateAim(origin, dir);

            // SemiAuto: one shot per fresh trigger press. Auto / everything else: fire while held.
            // Burst / Charged / Beam / Melee specifics are M3 follow-up.
            var fire = Active.Data.Mode == FireMode.SemiAuto
                ? _triggerJustPressed
                : _triggerHeld;
            if (fire) Active.TryFire();
            _triggerJustPressed = false;
        }

        // Muzzle direction is computed in world space from the player's position +
        // aim direction (NOT from the muzzle Transform's localPosition) so
        // bullets always spawn on the side the player is aiming, regardless of
        // the player root's localScale.x flip.
        //
        // When aim-assist is enabled and a damageable target lives within range,
        // override the cursor direction to point straight at it. Lets the user
        // test combat without sweating the trackpad cursor every shot.
        private Vector2 ResolveAimDirection()
        {
            var raw = _aim.sqrMagnitude > 0.0001f ? _aim.normalized : Vector2.right;
            if (!aimAssist) return raw;

            var hits = Physics2D.OverlapCircleAll(transform.position, aimAssistRange);
            Transform best = null;
            var bestSqr = float.MaxValue;
            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit == null) continue;
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                if (!hit.TryGetComponent<IDamageable>(out _)) continue;
                var d = ((Vector2)(hit.transform.position - transform.position)).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = hit.transform; }
            }
            if (best == null) return raw;

            var delta = (Vector2)(best.position - transform.position);
            return delta.sqrMagnitude > 0.0001f ? delta.normalized : raw;
        }

        public void SetTriggerHeld(bool held)
        {
            // Don't gate _triggerJustPressed on a false→true edge: with PlayerInput
            // SendMessages mode, the release event isn't always delivered for
            // rapid taps, so _triggerHeld can stay 'true' across multiple presses
            // and the edge check would silently swallow every shot after the first.
            // Treat any held=true notification as a fresh press; WeaponBase's own
            // cooldown still gates the actual fire rate.
            if (held) _triggerJustPressed = true;
            _triggerHeld = held;
        }
        public void SetAimDirection(Vector2 dir) => _aim = dir;

        public void ApplyMultipliers(float fireRateMul, float damageMul)
        {
            _fireRateMul = fireRateMul;
            _damageMul = damageMul;
            foreach (var w in _weapons) w.ApplyMultipliers(_fireRateMul, _damageMul);
        }

        public void SwapToNext()
        {
            if (_weapons.Count <= 1) return;
            _activeIndex = (_activeIndex + 1) % _weapons.Count;
            EventBus.Publish(new WeaponSwappedEvent(playerIndex, Active.Data.Id, Active.Data.DisplayName));
        }

        public bool Equip(WeaponData data)
        {
            if (data == null) return false;
            if (_weapons.Count >= maxCarried)
            {
                Destroy(_weapons[_activeIndex].gameObject);
                _weapons[_activeIndex] = WeaponBase.Create(data, transform, _fireRateMul, _damageMul);
            }
            else
            {
                _weapons.Add(WeaponBase.Create(data, transform, _fireRateMul, _damageMul));
                _activeIndex = _weapons.Count - 1;
            }
            EventBus.Publish(new WeaponSwappedEvent(playerIndex, Active.Data.Id, Active.Data.DisplayName));
            return true;
        }
    }
}
