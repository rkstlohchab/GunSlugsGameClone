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
            Debug.Log($"[WeaponHolder] Awake on '{name}': startingLoadout count={startingLoadout?.Count ?? -1}, muzzle={(muzzle != null ? muzzle.name : "NULL")}");
            foreach (var data in startingLoadout)
            {
                Debug.Log($"[WeaponHolder]   loadout entry: {(data != null ? data.DisplayName + " (" + data.Id + "), prefab=" + (data.ProjectilePrefab != null ? data.ProjectilePrefab.name : "NULL") : "NULL")}");
                if (data != null) Equip(data);
            }
            Debug.Log($"[WeaponHolder] After Awake: Active={(Active != null ? Active.Data.Id : "NULL")}");
        }

        private void Update()
        {
            if (Active == null) return;
            Active.UpdateAim(muzzle != null ? muzzle.position : transform.position, _aim);
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
