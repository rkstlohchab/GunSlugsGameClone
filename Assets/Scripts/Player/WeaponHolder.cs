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

        private readonly List<WeaponBase> _weapons = new();
        private int _activeIndex;
        private bool _triggerHeld;
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
            // Muzzle position is computed from the player's world position +
            // aim direction, NOT from the muzzle Transform's localPosition.
            // Otherwise the muzzle inherits the player root's localScale.x flip
            // (which flips with movement facing, not aim) and bullets spawn on
            // the wrong side when the player faces away from the cursor.
            var dir = _aim.sqrMagnitude > 0.0001f ? _aim.normalized : Vector2.right;
            var origin = transform.position + (Vector3)(dir * muzzleOffset);
            if (muzzle != null) muzzle.position = origin; // keep the Transform in sync for visuals
            Active.UpdateAim(origin, dir);
            if (_triggerHeld) Active.TryFire();
        }

        public void SetTriggerHeld(bool held) => _triggerHeld = held;
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
            EventBus.Publish(new WeaponSwappedEvent(playerIndex, Active.Data.Id));
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
            EventBus.Publish(new WeaponSwappedEvent(playerIndex, Active.Data.Id));
            return true;
        }
    }
}
