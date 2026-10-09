"""El Profundo - diseno v2. Anfibio encorvado de Innsmouth. Lienzo 112x80, pies en y=76, mira a la derecha."""
import math
import numpy as np
from pxl import *

W, H = 112, 80
GROUND = 76


def build():
    c = Canvas(W, H)
    c.set_light(-0.6, 0.7, 0.45)

    skin = ramp("#32605a", 6, 0.13, 0.14, 1.40)
    skin_dk = ramp("#223f48", 5, 0.12, 0.20, 1.30)
    belly = ramp("#7e9a86", 5, 0.08, 0.28, 1.25)
    fin = ramp("#5a3d78", 5, 0.12, 0.20, 1.50)
    cloth = ramp("#272a4c", 5, 0.10, 0.22, 1.30)
    bone = ramp("#cdbf9a", 4, 0.06, 0.40, 1.20)
    slime = (150, 220, 195)
    eye_y = (255, 224, 90)
    wet = (190, 240, 225)

    n_skin = noise2d(W, H, 3, seed=4, octaves=2)
    n_big = noise2d(W, H, 7, seed=11, octaves=2)
    n_mot = noise2d(W, H, 5, seed=23, octaves=3)

    def mottle(shape):
        """Manchas de piel humeda: toca la forma ya pintada, con tonos mas oscuros y mas claros segun el ruido."""
        m = shape[0] & c.alpha_mask()
        dark = m & (n_mot > 0.60)
        light = m & (n_mot < 0.28)
        c.px[dark, :3] = np.array(skin[1], np.uint8)
        c.px[light, :3] = np.array(skin[4], np.uint8)

    # ---- crestas dorsales: espinas individuales con membrana fina y desgarrada (al fondo) -------
    spines = [((57, 27), (60, 10)), ((52, 29), (51, 10)), ((47, 30), (42, 12)), ((42, 32), (34, 15)),
              ((37, 35), (27, 20)), ((33, 39), (22, 27)), ((30, 44), (20, 34))]
    for (bx, by), (tx, ty) in spines:
        dx, dy = tx - bx, ty - by
        L = math.hypot(dx, dy)
        nx, ny = -dy / L * 2.2, dx / L * 2.2
        tri = [(bx + nx, by + ny), (tx, ty), (bx - nx, by - ny)]
        c.shade(polygon(W, H, tri, bevel=1.4), fin, outline=True, dither=0.2)
        c.line((bx, by), (tx, ty), fin[4])
    # membrana corta cerca de la base, con muescas
    mem = [(57, 28), (52, 30), (47, 31), (42, 33), (37, 36), (33, 40), (30, 45), (35, 44), (41, 41), (47, 38), (53, 35), (58, 32)]
    c.shade(polygon(W, H, mem, bevel=2.0), fin, noise=n_skin, noise_amt=0.3, outline=False)

    # ---- pierna trasera (en sombra) ---------------------------------------------------------------
    c.shade(capsule(W, H, (31, 50), (23, 62), 7.0, 3.8), skin_dk, noise=n_skin, noise_amt=0.3)
    c.shade(capsule(W, H, (23, 62), (31, 73), 3.8, 2.4), skin_dk)
    c.shade(polygon(W, H, [(27, 74), (31, 72), (39, 75), (42, 77), (24, 77)], bevel=1.4), skin_dk)
    for x in (37, 39, 41):
        c.line((x, 75), (x + 2, 77), bone[2])

    # ---- torso encorvado -------------------------------------------------------------------------------
    c.shade(ellipse(W, H, 36, 50, 11.5, 10.5), skin_dk, noise=n_skin, noise_amt=0.3)
    torso = ellipse(W, H, 46, 40, 16.5, 12.5, rot=-28)
    c.shade(torso, skin, noise=n_big, noise_amt=0.3, rim=(150, 240, 210), rim_amt=0.2, dither=0.25)
    mottle(torso)
    # vientre palido con placas horizontales y costillas
    c.shade(ellipse(W, H, 49, 45, 9.0, 7.5, rot=-28), belly, noise=n_skin, noise_amt=0.2, outline=False, dither=0.3)
    for i, (x, y) in enumerate([(45, 39), (48, 42), (50, 45), (51, 48), (50, 50)]):
        c.line((x, y), (x + 6, y - 3), belly[1]); c.line((x + 1, y + 1), (x + 7, y - 2), belly[4] if i % 2 == 0 else belly[3])
    # lomo con verrugas
    back = ellipse(W, H, 42, 37, 13, 8, rot=-28)
    c.stipple(back[0], skin[1], 0.16, seed=1)
    c.stipple(back[0], skin[4], 0.09, seed=2)
    # brillos de piel mojada
    for (x, y) in [(40, 33), (44, 31), (36, 38), (48, 31)]:
        c.set(x, y, wet)

    # ---- pantalon de marinero podrido (jirones) ---------------------------------------------------------
    cloth_pts = [(26, 47), (44, 52), (46, 61), (42, 58), (40, 65), (36, 58), (32, 64), (29, 56), (25, 61), (24, 52)]
    c.shade(polygon(W, H, cloth_pts, bevel=3.5, power=0.9), cloth, noise=n_big, noise_amt=0.5, rim=(110, 110, 190), rim_amt=0.15)
    c.line((27, 50), (26, 58), cloth[0]); c.line((35, 53), (34, 60), cloth[0]); c.line((41, 54), (42, 58), cloth[1])
    c.hline(26, 43, 49, cloth[4])
    for x in range(27, 43, 3):
        c.set(x, 50, cloth[0])
    # alga enredada en la cintura
    for (x, y) in [(28, 52), (31, 55), (30, 58)]:
        c.set(x, y, skin[3]); c.set(x, y + 1, skin[1])

    # ---- pierna delantera digitigrada ---------------------------------------------------------------------
    thigh = capsule(W, H, (38, 53), (47, 63), 7.2, 4.4)
    c.shade(thigh, skin, noise=n_skin, noise_amt=0.3, rim=(150, 240, 210), rim_amt=0.2)
    mottle(thigh)
    c.shade(capsule(W, H, (47, 63), (40, 73), 4.4, 2.6), skin)
    c.set(46, 61, wet); c.set(45, 60, wet)
    foot = polygon(W, H, [(37, 72), (42, 70), (52, 74), (57, 77), (34, 77)], bevel=1.6)
    c.shade(foot, skin, noise=n_skin, noise_amt=0.2)
    for x0 in (47, 50, 53):
        c.line((x0, 74), (x0 + 3, 77), bone[3]); c.set(x0 + 3, 77, bone[1])
    c.contact_shadow(GROUND, 18, 62, alpha=150)

    # ---- brazos largos y flacos con manos de garras largas ------------------------------------------------------
    # brazo trasero
    c.shade(capsule(W, H, (52, 36), (60, 50), 4.0, 2.4), skin_dk, noise=n_skin, noise_amt=0.3)
    c.shade(capsule(W, H, (60, 50), (68, 65), 2.4, 1.8), skin_dk)
    # brazo delantero: hombro musculoso -> antebrazo fino y largo
    sh = capsule(W, H, (55, 34), (65, 47), 5.6, 3.0)
    c.shade(sh, skin, noise=n_skin, noise_amt=0.3, rim=(150, 240, 210), rim_amt=0.2)
    mottle(sh)
    c.shade(capsule(W, H, (65, 47), (77, 61), 3.0, 2.0), skin, rim=(150, 240, 210), rim_amt=0.15)
    c.shade(ellipse(W, H, 65.5, 47, 3.4, 3.0), skin)       # codo nudoso
    c.set(64, 45, wet)
    # mano palmeada
    hand = polygon(W, H, [(75, 59), (80, 58), (85, 64), (83, 69), (77, 66)], bevel=1.4)
    c.shade(hand, skin, noise=n_skin, noise_amt=0.2)
    for (x0, y0, x1, y1) in [(81, 59, 92, 62), (83, 64, 93, 69), (79, 66, 86, 74)]:
        c.line((x0, y0), (x1, y1), bone[3]); c.line((x0, y0 + 1), (x1, y1), bone[1])
        c.set(x1, y1, bone[3]); c.set(x1 + 1, y1, bone[2])
    c.line((78, 59), (82, 66), skin[1]); c.line((80, 58), (84, 68), skin[1])

    # ---- cabeza de rana-pez -------------------------------------------------------------------------------------------
    # aletas branquiales (abanico detras de la mandibula)
    for (x1, y1) in [(50, 26), (49, 31), (50, 36), (52, 41)]:
        c.line((61, 33), (x1, y1), fin[4]); c.line((61, 34), (x1, y1 + 1), fin[2])
    c.shade(polygon(W, H, [(61, 31), (50, 26), (49, 31), (50, 36), (52, 41), (61, 37)], bevel=1.8), fin, outline=False, dither=0.3)
    # cuello
    c.shade(capsule(W, H, (56, 30), (63, 30), 7, 6.2), skin, noise=n_skin, noise_amt=0.3)
    # papo hinchado de la garganta
    throat = ellipse(W, H, 76, 43, 8, 4.4, rot=8)
    c.shade(throat, belly, noise=n_skin, noise_amt=0.2, dither=0.3)
    for k in range(4):
        c.line((70 + k * 3, 41), (70 + k * 3 + 1, 46), belly[1])
    # craneo
    skull = ellipse(W, H, 66, 28, 10.5, 8.5, rot=-10)
    c.shade(skull, skin, noise=n_skin, noise_amt=0.3, rim=(150, 240, 210), rim_amt=0.25, dither=0.25)
    mottle(skull)
    # hocico ancho y corto
    muzzle = polygon(W, H, [(68, 28), (82, 27), (89, 31), (90, 35), (84, 37), (69, 37)], bevel=2.8, power=0.9)
    c.shade(muzzle, skin, noise=n_skin, noise_amt=0.25, rim=(150, 240, 210), rim_amt=0.2)
    # mandibula entreabierta con dientes en aguja
    jaw = polygon(W, H, [(70, 37), (88, 36), (90, 41), (82, 44), (72, 42)], bevel=1.8)
    c.shade(jaw, skin_dk, noise=n_skin, noise_amt=0.2)
    mouth = polygon(W, H, [(72, 35.5), (89, 35), (88, 38.5), (73, 39)], bevel=1.0)
    c.shade(mouth, [(24, 6, 14), (60, 12, 26), (110, 22, 36), (150, 44, 56)], outline=False)
    for x in range(73, 89, 2):
        c.line((x, 36), (x, 38 + (x % 3 == 0)), bone[3]); c.set(x, 38, bone[2])
    for x in range(74, 88, 3):
        c.line((x, 39), (x, 37), bone[2])
    c.set(88, 31, skin_dk[1]); c.set(89, 31, skin_dk[1])  # fosa nasal
    # dos ojos saltones: el delantero amarillo, el trasero en sombra
    c.shade(ellipse(W, H, 65, 21, 3.6, 3.4), skin_dk, outline=True)
    c.shade(ellipse(W, H, 65.5, 21, 2.4, 2.4), [(80, 60, 10), (130, 100, 20), (170, 140, 30)], outline=False)
    c.shade(ellipse(W, H, 73, 22, 5.6, 5.4), [skin_dk[0], skin_dk[1], skin[2], skin[3]], outline=True)
    eye = ellipse(W, H, 73.4, 22.2, 4.1, 4.1)
    c.shade(eye, [(120, 90, 20), (190, 150, 30), (240, 200, 60), eye_y], outline=False, dither=0.3)
    for y in range(19, 26):
        c.set(74, y, (28, 18, 6))
    c.set(73, 23, (28, 18, 6)); c.set(73, 20, (28, 18, 6))
    c.set(71, 20, (255, 250, 220)); c.set(72, 19, (255, 250, 220))
    ys, xs = np.nonzero(eye[0])
    for x, y in zip(xs, ys):
        if c.px[y, x, 0] > 200:
            c.emit[y, x] = (*c.px[y, x, :3], 255)
    # frente fruncida y verrugas
    c.line((68, 18), (77, 18), skin[4]); c.line((67, 19), (68, 18), skin[3])
    for (x, y) in [(63, 25), (66, 33), (81, 30), (60, 28)]:
        c.set(x, y, skin[4]); c.set(x, y + 1, skin_dk[1])
    c.set(70, 17, wet); c.set(69, 17, wet)

    # ---- baba y agua que escurre ---------------------------------------------------------------------------------------
    for x, y in [(80, 47), (86, 46), (72, 64), (60, 56)]:
        c.set(x, y, slime); c.set(x, y + 1, mix(slime, (40, 80, 80), 0.5))
    c.line((77, 44), (77, 54), mix(slime, (60, 110, 100), 0.4))
    c.line((84, 42), (84, 47), mix(slime, (60, 110, 100), 0.4))
    return c


if __name__ == "__main__":
    import os
    from ahogado import moody_preview
    c = build()
    out = os.path.join(os.path.dirname(__file__), "_preview")
    os.makedirs(out, exist_ok=True)
    c.save(os.path.join(out, "profundo_x1.png"))
    moody_preview(c, 6).save(os.path.join(out, "profundo_x6.png"))
    print("ok")
