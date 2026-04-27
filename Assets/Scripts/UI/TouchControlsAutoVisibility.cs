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
        }

        private void OnEnable() => InputSystem.onDeviceChange += OnDeviceChange;
        private void OnDisable() => InputSystem.onDeviceChange -= OnDeviceChange;
        private void Start() => Refresh();

        private void OnDeviceChange(InputDevice _, InputDeviceChange __) => Refresh();

        private void Refresh()
        {
            // Show on touch devices, hide otherwise. Editor / standalone builds
            // typically have a Mouse + Keyboard but no Touchscreen, so the
            // overlay disappears unless the user is testing on-device.
            var hasTouch = Touchscreen.current != null;
            var visible = hasTouch && Gamepad.current == null;
            _group.alpha = visible ? 1f : 0f;
            _group.interactable = visible;
            _group.blocksRaycasts = visible;
        }
    }
}
