#!/usr/bin/env python3
"""Validate painted atlas regions against measured illustration boundaries.

Run without arguments to check; --write refreshes normalized catalog rectangles
and the item audit CSV from Docs/ICON_SHEET_LAYOUTS.json. No PNGs are modified.
"""
import argparse
import csv
import json
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "Assets/_Project/Resources/Art/Readable"
INSET = 3


def dimensions(path):
    header = path.read_bytes()[:24]
    assert header[:8] == b"\x89PNG\r\n\x1a\n", path
    return struct.unpack(">II", header[16:24])


def region(entry, size):
    w, h = size
    if entry.get("width", 0) > 0 and entry.get("height", 0) > 0:
        x, y = entry["x"] * w, entry["y"] * h
        width, height = entry["width"] * w, entry["height"] * h
    else:
        width, height = w / entry["columns"], h / entry["rows"]
        x = entry["cell"] % entry["columns"] * width
        y = entry["cell"] // entry["columns"] * height
    return x + INSET, y + INSET, width - INSET * 2, height - INSET * 2


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--write", action="store_true")
    args = parser.parse_args()
    catalog_path = ART / "catalog.json"
    catalog = json.loads(catalog_path.read_text())
    layouts = json.loads((ROOT / "Docs/ICON_SHEET_LAYOUTS.json").read_text())["sheets"]
    bounds, sizes, changed = {}, {}, set()
    checked = 0
    for entry in catalog["entries"]:
        key = entry["category"], entry["id"]
        assert key not in bounds, ("Duplicate mapping", key)
        sheet = entry["sheet"]
        path = ART / (sheet + ".png")
        assert path.with_suffix(".png.meta").exists(), path
        if sheet not in sizes:
            sizes[sheet] = dimensions(path)
        w, h = sizes[sheet]
        layout = layouts.get(sheet)
        if layout:
            assert (w, h) == (layout["width"], layout["height"]), sheet
            row, col = divmod(entry["cell"], entry["columns"])
            ys = layout["y"]
            xs = layout.get("rowX", {}).get(str(row), layout["x"])
            assert entry["rows"] == len(ys) - 1, key
            assert entry["columns"] == len(xs) - 1, key
            x0, x1 = xs[col:col + 2]
            y0, y1 = ys[row:row + 2]
            expected = dict(x=x0 / w, y=y0 / h, width=(x1 - x0) / w, height=(y1 - y0) / h)
            if any(abs(entry.get(field, -1) - value) > 1e-9 for field, value in expected.items()):
                assert args.write, ("Region differs from illustration boundaries", key)
                entry.update(expected)
                changed.add(key)
            checked += 1
        elif entry["category"] in ("Item", "DefaultItem"):
            raise AssertionError(("Unmeasured item sheet", key, sheet))
        x, y, width, height = region(entry, (w, h))
        assert width > 0 and height > 0 and x >= 0 and y >= 0, key
        assert x + width <= w + .001 and y + height <= h + .001, key
        bounds[key] = sheet, x, y, width, height

    items = {key[1]: rect for key, rect in bounds.items() if key[0] == "Item"}
    for i, (id_a, rect_a) in enumerate(items.items()):
        sa, xa, ya, wa, ha = rect_a
        for id_b, rect_b in list(items.items())[i + 1:]:
            sb, xb, yb, wb, hb = rect_b
            assert sa != sb or min(xa + wa, xb + wb) <= max(xa, xb) + .001 or min(ya + ha, yb + hb) <= max(ya, yb) + .001, ("Overlapping items", id_a, id_b)

    audit_path = ROOT / "Docs/ITEM_ICON_AUDIT.csv"
    with audit_path.open(newline="") as source:
        reader = csv.DictReader(source)
        fields, rows = reader.fieldnames, list(reader)
    assert {row["id"] for row in rows} == set(items), "Audit does not cover catalog items"
    for row in rows:
        sheet, x, y, width, height = items[row["id"]]
        values = dict(sheet=sheet, x_from_left_px=round(x, 3), y_from_top_px=round(y, 3), width_px=round(width, 3), height_px=round(height, 3))
        if args.write:
            row.update(values)
            if ("Item", row["id"]) in changed:
                row["result"] = "corrected"
        else:
            for field, value in values.items():
                matches = row[field] == value if field == "sheet" else abs(float(row[field]) - value) < .001
                assert matches, ("Stale audit", row["id"], field)
    if args.write:
        catalog_path.write_text(json.dumps(catalog, ensure_ascii=False, indent=2) + "\n")
        with audit_path.open("w", newline="") as target:
            writer = csv.DictWriter(target, fieldnames=fields, lineterminator="\n")
            writer.writeheader()
            writer.writerows(rows)
    print(f"Checked {len(bounds)} mappings, {checked} measured regions, {len(items)} items; no overlapping item regions.")


if __name__ == "__main__":
    main()
