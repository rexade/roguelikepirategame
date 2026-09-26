"""Generates the Drowned Sun UI textures (engraved-chart parchment look).

Run from the project root:  python tools/theme/generate_theme.py
Writes PNGs to Assets/_Game/UI/Theme/. Deterministic (fixed seeds), so re-running
only changes files when this script changes. Unity import settings for these files
are applied by Assets/_Game/UI/Theme/Editor/ThemeTextureImport.cs.
"""
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

OUT = os.path.join("Assets", "_Game", "UI", "Theme")

PARCHMENT = np.array([232, 220, 192], dtype=np.float32)   # #E8DCC0
PARCHMENT_DARK = np.array([206, 186, 146], dtype=np.float32)
FOXING = np.array([168, 128, 78], dtype=np.float32)
INK = (43, 33, 24)                                          # #2B2118
GOLD = (184, 137, 43)                                       # #B8892B
GOLD_LIGHT = (226, 184, 96)


def smooth_noise(w, h, scale, rng):
    """Value noise: random field blurred at `scale` pixels, normalised to 0..1."""
    small = rng.random((max(2, h // scale + 2), max(2, w // scale + 2))).astype(np.float32)
    img = Image.fromarray((small * 255).astype(np.uint8), "L").resize((w, h), Image.BICUBIC)
    a = np.asarray(img, dtype=np.float32) / 255.0
    return (a - a.min()) / max(1e-6, a.max() - a.min())


def parchment(w, h, seed, age=1.0, edge=0.55, fibers=True, spots=6):
    """RGB parchment: mottled fill, fibres, foxing spots and darker, burnt edges."""
    rng = np.random.default_rng(seed)
    mottled = 0.5 * smooth_noise(w, h, max(8, w // 6), rng) + 0.3 * smooth_noise(w, h, max(4, w // 24), rng) \
        + 0.2 * smooth_noise(w, h, 3, rng)
    tone = (mottled - 0.5) * 0.55 * age
    rgb = PARCHMENT[None, None, :] + tone[..., None] * (PARCHMENT - PARCHMENT_DARK)[None, None, :] * 2.2
    # Burnt, darker edges: distance to the nearest border, eased.
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    d = np.minimum(np.minimum(xx, w - 1 - xx) / w, np.minimum(yy, h - 1 - yy) / h)
    burn = np.clip(1.0 - d / 0.16, 0, 1) ** 2.2 * edge * age
    rgb = rgb * (1 - burn[..., None] * 0.38) + FOXING[None, None, :] * burn[..., None] * 0.18
    img = Image.fromarray(np.clip(rgb, 0, 255).astype(np.uint8), "RGB")
    draw = ImageDraw.Draw(img, "RGBA")
    if fibers:
        for _ in range(int(w * h / 900)):
            x, y = rng.random() * w, rng.random() * h
            length = 3 + rng.random() * 14
            angle = rng.random() * np.pi
            dx, dy = np.cos(angle) * length, np.sin(angle) * length
            shade = int(rng.integers(90, 150))
            draw.line([(x, y), (x + dx, y + dy)], fill=(shade, shade - 15, shade - 40, int(rng.integers(10, 26))), width=1)
    for _ in range(spots):
        # Foxing: soft brown blotches, drawn on a blurred layer.
        layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        ld = ImageDraw.Draw(layer)
        cx, cy = rng.random() * w, rng.random() * h
        r = (0.01 + rng.random() * 0.035) * min(w, h)
        ld.ellipse([cx - r, cy - r * 0.8, cx + r, cy + r * 0.8], fill=(150, 104, 52, int(20 + rng.random() * 35 * age)))
        layer = layer.filter(ImageFilter.GaussianBlur(r * 0.45))
        img.paste(layer, (0, 0), layer)
    return img


def diamond(draw, cx, cy, r, fill):
    draw.polygon([(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)], fill=fill)


def framed(img, outer, inner, line=2, corners=True, color=INK, gold=False):
    """Double engraved border: heavy outer rule, fine inner rule, corner diamonds."""
    w, h = img.size
    draw = ImageDraw.Draw(img, "RGBA")
    ink = color + (235,)
    draw.rectangle([outer, outer, w - 1 - outer, h - 1 - outer], outline=ink, width=line)
    draw.rectangle([inner, inner, w - 1 - inner, h - 1 - inner], outline=color + (190,), width=1)
    if corners:
        mid = (outer + inner) / 2.0
        for cx, cy in [(mid, mid), (w - 1 - mid, mid), (mid, h - 1 - mid), (w - 1 - mid, h - 1 - mid)]:
            diamond(draw, cx, cy, (inner - outer) * 0.75 + 1, (GOLD if gold else color) + (255,))
    return img


def with_alpha_edges(img, cut):
    """Transparent 45-degree cut corners, so panels read as cut sheets, not boxes."""
    rgba = img.convert("RGBA")
    w, h = rgba.size
    mask = Image.new("L", (w, h), 255)
    md = ImageDraw.Draw(mask)
    for pts in ([(0, 0), (cut, 0), (0, cut)], [(w, 0), (w - cut, 0), (w, cut)],
                [(0, h), (cut, h), (0, h - cut)], [(w, h), (w - cut, h), (w, h - cut)]):
        md.polygon(pts, fill=0)
    rgba.putalpha(mask)
    return rgba


def save(img, name):
    path = os.path.join(OUT, name)
    img.save(path, optimize=True)
    print("wrote", path, img.size)


def panel():
    # Cards, ledger and chart frame: 9-slice 48 px.
    img = parchment(512, 512, seed=11)
    framed(img, outer=10, inner=17, line=3, gold=True)
    save(with_alpha_edges(img, 7), "parchment-panel.png")


def tag():
    # Small HUD tags: 9-slice 20 px, single rule, lighter ageing.
    img = parchment(256, 128, seed=23, age=0.7, edge=0.45, spots=2)
    draw = ImageDraw.Draw(img, "RGBA")
    draw.rectangle([5, 5, 250, 122], outline=INK + (220,), width=2)
    draw.rectangle([9, 9, 246, 118], outline=INK + (110,), width=1)
    save(with_alpha_edges(img, 4), "parchment-tag.png")


def button(name, top, bottom, rule, seed):
    # Buttons: vertical wash, engraved rule; 9-slice 14 px.
    w, h = 192, 64
    base = parchment(w, h, seed=seed, age=0.4, edge=0.2, spots=0, fibers=False)
    grad = np.linspace(0, 1, h, dtype=np.float32)[:, None, None]
    wash = np.array(top, np.float32)[None, None, :] * (1 - grad) + np.array(bottom, np.float32)[None, None, :] * grad
    arr = np.asarray(base, np.float32) * 0.35 + wash * 0.65
    img = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGB")
    draw = ImageDraw.Draw(img, "RGBA")
    draw.rectangle([1, 1, w - 2, h - 2], outline=rule + (240,), width=2)
    draw.line([(6, 5), (w - 7, 5)], fill=(255, 250, 230, 60), width=1)
    save(with_alpha_edges(img, 5), name)


def chart():
    # Sea chart paper: large, older, with fold creases.
    w = h = 1024
    img = parchment(w, h, seed=37, age=1.25, edge=0.75, spots=14)
    arr = np.asarray(img, np.float32)
    idx = np.arange(w, dtype=np.float32)
    # A fold: faint shadow on one side of the crease, faint highlight on the other.
    fold = -0.07 * np.exp(-((idx - w / 2 + 1.5) ** 2) / 4.0) + 0.05 * np.exp(-((idx - w / 2 - 2.5) ** 2) / 9.0)
    arr *= (1 + fold)[None, :, None]            # vertical fold
    arr *= (1 + fold)[:, None, None]            # horizontal fold
    img = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGB")
    framed(img, outer=14, inner=24, line=4, gold=True)
    save(with_alpha_edges(img, 10), "chart-paper.png")


def hatch():
    # Tiling diagonal hatching (ink on transparent) for meters and shading.
    size = 16
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    for k in range(-size, size * 2, 4):
        draw.line([(k, size), (k + size, 0)], fill=INK + (120,), width=1)
    save(img, "hatch.png")


def vignette():
    # Modal overlay: deep-sea blue, clear centre, dark corners.
    w, h = 256, 256
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    r = np.sqrt(((xx - w / 2) / (w / 2)) ** 2 + ((yy - h / 2) / (h / 2)) ** 2)
    alpha = np.clip((r - 0.35) / 0.9, 0, 1) ** 1.6 * 200 + 70
    rgba = np.zeros((h, w, 4), np.uint8)
    rgba[..., 0], rgba[..., 1], rgba[..., 2] = 8, 26, 36
    rgba[..., 3] = np.clip(alpha, 0, 255).astype(np.uint8)
    save(Image.fromarray(rgba, "RGBA"), "vignette.png")


def banner():
    # Inscription banner: a long strip that fades to transparent at both ends.
    w, h = 1024, 128
    img = parchment(w, h, seed=53, age=0.8, edge=0.3, spots=3).convert("RGBA")
    draw = ImageDraw.Draw(img, "RGBA")
    draw.line([(40, 12), (w - 41, 12)], fill=GOLD + (230,), width=2)
    draw.line([(40, h - 13), (w - 41, h - 13)], fill=GOLD + (230,), width=2)
    draw.line([(60, 18), (w - 61, 18)], fill=INK + (120,), width=1)
    draw.line([(60, h - 19), (w - 61, h - 19)], fill=INK + (120,), width=1)
    xs = np.arange(w, dtype=np.float32)
    fade = np.clip(np.minimum(xs, w - 1 - xs) / 150.0, 0, 1) ** 1.4
    a = np.asarray(img, np.uint8).copy()
    a[..., 3] = (a[..., 3].astype(np.float32) * fade[None, :]).astype(np.uint8)
    save(Image.fromarray(a, "RGBA"), "banner.png")


def preview_backdrop():
    # Stand-in vista for the UI preview player only (the game shows the live world):
    # a bright lagoon with a hazy horizon, the hardest background for HUD contrast.
    w, h, horizon = 1920, 1080, 430
    rng = np.random.default_rng(71)
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    sky_t = np.clip(yy / horizon, 0, 1)[..., None]
    top, mid, low = np.array([38, 92, 150], np.float32), np.array([122, 178, 208], np.float32), np.array([246, 224, 176], np.float32)
    sky = np.where(sky_t < 0.6, top + (mid - top) * (sky_t / 0.6), mid + (low - mid) * ((sky_t - 0.6) / 0.4))
    sea_t = np.clip((yy - horizon) / (h - horizon), 0, 1)[..., None]
    deep, shallow = np.array([28, 84, 122], np.float32), np.array([52, 196, 188], np.float32)
    sea = deep + (shallow - deep) * np.sqrt(sea_t)
    bands = 0.5 + 0.5 * np.sin(yy / 23.0 + 2.0 * smooth_noise(w, h, 90, rng) * 6.0)
    sea = sea + (bands[..., None] - 0.5) * 18 * sea_t
    img = np.where((yy < horizon)[..., None], sky, sea)
    # Sun glow low in the west.
    glow = np.exp(-(((xx - 1450) / 520) ** 2 + ((yy - 390) / 260) ** 2))[..., None]
    img = img + glow * np.array([70, 48, 12], np.float32)
    base = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), "RGB")
    draw = ImageDraw.Draw(base, "RGBA")
    haze = (104, 128, 150, 235)
    # A half-sunken colossus head on the horizon, and the Sun Gate beyond it.
    draw.polygon([(330, horizon), (338, 330), (352, 300), (348, 262), (372, 250), (410, 252), (430, 280),
                  (436, 322), (448, 352), (440, 382), (452, horizon)], fill=haze)
    for x in (1010, 1060):
        draw.rectangle([x, 352, x + 12, horizon], fill=(120, 142, 162, 230))
    draw.line([(1002, 350), (1040, 347)], fill=(120, 142, 162, 230), width=7)
    # Palm cays.
    for cx, scale in ((700, 1.0), (1620, 0.7)):
        draw.ellipse([cx - 90 * scale, horizon - 10 * scale, cx + 90 * scale, horizon + 8 * scale], fill=(92, 128, 108, 255))
        for k in range(3):
            tx, ty = cx - 30 * scale + k * 32 * scale, horizon - 8 * scale
            top_x, top_y = tx + (k - 1) * 10 * scale, ty - (70 + 12 * k) * scale
            draw.line([(tx, ty), (top_x, top_y)], fill=(70, 96, 84, 255), width=int(5 * scale))
            for a in range(6):
                ang = a * np.pi / 3 + 0.3
                draw.line([(top_x, top_y), (top_x + np.cos(ang) * 38 * scale, top_y + np.sin(ang) * 14 * scale + 10 * scale)],
                          fill=(64, 110, 80, 255), width=int(4 * scale))
    # Pale ruins below the surface in the foreground shallows.
    for i in range(7):
        x = 250 + i * 190 + rng.random() * 40
        y = 780 + (i % 3) * 60
        draw.ellipse([x - 26, y - 9, x + 26, y + 9], fill=(214, 236, 214, 70))
        draw.rectangle([x - 18, y - 4, x + 18, y + 90], fill=(200, 232, 214, 38))
    save(base.filter(ImageFilter.GaussianBlur(1.2)), os.path.join("..", "Preview", "preview-backdrop.png"))


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    panel()
    tag()
    button("button.png", (226, 212, 178), (201, 183, 142), INK, 61)
    button("button-hover.png", (238, 226, 194), (214, 196, 154), INK, 62)
    button("button-primary.png", (226, 184, 96), (168, 120, 36), (92, 60, 16), 63)
    chart()
    hatch()
    vignette()
    banner()
    preview_backdrop()
