"""scenery.py - herramientas de escenario para ABISMO (640x360): cielos, siluetas, mamposteria, arcos, vidrieras
y el paso de iluminacion (luces puntuales, bloom, niebla, viñeta, grano) que da el aspecto 'Blasphemous'."""
import math
import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage as ndi
from pxl import Canvas, noise2d, ramp, polygon, ellipse, hex2rgb, mix, shadow_of, _BAYER4, bezier_pts

W, H = 640, 360


# ----------------------------------------------------------------------------------------------
# Relleno y cielo
# ----------------------------------------------------------------------------------------------
def grad_fill(c, x0, y0, x1, y1, stops, dither=True):
    """Degradado vertical por bandas con tramado ordenado entre colores (aspecto pixel art)."""
    h, w = y1 - y0, x1 - x0
    n = len(stops)
    t = np.linspace(0, 1, h)[:, None] * np.ones((1, w))
    pos = t * (n - 1)
    i = np.clip(np.floor(pos).astype(int), 0, n - 2)
    frac = pos - i
    by = np.tile(_BAYER4, (h // 4 + 1, w // 4 + 1))[:h, :w] + 0.5
    pick = (frac > by) if dither else (frac > 0.5)
    idx = np.where(pick, i + 1, i)
    cols = np.array(stops, np.uint8)[idx]
    c.px[y0:y1, x0:x1, :3] = cols
    c.px[y0:y1, x0:x1, 3] = 255


def stars(c, n, seed, y_max, color=(200, 224, 214), twinkle=0.25):
    rng = np.random.default_rng(seed)
    for _ in range(n):
        x, y = int(rng.integers(0, c.w)), int(rng.integers(0, y_max))
        b = rng.random()
        col = tuple(int(v * (0.45 + 0.55 * b)) for v in color)
        c.set(x, y, col)
        if rng.random() < twinkle:
            c.set(x + 1, y, tuple(int(v * 0.5) for v in col)); c.set(x - 1, y, tuple(int(v * 0.5) for v in col))
            c.set(x, y + 1, tuple(int(v * 0.5) for v in col)); c.set(x, y - 1, tuple(int(v * 0.5) for v in col))


def moon(c, cx, cy, r, base="#d6e6b8", seed=3):
    rmp = ramp(base, 5, 0.06, 0.45, 1.15)
    n = noise2d(c.w, c.h, 9, seed=seed, octaves=2)
    m, hh = ellipse(c.w, c.h, cx, cy, r, r)
    c.shade((m, hh), rmp, noise=n, noise_amt=0.0, outline=False, dither=0.4, contrast=0.8, bias=0.28)
    # craters (manchas oscuras)
    rng = np.random.default_rng(seed)
    for _ in range(9):
        a = rng.random() * 6.28
        d = rng.random() * r * 0.75
        px, py = cx + math.cos(a) * d, cy + math.sin(a) * d
        rr = 2 + rng.random() * 5
        cm, _ = ellipse(c.w, c.h, px, py, rr, rr * 0.8)
        cm &= m
        c.px[cm, :3] = (np.array(rmp[1], np.float32) * 0.9 + np.array(rmp[2], np.float32) * 0.1).astype(np.uint8)
    c.emit[m] = (*rmp[3], 62)


def cloud_band(c, y, h, seed, color, scale=26, thresh=0.5, x0=0, x1=None, alpha=235):
    x1 = x1 or c.w
    n = noise2d(c.w, c.h, scale, seed=seed, octaves=3)
    rows = np.arange(c.h)[:, None]
    center = y + h / 2
    prof = 1 - np.abs(rows - center) / (h / 2)
    mask = (n + prof * 0.55 > 0.5 + thresh * 0.4) & (np.abs(rows - center) < h / 2)
    mask[:, :x0] = False
    mask[:, x1:] = False
    # borde escalonado
    c.px[mask, :3] = color
    c.px[mask, 3] = alpha


# ----------------------------------------------------------------------------------------------
# Siluetas
# ----------------------------------------------------------------------------------------------
def ridge(c, y_base, amp, seed, color, scale=40, x0=0, x1=None, rim=None):
    x1 = x1 or c.w
    n = noise2d(c.w, 4, scale, seed=seed, octaves=3)[0]
    for x in range(x0, x1):
        top = int(y_base - n[x] * amp)
        c.px[top:, x, :3] = color
        c.px[top:, x, 3] = 255
        if rim is not None:
            c.px[top, x, :3] = rim


def houses(c, y_base, x0, x1, seed, color, window=(255, 176, 76), density=0.16, rim=None, hmin=26, hmax=70):
    """Casas de tejado a dos aguas (Innsmouth): siluetas con ventanas ambar sueltas."""
    rng = np.random.default_rng(seed)
    x = x0
    while x < x1:
        w = int(rng.integers(20, 44))
        h = int(rng.integers(hmin, hmax))
        kind = rng.integers(0, 3)
        top = y_base - h
        body = [(x, y_base), (x, top + 10), (x + w, top + 10), (x + w, y_base)]
        if kind == 0:   # tejado a dos aguas
            roof = [(x - 3, top + 12), (x + w / 2, top - 6), (x + w + 3, top + 12)]
        elif kind == 1:  # mansarda (gambrel)
            roof = [(x - 2, top + 12), (x + w * 0.2, top), (x + w * 0.5, top - 6), (x + w * 0.8, top), (x + w + 2, top + 12)]
        else:            # torre con aguja
            roof = [(x + 2, top + 12), (x + w / 2, top - 22), (x + w - 2, top + 12)]
        for poly in (body, roof):
            m = polygon(c.w, c.h, poly, bevel=1)[0]
            c.px[m, :3] = color
            c.px[m, 3] = 255
        # chimenea
        if rng.random() < 0.5:
            cx = int(x + w * (0.2 + rng.random() * 0.5))
            c.px[top - 4: top + 8, cx:cx + 3, :3] = color
            c.px[top - 4: top + 8, cx:cx + 3, 3] = 255
        # ventanas
        for wy in range(top + 16, y_base - 4, 9):
            for wx in range(x + 4, x + w - 4, 8):
                if rng.random() < density:
                    c.px[wy:wy + 4, wx:wx + 3, :3] = window
                    c.px[wy:wy + 4, wx:wx + 3, 3] = 255
                    c.emit[wy:wy + 4, wx:wx + 3] = (*window, 255)
        x += w + int(rng.integers(-4, 6))
    # el suelo bajo las casas
    c.px[y_base:, x0:x1, :3] = color
    c.px[y_base:, x0:x1, 3] = 255


# ----------------------------------------------------------------------------------------------
# Mamposteria, arcos, columnas, vidrieras
# ----------------------------------------------------------------------------------------------
def masonry(c, x0, y0, x1, y1, base, bw=24, bh=11, seed=0, moss=0.0, variation=0.18, mortar=0.55):
    rmp = ramp(base, 5, 0.08, 0.35, 1.3)
    rng = np.random.default_rng(seed)
    n = noise2d(c.w, c.h, 3, seed=seed + 1, octaves=2)
    y = y0
    row = 0
    while y < y1:
        off = (row % 2) * bw // 2 + int(rng.integers(0, 4))
        x = x0 - off
        while x < x1:
            w = bw + int(rng.integers(-5, 6))
            tone = rng.integers(1, 4)
            col = np.array(rmp[tone], np.float32) * (1 + (rng.random() - 0.5) * variation)
            xa, xb = max(x0, x), min(x1, x + w)
            ya, yb = y, min(y1, y + bh)
            if xb > xa:
                blk = np.clip(col, 0, 255).astype(np.uint8)
                c.px[ya:yb, xa:xb, :3] = blk
                c.px[ya:yb, xa:xb, 3] = 255
                # luz arriba/izquierda, sombra abajo/derecha
                c.px[ya:ya + 1, xa:xb, :3] = rmp[4]
                if xa == x:
                    c.px[ya:yb, xa:xa + 1, :3] = rmp[3]
                c.px[yb - 1:yb, xa:xb, :3] = rmp[0]
                if xb == x + w:
                    c.px[ya:yb, xb - 1:xb, :3] = rmp[0]
                # grano de piedra
                sub = n[ya:yb, xa:xb]
                dark = sub < 0.3
                c.px[ya:yb, xa:xb, :3][dark] = (c.px[ya:yb, xa:xb, :3][dark] * 0.82).astype(np.uint8)
            x += w
        y += bh
        row += 1
    if moss > 0:
        rng2 = np.random.default_rng(seed + 9)
        mm = rng2.random((c.h, c.w)) < moss
        reg = np.zeros((c.h, c.w), bool)
        reg[y0:y1, x0:x1] = True
        mm &= reg & (noise2d(c.w, c.h, 6, seed=seed + 4) > 0.5)
        c.px[mm, :3] = (60, 110, 80)


def pointed_arch_mask(w, h, cx, base_y, span, height):
    """Mascara del hueco de un arco ojival (interseccion de dos circulos)."""
    ys, xs = np.mgrid[0:h, 0:w]
    r = span * 0.9
    m = (xs - (cx + span / 2 - r)) ** 2 + (ys - (base_y - height + r)) ** 2 <= r * r
    m &= (xs - (cx - span / 2 + r)) ** 2 + (ys - (base_y - height + r)) ** 2 <= r * r
    m |= False
    # recto en la parte baja
    rect = (xs >= cx - span / 2) & (xs <= cx + span / 2) & (ys >= base_y - height + r * 0.55) & (ys <= base_y)
    # el arco apunta: usa los dos circulos desplazados
    c1 = (xs - (cx + span * 0.5 - r + span * 0.0)) ** 2 + (ys - (base_y - height * 0.45)) ** 2 <= (r * 1.0) ** 2
    return (m | rect) & (ys <= base_y)


def arch(c, cx, base_y, span, height, stone, dark=(6, 10, 14), thickness=7, seed=0, interior=None):
    """Arco ojival: hueco oscuro (o con un degradado/interior) y marco de sillares."""
    h_open = pointed_arch_mask(c.w, c.h, cx, base_y, span, height)
    big = pointed_arch_mask(c.w, c.h, cx, base_y, span + thickness * 2, height + thickness)
    frame = big & ~h_open
    rmp = ramp(stone, 5, 0.08, 0.3, 1.35)
    n = noise2d(c.w, c.h, 3, seed=seed, octaves=2)
    cols = np.array(rmp, np.uint8)[np.clip((n * 3 + 1.0).astype(int), 0, 4)]
    c.px[frame, :3] = cols[frame]
    c.px[frame, 3] = 255
    # borde interior iluminado
    edge = frame & ndi.binary_dilation(h_open)
    c.px[edge, :3] = rmp[4]
    if interior is None:
        c.px[h_open, :3] = dark
        c.px[h_open, 3] = 255
    else:
        interior(c, h_open)
    return h_open


def column(c, x, y_base, y_top, r, stone, cap=True, flutes=True):
    rmp = ramp(stone, 6, 0.08, 0.22, 1.45)
    xs = np.arange(x - r, x + r)
    for xx in xs:
        if xx < 0 or xx >= c.w:
            continue
        u = (xx + 0.5 - x) / r
        shade = np.clip(0.5 + (-u) * 0.45 + math.sqrt(max(0, 1 - u * u)) * 0.25, 0, 1)
        idx = min(len(rmp) - 1, int(shade * len(rmp)))
        c.px[y_top:y_base, xx, :3] = rmp[idx]
        c.px[y_top:y_base, xx, 3] = 255
        if flutes and int((xx - (x - r))) % 4 == 0:
            c.px[y_top + 6:y_base - 4, xx, :3] = rmp[max(0, idx - 1)]
    if cap:
        for (yy, ww, hh, tone) in [(y_top - 6, r + 5, 6, 4), (y_top - 2, r + 3, 3, 3), (y_base - 4, r + 4, 4, 3), (y_base, r + 7, 4, 2)]:
            xa, xb = max(0, x - ww), min(c.w, x + ww)
            yy = max(0, yy)
            c.px[yy:yy + hh, xa:xb, :3] = rmp[tone]
            c.px[yy:yy + hh, xa:xb, 3] = 255
            c.px[yy:yy + 1, xa:xb, :3] = rmp[5]
            c.px[yy + hh - 1:yy + hh, xa:xb, :3] = rmp[0]


def stained_window(c, cx, base_y, w, h, seed, palette, lead=(8, 8, 14)):
    """Vidriera ojival con celdas de color separadas por plomo; todo emisivo."""
    m = pointed_arch_mask(c.w, c.h, cx, base_y, w, h)
    rng = np.random.default_rng(seed)
    pts = rng.random((28, 2)) * [w, h] + [cx - w / 2, base_y - h]
    ys, xs = np.nonzero(m)
    d = (xs[:, None] - pts[None, :, 0]) ** 2 + (ys[:, None] - pts[None, :, 1]) ** 2
    cell = np.argmin(d, axis=1)
    cols = np.array([palette[int(i) % len(palette)] for i in range(len(pts))], np.uint8)
    c.px[ys, xs, :3] = cols[cell]
    c.px[ys, xs, 3] = 255
    # plomo: donde cambia de celda
    lab = np.full((c.h, c.w), -1)
    lab[ys, xs] = cell
    edge = np.zeros((c.h, c.w), bool)
    edge[:, 1:] |= (lab[:, 1:] != lab[:, :-1]) & (lab[:, 1:] >= 0) & (lab[:, :-1] >= 0)
    edge[1:, :] |= (lab[1:, :] != lab[:-1, :]) & (lab[1:, :] >= 0) & (lab[:-1, :] >= 0)
    c.px[edge, :3] = lead
    # parteluz central
    c.px[base_y - h:base_y, cx - 1:cx + 1, :3] = lead
    c.px[base_y - h + 6:base_y - h + 8, cx - w // 2:cx + w // 2, :3] = lead
    c.emit[m] = np.concatenate([c.px[m, :3], np.full((m.sum(), 1), 255, np.uint8)], axis=1)
    c.emit[edge | (c.px[..., :3] == lead).all(axis=2)] = (0, 0, 0, 0)
    return m


def flagstones(c, y, thickness, base, seed=0, moss=(70, 120, 90), grass=None, x0=0, x1=None):
    """Suelo de losas irregulares con caras biseladas, juntas oscuras, musgo y hierbajos en el borde."""
    x1 = x1 or c.w
    rmp = ramp(base, 5, 0.1, 0.3, 1.5)
    rng = np.random.default_rng(seed)
    x = x0
    # cara superior: hilera de losas
    while x < x1:
        w = int(rng.integers(14, 34))
        hh = int(rng.integers(5, 8))
        xa, xb = x, min(x1, x + w)
        tone = int(rng.integers(2, 4))
        c.px[y:y + hh, xa:xb, :3] = rmp[tone]
        c.px[y:y + hh, xa:xb, 3] = 255
        c.px[y:y + 1, xa:xb, :3] = rmp[4]
        c.px[y + hh - 1:y + hh, xa:xb, :3] = rmp[1]
        c.px[y:y + hh, xb - 1:xb, :3] = rmp[0]
        if rng.random() < 0.3:
            c.px[y + 1:y + 4, xa + 3:xb - 3, :3] = rmp[min(4, tone + 1)]
        x += w
    # frente: sillares oscuros que se hunden en negro
    masonry(c, x0, y + 7, x1, y + thickness, base, bw=26, bh=12, seed=seed + 3, moss=0.0, variation=0.12)
    # fundido a negro hacia abajo
    for i, yy in enumerate(range(y + 10, min(c.h, y + thickness))):
        f = min(1.0, (i + 1) / max(1, thickness - 10))
        row = c.px[yy, x0:x1, :3].astype(np.float32)
        c.px[yy, x0:x1, :3] = (row * (1 - f * 0.9)).astype(np.uint8)
    # musgo
    mm = (rng.random((c.h, c.w)) < 0.07)
    reg = np.zeros((c.h, c.w), bool)
    reg[y:y + 9, x0:x1] = True
    c.px[mm & reg & c.alpha_mask(), :3] = moss
    # hierba en el borde
    if grass:
        for xx in range(x0, x1):
            if rng.random() < 0.35:
                hgt = int(rng.integers(2, 6))
                for k in range(hgt):
                    c.set(xx, y - 1 - k, grass if k < hgt - 1 else mix(grass, (200, 230, 190), 0.3))


# ----------------------------------------------------------------------------------------------
# Iluminacion
# ----------------------------------------------------------------------------------------------
def _radial(w, h, cx, cy, radius, power=2.0):
    yy, xx = np.mgrid[0:h, 0:w]
    d = np.hypot(xx - cx, yy - cy) / radius
    return np.clip(1 - d, 0, 1) ** power


def light_pass(img, emit, ambient, lights=(), fog=None, vignette=0.55, grain=0.03, bloom=0.55, seed=1, shafts=()):
    """
    img, emit: arrays RGBA uint8 (escena completa y capa de emision).
    ambient: (r,g,b) 0..1 multiplicador global.
    lights: (x, y, radio, (r,g,b), intensidad, power). Se multiplican por el color de la escena (iluminan la piedra)
            y ademas suman un resplandor suave.
    fog: [(y, alto, (r,g,b), fuerza, escala_ruido)] bandas de niebla.
    shafts: [(x_top, y_top, x_bot, y_bot, ancho, (r,g,b), fuerza)] rayos de luz.
    """
    h, w = img.shape[:2]
    base = img[..., :3].astype(np.float32) / 255.0
    out = base * np.array(ambient, np.float32)
    add = np.zeros_like(out)
    for (lx, ly, rad, col, inten, *rest) in lights:
        power = rest[0] if rest else 2.0
        f = _radial(w, h, lx, ly, rad, power)[..., None]
        col = np.array(col, np.float32)
        out += base * f * col * inten * 1.6            # ilumina el relieve
        add += f * col * inten * 0.22                  # resplandor
    out += add
    # rayos de luz
    for (xt, yt, xb, yb, wd, col, strength) in shafts:
        beam = np.zeros((h, w), np.float32)
        pts = [(xt - wd * 0.3, yt), (xt + wd * 0.3, yt), (xb + wd, yb), (xb - wd, yb)]
        im = Image.new("L", (w, h), 0)
        from PIL import ImageDraw
        ImageDraw.Draw(im).polygon(pts, fill=255)
        im = im.filter(ImageFilter.GaussianBlur(5))
        beam = np.asarray(im, np.float32) / 255.0
        yy = np.linspace(0, 1, h)[:, None]
        fade = np.clip(1 - np.abs((yy * h - yt) / max(1, (yb - yt))) , 0, 1)
        out += beam[..., None] * np.array(col, np.float32) * strength * 0.5
    # niebla
    if fog:
        for k, (fy, fh, fcol, fs, fscale) in enumerate(fog):
            n = noise2d(w, h, fscale, seed=seed + k, octaves=3)
            rows = np.arange(h)[:, None]
            prof = np.clip(1 - np.abs(rows - fy) / fh, 0, 1) ** 1.5
            f = (prof * (0.35 + n * 0.9) * fs)[..., None]
            out = out * (1 - np.clip(f, 0, 0.9)) + np.array(fcol, np.float32) * np.clip(f, 0, 0.9)
    # emision y bloom
    em = emit[..., :3].astype(np.float32) / 255.0 * (emit[..., 3:4].astype(np.float32) / 255.0)
    out = out * (1 - np.clip(em.max(axis=2, keepdims=True), 0, 1)) + em
    if bloom:
        e_img = Image.fromarray((np.clip(em, 0, 1) * 255).astype(np.uint8))
        for r, k in ((3, 0.55), (9, 0.4), (20, 0.35)):
            b = np.asarray(e_img.filter(ImageFilter.GaussianBlur(r)), np.float32) / 255.0
            out += b * k * bloom
    # viñeta
    yy, xx = np.mgrid[0:h, 0:w]
    d = np.hypot((xx - w / 2) / (w / 2), (yy - h / 2) / (h / 2))
    out *= (1 - vignette * np.clip(d - 0.35, 0, 1) ** 1.6)[..., None]
    # grano
    rng = np.random.default_rng(seed)
    out += (rng.random((h, w, 1)) - 0.5) * grain
    return (np.clip(out, 0, 1) * 255).astype(np.uint8)


def flatten(layers, w=W, h=H):
    """Compone una lista de Canvas (fondo -> frente). Devuelve (rgba, emision)."""
    base = Canvas(w, h)
    for ly in layers:
        base.blit(ly, 0, 0, glow=True)
    return base.px.copy(), base.emit.copy()
