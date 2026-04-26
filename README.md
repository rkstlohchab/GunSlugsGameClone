# GunSlugs-Style Mobile Game (Working Title)

Cross-platform 2D pixel-art run-and-gun for iOS and Android. **This project is a gameplay homage to the run-and-gun genre with original art, audio, characters, and branding** — no copied assets or trademarks from any commercial title.

> **Status:** M0 — Bootstrap. Repo scaffolded. Unity project not yet created.
> **Plan:** see [`clone-gunslugs2-game-for-indexed-dragon.md`](clone-gunslugs2-game-for-indexed-dragon.md)

---

## Tech Stack

- **Engine:** Unity 6 LTS (`6000.x` LTS) — 2D + URP
- **Targets:** iOS 17+ (iPhone/iPad), Android 8.0+ (API 26+)
- **Input:** Unity Input System (touch, MFi/Bluetooth gamepad, keyboard)
- **Tiles / camera:** Tilemap + 2D Tilemap Extras, Cinemachine 2D
- **Content:** Addressables for biome/audio streaming
- **Leaderboards:** Apple Game Center (iOS), Google Play Games Services (Android)
- **Source control:** Git + Git LFS

---

## One-time setup

### 1. Install tools

| Tool | Version | Notes |
|---|---|---|
| Unity Hub | latest | https://unity.com/download |
| Unity Editor | 6 LTS (`6000.x`) | Install via Hub. Add modules: **iOS Build Support**, **Android Build Support** (with OpenJDK + Android SDK + NDK) |
| Xcode | 16+ | macOS-only. Required for iOS device builds + signing. |
| Apple Developer account | active | For TestFlight + App Store. Game Center is configured in App Store Connect. |
| Google Play Console account | active | For Play Internal Testing + Production. Play Games Services configured in Play Console. |
| Git LFS | latest | `brew install git-lfs && git lfs install` |
| Android device + iOS device | physical | Simulators are useful for UI but **not** for performance verification. |

### 2. Initialise Git LFS in your local clone

```bash
cd /Users/raksithlochabb/Documents/GitHub/GunSlugsGameClone
git lfs install      # one-time per machine
git lfs pull         # after every clone
```

### 3. Create the Unity project (manual step)

The Unity project files are intentionally **not committed yet** — Unity Editor must generate them.

1. Open Unity Hub → **New project**
2. Template: **Universal 2D (Core)** (or "2D (URP)" depending on Hub version)
3. Editor version: **6000.x LTS**
4. Project name: `GunSlugsGameClone`
5. **Location:** point at `/Users/raksithlochabb/Documents/GitHub/`
   - Hub will create the project inside `GunSlugsGameClone/`. Existing files (`.gitignore`, `README.md`, etc.) at the root are preserved.
   - If Hub refuses because the folder isn't empty, create the project in a temp location and copy `Assets/`, `Packages/`, `ProjectSettings/` into this folder afterwards.
6. Click **Create project**.

### 4. Install required Unity packages

In Unity, open **Window → Package Manager** and install:

- `com.unity.inputsystem` (Input System)
- `com.unity.cinemachine` (Cinemachine)
- `com.unity.2d.tilemap.extras` (2D Tilemap Extras)
- `com.unity.addressables` (Addressables)
- `com.unity.textmeshpro` (TextMeshPro — usually preinstalled)
- `com.unity.test-framework` (Test Framework — usually preinstalled)

When prompted to switch to the new Input System backend, click **Yes** (Unity will restart).

### 5. Set bundle IDs and platform settings

**Edit → Project Settings → Player:**

- **Company name:** *(your studio name)*
- **Product name:** *(working title)*
- **Default Icon:** placeholder for now
- **iOS tab:**
  - Bundle Identifier: `com.<studio>.gunslugsclone`
  - Target minimum iOS Version: `17.0`
  - Camera Usage Description / Microphone Usage Description: leave blank (not used)
- **Android tab:**
  - Package Name: `com.<studio>.gunslugsclone`
  - Minimum API Level: `26 (Android 8.0)`
  - Target API Level: `Automatic (highest installed)`
  - Scripting Backend: `IL2CPP`
  - Target Architectures: `ARMv7` + `ARM64`

### 6. First "hello sprite" build

- Open `Assets/Scenes/SampleScene.unity` (Unity creates this by default)
- Drag any sprite into the scene; attach a simple movement script (we'll write the real `PlayerController` in M1)
- **File → Build Settings → iOS → Build** → produces an Xcode project; open it, set signing team, Run on physical device
- **File → Build Settings → Android → Build** → produces `.apk`; install with `adb install -r build.apk`

When that build runs at 60 FPS on both devices, **M0 is complete** and we move on to M1 (core gameplay loop).

---

## Repo layout (after Unity project is created)

```
GunSlugsGameClone/
├── Assets/                 # Unity assets (created by Unity)
├── Packages/               # Unity package manifest
├── ProjectSettings/        # Unity project settings
├── .gitignore
├── .gitattributes          # LFS rules
├── README.md               # this file
├── ATTRIBUTIONS.md         # CC asset credits
└── clone-gunslugs2-game-for-indexed-dragon.md   # full plan
```

See the plan document for the full proposed `Assets/` layout (Scripts, Prefabs, ScriptableObjects, etc.).

---

## Roadmap (high level)

| Milestone | Focus | Estimate |
|---|---|---|
| **M0** | Bootstrap (this) | ~1 wk |
| M1 | Core gameplay loop | ~3 wk |
| M2 | Procedural levels | ~3 wk |
| M3 | Combat content (weapons/enemies) | ~4 wk |
| M4 | Biomes + bosses | ~4 wk |
| M5 | Touch + gamepad + co-op | ~2 wk |
| M6 | Meta progression + leaderboards | ~3 wk |
| M7 | Polish + store submission | ~3 wk |
| **Total** | | **~23 wk** |

Full milestone definitions and "done when" criteria live in the plan document.

---

## Asset & IP policy

- **No assets, sprites, audio, character names, weapon names, or biome names from any commercial title.** All in-game content is original or sourced from CC0 / CC-BY pools (Kenney, OpenGameArt, Freesound).
- Every CC-BY asset is credited in [`ATTRIBUTIONS.md`](ATTRIBUTIONS.md). Update that file the moment a new asset is added.
- Commissioned art is "work for hire" — keep contracts on file.

---

## Useful commands

```bash
# Clone fresh
git clone <repo>
cd GunSlugsGameClone
git lfs pull

# Track a new binary asset type with LFS (rare — most are already in .gitattributes)
git lfs track "*.<ext>"
git add .gitattributes

# Verify LFS is working
git lfs ls-files | head
```
