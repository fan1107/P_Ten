# Jewel Plum gameplay art — 2026-11-03

The approved Deep Plum / Jewel Arts reference is implemented as independent runtime sprites, not a flattened gameplay screenshot. Existing game rules, board geometry, scores, timers, purchase and revive logic are unchanged. Existing home illustration is retained. UI placement is retained; the current chapter remains the gameplay heading.

## Production resources

All new art is under `Assets/Resources/Atlas/JewelPlum/`:

- `Background.png`: deep plum velvet field, gold Art Deco frame and emerald bottom ornament.
- `Tiles.png`: transparent sheet with 12 individually named sprites. No numerals are baked in.
- `Panels.png`: transparent sheet with dark, cream, plum button and bar-track sprites; sliced borders support existing panel dimensions.
- `JewelPlum.spriteatlas`: tiles and panels, bilinear filtering, no rotation/tight packing.
- `Cinzel.ttf`, `Cinzel SDF.asset`, `Cinzel-OFL.txt`: open-source serif face, with the existing Noto Sans / Korean fallback. Source: https://github.com/google/fonts/tree/main/ofl/cinzel .

Tile mapping: 1 emerald fan; 2 purple arches; 3 rose scallops; 4 teal steps; 5 champagne sunburst; 6 dusty rose petals; 7 lavender steps; 8 burgundy petals; 9 cobalt rays; 0 yellow sunburst; -1 sapphire stars; ×2 ruby geometry. Special colors are part of the artwork, so faces remain white rather than multiplying a color across the ivory medallion.

`JewelTheme` provides the art mapping; `MintTheme` retains the existing calling interface. `TileVisual` preserves the same numbered artwork across idle, selected, blocked and clearing states. Blocked sprites receive a translucent plum shade; selection retains the existing lift and adds a warm highlight. Numerals are separately rendered centered in each medallion.

## Build / validation

Unity menu: **THE TEN → Build Jewel Plum art** regenerates the named sprite rectangles, font and atlas from source sheets. The tile sheet has slightly inset outer columns; slicing uses its actual transparent gutters, not a uniform grid that would clip gold rims.

**THE TEN → Validate Jewel Plum art** runs five existing focused PlayMode cases. Result: **5 passed, 0 failed, 0 skipped**. XML: `Captures/jewel-plum-tests.xml`.

Checked: special sprite mapping and bonus/fireworks; Cinzel Latin/symbols and Korean fallback; selection lift without geometry changes; clear/reveal and immediate next selection; topmost spatial raycast.

Actual Unity screenshot: `Captures/jewel-plum-game.png`. Preview is paused and does not submit a score. No APK was rebuilt for this resource update.

## Rollback

Original image resources remain intact. Pre-change scripts are in `RND/2026-11-03-jewel-plum/before/`: MintTheme, TileVisual, TheTenApp, UiTypography, PrototypeFlowTests and the Editor assembly definition. Restore them to their corresponding Runtime/Tests/Editor locations to detach this theme; new JewelPlum resources may remain unused. Do not run the old Mint Art Builder to restore scripts.

## Generation record

Images were made with the built-in image generation tool from the user's Deep Plum concept, then imported through Unity. No scene/UI rasterization replaces actual game objects.

Production prompts:

1. **Tile sheet:** Match reference jewel tile materials, polished gold rims, colored enamel geometry and blank ivory medallions; twelve isolated orthographic portrait faces, 6 × 2 sheet, genuine transparent background, no text/numbers, all equal scale. Row 1 emerald fan / purple arches / rose scallops / teal steps / champagne sunburst / dusty rose petals; row 2 lavender steps / burgundy petals / cobalt rays / yellow sunburst / sapphire stars / ruby steps. Preserve transparent gutters and lower bevel thickness. A background-removal pass retained the exact artwork and positions while clearing outside alpha.
2. **Background:** Deep plum velvet portrait playing field matching the reference, thin rounded polished gold outer frame, ivory/gold Art Deco corners, small emerald fan at bottom center; empty uninterrupted interior, no logo, tiles, numbers, HUD, buttons, characters or exterior margins.
3. **Panels:** Four matching front-facing rounded gold double-rim panel assets, 2 × 2 transparent sheet, dark plum / warm ivory / burgundy / near-black plum interiors, no text/icons, subtle highlights and uniform centers suitable for 9-slice stretching.
