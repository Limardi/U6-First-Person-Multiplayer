# Gameplay Visual Generation Prompts

Status: prompts prepared; images have not been generated.

Deliverables: exploration.png, crafting-brewing.png, and frog-king-fight.png.
Format: three separate 16:9 PNG gameplay concept images, targeting 2048 x 1152 pixels.
These depict the proposed game; they are not screenshots from the current Unity project.

## Shared Visual Specification

Use case: stylized-concept.
Asset type: first-person cooperative game screenshot concept.
Style: polished stylized 3D with chunky silhouettes, subtly painted surface textures, expressive witches, and readable game environments. Cozy woodland exploration with adventure and danger. Smooth modeled terrain, not voxel blocks. Moss green foliage, warm wood, amber sunlight, and teal magic. Avoid photorealism, pixel art, flat illustration, cinematic posters, and dense particle effects.
Camera: player eye height, wide gameplay field of view, believable ground plane, naturally foreshortened hands and tools at the bottom edge. No third-person view of the controlled player.
Continuity: the player wears moss-green witch sleeves with worn brown cuffs. Teammates are small adult apprentice witches with oversized pointed hats, satchels, short cloaks, and chunky boots. The teammate with an ochre hat wears a rust cloak; the teammate with a teal hat wears a cream cloak. Keep their proportions, outfits, and equipment consistent in all images. Bottles use round glass bodies, corks, twine around the neck, and visibly colored liquid. Wands use carved branches with small teal crystal tips.
HUD: a tiny cream crosshair centered on the scene; a compact health indicator at the lower left; a six-slot hotbar centered along the bottom with clearly separated pictorial icons for a gathering tool, wand, herb, mushroom, healing bottle, and attack bottle. Highlight the held item's slot. HUD occupies little space and does not obscure interactions. No large inventory overlay, quest list, invented labels, watermark, title, or logo. Only the combat view has a boss bar and the exact text "Frog King".
Composition: one coherent full-frame screenshot per image. No collage, inset panels, large framing border, split-screen, or promotional typography.

## 1. Exploration - exploration.png

Primary request: show the player freely exploring a floating woodland island and gathering useful materials with two witch teammates.
Scene: a gently winding dirt trail through enormous trees, mossy rocks, and large fern leaves. Nearby mushrooms and herbs have clear collectible silhouettes. A cave entrance in the middle distance contains a visible glimmering ore vein. Grandpa's small timber cottage is visible behind the party through a break in the trees. Another opening reveals the edge of the floating island with clouds far below; a ruined tower on a distant ridge suggests a later destination. The scene connects the foreground resource, cave, cottage, and distant destination through a readable layout.
Player action: a moss-green-sleeved hand holds a simple wooden gathering tool with a stone head, ready to collect a cluster of amber mushrooms. The ochre-hatted teammate crouches to gather an herb; the teal-hatted teammate looks toward the cave. Keep both teammates naturally grounded in the scene, with believable hands and poses.
Lighting: clear warm morning light filtered through leaves, gentle distance haze, inviting colors, legible resources and paths.
HUD: use the shared HUD; highlight the gathering-tool slot. No extra text.
Constraints: emphasize several exploration choices and materials worth collecting. The foreground tool must not cover the collectible. Show the island's floating nature without making the entire scene an aerial island overview.

## 2. Crafting and Brewing - crafting-brewing.png

Primary request: show the same player and teammates preparing equipment and potions at Grandpa's cottage after gathering materials.
Reference handling: use the final exploration image as a style and identity reference. Preserve its witch outfits, proportions, player sleeve design, bottle shapes, wand design, and HUD layout. Change the setting and action to the cottage interior; do not preserve the exploration composition.
Scene: a cozy timber cottage with a stone floor, exposed beams, a sturdy crafting bench, shared supply shelves, and a large iron cauldron. The workbench visibly holds wood, stone, a carved wand, a simple gathering tool, and empty potion bottles. An open illustrated guidebook rests on a dry surface beside the cauldron, showing ingredient drawings and simple recipe symbols rather than illegible paragraphs. Collected mushrooms and herbs sit in a basket. A window provides a glimpse of the surrounding island woodland.
Player action: one moss-green-sleeved hand holds an herb over a gently bubbling teal cauldron. Keep the ingredient clearly above the liquid and the hand away from the rim. The ochre-hatted teammate sorts ore and wood at the crafting bench. The teal-hatted teammate bottles a finished brew at a separate nearby station. Position props so equipment crafting and potion brewing are both visible and understandable.
Lighting: warm afternoon window light with restrained teal cauldron illumination; readable shadows and wood texture.
HUD: use the identical shared HUD; highlight the herb slot. No boss bar or additional labels.
Constraints: the image must feel like interactive first-person gameplay, not a decorative interior painting. Keep separate functional spaces for the bench and cauldron. Do not add giant menus, floating recipe cards, additional characters, or random machinery.

## 3. Frog King Fight - frog-king-fight.png

Primary request: show the same player and teammates fighting the giant Frog King with equipment and potions they crafted.
Reference handling: use the final exploration image as a style and identity reference. Preserve its witch outfits, proportions, player sleeves, bottles, wand, and HUD. Change the scene and action to a boss fight.
Scene: a wide ruined marsh arena with mossy stone paving, shallow water, broken columns, lily pads at its edges, and open lanes for dodging. A huge expressive green Frog King wears a small crooked golden crown with a glowing teal Heartseed crystal. His scale is several times that of the teammates. The ruined sky gate is visible beyond him.
Boss action: the Frog King is airborne at the beginning of a heavy leap, facing the party, with a clearly visible amber landing-warning ring on open ground ahead of the player. Render enough separation between his body and the ground to communicate the leap; the frog has a coherent body, four limbs, and no humanoid weapons.
Player action: the right moss-green-sleeved hand holds a corked round attack potion glowing amber, ready to throw. The left hand holds the established branch wand in a lower ready position, leaving the central action clear. The ochre-hatted teammate attacks from the flank with a wand; the teal-hatted teammate uses a healing brew. Keep each action readable and potion effects restrained. No extra teammates or duplicated limbs.
Lighting: late afternoon marsh light, a clear warm-versus-teal contrast, subtle mist in the distance, high readability in the arena.
HUD: identical crosshair, lower-left health indicator, and six-slot hotbar; highlight the attack-bottle slot. Add a slim boss health bar across the upper center, roughly two-thirds full, with only the exact label "Frog King". Use readable cream lettering.
Constraints: show a fight players could navigate, with open dodge space and clear attack telegraph. No gore, cinematic camera, giant explosions, defeated teammates, tutorial overlays, or additional bosses.

## Generation and Review

Generate exploration first and inspect it before generating the other scenes with that image as a reference. If the generator uses an edit workflow for references, explicitly request a new scene and composition while preserving visual identity.

Check that all three images have the same camera style, character identities, equipment design, and HUD. Verify anatomically coherent hands, separate characters, readable resources, valid object interactions, and the exact boss label. Check that combat effects leave the landing warning visible. Correct individual issues with targeted edits.

Generation method is pending availability of the built-in image generator or an explicitly authorized API fallback. The API fallback requires OPENAI_API_KEY configured locally; never include the key in prompts, documents, or chat. Preserve existing images by choosing versioned filenames if these deliverable names already exist.
