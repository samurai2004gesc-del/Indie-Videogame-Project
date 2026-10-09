"""El Sectario de Dagon - diseno v2. Lienzo 96x96, pies en y=88, mira a la derecha.
Capucha alta y puntiaguda, mascara dorada de pez, tunica con dobladillo de olas y baculo con orbe."""
import math
import numpy as np
from pxl import *

W = H = 96
GROUND = 88


def build():
    c = Canvas(W, H)
    c.set_light(-0.6, 0.7, 0.45)

    robe = ramp("#47306a", 6, 0.12, 0.14, 1.40)
    robe_dk = ramp("#2a1e46", 5, 0.12, 0.20, 1.30)
    gold = ramp("#b58a2c", 6, 0.11, 0.18, 1.55)
    gold_dk = ramp("#6e5220", 5, 0.10, 0.22, 1.35)
    wood = ramp("#4a3322", 5, 0.08, 0.25, 1.30)
    skin = ramp("#7f9a86", 4, 0.07, 0.35, 1.25)
    pearl = ramp("#d8d2c0", 4, 0.05, 0.50, 1.15)
    orb_core = (255, 190, 90)
    orb_mid = (255, 120, 40)
    orb_vio = (150, 60, 210)
    orb_dk = (60, 20, 90)

    n_cloth = noise2d(W, H, 5, seed=5, octaves=2)
    n_rough = noise2d(W, H, 3, seed=8, octaves=2)

    # ---- baculo (al fondo): palo de madera con bandas de laton -------------------------------
    c.shade(rect(W, H, 70, 12, 73, 89, 1.2), wood, noise=n_rough, noise_amt=0.2, outline=True)
    for y in (24, 40, 56, 72):
        c.shade(rect(W, H, 69, y, 74, y + 2, 0.8), gold, outline=False)
    for y in range(14, 88, 3):
        c.set(71, y, wood[4])

    # ---- tunica larga (cae hasta el suelo, mas ancha abajo) ------------------------------------------
    robe_pts = [(39, 38), (55, 38), (59, 52), (63, 70), (66, 88), (28, 88), (31, 70), (35, 52)]
    c.shade(polygon(W, H, robe_pts, bevel=7, power=0.9), robe, noise=n_cloth, noise_amt=0.6,
            rim=(190, 140, 255), rim_amt=0.22, dither=0.25)
    # pliegues largos
    for pts, col in [([(42, 52), (38, 70), (35, 86)], robe[0]), ([(48, 54), (46, 72), (46, 87)], robe[1]),
                     ([(54, 54), (56, 72), (59, 86)], robe[1]), ([(44, 50), (42, 66), (41, 80)], robe[4])]:
        for a, b in zip(pts[:-1], pts[1:]):
            c.line(a, b, col)
    # sombra de contacto
    c.contact_shadow(GROUND, 24, 70, alpha=150)

    # dobladillo con olas doradas
    hem = polygon(W, H, [(29, 82), (65, 82), (66, 88), (28, 88)], bevel=1.6)
    c.shade(hem, gold_dk, noise=n_rough, noise_amt=0.2, outline=False)
    for x in range(30, 66, 5):
        c.set(x, 83, gold[4]); c.set(x + 1, 82, gold[3]); c.set(x + 2, 83, gold[4]); c.set(x + 3, 84, gold[2])
        c.set(x, 86, gold[1]); c.set(x + 2, 85, gold[2]); c.set(x + 4, 86, gold[1])
    c.hline(28, 66, 88, robe_dk[0])

    # bordados de escamas en el pecho (patron simple en V)
    for r in range(4):
        for k in range(5 - r % 2):
            x = 40 + k * 4 + (r % 2) * 2
            y = 56 + r * 4
            c.set(x, y, gold_dk[3]); c.set(x + 1, y + 1, gold_dk[2]); c.set(x - 1, y + 1, gold_dk[2])

    # cordon dorado a la cintura con borlas
    belt = polygon(W, H, [(34, 56), (60, 56), (61, 60), (33, 60)], bevel=1.4)
    c.shade(belt, gold, outline=True)
    for x in range(35, 60, 3):
        c.set(x, 57, gold[5]); c.set(x + 1, 59, gold[1])
    c.line((56, 60), (58, 72), gold[3]); c.line((57, 60), (59, 72), gold[1])
    c.disc(58.5, 73.5, 1.6, gold[4]); c.set(58, 72, gold[5])

    # rosario de perlas colgando del cuello
    beads = bezier_pts((42, 42), (47, 62), (56, 44), 14)
    for i, (x, y) in enumerate(beads):
        c.disc(x, y, 1.1, pearl[3] if i % 3 else pearl[2])
        c.set(round(x) - 1, round(y) - 1, pearl[3])
    c.line((49, 54), (49, 62), pearl[1]); c.disc(49, 63, 1.5, gold[3]); c.set(48, 62, gold[5])

    # ---- brazo trasero (manga ancha en sombra) -----------------------------------------------------------
    c.shade(capsule(W, H, (41, 42), (37, 56), 4.2, 3.4), robe_dk, noise=n_cloth, noise_amt=0.4)
    c.shade(polygon(W, H, [(34, 52), (41, 53), (43, 64), (37, 66), (33, 62)], bevel=2.6), robe_dk, noise=n_cloth, noise_amt=0.4)

    # ---- brazo delantero extendido hacia el baculo ---------------------------------------------------------
    c.shade(capsule(W, H, (54, 42), (62, 50), 4.4, 3.8), robe, noise=n_cloth, noise_amt=0.4, rim=(190, 140, 255), rim_amt=0.2)
    sleeve = polygon(W, H, [(55, 46), (66, 50), (70, 56), (66, 62), (56, 58)], bevel=3.0)
    c.shade(sleeve, robe, noise=n_cloth, noise_amt=0.4, rim=(190, 140, 255), rim_amt=0.2)
    c.hline(56, 69, 57, gold[3]); c.hline(56, 69, 58, gold_dk[1])
    # mano palida agarrando el baculo
    c.shade(ellipse(W, H, 70.5, 55, 2.8, 2.8), skin, noise=n_rough, noise_amt=0.2)
    for y in (53, 55, 57):
        c.set(72, y, skin[1])
    c.set(69, 54, skin[3])

    # ---- capucha alta y puntiaguda ----------------------------------------------------------------------------
    hood_pts = [(37, 42), (33, 32), (35, 21), (40, 12), (45, 5), (48, 2), (50, 8), (51, 15), (55, 23), (57, 33), (56, 42)]
    hood = polygon(W, H, hood_pts, bevel=6.0, power=0.9)
    c.shade(hood, robe, noise=n_cloth, noise_amt=0.5, rim=(190, 140, 255), rim_amt=0.28, dither=0.25)
    # pliegues de la capucha
    c.line((42, 14), (38, 30), robe[0]); c.line((44, 12), (41, 28), robe[1]); c.line((47, 6), (46, 18), robe[4])
    # punta caida con perla
    c.line((48, 2), (50, 4), robe[2]); c.disc(50.5, 5.2, 1.5, pearl[3]); c.set(50, 4, pearl[4] if len(pearl) > 4 else pearl[3])
    # reborde dorado del rostro
    c.line((52, 17), (56, 24), gold[3]); c.line((56, 24), (57, 33), gold[3]); c.line((57, 33), (56, 41), gold[2])
    c.line((51, 17), (55, 24), gold[1])
    # hueco de la capucha (oscuridad)
    face = ellipse(W, H, 51.5, 29.5, 5.6, 9.5, rot=-6)
    c.shade(face, [(8, 4, 16), (14, 8, 28), (24, 14, 44), (36, 22, 58)], outline=False)
    # mascara de pez dorada (ovalo con aletas laterales y ranuras de branquia)
    mask = ellipse(W, H, 52.6, 29.5, 4.6, 7.4, rot=-6)
    c.shade(mask, gold, noise=n_rough, noise_amt=0.1, rim=(255, 235, 170), rim_amt=0.3, contrast=1.1, bias=-0.06)
    # branquias y aletas
    for dy in (-1, 1, 3):
        c.line((50, 29 + dy), (51, 31 + dy), gold_dk[0])
    c.line((56, 22), (59, 19), gold[4]); c.line((56, 36), (59, 39), gold[3])   # aletas
    # ojo del pez: brasa ambar
    c.disc(54, 26, 1.8, (60, 20, 6)); c.disc(54, 26, 1.1, orb_mid, glow=True); c.set(54, 26, orb_core, 255)
    c.emit[26, 54] = (*orb_core, 255)
    # boca/ranura y barbilla
    c.line((52, 34), (55, 34), gold_dk[0]); c.set(55, 35, gold_dk[1])

    # ---- cabeza del baculo: aro dorado con Signo Antiguo y orbe ---------------------------------------------------
    cx, cy = 71.5, 8.0
    ring_o, _ = ellipse(W, H, cx, cy, 7.4, 7.4)
    ring_i, _ = ellipse(W, H, cx, cy, 4.9, 4.9)
    ring_shape = Shape(ring_o & ~ring_i, np.ones((H, W), np.float32) * 0.8, 2.2)
    c.shade(ring_shape, gold, outline=True, contrast=1.1, bias=-0.04)
    # estrella de cinco puntas dentro del aro (Signo Antiguo)
    pts = []
    for k in range(5):
        a = -math.pi / 2 + k * 2 * math.pi / 5
        pts.append((cx + math.cos(a) * 4.2, cy + math.sin(a) * 4.2))
    for k in range(5):
        a, b = pts[k], pts[(k + 2) % 5]
        c.line(a, b, gold[4])
    # orbe que flota en el centro
    orb = ellipse(W, H, cx, cy, 3.1, 3.1)
    c.shade(orb, [orb_dk, orb_vio, orb_mid, orb_core, (255, 240, 200)], outline=False, dither=0.3)
    ys, xs = np.nonzero(orb[0])
    for x, y in zip(xs, ys):
        c.emit[y, x] = (*c.px[y, x, :3], 255)
    # puntas que sujetan el aro al palo
    c.shade(polygon(W, H, [(69.5, 14), (73.5, 14), (73, 18), (70, 18)], bevel=1.0), gold, outline=True)
    # amuletos colgando (huesos de pez)
    for (x, y0, L) in [(65, 12, 7), (78, 12, 6)]:
        c.line((x, y0), (x, y0 + L), pearl[2]); c.set(x - 1, y0 + L, pearl[3]); c.set(x + 1, y0 + L, pearl[3])
    # chispas y brasas flotando alrededor del orbe
    for (x, y, col) in [(63, 3, orb_mid), (80, 4, orb_vio), (77, 0, orb_mid), (66, 17, orb_vio), (81, 13, orb_mid)]:
        c.glow_set(x, y, col)
    return c


if __name__ == "__main__":
    import os
    from ahogado import moody_preview
    c = build()
    out = os.path.join(os.path.dirname(__file__), "_preview")
    os.makedirs(out, exist_ok=True)
    c.save(os.path.join(out, "sectario_x1.png"))
    moody_preview(c, 6).save(os.path.join(out, "sectario_x6.png"))
    print("ok")
