"""El Ahogado - diseno v2 (pose de reposo, mira a la derecha). Lienzo 96x96, pies en y=88.
Altura total ~62 px (misma que el juego). Silueta esbelta: capa rota, abrigo largo de hule,
red de pesca al hombro, escafandra de laton envejecido y el Garfio de Devil Reef."""
import math
import numpy as np
from pxl import *

W = H = 96
GROUND = 88


def build():
    c = Canvas(W, H)
    c.set_light(-0.62, 0.68, 0.42)

    # ---- rampas (mas oscuras y apagadas que v1, con brillos pequenos) --------------
    brass = ramp("#8f6a26", 6, 0.12, 0.16, 1.62)
    brass_dk = ramp("#5e4620", 5, 0.10, 0.22, 1.35)
    coat = ramp("#2a4f57", 5, 0.10, 0.22, 1.30)
    coat_dk = ramp("#1b323c", 5, 0.10, 0.25, 1.25)
    cape = ramp("#7a1a27", 5, 0.09, 0.20, 1.40)
    leather = ramp("#5a3a22", 5, 0.09, 0.24, 1.30)
    steel = ramp("#7d8d99", 5, 0.08, 0.22, 1.45)
    rope = ramp("#8f7a52", 4, 0.07, 0.35, 1.25)
    verd = ramp("#2f7d6c", 4, 0.07, 0.35, 1.35)
    teal_glow = (150, 255, 238)
    teal_hi = (92, 232, 214)
    teal_mid = (34, 170, 168)
    teal_lo = (16, 100, 112)
    teal_dk = (8, 52, 66)

    n_cloth = noise2d(W, H, 5, seed=3, octaves=2)
    n_rough = noise2d(W, H, 3, seed=9, octaves=2)

    # =====================================================================================
    # CAPA ROTA (al fondo): triangulos largos que ondean hacia atras
    # =====================================================================================
    cape_pts = [(43, 40), (52, 41), (49, 56), (47, 72), (50, 82), (44, 76), (42, 86), (37, 77), (32, 84),
                (30, 72), (24, 76), (27, 62), (33, 52), (38, 44)]
    c.shade(polygon(W, H, cape_pts, bevel=5.5, power=0.9), cape, noise=n_cloth, noise_amt=0.65,
            rim=(255, 110, 96), rim_amt=0.22, dither=0.15)
    # pliegues verticales (sombra y luz)
    for pts, col in [([(40, 46), (36, 62), (36, 80)], cape[0]), ([(45, 48), (44, 66), (45, 78)], cape[0]),
                     ([(34, 56), (30, 68), (30, 74)], cape[1]), ([(41, 44), (38, 58), (38, 70)], cape[3])]:
        for a, b in zip(pts[:-1], pts[1:]):
            c.line(a, b, col)

    # =====================================================================================
    # PIERNAS Y BOTAS
    # =====================================================================================
    # pierna trasera (mas oscura, en sombra)
    back = polygon(W, H, [(41, 68), (47, 68), (47, 80), (48, 85), (51, 88), (38, 88), (39, 84), (40, 78)], bevel=2.6)
    c.shade(back, [shadow_of(x, 0.25) for x in leather], noise=n_rough, noise_amt=0.2)
    # pierna delantera adelantada
    front = polygon(W, H, [(47, 66), (54, 66), (55, 79), (58, 84), (63, 88), (49, 88), (50, 80), (47, 74)], bevel=2.6)
    c.shade(front, leather, noise=n_rough, noise_amt=0.2)
    # pantalon desgastado asomando bajo el faldon
    pants = polygon(W, H, [(41, 66), (55, 66), (55, 78), (48, 80), (41, 79)], bevel=2.5)
    c.shade(pants, coat_dk, noise=n_cloth, noise_amt=0.3)
    # punos de las botas (vuelta de cuero) con hebillas
    for x0, x1, y in [(39, 48, 79), (49, 58, 78)]:
        c.hline(x0, x1, y, leather[3]); c.hline(x0, x1, y + 1, leather[4])
        c.hline(x0, x1, y + 2, leather[0])
    c.set(44, 83, brass[4]); c.set(44, 84, brass[2]); c.set(54, 82, brass[4]); c.set(54, 83, brass[2])
    c.hline(57, 62, 87, leather[0])
    c.contact_shadow(GROUND, 32, 70, alpha=140)

    # =====================================================================================
    # ABRIGO LARGO DE HULE (faldon desgarrado, con abertura para ver las piernas)
    # =====================================================================================
    coat_pts = [(42, 40), (56, 40), (57, 50), (58, 62), (61, 73), (57, 76), (55, 72), (52, 77), (49, 71), (46, 77),
                (43, 71), (40, 75), (38, 66), (39, 52)]
    c.shade(polygon(W, H, coat_pts, bevel=5, power=0.9), coat, noise=n_cloth, noise_amt=0.55,
            rim=(110, 255, 235), rim_amt=0.2, dither=0.2)
    # cuello alto
    collar = polygon(W, H, [(42, 38), (56, 38), (55, 45), (49, 49), (43, 45)], bevel=2.6)
    c.shade(collar, coat_dk, noise=n_cloth, noise_amt=0.3)
    c.line((49, 49), (49, 72), coat[0])   # costura
    c.line((44, 52), (42, 70), coat[1]); c.line((55, 52), (57, 70), coat[1])
    for y in (50, 55, 60, 65):
        c.disc(52, y, 0.9, brass[3]); c.set(52, y - 1, brass[5])

    # --- red de pesca: una banda diagonal con malla (clara y legible) ---
    for k in range(0, 4):
        c.line((45 + k * 2, 44 + k), (54 + k * 1, 60 + k * 0), rope[2])
    for k in range(0, 4):
        c.line((55 - k * 2, 44 + k), (46 - k * 1, 60 + k * 0), rope[1])
    for (x, y) in [(49, 49), (51, 53), (53, 57), (47, 54), (50, 58)]:
        c.set(x, y, rope[3])

    # --- cinturon de cuerda + anzuelos ---
    belt = polygon(W, H, [(39, 62), (59, 62), (59, 65), (39, 65)], bevel=1.4)
    c.shade(belt, rope, noise=n_rough, noise_amt=0.3)
    for x in range(40, 59, 3):
        c.set(x, 63, rope[0]); c.set(x + 1, 64, rope[0])
    c.shade(rect(W, H, 48, 61, 53, 66, 1.4), brass, outline=True)
    c.set(50, 63, brass_dk[0])
    # anzuelos colgando
    for hx, hy in [(55, 66), (41, 66)]:
        c.line((hx, hy), (hx, hy + 4), steel[3])
        c.line((hx, hy + 4), (hx - 2, hy + 6), steel[2]); c.set(hx - 2, hy + 5, steel[4])
    # bolsa de cuero
    pouch = polygon(W, H, [(39, 64), (45, 64), (45, 71), (40, 72)], bevel=1.8)
    c.shade(pouch, leather, noise=n_rough, noise_amt=0.2)
    c.hline(40, 44, 66, leather[0]); c.set(42, 67, brass[3])

    # =====================================================================================
    # BRAZO TRASERO
    # =====================================================================================
    c.shade(capsule(W, H, (43, 43), (40, 54), 2.7, 2.3), coat_dk, noise=n_cloth, noise_amt=0.3)
    c.shade(capsule(W, H, (40, 54), (43, 61), 2.3, 2.0), coat_dk)
    c.shade(ellipse(W, H, 43.5, 62, 2.2, 2.0), leather)

    # =====================================================================================
    # EL GARFIO DE DEVIL REEF: espada con hoja en forma de anzuelo
    # =====================================================================================
    # empunadura envuelta en cuerda
    c.shade(capsule(W, H, (60, 52), (63, 63), 1.6, 1.6), leather, noise=n_rough, noise_amt=0.2)
    for y in (54, 56, 58, 60):
        c.set(61 + (y - 54) // 3, y, rope[3])
    # pomo con gema verdosa
    c.shade(ellipse(W, H, 59.5, 50.8, 2.3, 2.3), brass, outline=True)
    c.set(59, 50, verd[3]); c.set(59, 49, brass[5])
    # guarda curva con puntas hacia abajo
    guard = [(56, 63), (60, 62), (66, 62), (69, 64), (66, 63.5), (62, 64.5), (58, 65.5)]
    c.shade(polygon(W, H, guard, bevel=1.3), brass, outline=True)
    c.set(56, 64, brass[4]); c.set(69, 64, brass[3])
    # hoja: ancha en la base, se curva hacia el suelo y remata en un gancho con barba hacia arriba
    blade = [(62, 65), (66, 64), (71, 70), (76, 77), (81, 83), (86, 86), (91, 85), (92, 80), (89, 83), (86, 82),
             (84, 79), (80, 75), (74, 68), (68, 63)]
    c.shade(polygon(W, H, blade, bevel=2.0, power=0.9), steel, noise=n_rough, noise_amt=0.12,
            rim=(200, 245, 255), rim_amt=0.32)
    # canal (sangradura) y brillo del filo superior
    c.line((66, 66), (84, 84), steel[1]); c.line((65, 65), (80, 80), steel[4])
    c.line((69, 65), (81, 77), steel[3])
    # barba y punta del gancho
    c.set(92, 79, steel[4]); c.set(92, 80, steel[3]); c.set(91, 78, steel[4])
    # sal y oxido
    for x, y in [(71, 71), (75, 76), (79, 80), (84, 83)]:
        c.set(x, y, mix(steel[2], (150, 90, 50), 0.55))
    # gotas de agua negra cayendo de la hoja
    c.set(78, 84, (40, 70, 90)); c.set(78, 86, (30, 55, 75))

    # =====================================================================================
    # BRAZO DELANTERO + MANO
    # =====================================================================================
    c.shade(capsule(W, H, (54, 42), (58, 52), 3.0, 2.5), coat, noise=n_cloth, noise_amt=0.3, rim=(110, 255, 235), rim_amt=0.2)
    c.shade(capsule(W, H, (58, 52), (61, 58), 2.5, 2.1), coat)
    c.shade(ellipse(W, H, 61.5, 59, 2.8, 2.6), leather, noise=n_rough, noise_amt=0.2)
    c.hline(58, 63, 56, brass[3])

    # hombrera de laton con percebes y alga
    pauld = ellipse(W, H, 55.5, 42.5, 6.4, 4.6, rot=-20)
    c.shade(pauld, brass_dk, noise=n_rough, noise_amt=0.35, rim=(220, 190, 120), rim_amt=0.2)
    for (x, y) in [(51, 40), (53, 38), (56, 39), (58, 42), (52, 44)]:
        c.set(x, y, verd[3]); c.set(x, y + 1, verd[1])
    c.set(55, 37, verd[3]); c.set(56, 37, verd[2])
    # alga seca colgando de la hombrera
    for i, (x, y) in enumerate([(57, 46), (57, 47), (58, 48), (58, 49), (57, 50)]):
        c.set(x, y, verd[1 + (i % 2)])

    # =====================================================================================
    # CABEZA: ESCAFANDRA DE LATON (mas pequena, con anillo de izado y gorguera)
    # =====================================================================================
    cx, cy = 48.0, 30.0
    # manguera de aire: sale por detras, forma un lazo y baja a la espalda
    hose = bezier_pts((41, 31), (30, 38), (36, 56), 26)
    c.shade(tentacle(W, H, hose, 1.5, 1.3), brass_dk, noise=n_rough, noise_amt=0.1, outline=True)
    for i in range(1, len(hose) - 1, 2):
        x, y = hose[i]
        c.set(round(x), round(y), brass_dk[3]); c.set(round(x) + 1, round(y), brass_dk[0])
    # gorguera con tornillos
    c.shade(ellipse(W, H, cx - 0.5, 38.6, 9.6, 3.0), brass, noise=n_rough, noise_amt=0.12, rim=(240, 215, 150), rim_amt=0.25, contrast=1.1, bias=-0.10)
    for x in (40.5, 43, 45.5, 48, 50.5, 53, 55.5):
        c.set(x, 38, brass[5]); c.set(x, 39, brass[1])
    # cupula
    dome = ellipse(W, H, cx, cy, 8.6, 8.8)
    c.shade(dome, brass, noise=n_rough, noise_amt=0.10, dither=0.35, rim=(240, 215, 150), rim_amt=0.3, contrast=1.25, ambient=0.10, bias=-0.16)
    # brillo especular de la cupula (arriba-izquierda) y media luna de sombra (abajo-derecha)
    for (x, y) in [(43, 24), (44, 23), (43, 25), (45, 23)]:
        c.set(x, y, brass[5])
    c.set(42, 26, brass[4])
    for (x, y) in [(55, 34), (54, 35), (56, 33), (53, 36), (56, 34)]:
        c.set(x, y, brass[0])
    # banda remachada a media altura
    for x in range(40, 57):
        yy = round(cy + 4.4 - ((x - cx) / 8.6) ** 2 * 1.6)
        c.set(x, yy, brass[1]); c.set(x, yy - 1, brass[4] if x % 3 == 0 else brass[3])
    # costura trasera
    c.line((cx - 5, cy - 7), (cx - 7, cy + 3), brass[1])
    # anillo de izado + remate
    c.shade(polygon(W, H, [(46, 22), (50.5, 22), (49.5, 20), (47, 20)], bevel=1), brass, outline=True)
    ring = ellipse(W, H, 48.2, 17.6, 3.0, 3.0)
    ring_m, _ = ring
    inner, _ = ellipse(W, H, 48.2, 17.6, 1.4, 1.4)
    c.shade((ring_m & ~inner, np.ones((H, W), np.float32) * 0.85), brass, outline=False, bias=0.1)
    # verdete y goteras de oxido
    for (x, y) in [(43, 28), (44, 30), (46, 37), (50, 37), (53, 36)]:
        c.set(x, y, verd[3]); c.set(x, y + 1, verd[1])
    for x in (44, 48, 52):
        c.set(x, 35, brass_dk[1]); c.set(x, 36, brass_dk[0])
    # OJO DE BUEY
    px, py = 53.0, 30.0
    c.shade(ellipse(W, H, px, py, 4.1, 4.7), brass, noise=n_rough, noise_amt=0.1, rim=(240, 215, 150), rim_amt=0.3)
    for k in range(8):
        a = k * math.pi / 4
        c.set(round(px + math.cos(a) * 3.7), round(py + math.sin(a) * 4.2), brass[5])
    glass = ellipse(W, H, px + 0.3, py, 2.7, 3.3)
    c.shade(glass, [teal_dk, teal_lo, teal_mid, teal_hi, teal_glow], outline=False, dither=0.45)
    c.set(51, 28, (235, 255, 250)); c.set(52, 28, (210, 255, 245)); c.set(51, 29, (190, 250, 240))
    c.set(53, 32, teal_dk); c.set(54, 32, teal_dk)
    ys, xs = np.nonzero(glass[0])
    for x, y in zip(xs, ys):
        if c.px[y, x, 1] > 140:
            c.emit[y, x] = (*c.px[y, x, :3], 255)

    # gotas de agua que caen del casco
    c.set(55, 41, (70, 110, 130)); c.set(55, 43, (50, 90, 110))
    return c


def moody_preview(c, scale=6, bg=(16, 22, 24)):
    """Vista previa sobre fondo oscuro + halo del brillo, para juzgar como se ve en el juego."""
    from PIL import Image, ImageFilter
    base = Image.new("RGBA", (c.w, c.h), (*bg, 255))
    glow = Image.fromarray(c.emit, "RGBA")
    halo = glow.filter(ImageFilter.GaussianBlur(5))
    base.alpha_composite(halo)
    base.alpha_composite(halo)
    base.alpha_composite(c.to_image())
    return base.resize((c.w * scale, c.h * scale), Image.NEAREST)


if __name__ == "__main__":
    import os
    c = build()
    out = os.path.join(os.path.dirname(__file__), "_preview")
    os.makedirs(out, exist_ok=True)
    c.save(os.path.join(out, "ahogado_x1.png"))
    moody_preview(c, 6).save(os.path.join(out, "ahogado_x6.png"))
    print("ok")
