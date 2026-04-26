using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GunSlugsClone.Input
{
    public enum ControlScheme { TouchOrKeyboard, Gamepad }

    public sealed class ControlSchemeWatcher : MonoBehaviour
    {
        public static ControlSchemeWatcher Instance { get; private set; }
        public ControlScheme Current { get; private set; } = ControlScheme.TouchOrKeyboard;
        public event Action<ControlScheme> SchemeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Refresh();
        }

        private void OnEnable() => InputSystem.onDeviceChange += OnDeviceChange;
        private void OnDisable() => InputSystem.onDeviceChange -= OnDeviceChange;

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Added || change == InputDeviceChange.Removed
                || change == InputDeviceChange.Reconnected || change == InputDeviceChange.Disconnected)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            var hasGamepad = Gamepad.current != null;
            var next = hasGamepad ? ControlScheme.Gamepad : ControlScheme.TouchOrKeyboard;
            if (next == Current) return;
            Current = next;
            SchemeChanged?.Invoke(Current);
        }
    }
}
