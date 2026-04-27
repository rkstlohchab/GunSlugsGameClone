using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GunSlugsClone.Core;
using GunSlugsClone.Enemies;
using GunSlugsClone.Enemies.AI;
using GunSlugsClone.Player;
using GunSlugsClone.Procedural;
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
        private const string EnemyDataPath   = "Assets/ScriptableObjects/Enemies/enemy_grunt.asset";
        private const string EnemyPrefabPath  = "Assets/Prefabs/Enemy_Grunt.prefab";
        private const string ChargerDataPath  = "Assets/ScriptableObjects/Enemies/enemy_charger.asset";
        private const string ChargerPrefabPath = "Assets/Prefabs/Enemy_Charger.prefab";
        private const string RoomPrefabPath   = "Assets/Prefabs/RoomTemplate_Standard.prefab";
        private const string HealthPickupPath = "Assets/Prefabs/HealthPickup.prefab";
        private const string DeathBurstPath   = "Assets/Prefabs/Vfx_DeathBurst.prefab";
        private const string MuzzleFlashPath  = "Assets/Prefabs/Vfx_MuzzleFlash.prefab";

        private const string KenneySource = "/Users/raksithlochabb/Downloads/kenneypack/kenney_pixel-platformer";
        private const string KenneyDest   = "Assets/Art/Kenney";

        // Curated subset of the Kenney pixel-platformer pack copied into the
        // project on first build. Once these PNGs are committed they ride along
        // with the repo, so a fresh clone on another machine doesn't need the
        // user's Downloads folder.
        private static readonly (string src, string dst, int pixelsPerUnit)[] KenneyFiles = new[]
        {
            ("Tiles/Characters/tile_0000.png", "character_player.png",  24),
            ("Tiles/Characters/tile_0024.png", "character_enemy.png",   24),
            ("Tiles/Characters/tile_0009.png", "character_charger.png", 24),
            ("Tiles/tile_0006.png",            "tile_floor.png",        18),
            ("Tiles/tile_0044.png",            "tile_heart.png",        18),
            ("Tiles/tile_0151.png",            "tile_bullet.png",       18),
            ("Tiles/Backgrounds/tile_0000.png","tile_bg.png",            18),
        };

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
            ImportKenneyArt();
            EnsureBulletPrefab();
            EnsureMuzzleFlashPrefab();
            EnsurePistolAsset();
            EnsureEnemyAssets();
            EnsureChargerAssets();
            EnsureRoomTemplatePrefab();
            EnsureHealthPickupPrefab();
            EnsureDeathBurstPrefab();

            var scene = OpenOrCreateScene();
            ClearScene(scene);

            var camera = CreateCamera();
            CreateBackground();

            // Three rooms strung along X. Center room is the spawn room; the
            // door gaps in the side walls let the player walk between them.
            const float roomWidth = 30f;
            var roomMiddle = InstantiateRoom(Vector3.zero);
            var roomLeft   = InstantiateRoom(new Vector3(-roomWidth, 0f, 0f));
            var roomRight  = InstantiateRoom(new Vector3( roomWidth, 0f, 0f));

            var player = CreatePlayer();
            PositionPlayerAtRoomSpawn(player, roomMiddle);
            WirePlayerInput(player);

            // Wave-based spawning replaces the old static "spawn 2 enemies per
            // room" pass — the WaveSpawner watches EnemyKilledEvent and spawns
            // the next wave when all enemies are cleared.
            CreateWaveSpawner(roomMiddle, roomLeft, roomRight);

            CreateLootSpawner();
            CreateVfxSpawner();
            AttachCameraFollow(camera, player.transform);
            CreateGameOverScreen();

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

        private static Camera CreateCamera()
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
            return cam;
        }

        private static void AttachCameraFollow(Camera camera, Transform target)
        {
            if (camera == null || target == null) return;
            var follow = camera.gameObject.AddComponent<CameraFollow>();
            SetPrivateField(follow, "target", target);
            SetPrivateField(follow, "offset", new Vector3(0f, 1f, -10f));
            SetPrivateField(follow, "damping", 6f);
            SetPrivateField(follow, "snapOnFirstFrame", true);
        }

        // Authors Assets/Prefabs/RoomTemplate_Standard.prefab — a real
        // RoomTemplate prefab with floor + ceiling + side walls (with door
        // gaps), DoorSocket components on each side, a player spawn anchor,
        // and three enemy spawn anchors. M2 next steps swap multiple
        // RoomTemplate variants into a BiomeConfig SO and let LevelGenerator
        // stitch them via the door sockets.
        // Charger variant — faster, lower HP, more aggressive aggro range.
        // Same GroundGruntAI behaviour for now (just data-driven differences);
        // a dedicated ChargerAI with leap/dash logic is later combat-content work.
        private static void EnsureChargerAssets()
        {
            try
            {
                EnsureFolder("Assets/ScriptableObjects");
                EnsureFolder("Assets/ScriptableObjects/Enemies");
                EnsureFolder("Assets/Prefabs");

                if (AssetDatabase.LoadMainAssetAtPath(ChargerDataPath) != null)
                    AssetDatabase.DeleteAsset(ChargerDataPath);
                if (AssetDatabase.LoadMainAssetAtPath(ChargerPrefabPath) != null)
                    AssetDatabase.DeleteAsset(ChargerPrefabPath);

                var dataInstance = ScriptableObject.CreateInstance<EnemyData>();
                AssetDatabase.CreateAsset(dataInstance, ChargerDataPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                var data = AssetDatabase.LoadAssetAtPath<EnemyData>(ChargerDataPath);
                if (data == null)
                {
                    Debug.LogError($"[SmokeTestSetup] LoadAssetAtPath returned null after CreateAsset for {ChargerDataPath}");
                    return;
                }

                SetPrivateField(data, "Id", "enemy_charger");
                SetPrivateField(data, "DisplayName", "Charger");
                SetPrivateField(data, "Archetype", EnemyArchetype.Charger);
                SetPrivateField(data, "MaxHealth", 12);
                SetPrivateField(data, "MoveSpeed", 4.5f);
                SetPrivateField(data, "AggroRange", 12f);
                SetPrivateField(data, "AttackRange", 1.0f);
                SetPrivateField(data, "AttackCooldown", 0.6f);
                SetPrivateField(data, "ContactDamage", 1);
                SetPrivateField(data, "ScoreOnKill", 20);
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssets();

                var go = new GameObject("Enemy_Charger");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 1;
                var sprite = LoadKenneySprite("character_charger.png");
                if (sprite != null)
                {
                    sr.sprite = sprite;
                    sr.flipX = true;
                }
                else
                {
                    var ps = go.AddComponent<ProceduralSquare>();
                    SetSerializedColor(ps, new Color(0.95f, 0.45f, 0.15f));
                }

                var rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = 3.5f;
                rb.freezeRotation = true;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                var col = go.AddComponent<BoxCollider2D>();
                col.size = new Vector2(1f, 1f);

                var ai = go.AddComponent<GroundGruntAI>();
                SetPrivateField(ai, "data", data);
                SetPrivateField(ai, "flashRenderer", sr);
                SetPrivateField(ai, "patrolDistance", 4f);
                SetPrivateField(ai, "groundMask", (LayerMask)1);

                var edgeCheck = new GameObject("EdgeCheck");
                edgeCheck.transform.SetParent(go.transform, worldPositionStays: false);
                edgeCheck.transform.localPosition = new Vector3(0.6f, -0.6f, 0);
                SetPrivateField(ai, "edgeCheck", edgeCheck.transform);

                var prefab = PrefabUtility.SaveAsPrefabAsset(go, ChargerPrefabPath);
                Object.DestroyImmediate(go);

                var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ChargerPrefabPath);
                SetPrivateField(data, "Prefab", prefabAsset);
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssets();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SmokeTestSetup] EnsureChargerAssets threw: {e}");
            }
        }

        private static void CreateWaveSpawner(GameObject roomMiddle, GameObject roomLeft, GameObject roomRight)
        {
            var go = new GameObject("WaveSpawner");
            var spawner = go.AddComponent<WaveSpawner>();

            var grunt = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            var charger = AssetDatabase.LoadAssetAtPath<GameObject>(ChargerPrefabPath);
            if (grunt != null) SetPrivateField(spawner, "gruntPrefab", grunt);
            if (charger != null) SetPrivateField(spawner, "chargerPrefab", charger);

            var anchors = new List<Transform>();
            CollectAnchors(roomMiddle, anchors);
            CollectAnchors(roomLeft, anchors);
            CollectAnchors(roomRight, anchors);
            SetPrivateField(spawner, "spawnAnchors", anchors);

            var waves = new List<WaveSpawner.WaveConfig>
            {
                new WaveSpawner.WaveConfig { gruntCount = 3, chargerCount = 0 },
                new WaveSpawner.WaveConfig { gruntCount = 4, chargerCount = 1 },
                new WaveSpawner.WaveConfig { gruntCount = 5, chargerCount = 2 },
                new WaveSpawner.WaveConfig { gruntCount = 6, chargerCount = 3 },
                new WaveSpawner.WaveConfig { gruntCount = 7, chargerCount = 4 },
            };
            SetPrivateField(spawner, "waves", waves);
            SetPrivateField(spawner, "startDelay", 0.6f);
            SetPrivateField(spawner, "delayBetweenWaves", 1.5f);
        }

        private static void CollectAnchors(GameObject roomGo, List<Transform> dest)
        {
            if (roomGo == null) return;
            if (!roomGo.TryGetComponent<RoomTemplate>(out var rt)) return;
            foreach (var a in rt.EnemySpawns)
                if (a != null) dest.Add(a);
        }

        private static void EnsureRoomTemplatePrefab()
        {
            try
            {
                EnsureFolder("Assets/Prefabs");
                if (AssetDatabase.LoadMainAssetAtPath(RoomPrefabPath) != null)
                    AssetDatabase.DeleteAsset(RoomPrefabPath);

                const float halfWidth = 15f;
                const float halfHeight = 7f;
                const float doorHeight = 3f; // door gap height in world units

                // Floor and walls share the same tile to guarantee no decoration
                // tiles end up on the geometry. Easy to point walls at a different
                // sprite later when we have a confirmed solid-block tile that
                // doesn't render as hearts/stars.
                var floorSprite = LoadKenneySprite("tile_floor.png");
                var wallSprite  = floorSprite;

                var room = new GameObject("RoomTemplate_Standard");
                var rt = room.AddComponent<RoomTemplate>();
                SetPrivateField(rt, "templateId", "room_standard");
                SetPrivateField(rt, "biomeTag", "biome_starter");
                SetPrivateField(rt, "size", new Vector2(halfWidth * 2f, halfHeight * 2f));

                // Top + bottom slabs span the full width. Floor top edge is at
                // y = -halfHeight + 0.5; the door gap starts there.
                BuildSlab(room.transform, "Floor",   new Vector3(0f, -halfHeight, 0f), new Vector2(halfWidth * 2f, 1f), new Color(0.42f, 0.70f, 0.34f), floorSprite);
                BuildSlab(room.transform, "Ceiling", new Vector3(0f,  halfHeight, 0f), new Vector2(halfWidth * 2f, 1f), new Color(0.25f, 0.28f, 0.32f), wallSprite);

                // Side walls cover from doorTop to the ceiling. Door starts at
                // floor top so the player walks straight through at ground level.
                var floorTop = -halfHeight + 0.5f;
                var doorTop = floorTop + doorHeight;          // top of the door gap
                var ceilingBottom = halfHeight - 0.5f;
                var wallHeight = ceilingBottom - doorTop;
                var wallY = (doorTop + ceilingBottom) * 0.5f;
                BuildSlab(room.transform, "WallLeft",  new Vector3(-halfWidth, wallY, 0f), new Vector2(1f, wallHeight), new Color(0.25f, 0.28f, 0.32f), wallSprite);
                BuildSlab(room.transform, "WallRight", new Vector3( halfWidth, wallY, 0f), new Vector2(1f, wallHeight), new Color(0.25f, 0.28f, 0.32f), wallSprite);

                // Door sockets sit at the door midpoint — metadata for
                // LevelGenerator's later stitching pass.
                var doorMidY = (floorTop + doorTop) * 0.5f;
                var doors = new List<DoorSocket>
                {
                    AddDoorSocket(room.transform, "DoorEast", new Vector3( halfWidth, doorMidY, 0f), DoorDirection.East),
                    AddDoorSocket(room.transform, "DoorWest", new Vector3(-halfWidth, doorMidY, 0f), DoorDirection.West),
                };
                SetPrivateField(rt, "doors", doors);

                // Player spawn anchor.
                var playerSpawn = new GameObject("PlayerSpawn");
                playerSpawn.transform.SetParent(room.transform, worldPositionStays: false);
                playerSpawn.transform.localPosition = new Vector3(0f, -3f, 0f);
                SetPrivateField(rt, "playerSpawn", playerSpawn.transform);

                // Three enemy spawn anchors spaced along the floor.
                var enemySpawns = new List<Transform>();
                var positions = new[]
                {
                    new Vector3(-7f, -5f, 0f),
                    new Vector3( 0f, -5f, 0f),
                    new Vector3( 7f, -5f, 0f),
                };
                for (var i = 0; i < positions.Length; i++)
                {
                    var es = new GameObject($"EnemySpawn_{i}");
                    es.transform.SetParent(room.transform, worldPositionStays: false);
                    es.transform.localPosition = positions[i];
                    enemySpawns.Add(es.transform);
                }
                SetPrivateField(rt, "enemySpawns", enemySpawns);

                PrefabUtility.SaveAsPrefabAsset(room, RoomPrefabPath);
                Object.DestroyImmediate(room);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SmokeTestSetup] EnsureRoomTemplatePrefab threw: {e}");
            }
        }

        private static DoorSocket AddDoorSocket(Transform parent, string name, Vector3 localPos, DoorDirection direction)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = localPos;
            var ds = go.AddComponent<DoorSocket>();
            SetPrivateField(ds, "direction", direction);
            return ds;
        }

        private static GameObject InstantiateRoom(Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[SmokeTestSetup] RoomTemplate prefab missing at {RoomPrefabPath}");
                return null;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = position;
            return go;
        }

        private static void PositionPlayerAtRoomSpawn(GameObject player, GameObject roomGo)
        {
            if (player == null || roomGo == null) return;
            if (!roomGo.TryGetComponent<RoomTemplate>(out var rt) || rt.PlayerSpawn == null) return;
            player.transform.position = rt.PlayerSpawn.position;
        }

        private static void SpawnEnemiesAtRoomAnchors(GameObject player, GameObject roomGo, int maxPerRoom = int.MaxValue)
        {
            if (roomGo == null || !roomGo.TryGetComponent<RoomTemplate>(out var rt)) return;
            var spawned = 0;
            foreach (var anchor in rt.EnemySpawns)
            {
                if (anchor == null) continue;
                if (spawned >= maxPerRoom) break;
                SpawnEnemy(player, anchor.position);
                spawned++;
            }
        }

        private static void BuildSlab(Transform parent, string name, Vector3 position, Vector2 size, Color fallbackColor, Sprite tileSprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.position = position;
            go.transform.localScale = Vector3.one;

            var sr = go.AddComponent<SpriteRenderer>();
            BoxCollider2D col;
            if (tileSprite != null)
            {
                // Tiled draw mode repeats the sprite across the GameObject's size,
                // so we get e.g. 30 tiles of grass in a 30-wide floor without
                // stretching one sprite.
                sr.sprite = tileSprite;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.size = size;
                col = go.AddComponent<BoxCollider2D>();
                col.size = size;
            }
            else
            {
                go.transform.localScale = new Vector3(size.x, size.y, 1f);
                var ps = go.AddComponent<ProceduralSquare>();
                SetSerializedColor(ps, fallbackColor);
                col = go.AddComponent<BoxCollider2D>();
                col.size = new Vector2(1f, 1f);
            }
        }

        private static GameObject CreatePlayer()
        {
            // Load pistol fresh at the moment of use — sidesteps the post-CreateAsset
            // fake-null window that broke earlier attempts to pass the reference around.
            var pistol = AssetDatabase.LoadAssetAtPath<WeaponData>(PistolAssetPath);

            var go = new GameObject("Player");
            go.transform.position = new Vector3(0, 0, 0);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 1;
            var playerSprite = LoadKenneySprite("character_player.png");
            if (playerSprite != null)
            {
                sr.sprite = playerSprite;
                // Kenney pixel-platformer characters face LEFT in their source PNG.
                // Pre-flipping so the default forward (idle) is right; PlayerController
                // multiplies localScale.x by facing, which then mirrors correctly on
                // left-movement.
                sr.flipX = true;
            }
            else
            {
                var ps = go.AddComponent<ProceduralSquare>();
                SetSerializedColor(ps, new Color(0.95f, 0.62f, 0.20f));
            }

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3.5f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f);

            var playerHealth = go.AddComponent<PlayerHealth>();
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

            // SerializedObject array operations on freshly-AddComponent'd components are
            // unreliable for List<T> fields (the muzzle Transform persists, but the List<>
            // resize silently doesn't). Reflection writes the C# field directly, then
            // EditorUtility.SetDirty marks the component for save.
            SetPrivateField(ctrl, "groundCheck", groundCheck.transform);
            SetPrivateField(ctrl, "extraJumps", 1); // single jump + 1 double-jump
            SetPrivateField(weaponHolder, "muzzle", muzzle.transform);
            if (pistol != null)
                SetPrivateField(weaponHolder, "startingLoadout", new List<WeaponData> { pistol });
            else
                Debug.LogError("[SmokeTestSetup] Pistol is null when wiring Player — bullets will not fire.");
            EditorUtility.SetDirty(ctrl);
            EditorUtility.SetDirty(weaponHolder);

            CreateHealthBar(playerHealth);

            return go;
        }

        private static void CreateHealthBar(PlayerHealth target)
        {
            var go = new GameObject("HealthBar");
            go.transform.position = target.transform.position + new Vector3(0f, 0.7f, 0f);
            go.transform.localScale = new Vector3(1f, 0.15f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;
            var ps = go.AddComponent<ProceduralSquare>();
            SetSerializedColor(ps, new Color(0.20f, 0.85f, 0.30f));

            var bar = go.AddComponent<HealthBar>();
            SetPrivateField(bar, "target", target);
            SetPrivateField(bar, "fullWidth", 1f);
            SetPrivateField(bar, "thickness", 0.15f);
        }

        private static void ImportKenneyArt()
        {
            try
            {
                if (!Directory.Exists(KenneySource))
                {
                    // First-clone or different machine — Kenney files already in repo.
                    return;
                }
                EnsureFolder("Assets/Art");
                EnsureFolder(KenneyDest);

                var anyCopied = false;
                foreach (var (src, dst, _) in KenneyFiles)
                {
                    var srcPath = Path.Combine(KenneySource, src);
                    var dstPath = $"{KenneyDest}/{dst}";
                    if (!File.Exists(srcPath))
                    {
                        Debug.LogWarning($"[SmokeTestSetup] Kenney source missing: {srcPath}");
                        continue;
                    }
                    if (File.Exists(dstPath)) continue;
                    File.Copy(srcPath, dstPath, overwrite: false);
                    anyCopied = true;
                }
                if (anyCopied) AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                foreach (var (_, dst, ppu) in KenneyFiles)
                {
                    var dstPath = $"{KenneyDest}/{dst}";
                    var importer = AssetImporter.GetAtPath(dstPath) as TextureImporter;
                    if (importer == null) continue;

                    // Need to read+write through TextureImporterSettings to set
                    // spriteMeshType. SpriteMeshType.Tight (default) clips
                    // transparent pixels off the mesh, which breaks
                    // SpriteDrawMode.Tiled — Unity logs the
                    // 'Sprite Tiling might not appear correctly' warning. FullRect
                    // forces a quad mesh that tiles cleanly.
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);

                    var alreadyConfigured =
                        importer.textureType == TextureImporterType.Sprite &&
                        Mathf.Approximately(importer.spritePixelsPerUnit, ppu) &&
                        importer.filterMode == FilterMode.Point &&
                        settings.spriteMeshType == SpriteMeshType.FullRect;
                    if (alreadyConfigured) continue;

                    settings.textureType = TextureImporterType.Sprite;
                    settings.spriteMode = (int)SpriteImportMode.Single;
                    settings.spritePixelsPerUnit = ppu;
                    settings.filterMode = FilterMode.Point;
                    settings.mipmapEnabled = false;
                    settings.wrapMode = TextureWrapMode.Clamp;
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    importer.SetTextureSettings(settings);
                    importer.SaveAndReimport();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SmokeTestSetup] ImportKenneyArt threw: {e.Message}");
            }
        }

        private static Sprite LoadKenneySprite(string fileName)
            => AssetDatabase.LoadAssetAtPath<Sprite>($"{KenneyDest}/{fileName}");

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            // Walk the inheritance chain so protected fields on a base class
            // (e.g. EnemyBase.data when target is GroundGruntAI) resolve.
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
                type = type.BaseType;
            }
            Debug.LogWarning($"[SmokeTestSetup] Field '{fieldName}' not found on {target.GetType().Name} or any base type");
        }

        private static GameObject EnsureBulletPrefab()
        {
            EnsureFolder("Assets/Prefabs");
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(BulletPrefabPath);
            if (existing != null) return existing;

            var go = new GameObject("Bullet");
            go.transform.localScale = new Vector3(0.25f, 0.25f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            var bulletSprite = LoadKenneySprite("tile_bullet.png");
            if (bulletSprite != null)
            {
                sr.sprite = bulletSprite;
                sr.color = new Color(1f, 0.95f, 0.45f); // warm yellow tint over whatever the source tile is
            }
            else
            {
                var ps = go.AddComponent<ProceduralSquare>();
                SetSerializedColor(ps, new Color(1f, 0.85f, 0.30f));
            }

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

        private static void EnsurePistolAsset()
        {
            try
            {
                EnsureFolder("Assets/ScriptableObjects");
                EnsureFolder("Assets/ScriptableObjects/Weapons");

                if (AssetDatabase.LoadMainAssetAtPath(PistolAssetPath) != null)
                    AssetDatabase.DeleteAsset(PistolAssetPath);

                var instance = ScriptableObject.CreateInstance<WeaponData>();
                AssetDatabase.CreateAsset(instance, PistolAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Reload through the AssetDatabase to get a stable reference
                // past Unity's post-CreateAsset fake-null window.
                var pistol = AssetDatabase.LoadAssetAtPath<WeaponData>(PistolAssetPath);
                if (pistol == null)
                {
                    Debug.LogError($"[SmokeTestSetup] LoadAssetAtPath returned null after CreateAsset for {PistolAssetPath}");
                    return;
                }

                var bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BulletPrefabPath);
                if (bulletPrefab == null)
                {
                    Debug.LogError($"[SmokeTestSetup] Bullet prefab missing at {BulletPrefabPath}");
                    return;
                }

                SetPrivateField(pistol, "Id", "weapon_pistol");
                SetPrivateField(pistol, "DisplayName", "Pistol");
                SetPrivateField(pistol, "Mode", FireMode.SemiAuto);
                SetPrivateField(pistol, "Damage", 10);
                SetPrivateField(pistol, "FireRate", 4f);
                SetPrivateField(pistol, "ProjectilesPerShot", 1);
                SetPrivateField(pistol, "SpreadDegrees", 1f);
                SetPrivateField(pistol, "ProjectileSpeed", 18f);
                SetPrivateField(pistol, "ProjectileLifetime", 1.5f);
                SetPrivateField(pistol, "Knockback", 1f);
                SetPrivateField(pistol, "Recoil", 0f);
                SetPrivateField(pistol, "Infinite", true);
                SetPrivateField(pistol, "MagazineSize", 12);
                SetPrivateField(pistol, "ReloadSeconds", 1.0f);
                SetPrivateField(pistol, "BurstCount", 1);
                SetPrivateField(pistol, "BurstInterval", 0f);
                SetPrivateField(pistol, "ProjectilePrefab", bulletPrefab);

                var muzzleFlashPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MuzzleFlashPath);
                if (muzzleFlashPrefab != null)
                    SetPrivateField(pistol, "MuzzleFlashPrefab", muzzleFlashPrefab);

                EditorUtility.SetDirty(pistol);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SmokeTestSetup] EnsurePistolAsset threw: {e}");
            }
        }

        private static void EnsureEnemyAssets()
        {
            try
            {
                EnsureFolder("Assets/ScriptableObjects");
                EnsureFolder("Assets/ScriptableObjects/Enemies");
                EnsureFolder("Assets/Prefabs");

                if (AssetDatabase.LoadMainAssetAtPath(EnemyDataPath) != null)
                    AssetDatabase.DeleteAsset(EnemyDataPath);
                if (AssetDatabase.LoadMainAssetAtPath(EnemyPrefabPath) != null)
                    AssetDatabase.DeleteAsset(EnemyPrefabPath);

                var dataInstance = ScriptableObject.CreateInstance<EnemyData>();
                AssetDatabase.CreateAsset(dataInstance, EnemyDataPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                var enemyData = AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyDataPath);
                if (enemyData == null)
                {
                    Debug.LogError($"[SmokeTestSetup] LoadAssetAtPath returned null after CreateAsset for {EnemyDataPath}");
                    return;
                }

                SetPrivateField(enemyData, "Id", "enemy_grunt");
                SetPrivateField(enemyData, "DisplayName", "Grunt");
                SetPrivateField(enemyData, "Archetype", EnemyArchetype.Grunt);
                SetPrivateField(enemyData, "MaxHealth", 20);
                SetPrivateField(enemyData, "MoveSpeed", 2.5f);
                SetPrivateField(enemyData, "AggroRange", 8f);
                SetPrivateField(enemyData, "AttackRange", 1.2f);
                SetPrivateField(enemyData, "AttackCooldown", 1.0f);
                SetPrivateField(enemyData, "ContactDamage", 1);
                SetPrivateField(enemyData, "ScoreOnKill", 10);
                EditorUtility.SetDirty(enemyData);
                AssetDatabase.SaveAssets();

                // Build prefab
                var go = new GameObject("Enemy_Grunt");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 1;
                var enemySprite = LoadKenneySprite("character_enemy.png");
                if (enemySprite != null)
                {
                    sr.sprite = enemySprite;
                    sr.flipX = true; // same default-faces-left correction as the player
                }
                else
                {
                    var ps = go.AddComponent<ProceduralSquare>();
                    SetSerializedColor(ps, new Color(0.9f, 0.25f, 0.25f));
                }

                var rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = 3.5f;
                rb.freezeRotation = true;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                var col = go.AddComponent<BoxCollider2D>();
                col.size = new Vector2(1f, 1f);

                var ai = go.AddComponent<GroundGruntAI>();
                SetPrivateField(ai, "data", enemyData);
                SetPrivateField(ai, "flashRenderer", sr);
                SetPrivateField(ai, "patrolDistance", 3f);
                SetPrivateField(ai, "groundMask", (LayerMask)1); // Default layer

                var edgeCheck = new GameObject("EdgeCheck");
                edgeCheck.transform.SetParent(go.transform, worldPositionStays: false);
                edgeCheck.transform.localPosition = new Vector3(0.6f, -0.6f, 0);
                SetPrivateField(ai, "edgeCheck", edgeCheck.transform);

                var prefab = PrefabUtility.SaveAsPrefabAsset(go, EnemyPrefabPath);
                Object.DestroyImmediate(go);

                // Wire prefab back into the EnemyData (closes the loop)
                var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
                SetPrivateField(enemyData, "Prefab", prefabAsset);
                EditorUtility.SetDirty(enemyData);
                AssetDatabase.SaveAssets();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SmokeTestSetup] EnsureEnemyAssets threw: {e}");
            }
        }

        private static void SpawnEnemy(GameObject player, Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[SmokeTestSetup] Enemy prefab missing at {EnemyPrefabPath}");
                return;
            }
            var enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            enemy.transform.position = position;
            if (enemy.TryGetComponent<EnemyBase>(out var eb))
                eb.SetTarget(player.transform);
        }

        private static void CreateGameOverScreen()
        {
            var go = new GameObject("GameOverScreen");
            go.AddComponent<GameOverScreen>();
        }

        private static void EnsureDeathBurstPrefab()
        {
            try
            {
                EnsureFolder("Assets/Prefabs");
                if (AssetDatabase.LoadMainAssetAtPath(DeathBurstPath) != null)
                    AssetDatabase.DeleteAsset(DeathBurstPath);

                var go = new GameObject("Vfx_DeathBurst");
                var burst = go.AddComponent<SimpleParticleBurst>();
                SetPrivateField(burst, "count", 18);
                SetPrivateField(burst, "speed", 5.5f);
                SetPrivateField(burst, "speedJitter", 1.5f);
                SetPrivateField(burst, "lifetime", 0.7f);
                SetPrivateField(burst, "size", 0.18f);
                SetPrivateField(burst, "color", new Color(1f, 0.35f, 0.35f, 1f));
                SetPrivateField(burst, "gravityScale", 1.4f);
                SetPrivateField(burst, "coneAngleDegrees", 360f);
                SetPrivateField(burst, "sortingOrder", 10);

                PrefabUtility.SaveAsPrefabAsset(go, DeathBurstPath);
                Object.DestroyImmediate(go);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SmokeTestSetup] EnsureDeathBurstPrefab threw: {e}");
            }
        }

        private static void EnsureMuzzleFlashPrefab()
        {
            try
            {
                EnsureFolder("Assets/Prefabs");
                if (AssetDatabase.LoadMainAssetAtPath(MuzzleFlashPath) != null)
                    AssetDatabase.DeleteAsset(MuzzleFlashPath);

                var go = new GameObject("Vfx_MuzzleFlash");
                var burst = go.AddComponent<SimpleParticleBurst>();
                SetPrivateField(burst, "count", 8);
                SetPrivateField(burst, "speed", 4f);
                SetPrivateField(burst, "speedJitter", 1f);
                SetPrivateField(burst, "lifetime", 0.25f);
                SetPrivateField(burst, "size", 0.15f);
                SetPrivateField(burst, "color", new Color(1f, 0.92f, 0.40f, 1f));
                SetPrivateField(burst, "gravityScale", 0f);
                SetPrivateField(burst, "coneAngleDegrees", 35f);
                SetPrivateField(burst, "baseDirection", Vector2.up);
                SetPrivateField(burst, "sortingOrder", 10);

                PrefabUtility.SaveAsPrefabAsset(go, MuzzleFlashPath);
                Object.DestroyImmediate(go);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SmokeTestSetup] EnsureMuzzleFlashPrefab threw: {e}");
            }
        }

        private static void CreateVfxSpawner()
        {
            var go = new GameObject("VfxSpawner");
            var spawner = go.AddComponent<VfxSpawner>();
            var burst = AssetDatabase.LoadAssetAtPath<GameObject>(DeathBurstPath);
            if (burst != null) SetPrivateField(spawner, "deathBurstPrefab", burst);
        }

        private static void EnsureHealthPickupPrefab()
        {
            try
            {
                EnsureFolder("Assets/Prefabs");
                if (AssetDatabase.LoadMainAssetAtPath(HealthPickupPath) != null)
                    AssetDatabase.DeleteAsset(HealthPickupPath);

                var go = new GameObject("HealthPickup");
                go.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 2;
                var heart = LoadKenneySprite("tile_heart.png");
                if (heart != null)
                {
                    sr.sprite = heart;
                }
                else
                {
                    var ps = go.AddComponent<ProceduralSquare>();
                    SetSerializedColor(ps, new Color(1f, 0.25f, 0.45f));
                }

                var rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = 2.5f;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                rb.freezeRotation = true;

                // Two-collider setup: a small solid collider catches floor/wall
                // physics so the pickup actually lands instead of falling
                // through, and a slightly larger trigger collider is what fires
                // OnTriggerEnter2D in HealthPickup when the player touches it.
                var solid = go.AddComponent<CircleCollider2D>();
                solid.isTrigger = false;
                solid.radius = 0.35f;

                var pickupTrigger = go.AddComponent<CircleCollider2D>();
                pickupTrigger.isTrigger = true;
                pickupTrigger.radius = 0.6f;

                go.AddComponent<HealthPickup>();

                PrefabUtility.SaveAsPrefabAsset(go, HealthPickupPath);
                Object.DestroyImmediate(go);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SmokeTestSetup] EnsureHealthPickupPrefab threw: {e}");
            }
        }

        private static void CreateBackground()
        {
            var go = new GameObject("Background");
            go.transform.position = new Vector3(0f, 0f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = -50;
            var bg = LoadKenneySprite("tile_bg.png");
            if (bg != null)
            {
                sr.sprite = bg;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                // 3 rooms × 30 wide + 30 of margin each side = 150 wide; 30 tall
                sr.size = new Vector2(150f, 30f);
            }
            else
            {
                go.transform.localScale = new Vector3(150f, 30f, 1f);
                var ps = go.AddComponent<ProceduralSquare>();
                SetSerializedColor(ps, new Color(0.10f, 0.16f, 0.24f));
            }
        }

        private static void CreateLootSpawner()
        {
            var go = new GameObject("LootSpawner");
            var spawner = go.AddComponent<LootSpawner>();
            var pickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HealthPickupPath);
            if (pickupPrefab != null)
                SetPrivateField(spawner, "healthPickupPrefab", pickupPrefab);
            SetPrivateField(spawner, "healthDropChance", 0.5f);
            SetPrivateField(spawner, "popupVelocity", new Vector2(2.5f, 5f));
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
