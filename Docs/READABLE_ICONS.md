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

`cell` is zero-based, counted left to right and top to bottom. `columns` and `rows` describe the slicing grid for that entry. The same sheet may contain rows with different column counts and nonuniform pixel sizes; do not assume a single grid for the entire image. All item/default regions now use measured normalized rectangles; `cell` identifies the illustration only. See the boundary correction below. Normalized `x`, `y`, `width`, `height` describe the measured region; `y` is measured from the top. These measured overrides and the remapped final sheet account for the generated art's actual layout. Do not regenerate ordering from array positions alone.

The loader insets each region by three pixels to exclude grid seams. Sheet dimensions remain unchanged on import, mipmaps are disabled and textures are compressed. Sprites and source textures are cached and shared; no texture copies or GPU readbacks are needed.

## UI integration and checks

Lists, inspectors, gacha and battle commands all use `ArtLibrary`. Inner-method cards use `ArtLibrary.InnerArt`. `WuxiaGlyph` renders painted symbols for mapped keys while preserving vector frames, stance markers and decorative halos. Changing its key invalidates both geometry and texture binding.

`ReadableIconTests` covers content coverage, distinct item regions, valid sprite bounds, shared lookup results, fallback items and dynamic glyph texture changes. Run these tests in Unity EditMode. Cloud validation checks all 385 mappings, image bounds and metadata, and compiles UI/test sources; it does not execute Unity rendering or touch input.

The PNG sheets were created with image generation and their ID mappings were inspected and corrected against the existing content. To add an item, assign a distinct, visually verified region and add the catalog entry; keep the legacy fallback for future content without painted art.

## Equipment mapping audit (2026-10-23)

Reviewed all 281 item entries, including all 229 equipment entries, against the illustrated sheets.
62 equipment mappings were corrected. The wrong pictures came from layout differences within generated atlases:

- `items_0`: an extra weapon before the robes shifts the armor run by one; the final accessory row contains nine illustrations.
- `items_1`: an extra purple weapon before the normal weapons shifts subsequent weapons, armor and pants by one. The missing straw greaves and unclear leather wraps now have dedicated replacement pictures.
- `items_3`: the earlier nine-column assumption for the penultimate row was incorrect and is superseded by the pixel-boundary audit below. The row has eight illustrations; the final materials row has six.
- `equipment_repair`: repairs the two reported starter icons directly and replaces the coiled-wire picture of the iron fan with a recognizable folding fan.

The three non-Biroe mythic weapons use separate effects: 성락검 has platinum/gold stars and a comet trail, 참천도 has a vermilion/gold cutting crescent, 혼천검 has jade/turquoise flowing vortices. These are visual treatments, without new gameplay effects or item statistics. The Biroe armor and accessories retain their original purple lightning artwork; the later explicit weapon-icon exchange is described below.

`ITEM_ICON_AUDIT.csv` records every item's name, kind and resolved pixel rectangle (after the loader's three-pixel inset, with Y measured from the top). `corrected` marks mappings changed since the previous release (74 after the subsequent icon and aura updates); `verified` marks the remaining items. PNG artwork was inspected visually; automated checks verify all 385 catalog bounds, complete item coverage and zero overlapping item regions. These geometry checks complement, rather than replace, visual item identification.

Gacha grade previews now show five item cards per measured scrolling row, including a 104-pixel icon, name, effects and set. The grade and probability remain in the header. The close button stays outside the scrolling area. Pull results also use five columns, while gacha probabilities are unchanged.

Regression coverage adds known-cell assertions for shifted armor, nine-column accessories, normal weapons, the mythic orb and the replacement atlas. The grade-preview test covers all grades at 1440/1920/2400-pixel widths, row counts, card separation, icon/name separation and no inventory/currency changes. Unity UI tests require the Editor; cloud validation compiles their source only.

## Jin Biroedo and transparent icon canvases

The previous `jin_biroedo` region (`items_0`, cell 9) is now used by `biroe_blade`, as requested.
`jin_biroedo` uses its own transparent PNG, with the original paired-dagger composition based on the user's reference photograph; only the hanging chain was removed, without switching to crossed daggers.
Only the icon changed; the two weapons' effects and existing save IDs did not change.

`IconCutout` now shares one transparent-canvas UI material across all item grades and menu/command icons. The old grade-coloured backplates were removed at the user's request. The shader keys the dark teal/blue painted canvas into alpha while preserving neutral metal and bright effects; source alpha, UI stencil masks and RectMask2D clipping remain supported. Colour matching uses gamma-space values in both Unity colour spaces.
`ItemArt.Apply`, navigation images, semantic glyphs and battle commands/tooltips use the material. Martial-art and inner-method pictures, portraits and scenery keep the standard material; reused images reset when their content changes. Grade names and card frames still identify rarity. This is a rendering treatment: original painted atlas pixels stay intact and there are no GPU readbacks or per-item texture copies. Shader compilation and visual edge quality require Unity/device verification.

The item definition update exchanges all base effects and breakthrough trait packages of 적염도 and 폭풍도:
적염도 is legendary (attack +22%, critical chance +3%, speed +3), 폭풍도 is epic (HP +3%, attack +16%).
적염도 keeps its flame-set membership; 폭풍도 keeps its prior absence of a set. Grade-dependent costs and gacha membership follow the exchanged grades.

Validation: 41 targeted effect/trait tests passed. Full Core: 657 tests, 642 passed and the same 15 pre-existing balance failures.
UI C# and test sources compile successfully. Shader GPU compilation, alpha-key edge quality and mask rendering still require a Unity Editor/device check;
the cloud environment cannot execute those rendering tests. PNG alpha and all 385 catalog bounds/281 unique item regions were checked separately.

## Aura colors and distinct equipment effects

`elemental_equipment.png` is a 4×3 atlas replacing eleven equipment illustrations. Thunder-set pieces use cyan/white lightning; black iron uses neutral silver streaks; 자하검 uses ivory/gold light; 자운포 and 자운 신 use white mist/wind; 묵린패 uses jade smoke; 혼원주 uses amber cosmic mist; 천마심장 uses red/orange fire. The final atlas cell is unused.
Purple magical auras are reserved for mythic/transcendent Biroe equipment. Physical purple gemstones, cloth colors and the epic-grade background remain distinct from magical auras.

All 229 equipment definitions now have unique base-effect combinations and distinct displayed effect text. `GEAR_STAT_DIFFERENTIATION.csv` records 62 redistributed definitions. Starting training gear and mythic/transcendent effects are unchanged by this differentiation pass. IDs, grades and set membership are preserved.
The tuning guard weights HP/Qi/attack/defense percentage points and speed at 1, critical chance percentage points at 2, and critical-damage percentage points at 0.25. Each redistributed item retains its previous weighted total; this guards against blanket increases but does not establish equal combat performance for every build.

The 63 differentiation checks pass, including distinct numeric/displayed profiles and preserved weighted totals. The full suite retains the same 15 pre-existing failure names and messages. UI/test C# compilation succeeds; Unity rendering, shader execution and device appearance still require Editor/device verification.

## Compact equipment workspace

Equipment inventory uses seven cards per measured scrolling row with 88-pixel icons, names and worn/star states. Effects and set names for the selected item appear in the action panel below; full details remain accessible there. Six part selectors and the six worn slots both filter the list to the selected kind, with separate accessory destinations. Selecting a candidate never changes the chosen destination slot. Gold labels distinguish the selected part on the left from the selected item on the right, independently of rarity colors.

UI regression sources cover five-column single/ten/fifty pull results and grade previews, seven-column equipment rows, all six slot filters, selection markers, accessory destination retention and unchanged inventories during preview at 1440/1920/2400 widths. These sources compile in the cloud; executing their layout and input assertions requires Unity Editor.

## Record notices and separate menu symbols

`menus.png` adds eight distinct 4×2 regions: a single swordsman for companions, a lantern-lit inn for factions, an open encyclopedia, an achievement medal, a brush/scroll for stories, paired pendants for memories, a dagger altar for the ritual, and sealed bamboo slips/reward purse for missions. Party formation retains its group symbol, and the tower retains its tower symbol. Record menu entries no longer reuse those unrelated destination icons; the redundant companion/status entry is removed.

The bottom record tab now aggregates unread codex discoveries, newly accessible story recaps, new faction tiers, claimable achievements, unseen memories and the available ritual. `SaveData.ReadRecordUpdates` persists page acknowledgements. Opening the relevant codex/story/faction page acknowledges its current updates, while rewards and memory scenes remain pending until claimed/completed. Opening only the record tab does not clear notices. Fortune choice buttons retain their height and now have a 3% screen-height gap.

Validation: three new Core notice tests pass. Full Core: 660 tests, 645 pass, with the same 15 pre-existing failures. UI and regression sources compile; badge rendering, icon readability and touch spacing still need Unity Editor/device verification.

## Scene backgrounds

See `SCENERY_ART.md` for the ten new environment assets and the seven battle themes. The former `stage_01.png` and its repeated mountain wash have been removed. Scene images use centered UV cover cropping rather than nonuniform stretching; UI panels retain dark reading surfaces.

## Stage and character browsing

The stage selector uses two stage cards per measured row, preserving the shared difficulty picker, individual stage actions and progression locks. Inner-method lists use three cards per row with 96-pixel icons and measured text height. The companion screen no longer reserves an upper character strip or profile shortcuts to party/equipment; its larger workspace has a single profile-bottom character-change action. That action opens a scrollable three-column portrait picker containing the protagonist and joined companions. Selecting a person closes the picker, keeps the current status/arts/bonds page, and does not equip gear or change party deployment. Switching away closes the picker. Equipment uses the same joined-character picker from its underlined name/level header with a dropdown arrow. The upper character strip is removed and the freed height expands the equipment workspace. Changing the viewed person preserves the selected equipment destination (including accessory slot 2), refreshes hero-only filtering, and does not equip anything.

UI regression sources cover two-column stage availability, three-column inner methods, the character picker's available roster, cancel/selection behavior and page retention at several landscape widths. C# compilation passes; UI execution and touch checks require Unity Editor.

The shared `CharacterPicker` component keeps roster filtering, selected-person marking, portrait layout and dismissal identical in companion/equipment workspaces. Equipment picker regressions cover available characters, cancel, slot retention, hero-only filtering and no inventory/equipment changes at 1440/1920/2400 widths. Cloud validation compiles these sources; Unity Editor is required to execute UI checks.


## Illustration boundary correction (2026-10-24)

Reports of a second object next to 봉인 구슬 and below 한철검 exposed incorrect equal-grid assumptions. The previous geometry audit only proved rectangles were inside the image and did not overlap each other; it did not prove they followed the actual painted dividers.

All **281 item icons**, all eight kind defaults and 24 additional mapped icons now use explicit measured source rectangles (313 regions total). Source PNG artwork, item stats and rarity are unchanged. The shared catalog fixes gacha results/previews, equipment slots/lists, bag cards and inspectors together.

Notable repairs:

- `items_0` rows are not equal height: the sword row begins at Y=611, not 627. The old frost-sword crop included the next row's robe. Its bottom row has nine **unequal-width** cells, fixing jade rings, wooden charms, bells, pendants and boots.
- `items_1`'s final row starts at Y=1082; `items_2`'s at Y=1092. Both are measured instead of assumed to start at 1097.25.
- `items_3`'s legendary row is **eight columns**, fixing the sealed orb, dragon necklace, boots, pants and robes. Its materials row is six columns.
- `items_4`'s final status-symbol row is **seven unequal-width columns**, fixing stun, poison, inner wound, speed and attack symbols.

`Docs/ICON_SHEET_LAYOUTS.json` records visually inspected divider coordinates. `Tools/audit_icon_regions.py` validates the catalog against these dividers and PNG dimensions, checks complete CSV coverage and rejects overlapping item regions. Run:

```bash
python3 Tools/audit_icon_regions.py
# After deliberately updating inspected sheet boundaries:
python3 Tools/audit_icon_regions.py --write
```

`--write` refreshes normalized catalog rectangles and `ITEM_ICON_AUDIT.csv`; it never modifies PNGs. The loader keeps its existing three-pixel filtering inset. A new/replaced illustration sheet must have its divider layout inspected rather than inferred solely from the item count.

Validation: 385 catalog mappings pass geometry checks, including 313 measured regions and 281 distinct item regions. Browser-rendered source-region previews of all 281 items were inspected for neighboring fragments. UI regression sources pin 24 representative item rectangles and seven status-symbol rectangles, and check pairwise item-region separation. Game/UI and UI-test C# sources compile successfully. These cloud previews verify source slicing; Unity shader execution and device rendering still require Editor/device verification.
