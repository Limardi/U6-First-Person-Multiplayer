# Floating Forest Island

An editable Unity art showcase for Little Witches in the Wood. This scene
focuses on the forest and Grandpa's cottage exterior, with a cave entrance,
ingredient habitats, and a clearing reserved for the Root Guardian.

## Open And Inspect

The Git repository includes the authored scenes, generated geometry, scripts,
and render galleries. Licensed Synty source packs and downloaded archives are
kept locally and excluded from Git. On a new checkout, install your own copies
of POLYGON Enchanted Forest, POLYGON Fantasy Kingdom, and SIMPLE Fantasy
Interiors under `Assets/Visualization/Imported/`, preserving their source GUIDs.
The older storyboard scenes also require POLYGON Fantasy Characters.
Missing source packs cause unresolved prefab and texture references; the PNG
galleries remain viewable without Unity or the source packs.

The package importer can extract a `.unitypackage` into the expected folder:

```powershell
node output/asset-visualization/import-package.mjs 'C:/path/to/your/package.unitypackage'
```

Run it from the project root. The importer preserves source `.meta` files and
excludes package scripts and demo scenes. The saved scene materials use copied
textures from those packs; the original source materials remain local.

Open `Assets/Visualization/Scenes/04-Forest-Island.unity` in Unity 6. The
overview camera is active by default. Enter Play mode and hold the right
mouse button to look around. Use W/A/S/D to move, Q/E to move vertically,
and Shift for faster movement. This is a fly camera, not a player controller.

Seven saved cameras live under `06 Showcase Cameras`. Select a camera to see
its editor preview. To use another camera in Game view, disable the overview
camera component and enable the desired camera component. Save a copy of the
scene before experimenting if you want to retain its generated camera setup.

The hierarchy groups landform/trails, cottage dressing, woodland/habitats,
landmarks, atmosphere, and cameras separately. Prefab instances remain editable.
`Future Cave Interior Route` and `Future Marsh Route` mark expansion positions.

## Renders

Open `output/unity-island/index.html` for the offline gallery. The seven PNGs
are 2048 x 1152:

- `01-Island-Overview.png`: the island above the clouds and its rocky underside.
- `02-Cottage-Approach.png`: cottage, ingredient garden, and preparation area.
- `03-Forest-Trail.png`: the woodland trail and shaded mushroom habitat.
- `04-Cave-Entrance.png`: ridge, roots, lantern, and Moon Crystal accents.
- `05-Mushroom-Grove.png`: ornamental mushrooms, moss, ferns, and fallen logs.
- `06-Guardian-Grove.png`: ancient tree, open clearing, ruins, and teal crystals.
- `07-Meadow-And-Sky.png`: sunny wildflowers, branching trail, clouds, and sun.

`used-prefabs.txt` records exact source prefab paths. `scene-report.txt`
records the scene, generation seed, placement fingerprint, and validation/render information.
The `review-before` folder keeps the seven renders from before the world refinement pass.

## Rebuild

Use **Little Witches > Build And Render Forest Island** in Unity. Save your
open scene when prompted. Rebuilding replaces the generated island scene and
its dedicated generated assets; keep manual scene changes in a separately
named copy. The older storyboard scenes and game build list are not changed.

With this project closed in Unity, the batch command is:

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.3.5f2/Editor/Unity.exe' `
  -batchmode `
  -projectPath 'D:/uni/intro_to_gp/U6-First-Person-Multiplayer' `
  -executeMethod LittleWitches.Visualization.ForestIslandVisualization.BuildAndRender `
  -logFile 'D:/uni/intro_to_gp/U6-First-Person-Multiplayer/output/unity-island-build.log'
```

Rendering requires a graphics device; omit `-nographics`. The command saves
and reloads the island scene, renders the seven cameras, and exits Unity.

## Sky And Biomes

`03 Woodland And Habitats` contains dedicated `Sunny Wildflower Meadow`,
`Shaded Mushroom Grove`, and `Ancient Guardian Grove` groups. The meadow has
an open canopy and clustered flowers; the mushroom grove uses cooler ground
colors and dense moss/fungi; the guardian clearing keeps its center open with
roots, ruins, and crystals around its edge. The cave sits on a rocky ridge.

The `Cloud-Sky.mat` material uses `Little Witches/Island Sky`. Its Inspector
exposes upper sky, horizon, lower sky, cloud highlight/shadow colors, cloud
coverage, sun direction, and sun glow. The builder aligns the sky's sun with
`Late Afternoon Sun`. If rotating that light manually, update the material's
sun direction to the negative of the light's forward vector. Cloud banks
below the island provide foreground depth alongside the procedural sky.

For changes that survive rebuilding, edit `CreateBiomePockets`,
`MeadowColor`, `CreateAtmosphere`, and `CreateCameras` in the scene builder.
Manual edits to generated materials and the scene are replaced by a rebuild.

## World Refinement

The island has an irregular stone underside and a tighter cliff rim with
fewer repeated root silhouettes. Trail forks have imported wooden direction
signs, and lanterns mark the routes to the cave and guardian. The cave arch
has a rough inner edge and rock pieces across its brow. The overview camera
looks from the meadow side so the open habitat and woodland can be read together.

Tree placement reserves clear viewing corridors for the cottage, mushroom
grove, and guardian cameras. Ambient light uses a more neutral fill, while
copied foliage materials have minimum shadow fill and a small leaf backlight.
The original foliage materials keep their default shader values.

## Asset And Geometry Sources

- POLYGON Enchanted Forest: tree variants, ferns, logs, roots, mushrooms,
  ground patches, cliff edges, lantern stands, crystals, and stone ruins.
- POLYGON Fantasy Kingdom: cottage preset, fencing, barrels, flowers,
  flowering bushes, and cave/ridge rock pieces.
- SIMPLE Fantasy Interiors: outdoor potion table, open guidebook, and bottles.
- Original generated geometry: rolling ground, tapered underside, trails,
  clearing patches, garden beds, faceted cave arch/recess, grass blades, and cloud banks.

Generation uses seed `61006` with a private random generator so Unity editor
activity and prefab callbacks cannot consume its placement sequence.
A shared height function grounds placements;
landmarks and trails are authored, with deterministic vegetation variation.
Generated meshes, materials, sky, and volume profile live in
`Assets/Visualization/Generated/ForestIsland/`. Used materials are copied into
this folder for scene-local adaptation, including every LOD. Vegetation keeps
its leaf/trunk texture mask; cliff materials restore the pack's top/side
textures with a URP triplanar shader. Source materials are not edited by this builder.

This pass has no HUD, characters, harvestable resources, combat, multiplayer
integration, or playable collision guarantees. Cave and cottage interiors,
the marsh, and boss implementation are later work. This update was compiled,
generated, and checked in the open Unity Editor through Unity MCP. The editor
menu and optional batch command also support rebuilding without MCP.
