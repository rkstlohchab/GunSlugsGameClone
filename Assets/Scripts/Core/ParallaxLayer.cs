using UnityEngine;

namespace GunSlugsClone.Core
{
    // Single parallax layer that follows the main camera at a fraction of its
    // movement. parallaxFactor=0 freezes the layer in place (true skybox);
    // parallaxFactor=1 makes it lock to the camera (no parallax). Mid layers
    // sit between (e.g. 0.3 for far hills, 0.7 for foreground silhouettes).
    [ExecuteAlways]
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [Range(0f, 1f)] [SerializeField] private float parallaxFactor = 0.3f;
        [SerializeField] private Vector2 baseOffset;

        private Camera _cam;
        private Vector3 _origin;

        private void OnEnable()
        {
            _origin = transform.position - new Vector3(baseOffset.x, baseOffset.y, 0f);
        }

        private void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;
            var camPos = _cam.transform.position;
            var p = transform.position;
            p.x = _origin.x + camPos.x * parallaxFactor + baseOffset.x;
            p.y = _origin.y + camPos.y * parallaxFactor * 0.5f + baseOffset.y;
            transform.position = p;
        }
    }
}
