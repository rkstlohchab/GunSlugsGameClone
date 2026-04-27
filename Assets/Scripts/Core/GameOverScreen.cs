using UnityEngine;
using UnityEngine.SceneManagement;

namespace GunSlugsClone.Core
{
    // Stop-gap UI for the smoke test: shows a top-left score HUD that ticks up
    // on EnemyKilledEvent, and a full-screen "YOU DIED" panel with a Restart
    // button when PlayerDiedEvent fires. Drawn via OnGUI so it doesn't depend
    // on a Canvas + EventSystem + TMP Essentials chain. M7 polish replaces
    // this with a real Canvas-based UI.
    public sealed class GameOverScreen : MonoBehaviour
    {
        private bool _dead;
        private int _score;

        private GUIStyle _titleStyle;
        private GUIStyle _hudLeft;
        private GUIStyle _hudCenter;
        private GUIStyle _btnStyle;

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerDiedEvent>(OnDied);
            EventBus.Subscribe<EnemyKilledEvent>(OnKilled);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerDiedEvent>(OnDied);
            EventBus.Unsubscribe<EnemyKilledEvent>(OnKilled);
        }

        private void OnKilled(EnemyKilledEvent e) => _score += e.ScoreReward;
        private void OnDied(PlayerDiedEvent e) => _dead = true;

        private void OnGUI()
        {
            EnsureStyles();
            var w = Screen.width;
            var h = Screen.height;

            // Always-visible score
            GUI.Label(new Rect(20, 20, 320, 40), $"Score: {_score}", _hudLeft);

            if (!_dead) return;

            // Dim background
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = prev;

            GUI.Label(new Rect(0, h * 0.32f, w, 100), "YOU DIED", _titleStyle);
            GUI.Label(new Rect(0, h * 0.46f, w, 40), $"Final Score: {_score}", _hudCenter);

            if (GUI.Button(new Rect(w / 2f - 110, h * 0.55f, 220, 60), "Restart", _btnStyle))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
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
