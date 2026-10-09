"""
pxl.py - mini motor de pixel art para ABISMO.

Misma filosofia que Editor/Art/ShadedCanvas.cs del proyecto:
  * Cada pieza es una forma con un mapa de alturas (volumen 2.5D).
  * Se ilumina por BANDAS con rampas de matiz desplazado (sombras frias, luces calidas).
  * Contornos selectivos de color (nunca negro puro), contraluz (rim) y sombras de contacto.
  * Lo que brilla se dibuja en una capa aparte (emision) que alimenta el bloom.

Todo trabaja a resolucion nativa del juego (640x360, 32 px = 1 casilla); la vista previa
se escala con vecino mas cercano.
"""
import colorsys
import math

import numpy as np
from PIL import Image
from scipy import ndimage as ndi

# ----------------------------------------------------------------------------
# Color
# ----------------------------------------------------------------------------


def hex2rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def _toward_hue(h, target, amount):
    d = target - h
    if d > 0.5:
        d -= 1.0
    if d < -0.5:
        d += 1.0
    r = h + d * max(0.0, min(1.0, amount))
    return r % 1.0


def ramp(base, steps=5, hue_shift=0.08, darkest=0.32, lightest=1.45):
    """Rampa de oscuro a claro. Sombras hacia azul-violeta, luces hacia amarillo calido."""
    if isinstance(base, str):
        base = hex2rgb(base)
    h, s, v = colorsys.rgb_to_hsv(*(c / 255.0 for c in base))
    out = []
    for i in range(steps):
        t = 0.5 if steps == 1 else i / (steps - 1)
        val = max(0.0, min(1.0, v * (darkest + (lightest - darkest) * t)))
        if t < 0.5:
            hue = _toward_hue(h, 0.70, hue_shift * (0.5 - t) * 2)
        else:
            hue = _toward_hue(h, 0.13, hue_shift * (t - 0.5) * 2)
        sat = max(0.0, min(1.0, s * (1.08 + (0.78 - 1.08) * t) * (0.9 if t < 0.15 else 1.0)))
        r, g, b = colorsys.hsv_to_rgb(hue, sat, val)
        out.append((round(r * 255), round(g * 255), round(b * 255)))
    return out


def shadow_of(c, amount=0.55):
    h, s, v = colorsys.rgb_to_hsv(*(x / 255.0 for x in c))
    h = _toward_hue(h, 0.72, amount * 0.5)
    r, g, b = colorsys.hsv_to_rgb(h, min(1.0, s * (1 + amount * 0.2)), v * (1 - amount))
    return (round(r * 255), round(g * 255), round(b * 255))


def mix(a, b, t):
    return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(3))


# ----------------------------------------------------------------------------
# Formas: devuelven Shape (mascara bool, altura 0..1, escala de relieve en px)
# ----------------------------------------------------------------------------


class Shape:
    """Se desempaqueta como (mascara, altura); `scale` es el grosor aproximado en px (para la normal)."""
    __slots__ = ("mask", "height", "scale")

    def __init__(self, mask, height, scale=3.2):
        self.mask, self.height, self.scale = mask, height, scale

    def __iter__(self):
        yield self.mask
        yield self.height

    def __getitem__(self, i):
        return (self.mask, self.height)[i]


_BAYER4 = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]], dtype=np.float32) / 16.0 - 0.5


def _grid(w, h):
    ys, xs = np.mgrid[0:h, 0:w]
    return xs.astype(np.float32) + 0.5, ys.astype(np.float32) + 0.5


def height_from_mask(mask, bevel=4.0, power=0.7):
    """Altura abombada a partir de la distancia al borde (volumen de cojin)."""
    d = ndi.distance_transform_edt(np.pad(mask, 1))[1:-1, 1:-1].astype(np.float32)
    t = np.clip(d / max(0.5, bevel), 0, 1)
    return np.where(mask, np.power(1.0 - (1.0 - t) ** 2, power), 0.0).astype(np.float32)


def ellipse(w, h, cx, cy, rx, ry, rot=0.0):
    xs, ys = _grid(w, h)
    dx, dy = xs - cx, ys - cy
    if rot:
        c, s = math.cos(math.radians(rot)), math.sin(math.radians(rot))
        dx, dy = dx * c + dy * s, -dx * s + dy * c
    d2 = (dx / rx) ** 2 + (dy / ry) ** 2
    mask = d2 <= 1.0
    height = np.sqrt(np.clip(1.0 - d2, 0, 1))
    return Shape(mask, height.astype(np.float32), max(1.5, min(rx, ry)))


def capsule(w, h, p0, p1, r0, r1=None):
    """Segmento redondeado (miembros, tentaculos). Altura abombada en el eje."""
    if r1 is None:
        r1 = r0
    xs, ys = _grid(w, h)
    x0, y0 = p0
    x1, y1 = p1
    vx, vy = x1 - x0, y1 - y0
    L2 = vx * vx + vy * vy + 1e-6
    t = np.clip(((xs - x0) * vx + (ys - y0) * vy) / L2, 0, 1)
    px, py = x0 + t * vx, y0 + t * vy
    r = r0 + (r1 - r0) * t
    d = np.sqrt((xs - px) ** 2 + (ys - py) ** 2)
    mask = d <= r
    height = np.sqrt(np.clip(1.0 - (d / np.maximum(r, 0.01)) ** 2, 0, 1))
    return Shape(mask, height.astype(np.float32), max(1.2, (r0 + r1) / 2))


def polygon(w, h, pts, bevel=3.0, power=0.7):
    img = Image.new("L", (w, h), 0)
    from PIL import ImageDraw
    ImageDraw.Draw(img).polygon([(float(x), float(y)) for x, y in pts], fill=255)
    mask = np.array(img) > 0
    return Shape(mask, height_from_mask(mask, bevel, power), max(1.2, bevel))


def rect(w, h, x0, y0, x1, y1, bevel=2.0, power=0.8):
    mask = np.zeros((h, w), bool)
    mask[int(y0):int(y1), int(x0):int(x1)] = True
    return Shape(mask, height_from_mask(mask, bevel, power), max(1.2, bevel))


def bezier_pts(a, c, b, n=24):
    out = []
    for i in range(n):
        t = i / (n - 1)
        u = 1 - t
        out.append((u * u * a[0] + 2 * u * t * c[0] + t * t * b[0], u * u * a[1] + 2 * u * t * c[1] + t * t * b[1]))
    return out


def tentacle(w, h, pts, r0, r1):
    """Curva gruesa con radio variable: union de capsulas encadenadas."""
    mask = np.zeros((h, w), bool)
    height = np.zeros((h, w), np.float32)
    n = len(pts)
    for i in range(n - 1):
        t0, t1 = i / (n - 1), (i + 1) / (n - 1)
        m, hh = capsule(w, h, pts[i], pts[i + 1], r0 + (r1 - r0) * t0, r0 + (r1 - r0) * t1)
        height = np.where(m, np.maximum(height, hh), height)
        mask |= m
    return Shape(mask, height, max(1.2, (r0 + r1) / 2))


# ----------------------------------------------------------------------------
# Lienzo
# ----------------------------------------------------------------------------


class Canvas:
    """Lienzo RGBA (y hacia abajo). `emit` es la capa de emision (lo que brilla)."""

    def __init__(self, w, h):
        self.w, self.h = w, h
        self.px = np.zeros((h, w, 4), np.uint8)
        self.emit = np.zeros((h, w, 4), np.uint8)
        # Luz principal: arriba a la izquierda y algo hacia la camara (x, y, z con y hacia arriba).
        self.light = np.array([-0.55, 0.65, 0.55], np.float32)
        self.light /= np.linalg.norm(self.light)

    # -- basicos ---------------------------------------------------------
    def set_light(self, x, y, z):
        v = np.array([x, y, z], np.float32)
        self.light = v / np.linalg.norm(v)

    def fill(self, mask, color, alpha=255):
        self.px[mask] = (*color[:3], alpha)

    def set(self, x, y, color, alpha=255):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.px[int(y), int(x)] = (*color[:3], alpha)

    def glow_set(self, x, y, color, alpha=255):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.emit[int(y), int(x)] = (*color[:3], alpha)
            self.px[int(y), int(x)] = (*color[:3], alpha)

    def alpha_mask(self):
        return self.px[..., 3] > 0

    # -- sombreado -------------------------------------------------------
    def shade(self, shape, rmp, bands=None, dither=0.0, noise=None, noise_amt=0.0, outline=True,
              rim=None, rim_amt=0.35, ambient=0.18, contrast=1.0, bias=0.0, tint=None):
        """
        Pinta una forma con volumen. `rmp` = lista de colores oscuro->claro.
        dither: 0..1, cuanto se tramado entre bandas. noise: array HxW para perturbar el relieve.
        outline: True = contorno de la propia pieza (oscuro, mas suave en el lado iluminado).
        rim: color de contraluz opcional (luz a la derecha/atras).
        """
        mask, height = shape
        relief = getattr(shape, 'scale', 3.2)
        if not mask.any():
            return
        n = len(rmp) if bands is None else bands
        hgt = height.copy()
        if noise is not None and noise_amt:
            hgt = np.clip(hgt + (noise - 0.5) * noise_amt, 0, 1)
        gy, gx = np.gradient(hgt)
        # normal en espacio pantalla con y hacia arriba
        nx, ny, nz = -gx * relief, gy * relief, np.ones_like(hgt)
        ln = np.sqrt(nx * nx + ny * ny + nz * nz)
        nx, ny, nz = nx / ln, ny / ln, nz / ln
        lam = nx * self.light[0] + ny * self.light[1] + nz * self.light[2]
        lam = np.clip(lam * 0.5 + 0.5, 0, 1)
        lam = np.clip(ambient + (lam - ambient) * contrast + bias, 0, 1)
        if dither > 0:
            by = np.tile(_BAYER4, (self.h // 4 + 1, self.w // 4 + 1))[: self.h, : self.w]
            lam = np.clip(lam + by * dither / max(1, n - 1), 0, 1)
        idx = np.clip((lam * n).astype(np.int32), 0, n - 1)
        # mapear bandas a la rampa si hay menos bandas que colores
        if bands is not None and bands != len(rmp):
            idx = np.clip(np.round(idx * (len(rmp) - 1) / max(1, n - 1)).astype(np.int32), 0, len(rmp) - 1)
        cols = np.array(rmp, np.uint8)[idx]
        if tint is not None:
            cols = (cols * 0.85 + np.array(tint, np.float32) * 0.15).astype(np.uint8)
        out = self.px.copy()
        out[mask, :3] = cols[mask]
        out[mask, 3] = 255
        if rim is not None:
            # contraluz: borde interior del lado derecho/inferior
            er = ndi.binary_erosion(mask, structure=np.array([[0, 0, 0], [1, 1, 0], [0, 0, 0]], bool))
            rim_px = mask & ~er
            out[rim_px, :3] = (np.array(out[rim_px, :3], np.float32) * (1 - rim_amt) + np.array(rim, np.float32) * rim_amt).astype(np.uint8)
        if outline:
            er = ndi.binary_erosion(mask)
            edge = mask & ~er
            dark = np.array(shadow_of(rmp[0], 0.35), np.uint8)
            # selectivo: en el lado iluminado (arriba/izquierda) el contorno es un tono mas claro
            up = np.zeros_like(mask)
            up[1:, :] = ~mask[:-1, :]
            left = np.zeros_like(mask)
            left[:, 1:] = ~mask[:, :-1]
            lit_edge = edge & (up | left)
            soft = np.array(rmp[0], np.uint8)
            out[edge, :3] = dark
            out[lit_edge, :3] = soft
        self.px = out

    def outline_all(self, color=None, mode="dark", skip_lit=False):
        """Contorno exterior de TODO el sprite (1 px por fuera de la silueta)."""
        m = self.alpha_mask()
        grown = ndi.binary_dilation(m, structure=ndi.generate_binary_structure(2, 1))
        edge = grown & ~m
        if color is None:
            color = (14, 16, 24)
        if skip_lit:
            # no dibujes contorno sobre el lado de arriba-izquierda (la luz borra el borde)
            pass
        self.px[edge] = (*color[:3], 255)

    def inner_edges(self, color_fn=None):
        pass

    def drop_shadow(self, dx=0, dy=0, color=(8, 10, 16), alpha=120):
        m = self.alpha_mask()
        sh = np.zeros_like(m)
        sh[max(0, dy):, max(0, dx):] = m[: self.h - max(0, dy), : self.w - max(0, dx)]
        sh &= ~m
        self.px[sh] = (*color, alpha)

    def contact_shadow(self, y, x0, x1, color=(6, 8, 12), alpha=110, squash=0.35):
        cx, rx = (x0 + x1) / 2, (x1 - x0) / 2
        ry = max(1.5, rx * squash * 0.25)
        m, _ = ellipse(self.w, self.h, cx, y, rx, ry)
        under = m & ~self.alpha_mask()
        self.px[under] = (*color, alpha)

    # -- texturas ---------------------------------------------------------
    def stipple(self, mask, color, density=0.1, seed=0, alpha=255):
        rng = np.random.default_rng(seed)
        r = rng.random((self.h, self.w)) < density
        m = mask & r & self.alpha_mask()
        self.px[m] = (*color[:3], alpha)

    def hline(self, x0, x1, y, color, alpha=255):
        for x in range(int(x0), int(x1) + 1):
            self.set(x, y, color, alpha)

    def vline(self, x, y0, y1, color, alpha=255):
        for y in range(int(y0), int(y1) + 1):
            self.set(x, y, color, alpha)

    def line(self, p0, p1, color, alpha=255, glow=False):
        x0, y0 = p0
        x1, y1 = p1
        n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
        for i in range(n + 1):
            t = i / max(1, n)
            x = round(x0 + (x1 - x0) * t)
            y = round(y0 + (y1 - y0) * t)
            (self.glow_set if glow else self.set)(x, y, color, alpha)

    def disc(self, cx, cy, r, color, glow=False):
        for y in range(int(cy - r - 1), int(cy + r + 2)):
            for x in range(int(cx - r - 1), int(cx + r + 2)):
                if (x + 0.5 - cx) ** 2 + (y + 0.5 - cy) ** 2 <= r * r:
                    (self.glow_set if glow else self.set)(x, y, color)

    def blit(self, other, ox, oy, glow=True):
        """Pega otro lienzo (alfa 'sobre')."""
        for layer_name in (("px", "px"), ("emit", "emit")) if glow else (("px", "px"),):
            src = getattr(other, layer_name[0])
            dst = getattr(self, layer_name[1])
            sx0, sy0 = max(0, -ox), max(0, -oy)
            sx1, sy1 = min(other.w, self.w - ox), min(other.h, self.h - oy)
            if sx1 <= sx0 or sy1 <= sy0:
                continue
            region = src[sy0:sy1, sx0:sx1]
            d = dst[sy0 + oy: sy1 + oy, sx0 + ox: sx1 + ox]
            a = region[..., 3:4].astype(np.float32) / 255.0
            da = d[..., 3:4].astype(np.float32) / 255.0
            oa = a + da * (1 - a)
            oc = (region[..., :3] * a + d[..., :3] * da * (1 - a)) / np.maximum(oa, 1e-6)
            d[..., :3] = oc.astype(np.uint8)
            d[..., 3:4] = (oa * 255).astype(np.uint8)

    # -- salida -----------------------------------------------------------
    def to_image(self):
        return Image.fromarray(self.px, "RGBA")

    def save(self, path, scale=1):
        im = self.to_image()
        if scale != 1:
            im = im.resize((im.width * scale, im.height * scale), Image.NEAREST)
        im.save(path)


def noise2d(w, h, scale=4, seed=0, octaves=2):
    """Ruido de valor suave en 0..1 (para musgo, tela, roca)."""
    rng = np.random.default_rng(seed)
    out = np.zeros((h, w), np.float32)
    amp, tot = 1.0, 0.0
    for o in range(octaves):
        s = max(1, int(scale / (2 ** o)))
        gw, gh = w // s + 3, h // s + 3
        g = rng.random((gh, gw)).astype(np.float32)
        im = Image.fromarray((g * 255).astype(np.uint8)).resize((gw * s, gh * s), Image.BICUBIC)
        a = np.array(im, np.float32)[:h, :w] / 255.0
        out += a * amp
        tot += amp
        amp *= 0.5
    return out / tot


def sheet(items, cols, cell_w, cell_h, scale=3, bg=(12, 15, 20), labels=None, pad=8, label_h=14):
    """Hoja de contacto con vecino mas cercano, para revisar el arte."""
    from PIL import ImageDraw
    rows = (len(items) + cols - 1) // cols
    W = cols * (cell_w * scale + pad) + pad
    H = rows * (cell_h * scale + pad + (label_h if labels else 0)) + pad
    img = Image.new("RGBA", (W, H), (*bg, 255))
    d = ImageDraw.Draw(img)
    for i, it in enumerate(items):
        im = it.to_image() if hasattr(it, "to_image") else it
        im = im.resize((im.width * scale, im.height * scale), Image.NEAREST)
        x = pad + (i % cols) * (cell_w * scale + pad)
        y = pad + (i // cols) * (cell_h * scale + pad + (label_h if labels else 0))
        img.alpha_composite(im, (x, y))
        if labels:
            d.text((x, y + cell_h * scale + 1), labels[i], fill=(200, 205, 215, 255))
    return img
