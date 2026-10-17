"""Slice the generated 4x4 transparent atlas into Unity-ready item icons."""
from pathlib import Path
from PIL import Image
import sys

ATLAS = Path(sys.argv[1])
ROOT = Path(sys.argv[2])
PRIMARY_NAMES = (
    "spear", "crossbow", "pickaxe", "hammer",
    "rapier", "shield", "wheel", "armor",
    "ram", "core", "devourer", "health_lab",
    "strength_lab", "speed_lab", "injector", "break_core",
)
SECONDARY_NAMES = (
    "padding", "bulwark", "boots", "afterburner",
    "plating", "ram", "injector", "strength_injector",
    "speed_injector", "gold_ore", "greatsword", "halberd",
    "arc_cannon", "repulsor", "enchant_stone", "merchant_chest",
)
secondary = len(sys.argv) > 3 and sys.argv[3] == "secondary"
NAMES = SECONDARY_NAMES if secondary else PRIMARY_NAMES
source = Image.open(ATLAS).convert("RGBA")
if source.width != source.height:
    raise ValueError("Expected square atlas")
atlas_target = ROOT / "Art" / "Generated" / (
    "item-atlas-variants.png" if secondary else "item-atlas.png")
icons = ROOT / "Resources" / "Icons"
atlas_target.parent.mkdir(parents=True, exist_ok=True)
icons.mkdir(parents=True, exist_ok=True)
source.save(atlas_target)

for index, name in enumerate(NAMES):
    col, row = index % 4, index // 4
    x0 = round(col * source.width / 4)
    x1 = round((col + 1) * source.width / 4)
    y0 = round(row * source.height / 4)
    y1 = round((row + 1) * source.height / 4)
    cell = source.crop((x0, y0, x1, y1))
    bounds = cell.getchannel("A").getbbox()
    if bounds is None:
        raise ValueError(f"Empty atlas cell: {name}")
    cutout = cell.crop(bounds)
    cutout.thumbnail((232, 232), Image.Resampling.LANCZOS)
    target = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    target.alpha_composite(cutout, ((256 - cutout.width) // 2, (256 - cutout.height) // 2))
    target.save(icons / f"{name}.png")
print(f"Saved {len(NAMES)} transparent icons to {icons}")
