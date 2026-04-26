using GunSlugsClone.Core;
using UnityEngine;

namespace GunSlugsClone.Player
{
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private int playerIndex = 0;
        [SerializeField] private int maxHealth = 5;
        [SerializeField] private float invulnerabilityOnHit = 0.8f;
        [SerializeField] private SpriteRenderer flashRenderer;
        [SerializeField] private Color flashColor = Color.white;

        public int Current { get; private set; }
        public int Max => maxHealth;
        public bool IsAlive => Current > 0;

        private float _invulnTimer;

        private void Awake() => Current = maxHealth;

        private void Update()
        {
            if (_invulnTimer > 0f) _invulnTimer -= Time.deltaTime;
        }

        public void ApplyMaxHealthMultiplier(float mul)
        {
            maxHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * mul));
            Current = maxHealth;
        }

        public void TakeDamage(int amount)
        {
            if (!IsAlive || _invulnTimer > 0f || amount <= 0) return;
            Current = Mathf.Max(0, Current - amount);
            _invulnTimer = invulnerabilityOnHit;
            EventBus.Publish(new PlayerDamagedEvent(playerIndex, amount, Current));
            if (flashRenderer != null) StartCoroutine(HitFlash());
            if (Current == 0) Die();
        }

        public void Heal(int amount)
        {
            if (!IsAlive || amount <= 0) return;
            Current = Mathf.Min(maxHealth, Current + amount);
        }

        public void SetInvulnerable(float seconds)
        {
            if (seconds > _invulnTimer) _invulnTimer = seconds;
        }

        private void Die() => EventBus.Publish(new PlayerDiedEvent(playerIndex));

        private System.Collections.IEnumerator HitFlash()
        {
            var orig = flashRenderer.color;
            flashRenderer.color = flashColor;
            yield return new WaitForSeconds(0.06f);
            flashRenderer.color = orig;
        }
    }
}
