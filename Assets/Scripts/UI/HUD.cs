using GunSlugsClone.Core;
using GunSlugsClone.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GunSlugsClone.UI
{
    public sealed class HUD : MonoBehaviour
    {
        [SerializeField] private Image healthFill;
        [SerializeField] private TMP_Text ammoText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text weaponNameText;
        [SerializeField] private PlayerHealth boundPlayer;
        [SerializeField] private WeaponHolder boundWeapon;

        private int _score;

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerDamagedEvent>(OnDamage);
            EventBus.Subscribe<EnemyKilledEvent>(OnKill);
            EventBus.Subscribe<WeaponSwappedEvent>(OnWeaponSwap);
            RefreshAll();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerDamagedEvent>(OnDamage);
            EventBus.Unsubscribe<EnemyKilledEvent>(OnKill);
            EventBus.Unsubscribe<WeaponSwappedEvent>(OnWeaponSwap);
        }

        private void Update()
        {
            if (boundWeapon != null && boundWeapon.Active != null && ammoText != null)
            {
                var ammo = boundWeapon.Active.Ammo;
                ammoText.text = boundWeapon.Active.Data.Infinite ? "∞" : ammo.ToString();
            }
        }

        private void OnDamage(PlayerDamagedEvent e)
        {
            if (boundPlayer == null || boundPlayer.PlayerIndex() != e.PlayerIndex) return;
            if (healthFill != null) healthFill.fillAmount = (float)e.RemainingHealth / boundPlayer.Max;
        }

        private void OnKill(EnemyKilledEvent e)
        {
            _score += e.ScoreReward;
            if (scoreText != null) scoreText.text = _score.ToString("N0");
        }

        private void OnWeaponSwap(WeaponSwappedEvent e)
        {
            if (weaponNameText != null && boundWeapon != null && boundWeapon.Active != null)
                weaponNameText.text = boundWeapon.Active.Data.DisplayName;
        }

        private void RefreshAll()
        {
            if (boundPlayer != null && healthFill != null)
                healthFill.fillAmount = (float)boundPlayer.Current / Mathf.Max(1, boundPlayer.Max);
            if (scoreText != null) scoreText.text = "0";
        }
    }

    internal static class PlayerHealthIndexExtensions
    {
        public static int PlayerIndex(this PlayerHealth h)
        {
            // Read serialized field via reflection-free convention: PlayerHealth lives on the same object as PlayerController.
            var ctrl = h.GetComponent<PlayerController>();
            return ctrl != null ? ctrl.PlayerIndex : 0;
        }
    }
}
