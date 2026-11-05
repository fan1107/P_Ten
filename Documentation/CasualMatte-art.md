# Casual Matte renewal — 2026-11-04

Approved direction: dark plum background, opaque matte patterned tiles, English THE TEN and a dominant 10 on the title. No jelly/gloss finish.

## Assets
Assets/Resources/Atlas/CasualMatte contains generated Tiles (12 named sprites), Icons (9 named sprites), HomeHero, Background, native nine-slice UI panels, Fredoka SDF and CasualMatte.spriteatlas.
Original generated art remains in the Codex generated_images folder; copies are self-contained in the project. Fredoka is from Google Fonts under the included SIL OFL.
THE TEN/Build Casual Matte art rebuilds import settings, sprite slices, native panels, font and atlas. Number labels are live TMP text, not baked into gameplay sprites.

## Runtime
JewelTheme remains a compatibility facade but now resolves CasualMatte resources. HomeMenu uses the new hero and real menu buttons. Gameplay moves time/score into one header, centers the combo badge, colors the bar by tier and enlarges/translates both tile and effect roots equally. Selected tiles lift and display a gold border edged in dark plum; particles use smooth positions and rounded shapes.
Gameplay models, 36-tile generation, scores, timers, chapter gates and commerce rules are unchanged. Legacy art remains available for rollback. APK was not rebuilt during this art change.

## Rollback
RND/2026-11-04-casual-matte/before contains the pre-renewal runtime sources and PlayMode tests. Restore those sources and refresh Unity to return to the preceding JewelPlum direction. Keep the old JewelPlum/MintCream resources until the direction is accepted.

## Verification
Unity Game view captures: Captures/casual-matte-home.png and Captures/casual-matte-game.png.
PlayMode: full 20-case run passed 19; remaining spatial-input test used a coordinate outside the enlarged board viewport. That coordinate was corrected and the case rerun separately. The test expectations also follow the new bar colors, font/layout and slogan; asynchronous reward verification uses a bounded condition wait.

Final result: 19/20 passed in full run bdd8d6e883784f17ab3bb4760bb569c2, and the corrected remaining case passed 1/1 in job 79c070f3109c4badb5b3fe22980f9e7d. All 20 cases have passing verification. An intervening test-runner initialization error occurred inside Unity's test framework before any case ran; reopening the game scene resolved it.

## Legacy asset cleanup — 2026-11-04
At the user's request, removed the old MintCream/pixel and JewelPlum art, their import metadata and three obsolete art builders. The runtime fallback to the old atlas is removed. The current matte assets and Korean fallback fonts remain. The earlier rollback directions above are historical: restoring old runtime backups alone no longer restores old art. Reference captures and source backups outside Assets remain as design history.

Home preview update: HomeHero now contains only the logo, 10 and pedestal. Three non-interactive sprites use the gameplay atlas and live number labels. Each app instance chooses positive-number triples totaling 10, excludes the previous saved combination, shuffles display order, and retains the choice through menu navigation. The existing home flow test verifies total, sprite mapping and menu stability.
