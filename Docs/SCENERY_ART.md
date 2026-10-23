# Wuxia Environment Art

Ten full-screen landscape paintings replace the previous single `stage_01.png`. PNG source dimensions are 1659×948 (manor 1660×948), about 16:9. The imported textures disable mipmaps and CPU readability, retain NPOT dimensions and use platform texture compression. All generated originals remain outside the project.

## Battle themes

`Game/Systems/SceneryArt.cs` maps the existing 30 stages without changing IDs, encounters, rewards or progression.

| File | Setting | Stages |
|---|---|---|
| battle_forest | Emei/Wudang woodland and mountain training | 1, 14, 18 |
| battle_road | Mountain trade road and bandit stockade | 3, 4, 15, 21, 26 |
| battle_manor | Inn, estate and academy building courtyard | 2, 5, 6, 8, 16, 25, 28 |
| battle_academy | Academy training/tournament arena | 11, 12, 13, 17 |
| battle_pavilion | Moonlit garden/residence terrace | 9, 10 |
| battle_cave | Mechanism trial and subterranean cavern | 7, 19, 20, 29 |
| battle_valley | Gorge and hostile mountain battlefield | 22, 23, 24, 27, 30 |

Tower and promotion duels use the academy arena; quest battles use the trade-road theme. These are reusable atmospheric themes, not literal reconstructions of every room or event in the novels. Major landmarks and props stay around the edges, leaving central space for combatants. Stage story panels inherit the selected environment; independent record story playback uses the archive.

## Menu mapping

| Tab | Environment |
|---|---|
| World | Mountain/forest clearing |
| Companions | Riverside inn (`menu_inn`) |
| Equipment | Swordsmith armory (`menu_forge`) |
| Martial arts | Academy training courtyard |
| Gacha | Moonlit pavilion |
| Bag | Riverside inn |
| Records | Scroll library (`menu_archive`) |

Dark overlays and existing card surfaces separate text from the art. `BackgroundCover` crops centered UVs to fill each rectangle with the original aspect ratio, without changing the image quad, blocking touches, introducing letterboxes or stretching the scenery. It refreshes when the viewport resizes, including safe-area changes.

## Validation

All ten files and Unity metadata exist and pass source dimension/aspect checks. UI/game/test C# compiles against Unity reference assemblies. EditMode regression sources cover seven themes, stage settings, menu availability, deletion of the old image, and cover geometry at 4:3, 16:9 and 20:9. The cloud cannot execute Unity rendering or those UI assertions; verify sprite cropping, transparent icon edges and contrast in Editor/Android.
