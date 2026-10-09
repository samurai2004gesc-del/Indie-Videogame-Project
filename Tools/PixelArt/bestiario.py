"""Criaturas nuevas del bestiario (GDD seccion 5): Shoggoth menor, Byakhee, Mi-Go y Sacerdotisa de Hydra.
Disenos originales; todos miran a la derecha. Lienzos de 96-112 px, pies/suelo en y ~ 90."""
import math
import numpy as np
from pxl import *


def _union(shapes, scale=8.0):
    mask = np.zeros_like(shapes[0][0])
    height = np.zeros_like(shapes[0][1])
    for m, h in (s for s in shapes):
        height = np.where(m, np.maximum(height, h), height)
        mask |= m
    return Shape(mask, height, scale)


def _wing_poly(shoulder, tips, depth):
    """Poligono de ala: membrana festoneada entre los dedos."""
    pts = [shoulder]
    for i, t in enumerate(tips):
        if i > 0:
            prev = tips[i - 1]
            mid = ((prev[0] + t[0]) / 2, (prev[1] + t[1]) / 2)
            pts.append((mid[0] + (shoulder[0] - mid[0]) * depth, mid[1] + (shoulder[1] - mid[1]) * depth))
        pts.append(t)
    pts.append(shoulder)
    return pts


# =====================================================================================================
# SHOGGOTH MENOR  (96 x 96)
# =====================================================================================================
def build_shoggoth():
    W = H = 96
    GROUND = 90
    c = Canvas(W, H)
    c.set_light(-0.55, 0.7, 0.5)
    ooze = ramp("#201c36", 6, 0.14, 0.10, 1.40)
    n = noise2d(W, H, 4, seed=31, octaves=3)
    irid = noise2d(W, H, 9, seed=17, octaves=2)

    # charco bajo el cuerpo
    pud, _ = ellipse(W, H, 48, GROUND, 40, 3.2)
    c.fill(pud, (14, 20, 30), 200)

    # seudopodos (al fondo)
    for (a, ctrl, b, r0, r1) in [((30, 62), (10, 56), (14, 30), 8, 3.2), ((66, 58), (88, 50), (82, 24), 8, 3.2),
                                 ((50, 52), (46, 36), (44, 22), 6, 2.4)]:
        pts = bezier_pts(a, ctrl, b, 26)
        c.shade(tentacle(W, H, pts, r0, r1), ooze, noise=n, noise_amt=0.35, dither=0.3, outline=True)
        # mano con tres dedos
        tx, ty = b
        for dx, dy in [(-3, -4), (0, -5), (3, -4)]:
            c.shade(capsule(W, H, (tx, ty), (tx + dx, ty + dy), 1.8, 0.9), ooze, outline=True)

    # masa principal: union de lobulos
    lobes = [ellipse(W, H, 48, 66, 34, 24), ellipse(W, H, 26, 72, 17, 13), ellipse(W, H, 72, 70, 19, 15),
             ellipse(W, H, 40, 52, 17, 15), ellipse(W, H, 58, 54, 15, 13), ellipse(W, H, 48, 80, 38, 10)]
    body = _union(lobes, scale=10.0)
    c.shade(body, ooze, noise=n, noise_amt=0.40, dither=0.30, contrast=1.0, bias=-0.16)
    # brillo iridiscente aceitoso: solo en los reflejos mas altos y a manchas sueltas
    px_sum = c.px[..., :3].astype(int).sum(axis=2)
    top = body[0] & (px_sum >= sum(ooze[-1]) - 2) & (irid > 0.30)
    for lo, hi_, col in [(0.30, 0.52, (110, 80, 190)), (0.52, 0.76, (50, 170, 150)), (0.76, 1.1, (190, 70, 140))]:
        m = top & (irid >= lo) & (irid < hi_)
        base = c.px[m, :3].astype(np.float32)
        c.px[m, :3] = (base * 0.30 + np.array(col, np.float32) * 0.70).astype(np.uint8)
    # puntos de luz humeda
    for (x, y) in [(30, 60), (44, 44), (62, 50), (54, 62), (22, 70), (74, 66)]:
        c.set(x, y, (225, 245, 255)); c.set(x + 1, y, (170, 220, 235))

    # craneo de marinero a medio absorber
    c.shade(ellipse(W, H, 24, 72, 5.4, 5.0), ramp("#cdbf9a", 4, 0.06, 0.40, 1.2), outline=True)
    c.set(22, 71, (10, 6, 14)); c.set(26, 71, (10, 6, 14)); c.set(24, 74, (10, 6, 14))
    for x in (22, 24, 26):
        c.set(x, 76, (190, 175, 140))

    # ojos de distintos tamanos (emisivos)
    eyes = [(40, 56, 3.4), (56, 60, 2.6), (30, 66, 2.2), (68, 68, 3.0), (48, 48, 2.4), (78, 74, 1.8), (36, 76, 1.8), (60, 50, 1.8)]
    for (x, y, r) in eyes:
        c.disc(x, y, r + 0.8, (20, 14, 30))
        c.disc(x, y, r, (230, 235, 170), glow=True)
        c.disc(x + 0.4, y, r * 0.55, (170, 220, 60), glow=True)
        c.set(round(x), round(y), (10, 10, 10)); c.emit[int(y), int(x)] = (10, 10, 10, 0)
    # bocas con dientes
    for (x0, x1, y) in [(34, 54, 66), (58, 72, 76)]:
        for x in range(x0, x1):
            yy = y + round(math.sin((x - x0) / (x1 - x0) * math.pi) * 3)
            c.set(x, yy, (22, 6, 16)); c.set(x, yy + 1, (60, 12, 36))
        for x in range(x0 + 2, x1 - 1, 3):
            yy = y + round(math.sin((x - x0) / (x1 - x0) * math.pi) * 3)
            c.set(x, yy, (225, 215, 190)); c.set(x, yy + 1, (190, 180, 160))
    # babas y goteo
    for x, y0, L in [(34, 74, 12), (50, 78, 9), (66, 76, 12), (22, 80, 7)]:
        c.line((x, y0), (x, min(GROUND - 1, y0 + L)), (88, 120, 120))
        c.set(x, min(GROUND, y0 + L + 1), (150, 190, 190))
    c.contact_shadow(GROUND, 12, 86, alpha=130)
    return c


# =====================================================================================================
# BYAKHEE  (112 x 96)
# =====================================================================================================
def build_byakhee():
    W, H = 112, 96
    c = Canvas(W, H)
    c.set_light(-0.55, 0.7, 0.5)
    fur = ramp("#3d3350", 6, 0.12, 0.14, 1.45)
    wing = ramp("#5b4d78", 6, 0.12, 0.16, 1.50)
    bone = ramp("#cdbf9a", 4, 0.06, 0.40, 1.20)
    amber = (255, 190, 70)
    n = noise2d(W, H, 4, seed=41, octaves=2)

    def wing_shape(shoulder, tips, depth):
        """Membrana entre los dedos con borde inferior festoneado."""
        pts = [shoulder]
        for i, t in enumerate(tips):
            if i > 0:
                prev = tips[i - 1]
                mid = ((prev[0] + t[0]) / 2, (prev[1] + t[1]) / 2)
                pts.append((mid[0] + (shoulder[0] - mid[0]) * depth, mid[1] + (shoulder[1] - mid[1]) * depth))
            pts.append(t)
        pts.append(shoulder)
        return pts

    # ala trasera (izquierda, en sombra)
    back_tips = [(8, 6), (2, 24), (10, 42), (26, 54)]
    bp = wing_shape((46, 34), back_tips, 0.30)
    c.shade(polygon(W, H, bp, bevel=5, power=0.9), [shadow_of(x, 0.25) for x in wing], noise=n, noise_amt=0.4, outline=True, dither=0.25)
    for t in back_tips:
        c.line((46, 34), t, bone[1]); c.set(t[0], t[1], bone[2])
    # ala delantera (derecha)
    front_tips = [(102, 36), (110, 52), (102, 67), (82, 72)]
    fp = wing_shape((56, 42), front_tips, 0.16)
    c.shade(polygon(W, H, fp, bevel=5, power=0.9), wing, noise=n, noise_amt=0.4, outline=True, rim=(210, 190, 255), rim_amt=0.25, dither=0.25)
    for t in front_tips:
        c.line((56, 42), t, bone[2]); c.line((56, 43), t, bone[0]); c.set(t[0], t[1], bone[3])
    # patas colgando
    c.shade(capsule(W, H, (46, 62), (41, 78), 3.2, 1.8), fur, noise=n, noise_amt=0.3)
    c.shade(capsule(W, H, (54, 62), (60, 80), 3.2, 1.8), fur, noise=n, noise_amt=0.3)
    for (x, y, d) in [(41, 78, -1), (60, 80, 1)]:
        for k in (-2, 0, 2):
            c.line((x, y), (x + k + d, y + 7), bone[3]); c.set(x + k + d, y + 7, bone[2])
    # torso demacrado con costillas
    torso = ellipse(W, H, 50, 48, 10, 17, rot=-6)
    c.shade(torso, fur, noise=n, noise_amt=0.35, rim=(190, 160, 255), rim_amt=0.25, dither=0.25)
    for i, y in enumerate(range(40, 58, 3)):
        c.line((46, y), (54, y + 1), fur[1]); c.line((46, y - 1), (54, y), fur[4])
    c.stipple(torso[0], fur[4], 0.14, seed=7)
    # cola de pelo y mechones
    for (x, y) in [(45, 62), (47, 65), (52, 64), (50, 67)]:
        c.line((x, y), (x - 1, y + 5), fur[3])
    # brazos con garra de la que cuelga el ala
    c.shade(capsule(W, H, (54, 34), (66, 30), 3.0, 2.2), fur, noise=n, noise_amt=0.3)
    # cabeza alargada de buitre-murcielago
    c.shade(capsule(W, H, (50, 34), (54, 28), 5, 4.2), fur, noise=n, noise_amt=0.3)
    head = ellipse(W, H, 56, 24, 8.5, 7.0, rot=-8)
    c.shade(head, fur, noise=n, noise_amt=0.3, rim=(190, 160, 255), rim_amt=0.25, dither=0.25)
    # hocico largo con probóscide enroscada
    snout = polygon(W, H, [(62, 22), (74, 24), (80, 28), (74, 31), (62, 30)], bevel=2.4)
    c.shade(snout, fur, noise=n, noise_amt=0.2, rim=(190, 160, 255), rim_amt=0.2)
    prob = bezier_pts((78, 28), (86, 28), (86, 36), 12)
    c.shade(tentacle(W, H, prob, 2.0, 1.2), ramp("#8a3f55", 4, 0.08, 0.3, 1.3), outline=True)
    c.set(85, 37, (230, 150, 150))
    # mandibula con colmillos
    c.line((64, 31), (76, 33), fur[0]); c.set(70, 32, bone[3]); c.set(73, 32, bone[3]); c.set(67, 32, bone[3])
    # orejas largas
    c.shade(polygon(W, H, [(52, 20), (50, 8), (57, 18)], bevel=1.4), fur, outline=True)
    c.shade(polygon(W, H, [(58, 18), (60, 6), (63, 18)], bevel=1.4), fur, outline=True)
    # ojos de brasa
    for (x, y, r) in [(59, 22, 2.0), (65, 22, 1.5)]:
        c.disc(x, y, r + 0.7, (20, 10, 24)); c.disc(x, y, r, amber, glow=True); c.set(round(x), round(y), (255, 245, 200)); c.emit[int(y), int(x)] = (255, 245, 200, 255)
    return c


# =====================================================================================================
# MI-GO  (112 x 96)
# =====================================================================================================
def build_migo():
    W, H = 112, 96
    GROUND = 90
    c = Canvas(W, H)
    c.set_light(-0.55, 0.7, 0.5)
    crust = ramp("#b4583c", 6, 0.12, 0.14, 1.45)
    crust_dk = ramp("#6e2f3a", 5, 0.12, 0.20, 1.30)
    belly = ramp("#d8a58a", 5, 0.08, 0.30, 1.20)
    wing = ramp("#8a5a8e", 5, 0.12, 0.18, 1.45)
    cyan = (120, 250, 235)
    n = noise2d(W, H, 3, seed=51, octaves=2)

    # alas membranosas de murcielago plegadas hacia atras (al fondo)
    tips = [(8, 30), (16, 12), (32, 4), (50, 3), (66, 8)]
    wp = _wing_poly((62, 44), tips, 0.14)
    c.shade(polygon(W, H, wp, bevel=5, power=0.9), wing, noise=n, noise_amt=0.4, outline=True, dither=0.25, rim=(230, 190, 240), rim_amt=0.25)
    for t in tips:
        c.line((62, 44), t, wing[1]); c.line((62, 43), t, wing[4]); c.set(t[0], t[1], (225, 205, 215))
    # patas traseras (en sombra)
    for (x0, x1, x2) in [(34, 26, 24), (46, 40, 40)]:
        c.shade(capsule(W, H, (x0, 56), (x1, 70), 3.6, 2.4), crust_dk, noise=n, noise_amt=0.3)
        c.shade(capsule(W, H, (x1, 70), (x2, GROUND - 1), 2.4, 1.4), crust_dk)
        c.line((x2, GROUND - 1), (x2 - 3, GROUND), crust_dk[3]); c.line((x2, GROUND - 1), (x2 + 3, GROUND), crust_dk[3])
    # cola con aguijon que brilla
    tail = bezier_pts((30, 54), (10, 58), (8, 34), 22)
    c.shade(tentacle(W, H, tail, 6.5, 2.4), crust, noise=n, noise_amt=0.25, outline=True, dither=0.2)
    for i in range(3, 20, 4):
        x, y = tail[i]
        c.line((x - 3, y), (x + 3, y), crust_dk[1])
    c.shade(polygon(W, H, [(5, 37), (11, 37), (8, 25)], bevel=1.4), ramp("#4a2a3a", 5, 0.08, 0.3, 1.5), outline=True)
    c.glow_set(8, 28, cyan); c.glow_set(8, 30, cyan); c.glow_set(9, 31, (180, 255, 245))
    # abdomen y torax segmentados
    abd = ellipse(W, H, 38, 54, 14, 10.5, rot=-6)
    c.shade(abd, crust, noise=n, noise_amt=0.3, rim=(255, 180, 150), rim_amt=0.25, dither=0.25)
    thorax = ellipse(W, H, 58, 52, 16, 12.5, rot=-4)
    c.shade(thorax, crust, noise=n, noise_amt=0.3, rim=(255, 180, 150), rim_amt=0.25, dither=0.25)
    for x in (30, 38, 46, 52, 60, 68):
        c.line((x, 44), (x + 1, 62), crust_dk[1] if x % 2 else crust_dk[2])
    # vientre palido
    c.shade(ellipse(W, H, 50, 61, 20, 4.6), belly, noise=n, noise_amt=0.2, outline=False)
    # patas delanteras (4 pares visibles: 2 delante)
    for (x0, x1, x2) in [(52, 56, 52), (62, 70, 68)]:
        c.shade(capsule(W, H, (x0, 58), (x1, 72), 4.0, 2.6), crust, noise=n, noise_amt=0.3, rim=(255, 180, 150), rim_amt=0.2)
        c.shade(capsule(W, H, (x1, 72), (x2, GROUND - 1), 2.6, 1.5), crust)
        c.line((x2, GROUND - 1), (x2 - 3, GROUND), crust_dk[3]); c.line((x2, GROUND - 1), (x2 + 3, GROUND), crust_dk[3])
    c.contact_shadow(GROUND, 16, 84, alpha=140)
    # brazos con pinzas, alzados ante la cabeza
    for (a, b, pinch) in [((70, 46), (84, 52), (92, 50)), ((68, 52), (82, 62), (90, 64))]:
        c.shade(capsule(W, H, a, b, 3.6, 2.4), crust, noise=n, noise_amt=0.3, rim=(255, 180, 150), rim_amt=0.2)
        c.shade(polygon(W, H, [(b[0] - 1, b[1] - 2), (pinch[0] + 4, pinch[1] - 4), (pinch[0] + 6, pinch[1]), (b[0] + 2, b[1] + 3)], bevel=1.8), crust, outline=True)
        c.line((pinch[0], pinch[1] - 1), (pinch[0] + 5, pinch[1] + 2), crust_dk[0])
    # cabeza elipsoide cubierta de antenas luminosas
    head = ellipse(W, H, 80, 40, 12.5, 10.5, rot=-8)
    c.shade(head, ramp("#c46a48", 6, 0.12, 0.14, 1.45), noise=n, noise_amt=0.25, rim=(255, 190, 160), rim_amt=0.28, dither=0.3)
    for r in (3.5, 6.5, 9.0):
        for t in np.linspace(-1.4, 1.4, 22):
            x, y = 80 + math.cos(t) * r * 1.2, 40 + math.sin(t) * r * 0.9
            c.set(round(x), round(y), crust_dk[2])
    for k in range(16):
        a = -2.4 + k * 0.30
        x0, y0 = 80 + math.cos(a) * 11, 40 + math.sin(a) * 9
        x1, y1 = 80 + math.cos(a) * 17, 40 + math.sin(a) * 14
        c.line((x0, y0), (x1, y1), belly[2]); c.glow_set(round(x1), round(y1), cyan)
    return c


# =====================================================================================================
# SACERDOTISA DE HYDRA  (96 x 96)
# =====================================================================================================
def build_hydra():
    W = H = 96
    GROUND = 90
    c = Canvas(W, H)
    c.set_light(-0.55, 0.7, 0.5)
    robe = ramp("#2d6a66", 6, 0.12, 0.14, 1.40)
    robe_dk = ramp("#1c3f4e", 5, 0.12, 0.20, 1.30)
    skin = ramp("#8fb0b4", 5, 0.08, 0.28, 1.22)
    silver = ramp("#a9b8c0", 5, 0.07, 0.25, 1.40)
    snake = ramp("#3f8f7a", 5, 0.12, 0.18, 1.40)
    pearl = ramp("#dcd6c4", 4, 0.05, 0.50, 1.15)
    aqua = (110, 250, 230)
    n = noise2d(W, H, 5, seed=61, octaves=2)

    # charco de agua verdosa
    pud, _ = ellipse(W, H, 56, GROUND, 28, 3)
    c.fill(pud, (20, 70, 80), 190)
    for x in range(36, 76, 5):
        c.set(x, GROUND - 1, (120, 220, 215))

    # capa trasera con vuelo (al fondo)
    cloak = [(41, 38), (34, 46), (26, 64), (18, 82), (14, 90), (34, 90), (36, 68), (40, 54)]
    c.shade(polygon(W, H, cloak, bevel=5, power=0.9), robe_dk, noise=n, noise_amt=0.5, rim=(120, 220, 210), rim_amt=0.2, dither=0.2)
    c.line((38, 52), (28, 76), robe_dk[3]); c.line((36, 56), (24, 84), robe_dk[1])
    # tunica larga con pliegues
    rp = [(40, 36), (56, 36), (60, 52), (66, 72), (70, 90), (26, 90), (30, 72), (34, 52)]
    c.shade(polygon(W, H, rp, bevel=7, power=0.9), robe, noise=n, noise_amt=0.55, rim=(150, 255, 235), rim_amt=0.22, dither=0.25)
    for pts, col in [([(42, 52), (36, 72), (32, 88)], robe[0]), ([(48, 54), (46, 72), (46, 89)], robe[1]), ([(54, 54), (56, 72), (62, 88)], robe[1])]:
        for a, b in zip(pts[:-1], pts[1:]):
            c.line(a, b, col)
    # dobladillo plateado con olas
    for x in range(27, 70, 4):
        c.set(x, 86, silver[3]); c.set(x + 1, 85, silver[4]); c.set(x + 2, 86, silver[3]); c.set(x + 3, 87, silver[2])
    # sombra de contacto
    c.contact_shadow(GROUND, 22, 74, alpha=130)
    # faja con concha y perlas
    c.shade(polygon(W, H, [(34, 56), (60, 56), (61, 60), (33, 60)], bevel=1.4), silver, outline=True)
    c.shade(ellipse(W, H, 48, 58, 4.4, 3.6), pearl, outline=True)
    for x in range(36, 60, 4):
        c.disc(x, 58, 1.0, pearl[3])
    # cuello alto de conchas (gorguera)
    for k in range(6):
        x = 37 + k * 3.8
        c.shade(polygon(W, H, [(x, 40), (x + 4.2, 40), (x + 2.1, 33)], bevel=1.3), silver, outline=True)

    # brazo trasero + manos sosteniendo la concha
    c.shade(capsule(W, H, (42, 42), (44, 54), 3.4, 2.8), robe_dk, noise=n, noise_amt=0.3)
    # concha de vieira como caliz (delante)
    shell = polygon(W, H, [(56, 52), (72, 47), (77, 54), (73, 62), (61, 63), (56, 58)], bevel=2.6)
    c.shade(shell, silver, noise=n, noise_amt=0.15, rim=(210, 240, 255), rim_amt=0.3)
    for k in range(7):
        a = k / 6
        c.line((58, 58), (60 + a * 17, 47 + a * 15 * (1 if a > 0.5 else 0.2)), silver[1])
    # agua luminosa rebosando
    water = ellipse(W, H, 67.5, 52.5, 7.0, 2.8)
    c.shade(water, [(20, 90, 100), (40, 170, 170), (100, 240, 225), aqua], outline=False, dither=0.3)
    ys, xs = np.nonzero(water[0])
    for x, y in zip(xs, ys):
        c.emit[y, x] = (*c.px[y, x, :3], 255)
    for (x, y0, L) in [(72, 54, 20), (78, 56, 16), (66, 56, 14)]:
        for yy in range(y0, min(GROUND - 1, y0 + L), 1):
            c.glow_set(x, yy, mix(aqua, (30, 120, 130), (yy - y0) / L))
        c.glow_set(x, min(GROUND - 1, y0 + L), (200, 255, 245))
    # manos palidas sosteniendo la concha
    c.shade(capsule(W, H, (54, 42), (58, 53), 3.4, 2.6), robe, noise=n, noise_amt=0.3, rim=(150, 255, 235), rim_amt=0.2)
    c.shade(ellipse(W, H, 58.5, 55, 2.8, 2.6), skin)
    c.shade(ellipse(W, H, 70, 62, 2.6, 2.4), skin)

    # capucha redondeada con interior oscuro y rostro palido de perfil
    c.shade(ellipse(W, H, 49, 27, 10.5, 12.5), robe, noise=n, noise_amt=0.4, rim=(150, 255, 235), rim_amt=0.25, dither=0.2)
    c.shade(ellipse(W, H, 53.5, 28, 6.4, 8.6), [(6, 20, 28), (10, 34, 44), (16, 52, 62)], outline=False)
    face = ellipse(W, H, 55, 28.5, 4.8, 6.6)
    c.shade(face, skin, noise=n, noise_amt=0.15, rim=(210, 250, 255), rim_amt=0.3, contrast=1.1, bias=-0.04)
    c.set(60, 29, skin[3]); c.set(60, 30, skin[2])                       # nariz
    c.line((55, 33), (59, 33), robe_dk[1])                              # boca
    c.glow_set(56, 26, aqua); c.glow_set(57, 26, aqua); c.set(55, 26, (14, 60, 70))   # ojo
    # aletas branquiales detras de la mejilla
    for (x, y) in [(44, 24), (43, 28), (44, 32)]:
        c.line((x + 2, y), (x - 4, y - 2), snake[3]); c.line((x + 2, y + 1), (x - 4, y), snake[1])
    # corona de plata con gema
    c.shade(polygon(W, H, [(41, 17), (59, 17), (60, 21), (40, 21)], bevel=1.4), silver, outline=True)
    c.disc(50, 19, 1.7, aqua, glow=True)
    # tres cabezas de hidra con cuello en S, boca abierta y colmillos
    for (base, c1, mid, c2, tip, lean) in [((43, 17), (32, 14), (34, 7), (36, 4), (40, 1), -1),
                                             ((50, 17), (44, 10), (52, 6), (58, 4), (56, -2), 0),
                                             ((57, 17), (68, 14), (66, 7), (64, 4), (60, 1), 1)]:
        pts = bezier_pts(base, c1, mid, 12)[:-1] + bezier_pts(mid, c2, tip, 12)
        c.shade(tentacle(W, H, pts, 3.0, 2.0), snake, noise=n, noise_amt=0.2, outline=True, dither=0.2, rim=(180, 255, 220), rim_amt=0.2)
        for i in range(2, len(pts) - 2, 3):
            c.set(round(pts[i][0]) + 1, round(pts[i][1]), snake[4])
        hx, hy = tip
        c.shade(ellipse(W, H, hx + lean * 2, max(2.5, hy + 2), 4.4, 3.2, rot=lean * 20), snake, outline=True, rim=(180, 255, 220), rim_amt=0.25)
        c.glow_set(round(hx + lean * 2 - 1), max(1, round(hy + 1)), aqua); c.glow_set(round(hx + lean * 2 + 1), max(1, round(hy + 1)), aqua)
        c.set(round(hx + lean * 2), round(hy + 5), (235, 225, 205)); c.set(round(hx + lean * 2 + 1), round(hy + 5), (235, 225, 205))
    # perlas del collar
    for (x, y) in [(44, 42), (47, 44), (51, 45), (55, 44), (58, 42)]:
        c.disc(x, y, 1.1, pearl[3])
    return c


if __name__ == "__main__":
    import os
    from ahogado import moody_preview
    out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "_preview")
    os.makedirs(out, exist_ok=True)
    for name, fn in [("shoggoth", build_shoggoth), ("byakhee", build_byakhee), ("migo", build_migo), ("hydra", build_hydra)]:
        c = fn()
        moody_preview(c, 5).save(os.path.join(out, f"{name}_x5.png"))
    print("ok")
