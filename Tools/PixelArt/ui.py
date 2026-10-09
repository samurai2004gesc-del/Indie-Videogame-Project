"""Interfaz v2 de ABISMO: medallon del Ahogado, barras de vida y Revelacion, frascos de laudano, contador de oro,
divisor de titulos, barra del jefe y marco de panel. `hud_mockup()` compone un HUD completo a 640x360."""
import math
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from pxl import *

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
FONT_TITLE = os.path.join(ROOT, "Assets", "_Abismo", "Fonts", "Jacquard24-Regular.ttf")
FONT_TEXT = os.path.join(ROOT, "Assets", "_Abismo", "Fonts", "Jersey10-Regular.ttf")

GOLD = ramp("#b08a2c", 6, 0.12, 0.16, 1.60)
GOLD_DK = ramp("#6a4e1e", 5, 0.10, 0.22, 1.35)
DARK = (9, 12, 16)
DARK2 = (16, 22, 28)
TEAL = (110, 250, 230)
TEAL_MID = (34, 170, 168)
TEAL_DK = (10, 70, 82)
CRIM = ramp("#8e1f2b", 6, 0.10, 0.20, 1.50)


def _rect(c, x0, y0, x1, y1, col, a=255):
    c.px[int(y0):int(y1), int(x0):int(x1)] = (*col[:3], a)


def _star_pts(cx, cy, r):
    return [(cx + math.cos(-math.pi / 2 + k * 2 * math.pi / 5) * r, cy + math.sin(-math.pi / 2 + k * 2 * math.pi / 5) * r) for k in range(5)]


def _bevel_frame(c, x0, y0, x1, y1, t=2):
    """Marco biselado de oro: luz arriba-izquierda, sombra abajo-derecha, contorno oscuro."""
    _rect(c, x0, y0, x1, y1, DARK)
    for i in range(t):
        _rect(c, x0 + 1 + i, y0 + 1 + i, x1 - 1 - i, y0 + 2 + i, GOLD[4 if i == 0 else 3])        # arriba
        _rect(c, x0 + 1 + i, y0 + 1 + i, x0 + 2 + i, y1 - 1 - i, GOLD[4 if i == 0 else 3])        # izquierda
        _rect(c, x0 + 1 + i, y1 - 2 - i, x1 - 1 - i, y1 - 1 - i, GOLD[1 if i == 0 else 2])        # abajo
        _rect(c, x1 - 2 - i, y0 + 1 + i, x1 - 1 - i, y1 - 1 - i, GOLD[1 if i == 0 else 2])        # derecha


def _rivet(c, x, y):
    c.set(x, y, GOLD[5]); c.set(x + 1, y, GOLD[3]); c.set(x, y + 1, GOLD[2]); c.set(x + 1, y + 1, GOLD[1])


# ----------------------------------------------------------------------------------------------
def medallion():
    W = H = 64
    c = Canvas(W, H)
    c.set_light(-0.6, 0.7, 0.45)
    cx = cy = 32
    # aro exterior de laton con 8 tornillos y 4 puntas
    ro, _ = ellipse(W, H, cx, cy, 29, 29)
    ri, _ = ellipse(W, H, cx, cy, 22, 22)
    c.shade(Shape(ro & ~ri, np.ones((H, W), np.float32) * 0.9, 3.6), GOLD, outline=True, contrast=1.1, bias=-0.05)
    for k in range(4):
        a = k * math.pi / 2
        px, py = cx + math.cos(a) * 29, cy + math.sin(a) * 29
        c.shade(polygon(W, H, [(px + math.cos(a) * 3, py + math.sin(a) * 3), (px - math.sin(a) * 2.4, py + math.cos(a) * 2.4),
                               (px + math.sin(a) * 2.4, py - math.cos(a) * 2.4)], bevel=1.0), GOLD, outline=True)
    for k in range(8):
        a = k * math.pi / 4 + math.pi / 8
        _rivet(c, round(cx + math.cos(a) * 25.5) - 1, round(cy + math.sin(a) * 25.5) - 1)
    # cristal interior oscuro con degradado
    glass = ellipse(W, H, cx, cy, 21.5, 21.5)
    c.shade(glass, [(5, 12, 16), (8, 20, 26), (12, 30, 36), (18, 44, 50)], outline=True, contrast=0.9, bias=0.05)
    # retrato: la escafandra del Ahogado de perfil
    brass = ramp("#8f6a26", 6, 0.12, 0.16, 1.62)
    coat = ramp("#2a4f57", 5, 0.10, 0.22, 1.30)
    c.shade(polygon(W, H, [(14, 52), (50, 52), (48, 44), (40, 40), (24, 40), (16, 44)], bevel=4), coat, outline=True, rim=(110, 255, 235), rim_amt=0.2)
    c.shade(ellipse(W, H, 31, 41, 12, 3.4), brass, outline=True, contrast=1.1, bias=-0.1)
    dome = ellipse(W, H, 31, 28, 12.5, 12.8)
    c.shade(dome, brass, outline=True, contrast=1.25, ambient=0.1, bias=-0.16, dither=0.3, rim=(240, 215, 150), rim_amt=0.3)
    for (x, y) in [(24, 20), (25, 19), (24, 21), (26, 19)]:
        c.set(x, y, brass[5])
    for x in range(20, 43):
        yy = round(34.4 - ((x - 31) / 12.5) ** 2 * 1.6)
        c.set(x, yy, brass[1]); c.set(x, yy - 1, brass[4] if x % 3 == 0 else brass[3])
    c.shade(polygon(W, H, [(29, 15), (34, 15), (33, 12), (30, 12)], bevel=1), brass, outline=True)
    ring_o, _ = ellipse(W, H, 31.5, 10.4, 3.2, 3.2)
    ring_i, _ = ellipse(W, H, 31.5, 10.4, 1.5, 1.5)
    c.shade(Shape(ring_o & ~ring_i, np.ones((H, W), np.float32) * 0.8, 1.6), brass, outline=False, bias=0.1)
    # ojo de buey brillante
    c.shade(ellipse(W, H, 38.5, 28, 6.2, 7.0), brass, outline=True, rim=(240, 215, 150), rim_amt=0.3)
    glow = ellipse(W, H, 38.8, 28, 4.2, 5.0)
    c.shade(glow, [(8, 52, 66), (16, 100, 112), (34, 170, 168), (92, 232, 214), (170, 255, 240)], outline=False, dither=0.4)
    c.set(36, 25, (240, 255, 250)); c.set(37, 25, (215, 255, 246)); c.set(36, 26, (195, 250, 240))
    ys, xs = np.nonzero(glow[0])
    for x, y in zip(xs, ys):
        if c.px[y, x, 1] > 140:
            c.emit[y, x] = (*c.px[y, x, :3], 255)
    # tornillos del casco
    for (x, y) in [(22, 30), (25, 35), (28, 36)]:
        c.set(x, y, brass[5])
    # reflejo del cristal (brillo diagonal)
    for i in range(10):
        c.set(14 + i, 12 + i // 2 - 0, (190, 230, 235)) if False else None
    for (x, y) in [(15, 20), (16, 18), (17, 16), (19, 14)]:
        c.set(x, y, (140, 190, 200)); c.set(x + 1, y, (100, 150, 160))
    return c


# ----------------------------------------------------------------------------------------------
def bar(w, h, ratio, fill_ramp, ghost=None, tick=10, glow_edge=None, gem=None):
    """Barra gotica: marco de oro con dientes ornamentales, remates en flecha con gema y relleno segmentado."""
    c = Canvas(w, h)
    c.set_light(-0.6, 0.7, 0.45)
    CAP = 11
    x0, x1 = CAP, w - CAP
    # hueco oscuro
    _rect(c, x0, 2, x1, h - 2, DARK)
    _rect(c, x0 + 1, 3, x1 - 1, h - 3, DARK2)
    inner_w = x1 - x0 - 2
    # parte "fantasma" (dano reciente)
    if ghost is not None and ghost > ratio:
        gw = int(inner_w * ghost)
        _rect(c, x0 + 1, 3, x0 + 1 + gw, h - 3, (226, 214, 190))
        _rect(c, x0 + 1, h - 4, x0 + 1 + gw, h - 3, (150, 135, 120))
    fw = int(inner_w * ratio)
    if fw > 0:
        rows = h - 6
        for i in range(rows):
            t = i / max(1, rows - 1)
            col = fill_ramp[5] if i == 0 and len(fill_ramp) > 5 else (fill_ramp[4] if i == 0 else (fill_ramp[3] if t < 0.35 else (fill_ramp[2] if t < 0.7 else fill_ramp[1])))
            _rect(c, x0 + 1, 3 + i, x0 + 1 + fw, 4 + i, col)
        if tick:
            for x in range(x0 + 1 + tick, x0 + 1 + fw, tick):
                _rect(c, x, 3, x + 1, h - 3, fill_ramp[1])
        if glow_edge:
            for i in range(h - 6):
                c.glow_set(x0 + fw, 3 + i, glow_edge)
    # filetes de oro con dientes ornamentales
    _rect(c, x0, 0, x1, 1, GOLD[5]); _rect(c, x0, 1, x1, 2, GOLD[3])
    _rect(c, x0, h - 2, x1, h - 1, GOLD[2]); _rect(c, x0, h - 1, x1, h, GOLD_DK[2])
    for x in range(x0 + 6, x1 - 4, 8):
        c.set(x, 0, GOLD_DK[1]); c.set(x + 1, 0, GOLD_DK[1]); c.set(x, h - 1, GOLD_DK[0]); c.set(x + 1, h - 1, GOLD_DK[0])
        c.set(x, 2, GOLD[4])
    # remates en flecha con gema
    for side in (0, 1):
        sx = 1 if side == 0 else -1
        bx = 0 if side == 0 else w - 1
        pts = [(bx, h / 2), (bx + sx * 4, 0), (bx + sx * (CAP + 1), 0), (bx + sx * (CAP + 1), h), (bx + sx * 4, h)]
        c.shade(polygon(w, h, pts, bevel=2.6), GOLD, outline=True, contrast=1.1, bias=-0.04)
        # aletas que sobresalen arriba y abajo
        for dy, ln in ((1, 3), (h - 2, 3)):
            c.line((bx + sx * 5, dy), (bx + sx * 8, dy + (-2 if dy < h / 2 else 2)), GOLD[4])
        gx = bx + sx * 5
        gem_col = gem or (210, 40, 56)
        c.disc(gx, h / 2, 2.2, DARK)
        c.disc(gx, h / 2, 1.5, gem_col, glow=True)
        c.set(gx - 1, h / 2 - 1, (255, 236, 220))
    return c


def flask(full=True):
    W, H = 16, 22
    c = Canvas(W, H)
    c.set_light(-0.6, 0.7, 0.45)
    glass = [(20, 28, 34), (36, 52, 60), (70, 98, 108), (140, 180, 190)]
    body = polygon(W, H, [(4, 9), (12, 9), (14, 14), (13, 19), (10, 21), (6, 21), (3, 19), (2, 14)], bevel=3.0)
    c.shade(body, glass, outline=True, contrast=1.0)
    if full:
        liq = polygon(W, H, [(3, 12), (13, 12), (13, 19), (10, 20), (6, 20), (3, 19)], bevel=2.0)
        c.shade(liq, [(70, 12, 20), (130, 24, 32), (186, 44, 44), (230, 90, 70)], outline=False, dither=0.0, bias=0.05)
        c.set(5, 14, (255, 190, 160)); c.set(5, 15, (240, 150, 120)); c.set(11, 18, (255, 120, 100))
    else:
        c.set(5, 15, (110, 120, 130)); c.set(5, 14, (130, 140, 150))
    # cuello y corcho
    c.shade(rect(W, H, 6, 5, 10, 10, 1.0), glass, outline=True)
    c.shade(rect(W, H, 5, 2, 11, 6, 1.2), ramp("#8a6a46", 4, 0.08, 0.3, 1.3), outline=True)
    c.hline(5, 10, 3, (176, 140, 98))
    c.set(4, 9, GOLD[4]); c.set(11, 9, GOLD[3])
    return c


def coin_icon():
    W = H = 12
    c = Canvas(W, H)
    c.shade(ellipse(W, H, 6, 6, 5, 5), ramp("#c29a34", 6, 0.12, 0.16, 1.65), outline=True, contrast=1.1, bias=-0.04)
    pts = _star_pts(6, 6, 2.6)
    for k in range(5):
        c.line(pts[k], pts[(k + 2) % 5], GOLD[5])
    c.set(3, 3, (255, 246, 205))
    return c


def gold_frame(w=92, h=22):
    c = Canvas(w, h)
    _bevel_frame(c, 0, 0, w, h, 2)
    _rect(c, 4, 4, w - 4, h - 4, DARK2)
    # esquinas con remaches
    for (x, y) in [(2, 2), (w - 4, 2), (2, h - 4), (w - 4, h - 4)]:
        _rivet(c, x, y)
    # moneda a la izquierda
    c.blit(coin_icon(), 6, (h - 12) // 2)
    return c


def divider(w=180, h=11):
    """Filete con estrella central y extremos que se afinan, para los titulos de zona."""
    c = Canvas(w, h)
    cx, cy = w // 2, h // 2
    for side in (-1, 1):
        for i in range(0, cx - 8):
            x = cx + side * (8 + i)
            t = i / (cx - 8)
            a = int(255 * (1 - t ** 1.6))
            if a < 12:
                continue
            c.set(x, cy, GOLD[4] if i % 6 else GOLD[5], a)
            if t < 0.55:
                c.set(x, cy + 1, GOLD_DK[2], a)
        for k in range(1, 4):
            x = cx + side * (10 + k * 14)
            c.set(x, cy - 2, GOLD[3]); c.set(x, cy - 1, GOLD[4]); c.set(x, cy + 2, GOLD[2]) if False else None
    pts = _star_pts(cx, cy, 5)
    for k in range(5):
        c.line(pts[k], pts[(k + 2) % 5], GOLD[5])
    c.disc(cx, cy, 1.4, TEAL_MID, glow=True)
    c.glow_set(cx, cy, (200, 255, 245))
    return c


def panel(w=160, h=96):
    """Marco gotico de panel (dialogos / pausa): oro con esquinas en aleta y fondo semitransparente."""
    c = Canvas(w, h)
    _rect(c, 3, 3, w - 3, h - 3, (8, 11, 15), 232)
    _bevel_frame(c, 0, 0, w, h, 2)
    _rect(c, 3, 3, w - 3, h - 3, (8, 11, 15), 232)
    for (x, y, sx, sy) in [(0, 0, 1, 1), (w, 0, -1, 1), (0, h, 1, -1), (w, h, -1, -1)]:
        # esquina: tres puntas en abanico
        for k in range(3):
            c.line((x + sx * 3, y + sy * 3), (x + sx * (12 + k * 4), y + sy * (3 + k * 5)), GOLD[4 - k], 255)
            c.line((x + sx * 3, y + sy * 3), (x + sx * (3 + k * 5), y + sy * (12 + k * 4)), GOLD[4 - k], 255)
        _rivet(c, x + sx * 5 - (1 if sx < 0 else 0), y + sy * 5 - (1 if sy < 0 else 0))
    # filete interior
    c.hline(8, w - 9, 5, GOLD_DK[3], 160); c.hline(8, w - 9, h - 6, GOLD_DK[1], 160)
    c.vline(5, 8, h - 9, GOLD_DK[3], 160); c.vline(w - 6, 8, h - 9, GOLD_DK[1], 160)
    return c


def boss_bar(ratio=0.72):
    c = bar(300, 16, ratio, CRIM, ghost=min(1.0, ratio + 0.08), tick=15, gem=(210, 40, 56))
    # remate central con el Signo
    pts = _star_pts(150, 0, 0)
    return c


# ----------------------------------------------------------------------------------------------
def _text(img, xy, text, font_path, size, fill, anchor="la", shadow=(6, 8, 12)):
    d = ImageDraw.Draw(img)
    d.fontmode = "1"                     # sin suavizado: texto pixel
    f = ImageFont.truetype(font_path, size)
    x, y = xy
    if shadow:
        d.text((x + 1, y + 1), text, font=f, fill=(*shadow, 255), anchor=anchor)
    d.text((x, y), text, font=f, fill=(*fill, 255), anchor=anchor)


def hud_mockup(zone="Costa de Innsmouth", boss=None, bg=None, with_extras=True):
    W, H = 640, 360
    custom = bg
    bg = Image.new("RGBA", (W, H), (10, 16, 18, 255))
    arr = np.array(bg)
    ys = np.linspace(0, 1, H)[:, None]
    arr[..., 0] = (14 - ys * 8).astype(np.uint8)
    arr[..., 1] = (26 - ys * 14).astype(np.uint8)
    arr[..., 2] = (28 - ys * 14).astype(np.uint8)
    # luna palida y niebla suave para juzgar la legibilidad sobre un fondo real
    cx, cy = 210, 98
    yy, xx = np.mgrid[0:H, 0:W]
    d = np.hypot(xx - cx, yy - cy)
    moon = np.clip(1 - d / 38, 0, 1) ** 0.5
    halo = np.clip(1 - d / 120, 0, 1) ** 2
    for k, v in enumerate((150, 190, 140)):
        arr[..., k] = np.clip(arr[..., k] + halo * (30, 44, 36)[k] + (moon > 0.2) * moon * v * 0.8, 0, 255).astype(np.uint8)
    img = custom.copy() if custom is not None else Image.fromarray(arr, "RGBA")

    def paste(canvas, x, y):
        em = Image.fromarray(canvas.emit, "RGBA")
        from PIL import ImageFilter
        halo_i = em.filter(ImageFilter.GaussianBlur(3))
        img.alpha_composite(halo_i, (x, y))
        img.alpha_composite(canvas.to_image(), (x, y))

    paste(medallion(), 8, 6)
    paste(bar(186, 18, 0.72, CRIM, ghost=0.80, gem=(210, 40, 56)), 68, 11)
    paste(bar(120, 13, 0.55, ramp("#2fb8b0", 6, 0.08, 0.2, 1.4), glow_edge=(150, 255, 240), gem=(90, 235, 215)), 70, 32)
    for i, full in enumerate((True, True, False)):
        paste(flask(full), 78 + i * 18, 48)
    gf = gold_frame(96, 22)
    paste(gf, 536, 8)
    _text(img, (628, 19), "1250", FONT_TEXT, 16, (216, 184, 98), anchor="rm")
    if with_extras:
        paste(divider(200, 11), 220, 110)
        _text(img, (320, 133), zone, FONT_TITLE, 26, (232, 226, 206), anchor="mm")
        paste(divider(200, 11), 220, 150)
    if boss:
        bb = boss_bar(0.72)
        paste(bb, 170, 326)
        _text(img, (320, 312), boss, FONT_TITLE, 22, (232, 226, 206), anchor="mm")
    # panel de ejemplo (dialogo / lectura) abajo
    if with_extras:
        pn = panel(210, 64)
        paste(pn, 215, 232)
        _text(img, (320, 252), "El mar recuerda lo que olvidas.", FONT_TEXT, 16, (200, 214, 206), anchor="mm")
        _text(img, (320, 270), "Pulsa E para rezar ante el Signo", FONT_TEXT, 14, (124, 190, 168), anchor="mm")
    return img


def elements_sheet():
    """Todas las piezas sueltas, ampliadas, para revisar el detalle."""
    items = [("Medallón", medallion()), ("Barra de vida", bar(186, 18, 0.72, CRIM, ghost=0.80, gem=(210, 40, 56))),
             ("Barra de Revelación", bar(120, 13, 0.55, ramp("#2fb8b0", 6, 0.08, 0.2, 1.4), glow_edge=(150, 255, 240), gem=(90, 235, 215))),
             ("Láudano (lleno)", flask(True)), ("Láudano (vacío)", flask(False)), ("Marco de oro", gold_frame(96, 22)),
             ("Divisor", divider(180, 11)), ("Panel", panel(150, 90))]
    scale = 4
    W = 1180
    img = Image.new("RGBA", (W, 560), (12, 17, 20, 255))
    d = ImageDraw.Draw(img)
    f = ImageFont.truetype(FONT_TEXT, 18)
    x, y, rowh = 14, 14, 0
    for label, cv in items:
        im = Image.new("RGBA", (cv.w, cv.h), (0, 0, 0, 0))
        im.alpha_composite(Image.fromarray(cv.emit, "RGBA").filter(__import__("PIL.ImageFilter", fromlist=["x"]).GaussianBlur(2)))
        im.alpha_composite(cv.to_image())
        im = im.resize((cv.w * scale, cv.h * scale), Image.NEAREST)
        if x + im.width > W - 14:
            x, y, rowh = 14, y + rowh + 40, 0
        img.alpha_composite(im, (x, y + 20))
        d.text((x, y), label, font=f, fill=(200, 190, 140, 255))
        x += max(im.width, 200) + 26
        rowh = max(rowh, im.height + 20)
    return img


if __name__ == "__main__":
    out = os.path.join(HERE, "_preview")
    os.makedirs(out, exist_ok=True)
    hud_mockup(boss="El Arcipreste de las Mareas").resize((1280, 720), Image.NEAREST).save(os.path.join(out, "hud.png"))
    elements_sheet().save(os.path.join(out, "ui_elements.png"))
    print("ok")
