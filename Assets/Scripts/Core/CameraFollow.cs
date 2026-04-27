using UnityEngine;

namespace GunSlugsClone.Core
{
    // Smoothed 2D follow for the main camera. Lerps toward the target's position
    // every LateUpdate so the camera moves after the player has finished its
    // physics step. Cinemachine 3 takes over later when we need confiner-per-room
    // (M2) and co-op group framing (M5).
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -10f);
        [SerializeField, Min(0f)] private float damping = 6f;
        [SerializeField] private bool snapOnFirstFrame = true;

        private bool _hasSnapped;

        public void SetTarget(Transform t) => target = t;

        private void LateUpdate()
        {
            if (target == null) return;
            var desired = target.position + offset;

            if (snapOnFirstFrame && !_hasSnapped)
            {
                transform.position = desired;
                _hasSnapped = true;
                return;
            }

            transform.position = Vector3.Lerp(transform.position, desired, damping * Time.deltaTime);
        }
    }
}
