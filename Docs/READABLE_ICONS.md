# Readable Icon Art

The painted wuxia icon set covers all 31 martial arts, 20 inner methods and 281 items currently returned by Core content, plus menu/command icons and eight item-kind defaults. Equipment grade remains a separate UI frame; the object silhouette, material and effects identify the item itself. Existing portrait, battle figure and background art is unchanged.

## Assets and mapping

`Assets/_Project/Resources/Art/Readable/catalog.json` maps each `(category, id)` to its own region in a shared PNG. `ArtLibrary` uses this catalog first and retains the previous per-file artwork as fallback. Categories are `Art`, `Inner`, `Item`, `Icon` and `DefaultItem`.

- `skills.png`: martial arts, inner methods, commands and main navigation, in an 8×8 grid.
- `items_0.png` through `items_4.png`: individual items and additional menu symbols, predominantly 8×8 grids.
- `special.png`: 4×2 sheet for two mythic weapons/accessories and all six polishing materials. Spirit crystals, silver ingots, ancient black ingots and the celestial meteor have distinct silhouettes.
- `details.png`: 4×2 sheet correcting selected item silhouettes, including the five-element saber, thunder wine flask, bear-gall medicine and thunder pendant.

`cell` is zero-based, counted left to right and top to bottom. `columns` and `rows` describe the sheet. Six material illustrations in the final row of `items_3.png` use explicit normalized `x`, `y`, `width`, `height`; `y` is measured from the top. These measured overrides and the remapped final sheet account for the generated art's actual layout. Do not regenerate ordering from array positions alone.

The loader insets each region by three pixels to exclude grid seams. Sheet dimensions remain unchanged on import, mipmaps are disabled and textures are compressed. Sprites and source textures are cached and shared; no texture copies or GPU readbacks are needed.

## UI integration and checks

Lists, inspectors, gacha and battle commands all use `ArtLibrary`. Inner-method cards use `ArtLibrary.InnerArt`. `WuxiaGlyph` renders painted symbols for mapped keys while preserving vector frames, stance markers and decorative halos. Changing its key invalidates both geometry and texture binding.

`ReadableIconTests` covers content coverage, distinct item regions, valid sprite bounds, shared lookup results, fallback items and dynamic glyph texture changes. Run these tests in Unity EditMode. Cloud validation checks all 377 mappings, image bounds and metadata, and compiles UI/test sources; it does not execute Unity rendering or touch input.

The PNG sheets were created with image generation and their ID mappings were inspected and corrected against the existing content. To add an item, assign a distinct, visually verified region and add the catalog entry; keep the legacy fallback for future content without painted art.
