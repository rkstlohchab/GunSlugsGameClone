using GunSlugsClone.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GunSlugsClone.UI
{
    public sealed class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button quitButton;

        private void Awake()
        {
            if (playButton) playButton.onClick.AddListener(() => GameManager.Instance?.GoToHub());
            if (settingsButton) settingsButton.onClick.AddListener(() => settingsPanel?.SetActive(true));
            if (quitButton) quitButton.onClick.AddListener(Application.Quit);
        }
    }
}
