"""Compone las hojas de revision y exporta los sprites sueltos (PNG + mascara de brillo).

Uso:  python3 make_sheet.py            -> docs/art/personajes_v2.png y docs/art/sprites/*.png
"""
import os
import sys
import importlib

import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(ROOT, "docs", "art")
SPR = os.path.join(OUT, "sprites")
FONT = os.path.join(ROOT, "Assets", "_Abismo", "Fonts", "Jersey10-Regular.ttf")
sys.path.insert(0, HERE)


def font(size):
    try:
        return ImageFont.truetype(FONT, size)
    except Exception:
        return ImageFont.load_default()


def gradient_bg(w, h, top=(18, 30, 32), bottom=(4, 7, 9)):
    arr = np.zeros((h, w, 4), np.uint8)
    for y in range(h):
        t = y / max(1, h - 1)
        arr[y, :, :3] = [round(top[i] + (bottom[i] - top[i]) * t) for i in range(3)]
        arr[y, :, 3] = 255
    return Image.fromarray(arr, "RGBA")


def bbox(canvas):
    a = canvas.px[..., 3] > 0
    ys, xs = np.nonzero(a)
    return xs.min(), ys.min(), xs.max() + 1, ys.max() + 1


def export_sprite(name, canvas):
    os.makedirs(SPR, exist_ok=True)
    canvas.save(os.path.join(SPR, f"{name}.png"))
    glow = canvas.emit.copy()
    if (glow[..., 3] > 0).any():
        Image.fromarray(glow, "RGBA").save(os.path.join(SPR, f"{name}_brillo.png"))


def lineup(entries, scale=3, title=None, ground_pad=34, gap=26, float_dy=None):
    """entries: [(etiqueta, Canvas, flotante_px)] alineados por los pies."""
    float_dy = float_dy or {}
    boxes = [bbox(c) for _, c, _ in entries]
    widths = [(b[2] - b[0]) for b in boxes]
    heights = [(b[3] - b[1]) + fl for b, (_, _, fl) in zip(boxes, entries)]
    W = sum(widths) * scale + gap * (len(entries) + 1) * scale // 3 + 40
    Hh = (max(heights) + 30) * scale + 60
    img = gradient_bg(W, Hh)
    d = ImageDraw.Draw(img)
    ground_y = Hh - ground_pad - 12
    d.line([(20, ground_y), (W - 20, ground_y)], fill=(40, 58, 60, 255), width=2)
    x = 20 + gap * scale // 3
    f = font(22)
    for (label, c, fl), b, w in zip(entries, boxes, widths):
        crop_px = Image.fromarray(c.px, "RGBA").crop(b)
        crop_em = Image.fromarray(c.emit, "RGBA").crop(b)
        sw, sh = crop_px.width * scale, crop_px.height * scale
        px = crop_px.resize((sw, sh), Image.NEAREST)
        em = crop_em.resize((sw, sh), Image.NEAREST)
        y = ground_y - sh - fl * scale
        # halo del brillo (bloom aproximado)
        halo = Image.new("RGBA", (sw + 120, sh + 120), (0, 0, 0, 0))
        halo.alpha_composite(em, (60, 60))
        halo = halo.filter(ImageFilter.GaussianBlur(7 * scale / 3))
        img.alpha_composite(halo, (x - 60, y - 60))
        img.alpha_composite(halo, (x - 60, y - 60))
        img.alpha_composite(px, (x, y))
        tw = d.textlength(label, font=f)
        d.text((x + sw / 2 - tw / 2, ground_y + 12), label, font=f, fill=(210, 205, 190, 255))
        x += sw + gap * scale // 3
    if title:
        d.text((24, 12), title, font=font(26), fill=(190, 170, 110, 255))
    return img


def bestiary():
    import bestiario
    items = [("shoggoth", "Shoggoth menor", bestiario.build_shoggoth, 0),
             ("byakhee", "Byakhee", bestiario.build_byakhee, 12),
             ("migo", "Mi-Go", bestiario.build_migo, 0),
             ("hydra", "Sacerdotisa de Hydra", bestiario.build_hydra, 0)]
    entries = []
    for key, label, fn, fl in items:
        c = fn()
        export_sprite(f"{key}_v2", c)
        entries.append((label, c, fl))
    img = lineup(entries, scale=3, title="ABISMO · bestiario nuevo (escala del juego ×3)")
    path = os.path.join(OUT, "bestiario_nuevo.png")
    img.convert("RGB").save(path)
    print("sheet:", path, img.size)


def props_sheet():
    import props
    entries = []
    for label, fn, fl in props.PROPS:
        c = fn()
        export_sprite(f"prop_{fn.__name__}_v2", c)
        entries.append((label, c, fl))
    img = lineup(entries, scale=3, title="ABISMO · props y decorado v2 (escala del juego ×3)", gap=22)
    path = os.path.join(OUT, "props_v2.png")
    img.convert("RGB").save(path)
    print("sheet:", path, img.size)


def ui_export():
    import ui
    for name, cv in [("ui_medallon", ui.medallion()),
                     ("ui_barra_vida", ui.bar(186, 18, 0.72, ui.CRIM, ghost=0.80, gem=(210, 40, 56))),
                     ("ui_barra_revelacion", ui.bar(120, 13, 0.55, ui.ramp("#2fb8b0", 6, 0.08, 0.2, 1.4), glow_edge=(150, 255, 240), gem=(90, 235, 215))),
                     ("ui_barra_jefe", ui.boss_bar(0.72)),
                     ("ui_laudano_lleno", ui.flask(True)), ("ui_laudano_vacio", ui.flask(False)),
                     ("ui_marco_oro", ui.gold_frame(96, 22)), ("ui_divisor", ui.divider(200, 11)), ("ui_panel", ui.panel(210, 64))]:
        export_sprite(name, cv)
    ui.hud_mockup(boss="El Arcipreste de las Mareas").resize((1280, 720), Image.NEAREST).convert("RGB").save(os.path.join(OUT, "hud_v2.png"))
    ui.elements_sheet().convert("RGB").save(os.path.join(OUT, "ui_elementos_v2.png"))
    print("ui exportada")


def main():
    os.makedirs(SPR, exist_ok=True)
    mods = {
        "ahogado": ("El Ahogado", 0),
        "profundo": ("Profundo", 0),
        "sectario": ("Sectario de Dagón", 0),
        "ojo": ("Ojo del Vacío", 14),
        "arcipreste": ("El Arcipreste de las Mareas", 0),
    }
    entries = []
    for key, (label, fl) in mods.items():
        m = importlib.import_module(key)
        c = m.build()
        export_sprite(f"{key}_v2", c)
        entries.append((label, c, fl))
    img = lineup(entries, scale=3, title="ABISMO · personajes v2 (escala del juego ×3)")
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, "personajes_v2.png")
    img.convert("RGB").save(path)
    print("sheet:", path, img.size)
    bestiary()
    props_sheet()
    ui_export()


if __name__ == "__main__":
    main()
