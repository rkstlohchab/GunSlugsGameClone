using System.IO;
using GunSlugsClone.Core;
using GunSlugsClone.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace GunSlugsClone.EditorTools
{
    // One-click smoke test: GunSlugs > Build Smoke Test Scene
    // Builds a fresh SmokeTest scene with a camera, ground, and a fully-wired Player
    // (Rigidbody2D + collider + PlayerController + Health + WeaponHolder + GroundCheck child + PlayerInput).
    // Visual squares are produced by the ProceduralSquare runtime component — no PNG/Sprite asset
    // import is required, which avoids Unity's flaky first-time texture import timing.
    public static class SmokeTestSetup
    {
        private const string ScenePath        = "Assets/Scenes/SmokeTest.unity";
        private const string InputActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

        [MenuItem("GunSlugs/Build Smoke Test Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "Stop Play Mode First",
                    "Play Mode is active. Click the ▶ button at the top of the Editor to stop it, then run this menu again.",
                    "OK");
                return;
            }

            EnsureFolder("Assets/Scenes");

            var scene = OpenOrCreateScene();
            ClearScene(scene);

            CreateCamera();
            CreateGround();
            var player = CreatePlayer();
            WirePlayerInput(player);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            Selection.activeGameObject = player;
            SceneView.lastActiveSceneView?.FrameSelected();
            EditorUtility.DisplayDialog(
                "GunSlugs",
                "Smoke test scene built. Press the Play button (▶) at the top of the Editor.\n\nWASD/arrows = move, Space = jump.",
                "OK");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path);
            var leaf = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || !AssetDatabase.IsValidFolder(parent))
                AssetDatabase.CreateFolder("Assets", leaf);
            else
                AssetDatabase.CreateFolder(parent, leaf);
        }

        private static UnityEngine.SceneManagement.Scene OpenOrCreateScene()
        {
            if (File.Exists(ScenePath))
                return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(s, ScenePath);
            return s;
        }

        private static void ClearScene(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var go in scene.GetRootGameObjects())
                Object.DestroyImmediate(go);
        }

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0, 0, -10);
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.12f, 0.16f, 1f);
            go.AddComponent<AudioListener>();
        }

        private static void CreateGround()
        {
            var go = new GameObject("Ground");
            go.transform.position = new Vector3(0, -3, 0);
            go.transform.localScale = new Vector3(20, 1, 1);
            go.AddComponent<SpriteRenderer>();
            var ps = go.AddComponent<ProceduralSquare>();
            SetSerializedColor(ps, new Color(0.42f, 0.70f, 0.34f));
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f);
        }

        private static GameObject CreatePlayer()
        {
            var go = new GameObject("Player");
            go.transform.position = new Vector3(0, 0, 0);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 1;
            var ps = go.AddComponent<ProceduralSquare>();
            SetSerializedColor(ps, new Color(0.95f, 0.62f, 0.20f));

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3.5f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f);

            go.AddComponent<PlayerHealth>();
            go.AddComponent<WeaponHolder>();
            var ctrl = go.AddComponent<PlayerController>();

            var groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(go.transform, worldPositionStays: false);
            groundCheck.transform.localPosition = new Vector3(0, -0.5f, 0);

            var so = new SerializedObject(ctrl);
            var prop = so.FindProperty("groundCheck");
            if (prop != null)
            {
                prop.objectReferenceValue = groundCheck.transform;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else Debug.LogWarning("[SmokeTestSetup] Could not find groundCheck SerializedProperty on PlayerController.");

            return go;
        }

        private static void WirePlayerInput(GameObject player)
        {
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (actions == null)
            {
                Debug.LogError($"[SmokeTestSetup] Missing InputActionAsset at {InputActionsPath} — input will not work.");
                return;
            }
            var pi = player.AddComponent<PlayerInput>();
            pi.actions = actions;
            pi.defaultActionMap = "Gameplay";
            pi.notificationBehavior = PlayerNotifications.SendMessages;
        }

        private static void SetSerializedColor(ProceduralSquare ps, Color color)
        {
            var so = new SerializedObject(ps);
            var prop = so.FindProperty("color");
            if (prop != null)
            {
                prop.colorValue = color;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
