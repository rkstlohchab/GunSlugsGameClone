# Plan — Cross-Platform GunSlugs-Style Game (Unity 2D, iOS + Android)

## Context

Greenfield project at `/Users/raksithlochabb/Documents/GitHub/GunSlugsGameClone`. The user wants a cross-platform mobile clone of GunSlugs 2 — a 2D pixel-art run-and-gun platformer with procedurally generated rooms, multiple characters, varied weapons, and biome bosses.

**IP boundary:** the original is a commercial title by Orange Pixel. This project ships a *gameplay homage* with **original art, audio, characters, and branding** — same genre and feel, no copied assets, sprites, names, or trademarks. Every art and audio asset is either created for this project or sourced under CC0 / CC-BY with attribution.

**Decisions locked with the user:**
- **Engine:** Unity 6 LTS, 2D + URP
- **Scope:** Full feature parity (biomes, character roster, weapon roster, bosses, unlocks)
- **Assets:** Original pixel art (commissioned/in-house) + CC0/CC-BY audio
- **Features:** Touch controls + MFi/Bluetooth gamepad, local same-device co-op, online leaderboards (Game Center on iOS, Play Games Services on Android)
- **Out of scope for v1:** ads, IAP, online multiplayer

Plan is milestone-based because full parity is ~5–6 months of focused work for a small team. Each milestone produces a runnable build on both platforms before the next begins.

---

## Tech Stack

| Concern | Choice | Rationale |
|---|---|---|
| Engine | Unity 6 LTS (`6000.x` LTS) | Latest LTS, stable mobile pipeline |
| Renderer | Universal Render Pipeline (URP) 2D | 2D Lights, post-fx, mobile-tuned |
| Input | Unity Input System (`com.unity.inputsystem`) | Touch + gamepad + keyboard from one action map |
| Tiles | Tilemap + 2D Tilemap Extras (Rule Tiles) | Procedural room composition |
| Camera | Cinemachine 2D | Player follow, screen shake, co-op group framing |
| Animation | 2D Animation + Sprite Skinning *or* AnimatedSprite (frame-based) | Frame-based animations match pixel-art aesthetic |
| Content | Addressables | Lazy-load biomes/audio, smaller initial download |
| Persistence | JSON file in `Application.persistentDataPath` | Portable, easy to back up |
| Audio | Unity Audio + AudioMixer | Music/SFX/UI buses, per-bus volume |
| Leaderboards iOS | Apple GameKit (Game Center) via Apple Unity Plugins | Official, native |
| Leaderboards Android | Google Play Games Services plugin for Unity | Official, native |
| Build (iOS) | Unity → Xcode project → Xcode 16+ → device / TestFlight | Requires macOS |
| Build (Android) | Unity → Gradle → `.aab` for Play Store, `.apk` for internal | Standard |
| Source control | Git + Git LFS (sprites, audio, prefabs) | LFS is mandatory for binary assets |
| CI (optional) | GitHub Actions + game.ci Unity images | Automated builds per branch |

---

## Project Layout

```
GunSlugsGameClone/
├── ProjectSettings/                # Unity project settings (committed)
├── Packages/                       # manifest.json — package versions
├── Assets/
│   ├── Art/
│   │   ├── Sprites/{Player,Enemies,Weapons,Pickups,FX,UI}
│   │   ├── Tilesets/{Biome01_Jungle,Biome02_Desert,...}
│   │   └── Animations/             # .anim + AnimatorControllers
│   ├── Audio/{Music,SFX,UI}        # CC0/CC-BY with ATTRIBUTIONS.md
│   ├── Prefabs/
│   │   ├── Player/                 # Player + WeaponHolder + 2P variant
│   │   ├── Enemies/                # one prefab per enemy type, Boss/
│   │   ├── Weapons/                # WeaponData SO + Bullet prefabs
│   │   ├── Pickups/                # health, ammo, weapon-swap
│   │   └── Rooms/                  # room templates per biome
│   ├── Scenes/
│   │   ├── Boot.unity              # init services, load MainMenu
│   │   ├── MainMenu.unity
│   │   ├── Hub.unity               # character/weapon select
│   │   └── Game.unity              # gameplay (single scene, level streamed in)
│   ├── ScriptableObjects/
│   │   ├── Weapons/*.asset         # WeaponData (damage, fire-rate, spread, ...)
│   │   ├── Enemies/*.asset         # EnemyData
│   │   ├── Biomes/*.asset          # BiomeConfig (tileset, palette, music, enemy pool, boss)
│   │   └── Characters/*.asset      # CharacterData (stats, sprite, unlock cost)
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── GameManager.cs      # scene flow, run state
│   │   │   ├── SaveSystem.cs       # JSON read/write w/ versioning
│   │   │   ├── AudioManager.cs     # mixer + pooled sources
│   │   │   ├── EventBus.cs         # decoupled events (OnPlayerHit, OnRoomCleared,...)
│   │   │   └── ObjectPool.cs       # bullets, FX, enemies
│   │   ├── Player/
│   │   │   ├── PlayerController.cs # movement, jump, dash
│   │   │   ├── PlayerHealth.cs
│   │   │   ├── WeaponHolder.cs     # equip/swap/fire
│   │   │   └── PlayerInputBridge.cs# binds Input System actions to actions
│   │   ├── Enemies/
│   │   │   ├── EnemyBase.cs
│   │   │   ├── AI/                 # GroundGrunt, Flyer, Turret, Charger, ...
│   │   │   └── Bosses/             # one class per boss
│   │   ├── Weapons/
│   │   │   ├── WeaponBase.cs
│   │   │   ├── Projectile.cs       # pooled bullet
│   │   │   └── WeaponData.cs       # SO definition
│   │   ├── Procedural/
│   │   │   ├── LevelGenerator.cs   # picks rooms, stitches doors, places spawners
│   │   │   ├── RoomTemplate.cs     # door sockets + spawn anchors metadata
│   │   │   └── BiomeRunner.cs      # progresses room→room→boss per biome
│   │   ├── Input/
│   │   │   ├── InputActions.inputactions
│   │   │   └── ControlSchemeWatcher.cs # auto-switch touch ↔ gamepad
│   │   ├── UI/
│   │   │   ├── HUD.cs              # health, ammo, score, mini-map
│   │   │   ├── TouchControlsUI.cs  # virtual joystick + buttons
│   │   │   ├── MainMenu.cs, PauseMenu.cs, ResultsScreen.cs
│   │   │   └── HubUI.cs            # character/weapon select
│   │   ├── Meta/
│   │   │   ├── ProgressionSystem.cs# unlocks, currency
│   │   │   └── Achievements.cs
│   │   └── Services/
│   │       ├── ILeaderboardService.cs
│   │       ├── GameCenterLeaderboards.cs   # iOS, #if UNITY_IOS
│   │       └── PlayGamesLeaderboards.cs    # Android, #if UNITY_ANDROID
│   ├── Settings/
│   │   ├── URP-2D-Mobile.asset
│   │   ├── InputSystem_Actions.inputactions
│   │   └── AudioMixer.mixer
│   └── Editor/                     # tooling: room template validator, etc.
├── ATTRIBUTIONS.md                 # CC asset credits
├── README.md
└── .gitattributes                  # LFS rules for *.png, *.psd, *.wav, *.ogg
```

---

## Milestones

Each milestone ends with a tagged build that runs on a physical iOS device and a physical Android device. No milestone is "done" until both builds are verified.

### M0 — Bootstrap (~1 week)
- `git init` + `.gitignore` (Unity template) + `.gitattributes` (LFS for binary assets)
- Install Unity 6 LTS + iOS Build Support + Android Build Support modules
- Create project, import URP 2D, Input System, Cinemachine, 2D Tilemap Extras, Addressables, TextMeshPro
- Set bundle IDs (`com.<studio>.gunslugsclone`), icons, splash, target SDK levels
- First "hello sprite" build to iOS device (Xcode) and Android device (`.apk`)
- **Done when:** an empty scene with one moving sprite runs at 60 FPS on both target devices

### M1 — Core gameplay loop (~3 weeks)
- `PlayerController`: 8-directional movement, jump w/ coyote-time + jump-buffer, optional dash
- `WeaponHolder` + `WeaponBase` + one weapon (pistol) using `Projectile` pool
- `EnemyBase` + one enemy (ground grunt) with patrol/aggro
- One static handcrafted room with tilemap collisions
- `HUD` (health + ammo) and game-over/restart loop
- `EventBus` + `SaveSystem` skeleton (JSON, versioned)
- **Done when:** Player can move, shoot, kill enemy, die, restart — on-device

### M2 — Procedural levels (~3 weeks)
- `RoomTemplate` (door sockets N/E/S/W + spawn anchors as child transforms)
- `LevelGenerator`: graph of rooms per biome, seeded RNG, deterministic for replay
- `BiomeConfig` SO referenced by generator (tileset, palette, enemy pool, music, boss)
- Author 8–12 starter rooms for first biome; verify no unreachable spawns
- Cinemachine 2D with confiner per room
- **Done when:** every run produces a fresh, traversable level; no infinite loops or unreachable rooms

### M3 — Combat content (~4 weeks)
- Full weapon roster (~10): pistol, SMG, shotgun, assault rifle, sniper, rocket launcher, flamethrower, laser, grenade-throw, melee — each as a `WeaponData` SO + prefab + SFX
- Enemy roster (~8 archetypes): grunt, fast charger, flyer, turret, sniper, exploder, shielded, summoner — all extending `EnemyBase`, configurable via `EnemyData` SO
- Pickups: health, ammo, weapon-swap, currency
- Damage system, knockback, hit-flash, screen shake (Cinemachine impulse)
- Object pooling for bullets, FX, enemies (target zero allocs in steady-state combat)
- **Done when:** combat feels responsive at 60 FPS with 20+ active entities + 50+ projectiles

### M4 — Biomes & bosses (~4 weeks)
- 4 biomes minimum (e.g. jungle, desert, factory, ice — original themes, original names)
- Tileset + palette + music per biome
- 1 boss per biome (~4 unique bosses), each in its own arena room with multi-phase pattern
- Run progression: biome 1 → 2 → 3 → 4 → end-of-run scoring
- Difficulty curve: enemy density / HP / new enemy types per biome
- **Done when:** a complete run from biome 1 boss to biome 4 boss is winnable and tuned

### M5 — Controls & co-op (~2 weeks)
- Touch UI: virtual joystick + jump + fire + weapon-swap + dash buttons, layout-tested on phone and tablet aspect ratios; safe-area aware
- `ControlSchemeWatcher`: auto-hide touch UI when MFi/Bluetooth gamepad connects, restore on disconnect
- Local 2-player co-op on one device:
  - Player 2 joins via gamepad pairing or split-touch layout
  - Cinemachine target group keeps both players framed
  - Shared lives or independent lives — pick one (recommend independent + revive-on-touch)
- Pause that doesn't break input rebinding
- **Done when:** single-player works with touch *and* gamepad; co-op works with two gamepads on iPad/Android tablet

### M6 — Meta progression & leaderboards (~3 weeks)
- Hub scene: character select (~6 characters with stat differences), weapon-loadout select
- Currency + unlock costs (earned per-run, persistent in save)
- Achievements list (defined as SOs, awarded via `EventBus`)
- iOS leaderboards via Apple Unity Plugins (GameKit) — submit on run end, fetch top scores
- Android leaderboards via Google Play Games Services plugin — same surface via `ILeaderboardService` so callers don't branch
- App Store Connect + Play Console: register leaderboards & achievements with matching IDs
- **Done when:** scores from both platforms appear on their respective dashboards; unlocks persist across reinstalls (via cloud save deferred to post-v1, local-only for now)

### M7 — Polish, store, submission (~3 weeks)
- Full audio pass: music per biome, SFX library (CC0/CC-BY) catalogued in `ATTRIBUTIONS.md`
- Particle FX (URP 2D lights for muzzle flash, explosions)
- Localization scaffold (English-only ship, but strings extracted) — TextMeshPro + a string table
- Settings screen: audio volume, controls, language, credits, restore-purchases stub
- Privacy policy + App Tracking Transparency prompt (iOS) + Play Data Safety form
- Store assets: icon, screenshots (5+ per device class), feature graphic, trailer (optional)
- Submit to TestFlight (iOS) and Google Play internal testing track
- **Done when:** both store listings reach reviewable state and a 30-minute play session has zero crashes

**Estimated total: ~23 weeks (5–6 months) for a focused small team.**

---

## Critical Files To Be Created

These are the files whose design matters most — the rest of the codebase derives from them.

- `Assets/Scripts/Core/GameManager.cs` — single-source-of-truth state machine: `Boot → MainMenu → Hub → Run(Biome[i]) → Results`
- `Assets/Scripts/Core/EventBus.cs` — typed pub/sub so systems (UI, audio, achievements, save) don't depend on each other
- `Assets/Scripts/Core/SaveSystem.cs` — versioned JSON, atomic write (write to `.tmp`, rename), schema migration hook
- `Assets/Scripts/Procedural/LevelGenerator.cs` — seeded, deterministic, returns a graph of room instances
- `Assets/Scripts/Procedural/RoomTemplate.cs` — designer-authored prefab metadata: door sockets, spawn anchors, biome tag
- `Assets/Scripts/Weapons/WeaponBase.cs` + `WeaponData.cs` SO — every weapon is data, not code
- `Assets/Scripts/Enemies/EnemyBase.cs` + `EnemyData.cs` SO — same principle for enemies
- `Assets/Scripts/Services/ILeaderboardService.cs` — single interface; iOS/Android impls swap via `#if UNITY_IOS / UNITY_ANDROID`
- `Assets/Settings/InputSystem_Actions.inputactions` — one action map (`Gameplay`) with `Move`, `Aim`, `Fire`, `Jump`, `Dash`, `SwapWeapon`, `Pause`; bindings cover keyboard, gamepad, and touch on-screen controls

---

## Reusable Building Blocks (To Adopt, Not Reinvent)

- **Unity Input System on-screen controls** — built-in `OnScreenStick` / `OnScreenButton`. No reason to write a custom virtual joystick.
- **Cinemachine 2D Confiner / Target Group** — handles per-room camera bounds and co-op framing without custom code.
- **Object pooling** — Unity's `UnityEngine.Pool.ObjectPool<T>` (built-in since 2021). Don't write a bespoke pool.
- **Addressables** — lazy-load biome assets to keep first install <150 MB.
- **Apple Unity Plugins (GameKit)** — Apple's official Game Center plugin (`com.apple.unityplugin.gamekit`).
- **Google Play Games Plugin for Unity** — official from `playgameservices/play-games-plugin-for-unity`.
- **Kenney + OpenGameArt + Freesound (CC0/CC-BY)** — placeholder/permanent audio. All credits go in `ATTRIBUTIONS.md`.

---

## Verification

For every milestone:

1. **Editor smoke test** — Play in Unity Editor; manually walk the new feature.
2. **Edit-mode unit tests** (Unity Test Framework) — pure-logic systems only: `LevelGenerator` determinism (same seed → same graph), `SaveSystem` round-trip, `WeaponData` math (DPS, spread cone), `ProgressionSystem` unlock rules.
3. **Play-mode tests** for at least: player damage, enemy death, room transition, co-op join/leave.
4. **iOS device build** — `File → Build Settings → iOS → Build`, open the Xcode project, run on a physical iPhone (Xcode 16+, iOS 17+ target). Verify:
   - 60 FPS in busy combat (Xcode FPS gauge)
   - Touch controls responsive within safe area
   - MFi controller pairing auto-hides touch UI
   - Game Center sign-in prompt appears, score submits
5. **Android device build** — `File → Build Settings → Android → Build`, install `.apk` via `adb install -r`. Verify:
   - 60 FPS on a mid-tier device (e.g. Pixel 6a)
   - Bluetooth controller works
   - Play Games sign-in succeeds, score submits
6. **Co-op verification** — two gamepads paired to a tablet; both players spawn, both can damage enemies, both deaths handled.
7. **Cold-launch crash check** — kill app, reopen 10× per platform. No crashes, save persists.
8. **Final submission gate (M7 only)** — TestFlight build distributed to ≥2 external testers; Play Console internal testing track build downloaded and run by ≥2 testers; both report no blockers over 30 min of play.

---

## Risks & Mitigations

| Risk | Mitigation |
|---|---|
| IP/trademark overlap with Orange Pixel | Original art, audio, names, and branding only. Add `ATTRIBUTIONS.md`. Don't reuse character names, weapon names, or biome names from the original. |
| Performance on low-end Android | Object pool everything, cap simultaneous bullets/FX, profile every milestone with Unity Profiler attached to device, keep URP 2D Renderer settings mobile-tuned (no MSAA, low-res post). |
| Procedural generator producing unfair / impossible rooms | Editor-time validator runs over every authored `RoomTemplate` (every door reachable, no spawn-in-wall). Generator unit-tested with fixed seeds in CI. |
| Apple/Google leaderboard API churn | Keep all platform code behind `ILeaderboardService` so changes are localized. |
| Asset pipeline bottleneck (art) | Start M3+ enemy work with placeholder Kenney sprites; swap to original art in M7 polish. Don't block code on art. |
| iOS/Android build divergence over time | Tag a build at the end of every milestone on *both* platforms; fix divergence immediately, never accumulate. |

---

## Open Questions (Defer to After Approval)

These don't block starting M0 but should be answered before the milestones they touch:

- M5: shared lives vs. independent lives in co-op?
- M6: exact character count and what stats differ between them?
- M6: cloud save in v1 or post-launch? (CloudKit / Play Games Saved Games)
- M7: studio name / app name / bundle ID — needed before App Store Connect + Play Console setup
