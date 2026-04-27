using UnityEngine;

namespace GunSlugsClone.Player
{
    // Simple world-space health bar that follows a Player and scales horizontally
    // with current/max HP. Lives at top level (not parented to the Player) so it
    // isn't flipped when PlayerController mirrors the Player's localScale on facing.
    public sealed class HealthBar : MonoBehaviour
    {
        [SerializeField] private PlayerHealth target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0.7f, 0f);
        [SerializeField] private float fullWidth = 1f;
        [SerializeField] private float thickness = 0.15f;

        private void LateUpdate()
        {
            if (target == null) { gameObject.SetActive(false); return; }
            transform.position = target.transform.position + offset;

            var ratio = target.Max > 0 ? (float)target.Current / target.Max : 0f;
            var scale = transform.localScale;
            scale.x = fullWidth * Mathf.Clamp01(ratio);
            scale.y = thickness;
            transform.localScale = scale;
        }
    }
}
