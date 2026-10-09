"""Escenarios v2 de ABISMO (640x360): Costa de Innsmouth, Ruinas Ciclopeas, Santuario de las Mareas y Arrecife del Diablo.
Cada escena = capas de paralaje (Canvas) + luces + niebla; `render()` las compone con el paso de iluminacion."""
import math
import numpy as np
from PIL import Image
from pxl import Canvas, noise2d, ramp, polygon, ellipse, tentacle, bezier_pts, mix, shadow_of
from scenery import *
import props as P
import ahogado, profundo, sectario, ojo, arcipreste, bestiario

W, H = 640, 360


def bbox(cv):
    a = cv.px[..., 3] > 0
    ys, xs = np.nonzero(a)
    return xs.min(), ys.min(), xs.max() + 1, ys.max() + 1


def place(layer, sprite, x, ground_y, flip=False, float_dy=0, anchor="bottom"):
    """Pega un sprite con el centro de su silueta en x y la base apoyada en ground_y."""
    x0, y0, x1, y1 = bbox(sprite)
    crop = Canvas(x1 - x0, y1 - y0)
    crop.px[:] = sprite.px[y0:y1, x0:x1]
    crop.emit[:] = sprite.emit[y0:y1, x0:x1]
    if flip:
        crop.px[:] = crop.px[:, ::-1]
        crop.emit[:] = crop.emit[:, ::-1]
    layer.blit(crop, int(x - crop.w / 2), int(ground_y - crop.h - float_dy), glow=True)
    return crop


def hang(layer, sprite, x, top_y):
    """Pega un sprite colgado desde y=top_y (su borde superior)."""
    x0, y0, x1, y1 = bbox(sprite)
    crop = Canvas(x1 - x0, y1 - y0)
    crop.px[:] = sprite.px[y0:y1, x0:x1]
    crop.emit[:] = sprite.emit[y0:y1, x0:x1]
    layer.blit(crop, int(x - crop.w / 2), int(top_y), glow=True)
    return crop


def haze(layer, y0, y1, color, strength=0.55, seed=0):
    """Neblina pintada entre capas (banda con textura), para la perspectiva atmosferica."""
    n = noise2d(layer.w, layer.h, 24, seed=seed, octaves=3)
    rows = np.arange(layer.h)[:, None]
    prof = np.clip(1 - np.abs(rows - (y0 + y1) / 2) / ((y1 - y0) / 2), 0, 1)
    a = np.clip(prof * (0.4 + n) * strength, 0, 1)
    m = a > 0.04
    layer.px[m, :3] = color
    layer.px[m, 3] = (a[m] * 255).astype(np.uint8)


# ======================================================================================================
# 1) COSTA DE INNSMOUTH
# ======================================================================================================
def costa():
    sky = Canvas(W, H)
    grad_fill(sky, 0, 0, W, 300, [(4, 9, 12), (8, 18, 20), (16, 34, 32), (30, 56, 48), (44, 74, 62)])
    stars(sky, 110, 4, 170)
    moon(sky, 188, 104, 36)
    cloud_band(sky, 84, 18, 5, (10, 22, 24), scale=22, thresh=0.5, x0=120, x1=300)
    cloud_band(sky, 122, 22, 6, (8, 18, 20), scale=30, thresh=0.45, x0=100, x1=360)
    cloud_band(sky, 56, 16, 8, (12, 26, 28), scale=28, thresh=0.5, x0=40, x1=380)

    # el Durmiente: colosal silueta alada con barba de tentaculos, apenas visible contra el cielo
    dur = Canvas(W, H)
    sil = (3, 9, 11)
    for poly in [[(470, 0), (520, 70), (560, 40), (600, 20), (640, 0)],                      # ala trasera
                 [(520, 270), (500, 190), (520, 110), (560, 70), (600, 60), (640, 70), (640, 270)],  # masa del cuerpo
                 [(560, 28), (640, 10), (640, 90), (590, 100)]]:                                       # hombro
        m = polygon(W, H, poly, bevel=1)[0]
        dur.px[m, :3] = sil
        dur.px[m, 3] = 255
    for (cx_, cy_, rx, ry) in [(590, 74, 38, 34)]:
        m = ellipse(W, H, cx_, cy_, rx, ry)[0]
        dur.px[m, :3] = sil
        dur.px[m, 3] = 255
    for k, (dx, dy) in enumerate([(-26, 8), (-16, 14), (-4, 18), (10, 16), (22, 10)]):
        pts = bezier_pts((590 + dx * 0.4, 96), (590 + dx * 1.4, 140 + k * 6), (590 + dx * 1.8, 200 + (k % 2) * 30), 20)
        m = tentacle(W, H, pts, 6.5 - k * 0.4, 1.4)[0]
        dur.px[m, :3] = sil
        dur.px[m, 3] = 255
    dm = dur.px[..., 3] > 0
    left = dm & ~np.roll(dm, 2, axis=1)
    dur.px[left, :3] = (20, 44, 44)
    top = dm & ~np.roll(dm, 2, axis=0)
    dur.px[top, :3] = (16, 36, 36)
    for ex in (576, 604):
        dur.emit[66:69, ex:ex + 5] = (170, 100, 40, 255)
        dur.px[66:69, ex:ex + 5] = (170, 100, 40, 255)

    # pueblo lejano y cercano con neblina entre capas
    far = Canvas(W, H)
    ridge(far, 262, 36, 11, (20, 42, 41), scale=70)
    houses(far, 276, 0, W, 21, (18, 38, 38), window=(150, 104, 50), density=0.08, hmin=18, hmax=46)
    mid = Canvas(W, H)
    houses(mid, 296, 0, W, 33, (6, 14, 15), window=(230, 150, 64), density=0.13, hmin=30, hmax=76)
    # arboles muertos en primer plano lejano
    for tx, th in [(40, 66), (600, 78)]:
        for k in range(6):
            ang = -1.2 + k * 0.5
            pts = bezier_pts((tx, 300), (tx + math.cos(ang) * 14, 300 - th * 0.5), (tx + math.cos(ang) * 38, 300 - th), 14)
            m = tentacle(W, H, pts, 3.0 - k * 0.15, 0.5)[0]
            mid.px[m, :3] = (6, 14, 15)
            mid.px[m, 3] = 255

    haze1 = Canvas(W, H)
    haze(haze1, 238, 296, (46, 82, 70), 0.60, seed=3)
    haze2 = Canvas(W, H)
    haze(haze2, 268, 312, (34, 62, 52), 0.40, seed=5)

    # suelo de losas y muro
    gnd = Canvas(W, H)
    flagstones(gnd, 300, 70, "#5a6a62", seed=8, grass=(46, 86, 66))
    gnd.px[300 + 70:, :, :3] = (3, 6, 8)
    gnd.px[300 + 70:, :, 3] = 255

    # props y personaje
    obj = Canvas(W, H)
    place(obj, P.altar(), 360, 305)
    place(obj, P.candelabra(), 450, 305)
    place(obj, P.tombstone(), 540, 305)
    place(obj, P.tombstone(), 610, 305)
    hero = place(obj, ahogado.build(), 270, 305)

    # primer plano: pilar roto oscuro a la izquierda
    fg = Canvas(W, H)
    masonry(fg, 0, 0, 30, 306, "#27322f", bw=26, bh=12, seed=44, moss=0.05)
    fg.px[0:306, 0:30, :3] = (fg.px[0:306, 0:30, :3] * 0.35).astype(np.uint8)
    fg.px[0:306, 28:34, :3] = (4, 8, 9)
    fg.px[0:306, 28:34, 3] = np.linspace(255, 0, 6).astype(np.uint8)

    layers = [sky, dur, far, haze1, mid, haze2, gnd, obj, fg]
    lights = [
        (188, 104, 340, (0.50, 0.72, 0.62), 0.32, 1.4),                       # luna
        (360, 268, 100, (0.30, 1.00, 0.85), 1.00, 2.0),                       # altar
        (450, 252, 80, (1.00, 0.70, 0.34), 0.85, 2.0),                        # candelabro
        (282, 267, 42, (0.35, 1.00, 0.90), 0.55, 2.0),                        # ojo de buey del Ahogado
        (480, 276, 70, (0.55, 0.78, 0.66), 0.10, 1.5),
    ]
    fog = [(262, 60, (0.30, 0.46, 0.42), 0.55, 46), (306, 32, (0.22, 0.34, 0.32), 0.45, 60)]
    return dict(name="costa_innsmouth", zone="Costa de Innsmouth", layers=layers, ambient=(0.50, 0.72, 0.64), lights=lights, fog=fog,
                vignette=0.60)


# ======================================================================================================
# utilidades extra de escena
# ======================================================================================================
def runes(layer, x, y, n, seed, color=(110, 250, 230)):
    """Grupos de runas pequenas y brillantes (trazos aleatorios en cajas de 7x9)."""
    rng = np.random.default_rng(seed)
    for k in range(n):
        gx, gy = x + k * 11, y + int(rng.integers(-2, 3))
        pts = [(gx + int(rng.integers(0, 7)), gy + int(rng.integers(0, 9))) for _ in range(4)]
        for a, b in zip(pts[:-1], pts[1:]):
            layer.line(a, b, color, glow=True)
        layer.glow_set(gx + 3, gy - 2, color)


def chain(layer, x, y0, y1, color=(54, 58, 62)):
    for i, y in enumerate(range(y0, y1, 4)):
        if i % 2 == 0:
            layer.px[y:y + 5, x - 1:x + 1, :3] = color
            layer.px[y:y + 5, x - 1:x + 1, 3] = 255
            layer.set(x - 2, y + 2, color); layer.set(x + 1, y + 2, color)
        else:
            layer.px[y + 1:y + 3, x - 2:x + 2, :3] = color
            layer.px[y + 1:y + 3, x - 2:x + 2, 3] = 255
    layer.px[y0:y1, x - 1:x, :3] = (88, 92, 96)


def lightning(layer, x0, x1, y_end, seed, color=(214, 230, 255)):
    rng = np.random.default_rng(seed)
    pts = [(x0, 0)]
    x, y = x0, 0
    while y < y_end:
        y += int(rng.integers(8, 18))
        x += int((x1 - x0) / (y_end / 12) + rng.integers(-9, 10))
        pts.append((x, min(y, y_end)))
    for a, b in zip(pts[:-1], pts[1:]):
        layer.line(a, b, color, glow=True)
        layer.glow_set(a[0] + 1, a[1], mix(color, (90, 110, 200), 0.6))
        if rng.random() < 0.55:
            ex, ey = b[0] + int(rng.integers(-26, 27)), b[1] + int(rng.integers(8, 28))
            layer.line(b, (ex, ey), mix(color, (80, 100, 190), 0.5), glow=True)
    return pts


# ======================================================================================================
# 2) RUINAS CICLOPEAS
# ======================================================================================================
def ruinas():
    back = Canvas(W, H)
    grad_fill(back, 0, 0, W, H, [(4, 10, 12), (8, 18, 20), (12, 26, 28)])
    masonry(back, 0, 0, W, 300, "#34504a", bw=64, bh=30, seed=3, moss=0.05, variation=0.22)
    back.px[..., :3] = (back.px[..., :3] * 0.55).astype(np.uint8)
    # arcos ojivales gigantes con fondo abisal
    def abyss(cv, m):
        ys, xs = np.nonzero(m)
        t = (ys - ys.min()) / max(1, ys.max() - ys.min())
        cols = np.stack([4 + t * 6, 10 + t * 14, 12 + t * 16], axis=1).astype(np.uint8)
        cv.px[ys, xs, :3] = cols
        cv.px[ys, xs, 3] = 255
    for cx_ in (110, 330, 550):
        arch(back, cx_, 300, 120, 210, "#3d5c54", thickness=9, seed=cx_, interior=abyss)
    # columnas lejanas dentro de los arcos
    far = Canvas(W, H)
    for cx_ in (90, 130, 310, 350, 530, 570):
        column(far, cx_, 300, 100, 7, "#233834", cap=True, flutes=True)
    far.px[..., :3] = (far.px[..., :3] * 0.6).astype(np.uint8)
    haze1 = Canvas(W, H)
    haze(haze1, 190, 310, (22, 52, 48), 0.55, seed=9)

    mid = Canvas(W, H)
    for cx_ in (30, 222, 438, 626):
        column(mid, cx_, 304, 30, 15, "#4a6a60", cap=True, flutes=True)
    # runas brillantes en los muros
    runes(mid, 60, 120, 3, 1); runes(mid, 250, 90, 3, 2); runes(mid, 470, 130, 3, 3); runes(mid, 560, 78, 2, 4)
    for x in (170, 290, 410, 520):
        chain(mid, x, 0, int(110 + (x % 60)))
    gnd = Canvas(W, H)
    flagstones(gnd, 300, 70, "#4c6a62", seed=12, grass=(46, 96, 76))
    # bloques caidos y escalon
    gnd.px[286:300, 520:600, :3] = (46, 66, 60); gnd.px[286:300, 520:600, 3] = 255
    gnd.px[286:288, 520:600, :3] = (86, 120, 108)
    gnd.px[300 + 70:, :, :3] = (3, 6, 8); gnd.px[300 + 70:, :, 3] = 255

    obj = Canvas(W, H)
    place(obj, P.mother_statue(), 360, 306)
    place(obj, P.idol(), 190, 306)
    place(obj, P.coral(), 560, 306)
    place(obj, P.column(), 470, 306)
    hang(obj, P.cage(), 140, 0)
    place(obj, ahogado.build(), 90, 306)
    place(obj, profundo.build(), 262, 306, flip=True)
    place(obj, ojo.build(), 460, 220, float_dy=0)
    fg = Canvas(W, H)
    fg.px[0:306, 0:18, :3] = (2, 5, 6); fg.px[0:306, 0:18, 3] = 255
    fg.px[0:306, 622:640, :3] = (2, 5, 6); fg.px[0:306, 622:640, 3] = 255

    layers = [back, far, haze1, mid, gnd, obj, fg]
    lights = [
        (360, 240, 150, (0.30, 1.00, 0.85), 0.70, 2.0),    # aureola de la Madre
        (200, 250, 70, (0.30, 1.00, 0.85), 0.30, 2.0),     # ojos del idolo
        (460, 222, 70, (1.00, 0.30, 0.25), 0.60, 2.0),     # iris del Ojo del Vacio
        (96, 262, 46, (0.35, 1.00, 0.90), 0.45, 2.0),      # ojo de buey del Ahogado
        (330, 60, 260, (0.30, 0.55, 0.50), 0.25, 1.4),     # luz fria desde lo alto
    ]
    shafts = [(300, 0, 360, 300, 40, (0.20, 0.55, 0.50), 0.35), (480, 0, 520, 300, 30, (0.20, 0.55, 0.50), 0.25)]
    fog = [(270, 70, (0.18, 0.40, 0.38), 0.55, 50), (130, 90, (0.12, 0.26, 0.26), 0.25, 70)]
    return dict(name="ruinas_ciclopeas", zone="Ruinas Ciclópeas", layers=layers, ambient=(0.40, 0.64, 0.58), lights=lights, fog=fog,
                shafts=shafts, vignette=0.65, seed=2)


# ======================================================================================================
# 3) SANTUARIO DE LAS MAREAS (jefe)
# ======================================================================================================
def santuario():
    back = Canvas(W, H)
    grad_fill(back, 0, 0, W, H, [(10, 6, 8), (22, 12, 14), (34, 18, 18)])
    masonry(back, 0, 0, W, 300, "#6a4a3e", bw=34, bh=16, seed=5, moss=0.0, variation=0.2)
    back.px[..., :3] = (back.px[..., :3] * 0.50).astype(np.uint8)
    # vidrieras altas que iluminan la nave
    glass_pal = [(22, 84, 80), (30, 104, 90), (16, 58, 74), (150, 98, 36), (104, 30, 44), (24, 70, 100)]
    for cx_, ww in ((120, 56), (320, 70)):
        arch(back, cx_, 232, ww + 16, 150, "#7a5a4a", thickness=7, seed=cx_, interior=lambda cv, m: None)
        stained_window(back, cx_, 232, ww, 144, cx_ + 3, glass_pal)
    back.emit[..., :3] = (back.emit[..., :3] * 0.55).astype(np.uint8)
    m_ = back.emit[..., 3] > 0
    back.px[m_, :3] = (back.px[m_, :3] * 0.7).astype(np.uint8)
    # arcos ciegos y arquerias
    far = Canvas(W, H)
    for cx_ in (30, 222, 420, 618):
        column(far, cx_, 300, 40, 16, "#7a5646", cap=True, flutes=True)
    far.px[..., :3] = (far.px[..., :3] * 0.7).astype(np.uint8)
    haze1 = Canvas(W, H)
    haze(haze1, 210, 310, (60, 36, 34), 0.40, seed=11)

    mid = Canvas(W, H)
    # estandartes carmesi colgando entre columnas
    for x in (110, 320):
        hang(mid, P.banner(), x, 36)
    # suelo con estrella dorada incrustada
    gnd = Canvas(W, H)
    flagstones(gnd, 300, 70, "#6a4a40", seed=15, moss=(110, 74, 54))
    gnd.px[300 + 70:, :, :3] = (4, 3, 4); gnd.px[300 + 70:, :, 3] = 255
    # tarima elevada a la derecha para el jefe
    gnd.px[286:300, 380:640, :3] = (88, 62, 52); gnd.px[286:300, 380:640, 3] = 255
    gnd.px[286:288, 380:640, :3] = (150, 108, 84)
    for x in range(384, 640, 22):
        gnd.px[288:300, x:x + 1, :3] = (46, 30, 26)
    obj = Canvas(W, H)
    place(obj, P.candelabra(), 250, 306)
    place(obj, P.candelabra(), 610, 292)
    place(obj, P.altar(), 36, 306)
    place(obj, arcipreste.build(), 480, 294, flip=True)
    place(obj, ahogado.build(), 150, 306)
    fg = Canvas(W, H)
    fg.px[0:306, 0:10, :3] = (2, 2, 3); fg.px[0:306, 0:10, 3] = 255
    fg.px[0:306, 632:640, :3] = (2, 2, 3); fg.px[0:306, 632:640, 3] = 255

    layers = [back, far, haze1, mid, gnd, obj, fg]
    lights = [
        (120, 160, 150, (0.20, 0.85, 0.75), 0.55, 1.6), (320, 150, 190, (0.25, 0.90, 0.75), 0.65, 1.6),
        (250, 258, 90, (1.00, 0.66, 0.30), 0.95, 2.0), (610, 244, 90, (1.00, 0.66, 0.30), 0.85, 2.0),
        (36, 262, 80, (0.30, 1.00, 0.85), 0.70, 2.0),
        (440, 190, 150, (1.00, 0.70, 0.45), 0.55, 2.0),    # gema del baculo / mitra
    ]
    shafts = [(120, 100, 190, 300, 34, (0.25, 0.80, 0.70), 0.55), (320, 90, 250, 300, 40, (0.25, 0.80, 0.70), 0.40)]
    fog = [(290, 50, (0.30, 0.20, 0.18), 0.45, 56)]
    return dict(name="santuario_mareas", zone="Santuario de las Mareas", layers=layers, ambient=(0.60, 0.46, 0.42), lights=lights, fog=fog,
                shafts=shafts, vignette=0.65, seed=3, boss="El Arcipreste de las Mareas")


# ======================================================================================================
# 4) ARRECIFE DEL DIABLO
# ======================================================================================================
def arrecife():
    sky = Canvas(W, H)
    grad_fill(sky, 0, 0, W, 300, [(8, 4, 20), (22, 12, 44), (44, 26, 74), (70, 42, 96), (96, 62, 112)])
    stars(sky, 40, 7, 90, color=(180, 170, 230))
    for k, (y, h, sd) in enumerate([(30, 40, 21), (80, 50, 22), (140, 46, 23), (190, 36, 24)]):
        cloud_band(sky, y, h, sd, mix((18, 10, 34), (50, 30, 78), k / 3), scale=34, thresh=0.42, alpha=240)
    bolt = Canvas(W, H)
    lightning(bolt, 470, 520, 230, 5)
    # R'lyeh emergiendo en el horizonte: torres ciclopeas con angulos imposibles y ventanas verdes
    rl = Canvas(W, H)
    towers = [[(300, 262), (306, 170), (322, 120), (338, 170), (348, 262)],
              [(350, 262), (352, 140), (380, 70), (404, 150), (402, 262)],
              [(404, 262), (420, 190), (452, 160), (470, 200), (470, 262)],
              [(250, 262), (262, 200), (292, 176), (300, 262)],
              [(468, 262), (486, 214), (520, 196), (540, 262)]]
    for poly in towers:
        m = polygon(W, H, poly, bevel=1)[0]
        rl.px[m, :3] = (24, 20, 46); rl.px[m, 3] = 255
        left = m & ~np.roll(m, 1, axis=1)
        rl.px[left, :3] = (46, 40, 78)
    rng = np.random.default_rng(31)
    ys, xs = np.nonzero(rl.px[..., 3] > 0)
    for _ in range(46):
        i = int(rng.integers(0, len(xs)))
        x, y = xs[i], ys[i]
        if 80 < y < 252:
            rl.px[y:y + 5, x:x + 1, :3] = (96, 255, 170); rl.px[y:y + 5, x:x + 1, 3] = 255
            rl.emit[y:y + 5, x:x + 1] = (96, 255, 170, 255)
    # mar con olas y reflejos
    sea = Canvas(W, H)
    grad_fill(sea, 0, 252, W, 306, [(30, 22, 58), (18, 12, 40), (10, 6, 26)])
    for y in range(256, 304, 3):
        for x in range(0, W, 2):
            off = math.sin(x * 0.07 + y * 0.9) * 2 + math.sin(x * 0.03 + y) * 1.4
            if int(off + 3) % 5 == 0:
                sea.set(x, y, (60, 50, 108) if y < 280 else (38, 30, 80))
    # reflejo del rayo y de R'lyeh
    for y in range(256, 300, 2):
        sea.set(500 + int(math.sin(y * 0.6) * 4), y, (150, 160, 220))
    for x, h in [(338, 40), (380, 46), (440, 30)]:
        for y in range(258, 258 + h, 2):
            sea.set(x + int(math.sin(y * 0.9) * 2), y, (40, 150, 100))
    mid = Canvas(W, H)
    # rocas negras dentadas del arrecife lejano
    for (rx, rh, rw) in [(40, 60, 70), (170, 46, 60), (560, 70, 80), (620, 40, 40)]:
        pts = [(rx - rw, 300), (rx - rw * 0.5, 300 - rh * 0.6), (rx - rw * 0.2, 300 - rh), (rx + rw * 0.1, 300 - rh * 0.7),
               (rx + rw * 0.4, 300 - rh * 0.9), (rx + rw, 300)]
        m = polygon(W, H, pts, bevel=1)[0]
        mid.px[m, :3] = (8, 6, 16); mid.px[m, 3] = 255
        left = m & ~np.roll(m, 1, axis=1)
        mid.px[left, :3] = (34, 26, 60)
    gnd = Canvas(W, H)
    flagstones(gnd, 300, 70, "#3a3550", seed=18, grass=None, moss=(60, 90, 90))
    gnd.px[300 + 70:, :, :3] = (3, 3, 6); gnd.px[300 + 70:, :, 3] = 255
    # percebes y salpicaduras claras en la roca
    rng = np.random.default_rng(5)
    m = (rng.random((H, W)) < 0.05)
    reg = np.zeros((H, W), bool); reg[300:312, :] = True
    gnd.px[m & reg & gnd.alpha_mask(), :3] = (150, 150, 170)
    obj = Canvas(W, H)
    place(obj, P.coral(), 120, 306)
    place(obj, P.coral(), 560, 306)
    place(obj, P.altar(), 330, 306)
    place(obj, ahogado.build(), 250, 306)
    place(obj, sectario.build(), 450, 306, flip=True)
    place(obj, bestiario.build_byakhee(), 90, 150, float_dy=0)
    place(obj, bestiario.build_hydra(), 600, 306, flip=True)
    fg = Canvas(W, H)
    for (rx, rh, rw) in [(0, 150, 46), (640, 120, 40)]:
        pts = [(rx - rw, 360), (rx - rw * 0.7, 360 - rh * 0.5), (rx - rw * 0.3, 360 - rh), (rx + rw * 0.2, 360 - rh * 0.6), (rx + rw, 360)]
        m = polygon(W, H, pts, bevel=1)[0]
        fg.px[m, :3] = (3, 3, 7); fg.px[m, 3] = 255

    layers = [sky, bolt, rl, sea, mid, gnd, obj, fg]
    lights = [
        (495, 110, 280, (0.70, 0.78, 1.00), 0.55, 1.5),     # rayo
        (390, 190, 220, (0.30, 1.00, 0.65), 0.45, 1.6),     # R'lyeh
        (330, 262, 90, (0.30, 1.00, 0.85), 0.80, 2.0),      # altar
        (258, 262, 44, (0.35, 1.00, 0.90), 0.45, 2.0),
        (450, 252, 40, (1.00, 0.60, 0.30), 0.55, 2.0),      # orbe del Sectario
        (600, 262, 70, (0.30, 1.00, 0.85), 0.50, 2.0),      # agua de la Sacerdotisa
    ]
    fog = [(262, 40, (0.34, 0.26, 0.50), 0.40, 50), (300, 36, (0.22, 0.18, 0.36), 0.40, 60)]
    return dict(name="arrecife_diablo", zone="Arrecife del Diablo", layers=layers, ambient=(0.52, 0.42, 0.70), lights=lights, fog=fog,
                vignette=0.60, seed=4)


# ======================================================================================================
def render(scene, hud=True, boss=None, scale=2):
    rgba, emit = flatten(scene["layers"])
    out = light_pass(rgba, emit, scene["ambient"], scene.get("lights", ()), scene.get("fog"),
                     vignette=scene.get("vignette", 0.55), grain=scene.get("grain", 0.035), bloom=scene.get("bloom", 0.6),
                     shafts=scene.get("shafts", ()), seed=scene.get("seed", 1))
    img = Image.fromarray(out, "RGB").convert("RGBA")
    if hud:
        import ui
        img = ui.hud_mockup(zone=scene["zone"], boss=boss, bg=img, with_extras=False)
    return img.resize((W * scale, H * scale), Image.NEAREST) if scale != 1 else img


SCENES = {"costa": costa, "ruinas": ruinas, "santuario": santuario, "arrecife": arrecife}

if __name__ == "__main__":
    import os, sys
    out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "_preview")
    os.makedirs(out, exist_ok=True)
    which = sys.argv[1:] or list(SCENES)
    for k in which:
        sc = SCENES[k]()
        render(sc, hud=False).convert("RGB").save(os.path.join(out, f"escena_{k}.png"))
    print("ok")


def export_all():
    import os
    from PIL import Image
    root = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "docs", "art", "escenarios"))
    os.makedirs(os.path.join(root, "capas"), exist_ok=True)
    for k, fn in SCENES.items():
        sc = fn()
        render(sc, hud=False, scale=2).convert("RGB").save(os.path.join(root, f"{sc['name']}.png"))
        render(sc, hud=True, boss=sc.get("boss"), scale=2).convert("RGB").save(os.path.join(root, f"{sc['name']}_hud.png"))
        for i, cv in enumerate(sc["layers"]):
            cv.save(os.path.join(root, "capas", f"{sc['name']}_capa{i}.png"))
    print("exportado", root)
