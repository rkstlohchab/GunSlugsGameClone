using GunSlugsClone.Core;
using UnityEngine;

namespace GunSlugsClone.Weapons
{
    public class WeaponBase : MonoBehaviour
    {
        public WeaponData Data { get; private set; }
        public int Ammo { get; private set; }

        private float _cooldown;
        private float _reloadRemaining;
        private bool _reloading;
        private Vector3 _origin;
        private Vector2 _aim = Vector2.right;
        private float _fireRateMul = 1f;
        private float _damageMul = 1f;

        public static WeaponBase Create(WeaponData data, Transform parent, float fireRateMul, float damageMul)
        {
            var go = new GameObject($"Weapon_{data.Id}");
            go.transform.SetParent(parent, worldPositionStays: false);
            var w = go.AddComponent<WeaponBase>();
            w.Data = data;
            w.Ammo = data.Infinite ? int.MaxValue : data.MagazineSize;
            w._fireRateMul = fireRateMul;
            w._damageMul = damageMul;
            return w;
        }

        public void ApplyMultipliers(float fireRateMul, float damageMul)
        {
            _fireRateMul = fireRateMul;
            _damageMul = damageMul;
        }

        public void UpdateAim(Vector3 origin, Vector2 aim)
        {
            _origin = origin;
            _aim = aim.sqrMagnitude > 0.0001f ? aim.normalized : Vector2.right;
        }

        private void Update()
        {
            if (_cooldown > 0f) _cooldown -= Time.deltaTime;
            if (_reloading)
            {
                _reloadRemaining -= Time.deltaTime;
                if (_reloadRemaining <= 0f) FinishReload();
            }
        }

        public bool TryFire()
        {
            if (_reloading) return false;
            if (_cooldown > 0f) return false;
            if (!Data.Infinite && Ammo <= 0) { BeginReload(); return false; }

            Fire();
            _cooldown = Data.SecondsBetweenShots / Mathf.Max(0.01f, _fireRateMul);
            if (!Data.Infinite) Ammo--;
            PublishAmmoChanged();
            return true;
        }

        private void PublishAmmoChanged()
        {
            EventBus.Publish(new WeaponAmmoChangedEvent(Ammo, Data.MagazineSize, Data.Infinite, _reloading));
        }

        protected virtual void Fire()
        {
            if (Data.Mode == FireMode.Melee)
            {
                DoMeleeAttack();
            }
            else
            {
                for (var i = 0; i < Data.ProjectilesPerShot; i++)
                {
                    var spread = Random.Range(-Data.SpreadDegrees * 0.5f, Data.SpreadDegrees * 0.5f);
                    var dir = (Quaternion.Euler(0, 0, spread) * _aim).normalized;
                    SpawnProjectile(dir);
                }
            }
            if (Data.MuzzleFlashPrefab != null)
                Instantiate(Data.MuzzleFlashPrefab, _origin, Quaternion.LookRotation(Vector3.forward, _aim));
            if (Data.FireSfx != null)
                AudioManager.Instance?.PlaySfx(Data.FireSfx, _origin);
        }

        // Melee weapons hit everything in a circle around the muzzle and apply
        // damage + knockback. No projectile is spawned. Cooldown still gates
        // swing rate so DPS is bounded.
        private void DoMeleeAttack()
        {
            var damage = Mathf.RoundToInt(Data.Damage * _damageMul);
            var hits = Physics2D.OverlapCircleAll(_origin, Data.MeleeRange);
            for (var i = 0; i < hits.Length; i++)
            {
                var col = hits[i];
                if (col == null) continue;
                if (!col.TryGetComponent<IDamageable>(out var d)) continue;
                if (col.transform.IsChildOf(transform.parent)) continue; // skip self / wielder
                var knockback = _aim.normalized * Data.Knockback;
                d.ApplyDamage(damage, knockback);
            }
        }

        private void SpawnProjectile(Vector2 dir)
        {
            if (Data.ProjectilePrefab == null) return;
            var go = Instantiate(Data.ProjectilePrefab, _origin, Quaternion.LookRotation(Vector3.forward, dir));
            if (go.TryGetComponent<Projectile>(out var p))
                p.Configure(dir * Data.ProjectileSpeed, Mathf.RoundToInt(Data.Damage * _damageMul), Data.ProjectileLifetime, Data.Knockback);
        }

        private void BeginReload()
        {
            _reloading = true;
            _reloadRemaining = Data.ReloadSeconds;
            if (Data.ReloadSfx != null) AudioManager.Instance?.PlaySfx(Data.ReloadSfx, _origin);
            PublishAmmoChanged();
        }

        private void FinishReload()
        {
            _reloading = false;
            Ammo = Data.MagazineSize;
            PublishAmmoChanged();
        }

        private void OnEnable()
        {
            // Surface initial ammo on every weapon-equip so the HUD updates
            // when the player switches to a different gun.
            if (Data != null) PublishAmmoChanged();
        }
    }
}
