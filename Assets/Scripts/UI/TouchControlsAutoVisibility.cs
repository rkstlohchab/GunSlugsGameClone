using UnityEngine;
using UnityEngine.InputSystem;

namespace GunSlugsClone.UI
{
    // Hide the touch-controls Canvas when a gamepad or non-touch primary
    // device is the active input. On mobile (no gamepad, no keyboard) it
    // stays visible.
    [RequireComponent(typeof(Canvas))]
    public sealed class TouchControlsAutoVisibility : MonoBehaviour
    {
        private CanvasGroup _group;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            // Disable the whole Canvas off-mobile so its Scene-view rectangle
            // also disappears (CanvasGroup alpha alone keeps the canvas
            // visualised in Scene view).
            if (!Application.isMobilePlatform)
            {
                var c = GetComponent<Canvas>();
                if (c != null) c.enabled = false;
            }
        }

        private void OnEnable() => InputSystem.onDeviceChange += OnDeviceChange;
        private void OnDisable() => InputSystem.onDeviceChange -= OnDeviceChange;
        private void Start() => Refresh();

        private void OnDeviceChange(InputDevice _, InputDeviceChange __) => Refresh();

        private void Refresh()
        {
            // Show only on real mobile platforms (iOS/Android builds). In the
            // Editor and on desktop builds Application.isMobilePlatform is
            // false, so the overlay stays hidden even when Unity surfaces a
            // pseudo-Touchscreen device (Unity Remote, simulator, etc.).
            var visible = Application.isMobilePlatform
                          && Touchscreen.current != null
                          && Gamepad.current == null;
            _group.alpha = visible ? 1f : 0f;
            _group.interactable = visible;
            _group.blocksRaycasts = visible;
        }
    }
}
