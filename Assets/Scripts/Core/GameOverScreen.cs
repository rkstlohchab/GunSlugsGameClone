using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace GunSlugsClone.Core
{
    // Stop-gap UI for the smoke test:
    //   * top-left score HUD that ticks up on EnemyKilledEvent
    //   * top-left wave readout
    //   * full-screen "YOU DIED" panel on PlayerDiedEvent
    //   * full-screen "VICTORY!" panel on AllWavesClearedEvent
    //   * full-screen "PAUSED" panel toggled by Escape (sets Time.timeScale=0)
    // Drawn via OnGUI so it doesn't depend on a Canvas + EventSystem + TMP
    // Essentials chain. M7 polish replaces this with a real Canvas-based UI.
    public sealed class GameOverScreen : MonoBehaviour
    {
        private enum OverlayState { None, Dead, Victory, Paused }

        private OverlayState _overlay = OverlayState.None;
        private int _score;
        private WaveSpawner _waveSpawner;

        private GUIStyle _titleStyle;
        private GUIStyle _hudLeft;
        private GUIStyle _hudCenter;
        private GUIStyle _btnStyle;

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerDiedEvent>(OnDied);
            EventBus.Subscribe<EnemyKilledEvent>(OnKilled);
            EventBus.Subscribe<AllWavesClearedEvent>(OnVictory);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerDiedEvent>(OnDied);
            EventBus.Unsubscribe<EnemyKilledEvent>(OnKilled);
            EventBus.Unsubscribe<AllWavesClearedEvent>(OnVictory);
        }

        private void Start()
        {
            // FindFirstObjectByType is fine here — a single WaveSpawner is in the
            // scene and we only resolve it once at scene start.
            _waveSpawner = FindFirstObjectByType<WaveSpawner>();
        }

        private void Update()
        {
            // Pause toggle on Escape. Read directly from Keyboard so we don't
            // need to plumb pause through PlayerInput / GameManager for the
            // smoke test.
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (_overlay == OverlayState.None)
                {
                    _overlay = OverlayState.Paused;
                    Time.timeScale = 0f;
                }
                else if (_overlay == OverlayState.Paused)
                {
                    _overlay = OverlayState.None;
                    Time.timeScale = 1f;
                }
            }
        }

        private void OnKilled(EnemyKilledEvent e) => _score += e.ScoreReward;
        private void OnDied(PlayerDiedEvent e)
        {
            _overlay = OverlayState.Dead;
            Time.timeScale = 0f;
        }
        private void OnVictory(AllWavesClearedEvent e)
        {
            if (_overlay == OverlayState.Dead) return; // dying mid-victory keeps death screen
            _overlay = OverlayState.Victory;
            Time.timeScale = 0f;
        }

        private void OnGUI()
        {
            EnsureStyles();
            var w = Screen.width;
            var h = Screen.height;

            // Always-visible HUD: score top-left, wave below.
            GUI.Label(new Rect(20, 20, 320, 40), $"Score: {_score}", _hudLeft);
            if (_waveSpawner != null)
                GUI.Label(new Rect(20, 56, 320, 30), $"Wave: {_waveSpawner.CurrentWave} / {_waveSpawner.TotalWaves}", _hudLeft);

            if (_overlay == OverlayState.None) return;

            // Dim background
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = prev;

            switch (_overlay)
            {
                case OverlayState.Dead:    DrawDeadOverlay(w, h);    break;
                case OverlayState.Victory: DrawVictoryOverlay(w, h); break;
                case OverlayState.Paused:  DrawPausedOverlay(w, h);  break;
            }
        }

        private void DrawDeadOverlay(int w, int h)
        {
            GUI.Label(new Rect(0, h * 0.30f, w, 100), "YOU DIED", _titleStyle);
            GUI.Label(new Rect(0, h * 0.44f, w, 40), $"Final Score: {_score}", _hudCenter);
            if (GUI.Button(new Rect(w / 2f - 110, h * 0.55f, 220, 60), "Restart", _btnStyle))
                Reload();
        }

        private void DrawVictoryOverlay(int w, int h)
        {
            GUI.Label(new Rect(0, h * 0.28f, w, 100), "VICTORY!", _titleStyle);
            GUI.Label(new Rect(0, h * 0.42f, w, 40), $"Final Score: {_score}", _hudCenter);
            GUI.Label(new Rect(0, h * 0.48f, w, 40), "All waves cleared.", _hudCenter);
            if (GUI.Button(new Rect(w / 2f - 110, h * 0.58f, 220, 60), "Play Again", _btnStyle))
                Reload();
        }

        private void DrawPausedOverlay(int w, int h)
        {
            GUI.Label(new Rect(0, h * 0.28f, w, 100), "PAUSED", _titleStyle);
            if (GUI.Button(new Rect(w / 2f - 110, h * 0.46f, 220, 60), "Resume", _btnStyle))
            {
                _overlay = OverlayState.None;
                Time.timeScale = 1f;
            }
            if (GUI.Button(new Rect(w / 2f - 110, h * 0.55f, 220, 60), "Restart", _btnStyle))
                Reload();
            if (GUI.Button(new Rect(w / 2f - 110, h * 0.64f, 220, 60), "Quit", _btnStyle))
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        private static void Reload()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void EnsureStyles()
        {
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 64,
                    fontStyle = FontStyle.Bold,
                };
                _titleStyle.normal.textColor = Color.white;
            }
            if (_hudLeft == null)
            {
                _hudLeft = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 22,
                    fontStyle = FontStyle.Bold,
                };
                _hudLeft.normal.textColor = Color.white;
            }
            if (_hudCenter == null)
            {
                _hudCenter = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 24,
                };
                _hudCenter.normal.textColor = Color.white;
            }
            if (_btnStyle == null)
            {
                _btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 22 };
            }
        }
    }
}
