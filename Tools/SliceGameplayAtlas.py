"""Slice the reviewed 3x2 monster and gameplay UI atlas revision."""
from pathlib import Path
import sys
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
GENERATED = ROOT / "Assets/PKBall/Art/Generated"
ART = ROOT / "Assets/PKBall/Resources/Art"
UI = ROOT / "Assets/PKBall/Resources/UI"


def extract(source, box, destination, square=False):
    image = source.crop(box).convert("RGBA")
    mask = image.getchannel("A").point(lambda a: 255 if a > 12 else 0)
    bounds = mask.getbbox()
    if bounds is None:
        raise ValueError(f"Empty atlas cell {box}")
    image = image.crop(bounds)
    if square:
        side = max(image.size)
        tile = Image.new("RGBA", (side, side))
        tile.alpha_composite(image, ((side - image.width) // 2, (side - image.height) // 2))
        image = tile.resize((512, 512), Image.Resampling.LANCZOS)
    destination.parent.mkdir(parents=True, exist_ok=True)
    image.save(destination)
    print(destination.name, image.size)


def main():
    monster_source, ui_source = map(Path, sys.argv[1:3])
    GENERATED.mkdir(parents=True, exist_ok=True)
    monsters = Image.open(monster_source).convert("RGBA")
    monsters.save(GENERATED / "monster-variants-atlas.png")
    menu = Image.open(ui_source).convert("RGBA")
    menu.save(GENERATED / "gameplay-ui-atlas.png")
    boxes = [(0, 0, 512, 512), (512, 0, 1024, 512), (1024, 0, 1536, 512),
             (0, 512, 512, 1024), (512, 512, 1024, 1024), (1024, 512, 1536, 1024)]
    boxes[0] = (0, 0, 495, 512)  # Exclude a disconnected spark from the next cell.
    for name, box in zip(("monster_spore", "monster_spark", "monster_saw",
                          "monster_frost", "monster_furnace", "monster_storm"), boxes):
        extract(monsters, box, ART / f"{name}.png", True)
    ui_boxes = [(0, 0, 512, 360), (512, 0, 1024, 360), (1024, 0, 1536, 360),
                (0, 360, 450, 1024), (450, 360, 1130, 1024), (1130, 360, 1536, 1024)]
    for name, box in zip(("hud_plate", "health_frame", "energy_frame",
                          "monster_card", "inventory_panel", "tier_medal"), ui_boxes):
        extract(menu, box, UI / f"{name}.png")


if __name__ == "__main__":
    main()
