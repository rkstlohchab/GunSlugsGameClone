using UnityEngine;

namespace GunSlugsClone.Enemies
{
    // Thin world-space health bar that floats above the enemy and only shows
    // for a short window after the enemy was last damaged. This way you only
    // see the health bar of the enemy you're currently shooting at, not every
    // enemy on screen at once.
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private EnemyBase target;
        [SerializeField] private SpriteRenderer fillRenderer;
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private float fullWidth = 1.1f;
        [SerializeField] private float thickness = 0.10f;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0.85f, 0f);
        [SerializeField] private float showSecondsAfterHit = 2f;

        private void LateUpdate()
        {
            if (target == null) { gameObject.SetActive(false); return; }
            transform.position = target.transform.position + offset;

            var visible = target.TimeSinceDamage < showSecondsAfterHit && target.CurrentHealth > 0;
            if (fillRenderer       != null) fillRenderer.enabled = visible;
            if (backgroundRenderer != null) backgroundRenderer.enabled = visible;
            if (!visible) return;

            var ratio = target.HealthRatio;
            // Fill bar scales horizontally from full to 0; background stays at fullWidth.
            if (fillRenderer != null)
            {
                var s = fillRenderer.transform.localScale;
                s.x = fullWidth * Mathf.Clamp01(ratio);
                s.y = thickness;
                fillRenderer.transform.localScale = s;
                fillRenderer.color = Color.Lerp(new Color(0.95f, 0.20f, 0.20f), new Color(0.30f, 0.85f, 0.35f), ratio);
            }
            if (backgroundRenderer != null)
            {
                var bs = backgroundRenderer.transform.localScale;
                bs.x = fullWidth;
                bs.y = thickness;
                backgroundRenderer.transform.localScale = bs;
            }
        }
    }
}
