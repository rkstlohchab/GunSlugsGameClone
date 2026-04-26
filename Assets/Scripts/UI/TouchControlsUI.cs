using GunSlugsClone.Input;
using UnityEngine;

namespace GunSlugsClone.UI
{
    // Hides itself automatically when a gamepad is present. Bind UI children to
    // OnScreenStick / OnScreenButton components from Unity's Input System package.
    public sealed class TouchControlsUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private bool autoHideOnGamepad = true;

        private void OnEnable()
        {
            if (ControlSchemeWatcher.Instance != null)
            {
                ControlSchemeWatcher.Instance.SchemeChanged += OnSchemeChanged;
                Apply(ControlSchemeWatcher.Instance.Current);
            }
        }

        private void OnDisable()
        {
            if (ControlSchemeWatcher.Instance != null)
                ControlSchemeWatcher.Instance.SchemeChanged -= OnSchemeChanged;
        }

        private void OnSchemeChanged(ControlScheme s) => Apply(s);

        private void Apply(ControlScheme s)
        {
            if (!autoHideOnGamepad || group == null) return;
            var visible = s == ControlScheme.TouchOrKeyboard;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
