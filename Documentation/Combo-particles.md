# Combo particle prefabs

Location: Assets/Resources/Effects/Combo/ComboStage1.prefab through ComboStage5.prefab.

| Prefab | Combo | Main burst | Extra bursts | Style |
| --- | --- | --- | --- | --- |
| ComboStage1 | x1.5 | 18 | 0 | warm cream sparkles |
| ComboStage2 | x2 | 30 | 1 | mint burst |
| ComboStage3 | x2.5, x3 | 46 | 2 | blue elongated sparkles |
| ComboStage4 | x4 | 68 | 3 | violet multiple bursts |
| ComboStage5 | x5 | 92 | 4 | coral/gold finale |

Open a prefab and select its Particles child. Edit native Particle System Main (Start Color, Size, Speed, Lifetime), Emission bursts, Shape radius, Color over Lifetime and Size over Lifetime. Keep Local simulation space, useUnscaledTime, AlwaysSimulate and maxParticles <= 256. The builder creates missing prefabs only and will not overwrite tuned prefabs.

Root ComboParticleGraphic renders the native simulation inside the screen-space overlay Canvas. ParticleSystemRenderer is intentionally disabled because the UI renderer draws the particles. Keep the root raycast target disabled. Root Stretch Sparkles and Particle Sprite are editable too. Preview in Game view using the root component context menu Play combo burst during Play mode.

THE TEN/Create missing combo effect prefabs recreates any missing defaults.

ComboFeedback preloads five instances. Each successful combo match triggers the corresponding stage (including repeated x5 matches), stops the previous burst and reuses instances. x1 has no combo burst. Home/new-run hiding clears particles. The effect is centered behind the combo badge and does not intercept touches or change scoring/timing.

TileComboParticles emits at the center of each cleared tile face, including its selection lift. Survival uses stage 1 for the first match and follows the combo stage after that. Touch R&D uses ComboStage1 for every cleared tile, including matches made by deselection and the final tile. Emitters are pooled on demand at 0.85 board scale and reused on the next match. They outlive their tile objects and let touches pass through.

Punch tuning: stages emit 1/2/3/4/5 rapid bursts spaced 0.055s apart. Emission starts near the center, shoots outward quickly and decelerates sharply. Higher stages use larger particles and longer streaks. Apply punchier combo effect defaults explicitly resets these tuned modules; the create-missing menu continues preserving existing edits.
