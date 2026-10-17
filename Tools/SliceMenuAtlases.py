"""Slice the reviewed character and menu atlases into transparent Unity resources.

Usage: python Tools/SliceMenuAtlases.py character-atlas.png menu-ui-atlas.png menu-background.png
The coordinates document this atlas revision; re-review them when regenerating art.
"""
from pathlib import Path
import sys
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
GENERATED = ROOT / "Assets/PKBall/Art/Generated"
ART = ROOT / "Assets/PKBall/Resources/Art"
UI = ROOT / "Assets/PKBall/Resources/UI"


def trim(image, bounds, pad=6):
    part = image.crop(bounds).convert("RGBA")
    alpha = part.getchannel("A").point(lambda value: 255 if value > 12 else 0)
    box = alpha.getbbox()
    if box is None:
        raise ValueError(f"Empty atlas cell: {bounds}")
    left, top, right, bottom = box
    return part.crop((max(0, left - pad), max(0, top - pad),
                      min(part.width, right + pad), min(part.height, bottom + pad)))


def save_cell(atlas, bounds, folder, name, square=False):
    part = trim(atlas, bounds)
    if square:
        side = max(part.size)
        tile = Image.new("RGBA", (side, side))
        tile.alpha_composite(part, ((side - part.width) // 2, (side - part.height) // 2))
        part = tile.resize((512, 512), Image.Resampling.LANCZOS)
    folder.mkdir(parents=True, exist_ok=True)
    part.save(folder / f"{name}.png")
    print(name, part.size)


def main():
    character_path, ui_path, background_path = map(Path, sys.argv[1:4])
    GENERATED.mkdir(parents=True, exist_ok=True)
    characters = Image.open(character_path).convert("RGBA")
    characters.save(GENERATED / "character-atlas.png")
    for name, bounds in [
        ("player_warrior", (0, 0, 495, 510)),
        ("player_mage", (510, 0, 1025, 510)),
        ("player_engineer", (1025, 0, 1536, 510)),
        ("monster_normal", (0, 510, 510, 1024)),
        ("monster_elite", (510, 510, 1025, 1024)),
        ("monster_boss", (1025, 510, 1536, 1024)),
    ]:
        save_cell(characters, bounds, ART, name, square=True)
    for alias, original in [("player", "player_warrior"),
                            ("rival", "player_engineer"),
                            ("monster", "monster_normal")]:
        (ART / f"{alias}.png").write_bytes((ART / f"{original}.png").read_bytes())

    ui = Image.open(ui_path).convert("RGBA")
    ui.save(GENERATED / "menu-ui-atlas.png")
    for name, bounds in [
        ("title_plaque", (0, 0, 680, 505)),
        ("match_banner", (675, 0, 1100, 510)),
        ("blue_banner", (1100, 0, 1536, 510)),
        ("green_banner", (0, 510, 470, 1024)),
        ("wood_button", (455, 520, 1080, 1000)),
        ("portrait_frame", (1080, 510, 1536, 1024)),
    ]:
        save_cell(ui, bounds, UI, name)
    background = Image.open(background_path).convert("RGB")
    background.save(GENERATED / "menu-background-source.png")
    background.resize((1920, 1080), Image.Resampling.LANCZOS).save(
        UI / "menu_background.png", optimize=True)


if __name__ == "__main__":
    main()
