# CLAUDE.md — Project context for new sessions

This file is auto-loaded by Claude Code when working in this repository. Read it first before answering questions about the project.

## What this project is

Cross-platform **Unity 6 LTS** mobile game targeting **iOS and Android**. It is a *gameplay homage* to Orange Pixel's GunSlugs 2 (2D pixel-art run-and-gun platformer with procedurally-generated rooms). **Not a copy** — original art, audio, character names, weapon names, biome names, branding. The IP boundary is non-negotiable.

Full milestone plan lives at: [`clone-gunslugs2-game-for-indexed-dragon.md`](clone-gunslugs2-game-for-indexed-dragon.md)

## Tech stack (locked decisions)

- **Engine:** Unity 6 LTS, currently `6000.4.4f1` Apple Silicon
- **Renderer:** 2D + URP
- **Input:** Unity Input System (`com.unity.inputsystem`), action map `Gameplay`, PlayerInput in **Send Messages** mode (so methods are auto-named `OnMove`, `OnJump`, `OnFire(InputValue v)`, etc.)
- **Targets:** iOS 17+, Android API 26+ (ARM64, IL2CPP)
- **Tilemaps:** `com.unity.2d.tilemap` + Tilemap Extras
- **Sprites:** `com.unity.2d.sprite` (installed manually because the project was created from a default 3D template, not the 2D template)
- **UI:** `com.unity.ugui` (UGUI 2.0 — TextMeshPro is bundled inside it; do NOT try to install `com.unity.textmeshpro` separately, that package no longer exists in Unity 6)
- **Camera:** Cinemachine 3 (`Unity.Cinemachine` namespace)
- **Persistence:** JSON file in `Application.persistentDataPath`
- **Leaderboards:** Apple GameKit (iOS) and Google Play Games (Android), abstracted behind `ILeaderboardService` with platform stubs guarded by `#if UNITY_IOS` / `#if UNITY_ANDROID`. Real plugins are not yet installed — those land in M6.

## Repo layout

```
GunSlugsGameClone/
├── Assets/
│   ├── Editor/              # Editor-only tools (SmokeTestSetup, etc.)
│   ├── Scripts/             # Runtime code, one asmdef per layer (see below)
│   ├── Settings/            # InputSystem_Actions.inputactions and similar
│   ├── Scenes/              # Saved scenes (SmokeTest.unity)
│   ├── Prefabs/             # Generated prefabs (Bullet.prefab)
│   ├── ScriptableObjects/   # SO assets (weapon_pistol.asset)
│   └── Tests/EditMode/      # NUnit edit-mode tests
├── Packages/
├── ProjectSettings/
├── README.md                # User-facing setup instructions
├── ATTRIBUTIONS.md          # Asset credit ledger (CC0/CC-BY tracking)
└── clone-gunslugs2-game-for-indexed-dragon.md  # Full plan
```

### Assembly graph

Every script folder under `Assets/Scripts/` has its own `.asmdef` to enforce a strict layered dependency graph. Adding a reference is a deliberate decision; do not add cycles.

```
Core ←──────────── (no GunSlugs deps)
 ↑   ↖
Weapons ─────────── Core
 ↑   ↖
Player ──────────── Core, Weapons, Unity.InputSystem
 ↑
Enemies ─────────── Core, Weapons, Player
 ↑
Procedural ──────── Core, Weapons, Enemies, Player
                   
Input ───────────── Unity.InputSystem
Services ────────── (standalone)
Meta ────────────── Core, Services
UI ──────────────── Core, Player, Weapons, Services, Meta, Input, UnityEngine.UI, Unity.TextMeshPro, Unity.InputSystem
Editor (editor-only) ── Core, Player, Weapons, Unity.InputSystem
Tests.EditMode ──── Core, Weapons, Procedural, Meta, NUnit, TestRunner
```

Namespaces match: `GunSlugsClone.Core`, `GunSlugsClone.Player`, etc.

## How the user prefers to work

1. **Automate Unity setup with Editor menu commands.** The user is new to Unity and works on a Mac with only a trackpad — manual GUI setup (clicking through Hierarchy / Inspector / dragging components) is slow, error-prone, and they have explicitly pushed back on it. When a setup task can be done programmatically via `UnityEditor` APIs (creating GameObjects, wiring `[SerializeField]` fields via `SerializedObject`, generating `.prefab` and `.asset` files via `PrefabUtility.SaveAsPrefabAsset` / `AssetDatabase.CreateAsset`), wrap it in a `[MenuItem("GunSlugs/...")]` command in `Assets/Editor/`. The reference example is [`Assets/Editor/SmokeTestSetup.cs`](Assets/Editor/SmokeTestSetup.cs), which builds the entire smoke-test scene end-to-end with one menu click.
2. **Don't try to drive the Unity GUI for them.** I cannot remote-control the Editor. Anything that requires the GUI (installing packages via Package Manager, creating Unity projects in Hub, setting Project Settings) the user does. Anything else, automate.
3. **Trackpad note:** the user has no physical mouse buttons. Fire is bound to `<Mouse>/leftButton`, `<Gamepad>/rightTrigger`, and `<Keyboard>/leftCtrl`. They typically use Left Ctrl. A trackpad single-finger physical click also fires.

## Where we are (high-level — verify against `git log` for details)

- M0 (Bootstrap) ✅ — repo, .gitignore, .gitattributes, README, plan, full architecture scaffold
- M1 (Core gameplay loop) — *partial*. Player movement + jump + ground collision verified on-device. Weapon firing under active debugging.
- M2–M7 — not started.

## Design conventions worth knowing

- **Data-driven content.** Weapons, enemies, biomes, characters are defined as `ScriptableObject` assets, not hard-coded. `WeaponBase`, `EnemyBase` are stateless behaviour shells driven by `WeaponData` / `EnemyData` SOs. To add a new weapon, create a new `weapon_X.asset` — don't subclass.
- **Determinism.** `LevelGenerator` uses `DeterministicRng` (xorshift32) seeded explicitly so a given run replays identically. Don't sneak in `UnityEngine.Random` for gameplay-affecting decisions.
- **Loose coupling via `EventBus`.** Systems publish strongly-typed events (`PlayerDamagedEvent`, `EnemyKilledEvent`, `RoomClearedEvent`, …). UI/audio/save/achievements subscribe. Don't cross-reference systems directly when an event already exists.
- **Object pooling.** Use `UnityEngine.Pool.ObjectPool<T>` (built-in). Bullets, FX, and enemies should pool — currently most are still instantiated. Migrating to pools is M3 polish.
- **Procedural visuals stop-gap.** Until real pixel art lands, [`ProceduralSquare`](Assets/Scripts/Core/ProceduralSquare.cs) generates a tinted white square at runtime. It exists specifically to bypass Unity's flaky asset import pipeline for placeholder visuals — real sprites should replace it incrementally.

## Pitfalls already hit (don't re-introduce)

- **Don't rely on PNG asset import for placeholder sprites.** Unity's first-time texture-to-sprite import is timing-sensitive; `LoadAssetAtPath<Sprite>` returns null on the run that creates the asset. The pattern that works is `ProceduralSquare` (runtime sprite) or pre-existing committed assets. Solved in commit c9422f9.
- **Don't auto-fit BoxCollider2D.** When a `SpriteRenderer.sprite` is null (e.g. before sprite import finishes), an auto-fit `BoxCollider2D` becomes 0×0 and physics silently break. Always set `col.size = new Vector2(1, 1)` explicitly. Solved in commit 5c63f6c.
- **`com.unity.textmeshpro` doesn't exist in Unity 6** — TextMeshPro is bundled inside `com.unity.ugui`. Trying to install it by name returns "No results".
- **Don't change Unity input handling without restart prompting.** The "Active Input Handling" Player Setting requires an Editor restart. Don't tell the user "just install Input System, it'll switch automatically" — Unity 6 templates can default to Input Manager (Old) and require a manual switch to "Input System Package (New)".
- **`PlayerInput` SendMessages mode requires `OnX(InputValue v)` signatures.** Earlier versions of `PlayerController` used `InputAction.CallbackContext` which works only in "Invoke C# Events" / "Invoke Unity Events" modes. Don't switch back without changing all method signatures.

## Useful commands

```bash
# Run edit-mode tests (Unity must be running)
# Window → General → Test Runner → EditMode → Run All

# Build and run smoke test (after opening project in Unity)
# Top menu: GunSlugs → Build Smoke Test Scene → Play

# Search for specific files
git ls-files | rg <pattern>

# See milestone history
git log --oneline
```

## When in doubt

- **Implementation plan / scope questions** → read [`clone-gunslugs2-game-for-indexed-dragon.md`](clone-gunslugs2-game-for-indexed-dragon.md).
- **Asset credit / IP** → [`ATTRIBUTIONS.md`](ATTRIBUTIONS.md).
- **Setup / packages** → [`README.md`](README.md).
- **What changed recently** → `git log --oneline -20`.
