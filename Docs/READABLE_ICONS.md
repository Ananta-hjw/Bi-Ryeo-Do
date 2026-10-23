# Readable Icon Art

The painted wuxia icon set covers all 31 martial arts, 20 inner methods and 281 items currently returned by Core content, plus menu/command icons and eight item-kind defaults. Equipment grade remains a separate UI frame; the object silhouette, material and effects identify the item itself. Existing portrait, battle figure and background art is unchanged.

## Assets and mapping

`Assets/_Project/Resources/Art/Readable/catalog.json` maps each `(category, id)` to its own region in a shared PNG. `ArtLibrary` uses this catalog first and retains the previous per-file artwork as fallback. Categories are `Art`, `Inner`, `Item`, `Icon` and `DefaultItem`.

- `skills.png`: martial arts, inner methods, commands and main navigation, in an 8×8 grid.
- `items_0.png` through `items_4.png`: individual items and additional menu symbols, predominantly 8×8 grids.
- `special.png`: 4×2 sheet for two mythic weapons/accessories and all six polishing materials. Spirit crystals, silver ingots, ancient black ingots and the celestial meteor have distinct silhouettes.
- `details.png`: 4×2 sheet correcting selected item silhouettes, including the five-element saber, thunder wine flask, bear-gall medicine and thunder pendant.
- `equipment_repair.png`: 4×2 sheet with distinct star/comet, vermilion slash and jade vortex mythical weapons, plus corrected cloth robe, straw sandals, straw greaves, iron fan and leather leg wraps.
- `jin_biroedo.png`: transparent full-size transcendent icon, paired angular silver daggers without chains, white/violet lightning and a gold energy halo.

`cell` is zero-based, counted left to right and top to bottom. `columns` and `rows` describe the slicing grid for that entry. The same sheet may contain rows with different column counts; do not assume a single grid for the entire image. Six material illustrations in the final row of `items_3.png` use explicit normalized `x`, `y`, `width`, `height`; `y` is measured from the top. These measured overrides and the remapped final sheet account for the generated art's actual layout. Do not regenerate ordering from array positions alone.

The loader insets each region by three pixels to exclude grid seams. Sheet dimensions remain unchanged on import, mipmaps are disabled and textures are compressed. Sprites and source textures are cached and shared; no texture copies or GPU readbacks are needed.

## UI integration and checks

Lists, inspectors, gacha and battle commands all use `ArtLibrary`. Inner-method cards use `ArtLibrary.InnerArt`. `WuxiaGlyph` renders painted symbols for mapped keys while preserving vector frames, stance markers and decorative halos. Changing its key invalidates both geometry and texture binding.

`ReadableIconTests` covers content coverage, distinct item regions, valid sprite bounds, shared lookup results, fallback items and dynamic glyph texture changes. Run these tests in Unity EditMode. Cloud validation checks all 377 mappings, image bounds and metadata, and compiles UI/test sources; it does not execute Unity rendering or touch input.

The PNG sheets were created with image generation and their ID mappings were inspected and corrected against the existing content. To add an item, assign a distinct, visually verified region and add the catalog entry; keep the legacy fallback for future content without painted art.

## Equipment mapping audit (2026-10-23)

Reviewed all 281 item entries, including all 229 equipment entries, against the illustrated sheets.
62 equipment mappings were corrected. The wrong pictures came from layout differences within generated atlases:

- `items_0`: an extra weapon before the robes shifts the armor run by one; the final accessory row contains nine illustrations.
- `items_1`: an extra purple weapon before the normal weapons shifts subsequent weapons, armor and pants by one. The missing straw greaves and unclear leather wraps now have dedicated replacement pictures.
- `items_3`: the penultimate row contains nine illustrations, including the legendary equipment and the mythic orb. The final materials row still uses its existing six-column explicit regions.
- `equipment_repair`: repairs the two reported starter icons directly and replaces the coiled-wire picture of the iron fan with a recognizable folding fan.

The three non-Biroe mythic weapons use separate effects: 성락검 has platinum/gold stars and a comet trail, 참천도 has a vermilion/gold cutting crescent, 혼천검 has jade/turquoise flowing vortices. These are visual treatments, without new gameplay effects or item statistics. The Biroe armor and accessories retain their original purple lightning artwork; the later explicit weapon-icon exchange is described below.

`ITEM_ICON_AUDIT.csv` records every item's name, kind and resolved pixel rectangle (after the loader's three-pixel inset, with Y measured from the top). `corrected` marks mappings changed since the previous release (74 after the subsequent icon and aura updates); `verified` marks the remaining items. PNG artwork was inspected visually; automated checks verify all 377 catalog bounds, complete item coverage and zero overlapping item regions. These geometry checks complement, rather than replace, visual item identification.

Gacha grade previews now show three item cards per measured scrolling row, including a 128-pixel icon, name, effects and set. The grade and probability remain in the header. The close button stays outside the scrolling area. Existing three-column pull results and gacha probabilities are unchanged.

Regression coverage adds known-cell assertions for shifted armor, nine-column accessories, normal weapons, the mythic orb and the replacement atlas. The grade-preview test covers all grades at 1440/1920/2400-pixel widths, row counts, card separation, icon/name separation and no inventory/currency changes. Unity UI tests require the Editor; cloud validation compiles their source only.

## Jin Biroedo and grade backgrounds

The previous `jin_biroedo` region (`items_0`, cell 9) is now used by `biroe_blade`, as requested.
`jin_biroedo` uses its own transparent PNG, with the original paired-dagger composition based on the user's reference photograph; only the hanging chain was removed, without switching to crossed daggers.
Only the icon changed; the two weapons' effects and existing save IDs did not change.

`ItemIconPalette` shares one cached `GradeIcon` UI material per colored grade. Normal uses the default material without additional grade color.
Fine/rare/epic/legendary/mythic/transcendent use muted green/blue/purple/orange/red/gold backgrounds.
The shader replaces only low-luminance cool-colored canvas pixels; neutral dark steel and bright magical effects retain their source colors.
Transparent artwork is alpha-composited onto the same grade background, preserving its soft glow.
`ItemArt.Apply` applies the treatment in equipment, bag, gacha, rewards, item details and discovered codex entries.
Changing a reused icon back to normal or a placeholder resets the material. The shader supports UI stencil masks and RectMask2D clipping.
No atlas copies, GPU readbacks or per-item material allocations are used.

The item definition update exchanges all base effects and breakthrough trait packages of 적염도 and 폭풍도:
적염도 is legendary (attack +22%, critical chance +3%, speed +3), 폭풍도 is epic (HP +3%, attack +16%).
적염도 keeps its flame-set membership; 폭풍도 keeps its prior absence of a set. Grade-dependent costs and gacha membership follow the exchanged grades.

Validation: 41 targeted effect/trait tests passed. Full Core: 657 tests, 642 passed and the same 15 pre-existing balance failures.
UI C# and test sources compile successfully. Shader GPU compilation, actual background colors and mask rendering still require a Unity Editor/device check;
the cloud environment cannot execute those rendering tests. PNG alpha and all 377 catalog bounds/281 unique item regions were checked separately.

## Aura colors and distinct equipment effects

`elemental_equipment.png` is a 4×3 atlas replacing eleven equipment illustrations. Thunder-set pieces use cyan/white lightning; black iron uses neutral silver streaks; 자하검 uses ivory/gold light; 자운포 and 자운 신 use white mist/wind; 묵린패 uses jade smoke; 혼원주 uses amber cosmic mist; 천마심장 uses red/orange fire. The final atlas cell is unused.
Purple magical auras are reserved for mythic/transcendent Biroe equipment. Physical purple gemstones, cloth colors and the epic-grade background remain distinct from magical auras.

All 229 equipment definitions now have unique base-effect combinations and distinct displayed effect text. `GEAR_STAT_DIFFERENTIATION.csv` records 62 redistributed definitions. Starting training gear and mythic/transcendent effects are unchanged by this differentiation pass. IDs, grades and set membership are preserved.
The tuning guard weights HP/Qi/attack/defense percentage points and speed at 1, critical chance percentage points at 2, and critical-damage percentage points at 0.25. Each redistributed item retains its previous weighted total; this guards against blanket increases but does not establish equal combat performance for every build.

The 63 differentiation checks pass, including distinct numeric/displayed profiles and preserved weighted totals. The full suite retains the same 15 pre-existing failure names and messages. UI/test C# compilation succeeds; Unity rendering, shader execution and device appearance still require Editor/device verification.
