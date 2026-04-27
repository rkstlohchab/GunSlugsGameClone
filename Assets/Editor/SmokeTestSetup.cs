using System.IO;
using GunSlugsClone.Core;
using GunSlugsClone.Player;
using GunSlugsClone.Weapons;
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
        private const string BulletPrefabPath = "Assets/Prefabs/Bullet.prefab";
        private const string PistolAssetPath  = "Assets/ScriptableObjects/Weapons/weapon_pistol.asset";

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
            var bulletPrefab = EnsureBulletPrefab();
            var pistolData   = EnsurePistolAsset(bulletPrefab);

            var scene = OpenOrCreateScene();
            ClearScene(scene);

            CreateCamera();
            CreateGround();
            var player = CreatePlayer(pistolData);
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

        private static GameObject CreatePlayer(WeaponData pistol)
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
            var weaponHolder = go.AddComponent<WeaponHolder>();
            var ctrl = go.AddComponent<PlayerController>();

            var groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(go.transform, worldPositionStays: false);
            groundCheck.transform.localPosition = new Vector3(0, -0.5f, 0);

            // Muzzle is offset outside the player's BoxCollider2D so spawned bullets
            // don't immediately self-destruct from a contact with the player itself.
            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(go.transform, worldPositionStays: false);
            muzzle.transform.localPosition = new Vector3(0.8f, 0.05f, 0);

            var ctrlSo = new SerializedObject(ctrl);
            var groundProp = ctrlSo.FindProperty("groundCheck");
            if (groundProp != null)
            {
                groundProp.objectReferenceValue = groundCheck.transform;
                ctrlSo.ApplyModifiedPropertiesWithoutUndo();
            }
            else Debug.LogWarning("[SmokeTestSetup] Could not find groundCheck SerializedProperty on PlayerController.");

            var whSo = new SerializedObject(weaponHolder);
            var muzzleProp = whSo.FindProperty("muzzle");
            if (muzzleProp != null) muzzleProp.objectReferenceValue = muzzle.transform;
            var loadout = whSo.FindProperty("startingLoadout");
            if (loadout != null && pistol != null)
            {
                loadout.arraySize = 1;
                loadout.GetArrayElementAtIndex(0).objectReferenceValue = pistol;
            }
            whSo.ApplyModifiedPropertiesWithoutUndo();

            return go;
        }

        private static GameObject EnsureBulletPrefab()
        {
            EnsureFolder("Assets/Prefabs");
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(BulletPrefabPath);
            if (existing != null) return existing;

            var go = new GameObject("Bullet");
            go.transform.localScale = new Vector3(0.25f, 0.25f, 1f);

            go.AddComponent<SpriteRenderer>();
            var ps = go.AddComponent<ProceduralSquare>();
            SetSerializedColor(ps, new Color(1f, 0.85f, 0.30f));

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f; // local; world radius = 0.125 with 0.25 scale

            go.AddComponent<Projectile>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, BulletPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static WeaponData EnsurePistolAsset(GameObject bulletPrefab)
        {
            EnsureFolder("Assets/ScriptableObjects");
            EnsureFolder("Assets/ScriptableObjects/Weapons");

            var pistol = AssetDatabase.LoadAssetAtPath<WeaponData>(PistolAssetPath);
            if (pistol == null)
            {
                pistol = ScriptableObject.CreateInstance<WeaponData>();
                AssetDatabase.CreateAsset(pistol, PistolAssetPath);
            }

            var so = new SerializedObject(pistol);
            so.FindProperty("Id").stringValue = "weapon_pistol";
            so.FindProperty("DisplayName").stringValue = "Pistol";
            so.FindProperty("Mode").enumValueIndex = (int)FireMode.SemiAuto;
            so.FindProperty("Damage").intValue = 10;
            so.FindProperty("FireRate").floatValue = 4f;
            so.FindProperty("ProjectilesPerShot").intValue = 1;
            so.FindProperty("SpreadDegrees").floatValue = 1f;
            so.FindProperty("ProjectileSpeed").floatValue = 18f;
            so.FindProperty("ProjectileLifetime").floatValue = 1.5f;
            so.FindProperty("Knockback").floatValue = 1f;
            so.FindProperty("Recoil").floatValue = 0f;
            so.FindProperty("Infinite").boolValue = true;
            so.FindProperty("MagazineSize").intValue = 12;
            so.FindProperty("ReloadSeconds").floatValue = 1.0f;
            so.FindProperty("BurstCount").intValue = 1;
            so.FindProperty("BurstInterval").floatValue = 0f;
            so.FindProperty("ProjectilePrefab").objectReferenceValue = bulletPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pistol);
            AssetDatabase.SaveAssets();
            return pistol;
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
