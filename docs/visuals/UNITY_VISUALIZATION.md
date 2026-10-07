# Unity Storyboard Visualization

This visualization uses actual meshes downloaded from the asset links in
`D:/uni/intro_to_gp/used_asset.md`, staged in Unity 6. These are art previews of
the proposed game, not implemented gathering, brewing, or boss gameplay.

## Scenes and Renders

| View | Editable Unity scene | Render |
| --- | --- | --- |
| Forest exploration | `Assets/Visualization/Scenes/01-Exploration.unity` | `output/unity-visualization/01-Exploration.png` |
| Cottage preparation | `Assets/Visualization/Scenes/02-Crafting-Brewing.unity` | `output/unity-visualization/02-Crafting-Brewing.png` |
| Frog King fight | `Assets/Visualization/Scenes/03-Frog-King.unity` | `output/unity-visualization/03-Frog-King.png` |

All three renders target 2048 x 1152. The scene cameras use a consistent
first-person field of view, moss-green sleeves, and a six-slot hotbar.
Open `output/unity-visualization/index.html` to compare the views.

## Asset Use

- **POLYGON Enchanted Forest:** trees, ferns, mushrooms, grass, fallen logs,
  and ruined stone props for the woodland and marsh.
- **POLYGON Fantasy Kingdom:** a complete house preset for Grandpa's cottage
  exterior and a tower preset for the distant landmark and sky gate.
- **POLYGON Fantasy Characters:** the female witch prefab supplies both
  teammate bodies. Colored oversized hats distinguish the teammates.
- **SIMPLE Fantasy Interiors:** tables, shelves, fireplace, potion worktable,
  and an open book form the crafting and brewing spaces.
- **POLYGON Fantasy Rivals:** imported as an available source for the
  storyboard's later Root Guardian; the three primary shots do not depict it.

`output/unity-visualization/used-prefabs.txt` records the exact prefab paths
actually instantiated by the renderer. Import reports are in
`output/asset-visualization/import-reports/`.

The custom geometry comprises the floating landforms, cave framing and ore,
cauldron and brew, potion bottles, first-person hands and tools, teammate hat
overlays, lily pads, leap warning, and Frog King with its crown and Heartseed.
The Frog King is an original faceted model assembled by the scene builder;
it is not advertised as an included Synty character.

## Rebuild

In Unity, use **Little Witches > Build And Render Storyboard**. This saves the
three scenes and regenerates their PNGs. The builder operates only on the
visualization folders and does not add scenes to the game's build list.
Save any open scene before invoking it, since the builder creates new scenes.

For background rendering in PowerShell:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.5f2\Editor\Unity.exe' `
  -batchmode `
  -projectPath 'D:\uni\intro_to_gp\U6-First-Person-Multiplayer' `
  -executeMethod LittleWitches.Visualization.LittleWitchesVisualization.BuildAndRender `
  -logFile 'D:\uni\intro_to_gp\U6-First-Person-Multiplayer\output\asset-visualization\unity-render.log'
```

Do not add `-nographics`: this job needs the graphics device. The method exits
Unity when batch rendering finishes. Close any Unity instance using this
project before starting a second editor in batch mode.

Open a visualization scene and enter Play mode to inspect its layout. Hold
the right mouse button to look around; use W/A/S/D to move, Q/E to descend or
ascend, and Shift to move faster. This is a camera inspection mode.

## Import Isolation

Source art resides in `Assets/Visualization/Imported/`, retaining the source
asset GUIDs so prefab, mesh, and texture references resolve. Package scripts,
DLLs, demonstration scenes, and embedded installers are excluded. Materials
inside that imported folder are adapted to this project's URP Lit shader.
Biome vegetation uses a custom URP shader that preserves the pack's
leaf/trunk textures and their vertex-color mask.
The downloaded source archives remain in the ignored
`output/asset-visualization/packages/` folder.

## Visual Limitations

The previews use staged poses and custom geometry for unprovided gameplay
props. They establish composition, scale, light, and pack compatibility.
Character animation, transparent potion glass, richer terrain, and actual
interactive systems are subsequent production work.
