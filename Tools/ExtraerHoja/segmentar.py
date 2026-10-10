# Detecta los elementos sueltos de la hoja conceptual (fondo oscuro casi uniforme) y guarda una imagen anotada
# con el número y el recuadro de cada uno, para elegir cuáles se recortan.
# Uso: python3 -I segmentar.py <hoja.png> <salida_dir>
import sys, os, json
import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage as ndi

src, out = sys.argv[1], sys.argv[2]
os.makedirs(out, exist_ok=True)
im = np.asarray(Image.open(src).convert('RGB')).astype(np.float32)
L = im @ np.array([0.299, 0.587, 0.114])
# Fondo local: mediana muy amplia sobre una versión reducida.
small = Image.fromarray(L.astype(np.uint8)).resize((im.shape[1] // 8, im.shape[0] // 8), Image.BILINEAR)
bg = ndi.median_filter(np.asarray(small).astype(np.float32), size=9)
bg = np.asarray(Image.fromarray(bg.astype(np.uint8)).resize((im.shape[1], im.shape[0]), Image.BILINEAR)).astype(np.float32)
sat = im.max(-1) - im.min(-1)
mask = (L - bg > 9) | (sat > 28)
mask = ndi.binary_closing(mask, iterations=2)
mask = ndi.binary_fill_holes(mask)
mask = ndi.binary_opening(mask, iterations=1)
lab, n = ndi.label(ndi.binary_dilation(mask, iterations=2))
boxes = []
for i, sl in enumerate(ndi.find_objects(lab), 1):
    h, w = sl[0].stop - sl[0].start, sl[1].stop - sl[1].start
    area = (lab[sl] == i).sum()
    if area < 120 or w < 8 or h < 8: continue
    boxes.append(dict(id=len(boxes), x=sl[1].start, y=sl[0].start, w=w, h=h, area=int(area)))
json.dump(boxes, open(os.path.join(out, 'cajas.json'), 'w'), indent=0)
ann = Image.open(src).convert('RGB').resize((im.shape[1] * 2, im.shape[0] * 2), Image.NEAREST)
d = ImageDraw.Draw(ann)
for b in boxes:
    d.rectangle([b['x'] * 2, b['y'] * 2, (b['x'] + b['w']) * 2, (b['y'] + b['h']) * 2], outline=(255, 0, 255))
    d.text((b['x'] * 2 + 2, b['y'] * 2 + 1), str(b['id']), fill=(255, 255, 0))
ann.save(os.path.join(out, 'anotada.png'))
Image.fromarray((mask * 255).astype(np.uint8)).save(os.path.join(out, 'mascara.png'))
print(len(boxes), 'elementos')
