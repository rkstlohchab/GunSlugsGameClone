using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GunSlugsClone.Core;
using GunSlugsClone.Enemies;
using GunSlugsClone.Enemies.AI;
using GunSlugsClone.Player;
using GunSlugsClone.Procedural;
using GunSlugsClone.UI;
using GunSlugsClone.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        private const string PistolAssetPath   = "Assets/ScriptableObjects/Weapons/weapon_pistol.asset";
        private const string SmgAssetPath      = "Assets/ScriptableObjects/Weapons/weapon_smg.asset";
        private const string ShotgunAssetPath  = "Assets/ScriptableObjects/Weapons/weapon_shotgun.asset";
        private const string RocketAssetPath   = "Assets/ScriptableObjects/Weapons/weapon_rocket.asset";
        private const string SniperAssetPath   = "Assets/ScriptableObjects/Weapons/weapon_sniper.asset";
        private const string KnifeAssetPath    = "Assets/ScriptableObjects/Weapons/weapon_knife.asset";
        private const string EnemyDataPath   = "Assets/ScriptableObjects/Enemies/enemy_grunt.asset";
        private const string EnemyPrefabPath  = "Assets/Prefabs/Enemy_Grunt.prefab";
        private const string ChargerDataPath  = "Assets/ScriptableObjects/Enemies/enemy_charger.asset";
        private const string ChargerPrefabPath = "Assets/Prefabs/Enemy_Charger.prefab";
        private const string FlyerDataPath    = "Assets/ScriptableObjects/Enemies/enemy_flyer.asset";
        private const string FlyerPrefabPath  = "Assets/Prefabs/Enemy_Flyer.prefab";
        private const string BossDataPath     = "Assets/ScriptableObjects/Enemies/enemy_boss.asset";
        private const string BossPrefabPath   = "Assets/Prefabs/Enemy_Boss.prefab";
        private const string RoomPrefabPath        = "Assets/Prefabs/RoomTemplate_Standard.prefab";
        private const string RoomHallwayPath       = "Assets/Prefabs/RoomTemplate_Hallway.prefab";
        private const string RoomBossArenaPath     = "Assets/Prefabs/RoomTemplate_BossArena.prefab";
        private const string BiomeConfigPath       = "Assets/ScriptableObjects/Biomes/biome_starter.asset";
        private const string HealthPickupPath      = "Assets/Prefabs/HealthPickup.prefab";
        private const string HostagePrefabPath     = "Assets/Prefabs/Hostage.prefab";
        private const string DeathBurstPath        = "Assets/Prefabs/Vfx_DeathBurst.prefab";
        private const string RescueBurstPath       = "Assets/Prefabs/Vfx_RescueBurst.prefab";
        private const string MuzzleFlashPath       = "Assets/Prefabs/Vfx_MuzzleFlash.prefab";

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
            ("Tiles/Characters/tile_0026.png", "character_boss.png",    24),
            ("Tiles/Characters/tile_0003.png", "character_hostage.png", 24),
            ("Tiles/Characters/tile_0015.png", "character_flyer.png",   24),
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
            EnsureSmgAsset();
            EnsureShotgunAsset();
            EnsureRocketAsset();
            EnsureSniperAsset();
            EnsureKnifeAsset();
            EnsureEnemyAssets();
            EnsureChargerAssets();
            EnsureFlyerAssets();
            EnsureBossAssets();
            EnsureRoomTemplatePrefab();          // standard variant
            EnsureRoomHallwayPrefab();
            EnsureRoomBossArenaPrefab();
            EnsureBiomeConfig();
            EnsureHostagePrefab();
            EnsureHealthPickupPrefab();
            EnsureDeathBurstPrefab();
            EnsureRescueBurstPrefab();

            var scene = OpenOrCreateScene();
            ClearScene(scene);

            var camera = CreateCamera();

            // Build the stage FIRST so we know the world-space bounds, then
            // size+position the background to cover them. GunSlugs-style
            // single horizontal stage rather than the dungeon-rooms model.
            var rooms = BuildStage();
            CreateBackgroundForLevel(rooms);
            var startRoom = rooms.Count > 0 ? rooms[0] : null;

            var player = CreatePlayer();
            if (startRoom != null) PositionPlayerAtRoomSpawn(player, startRoom);
            WirePlayerInput(player);

            // Spawn hostages in non-boss rooms and tally the total for HUD.
            var hostagesTotal = 0;
            foreach (var roomGo in rooms)
                hostagesTotal += SpawnHostagesAtRoomAnchors(roomGo);

            CreateWaveSpawnerFromRooms(rooms, player.transform);

            CreateLootSpawner();
            CreateVfxSpawner();
            AttachCameraFollow(camera, player.transform);
            CreateGameOverScreen(hostagesTotal, playerMaxHp: 100);
            CreateTouchControlsCanvas();

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
        // Uses dedicated ChargerAI: approach → telegraph (yellow flash) → dash
        // burst → recovery cooldown so it reads as a deliberate threat instead
        // of a faster grunt.
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
                SetPrivateField(data, "MaxHealth", 20);
                SetPrivateField(data, "MoveSpeed", 4.5f);
                SetPrivateField(data, "AggroRange", 12f);
                SetPrivateField(data, "AttackRange", 1.0f);
                SetPrivateField(data, "AttackCooldown", 0.6f);
                SetPrivateField(data, "ContactDamage", 10);
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

                var ai = go.AddComponent<ChargerAI>();
                SetPrivateField(ai, "data", data);
                SetPrivateField(ai, "flashRenderer", sr);

                AddEnemyHealthBarTo(go, ai);

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

        // GunSlugs-style stage: ONE long horizontal floor, solid walls only at
        // the far-left start barrier and the far-right boss gate, no ceiling
        // (open sky), houses scattered along as cosmetic structures, with
        // enemy + hostage spawn anchors distributed across the floor. Returns
        // the stage as a single-element list because the rest of the smoke
        // test was built around a 'list of rooms' contract — keeping that for
        // now so WaveSpawner anchor collection and HUD wiring don't change.
        private static List<GameObject> BuildStage()
        {
            const float stageWidth = 220f;
            const float floorY     = -7f;     // floor centre Y
            const float floorTop   = -6.5f;   // floor top edge (y = floorY + 0.5)
            const float wallHeight = 14f;
            var rooms = new List<GameObject>();

            var stage = new GameObject("Stage");
            var rt = stage.AddComponent<RoomTemplate>();
            SetPrivateField(rt, "templateId", "stage_starter");
            SetPrivateField(rt, "biomeTag", "biome_starter");
            SetPrivateField(rt, "size", new Vector2(stageWidth, wallHeight));

            var floorSprite = LoadKenneySprite("tile_floor.png");

            // Continuous floor across the whole stage.
            BuildSlab(stage.transform, "Floor",
                new Vector3(stageWidth * 0.5f, floorY, 0f),
                new Vector2(stageWidth, 1f),
                new Color(0.42f, 0.70f, 0.34f),
                floorSprite);

            // Stage barriers: solid walls only at the very start and very end.
            BuildSlab(stage.transform, "WallLeft",
                new Vector3(0f, floorY + wallHeight * 0.5f + 0.5f, 0f),
                new Vector2(1f, wallHeight),
                new Color(0.25f, 0.28f, 0.32f),
                floorSprite);
            BuildSlab(stage.transform, "WallRight",
                new Vector3(stageWidth, floorY + wallHeight * 0.5f + 0.5f, 0f),
                new Vector2(1f, wallHeight),
                new Color(0.25f, 0.28f, 0.32f),
                floorSprite);

            // Decorative houses along the stage. Cosmetic only for the smoke
            // test; M2/M3 makes them enterable interiors. Each house is a small
            // box (no collider) sitting on the floor with a darker tint so it
            // reads as a building behind the action.
            BuildHouse(stage.transform, new Vector2(35f,  floorTop), new Vector2(8f, 7f), new Color(0.55f, 0.32f, 0.20f));
            BuildHouse(stage.transform, new Vector2(95f,  floorTop), new Vector2(10f, 8f), new Color(0.42f, 0.40f, 0.45f));
            BuildHouse(stage.transform, new Vector2(160f, floorTop), new Vector2(8f, 7f), new Color(0.55f, 0.32f, 0.20f));

            // Player spawn near the left edge.
            var playerSpawn = new GameObject("PlayerSpawn");
            playerSpawn.transform.SetParent(stage.transform, worldPositionStays: false);
            playerSpawn.transform.position = new Vector3(5f, floorY + 1f, 0f);
            SetPrivateField(rt, "playerSpawn", playerSpawn.transform);

            // 8 enemy spawn anchors along the stage at floor level.
            var enemySpawns = new List<Transform>();
            for (var i = 0; i < 8; i++)
            {
                var anchor = new GameObject($"EnemySpawn_{i}");
                anchor.transform.SetParent(stage.transform, worldPositionStays: false);
                anchor.transform.position = new Vector3(20f + i * 24f, floorY + 1f, 0f);
                enemySpawns.Add(anchor.transform);
            }
            SetPrivateField(rt, "enemySpawns", enemySpawns);

            // Hostage anchors placed in front of the houses (one per house).
            var hostageSpawns = new List<Transform>();
            float[] housePositions = { 35f, 95f, 160f };
            for (var i = 0; i < housePositions.Length; i++)
            {
                var anchor = new GameObject($"HostageSpawn_{i}");
                anchor.transform.SetParent(stage.transform, worldPositionStays: false);
                anchor.transform.position = new Vector3(housePositions[i], floorY + 1f, 0f);
                hostageSpawns.Add(anchor.transform);
            }
            SetPrivateField(rt, "hostageSpawns", hostageSpawns);

            rooms.Add(stage);
            return rooms;
        }

        private static void BuildHouse(Transform parent, Vector2 anchorBottom, Vector2 size, Color tint)
        {
            var go = new GameObject("House");
            go.transform.SetParent(parent, worldPositionStays: false);
            // Anchor by bottom-centre so the house sits on the floor regardless
            // of size.
            go.transform.position = new Vector3(anchorBottom.x, anchorBottom.y + size.y * 0.5f, 0f);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = -10; // behind characters/enemies but in front of the bg
            var ps = go.AddComponent<ProceduralSquare>();
            SetSerializedColor(ps, tint);

            // 'Doorway' marker: a darker rectangle in the lower middle of the
            // house. Purely cosmetic for now — selling the 'house' read.
            var door = new GameObject("Doorway");
            door.transform.SetParent(go.transform, worldPositionStays: false);
            door.transform.localPosition = new Vector3(0f, -0.25f, 0f);
            door.transform.localScale = new Vector3(0.25f, 0.5f, 1f);
            var doorSr = door.AddComponent<SpriteRenderer>();
            doorSr.sortingOrder = -9;
            var doorPs = door.AddComponent<ProceduralSquare>();
            SetSerializedColor(doorPs, new Color(0.10f, 0.08f, 0.06f));
        }

        // Kept around for future dungeon biomes — full graph-based layout
        // doesn't fit the GunSlugs side-scroller stage but is the right shape
        // for a top-down maze biome. Currently unused.
        private static List<GameObject> BuildLevelFromBiome()
        {
            var rooms = new List<GameObject>();
            var standard   = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath);
            var hallway    = AssetDatabase.LoadAssetAtPath<GameObject>(RoomHallwayPath);
            var bossArena  = AssetDatabase.LoadAssetAtPath<GameObject>(RoomBossArenaPath);
            if (standard == null || hallway == null || bossArena == null)
            {
                Debug.LogWarning("[SmokeTestSetup] One or more room prefabs missing — falling back to single Standard room.");
                if (standard != null) rooms.Add((GameObject)PrefabUtility.InstantiatePrefab(standard));
                return rooms;
            }

            // Seeded variant sequence: always Standard start + BossArena finish,
            // 3 random middle rooms picked between Standard and Hallway.
            var rng = new DeterministicRng(seed: 42);
            var middlePool = new[] { standard, hallway };
            var sequence = new List<GameObject> { standard };
            for (var i = 0; i < 3; i++) sequence.Add(rng.Pick(middlePool));
            sequence.Add(bossArena);

            // Place rooms left-to-right with walls touching. All variants are
            // 14 tall, so all floors land at y = -7 with a uniform y=0 center.
            var prevRightX = float.NegativeInfinity;
            foreach (var prefab in sequence)
            {
                var rt = prefab.GetComponent<RoomTemplate>();
                var size = rt != null ? rt.Size : new Vector2(30f, 14f);
                var halfW = size.x * 0.5f;

                float cx = prevRightX <= -1e6f ? halfW : prevRightX + halfW;

                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.transform.position = new Vector3(cx, 0f, 0f);
                rooms.Add(go);

                prevRightX = cx + halfW;
            }
            return rooms;
        }

        private static int SpawnHostagesAtRoomAnchors(GameObject roomGo)
        {
            if (roomGo == null) return 0;
            if (!roomGo.TryGetComponent<RoomTemplate>(out var rt)) return 0;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HostagePrefabPath);
            if (prefab == null) return 0;
            var spawned = 0;
            foreach (var anchor in rt.HostageSpawns)
            {
                if (anchor == null) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.transform.position = anchor.position;
                spawned++;
            }
            return spawned;
        }

        private static void CreateWaveSpawnerFromRooms(List<GameObject> rooms, Transform playerTransform)
        {
            var go = new GameObject("WaveSpawner");
            var spawner = go.AddComponent<WaveSpawner>();

            var grunt   = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            var charger = AssetDatabase.LoadAssetAtPath<GameObject>(ChargerPrefabPath);
            var flyer   = AssetDatabase.LoadAssetAtPath<GameObject>(FlyerPrefabPath);
            var boss    = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
            if (grunt != null)   SetPrivateField(spawner, "gruntPrefab",   grunt);
            if (charger != null) SetPrivateField(spawner, "chargerPrefab", charger);
            if (flyer != null)   SetPrivateField(spawner, "flyerPrefab",   flyer);
            if (boss != null)    SetPrivateField(spawner, "bossPrefab",    boss);

            SetPrivateField(spawner, "playerTransform", playerTransform);
            SetPrivateField(spawner, "minSpawnDistance", 5f);

            var anchors = new List<Transform>();
            foreach (var roomGo in rooms) CollectAnchors(roomGo, anchors);
            SetPrivateField(spawner, "spawnAnchors", anchors);

            // Five escalating waves; finale = chargers + flyer escort + boss.
            var waves = new List<WaveSpawner.WaveConfig>
            {
                new WaveSpawner.WaveConfig { gruntCount = 3, chargerCount = 0, flyerCount = 0, bossCount = 0 },
                new WaveSpawner.WaveConfig { gruntCount = 4, chargerCount = 1, flyerCount = 1, bossCount = 0 },
                new WaveSpawner.WaveConfig { gruntCount = 5, chargerCount = 2, flyerCount = 2, bossCount = 0 },
                new WaveSpawner.WaveConfig { gruntCount = 6, chargerCount = 3, flyerCount = 2, bossCount = 0 },
                new WaveSpawner.WaveConfig { gruntCount = 0, chargerCount = 2, flyerCount = 3, bossCount = 1 },
            };
            SetPrivateField(spawner, "waves", waves);
            SetPrivateField(spawner, "startDelay", 0.6f);
            SetPrivateField(spawner, "delayBetweenWaves", 1.5f);
        }

        private static void EnsureRoomHallwayPrefab()    => AuthorRoomTemplate(RoomHallwayPath,   "room_hallway", new Vector2(20f, 14f), enemyAnchors: 2, hostageAnchors: 1);
        private static void EnsureRoomBossArenaPrefab() => AuthorRoomTemplate(RoomBossArenaPath, "room_boss",    new Vector2(40f, 14f), enemyAnchors: 4, hostageAnchors: 0);

        private static void AuthorRoomTemplate(string path, string id, Vector2 worldSize, int enemyAnchors, int hostageAnchors)
        {
            try
            {
                EnsureFolder("Assets/Prefabs");
                if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                    AssetDatabase.DeleteAsset(path);

                var halfWidth  = worldSize.x * 0.5f;
                var halfHeight = worldSize.y * 0.5f;
                const float doorHeight = 3f;

                var floorSprite = LoadKenneySprite("tile_floor.png");
                var wallSprite  = floorSprite; // walls share the floor tile so no decoration tiles end up on them

                var room = new GameObject($"RoomTemplate_{id}");
                var rt = room.AddComponent<RoomTemplate>();
                SetPrivateField(rt, "templateId", id);
                SetPrivateField(rt, "biomeTag", "biome_starter");
                SetPrivateField(rt, "size", worldSize);

                BuildSlab(room.transform, "Floor",   new Vector3(0f, -halfHeight, 0f), new Vector2(worldSize.x, 1f), new Color(0.42f, 0.70f, 0.34f), floorSprite);
                BuildSlab(room.transform, "Ceiling", new Vector3(0f,  halfHeight, 0f), new Vector2(worldSize.x, 1f), new Color(0.25f, 0.28f, 0.32f), wallSprite);

                var floorTop = -halfHeight + 0.5f;
                var doorTop = floorTop + doorHeight;
                var ceilingBottom = halfHeight - 0.5f;
                var wallHeight = ceilingBottom - doorTop;
                var wallY = (doorTop + ceilingBottom) * 0.5f;
                BuildSlab(room.transform, "WallLeft",  new Vector3(-halfWidth, wallY, 0f), new Vector2(1f, wallHeight), new Color(0.25f, 0.28f, 0.32f), wallSprite);
                BuildSlab(room.transform, "WallRight", new Vector3( halfWidth, wallY, 0f), new Vector2(1f, wallHeight), new Color(0.25f, 0.28f, 0.32f), wallSprite);

                var doorMidY = (floorTop + doorTop) * 0.5f;
                var doors = new List<DoorSocket>
                {
                    AddDoorSocket(room.transform, "DoorEast", new Vector3( halfWidth, doorMidY, 0f), DoorDirection.East),
                    AddDoorSocket(room.transform, "DoorWest", new Vector3(-halfWidth, doorMidY, 0f), DoorDirection.West),
                };
                SetPrivateField(rt, "doors", doors);

                var playerSpawn = new GameObject("PlayerSpawn");
                playerSpawn.transform.SetParent(room.transform, worldPositionStays: false);
                playerSpawn.transform.localPosition = new Vector3(0f, -3f, 0f);
                SetPrivateField(rt, "playerSpawn", playerSpawn.transform);

                var enemyAnchorList = new List<Transform>();
                for (var i = 0; i < enemyAnchors; i++)
                {
                    var go = new GameObject($"EnemySpawn_{i}");
                    go.transform.SetParent(room.transform, worldPositionStays: false);
                    var x = Mathf.Lerp(-halfWidth + 3f, halfWidth - 3f, enemyAnchors == 1 ? 0.5f : (float)i / (enemyAnchors - 1));
                    go.transform.localPosition = new Vector3(x, -halfHeight + 2f, 0f);
                    enemyAnchorList.Add(go.transform);
                }
                SetPrivateField(rt, "enemySpawns", enemyAnchorList);

                var hostageAnchorList = new List<Transform>();
                for (var i = 0; i < hostageAnchors; i++)
                {
                    var go = new GameObject($"HostageSpawn_{i}");
                    go.transform.SetParent(room.transform, worldPositionStays: false);
                    var x = hostageAnchors == 1 ? 0f : Mathf.Lerp(-halfWidth + 4f, halfWidth - 4f, (float)i / (hostageAnchors - 1));
                    go.transform.localPosition = new Vector3(x, -halfHeight + 1.6f, 0f);
                    hostageAnchorList.Add(go.transform);
                }
                SetPrivateField(rt, "hostageSpawns", hostageAnchorList);

                PrefabUtility.SaveAsPrefabAsset(room, path);
                Object.DestroyImmediate(room);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SmokeTestSetup] AuthorRoomTemplate({id}) threw: {e}");
            }
        }

        private static void EnsureBiomeConfig()
        {
            try
            {
                EnsureFolder("Assets/ScriptableObjects");
                EnsureFolder("Assets/ScriptableObjects/Biomes");

                if (AssetDatabase.LoadMainAssetAtPath(BiomeConfigPath) != null)
                    AssetDatabase.DeleteAsset(BiomeConfigPath);

                var instance = ScriptableObject.CreateInstance<BiomeConfig>();
                AssetDatabase.CreateAsset(instance, BiomeConfigPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                var biome = AssetDatabase.LoadAssetAtPath<BiomeConfig>(BiomeConfigPath);
                if (biome == null)
                {
                    Debug.LogError("[SmokeTestSetup] BiomeConfig load returned null after CreateAsset.");
                    return;
                }

                SetPrivateField(biome, "Id", "biome_starter");
                SetPrivateField(biome, "DisplayName", "Starter Biome");
                SetPrivateField(biome, "RoomsPerRun", 5);

                var rooms = new List<GameObject>();
                AddIfPresent(rooms, RoomPrefabPath);
                AddIfPresent(rooms, RoomHallwayPath);
                AddIfPresent(rooms, RoomBossArenaPath);
                SetPrivateField(biome, "RoomTemplates", rooms);

                var enemyData = new List<EnemyData>();
                var grunt   = AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyDataPath);
                var charger = AssetDatabase.LoadAssetAtPath<EnemyData>(ChargerDataPath);
                if (grunt != null) enemyData.Add(grunt);
                if (charger != null) enemyData.Add(charger);
                SetPrivateField(biome, "EnemyPool", enemyData);

                var bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
                if (bossPrefab != null) SetPrivateField(biome, "BossPrefab", bossPrefab);

                EditorUtility.SetDirty(biome);
                AssetDatabase.SaveAssets();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SmokeTestSetup] EnsureBiomeConfig threw: {e}");
            }
        }

        private static void AddIfPresent(List<GameObject> dest, string path)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go != null) dest.Add(go);
        }

        private static void EnsureHostagePrefab()
        {
            try
            {
                EnsureFolder("Assets/Prefabs");
                if (AssetDatabase.LoadMainAssetAtPath(HostagePrefabPath) != null)
                    AssetDatabase.DeleteAsset(HostagePrefabPath);

                var go = new GameObject("Hostage");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 1;
                var sprite = LoadKenneySprite("character_hostage.png");
                if (sprite != null)
                {
                    sr.sprite = sprite;
                    sr.flipX = true;
                    sr.color = new Color(1f, 0.85f, 0.4f); // golden tint to read as 'rescue me'
                }
                else
                {
                    var ps = go.AddComponent<ProceduralSquare>();
                    SetSerializedColor(ps, new Color(1f, 0.85f, 0.4f));
                }

                var rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = 3.5f;
                rb.freezeRotation = true;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                var solid = go.AddComponent<BoxCollider2D>();
                solid.isTrigger = false;
                solid.size = new Vector2(0.6f, 1f);

                var triggerCol = go.AddComponent<CircleCollider2D>();
                triggerCol.isTrigger = true;
                triggerCol.radius = 0.7f;

                var hostage = go.AddComponent<Hostage>();
                SetPrivateField(hostage, "scoreReward", 50);

                PrefabUtility.SaveAsPrefabAsset(go, HostagePrefabPath);
                Object.DestroyImmediate(go);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SmokeTestSetup] EnsureHostagePrefab threw: {e}");
            }
        }

        private static void EnsureRescueBurstPrefab()
        {
            try
            {
                EnsureFolder("Assets/Prefabs");
                if (AssetDatabase.LoadMainAssetAtPath(RescueBurstPath) != null)
                    AssetDatabase.DeleteAsset(RescueBurstPath);

                var go = new GameObject("Vfx_RescueBurst");
                var burst = go.AddComponent<SimpleParticleBurst>();
                SetPrivateField(burst, "count", 20);
                SetPrivateField(burst, "speed", 6f);
                SetPrivateField(burst, "speedJitter", 1f);
                SetPrivateField(burst, "lifetime", 0.8f);
                SetPrivateField(burst, "size", 0.22f);
                SetPrivateField(burst, "color", new Color(0.35f, 1f, 0.55f, 1f));
                SetPrivateField(burst, "gravityScale", 0.4f);
                SetPrivateField(burst, "coneAngleDegrees", 360f);
                SetPrivateField(burst, "sortingOrder", 12);

                PrefabUtility.SaveAsPrefabAsset(go, RescueBurstPath);
                Object.DestroyImmediate(go);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SmokeTestSetup] EnsureRescueBurstPrefab threw: {e}");
            }
        }

        // Boss variant — single big tough enemy that ends the level when killed.
        // Reuses GroundGruntAI for now; boss-specific behaviour (multi-stage HP,
        // attack patterns, telegraphs) is M4 work.
        private static void EnsureBossAssets()
        {
            try
            {
                EnsureFolder("Assets/ScriptableObjects/Enemies");
                EnsureFolder("Assets/Prefabs");

                if (AssetDatabase.LoadMainAssetAtPath(BossDataPath) != null)
                    AssetDatabase.DeleteAsset(BossDataPath);
                if (AssetDatabase.LoadMainAssetAtPath(BossPrefabPath) != null)
                    AssetDatabase.DeleteAsset(BossPrefabPath);

                var dataInstance = ScriptableObject.CreateInstance<EnemyData>();
                AssetDatabase.CreateAsset(dataInstance, BossDataPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                var data = AssetDatabase.LoadAssetAtPath<EnemyData>(BossDataPath);
                if (data == null)
                {
                    Debug.LogError($"[SmokeTestSetup] LoadAssetAtPath returned null after CreateAsset for {BossDataPath}");
                    return;
                }

                SetPrivateField(data, "Id", "enemy_boss");
                SetPrivateField(data, "DisplayName", "Boss");
                SetPrivateField(data, "Archetype", EnemyArchetype.Boss);
                SetPrivateField(data, "MaxHealth", 250);
                SetPrivateField(data, "MoveSpeed", 1.6f);
                SetPrivateField(data, "AggroRange", 16f);
                SetPrivateField(data, "AttackRange", 1.5f);
                SetPrivateField(data, "AttackCooldown", 0.7f);
                SetPrivateField(data, "ContactDamage", 20);
                SetPrivateField(data, "ScoreOnKill", 200);
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssets();

                var go = new GameObject("Enemy_Boss");
                go.transform.localScale = new Vector3(1.8f, 1.8f, 1f); // ~2x size

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 1;
                var sprite = LoadKenneySprite("character_boss.png");
                if (sprite != null)
                {
                    sr.sprite = sprite;
                    sr.flipX = true;
                }
                else
                {
                    var ps = go.AddComponent<ProceduralSquare>();
                    SetSerializedColor(ps, new Color(0.6f, 0.1f, 0.1f));
                }

                var rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = 3.5f;
                rb.freezeRotation = true;
                rb.mass = 5f; // heavier — bullets shouldn't shove it as much
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                var col = go.AddComponent<BoxCollider2D>();
                col.size = new Vector2(1f, 1f); // collider stays 1x1 in local; transform.localScale 1.8 makes world 1.8x1.8

                var ai = go.AddComponent<GroundGruntAI>();
                SetPrivateField(ai, "data", data);
                SetPrivateField(ai, "flashRenderer", sr);
                SetPrivateField(ai, "patrolDistance", 4f);
                SetPrivateField(ai, "groundMask", (LayerMask)1);

                var edgeCheck = new GameObject("EdgeCheck");
                edgeCheck.transform.SetParent(go.transform, worldPositionStays: false);
                edgeCheck.transform.localPosition = new Vector3(0.6f, -0.6f, 0);
                SetPrivateField(ai, "edgeCheck", edgeCheck.transform);

                AddEnemyHealthBarTo(go, ai);

                var prefab = PrefabUtility.SaveAsPrefabAsset(go, BossPrefabPath);
                Object.DestroyImmediate(go);

                var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
                SetPrivateField(data, "Prefab", prefabAsset);
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssets();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SmokeTestSetup] EnsureBossAssets threw: {e}");
            }
        }

        private static void CreateWaveSpawner(GameObject roomMiddle, GameObject roomLeft, GameObject roomRight, Transform playerTransform)
        {
            var go = new GameObject("WaveSpawner");
            var spawner = go.AddComponent<WaveSpawner>();

            var grunt   = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            var charger = AssetDatabase.LoadAssetAtPath<GameObject>(ChargerPrefabPath);
            var boss    = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
            if (grunt != null)   SetPrivateField(spawner, "gruntPrefab",   grunt);
            if (charger != null) SetPrivateField(spawner, "chargerPrefab", charger);
            if (boss != null)    SetPrivateField(spawner, "bossPrefab",    boss);

            SetPrivateField(spawner, "playerTransform", playerTransform);
            SetPrivateField(spawner, "minSpawnDistance", 5f);

            var anchors = new List<Transform>();
            CollectAnchors(roomMiddle, anchors);
            CollectAnchors(roomLeft, anchors);
            CollectAnchors(roomRight, anchors);
            SetPrivateField(spawner, "spawnAnchors", anchors);

            // Five waves, last one is the boss + escort.
            var waves = new List<WaveSpawner.WaveConfig>
            {
                new WaveSpawner.WaveConfig { gruntCount = 3, chargerCount = 0, bossCount = 0 },
                new WaveSpawner.WaveConfig { gruntCount = 4, chargerCount = 1, bossCount = 0 },
                new WaveSpawner.WaveConfig { gruntCount = 5, chargerCount = 2, bossCount = 0 },
                new WaveSpawner.WaveConfig { gruntCount = 6, chargerCount = 3, bossCount = 0 },
                new WaveSpawner.WaveConfig { gruntCount = 0, chargerCount = 2, bossCount = 1 },
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

        // Standard variant — delegates to the generic AuthorRoomTemplate so all
        // four variants (Standard, Hallway, BossArena, plus future ones) go
        // through one code path.
        private static void EnsureRoomTemplatePrefab()
            => AuthorRoomTemplate(RoomPrefabPath, "room_standard", new Vector2(30f, 14f), enemyAnchors: 3, hostageAnchors: 2);

        // Builds an EnemyHealthBar GameObject as a child of `enemy`. Two child
        // SpriteRenderers (background + fill) using ProceduralSquare for the
        // visual; EnemyHealthBar component scales the fill each frame.
        private static void AddEnemyHealthBarTo(GameObject enemy, EnemyBase enemyBase)
        {
            var bar = new GameObject("HealthBar");
            bar.transform.SetParent(enemy.transform, worldPositionStays: false);
            bar.transform.localPosition = Vector3.zero;

            // Background (dim track).
            var bg = new GameObject("BarBg");
            bg.transform.SetParent(bar.transform, worldPositionStays: false);
            bg.transform.localScale = new Vector3(1.1f, 0.10f, 1f);
            var bgSr = bg.AddComponent<SpriteRenderer>();
            bgSr.sortingOrder = 9;
            var bgPs = bg.AddComponent<ProceduralSquare>();
            SetSerializedColor(bgPs, new Color(0f, 0f, 0f, 0.6f));

            // Fill (scaled by ratio).
            var fill = new GameObject("BarFill");
            fill.transform.SetParent(bar.transform, worldPositionStays: false);
            fill.transform.localScale = new Vector3(1.1f, 0.10f, 1f);
            var fillSr = fill.AddComponent<SpriteRenderer>();
            fillSr.sortingOrder = 10;
            var fillPs = fill.AddComponent<ProceduralSquare>();
            SetSerializedColor(fillPs, new Color(0.30f, 0.85f, 0.35f));

            var hb = bar.AddComponent<EnemyHealthBar>();
            SetPrivateField(hb, "target", enemyBase);
            SetPrivateField(hb, "fillRenderer", fillSr);
            SetPrivateField(hb, "backgroundRenderer", bgSr);
            SetPrivateField(hb, "fullWidth", 1.1f);
            SetPrivateField(hb, "thickness", 0.10f);
            SetPrivateField(hb, "offset", new Vector3(0f, 0.85f, 0f));
            SetPrivateField(hb, "showSecondsAfterHit", 2f);
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
            // Load all weapons fresh at the moment of use — sidesteps the
            // post-CreateAsset fake-null window from earlier sessions.
            var pistol  = AssetDatabase.LoadAssetAtPath<WeaponData>(PistolAssetPath);
            var smg     = AssetDatabase.LoadAssetAtPath<WeaponData>(SmgAssetPath);
            var shotgun = AssetDatabase.LoadAssetAtPath<WeaponData>(ShotgunAssetPath);
            var rocket  = AssetDatabase.LoadAssetAtPath<WeaponData>(RocketAssetPath);
            var sniper  = AssetDatabase.LoadAssetAtPath<WeaponData>(SniperAssetPath);
            var knife   = AssetDatabase.LoadAssetAtPath<WeaponData>(KnifeAssetPath);

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
            // Player carries all six starting weapons; Q (SwapWeapon action)
            // cycles between them. maxCarried bumped to 6 so the slot is full.
            var loadout = new List<WeaponData>();
            if (pistol  != null) loadout.Add(pistol);
            if (smg     != null) loadout.Add(smg);
            if (shotgun != null) loadout.Add(shotgun);
            if (rocket  != null) loadout.Add(rocket);
            if (sniper  != null) loadout.Add(sniper);
            if (knife   != null) loadout.Add(knife);
            if (loadout.Count == 0)
                Debug.LogError("[SmokeTestSetup] All weapons null when wiring Player — bullets will not fire.");
            SetPrivateField(weaponHolder, "startingLoadout", loadout);
            SetPrivateField(weaponHolder, "maxCarried", 6);
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
                    settings.npotScale = TextureImporterNPOTScale.None;
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
            // Always re-author so changes (e.g. adding a TrailRenderer below)
            // land on the next 'Build Smoke Test Scene' run.
            if (AssetDatabase.LoadMainAssetAtPath(BulletPrefabPath) != null)
                AssetDatabase.DeleteAsset(BulletPrefabPath);

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

            // Glowing trail behind the bullet. Sprites/Default works in URP 2D
            // (URP doesn't ship a default trail material). Width tapers
            // from full to zero across a 0.15s lifetime so the trail looks
            // like a streak rather than a solid line.
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.15f;
            trail.startWidth = 0.6f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.05f;
            trail.numCornerVertices = 0;
            trail.numCapVertices = 0;
            trail.sortingOrder = 2;
            var trailShader = Shader.Find("Sprites/Default");
            if (trailShader != null)
                trail.sharedMaterial = new Material(trailShader) { name = "BulletTrailMat" };
            trail.colorGradient = new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(new Color(1f, 0.95f, 0.50f), 0f),
                    new GradientColorKey(new Color(1f, 0.50f, 0.10f), 1f),
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f),
                },
            };

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

        private static void EnsureSmgAsset()
        {
            try
            {
                EnsureFolder("Assets/ScriptableObjects/Weapons");
                if (AssetDatabase.LoadMainAssetAtPath(SmgAssetPath) != null)
                    AssetDatabase.DeleteAsset(SmgAssetPath);
                var instance = ScriptableObject.CreateInstance<WeaponData>();
                AssetDatabase.CreateAsset(instance, SmgAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                var smg = AssetDatabase.LoadAssetAtPath<WeaponData>(SmgAssetPath);
                if (smg == null) return;

                SetPrivateField(smg, "Id", "weapon_smg");
                SetPrivateField(smg, "DisplayName", "SMG");
                SetPrivateField(smg, "Mode", FireMode.Auto);
                SetPrivateField(smg, "Damage", 4);
                SetPrivateField(smg, "FireRate", 12f);
                SetPrivateField(smg, "ProjectilesPerShot", 1);
                SetPrivateField(smg, "SpreadDegrees", 5f);
                SetPrivateField(smg, "ProjectileSpeed", 22f);
                SetPrivateField(smg, "ProjectileLifetime", 1.5f);
                SetPrivateField(smg, "Knockback", 0.5f);
                SetPrivateField(smg, "Recoil", 0f);
                SetPrivateField(smg, "Infinite", true);
                SetPrivateField(smg, "MagazineSize", 30);
                SetPrivateField(smg, "ReloadSeconds", 1.2f);

                var bullet = AssetDatabase.LoadAssetAtPath<GameObject>(BulletPrefabPath);
                if (bullet != null) SetPrivateField(smg, "ProjectilePrefab", bullet);
                var flash = AssetDatabase.LoadAssetAtPath<GameObject>(MuzzleFlashPath);
                if (flash != null) SetPrivateField(smg, "MuzzleFlashPrefab", flash);

                EditorUtility.SetDirty(smg);
                AssetDatabase.SaveAssets();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SmokeTestSetup] EnsureSmgAsset threw: {e}");
            }
        }

        private static void EnsureShotgunAsset()
        {
            try
            {
                EnsureFolder("Assets/ScriptableObjects/Weapons");
                if (AssetDatabase.LoadMainAssetAtPath(ShotgunAssetPath) != null)
                    AssetDatabase.DeleteAsset(ShotgunAssetPath);
                var instance = ScriptableObject.CreateInstance<WeaponData>();
                AssetDatabase.CreateAsset(instance, ShotgunAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                var sg = AssetDatabase.LoadAssetAtPath<WeaponData>(ShotgunAssetPath);
                if (sg == null) return;

                SetPrivateField(sg, "Id", "weapon_shotgun");
                SetPrivateField(sg, "DisplayName", "Shotgun");
                SetPrivateField(sg, "Mode", FireMode.SemiAuto);
                SetPrivateField(sg, "Damage", 6);
                SetPrivateField(sg, "FireRate", 1.5f);
                SetPrivateField(sg, "ProjectilesPerShot", 6);
                SetPrivateField(sg, "SpreadDegrees", 25f);
                SetPrivateField(sg, "ProjectileSpeed", 16f);
                SetPrivateField(sg, "ProjectileLifetime", 1.2f);
                SetPrivateField(sg, "Knockback", 2f);
                SetPrivateField(sg, "Recoil", 0.1f);
                SetPrivateField(sg, "Infinite", true);
                SetPrivateField(sg, "MagazineSize", 6);
                SetPrivateField(sg, "ReloadSeconds", 1.6f);

                var bullet = AssetDatabase.LoadAssetAtPath<GameObject>(BulletPrefabPath);
                if (bullet != null) SetPrivateField(sg, "ProjectilePrefab", bullet);
                var flash = AssetDatabase.LoadAssetAtPath<GameObject>(MuzzleFlashPath);
                if (flash != null) SetPrivateField(sg, "MuzzleFlashPrefab", flash);

                EditorUtility.SetDirty(sg);
                AssetDatabase.SaveAssets();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SmokeTestSetup] EnsureShotgunAsset threw: {e}");
            }
        }

        private static void EnsureRocketAsset()
        {
            try
            {
                EnsureFolder("Assets/ScriptableObjects/Weapons");
                if (AssetDatabase.LoadMainAssetAtPath(RocketAssetPath) != null)
                    AssetDatabase.DeleteAsset(RocketAssetPath);
                var instance = ScriptableObject.CreateInstance<WeaponData>();
                AssetDatabase.CreateAsset(instance, RocketAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                var rl = AssetDatabase.LoadAssetAtPath<WeaponData>(RocketAssetPath);
                if (rl == null) return;

                SetPrivateField(rl, "Id", "weapon_rocket");
                SetPrivateField(rl, "DisplayName", "Rocket Launcher");
                SetPrivateField(rl, "Mode", FireMode.SemiAuto);
                SetPrivateField(rl, "Damage", 50);
                SetPrivateField(rl, "FireRate", 0.8f);
                SetPrivateField(rl, "ProjectilesPerShot", 1);
                SetPrivateField(rl, "SpreadDegrees", 0f);
                SetPrivateField(rl, "ProjectileSpeed", 12f);
                SetPrivateField(rl, "ProjectileLifetime", 2.5f);
                SetPrivateField(rl, "Knockback", 5f);
                SetPrivateField(rl, "Recoil", 0.3f);
                SetPrivateField(rl, "Infinite", true);
                SetPrivateField(rl, "MagazineSize", 4);
                SetPrivateField(rl, "ReloadSeconds", 2.5f);

                var bullet = AssetDatabase.LoadAssetAtPath<GameObject>(BulletPrefabPath);
                if (bullet != null) SetPrivateField(rl, "ProjectilePrefab", bullet);
                var flash = AssetDatabase.LoadAssetAtPath<GameObject>(MuzzleFlashPath);
                if (flash != null) SetPrivateField(rl, "MuzzleFlashPrefab", flash);

                EditorUtility.SetDirty(rl);
                AssetDatabase.SaveAssets();
            }
            catch (System.Exception e) { Debug.LogError($"[SmokeTestSetup] EnsureRocketAsset threw: {e}"); }
        }

        private static void EnsureSniperAsset()
        {
            try
            {
                EnsureFolder("Assets/ScriptableObjects/Weapons");
                if (AssetDatabase.LoadMainAssetAtPath(SniperAssetPath) != null)
                    AssetDatabase.DeleteAsset(SniperAssetPath);
                var instance = ScriptableObject.CreateInstance<WeaponData>();
                AssetDatabase.CreateAsset(instance, SniperAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                var sn = AssetDatabase.LoadAssetAtPath<WeaponData>(SniperAssetPath);
                if (sn == null) return;

                SetPrivateField(sn, "Id", "weapon_sniper");
                SetPrivateField(sn, "DisplayName", "Sniper Rifle");
                SetPrivateField(sn, "Mode", FireMode.SemiAuto);
                SetPrivateField(sn, "Damage", 80);
                SetPrivateField(sn, "FireRate", 0.7f);
                SetPrivateField(sn, "ProjectilesPerShot", 1);
                SetPrivateField(sn, "SpreadDegrees", 0f);
                SetPrivateField(sn, "ProjectileSpeed", 40f);
                SetPrivateField(sn, "ProjectileLifetime", 2f);
                SetPrivateField(sn, "Knockback", 3f);
                SetPrivateField(sn, "Recoil", 0.4f);
                SetPrivateField(sn, "Infinite", true);
                SetPrivateField(sn, "MagazineSize", 5);
                SetPrivateField(sn, "ReloadSeconds", 2.0f);

                var bullet = AssetDatabase.LoadAssetAtPath<GameObject>(BulletPrefabPath);
                if (bullet != null) SetPrivateField(sn, "ProjectilePrefab", bullet);
                var flash = AssetDatabase.LoadAssetAtPath<GameObject>(MuzzleFlashPath);
                if (flash != null) SetPrivateField(sn, "MuzzleFlashPrefab", flash);

                EditorUtility.SetDirty(sn);
                AssetDatabase.SaveAssets();
            }
            catch (System.Exception e) { Debug.LogError($"[SmokeTestSetup] EnsureSniperAsset threw: {e}"); }
        }

        private static void EnsureKnifeAsset()
        {
            try
            {
                EnsureFolder("Assets/ScriptableObjects/Weapons");
                if (AssetDatabase.LoadMainAssetAtPath(KnifeAssetPath) != null)
                    AssetDatabase.DeleteAsset(KnifeAssetPath);
                var instance = ScriptableObject.CreateInstance<WeaponData>();
                AssetDatabase.CreateAsset(instance, KnifeAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                var kn = AssetDatabase.LoadAssetAtPath<WeaponData>(KnifeAssetPath);
                if (kn == null) return;

                SetPrivateField(kn, "Id", "weapon_knife");
                SetPrivateField(kn, "DisplayName", "Knife");
                SetPrivateField(kn, "Mode", FireMode.Melee);
                SetPrivateField(kn, "Damage", 25);
                SetPrivateField(kn, "FireRate", 3f);
                SetPrivateField(kn, "ProjectilesPerShot", 0);
                SetPrivateField(kn, "SpreadDegrees", 0f);
                SetPrivateField(kn, "ProjectileSpeed", 0f);
                SetPrivateField(kn, "ProjectileLifetime", 0f);
                SetPrivateField(kn, "Knockback", 4f);
                SetPrivateField(kn, "Recoil", 0f);
                SetPrivateField(kn, "Infinite", true);
                SetPrivateField(kn, "MagazineSize", 1);
                SetPrivateField(kn, "ReloadSeconds", 0f);
                SetPrivateField(kn, "MeleeRange", 1.6f);

                EditorUtility.SetDirty(kn);
                AssetDatabase.SaveAssets();
            }
            catch (System.Exception e) { Debug.LogError($"[SmokeTestSetup] EnsureKnifeAsset threw: {e}"); }
        }

        private static void EnsureFlyerAssets()
        {
            try
            {
                EnsureFolder("Assets/ScriptableObjects/Enemies");
                EnsureFolder("Assets/Prefabs");

                if (AssetDatabase.LoadMainAssetAtPath(FlyerDataPath) != null)
                    AssetDatabase.DeleteAsset(FlyerDataPath);
                if (AssetDatabase.LoadMainAssetAtPath(FlyerPrefabPath) != null)
                    AssetDatabase.DeleteAsset(FlyerPrefabPath);

                var dataInstance = ScriptableObject.CreateInstance<EnemyData>();
                AssetDatabase.CreateAsset(dataInstance, FlyerDataPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>(FlyerDataPath);
                if (data == null) return;

                SetPrivateField(data, "Id", "enemy_flyer");
                SetPrivateField(data, "DisplayName", "Flyer");
                SetPrivateField(data, "Archetype", EnemyArchetype.Flyer);
                SetPrivateField(data, "MaxHealth", 15);
                SetPrivateField(data, "MoveSpeed", 3.5f);
                SetPrivateField(data, "AggroRange", 14f);
                SetPrivateField(data, "AttackRange", 1.0f);
                SetPrivateField(data, "AttackCooldown", 0.8f);
                SetPrivateField(data, "ContactDamage", 10);
                SetPrivateField(data, "ScoreOnKill", 25);
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssets();

                var go = new GameObject("Enemy_Flyer");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 1;
                var sprite = LoadKenneySprite("character_flyer.png");
                if (sprite != null)
                {
                    sr.sprite = sprite;
                    sr.flipX = true;
                    sr.color = new Color(0.6f, 0.7f, 1f); // bluish cold tint
                }
                else
                {
                    var ps = go.AddComponent<ProceduralSquare>();
                    SetSerializedColor(ps, new Color(0.4f, 0.5f, 0.9f));
                }

                var rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f; // flies
                rb.freezeRotation = true;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.linearDamping = 1.5f; // gentle drag so it can hover

                var col = go.AddComponent<BoxCollider2D>();
                col.size = new Vector2(0.9f, 0.9f);

                var ai = go.AddComponent<FlyerAI>();
                SetPrivateField(ai, "data", data);
                SetPrivateField(ai, "flashRenderer", sr);

                AddEnemyHealthBarTo(go, ai);

                var prefab = PrefabUtility.SaveAsPrefabAsset(go, FlyerPrefabPath);
                Object.DestroyImmediate(go);

                var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FlyerPrefabPath);
                SetPrivateField(data, "Prefab", prefabAsset);
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssets();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SmokeTestSetup] EnsureFlyerAssets threw: {e}");
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
                SetPrivateField(enemyData, "MaxHealth", 30);
                SetPrivateField(enemyData, "MoveSpeed", 2.5f);
                SetPrivateField(enemyData, "AggroRange", 8f);
                SetPrivateField(enemyData, "AttackRange", 1.2f);
                SetPrivateField(enemyData, "AttackCooldown", 1.0f);
                SetPrivateField(enemyData, "ContactDamage", 10);
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

                AddEnemyHealthBarTo(go, ai);

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

        private static void CreateGameOverScreen(int hostagesTotal, int playerMaxHp = 100)
        {
            var go = new GameObject("GameOverScreen");
            var screen = go.AddComponent<GameOverScreen>();
            screen.SetHostagesTotal(hostagesTotal);
            screen.SetPlayerHealth(playerMaxHp, playerMaxHp);
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
            var rescue = AssetDatabase.LoadAssetAtPath<GameObject>(RescueBurstPath);
            if (rescue != null) SetPrivateField(spawner, "rescueBurstPrefab", rescue);
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

        private static void CreateBackgroundForLevel(List<GameObject> rooms)
        {
            // Compute the X bounds of the laid-out rooms so the background
            // covers them with margin on each side.
            float minX = -50f, maxX = 50f;
            if (rooms.Count > 0)
            {
                minX = float.MaxValue;
                maxX = float.MinValue;
                foreach (var r in rooms)
                {
                    if (r == null) continue;
                    var rt = r.GetComponent<RoomTemplate>();
                    var size = rt != null ? rt.Size : new Vector2(30f, 14f);
                    var pos = r.transform.position;
                    minX = Mathf.Min(minX, pos.x - size.x * 0.5f);
                    maxX = Mathf.Max(maxX, pos.x + size.x * 0.5f);
                }
            }
            var centerX = (minX + maxX) * 0.5f;
            var width = (maxX - minX) + 80f; // 40 of margin each side
            var height = 40f;

            var go = new GameObject("Background");
            go.transform.position = new Vector3(centerX, 0f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = -50;
            var bg = LoadKenneySprite("tile_bg.png");
            if (bg != null)
            {
                sr.sprite = bg;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.size = new Vector2(width, height);
            }
            else
            {
                go.transform.localScale = new Vector3(width, height, 1f);
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

        // Builds a screen-space-overlay Canvas with an InputSystemUIInputModule
        // EventSystem, an OnScreenStick driving Gamepad leftStick (which the
        // InputActions Move binding picks up), and two OnScreenButtons for
        // Jump and Fire mapped to the same Gamepad paths the InputActions
        // already accept. Auto-disables in Editor when not running on a
        // touch screen so it doesn't get in the way during desktop testing.
        private static void CreateTouchControlsCanvas()
        {
            // EventSystem (skip if one already exists in the scene).
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }

            var canvasGo = new GameObject("TouchControlsCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Auto-hide on desktop with a gamepad attached, show on mobile / touch.
            canvasGo.AddComponent<TouchControlsAutoVisibility>();

            // Smaller still. Sized in 1920x1080 reference space:
            // ~5% of screen width per control, sit in the corners.
            CreateOnScreenStick(canvasGo.transform,
                anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(0f, 0f),
                pivot: new Vector2(0f, 0f),
                anchoredPos: new Vector2(40f, 40f),
                size: new Vector2(100f, 100f),
                controlPath: "<Gamepad>/leftStick");

            CreateOnScreenButton(canvasGo.transform,
                label: "JUMP",
                anchorMin: new Vector2(1f, 0f),
                anchorMax: new Vector2(1f, 0f),
                pivot: new Vector2(1f, 0f),
                anchoredPos: new Vector2(-110f, 50f),
                size: new Vector2(70f, 70f),
                controlPath: "<Keyboard>/space");

            CreateOnScreenButton(canvasGo.transform,
                label: "FIRE",
                anchorMin: new Vector2(1f, 0f),
                anchorMax: new Vector2(1f, 0f),
                pivot: new Vector2(1f, 0f),
                anchoredPos: new Vector2(-30f, 90f),
                size: new Vector2(80f, 80f),
                controlPath: "<Keyboard>/leftCtrl");
        }

        private static void CreateOnScreenStick(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size, string controlPath)
        {
            // Outer ring background.
            var ringGo = new GameObject("Joystick");
            ringGo.transform.SetParent(parent, worldPositionStays: false);
            var ringRect = ringGo.AddComponent<RectTransform>();
            ringRect.anchorMin = anchorMin;
            ringRect.anchorMax = anchorMax;
            ringRect.pivot = pivot;
            ringRect.anchoredPosition = anchoredPos;
            ringRect.sizeDelta = size;
            var ringImg = ringGo.AddComponent<Image>();
            ringImg.color = new Color(1f, 1f, 1f, 0.18f);

            // Inner thumb (this is what OnScreenStick moves).
            var thumbGo = new GameObject("Thumb");
            thumbGo.transform.SetParent(ringGo.transform, worldPositionStays: false);
            var thumbRect = thumbGo.AddComponent<RectTransform>();
            thumbRect.anchorMin = new Vector2(0.5f, 0.5f);
            thumbRect.anchorMax = new Vector2(0.5f, 0.5f);
            thumbRect.pivot = new Vector2(0.5f, 0.5f);
            thumbRect.sizeDelta = size * 0.45f;
            thumbRect.anchoredPosition = Vector2.zero;
            var thumbImg = thumbGo.AddComponent<Image>();
            thumbImg.color = new Color(1f, 1f, 1f, 0.42f);

            var stick = thumbGo.AddComponent<OnScreenStick>();
            stick.controlPath = controlPath;
        }

        private static void CreateOnScreenButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size, string controlPath)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent, worldPositionStays: false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.22f);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, worldPositionStays: false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var text = labelGo.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 38;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;

            var btn = go.AddComponent<OnScreenButton>();
            btn.controlPath = controlPath;
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
