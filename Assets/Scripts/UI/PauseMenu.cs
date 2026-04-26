using GunSlugsClone.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GunSlugsClone.UI
{
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button mainMenuButton;

        private void Awake()
        {
            if (resumeButton) resumeButton.onClick.AddListener(() => GameManager.Instance?.Resume());
            if (mainMenuButton) mainMenuButton.onClick.AddListener(() => GameManager.Instance?.GoToMainMenu());
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null) GameManager.Instance.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameState prev, GameState next)
        {
            if (panel) panel.SetActive(next == GameState.Paused);
        }
    }
}
