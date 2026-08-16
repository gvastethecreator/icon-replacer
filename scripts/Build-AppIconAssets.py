from __future__ import annotations

from pathlib import Path

try:
    from PIL import Image
except ImportError as error:
    raise SystemExit(
        "Pillow is required to regenerate official app-icon assets: python -m pip install Pillow"
    ) from error


REPO_ROOT = Path(__file__).resolve().parent.parent
SOURCE_ROOT = REPO_ROOT / "assets"
OUTPUT_ROOT = REPO_ROOT / "src" / "IconReplacer.App" / "Assets"
ICON_SIZES = (16, 20, 24, 32, 40, 48, 64, 128, 256)
DISPLAY_ICON_SIZE = (512, 512)
OBSOLETE_OUTPUT_NAMES = (
    "AppIcon.Light.ico",
    "AppIcon.Dark.ico",
    "AppIcon.Light.png",
    "AppIcon.Dark.png",
)


def load_source(name: str) -> Image.Image:
    path = SOURCE_ROOT / name
    if not path.is_file():
        raise SystemExit(f"Official app icon source is missing: {path}")

    image = Image.open(path).convert("RGBA")
    if min(image.size) < 256:
        raise SystemExit("Official app icons must be at least 256 pixels on each axis.")
    return image


def resized(source: Image.Image, size: tuple[int, int]) -> Image.Image:
    return source.resize(size, Image.Resampling.LANCZOS)


def squared(source: Image.Image) -> Image.Image:
    side = max(source.size)
    canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    canvas.alpha_composite(
        source,
        ((side - source.width) // 2, (side - source.height) // 2),
    )
    return canvas


def contained(
    source: Image.Image,
    size: tuple[int, int],
    padding: int,
) -> Image.Image:
    width, height = size
    available = (width - (padding * 2), height - (padding * 2))
    scale = min(available[0] / source.width, available[1] / source.height)
    content_size = (
        max(1, round(source.width * scale)),
        max(1, round(source.height * scale)),
    )
    content = resized(source, content_size)
    canvas = Image.new("RGBA", size, (0, 0, 0, 0))
    canvas.alpha_composite(
        content,
        ((width - content.width) // 2, (height - content.height) // 2),
    )
    content.close()
    return canvas


def save_png(source: Image.Image, name: str, size: tuple[int, int]) -> None:
    image = resized(source, size)
    try:
        image.save(OUTPUT_ROOT / name, format="PNG", optimize=True, compress_level=9)
    finally:
        image.close()


def save_contained_png(
    source: Image.Image,
    name: str,
    size: tuple[int, int],
    padding: int,
) -> None:
    image = contained(source, size, padding)
    try:
        image.save(OUTPUT_ROOT / name, format="PNG", optimize=True, compress_level=9)
    finally:
        image.close()


def save_ico(source: Image.Image, name: str) -> None:
    source.save(
        OUTPUT_ROOT / name,
        format="ICO",
        sizes=[(size, size) for size in ICON_SIZES],
        bitmap_format="png",
    )


def main() -> None:
    OUTPUT_ROOT.mkdir(parents=True, exist_ok=True)
    for name in OBSOLETE_OUTPUT_NAMES:
        (OUTPUT_ROOT / name).unlink(missing_ok=True)

    raw_source = load_source("icon.png")
    source = squared(raw_source)
    try:
        save_ico(source, "AppIcon.ico")
        save_png(source, "AppIcon.png", DISPLAY_ICON_SIZE)

        save_png(source, "Square150x150Logo.scale-200.png", (300, 300))
        save_png(source, "Square44x44Logo.scale-200.png", (88, 88))
        save_png(source, "StoreLogo.png", (50, 50))
        save_png(source, "LockScreenLogo.scale-200.png", (48, 48))
        save_png(source, "Square44x44Logo.targetsize-24_altform-unplated.png", (24, 24))
        save_png(source, "Square44x44Logo.targetsize-24_altform-lightunplated.png", (24, 24))
        save_png(source, "Square44x44Logo.targetsize-48_altform-unplated.png", (48, 48))
        save_png(source, "Square44x44Logo.targetsize-48_altform-lightunplated.png", (48, 48))
        save_contained_png(source, "SplashScreen.scale-200.png", (620, 300), 20)
        save_contained_png(source, "Wide310x150Logo.scale-200.png", (620, 300), 20)
    finally:
        raw_source.close()
        source.close()

    for path in sorted(OUTPUT_ROOT.iterdir()):
        if path.is_file() and path.name.startswith(
            ("AppIcon", "LockScreenLogo", "SplashScreen", "Square", "StoreLogo", "Wide")
        ):
            print(f"{path.name}|{path.stat().st_size}")


if __name__ == "__main__":
    main()
