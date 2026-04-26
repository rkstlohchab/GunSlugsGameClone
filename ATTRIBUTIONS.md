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
| *(none yet)* | | | | | |

## Audio — Music

| Track | Author | Source | License | Used For | Notes |
|---|---|---|---|---|---|
| *(none yet)* | | | | | |

## Audio — SFX

| SFX | Author | Source | License | Used For | Notes |
|---|---|---|---|---|---|
| *(none yet)* | | | | | |

## Fonts

| Font | Author | Source | License | Used For | Notes |
|---|---|---|---|---|---|
| *(none yet)* | | | | | |

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
