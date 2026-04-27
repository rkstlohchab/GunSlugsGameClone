using UnityEngine;

namespace GunSlugsClone.Core
{
    // Stop-gap visual: paints a SpriteRenderer with a procedurally-generated white square
    // (tinted via Color) at OnEnable. Lets us run the smoke test before any real sprite art
    // is in the project, with zero dependency on Unity's asset import pipeline.
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ProceduralSquare : MonoBehaviour
    {
        [SerializeField] private Color color = Color.white;

        private static Texture2D _sharedTex;
        private static Sprite _sharedSprite;

        private void OnEnable() => Apply();

        private void Apply()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;
            // If something already populated the sprite (e.g. a real Kenney
            // texture imported into the scene), don't clobber it. Just apply
            // the tint.
            if (sr.sprite == null) sr.sprite = GetSharedSprite();
            sr.color = color;
        }

        private static Sprite GetSharedSprite()
        {
            if (_sharedSprite != null) return _sharedSprite;

            if (_sharedTex == null)
            {
                _sharedTex = new Texture2D(4, 4, TextureFormat.RGBA32, mipChain: false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    name = "ProceduralSquare_Tex",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                var pixels = new Color32[16];
                for (var i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
                _sharedTex.SetPixels32(pixels);
                _sharedTex.Apply(updateMipmaps: false);
            }

            _sharedSprite = Sprite.Create(_sharedTex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), pixelsPerUnit: 4f);
            _sharedSprite.hideFlags = HideFlags.HideAndDontSave;
            _sharedSprite.name = "ProceduralSquare_Sprite";
            return _sharedSprite;
        }
    }
}
