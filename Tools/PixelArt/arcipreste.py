"""El Arcipreste de las Mareas - diseno v2 (jefe). Lienzo 192x192, pies en y=184, mira a la derecha.
Obispo batracio colosal: mitra dorada con el Signo, casulla carmesi, barba de tentaculos y baculo con ojo de gema."""
import math
import numpy as np
from pxl import *

W = H = 192
GROUND = 184


def build():
    c = Canvas(W, H)
    c.set_light(-0.6, 0.7, 0.5)

    crim = ramp("#7b1a2b", 6, 0.12, 0.14, 1.45)
    crim_dk = ramp("#4a1020", 5, 0.12, 0.20, 1.30)
    gold = ramp("#b8892a", 6, 0.12, 0.16, 1.60)
    gold_dk = ramp("#6c4e1e", 5, 0.10, 0.22, 1.35)
    skin = ramp("#365f5c", 6, 0.13, 0.14, 1.40)
    skin_dk = ramp("#21404a", 5, 0.12, 0.20, 1.30)
    alb = ramp("#6b7268", 6, 0.08, 0.18, 1.35)
    wood = ramp("#3e2a1c", 5, 0.08, 0.25, 1.30)
    pearl = ramp("#dcd6c4", 4, 0.05, 0.50, 1.15)
    teal_dk, teal_lo, teal_mid, teal_hi, teal_glow = (8, 52, 66), (16, 100, 112), (34, 170, 168), (92, 232, 214), (170, 255, 240)
    eye_y = (255, 224, 90)

    n_cloth = noise2d(W, H, 7, seed=2, octaves=3)
    n_rough = noise2d(W, H, 4, seed=7, octaves=2)
    n_skin = noise2d(W, H, 4, seed=13, octaves=2)

    # =====================================================================================
    # BACULO (al fondo, a la derecha): palo de madera con bandas, espiral dorada y ojo de gema
    # =====================================================================================
    sx = 160
    c.shade(rect(W, H, sx - 2, 30, sx + 3, 185, 1.6), wood, noise=n_rough, noise_amt=0.2, outline=True)
    for y in (48, 78, 112, 146):
        c.shade(rect(W, H, sx - 3.5, y, sx + 4.5, y + 4, 1.2), gold, outline=True)
        c.set(sx, y + 1, gold[5])
    # espiral (voluta) del baculo
    for t in np.linspace(0, 1, 120):
        ang = math.pi * 1.5 + t * math.pi * 2.6
        rad = 15 - t * 9
        x, y = sx - 1 + math.cos(ang) * rad, 24 + math.sin(ang) * rad * 0.95
        c.disc(x, y, 2.4 - t * 0.9, gold[4] if t < 0.5 else gold[3])
        c.set(round(x) - 2, round(y) - 2, gold[5])
    c.shade(polygon(W, H, [(sx - 4, 38), (sx + 5, 38), (sx + 6, 46), (sx - 5, 46)], bevel=1.4), gold, outline=True)
    # ojo de gema verdosa (emisivo)
    gem = ellipse(W, H, sx - 1, 21, 5.2, 5.2)
    c.shade(gem, [teal_dk, teal_lo, teal_mid, teal_hi, teal_glow], outline=True, dither=0.3)
    c.set(sx - 3, 19, (240, 255, 250)); c.set(sx - 2, 18, (230, 255, 248))
    ys, xs = np.nonzero(gem[0])
    for x, y in zip(xs, ys):
        c.emit[y, x] = (*c.px[y, x, :3], 255)

    # =====================================================================================
    # TENTACULO que asoma bajo la capa por la izquierda (al fondo)
    # =====================================================================================
    pts = bezier_pts((52, 150), (24, 140), (22, 108), 34)
    c.shade(tentacle(W, H, pts, 8.5, 2.0), skin_dk, noise=n_skin, noise_amt=0.3, outline=True, dither=0.2)
    for i in range(3, 32, 3):
        x, y = pts[i]
        c.disc(x + 2, y + 1, 1.3, skin[4]); c.set(round(x), round(y) - 1, skin[5])

    # =====================================================================================
    # ALBA (tunica gris por debajo): flecos de alga
    # =====================================================================================
    alb_pts = [(58, 130), (132, 130), (140, 160), (146, 184), (46, 184), (50, 158)]
    c.shade(polygon(W, H, alb_pts, bevel=10, power=0.9), alb, noise=n_cloth, noise_amt=0.65, rim=(190, 215, 205), rim_amt=0.18, dither=0.25)
    for pts2, col in [([(66, 140), (60, 160), (58, 182)], alb[0]), ([(86, 142), (84, 162), (84, 183)], alb[1]),
                      ([(108, 142), (112, 162), (118, 183)], alb[1]), ([(126, 140), (132, 160), (138, 182)], alb[0])]:
        for a, b in zip(pts2[:-1], pts2[1:]):
            c.line(a, b, col)
    # dobladillo con flecos de alga y barro
    for x in range(48, 146, 4):
        yy = 182 + (x % 3 == 0)
        c.line((x, 178), (x, yy), alb[0]); c.set(x, yy + 1 if x % 2 else yy, skin_dk[1])
    for (x, y) in [(56, 176), (74, 179), (100, 178), (126, 177), (140, 180)]:
        c.line((x, y), (x + 2, y + 4), skin[3]); c.set(x + 2, y + 4, skin[1])
    c.contact_shadow(GROUND, 30, 160, alpha=160)

    # =====================================================================================
    # CASULLA CARMESI (poncho ceremonial) con orla dorada y banda central con el Signo
    # =====================================================================================
    ch = [(78, 74), (118, 74), (136, 92), (146, 122), (150, 152), (140, 150), (134, 158), (122, 151), (110, 158),
          (98, 151), (86, 158), (74, 151), (62, 158), (52, 150), (46, 152), (50, 120), (60, 92)]
    cas = polygon(W, H, ch, bevel=9, power=0.9)
    c.shade(cas, crim, noise=n_cloth, noise_amt=0.7, rim=(255, 120, 110), rim_amt=0.22, dither=0.3, contrast=1.1)
    # pliegues grandes
    for pts2, col in [([(70, 96), (60, 124), (56, 150)], crim[1]), ([(82, 98), (76, 126), (74, 152)], crim_dk[2]),
                      ([(110, 98), (118, 126), (122, 150)], crim[1]), ([(124, 100), (134, 128), (140, 150)], crim[1]),
                      ([(66, 92), (58, 118), (54, 142)], crim[4])]:
        for a, b in zip(pts2[:-1], pts2[1:]):
            c.line(a, b, col)
    # orla dorada: contorno del borde inferior y laterales
    for a, b in zip(ch[4:-1], ch[5:]):
        pass
    for (x0, y0), (x1, y1) in zip(ch[2:6], ch[3:7]):
        c.line((x0, y0), (x1, y1), gold[3])
    for (x0, y0), (x1, y1) in zip(ch[-5:-1], ch[-4:]):
        c.line((x0, y0), (x1, y1), gold[2])
    # banda central (orfrey) con bordado de estrellas
    band = polygon(W, H, [(92, 76), (108, 76), (112, 152), (88, 152)], bevel=3.4)
    c.shade(band, gold_dk, noise=n_rough, noise_amt=0.25, outline=True, dither=0.2)
    c.line((93, 78), (90, 150), gold[3]); c.line((107, 78), (110, 150), gold[3])
    for y in (96, 116, 136):
        sx2, sy2 = 100, y
        pts3 = [(sx2 + math.cos(-math.pi / 2 + k * 2 * math.pi / 5) * 5.4, sy2 + math.sin(-math.pi / 2 + k * 2 * math.pi / 5) * 5.4) for k in range(5)]
        for k in range(5):
            c.line(pts3[k], pts3[(k + 2) % 5], gold[5])
        c.set(sx2, sy2, teal_hi)
    # tachones de oro en la orla
    for x in range(54, 148, 6):
        c.set(x, 150 + (x % 12 == 0), gold[4])

    # La cabeza se dibuja en su propio lienzo y se pega mas abajo, para que la mitra no tape los ojos.
    HEAD_DY = 17
    hd = Canvas(W, H)
    hd.set_light(-0.6, 0.7, 0.5)
    # =====================================================================================
    # CUELLO + CABEZA batracia (adelantada, encorvada)
    # =====================================================================================
    hd.shade(capsule(W, H, (98, 80), (116, 68), 17, 14), skin_dk, noise=n_skin, noise_amt=0.3)
    # aletas branquiales detras de la mandibula
    fin = ramp("#5a3d78", 5, 0.12, 0.20, 1.50)
    for (x1, y1) in [(86, 54), (84, 62), (86, 71), (90, 80)]:
        hd.line((106, 66), (x1, y1), fin[4]); hd.line((106, 67), (x1, y1 + 1), fin[2])
    hd.shade(polygon(W, H, [(106, 60), (86, 54), (84, 62), (86, 71), (90, 80), (106, 74)], bevel=3), fin, outline=False, dither=0.3)
    # craneo ancho
    head = ellipse(W, H, 118, 64, 25, 18, rot=-6)
    hd.shade(head, skin, noise=n_skin, noise_amt=0.3, rim=(150, 240, 210), rim_amt=0.22, dither=0.3)
    m = head[0] & hd.alpha_mask()
    hd.px[m & (n_skin > 0.62), :3] = np.array(skin[1], np.uint8)
    hd.px[m & (n_skin < 0.25), :3] = np.array(skin[4], np.uint8)
    # hocico y mandibula
    muz = polygon(W, H, [(120, 62), (146, 62), (156, 68), (156, 75), (146, 79), (122, 79)], bevel=5, power=0.9)
    hd.shade(muz, skin, noise=n_skin, noise_amt=0.25, rim=(150, 240, 210), rim_amt=0.2)
    jaw = polygon(W, H, [(122, 77), (152, 76), (154, 84), (142, 90), (124, 86)], bevel=3.2)
    hd.shade(jaw, skin_dk, noise=n_skin, noise_amt=0.2)
    mouth = polygon(W, H, [(124, 75), (153, 74), (152, 79), (126, 80)], bevel=1.5)
    hd.shade(mouth, [(24, 6, 14), (60, 12, 26), (110, 22, 36), (150, 44, 56)], outline=False)
    for x in range(126, 153, 3):
        hd.line((x, 75), (x, 78 + (x % 6 == 0)), pearl[3]); hd.set(x, 78, pearl[2])
    for x in range(128, 150, 5):
        hd.line((x, 81), (x, 78), pearl[2])
    hd.set(152, 66, skin_dk[1]); hd.set(153, 66, skin_dk[1]); hd.set(152, 67, skin_dk[1])
    # ojo trasero y ojo delantero saltones (amarillos con pupila rendija)
    hd.shade(ellipse(W, H, 105, 56, 6.4, 6.2), skin_dk, outline=True)
    hd.shade(ellipse(W, H, 105.5, 56, 4.4, 4.4), [(70, 52, 10), (120, 92, 20), (170, 140, 28)], outline=False)
    hd.shade(ellipse(W, H, 128, 58, 9.2, 9.0), [skin_dk[0], skin_dk[1], skin[2], skin[3], skin[4]], outline=True)
    eye = ellipse(W, H, 129, 58.5, 7.0, 7.0)
    hd.shade(eye, [(120, 90, 20), (190, 150, 30), (240, 200, 60), eye_y], outline=False, dither=0.3)
    for y in range(52, 66):
        hd.set(131, y, (28, 18, 6)); hd.set(130, y if 54 < y < 63 else 0, (28, 18, 6))
    hd.set(124, 53, (255, 250, 220)); hd.set(125, 52, (255, 250, 220)); hd.set(124, 54, (255, 235, 190))
    ys, xs = np.nonzero(eye[0])
    for x, y in zip(xs, ys):
        if hd.px[y, x, 0] > 200:
            hd.emit[y, x] = (*hd.px[y, x, :3], 255)
    # parpado pesado
    hd.line((120, 51), (138, 51), skin[4]); hd.line((118, 52), (121, 50), skin[3])

    # barba de tentaculos colgando de la barbilla sobre la casulla
    beard = [((130, 88), (122, 104), (112, 124)), ((134, 89), (132, 108), (126, 130)), ((138, 88), (146, 106), (150, 126)),
             ((126, 88), (114, 100), (100, 116)), ((141, 86), (154, 98), (162, 110))]
    for k, (a, ctrl, b) in enumerate(beard):
        pts = bezier_pts(a, ctrl, b, 24)
        hd.shade(tentacle(W, H, pts, 5.2 - (k % 2) * 0.8, 1.3), skin, noise=n_skin, noise_amt=0.3, outline=True, dither=0.2,
                rim=(150, 240, 210), rim_amt=0.15)
        for i in range(3, 22, 3):
            x, y = pts[i]
            hd.set(round(x) + 1, round(y) + 1, skin[5]); hd.set(round(x), round(y) + 1, skin_dk[1])
    # perlas enredadas en la barba
    for (x, y) in [(124, 106), (134, 110), (146, 108), (112, 106), (128, 120)]:
        hd.disc(x, y, 1.9, pearl[3]); hd.set(round(x) - 1, round(y) - 1, pearl[3])

    c.blit(hd, 0, HEAD_DY)

    # =====================================================================================
    # MITRA dorada: alta, con el Signo en un medallon y infulas carmesi
    # =====================================================================================
    # La mitra tambien va en su propio lienzo y se pega 8 px mas abajo, asentada sobre la frente.
    mt = Canvas(W, H)
    mt.set_light(-0.6, 0.7, 0.5)
    # infulas (cintas colgando por detras)
    for (x, y) in [(98, 52), (104, 53)]:
        mt.shade(polygon(W, H, [(x, y), (x + 6, y), (x - 6, y + 34), (x - 12, y + 32)], bevel=1.8), crim, noise=n_cloth, noise_amt=0.3, outline=True)
        mt.line((x - 12, y + 32), (x - 6, y + 34), gold[3])
    mit = [(94, 56), (146, 56), (146, 46), (140, 32), (132, 20), (122, 10), (118, 4), (113, 11), (106, 20), (98, 32), (94, 46)]
    mt.shade(polygon(W, H, mit, bevel=9, power=0.9), gold, noise=n_rough, noise_amt=0.14, rim=(255, 235, 170), rim_amt=0.3,
            contrast=1.2, bias=-0.10, dither=0.3)
    # bandas: circulo inferior y titulo vertical
    band = polygon(W, H, [(93, 52), (147, 52), (147, 60), (93, 60)], bevel=2.6)
    mt.shade(band, gold_dk, noise=n_rough, noise_amt=0.2, outline=True)
    for x in range(98, 146, 6):
        mt.disc(x, 56, 1.5, teal_mid if (x // 6) % 2 else crim[3]); mt.set(x - 1, 55, (255, 250, 230))
    mt.line((119, 10), (119, 52), gold_dk[1]); mt.line((120, 10), (120, 52), gold[4])
    mt.line((108, 22), (112, 52), gold_dk[1]); mt.line((131, 22), (127, 52), gold_dk[1])
    # medallon con el Signo Antiguo (estrella verde brillante)
    mt.shade(ellipse(W, H, 120, 34, 10.5, 10.5), gold_dk, outline=True, contrast=1.1)
    mt.shade(ellipse(W, H, 120, 34, 8.2, 8.2), [teal_dk, teal_lo, teal_mid], outline=False)
    star = [(120 + math.cos(-math.pi / 2 + k * 2 * math.pi / 5) * 7, 34 + math.sin(-math.pi / 2 + k * 2 * math.pi / 5) * 7) for k in range(5)]
    for k in range(5):
        mt.line(star[k], star[(k + 2) % 5], teal_glow, glow=True)
    mt.glow_set(120, 34, teal_glow)
    # corona de picos en la cumbre + joya
    mt.disc(119, 4, 2.4, teal_hi, glow=True); mt.set(118, 3, (240, 255, 250))

    c.blit(mt, 0, 8)

    am = Canvas(W, H)
    am.set_light(-0.6, 0.7, 0.5)
    # =====================================================================================
    # BRAZO DELANTERO: manga carmesi con puño dorado y mano palmeada agarrando el baculo
    # =====================================================================================
    am.shade(capsule(W, H, (130, 104), (148, 114), 12, 9), crim, noise=n_cloth, noise_amt=0.5, rim=(255, 120, 110), rim_amt=0.2)
    sleeve = polygon(W, H, [(132, 108), (152, 110), (160, 122), (152, 134), (138, 130)], bevel=7)
    am.shade(sleeve, crim, noise=n_cloth, noise_amt=0.5, rim=(255, 120, 110), rim_amt=0.22)
    am.shade(polygon(W, H, [(140, 124), (158, 120), (160, 130), (144, 136)], bevel=2.6), gold, outline=True)
    for x in range(141, 157, 4):
        am.set(x, 126, gold[5]); am.set(x + 1, 131, gold_dk[0])
    # mano grande con garras que abraza el baculo
    hand = polygon(W, H, [(154, 120), (166, 118), (172, 126), (168, 135), (156, 134)], bevel=3.2)
    am.shade(hand, skin, noise=n_skin, noise_amt=0.25, rim=(150, 240, 210), rim_amt=0.2)
    for (x0, y0, x1, y1) in [(160, 119, 168, 114), (165, 124, 172, 122), (166, 130, 172, 132), (162, 135, 168, 140)]:
        am.line((x0, y0), (x1, y1), pearl[3]); am.set(x1, y1, pearl[2])
    am.line((156, 122), (162, 132), skin[1]); am.line((158, 120), (164, 126), skin[1])

    c.blit(am, 0, 10)

    # =====================================================================================
    # Detalles finales: gotas de agua y brillo humedo
    # =====================================================================================
    for (x, y) in [(150, 126), (120, 160), (60, 152), (98, 160)]:
        c.set(x, y, (90, 140, 150)); c.set(x, y + 2, (60, 100, 120))
    for (x, y) in [(112, 58), (104, 52), (92, 66)]:
        c.set(x, y, (200, 240, 230))
    return c


if __name__ == "__main__":
    import os
    from ahogado import moody_preview
    c = build()
    out = os.path.join(os.path.dirname(__file__), "_preview")
    os.makedirs(out, exist_ok=True)
    c.save(os.path.join(out, "arcipreste_x1.png"))
    moody_preview(c, 4).save(os.path.join(out, "arcipreste_x4.png"))
    print("ok")
