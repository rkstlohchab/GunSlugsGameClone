using System.IO;
using GunSlugsClone.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace GunSlugsClone.EditorTools
{
    // One-click smoke test: GunSlugs > Build Smoke Test Scene
    // Creates a new SmokeTest scene with camera, ground, and a fully-wired Player
    // (Rigidbody2D, BoxCollider2D, PlayerController + Health + WeaponHolder, GroundCheck child,
    // PlayerInput component bound to InputSystem_Actions.inputactions in SendMessages mode).
    // Generates a tiny white sprite asset on first run so we don't depend on the 2D Sprite package.
    public static class SmokeTestSetup
    {
        private const string ScenePath        = "Assets/Scenes/SmokeTest.unity";
        private const string ArtFolder        = "Assets/Art";
        private const string SpritePath       = "Assets/Art/white_pixel.png";
        private const string InputActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

        [MenuItem("GunSlugs/Build Smoke Test Scene")]
        public static void Build()
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder(ArtFolder);
            var sprite = EnsureWhiteSprite();

            var scene = OpenOrCreateScene();
            ClearScene(scene);

            CreateCamera();
            CreateGround(sprite);
            var player = CreatePlayer(sprite);
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

        private static Sprite EnsureWhiteSprite()
        {
            // Always (re)write + (re)import. Cheap, and avoids the case where the
            // PNG exists on disk but its sprite sub-asset never finished importing.
            if (!File.Exists(SpritePath))
            {
                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                var pixels = new Color32[16];
                for (var i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
                tex.SetPixels32(pixels);
                tex.Apply();
                File.WriteAllBytes(SpritePath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            var importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 4f; // 4x4 sprite = 1 world unit
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            // Sub-asset may take a frame to materialise; scan all assets at the path.
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (sprite != null) return sprite;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(SpritePath))
                if (obj is Sprite s) return s;

            Debug.LogWarning("[SmokeTestSetup] White sprite not yet imported. Renderers will be invisible " +
                             "but collision will still work. Run 'Build Smoke Test Scene' once more to bind it.");
            return null;
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

        private static void CreateGround(Sprite sprite)
        {
            var go = new GameObject("Ground");
            go.transform.position = new Vector3(0, -3, 0);
            go.transform.localScale = new Vector3(20, 1, 1);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.42f, 0.70f, 0.34f);
            // Explicit size so collision works even if the sprite is still importing.
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f);
        }

        private static GameObject CreatePlayer(Sprite sprite)
        {
            var go = new GameObject("Player");
            go.transform.position = new Vector3(0, 0, 0);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.95f, 0.62f, 0.20f);
            sr.sortingOrder = 1;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3.5f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f); // explicit, sprite-independent
            go.AddComponent<PlayerHealth>();
            go.AddComponent<WeaponHolder>();
            var ctrl = go.AddComponent<PlayerController>();

            var groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(go.transform, worldPositionStays: false);
            groundCheck.transform.localPosition = new Vector3(0, -0.5f, 0);

            // Wire the [SerializeField] groundCheck on PlayerController without reflection
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
    }
}
