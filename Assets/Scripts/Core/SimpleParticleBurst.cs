using UnityEngine;

namespace GunSlugsClone.Core
{
    // Lightweight particle effect that doesn't depend on Unity's ParticleSystem
    // shader/material plumbing — too fragile to set up correctly across URP 2D
    // / built-in pipeline / different Unity versions. Spawns N child GameObjects
    // each with a SpriteRenderer (using a runtime-generated white sprite,
    // tinted by `color`) and a Rigidbody2D pushed in a random direction within
    // a cone, then destroys itself after `lifetime` seconds.
    public sealed class SimpleParticleBurst : MonoBehaviour
    {
        [SerializeField] private int count = 16;
        [SerializeField] private float speed = 5f;
        [SerializeField] private float speedJitter = 1.5f;
        [SerializeField] private float lifetime = 0.6f;
        [SerializeField] private float size = 0.2f;
        [SerializeField] private Color color = new Color(1f, 0.4f, 0.4f, 1f);
        [SerializeField] private float gravityScale = 1.5f;
        [SerializeField] private float coneAngleDegrees = 360f;
        [SerializeField] private Vector2 baseDirection = Vector2.up;
        [SerializeField] private int sortingOrder = 10;
        // Optional override — if assigned via the Editor, particles use this
        // sprite (e.g. a Kenney flash/explosion frame) instead of the runtime
        // white square. Lets us swap to real art without rewriting the burst.
        [SerializeField] private Sprite overrideSprite;

        private static Sprite _sharedSprite;

        private void Awake() => Burst();

        private void Burst()
        {
            var sprite = overrideSprite != null ? overrideSprite : GetSharedSprite();
            for (var i = 0; i < count; i++)
            {
                var p = new GameObject($"Particle_{i}");
                p.transform.SetParent(transform, worldPositionStays: false);
                p.transform.localScale = new Vector3(size, size, 1f);

                var sr = p.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = color;
                sr.sortingOrder = sortingOrder;

                var rb = p.AddComponent<Rigidbody2D>();
                rb.gravityScale = gravityScale;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.freezeRotation = false;

                // Pick a direction either anywhere on the unit circle (cone>=360)
                // or within a cone centred on baseDirection.
                var dir = ResolveDirection();
                var s = speed + Random.Range(-speedJitter, speedJitter);
                rb.linearVelocity = dir * s;
                rb.angularVelocity = Random.Range(-360f, 360f);
            }
            Destroy(gameObject, lifetime);
        }

        private Vector2 ResolveDirection()
        {
            if (coneAngleDegrees >= 359.99f)
            {
                var theta = Random.Range(0f, Mathf.PI * 2f);
                return new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
            }
            var basis = baseDirection.sqrMagnitude > 0.0001f ? baseDirection.normalized : Vector2.up;
            var spread = Random.Range(-coneAngleDegrees * 0.5f, coneAngleDegrees * 0.5f);
            return (Vector2)(Quaternion.Euler(0f, 0f, spread) * basis);
        }

        private static Sprite GetSharedSprite()
        {
            if (_sharedSprite != null) return _sharedSprite;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, mipChain: false)
            {
                name = "SimpleParticleBurst_Tex",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[16];
            for (var i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply(updateMipmaps: false);
            _sharedSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), pixelsPerUnit: 4f);
            _sharedSprite.hideFlags = HideFlags.HideAndDontSave;
            _sharedSprite.name = "SimpleParticleBurst_Sprite";
            return _sharedSprite;
        }
    }
}
