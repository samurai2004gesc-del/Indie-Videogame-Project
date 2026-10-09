"""El Ojo del Vacio - diseno v2. Globo ocular volador con corona de espinas y tentaculos. Lienzo 96x96, mira a la derecha."""
import math
import numpy as np
from pxl import *

W = H = 96


def build():
    c = Canvas(W, H)
    c.set_light(-0.55, 0.7, 0.5)

    flesh = ramp("#5b3a72", 6, 0.12, 0.14, 1.45)
    flesh_dk = ramp("#33204a", 5, 0.12, 0.20, 1.30)
    sclera = ramp("#cdb8ad", 6, 0.08, 0.30, 1.22)
    lid = ramp("#7a4468", 5, 0.10, 0.22, 1.35)
    bone = ramp("#d3c4a6", 4, 0.06, 0.40, 1.20)
    iris_r = (205, 24, 40)
    vein = (150, 30, 50)

    n_f = noise2d(W, H, 4, seed=6, octaves=2)

    # ---- corona de espinas en el lado de atras/arriba (al fondo) ------------------------------
    cx, cy = 50, 46
    spines = [(-170, 30), (-150, 34), (-128, 33), (-108, 30), (-88, 26), (-64, 22), (170, 28), (150, 30)]
    for ang, L in spines:
        a = math.radians(ang)
        bx, by = cx + math.cos(a) * 12, cy + math.sin(a) * 12
        tx, ty = cx + math.cos(a) * (12 + L), cy + math.sin(a) * (12 + L * 0.9)
        # espina curvada: Bezier con control desplazado
        ctrl = (bx + (tx - bx) * 0.5 + math.sin(a) * 5, by + (ty - by) * 0.5 - math.cos(a) * 5)
        pts = bezier_pts((bx, by), ctrl, (tx, ty), 14)
        c.shade(tentacle(W, H, pts, 3.8, 0.6), flesh, noise=n_f, noise_amt=0.2, outline=True, dither=0.2)
        c.set(round(tx), round(ty), bone[3])
        # punta de hueso
        for i in range(10, 14):
            c.set(round(pts[i][0]), round(pts[i][1]), bone[2 if i < 12 else 3])

    # ---- tentaculos colgantes con ventosas ---------------------------------------------------------
    tents = [((44, 58), (36, 72), (30, 90)), ((48, 60), (46, 76), (40, 94)), ((52, 61), (56, 77), (52, 95)),
             ((56, 58), (66, 72), (66, 92)), ((41, 55), (28, 66), (18, 80)), ((60, 55), (74, 64), (80, 78)),
             ((47, 59), (40, 70), (22, 76))]
    for k, (a, ctrl, b) in enumerate(tents):
        pts = bezier_pts(a, ctrl, b, 28)
        c.shade(tentacle(W, H, pts, 5.0 - (k % 3) * 0.5, 1.1), flesh, noise=n_f, noise_amt=0.25, outline=True, dither=0.2)
        # ventosas palidas en la cara interior
        for i in range(4, 27, 3):
            x, y = pts[i]
            c.set(round(x), round(y) + 1, lid[4]); c.set(round(x) + 1, round(y) + 1, lid[3])
        # brillo humedo
        for i in range(3, 20, 5):
            x, y = pts[i]
            c.set(round(x) - 1, round(y) - 1, flesh[5])

    # ---- globo ocular ------------------------------------------------------------------------------------
    eye = ellipse(W, H, cx, cy, 17, 17)
    c.shade(eye, sclera, noise=n_f, noise_amt=0.1, dither=0.35, rim=(255, 190, 170), rim_amt=0.28, contrast=1.15, bias=-0.08)
    # venas rojas ramificadas desde la parte de atras hacia el iris
    for (sx, sy, ex, ey) in [(36, 40, 50, 42), (35, 48, 48, 47), (38, 55, 50, 51), (40, 34, 52, 39), (42, 60, 54, 55),
                             (36, 44, 44, 38), (37, 52, 44, 56)]:
        c.line((sx, sy), (ex, ey), vein)
        # ramita
        mx, my = (sx + ex) // 2, (sy + ey) // 2
        c.line((mx, my), (mx + 3, my - 3), mix(vein, sclera[3], 0.35)); c.line((mx, my), (mx + 3, my + 3), mix(vein, sclera[3], 0.35))
    # iris rojo: mira a la derecha (elipse comprimida)
    iris = ellipse(W, H, 61, 46, 8.5, 10.5)
    c.shade(iris, [(70, 6, 22), (130, 12, 32), iris_r, (240, 80, 50), (255, 150, 80)], outline=True, dither=0.4, contrast=1.1)
    # estrias radiales del iris
    for k in range(12):
        a = k * math.pi / 6
        c.set(round(61 + math.cos(a) * 6.2), round(46 + math.sin(a) * 7.6), (255, 130, 70))
    # pupila vertical en rendija
    pup = ellipse(W, H, 62, 46, 2.3, 7.4)
    c.shade(pup, [(2, 0, 6), (8, 0, 12), (16, 2, 18)], outline=False)
    # reflejo de luz humeda
    c.set(57, 41, (255, 250, 235)); c.set(58, 40, (255, 250, 235)); c.set(57, 42, (255, 225, 205)); c.set(56, 41, (255, 225, 205))
    ys, xs = np.nonzero(iris[0] & ~pup[0])
    for x, y in zip(xs, ys):
        if c.px[y, x, 0] > 180:
            c.emit[y, x] = (*c.px[y, x, :3], 255)

    # ---- parpados carnosos: ceja enfadada arriba y bolsa inferior -----------------------------------
    upper = polygon(W, H, [(34, 34), (40, 28), (52, 26), (66, 34), (74, 40), (62, 36), (50, 34), (40, 37)], bevel=2.0)
    c.shade(upper, lid, noise=n_f, noise_amt=0.2, rim=(255, 160, 150), rim_amt=0.2)
    lower = polygon(W, H, [(36, 58), (46, 64), (58, 64), (70, 56), (62, 61), (50, 62), (40, 59)], bevel=2.0)
    c.shade(lower, lid, noise=n_f, noise_amt=0.2)
    # arrugas
    c.line((40, 31), (60, 31), lid[1]); c.line((44, 29), (56, 29), lid[3])
    # cicatrices y grietas
    c.line((34, 44), (30, 42), flesh_dk[1]); c.line((33, 50), (29, 53), flesh_dk[1])

    # ---- lagrimas de icor negro y motas ---------------------------------------------------------------------
    c.line((70, 58), (70, 66), (30, 24, 48)); c.set(70, 67, (50, 40, 70)); c.set(70, 68, (50, 40, 70))
    for (x, y) in [(76, 30), (20, 34), (82, 52)]:
        c.glow_set(x, y, (180, 60, 220))
    return c


if __name__ == "__main__":
    import os
    from ahogado import moody_preview
    c = build()
    out = os.path.join(os.path.dirname(__file__), "_preview")
    os.makedirs(out, exist_ok=True)
    c.save(os.path.join(out, "ojo_x1.png"))
    moody_preview(c, 6).save(os.path.join(out, "ojo_x6.png"))
    print("ok")
