using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GunSlugsClone.Core
{
    public enum GameState { Boot, MainMenu, Hub, Playing, Paused, Results }

    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public GameState State { get; private set; } = GameState.Boot;
        public event Action<GameState, GameState> StateChanged;

        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string hubScene = "Hub";
        [SerializeField] private string gameScene = "Game";

        public RunContext CurrentRun { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start() => StartCoroutine(BootSequence());

        private IEnumerator BootSequence()
        {
            SaveSystem.Load();
            AudioManager.Instance?.Initialise();
            yield return null;
            GoToMainMenu();
        }

        public void GoToMainMenu() => StartCoroutine(LoadAndSetState(mainMenuScene, GameState.MainMenu));
        public void GoToHub() => StartCoroutine(LoadAndSetState(hubScene, GameState.Hub));

        public void StartRun(RunContext context)
        {
            CurrentRun = context;
            StartCoroutine(LoadAndSetState(gameScene, GameState.Playing));
        }

        public void Pause()
        {
            if (State != GameState.Playing) return;
            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        public void EndRun(int finalScore, float duration)
        {
            Time.timeScale = 1f;
            EventBus.Publish(new RunCompletedEvent(finalScore, duration));
            SetState(GameState.Results);
        }

        private IEnumerator LoadAndSetState(string sceneName, GameState newState)
        {
            Time.timeScale = 1f;
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (op != null && !op.isDone) yield return null;
            SetState(newState);
        }

        private void SetState(GameState next)
        {
            if (State == next) return;
            var prev = State;
            State = next;
            StateChanged?.Invoke(prev, next);
        }
    }

    public sealed class RunContext
    {
        public int Seed;
        public string[] PlayerCharacterIds;
        public string StartingBiomeId;
        public bool TwoPlayerCoop;
    }
}
