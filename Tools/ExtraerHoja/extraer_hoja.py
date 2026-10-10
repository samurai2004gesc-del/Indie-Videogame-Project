# Extrae de la hoja conceptual los elementos que el juego puede usar tal cual (fondos pintados y decorado)
# y los deja en ArteHoja/ con fondo transparente. El constructor de Unity (Editor/Art/HojaArt.cs) los lee de ahí.
#
# Uso (desde la raíz del proyecto):
#   python3 -I Tools/ExtraerHoja/extraer_hoja.py ArteHoja/hoja_original.webp ArteHoja
#
# Las cajas están medidas sobre la hoja original (1536×1024). Para añadir otro elemento, busca su caja con
# segmentar.py (imagen anotada) y añádelo a DECORADO.
import sys, os, json
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

SRC, OUT = sys.argv[1], sys.argv[2]
sheet = Image.open(SRC).convert('RGB')
S = np.asarray(sheet).astype(np.float32)

# ------------------------------------------------------------------
# Fondos pintados: (zona, caja del panel). Se recorta por debajo del título impreso sobre el primer panel.
# ------------------------------------------------------------------
FONDOS = {
    'costa':     (0, 670, 423, 941),     # castillo sobre el acantilado y la luna
    'ruinas':    (425, 670, 781, 941),   # ruinas sobre el mar y el remolino-tentáculo
    'arrecife':  (783, 670, 1214, 941),  # cielo rojo con la criatura (presagio del jefe final)
    'santuario': (1216, 670, 1536, 941), # interior de catedral con la estatua y la vidriera
}
LAYER_W = 768        # ancho de las capas de fondo del juego (se repiten en horizontal)
TARGET_H = 380       # alto al que se escala el panel (la vista mide 360)

def fondo(box):
    p = sheet.crop(box)
    k = TARGET_H / p.height
    p = p.resize((round(p.width * k), TARGET_H), Image.LANCZOS)
    a = np.asarray(p).astype(np.float32)
    h, w = a.shape[:2]
    if w >= LAYER_W:
        x0 = (w - LAYER_W) // 2
        out = a[:, x0:x0 + LAYER_W]
    else:
        # Se completa hasta LAYER_W con el reflejo de los bordes, fundiendo el reflejo del borde derecho con el del
        # izquierdo para que la capa empalme consigo misma al repetirse.
        extra = LAYER_W - w
        out = np.zeros((h, LAYER_W, 3), np.float32)
        out[:, :w] = a
        for i in range(extra):
            t = (i + 0.5) / extra
            right = a[:, max(0, w - 1 - i)]              # sigue al borde derecho
            left = a[:, min(w - 1, extra - 1 - i)]       # acaba en el borde izquierdo
            out[:, w + i] = right * (1 - t) + left * t
    img = Image.fromarray(np.clip(out, 0, 255).astype(np.uint8))
    # Paleta corta: devuelve el aspecto de pixel art tras el escalado y quita el ruido de compresión.
    img = img.quantize(colors=96, method=Image.MEDIANCUT, dither=Image.NONE).convert('RGBA')
    return img

# ------------------------------------------------------------------
# Decorado: nombre → caja (x, y, ancho, alto) en la hoja.
# ------------------------------------------------------------------
DECORADO = {
    # Tileset / escenarios
    'h_columna':          (95, 351, 32, 123),
    'h_columna_rota':     (214, 495, 37, 131),
    'h_columnilla':       (255, 559, 20, 68),
    'h_aguja':            (401, 366, 28, 177),
    'h_portico':          (431, 420, 72, 117),
    'h_hornacina':        (273, 350, 53, 67),
    'h_ventana_a':        (204, 352, 39, 57),
    'h_ventana_b':        (245, 352, 28, 57),
    'h_verja':            (270, 420, 54, 77),
    'h_cadenas':          (511, 440, 34, 96),
    'h_farola':           (549, 422, 32, 151),
    'h_farolillo':        (623, 503, 13, 29),
    'h_farol_colgante':   (716, 438, 32, 64),
    'h_relicario':        (668, 401, 40, 101),
    'h_estatua_verde':    (625, 392, 37, 98),
    'h_estatua_velada':   (576, 457, 41, 116),
    'h_lampara_ojo':      (588, 408, 28, 41),
    'h_tentaculo':        (323, 581, 122, 46),
    'h_escombros':        (539, 577, 107, 59),
    'h_puente':           (453, 578, 74, 50),
    'h_balcon':           (508, 388, 56, 50),
    'h_balaustrada':      (432, 352, 69, 63),
    'h_altar_piedra':     (332, 405, 64, 40),
    # Objetos y NPCs (estáticos)
    'h_estatua_monje':    (1027, 37, 31, 94),
    'h_idolo_dorado':     (1115, 37, 49, 94),
    'h_linterna':         (1028, 140, 21, 59),
    'h_cofre':            (1065, 148, 49, 49),
    'h_capilla':          (1125, 143, 45, 55),
    'h_vela':             (1182, 151, 17, 48),
    'h_relicario_alto':   (1020, 203, 40, 92),
    'h_estatua_peregrino':(1020, 296, 40, 86),
    'h_cruz':             (1070, 207, 38, 54),
    'h_hornacina_farol':  (1127, 205, 37, 63),
    'h_cruz_pequena':     (1179, 203, 23, 58),
    'h_puerta':           (1064, 266, 47, 115),
    'h_estatua_capucha':  (1116, 273, 48, 108),
    'h_estatua_pilar':    (1166, 265, 44, 116),
    # Interfaz y niebla
    'h_ojo_emblema':      (1380, 412, 76, 64),
    'h_niebla_a':         (1319, 338, 111, 53),
    'h_niebla_b':         (1429, 339, 97, 52),
}
# Elementos que no tocan el suelo (no se recortan sus filas vacías de abajo para conservar el pivote arriba).
COLGANTES = {'h_farol_colgante', 'h_lampara_ojo', 'h_cadenas'}
NIEBLA = {'h_niebla_a', 'h_niebla_b'}

L_all = S @ np.array([0.299, 0.587, 0.114])

def recorte(name, box):
    x, y, w, h = box
    pad = 3
    x0, y0, x1, y1 = max(0, x - pad), max(0, y - pad), x + w + pad, y + h + pad
    a = S[y0:y1, x0:x1]
    L = L_all[y0:y1, x0:x1]
    # Color del fondo: mediana del marco exterior del recorte.
    border = np.concatenate([L[0], L[-1], L[:, 0], L[:, -1]])
    bgL = np.median(border)
    sat = a.max(-1) - a.min(-1)
    if name in NIEBLA:
        # La niebla es semitransparente: alfa según lo que se aparta del fondo.
        alpha = np.clip((L - bgL - 4) / 60.0, 0, 1) * 0.85
        rgb = np.clip(a, 0, 255)
        img = np.dstack([rgb, alpha * 255])
        return Image.fromarray(img.astype(np.uint8), 'RGBA')
    fg = (L - bgL > 10) | (sat > 26)
    fg = ndi.binary_closing(fg, iterations=1)
    fg = ndi.binary_fill_holes(fg)
    lab, n = ndi.label(fg)
    if n == 0: return None
    sizes = ndi.sum(fg, lab, range(1, n + 1))
    keep = np.zeros_like(fg)
    big = sizes.max()
    for i, sz in enumerate(sizes, 1):
        if sz >= max(12, big * 0.04): keep |= lab == i
    # Contorno oscuro de 1 px alrededor (el arte de la hoja lo tiene): se conserva si es más oscuro que el fondo.
    ring = ndi.binary_dilation(keep, iterations=1) & ~keep
    keep |= ring & (L < bgL + 6) & (L > 3)
    rgb = a.copy()
    alpha = keep.astype(np.float32) * 255
    img = Image.fromarray(np.dstack([rgb, alpha]).astype(np.uint8), 'RGBA')
    # Recorta al contenido.
    bb = img.getbbox()
    if bb is None: return None
    img = img.crop(bb)
    # Paleta corta por elemento (limpia el ruido de compresión).
    rgbq = img.convert('RGB').quantize(colors=40, method=Image.MEDIANCUT, dither=Image.NONE).convert('RGB')
    q = np.dstack([np.asarray(rgbq), np.asarray(img)[..., 3]])
    q[q[..., 3] < 128] = 0
    q[q[..., 3] >= 128, 3] = 255
    return Image.fromarray(q.astype(np.uint8), 'RGBA')

os.makedirs(os.path.join(OUT, 'fondos'), exist_ok=True)
os.makedirs(os.path.join(OUT, 'decorado'), exist_ok=True)
meta = {'fondos': {}, 'decorado': {}}
for zona, box in FONDOS.items():
    img = fondo(box)
    img.save(os.path.join(OUT, 'fondos', zona + '.png'))
    meta['fondos'][zona] = {'caja': box, 'tamano': img.size}
for name, box in DECORADO.items():
    img = recorte(name, box)
    if img is None: print('vacío:', name); continue
    img.save(os.path.join(OUT, 'decorado', name + '.png'))
    meta['decorado'][name] = {'caja': box, 'tamano': img.size}
json.dump(meta, open(os.path.join(OUT, 'extraccion.json'), 'w'), indent=1, ensure_ascii=False)
print('fondos', len(meta['fondos']), 'decorado', len(meta['decorado']))
