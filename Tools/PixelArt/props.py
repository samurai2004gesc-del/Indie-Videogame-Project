"""Props / decorado v2 de ABISMO. Cada funcion devuelve un Canvas (el suelo / la base esta en la fila inferior util).
Piezas: candelabro, altar del Signo, estatua de la Madre (pieza estrella), idolo del Durmiente, estandarte,
jaula colgante, lapida, farol, coral espinoso, oro de Innsmouth, columna rota."""
import math
import numpy as np
from pxl import *

FLAME_O = (255, 170, 70)
FLAME_Y = (255, 226, 130)
FLAME_W = (255, 248, 210)
TEAL = (110, 250, 230)
TEAL_MID = (34, 170, 168)
TEAL_DK = (10, 70, 82)


def _flame(c, x, y, h=5):
    """Llama emisiva en forma de gota (base en (x, y), punta hacia arriba)."""
    for i in range(h):
        col = FLAME_W if i < 1 else (FLAME_Y if i < h - 2 else FLAME_O)
        w = 1 if i < 1 or i > h - 2 else 2
        for dx in range(-(w - 1), w):
            c.glow_set(x + dx, y - i, col)
    c.glow_set(x, y - h, FLAME_O)


def _star(cx, cy, r, rot=-math.pi / 2):
    return [(cx + math.cos(rot + k * 2 * math.pi / 5) * r, cy + math.sin(rot + k * 2 * math.pi / 5) * r) for k in range(5)]


def _stone_ramp(base="#65726c"):
    return ramp(base, 6, 0.12, 0.16, 1.45)


# =====================================================================================================
# CANDELABRO  (40 x 64)
# =====================================================================================================
def candelabra():
    W, H = 40, 64
    c = Canvas(W, H)
    gold = ramp("#b08a2c", 6, 0.12, 0.16, 1.60)
    gold_dk = ramp("#6a4e1e", 5, 0.10, 0.22, 1.35)
    wax = ramp("#efe2bc", 4, 0.08, 0.55, 1.15)
    n = noise2d(W, H, 3, seed=2)
    # tripode
    for (a, b, cc) in [((20, 56), (8, 60), (5, 62)), ((20, 56), (32, 60), (35, 62)), ((20, 56), (20, 62), (20, 63))]:
        pts = bezier_pts(a, b, cc, 8)
        c.shade(tentacle(W, H, pts, 2.2, 1.2), gold, outline=True)
    c.contact_shadow(62, 6, 34, alpha=100)
    # fuste con nudos
    c.shade(rect(W, H, 18.5, 24, 21.5, 58, 1.3), gold, noise=n, noise_amt=0.1)
    for y, r in [(54, 3.4), (46, 2.8), (36, 3.0), (28, 2.6)]:
        c.shade(ellipse(W, H, 20, y, r, 2.4), gold, outline=True, contrast=1.1, bias=-0.04)
        c.set(19, y - 1, gold[5])
    # brazos curvos (5 velas): centro + 2 pares
    arms = [((20, 28), (20, 20), (20, 14)),
            ((20, 36), (10, 36), (8, 24)), ((20, 36), (30, 36), (32, 24)),
            ((20, 44), (5, 44), (3, 34)), ((20, 44), (35, 44), (37, 34))]
    cups = []
    for a, ctrl, tip in arms:
        pts = bezier_pts(a, ctrl, tip, 14)
        c.shade(tentacle(W, H, pts, 1.5, 1.2), gold, outline=True)
        cups.append(tip)
    for (x, y) in cups:
        c.shade(polygon(W, H, [(x - 3, y), (x + 3, y), (x + 2, y + 3), (x - 2, y + 3)], bevel=1.1), gold, outline=True)
        c.shade(rect(W, H, x - 1.5, y - 7, x + 1.5, y, 0.8), wax, outline=True, bias=0.22)
        c.set(x - 1, y - 6, wax[3]); c.set(x, y - 3, wax[1]); c.set(x + 1, y - 2, wax[2])
        _flame(c, x, y - 8, 5)
    return c


# =====================================================================================================
# ALTAR DEL SIGNO ANTIGUO  (56 x 72)
# =====================================================================================================
def altar():
    W, H = 56, 72
    c = Canvas(W, H)
    st = _stone_ramp("#5f7268")
    st_dk = _stone_ramp("#3f4e4a")
    n = noise2d(W, H, 4, seed=5, octaves=2)
    # base escalonada
    c.shade(rect(W, H, 4, 62, 52, 70, 2.6), st_dk, noise=n, noise_amt=0.3)
    c.shade(rect(W, H, 8, 56, 48, 64, 2.4), st, noise=n, noise_amt=0.3)
    c.contact_shadow(70, 2, 54, alpha=120)
    # fuste con hornacina ojival
    c.shade(polygon(W, H, [(12, 56), (44, 56), (42, 24), (14, 24)], bevel=4), st, noise=n, noise_amt=0.3, contrast=1.1)
    niche = polygon(W, H, [(18, 54), (38, 54), (38, 32), (33, 26), (28, 22), (23, 26), (18, 32)], bevel=3)
    c.shade(niche, [(6, 16, 20), (8, 28, 34), (12, 42, 48)], outline=True)
    # estrella verde brillante en la hornacina
    pts = _star(28, 40, 8)
    for k in range(5):
        c.line(pts[k], pts[(k + 2) % 5], TEAL, glow=True)
    c.disc(28, 40, 2.6, TEAL_MID, glow=True); c.glow_set(28, 40, (230, 255, 248))
    # halo suave dentro de la hornacina
    ys, xs = np.nonzero(niche[0])
    for x, y in zip(xs, ys):
        d = math.hypot(x - 28, y - 40)
        if d < 13 and c.emit[y, x, 3] == 0:
            a = int(max(0, 70 - d * 5))
            c.emit[y, x] = (60, 220, 200, a)
    # relieve de olas en la base del fuste
    for x in range(14, 44, 4):
        c.set(x, 57, st[4]); c.set(x + 1, 56, st[3]); c.set(x + 2, 57, st[4])
    # pilastras laterales
    for x in (12, 42):
        c.shade(rect(W, H, x - 1, 28, x + 3, 56, 1.2), st_dk, outline=False)
    # plato superior con agua luminosa
    c.shade(polygon(W, H, [(8, 24), (48, 24), (44, 18), (12, 18)], bevel=2.4), st, noise=n, noise_amt=0.2, outline=True)
    c.shade(ellipse(W, H, 28, 18, 14, 2.6), [TEAL_DK, TEAL_MID, TEAL, (200, 255, 245)], outline=False)
    ys, xs = np.nonzero(ellipse(W, H, 28, 18, 14, 2.6)[0])
    for x, y in zip(xs, ys):
        c.emit[y, x] = (*c.px[y, x, :3], 220)
    # dos velas
    wax = ramp("#d8cca6", 4, 0.08, 0.45, 1.20)
    for x in (10, 46):
        c.shade(rect(W, H, x - 1.5, 10, x + 1.5, 24, 0.8), wax, outline=True)
        c.set(x - 1, 14, wax[3]); c.set(x, 18, wax[1])
        _flame(c, x, 8, 5)
    # musgo
    c.stipple(rect(W, H, 4, 58, 52, 70, 1)[0], (70, 120, 90), 0.12, seed=3)
    return c


# =====================================================================================================
# LA MADRE DE LAS PROFUNDIDADES  (96 x 128)  - la pieza estrella
# =====================================================================================================
def mother_statue():
    W, H = 96, 128
    GROUND = 124
    c = Canvas(W, H)
    c.set_light(-0.55, 0.7, 0.5)
    st = _stone_ramp("#5a6e68")
    st_dk = _stone_ramp("#3a4a48")
    st_lt = _stone_ramp("#7b8c84")
    n = noise2d(W, H, 5, seed=7, octaves=3)
    n2 = noise2d(W, H, 3, seed=12, octaves=2)
    moss = ramp("#3f7a5a", 4, 0.08, 0.4, 1.3)

    # pedestal con relieve de olas
    c.shade(rect(W, H, 6, 108, 90, 126, 3.0), st_dk, noise=n, noise_amt=0.35)
    c.shade(rect(W, H, 12, 100, 84, 110, 2.6), st, noise=n, noise_amt=0.35)
    c.contact_shadow(GROUND + 1, 2, 94, alpha=130)
    for x in range(14, 82, 6):
        c.set(x, 113, st[4]); c.set(x + 1, 112, st[5]); c.set(x + 2, 112, st[4]); c.set(x + 3, 113, st[3])
        c.set(x, 118, st_dk[1]); c.set(x + 2, 119, st_dk[1])
    # aureola: aro de piedra tras la cabeza con el Signo
    ring_o, _ = ellipse(W, H, 48, 26, 21, 21)
    ring_i, _ = ellipse(W, H, 48, 26, 16, 16)
    c.shade(Shape(ring_o & ~ring_i, np.ones((H, W), np.float32) * 0.8, 3.0), st_dk, noise=n2, noise_amt=0.2, outline=True)
    pts = _star(48, 26, 14)
    for k in range(5):
        c.line(pts[k], pts[(k + 2) % 5], TEAL_MID)
    for k in range(5):
        c.glow_set(round(pts[k][0]), round(pts[k][1]), TEAL)

    # manto/tunica de rodillas: campana grande
    robe = [(34, 44), (62, 44), (74, 62), (84, 90), (88, 104), (10, 104), (14, 88), (22, 62)]
    c.shade(polygon(W, H, robe, bevel=10, power=0.9), st, noise=n, noise_amt=0.55, rim=(190, 230, 215), rim_amt=0.2, dither=0.2)
    # pliegues profundos
    for pts2, col in [([(36, 56), (26, 80), (18, 102)], st_dk[0]), ([(44, 60), (40, 84), (36, 103)], st_dk[1]),
                      ([(54, 60), (58, 84), (64, 103)], st_dk[1]), ([(62, 56), (72, 80), (80, 102)], st_dk[0]),
                      ([(32, 54), (23, 74), (16, 96)], st_lt[4])]:
        for a, b in zip(pts2[:-1], pts2[1:]):
            c.line(a, b, col)
    # regazo en sombra + el ahogado tendido (buzo de piedra clara con escafandra)
    c.shade(ellipse(W, H, 48, 91, 36, 11, rot=-6), st_dk, noise=n, noise_amt=0.3, outline=False)
    dv = ramp("#a3b6ab", 6, 0.10, 0.20, 1.40)
    dv_dk = ramp("#6d7f77", 5, 0.10, 0.22, 1.30)
    # piernas colgando a la izquierda, con botas
    c.shade(capsule(W, H, (26, 86), (14, 104), 6.2, 4.8), dv_dk, noise=n2, noise_amt=0.2, outline=True)
    c.shade(polygon(W, H, [(5, 102), (19, 101), (21, 111), (3, 112)], bevel=2.0), dv_dk, outline=True)
    c.hline(6, 19, 103, dv_dk[4])
    # torso con abrigo, botones y cinturon
    torso = ellipse(W, H, 46, 82, 27, 9.4, rot=-7)
    c.shade(torso, dv, noise=n2, noise_amt=0.2, rim=(225, 248, 238), rim_amt=0.3, contrast=1.1)
    c.line((31, 86), (62, 80), dv_dk[1]); c.line((31, 87), (62, 81), dv[1])
    for x in (38, 44, 51, 57):
        c.set(x, round(83 - (x - 38) * 0.18), dv_dk[0]); c.set(x, round(82 - (x - 38) * 0.18), dv[5])
    # brazo colgando a la derecha
    c.shade(capsule(W, H, (70, 84), (80, 99), 5.2, 3.6), dv, noise=n2, noise_amt=0.2, outline=True)
    c.shade(ellipse(W, H, 81, 101, 3.4, 3.2), dv, outline=True)
    for dx in (-2, 0, 2):
        c.line((81 + dx, 103), (82 + dx, 108), dv[2])
    # escafandra con gorguera y ojo de buey tenue
    c.shade(ellipse(W, H, 68, 80, 9.8, 8.8), dv, noise=n2, noise_amt=0.15, rim=(235, 250, 242), rim_amt=0.3, contrast=1.2, bias=-0.08)
    c.shade(ellipse(W, H, 68, 89, 10.4, 3.0), dv_dk, outline=True)
    for x in (62, 65, 68, 71, 74):
        c.set(x, 88, dv[5])
    port = ellipse(W, H, 72, 79, 3.8, 4.4)
    c.shade(port, [TEAL_DK, TEAL_MID, TEAL], outline=True)
    for x, y in zip(*np.nonzero(port[0]))[::-1] if False else []:
        pass
    ys, xs = np.nonzero(port[0])
    for x, y in zip(xs, ys):
        c.emit[y, x] = (*c.px[y, x, :3], 255)

    # brazos de la Madre: abrazan el cuerpo
    c.shade(capsule(W, H, (36, 52), (30, 72), 6.6, 4.6), st, noise=n, noise_amt=0.3, rim=(190, 230, 215), rim_amt=0.2)
    c.shade(capsule(W, H, (30, 72), (44, 86), 4.6, 3.6), st, noise=n, noise_amt=0.3)
    c.shade(capsule(W, H, (60, 52), (66, 70), 6.6, 4.6), st_dk, noise=n, noise_amt=0.3)
    c.shade(capsule(W, H, (66, 70), (56, 86), 4.6, 3.6), st_dk, noise=n, noise_amt=0.3)
    c.shade(ellipse(W, H, 46, 86, 4.0, 3.2), st)
    c.shade(ellipse(W, H, 55, 87, 3.8, 3.0), st_dk)
    # capucha y cabeza
    c.shade(polygon(W, H, [(34, 48), (30, 34), (36, 18), (48, 10), (60, 18), (66, 34), (62, 48)], bevel=7, power=0.9), st, noise=n, noise_amt=0.4,
            rim=(190, 230, 215), rim_amt=0.25, dither=0.2)
    c.line((40, 18), (36, 40), st_dk[1]); c.line((56, 18), (60, 40), st_dk[1])
    # rostro: oscuridad con ojos tenues y barba de tentaculos que cae como un velo
    c.shade(ellipse(W, H, 48, 32, 9.5, 12.5), [(6, 14, 18), (10, 24, 28), (16, 36, 40)], outline=False)
    c.glow_set(44, 29, (60, 210, 190)); c.glow_set(52, 29, (60, 210, 190))
    c.glow_set(44, 30, (30, 110, 100)); c.glow_set(52, 30, (30, 110, 100))
    beard = [((42, 38), (36, 50), (32, 64)), ((46, 40), (44, 54), (40, 70)), ((50, 40), (52, 54), (56, 70)),
             ((54, 38), (60, 50), (64, 64)), ((48, 41), (48, 56), (48, 72))]
    for k, (a, ctrl, b) in enumerate(beard):
        pts3 = bezier_pts(a, ctrl, b, 22)
        c.shade(tentacle(W, H, pts3, 3.2 - (k % 2) * 0.5, 0.9), st_lt, noise=n2, noise_amt=0.25, outline=True,
                rim=(220, 245, 235), rim_amt=0.2)
        for i in range(3, 20, 3):
            c.set(round(pts3[i][0]) + 1, round(pts3[i][1]), st[4])
    # musgo, humedad y algas en la base
    c.stipple(rect(W, H, 6, 100, 90, 126, 1)[0], moss[2], 0.14, seed=1)
    c.stipple(rect(W, H, 10, 60, 86, 110, 1)[0] & c.alpha_mask(), moss[1], 0.04, seed=4)
    for (x, y) in [(18, 100), (30, 102), (66, 101), (80, 100)]:
        c.line((x, y), (x + 1, y + 6), moss[2]); c.set(x + 1, y + 6, moss[3])
    # dos velas votivas en el pedestal con llama
    wax = ramp("#d8cca6", 4, 0.08, 0.45, 1.20)
    for x in (22, 74):
        c.shade(rect(W, H, x - 2, 94, x + 2, 101, 0.8), wax, outline=True)
        _flame(c, x, 92, 5)
    return c


# =====================================================================================================
# IDOLO DEL DURMIENTE  (48 x 64)
# =====================================================================================================
def idol():
    W, H = 48, 64
    c = Canvas(W, H)
    bas = ramp("#35504a", 6, 0.13, 0.12, 1.55)
    bas_dk = ramp("#1f3036", 5, 0.12, 0.18, 1.35)
    n = noise2d(W, H, 3, seed=9, octaves=2)
    # losa
    c.shade(rect(W, H, 2, 56, 46, 63, 2.2), bas_dk, noise=n, noise_amt=0.3)
    c.contact_shadow(63, 0, 48, alpha=110)
    # alas plegadas (al fondo)
    c.shade(polygon(W, H, [(14, 46), (4, 20), (10, 28), (8, 12), (16, 24), (20, 38)], bevel=2.0), bas_dk, outline=True, noise=n, noise_amt=0.3)
    c.shade(polygon(W, H, [(34, 46), (44, 20), (38, 28), (40, 12), (32, 24), (28, 38)], bevel=2.0), bas, outline=True, noise=n, noise_amt=0.3)
    # cuerpo rechoncho en cuclillas
    c.shade(ellipse(W, H, 24, 46, 13, 11), bas, noise=n, noise_amt=0.25, rim=(160, 230, 210), rim_amt=0.25, dither=0.2)
    c.shade(ellipse(W, H, 24, 50, 15, 8), bas, noise=n, noise_amt=0.25)
    # patas con garras
    for x in (9, 39):
        c.shade(ellipse(W, H, x, 54, 5, 4), bas, noise=n, noise_amt=0.2)
        for dx in (-2, 0, 2):
            c.line((x + dx, 57), (x + dx, 60), bas[5])
    # brazos sobre las rodillas
    c.shade(capsule(W, H, (14, 38), (10, 50), 3.2, 2.6), bas_dk)
    c.shade(capsule(W, H, (34, 38), (38, 50), 3.2, 2.6), bas)
    # cabeza con tentaculos
    c.shade(ellipse(W, H, 24, 26, 10, 9.5), bas, noise=n, noise_amt=0.2, rim=(160, 230, 210), rim_amt=0.3, dither=0.2)
    for k, (dx, L) in enumerate([(-6, 14), (-2, 18), (2, 18), (6, 14)]):
        pts = bezier_pts((24 + dx, 33), (24 + dx * 1.4, 33 + L * 0.5), (24 + dx * 1.2, 33 + L), 12)
        c.shade(tentacle(W, H, pts, 2.2, 0.8), bas, outline=True, rim=(160, 230, 210), rim_amt=0.2)
    # ojos que brillan
    for x in (20, 28):
        c.disc(x, 24, 1.6, (6, 16, 20)); c.glow_set(x, 24, TEAL); c.glow_set(x, 23, (200, 255, 245))
    # musgo
    c.stipple(rect(W, H, 2, 54, 46, 63, 1)[0], (70, 120, 90), 0.14, seed=2)
    return c


# =====================================================================================================
# ESTANDARTE CARMESI  (32 x 80)  (colgado: el techo esta arriba)
# =====================================================================================================
def banner():
    W, H = 32, 80
    c = Canvas(W, H)
    crim = ramp("#7a1a2b", 6, 0.12, 0.14, 1.45)
    gold = ramp("#b08a2c", 6, 0.12, 0.16, 1.60)
    n = noise2d(W, H, 4, seed=3, octaves=2)
    # barra superior con remates
    c.shade(rect(W, H, 1, 1, 31, 5, 1.4), gold, outline=True)
    for x in (1, 30):
        c.disc(x, 3, 2.0, gold[4]); c.set(x - 1, 2, gold[5])
    # tela con cola de golondrina
    cloth = [(4, 5), (28, 5), (29, 60), (26, 72), (16, 64), (6, 72), (3, 60)]
    c.shade(polygon(W, H, cloth, bevel=5, power=0.9), crim, noise=n, noise_amt=0.55, rim=(255, 120, 110), rim_amt=0.2, dither=0.25)
    for pts, col in [([(10, 8), (9, 36), (8, 60)], crim[1]), ([(21, 8), (22, 36), (23, 60)], crim[1]), ([(15, 10), (15, 40), (16, 58)], crim[4])]:
        for a, b in zip(pts[:-1], pts[1:]):
            c.line(a, b, col)
    # orlas doradas
    c.line((4, 6), (3, 60), gold[3]); c.line((28, 6), (29, 60), gold[3])
    c.line((3, 60), (6, 72), gold[2]); c.line((29, 60), (26, 72), gold[2])
    c.line((6, 72), (16, 64), gold[2]); c.line((16, 64), (26, 72), gold[2])
    # emblema: Signo (estrella) sobre olas
    pts = _star(16, 30, 8)
    for k in range(5):
        c.line(pts[k], pts[(k + 2) % 5], gold[5])
    c.disc(16, 30, 2.0, TEAL_MID, glow=True)
    c.emit[30, 16] = (*TEAL, 255)
    for x in range(8, 25, 4):
        c.set(x, 46, gold[3]); c.set(x + 1, 45, gold[4]); c.set(x + 2, 46, gold[3]); c.set(x + 3, 47, gold[2])
        c.set(x, 50, gold[2]); c.set(x + 1, 49, gold[3]); c.set(x + 2, 50, gold[2]); c.set(x + 3, 51, gold[1])
    # flecos
    for x in range(5, 28, 3):
        c.set(x, 6, gold_dk[0]) if False else None
    return c


# =====================================================================================================
# JAULA COLGANTE con esqueleto  (36 x 64)
# =====================================================================================================
def cage():
    W, H = 36, 64
    c = Canvas(W, H)
    iron = ramp("#4a4c52", 5, 0.07, 0.22, 1.7)
    rust = ramp("#7a4a34", 4, 0.09, 0.30, 1.4)
    bone = ramp("#cdbf9a", 4, 0.06, 0.40, 1.20)
    # cadena
    for y in range(0, 12, 3):
        c.shade(ellipse(W, H, 18, y + 1.5, 1.8, 2.2), iron, outline=True)
    # aro superior y barrotes
    c.shade(ellipse(W, H, 18, 14, 7, 2.6), iron, outline=True)
    c.shade(polygon(W, H, [(12, 14), (24, 14), (30, 20), (6, 20)], bevel=1.8), iron, outline=True)
    # interior oscuro
    c.shade(rect(W, H, 6, 20, 30, 56, 1.0), [(8, 10, 14), (12, 14, 20), (16, 20, 26)], outline=False)
    # esqueleto sentado: craneo, costillas, brazos colgando
    c.shade(ellipse(W, H, 18, 34, 4.8, 4.6), bone, outline=True)
    c.set(16, 33, (10, 6, 14)); c.set(20, 33, (10, 6, 14)); c.set(18, 36, (10, 6, 14)); c.line((16, 38), (20, 38), bone[1])
    c.line((18, 39), (18, 50), bone[2])
    for y in (41, 44, 47):
        c.line((13, y), (23, y), bone[2]); c.line((14, y + 1), (22, y + 1), bone[1])
    c.line((12, 41), (10, 52), bone[2]); c.line((24, 41), (26, 52), bone[2])
    # barrotes verticales (delante) con oxido
    for x in (6, 11, 16, 21, 26, 30):
        c.shade(rect(W, H, x - 0.5, 20, x + 1, 58, 0.6), iron, outline=False)
        c.line((x, 22), (x, 56), iron[4] if x < 18 else iron[3])
    # aros horizontales
    for y in (24, 38, 54):
        c.hline(5, 30, y, iron[2]); c.hline(5, 30, y + 1, iron[0])
    # base y goteo de oxido
    c.shade(ellipse(W, H, 18, 58, 13, 2.6), iron, outline=True)
    for (x, y) in [(10, 28), (24, 44), (14, 50)]:
        c.set(x, y, rust[2]); c.set(x, y + 1, rust[1]); c.set(x, y + 2, rust[0])
    return c


# =====================================================================================================
# LAPIDA con runas  (28 x 40)
# =====================================================================================================
def tombstone():
    W, H = 28, 40
    c = Canvas(W, H)
    st = _stone_ramp("#6c7a74")
    n = noise2d(W, H, 3, seed=14, octaves=2)
    moss = ramp("#3f7a5a", 4, 0.08, 0.4, 1.3)
    c.shade(rect(W, H, 3, 34, 25, 39, 1.8), _stone_ramp("#46524e"), noise=n, noise_amt=0.3)
    c.contact_shadow(39, 0, 28, alpha=100)
    stone = polygon(W, H, [(6, 36), (6, 12), (9, 5), (14, 3), (19, 5), (22, 12), (22, 36)], bevel=4.4)
    c.shade(stone, st, noise=n, noise_amt=0.4, rim=(190, 215, 205), rim_amt=0.25, dither=0.2)
    # runas grabadas
    for y in range(12, 30, 4):
        for x in range(10, 19, 3):
            if (x + y) % 5:
                c.line((x, y), (x, y + 2), st[0])
    c.line((14, 7), (14, 10), st[0]); c.line((12, 8), (16, 8), st[0])
    # grieta y musgo
    c.line((20, 14), (17, 22), st[0]); c.line((17, 22), (18, 28), st[0])
    c.stipple(stone[0], moss[2], 0.14, seed=3)
    for x in range(7, 22, 3):
        c.set(x, 35, moss[2]); c.set(x + 1, 34, moss[3])
    return c


# =====================================================================================================
# FAROL colgante  (20 x 44)
# =====================================================================================================
def lantern():
    W, H = 20, 44
    c = Canvas(W, H)
    brass = ramp("#8f6a26", 6, 0.12, 0.16, 1.60)
    for y in range(0, 10, 3):
        c.shade(ellipse(W, H, 10, y + 1.5, 1.5, 2.0), brass, outline=True)
    c.shade(polygon(W, H, [(4, 16), (16, 16), (14, 11), (6, 11)], bevel=1.6), brass, outline=True)
    c.shade(ellipse(W, H, 10, 10, 2.2, 1.6), brass, outline=True)
    glass = polygon(W, H, [(5, 17), (15, 17), (15, 34), (5, 34)], bevel=2.0)
    c.shade(glass, [(110, 50, 12), (190, 100, 24), (240, 160, 50), (255, 210, 110)], outline=False, dither=0.3)
    ys, xs = np.nonzero(glass[0])
    for x, y in zip(xs, ys):
        c.emit[y, x] = (*c.px[y, x, :3], 255)
    for x in (4, 10, 16):
        c.shade(rect(W, H, x - 0.5, 16, x + 1, 35, 0.6), brass, outline=False)
    c.shade(rect(W, H, 3, 16, 17, 18, 0.8), brass, outline=True)
    c.shade(polygon(W, H, [(3, 34), (17, 34), (14, 40), (6, 40)], bevel=1.6), brass, outline=True)
    _flame(c, 10, 32, 6)
    return c


# =====================================================================================================
# CORAL ESPINOSO (peligro)  (60 x 64)
# =====================================================================================================
def coral():
    W, H = 60, 64
    c = Canvas(W, H)
    coral_c = ramp("#a8405e", 6, 0.13, 0.16, 1.45)
    tip = ramp("#e8b090", 4, 0.08, 0.5, 1.2)
    n = noise2d(W, H, 3, seed=22, octaves=2)
    c.contact_shadow(62, 6, 54, alpha=110)
    branches = [((30, 62), (30, 40), (22, 14)), ((30, 62), (40, 44), (46, 18)), ((30, 62), (18, 48), (8, 30)),
                ((30, 62), (44, 56), (54, 40)), ((30, 56), (28, 34), (34, 6)), ((26, 60), (14, 58), (4, 52))]
    for k, (a, ctrl, b) in enumerate(branches):
        pts = bezier_pts(a, ctrl, b, 22)
        c.shade(tentacle(W, H, pts, 6.4 - (k % 3) * 0.8, 1.7), coral_c, noise=n, noise_amt=0.3, outline=True, dither=0.25,
                rim=(255, 170, 150), rim_amt=0.22)
        # espinas laterales
        for i in range(4, 21, 4):
            x, y = pts[i]
            dx = 1 if (i // 4 + k) % 2 else -1
            c.line((x, y), (x + dx * 6, y - 4), coral_c[4]); c.line((x, y + 1), (x + dx * 5, y - 3), coral_c[3]); c.set(round(x + dx * 6), round(y - 4), tip[3])
        c.set(round(b[0]), round(b[1]), tip[3]); c.set(round(b[0]), round(b[1]) - 1, tip[2])
    # pólipos pálidos
    for (x, y) in [(28, 30), (40, 40), (18, 42), (36, 18)]:
        c.set(x, y, tip[3]); c.set(x + 1, y, tip[2])
    return c


# =====================================================================================================
# ORO DE INNSMOUTH (moneda de joyeria extrana)  (16 x 16)
# =====================================================================================================
def coin():
    W, H = 20, 20
    c = Canvas(W, H)
    gold = ramp("#c29a34", 6, 0.12, 0.16, 1.65)
    gold_dk = ramp("#7a5a1e", 5, 0.10, 0.22, 1.35)
    c.shade(ellipse(W, H, 10, 10, 8.8, 8.8), gold, outline=True, contrast=1.1, bias=-0.04)
    ring_o, _ = ellipse(W, H, 10, 10, 8.0, 8.0)
    ring_i, _ = ellipse(W, H, 10, 10, 6.0, 6.0)
    c.shade(Shape(ring_o & ~ring_i, np.ones((H, W), np.float32) * 0.5, 2.0), gold_dk, outline=False, bias=-0.05)
    for k in range(16):
        a = k * math.pi / 8
        c.set(round(10 + math.cos(a) * 7.0), round(10 + math.sin(a) * 7.0), gold[5] if k % 2 else gold[2])
    pts = _star(10, 10, 4.4)
    for k in range(5):
        c.line(pts[k], pts[(k + 2) % 5], gold[5])
    c.set(10, 10, (110, 240, 220)); c.emit[10, 10] = (110, 240, 220, 255)
    c.set(5, 5, (255, 246, 205)); c.set(6, 4, (255, 246, 205))
    return c


# =====================================================================================================
# COLUMNA ROTA con algas  (44 x 100)
# =====================================================================================================
def column():
    W, H = 44, 100
    c = Canvas(W, H)
    st = _stone_ramp("#5f6f69")
    st_dk = _stone_ramp("#3b4845")
    moss = ramp("#3f7a5a", 4, 0.08, 0.4, 1.3)
    n = noise2d(W, H, 4, seed=18, octaves=3)
    c.shade(rect(W, H, 2, 90, 42, 99, 2.4), st_dk, noise=n, noise_amt=0.3)
    c.contact_shadow(99, 0, 44, alpha=120)
    c.shade(rect(W, H, 6, 82, 38, 92, 2.4), st, noise=n, noise_amt=0.3)
    # fuste acanalado con rotura irregular arriba
    shaft = polygon(W, H, [(9, 84), (35, 84), (34, 22), (31, 16), (27, 20), (22, 10), (18, 18), (13, 14), (10, 24)], bevel=7, power=0.9)
    c.shade(shaft, st, noise=n, noise_amt=0.35, rim=(185, 210, 200), rim_amt=0.2, dither=0.2)
    for x in (13, 17, 21, 25, 29, 33):
        c.line((x, 26), (x, 84), st_dk[1] if x % 8 else st_dk[2])
    for x in (15, 23, 31):
        c.line((x - 1, 28), (x - 1, 82), st[5])
    # grietas
    c.line((12, 40), (20, 48), st_dk[0]); c.line((20, 48), (18, 60), st_dk[0]); c.line((26, 30), (30, 44), st_dk[0])
    # musgo y alga seca
    c.stipple(shaft[0], moss[2], 0.10, seed=5)
    for x, y, L in [(12, 52, 14), (30, 60, 12), (22, 70, 10)]:
        for i in range(L):
            c.set(x + (1 if i % 4 < 2 else 0), y + i, moss[1 + (i % 2)])
    # escombros a los pies
    for (x, y, r) in [(6, 92, 3), (38, 94, 3.5), (12, 96, 2.4)]:
        c.shade(ellipse(W, H, x, y, r + 1, r), st, noise=n, noise_amt=0.2)
    return c


PROPS = [
    ("Candelabro", candelabra, 0), ("Altar del Signo", altar, 0), ("La Madre de las Profundidades", mother_statue, 0),
    ("Ídolo del Durmiente", idol, 0), ("Columna rota", column, 0), ("Lápida", tombstone, 0),
    ("Coral espinoso", coral, 0), ("Estandarte", banner, 30), ("Jaula", cage, 26), ("Farol", lantern, 34), ("Oro", coin, 10),
]

if __name__ == "__main__":
    import os
    from ahogado import moody_preview
    out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "_preview")
    os.makedirs(out, exist_ok=True)
    for name, fn, _ in PROPS:
        c = fn()
        moody_preview(c, 5).save(os.path.join(out, "prop_" + fn.__name__ + ".png"))
    print("ok")
