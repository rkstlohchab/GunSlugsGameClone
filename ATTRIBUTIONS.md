# Asset Attributions

Every third-party asset used in this project is listed here with its source, license, and (where required) author credit. **Add a row the moment a new asset is added — never let this file drift.**

The build is shipped only with assets whose licenses are compatible with commercial mobile distribution. Acceptable licenses:

- **CC0** — public domain, no attribution required (still listed here for traceability).
- **CC-BY 3.0 / 4.0** — attribution required, must appear in the in-game *Credits* screen and in this file.
- **CC-BY-SA** — **avoid** unless we are willing to share-alike derivative work; check before using.
- **Custom royalty-free packs** — keep the license file in the repo at `LICENSES/<pack-name>.txt`.

Anything from a commercial title (sprites, audio, names, fonts) is **forbidden**.

---

## Sprites & Tilesets

| Asset | Author | Source | License | Used For | Notes |
|---|---|---|---|---|---|
| Kenney "Pixel Platformer" | Kenney (Asset Jesus) | kenney.nl/assets/pixel-platformer | CC0 | Player + enemy + tile sprites; parallax background tiles | `Assets/Art/Kenney/character_*.png`, `tile_*.png`, `Backgrounds/tile_*.png` |
| Kenney "Smoke Particle Assets" | Kenney | kenney.nl/assets/smoke-particles | CC0 | Muzzle-flash + death-explosion frames | `Assets/Art/Kenney/Particles/flash*.png`, `explosion*.png` |
| Kenney "Particle Pack" | Kenney | kenney.nl/assets/particle-pack | CC0 | Spark / muzzle / smoke supplemental particles | `Assets/Art/Kenney/Particles/spark_03.png`, `muzzle_02.png`, `smoke_03.png` |

## Audio — Music

| Track | Author | Source | License | Used For | Notes |
|---|---|---|---|---|---|
| *(none yet — biome music TBD by user)* | | | | | |

## Audio — SFX

| SFX | Author | Source | License | Used For | Notes |
|---|---|---|---|---|---|
| Kenney "Sci-Fi Sounds" | Kenney | kenney.nl/assets/sci-fi-sounds | CC0 | Pistol / SMG / shotgun / sniper / rocket / reload SFX | `Assets/Audio/SFX/sfx_pistol.ogg`, `sfx_smg.ogg`, `sfx_shotgun.ogg`, `sfx_sniper.ogg`, `sfx_rocket.ogg`, `sfx_reload.ogg`, `sfx_death.ogg` |
| Kenney "Digital Audio Pack" | Kenney | kenney.nl/assets/digital-audio | CC0 | Jump + pickup SFX | `Assets/Audio/SFX/sfx_jump.ogg`, `sfx_pickup.ogg` |
| Kenney "Impact Sounds" | Kenney | kenney.nl/assets/impact-sounds | CC0 | Knife swing + enemy hit + player hurt SFX | `Assets/Audio/SFX/sfx_knife.ogg`, `sfx_enemy_hit.ogg`, `sfx_hurt.ogg` |

## Fonts

| Font | Author | Source | License | Used For | Notes |
|---|---|---|---|---|---|
| Kenney Pixel | Kenney | kenney.nl/assets/kenney-fonts | CC0 | Pixel-art font for HUD / menus (TMP asset to be generated in M5 polish) | `Assets/Fonts/Kenney_Pixel.ttf` |

## Code / Plugins

| Library | Author | Source | License | Used For | Notes |
|---|---|---|---|---|---|
| Unity Engine | Unity Technologies | unity.com | Unity Companion License | Engine | — |
| Apple Unity Plugins (GameKit) | Apple Inc. | github.com/apple/unityplugins | Apache-2.0 | iOS leaderboards (M6) | — |
| Google Play Games Plugin for Unity | Google | github.com/playgameservices/play-games-plugin-for-unity | Apache-2.0 | Android leaderboards (M6) | — |

---

## How to add a new asset

1. Drop the file into the appropriate `Assets/...` folder.
2. Add a row to the table above (Asset / Author / Source URL / License / Used For).
3. If license requires attribution, also add the credit string to the in-game Credits screen (M7).
4. If a license file accompanied the download, save it at `LICENSES/<pack-or-author>.txt`.
5. Commit the asset, the license file, and the updated `ATTRIBUTIONS.md` in one commit.
