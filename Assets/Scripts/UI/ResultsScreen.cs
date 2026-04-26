using GunSlugsClone.Core;
using GunSlugsClone.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GunSlugsClone.UI
{
    public sealed class ResultsScreen : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text durationText;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private string leaderboardId = "main_score";

        private void Awake()
        {
            if (mainMenuButton) mainMenuButton.onClick.AddListener(() => GameManager.Instance?.GoToMainMenu());
            if (playAgainButton) playAgainButton.onClick.AddListener(() => GameManager.Instance?.GoToHub());
        }

        private void OnEnable() => EventBus.Subscribe<RunCompletedEvent>(OnRunCompleted);
        private void OnDisable() => EventBus.Unsubscribe<RunCompletedEvent>(OnRunCompleted);

        private async void OnRunCompleted(RunCompletedEvent e)
        {
            if (panel) panel.SetActive(true);
            if (scoreText) scoreText.text = e.FinalScore.ToString("N0");
            if (durationText)
            {
                var ts = System.TimeSpan.FromSeconds(e.DurationSeconds);
                durationText.text = $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";
            }

            // Persist high score locally and submit to platform leaderboard.
            if (e.FinalScore > SaveSystem.Data.HighScore)
            {
                SaveSystem.Data.HighScore = e.FinalScore;
                SaveSystem.Save();
            }

            var svc = LeaderboardServiceFactory.Get();
            if (!svc.IsAuthenticated) await svc.AuthenticateAsync();
            await svc.SubmitScoreAsync(leaderboardId, e.FinalScore);
        }
    }
}
