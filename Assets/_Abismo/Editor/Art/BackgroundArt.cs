using System.Collections.Generic;
using UnityEngine;

namespace Abismo.EditorTools
{
    /// <summary>Una capa de fondo que se repite en horizontal (el borde izquierdo empalma con el derecho).</summary>
    public sealed class BackgroundLayer
    {
        public string Name;
        public PixelCanvas Canvas;
        /// <summary>0 = pegada al mundo, 1 = pegada a la cámara (x, y).</summary>
        public Vector2 Parallax;
        /// <summary>sortingOrder (más negativo = más lejos).</summary>
        public int Order;
        /// <summary>Unidades entre el borde inferior de la vista inicial y el borde inferior de la capa.</summary>
        public float BottomOffset;
        public Color32 Tint = new Color32(255, 255, 255, 255);
        /// <summary>Desplazamiento automático (unidades/s), p. ej. niebla.</summary>
        public Vector2 Scroll;
        /// <summary>Si recibe las luces 2D. Los fondos ya vienen pintados con su luz, así que por defecto no.</summary>
        public bool Lit;
        /// <summary>Color con el que rellenar por debajo / por encima de la capa (alpha 0 = sin relleno).</summary>
        public Color32 FillBelow, FillAbove;
    }

    /// <summary>
    /// Fondos con paralaje por zona, pintados con perspectiva atmosférica: cuanto más lejos, menos contraste y más
    /// del color de la niebla. Todos se repiten en horizontal sin costuras (el ruido es periódico en el ancho).
    /// </summary>
    public static class BackgroundArt
    {
        const int W = 768;
        const int H = 360;

        public static List<BackgroundLayer> For(Zone zone)
        {
            switch (zone)
            {
                case Zone.Ruins: return Ruins();
                case Zone.Sanctuary: return Sanctuary();
                case Zone.Reef: return Reef();
                default: return Coast();
            }
        }

        // ------------------------------------------------------------------
        // Herramientas de pintura (x siempre se repite módulo W)
        // ------------------------------------------------------------------

        static readonly float[,] Bayer =
        {
            { 0f / 16f, 8f / 16f, 2f / 16f, 10f / 16f },
            { 12f / 16f, 4f / 16f, 14f / 16f, 6f / 16f },
            { 3f / 16f, 11f / 16f, 1f / 16f, 9f / 16f },
            { 15f / 16f, 7f / 16f, 13f / 16f, 5f / 16f },
        };

        static Color32 C(string hex) => PixelCanvas.Hex(hex);
        static int Wrap(int x) => ((x % W) + W) % W;

        static void Put(PixelCanvas c, int x, int y, Color32 col)
        {
            if (y < 0 || y >= c.Height) return;
            c.Pixels[y * c.Width + Wrap(x)] = col;
        }

        static void Blend(PixelCanvas c, int x, int y, Color32 col)
        {
            if (y < 0 || y >= c.Height) return;
            c.Blend(Wrap(x), y, col);
        }

        static Color32 Get(PixelCanvas c, int x, int y) => y < 0 || y >= c.Height ? PixelCanvas.Clear : c.Pixels[y * c.Width + Wrap(x)];

        /// <summary>Ruido 1D periódico en el ancho de la capa (scale debe dividir a W).</summary>
        static float N1(float x, int scale, int seed) => PixelCanvas.ValueNoise(x / scale, seed * 3.7f, W / scale, seed);

        static float Fbm1(float x, int scale, int seed, int octaves = 3)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int o = 0; o < octaves; o++)
            {
                sum += N1(x, Mathf.Max(1, scale >> o), seed + o * 11) * amp;
                norm += amp;
                amp *= 0.5f;
            }
            return sum / norm;
        }

        /// <summary>Degradado vertical por bandas con tramado ordenado entre ellas (cielo de pixel art).</summary>
        static void Gradient(PixelCanvas c, int y0, int y1, params Color32[] stops)
        {
            int bands = (stops.Length - 1) * 4;
            for (int y = y0; y < y1; y++)
            {
                float t = Mathf.InverseLerp(y0, y1 - 1, y) * bands;
                for (int x = 0; x < W; x++)
                {
                    float k = t + (Bayer[y & 3, x & 3] - 0.5f) * 0.9f;
                    int band = Mathf.Clamp(Mathf.FloorToInt(k), 0, bands);
                    float s = band / (float)bands * (stops.Length - 1);
                    int i = Mathf.Min(stops.Length - 2, Mathf.FloorToInt(s));
                    c.Pixels[y * W + x] = PixelCanvas.Lerp(stops[i], stops[i + 1], s - i);
                }
            }
        }

        /// <summary>Rellena desde abajo hasta la altura dada por columna.</summary>
        static void Silhouette(PixelCanvas c, System.Func<int, float> height, Color32 col, int from = 0)
        {
            for (int x = 0; x < W; x++)
            {
                int h = Mathf.Min(c.Height, Mathf.RoundToInt(height(x)));
                for (int y = from; y < h; y++) c.Pixels[y * W + x] = col;
            }
        }

        static void Polygon(PixelCanvas c, IList<Vector2> pts, Color32 col)
        {
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var p in pts) { minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y); }
            var xs = new List<float>();
            for (int y = Mathf.Max(0, Mathf.FloorToInt(minY)); y <= Mathf.Min(c.Height - 1, Mathf.CeilToInt(maxY)); y++)
            {
                float py = y + 0.5f;
                xs.Clear();
                for (int i = 0, j = pts.Count - 1; i < pts.Count; j = i++)
                {
                    Vector2 a = pts[j], b = pts[i];
                    if ((a.y > py) != (b.y > py)) xs.Add(a.x + (py - a.y) / (b.y - a.y) * (b.x - a.x));
                }
                xs.Sort();
                for (int k = 0; k + 1 < xs.Count; k += 2)
                    for (int x = Mathf.CeilToInt(xs[k] - 0.5f); x <= Mathf.FloorToInt(xs[k + 1] - 0.5f); x++) Put(c, x, y, col);
            }
        }

        static void Rect(PixelCanvas c, int x0, int y0, int x1, int y1, Color32 col)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++) Put(c, x, y, col);
        }

        static void Disc(PixelCanvas c, float cx, float cy, float r, Color32 col, float alpha = 1f)
        {
            for (int y = Mathf.FloorToInt(cy - r); y <= Mathf.CeilToInt(cy + r); y++)
                for (int x = Mathf.FloorToInt(cx - r); x <= Mathf.CeilToInt(cx + r); x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    if (dx * dx + dy * dy > r * r) continue;
                    if (alpha >= 1f) Put(c, x, y, col);
                    else Blend(c, x, y, PixelCanvas.WithAlpha(col, alpha));
                }
        }

        static void Line(PixelCanvas c, Vector2 a, Vector2 b, float width, Color32 col)
        {
            float len = Vector2Distance(a, b);
            int steps = Mathf.Max(1, Mathf.CeilToInt(len * 2f));
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Disc(c, Mathf.Lerp(a.x, b.x, t), Mathf.Lerp(a.y, b.y, t), width * 0.5f, col);
            }
        }

        static float Vector2Distance(Vector2 a, Vector2 b) => Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));

        /// <summary>Borde iluminado de 1 píxel donde la silueta (color exacto) toca el vacío en la dirección de la luz.</summary>
        static void Rim(PixelCanvas c, Color32 body, Color32 light, int dx, int dy)
        {
            var copy = (Color32[])c.Pixels.Clone();
            for (int y = 0; y < c.Height; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    var p = copy[y * W + x];
                    if (!Same(p, body)) continue;
                    int nx = Wrap(x + dx), ny = y + dy;
                    if (ny < 0 || ny >= c.Height) continue;
                    var n = copy[ny * W + nx];
                    if (n.a == 0 || (!Same(n, body) && n.a < 255)) c.Pixels[y * W + x] = light;
                }
            }
        }

        static bool Same(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;

        /// <summary>Bandas de niebla periódicas, semitransparentes y cuantizadas.</summary>
        static PixelCanvas Fog(Color32 col, float maxAlpha, int y0, int y1, int seed)
        {
            var c = new PixelCanvas(W, H);
            for (int y = y0; y < y1; y++)
            {
                float v = Mathf.Sin(Mathf.InverseLerp(y0, y1, y) * Mathf.PI);
                for (int x = 0; x < W; x++)
                {
                    float n = PixelCanvas.ValueNoise(x / 48f, y / 10f, W / 48, seed) * 0.7f + PixelCanvas.ValueNoise(x / 16f, y / 5f, W / 16, seed + 3) * 0.3f;
                    float a = Mathf.Clamp01((n - 0.42f) * 2.2f) * v * maxAlpha;
                    a = Mathf.Floor(a * 6f + Bayer[y & 3, x & 3] * 0.8f) / 6f;
                    if (a <= 0.01f) continue;
                    c.Pixels[y * W + x] = PixelCanvas.WithAlpha(col, a);
                }
            }
            return c;
        }

        static void Stars(PixelCanvas c, int yMin, int seed, Color32 dim, Color32 bright)
        {
            for (int y = yMin; y < c.Height; y++)
                for (int x = 0; x < W; x++)
                {
                    float h = PixelCanvas.Hash(x, y, seed);
                    if (h > 0.9985f)
                    {
                        Put(c, x, y, bright);
                        if (h > 0.9996f) { Put(c, x + 1, y, dim); Put(c, x - 1, y, dim); Put(c, x, y + 1, dim); Put(c, x, y - 1, dim); }
                    }
                    else if (h > 0.996f) Put(c, x, y, dim);
                }
        }

        static BackgroundLayer Layer(string name, PixelCanvas canvas, float px, float py, int order, float bottom)
        {
            var layer = new BackgroundLayer { Name = name, Canvas = canvas, Parallax = new Vector2(px, py), Order = order, BottomOffset = bottom };
            return layer;
        }

        static Color32 RowColor(PixelCanvas c, int y)
        {
            int r = 0, g = 0, b = 0, n = 0;
            for (int x = 0; x < W; x += 3)
            {
                var p = c.Pixels[y * W + x];
                if (p.a < 250) continue;
                r += p.r; g += p.g; b += p.b; n++;
            }
            return n == 0 ? PixelCanvas.Clear : new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), 255);
        }

        // ------------------------------------------------------------------
        // Costa de Innsmouth
        // ------------------------------------------------------------------

        static List<BackgroundLayer> Coast()
        {
            var layers = new List<BackgroundLayer>();

            // Cielo nocturno verdoso con la luna enferma.
            var sky = new PixelCanvas(W, H);
            Gradient(sky, 0, H, C("2d4537"), C("16271f"), C("0a1513"), C("04090a"));
            Stars(sky, 150, 7, C("4f6a60"), C("a9c4b4"));
            // Nubes alargadas, oscuras, con el borde bajo iluminado por la luna.
            for (int y = 140; y < 330; y++)
                for (int x = 0; x < W; x++)
                {
                    float n = PixelCanvas.ValueNoise(x / 64f, y / 7f, W / 64, 21) * 0.65f + PixelCanvas.ValueNoise(x / 16f, y / 3f, W / 16, 22) * 0.35f;
                    float band = Mathf.Sin((y - 140) / 190f * Mathf.PI * 3f) * 0.5f + 0.5f;
                    if (n * band > 0.42f) Put(sky, x, y, n * band > 0.5f ? C("0b1614") : C("12201c"));
                }
            Moon(sky, 210f, 262f, 34f);
            var skyLayer = Layer("cielo", sky, 0.985f, 0.985f, -100, -1f);
            skyLayer.FillAbove = RowColor(sky, H - 1);
            layers.Add(skyLayer);

            // El Durmiente emerge del mar en el horizonte.
            var far = new PixelCanvas(W, H);
            var sea = C("0a1715");
            Silhouette(far, x => 64f + 2f * N1(x, 16, 3), sea);
            for (int y = 8; y < 62; y++)
                for (int x = 0; x < W; x++)
                    if (PixelCanvas.ValueNoise(x / 12f, y * 1.4f, W / 12, 5) > 0.74f && (y % 3) != 0) Put(far, x, y, C("1b302a"));
            // Reflejo de la luna (bajo ella en la vista inicial).
            for (int y = 10; y < 62; y += 2)
            {
                int w = Mathf.RoundToInt(10f + (62 - y) * 0.5f);
                for (int x = -w; x <= w; x++) if (PixelCanvas.Hash(x, y, 9) > 0.45f) Put(far, 210 + x + (y % 4), y, C("4f6e58"));
            }
            Colossus(far, 560f, 64f);
            // Islotes bajos.
            Silhouette(far, x => 64f + Mathf.Max(0f, (Fbm1(x, 96, 41) - 0.62f) * 90f), C("0e1d1a"));
            var farLayer = Layer("durmiente", far, 0.95f, 0.96f, -95, 1.2f);
            farLayer.FillBelow = sea;
            layers.Add(farLayer);

            // Innsmouth en ruinas: tejados a dos aguas, chimeneas, el campanario partido y los muelles.
            var town = new PixelCanvas(W, H);
            var townCol = C("0f1918");
            Town(town, townCol, C("27382f"), C("c98a3a"), C("5f4a2a"));
            var townLayer = Layer("innsmouth", town, 0.82f, 0.9f, -85, 2.4f);
            townLayer.FillBelow = townCol;
            layers.Add(townLayer);

            // Orilla: rocas dentadas y pilotes podridos.
            var shore = new PixelCanvas(W, H);
            var shoreCol = C("070c0c");
            Silhouette(shore, x => 30f + Fbm1(x, 64, 51) * 60f + Mathf.Max(0f, N1(x, 8, 52) - 0.6f) * 26f, shoreCol);
            for (int i = 0; i < 9; i++)
            {
                int x = 30 + i * 83 + (int)(PixelCanvas.Hash(i, 1, 53) * 30f);
                int h = 70 + (int)(PixelCanvas.Hash(i, 2, 53) * 60f);
                Rect(shore, x, 0, x + 4, h, shoreCol);
                if (i % 3 == 0) Line(shore, new Vector2(x + 2, h - 6), new Vector2(x + 40, h - 20), 2f, shoreCol);
            }
            Rim(shore, shoreCol, C("1d2c28"), -1, 1);
            var shoreLayer = Layer("orilla", shore, 0.62f, 0.8f, -70, 2.2f);
            shoreLayer.FillBelow = shoreCol;
            layers.Add(shoreLayer);

            var fog = Layer("niebla", Fog(C("6f9e8c"), 0.32f, 20, 170, 61), 0.5f, 0.75f, -60, 3f);
            fog.Scroll = new Vector2(0.15f, 0f);
            layers.Add(fog);
            return layers;
        }

        static void Moon(PixelCanvas c, float cx, float cy, float r)
        {
            // Halos cuantizados.
            Disc(c, cx, cy, r + 26f, C("2e4a3c"), 0.18f);
            Disc(c, cx, cy, r + 14f, C("4a6a52"), 0.22f);
            Disc(c, cx, cy, r + 6f, C("7a9470"), 0.25f);
            for (int y = Mathf.FloorToInt(cy - r); y <= Mathf.CeilToInt(cy + r); y++)
                for (int x = Mathf.FloorToInt(cx - r); x <= Mathf.CeilToInt(cx + r); x++)
                {
                    float dx = (x + 0.5f - cx) / r, dy = (y + 0.5f - cy) / r;
                    float d2 = dx * dx + dy * dy;
                    if (d2 > 1f) continue;
                    float limb = Mathf.Sqrt(1f - d2);
                    float maria = PixelCanvas.ValueNoise(x / 9f, y / 9f, 0, 71);
                    float l = limb * 0.6f + 0.4f - (maria > 0.55f ? 0.22f : 0f) - (dx > 0.25f ? (dx - 0.25f) * 0.5f : 0f);
                    l += (Bayer[y & 3, x & 3] - 0.5f) * 0.12f;
                    Color32 col = l > 0.85f ? C("eef0c8") : l > 0.68f ? C("cfd6a2") : l > 0.5f ? C("a6b384") : C("7b8a66");
                    Put(c, x, y, col);
                }
            // Jirones de nube delante.
            for (int y = (int)(cy - 12); y < cy - 6; y++)
                for (int x = (int)(cx - r - 20); x < cx + r + 30; x++)
                    if (PixelCanvas.ValueNoise(x / 10f, y / 2f, 0, 73) > 0.5f) Put(c, x, y, C("0d1a17"));
            for (int y = (int)(cy + 10); y < cy + 14; y++)
                for (int x = (int)(cx - r - 10); x < cx + r + 10; x++)
                    if (PixelCanvas.ValueNoise(x / 12f, y / 2f, 0, 74) > 0.55f) Put(c, x, y, C("13231f"));
        }

        static void Colossus(PixelCanvas c, float cx, float sea)
        {
            var body = C("172820");
            var rim = C("31493c");
            // Alas membranosas de murciélago con dedos.
            for (int side = -1; side <= 1; side += 2)
            {
                var shoulder = new Vector2(cx + side * 34f, sea + 150f);
                float[] ang = { 70f, 40f, 12f, -12f };
                float[] len = { 150f, 170f, 150f, 110f };
                var tips = new List<Vector2>();
                for (int k = 0; k < 4; k++)
                {
                    float a = (side > 0 ? ang[k] : 180f - ang[k]) * Mathf.Deg2Rad;
                    tips.Add(new Vector2(shoulder.x + Mathf.Cos(a) * len[k], shoulder.y + Mathf.Sin(a) * len[k]));
                }
                var wing = new List<Vector2> { shoulder };
                for (int k = 0; k < 4; k++)
                {
                    wing.Add(tips[k]);
                    if (k < 3) wing.Add(Vector2Lerp(Vector2Lerp(tips[k], tips[k + 1], 0.5f), shoulder, 0.3f));
                }
                wing.Add(new Vector2(shoulder.x + side * 20f, sea + 60f));
                Polygon(c, wing, body);
                foreach (var t in tips) Line(c, shoulder, t, 3f, body);
            }
            // Cuerpo encorvado.
            for (int y = (int)sea; y < sea + 170f; y++)
                for (int x = (int)(cx - 80f); x < cx + 80f; x++)
                {
                    float dx = x - cx, dy = y - (sea + 90f);
                    if ((dx * dx) / (70f * 70f) + (dy * dy) / (95f * 95f) < 1f) Put(c, x, y, body);
                }
            // Cabeza de pulpo: manto alto inclinado hacia atrás.
            for (int y = (int)(sea + 120f); y < sea + 300f; y++)
                for (int x = (int)(cx - 70f); x < cx + 70f; x++)
                {
                    float dx = x - (cx - 8f) + (y - (sea + 200f)) * 0.22f, dy = y - (sea + 205f);
                    if ((dx * dx) / (40f * 40f) + (dy * dy) / (70f * 70f) < 1f) Put(c, x, y, body);
                }
            Rim(c, body, rim, -1, 1);
            // Rostro: tentáculos que cuelgan y se rizan sobre el pecho.
            var face = C("102019");
            for (int i = 0; i < 8; i++)
            {
                float x0 = cx - 22f + i * 6.5f;
                float wv = 3.6f - Mathf.Abs(i - 3.5f) * 0.3f;
                for (int y = (int)(sea + 150f); y > sea + 40f + i % 3 * 12f; y--)
                {
                    float t = (sea + 150f - y) / 110f;
                    float x = x0 + Mathf.Sin(t * 5f + i * 1.3f) * 6f * t;
                    Rect(c, Mathf.RoundToInt(x - wv), y, Mathf.RoundToInt(x + wv), y, face);
                }
            }
            // Ojos que brillan bajo el manto.
            for (int side = -1; side <= 1; side += 2)
            {
                float ex = cx - 4f + side * 13f, ey = sea + 162f;
                Disc(c, ex, ey, 8f, C("ff6a2a"), 0.16f);
                Rect(c, (int)(ex - 3), (int)ey, (int)(ex + 2), (int)ey + 1, C("ff8a3c"));
                Put(c, (int)ex, (int)ey, C("ffe0a0"));
            }
        }

        static Vector2 Vector2Lerp(Vector2 a, Vector2 b, float t) => new Vector2(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);

        static void Town(PixelCanvas c, Color32 col, Color32 rim, Color32 lit, Color32 unlit)
        {
            int x = 0;
            int i = 0;
            float baseY = 34f;
            while (x < W - 24)
            {
                int w = 34 + (int)(PixelCanvas.Hash(i, 1, 91) * 46f);
                if (x + w > W - 6) w = W - 6 - x;
                int h = 34 + (int)(PixelCanvas.Hash(i, 2, 91) * 46f);
                bool church = i == 5;
                if (church) { w = 58; h = 86; }
                Rect(c, x, 0, x + w, (int)baseY + h, col);
                // Tejado a dos aguas (algunos hundidos).
                float roof = 18f + PixelCanvas.Hash(i, 3, 91) * 16f;
                bool sunk = PixelCanvas.Hash(i, 4, 91) > 0.7f;
                var tri = new List<Vector2>
                {
                    new Vector2(x - 3, baseY + h), new Vector2(x + w + 3, baseY + h),
                    new Vector2(x + w * (sunk ? 0.62f : 0.5f), baseY + h + roof * (sunk ? 0.6f : 1f)),
                };
                if (sunk) tri.Insert(2, new Vector2(x + w * 0.75f, baseY + h + roof * 0.9f));
                Polygon(c, tri, col);
                if (PixelCanvas.Hash(i, 5, 91) > 0.4f) Rect(c, x + w / 4, (int)(baseY + h + roof * 0.4f), x + w / 4 + 5, (int)(baseY + h + roof * 0.4f) + 18, col);
                if (church)
                {
                    // Campanario partido e inclinado.
                    int sx = x + w / 2 - 9;
                    Rect(c, sx, (int)baseY + h, sx + 18, (int)baseY + h + 70, col);
                    Polygon(c, new List<Vector2> { new Vector2(sx - 2, baseY + h + 70), new Vector2(sx + 20, baseY + h + 70), new Vector2(sx + 16, baseY + h + 104), new Vector2(sx + 12, baseY + h + 92), new Vector2(sx + 6, baseY + h + 112) }, col);
                    Rect(c, sx + 6, (int)baseY + h + 40, sx + 12, (int)baseY + h + 54, C("070b0a"));
                }
                // Ventanas: casi todas oscuras, alguna con luz cálida.
                for (int wy = (int)baseY + 14; wy < baseY + h - 10; wy += 16)
                    for (int wx = x + 6; wx < x + w - 6; wx += 12)
                    {
                        float hsh = PixelCanvas.Hash(wx, wy, 93);
                        if (hsh < 0.45f) continue;
                        Rect(c, wx, wy, wx + 3, wy + 5, hsh > 0.93f ? lit : (hsh > 0.86f ? unlit : C("070b0a")));
                    }
                x += w + 4 + (int)(PixelCanvas.Hash(i, 6, 91) * 14f);
                i++;
            }
            // Muelles: tablazón y pilotes.
            Rect(c, 0, 26, W - 1, 30, col);
            for (int px = 6; px < W; px += 22) Rect(c, px, 0, px + 3, 34, col);
            Rim(c, col, rim, -1, 1);
        }

        // ------------------------------------------------------------------
        // Ruinas Ciclópeas
        // ------------------------------------------------------------------

        static List<BackgroundLayer> Ruins()
        {
            var layers = new List<BackgroundLayer>();
            var voidC = new PixelCanvas(W, H);
            Gradient(voidC, 0, H, C("12231d"), C("0a1512"), C("040807"), C("020403"));
            var farBlock = C("142821");
            // Bloques gigantes inclinados y escaleras que no llevan a ninguna parte.
            for (int i = 0; i < 7; i++)
            {
                float cx = i * (W / 7f) + PixelCanvas.Hash(i, 1, 101) * 40f;
                float cy = 120f + PixelCanvas.Hash(i, 2, 101) * 160f;
                float w = 60f + PixelCanvas.Hash(i, 3, 101) * 70f, h = 40f + PixelCanvas.Hash(i, 4, 101) * 80f;
                float a = (PixelCanvas.Hash(i, 5, 101) - 0.5f) * 50f;
                Polygon(voidC, Box(cx, cy, w, h, a), farBlock);
            }
            for (int i = 0; i < 3; i++)
            {
                float ox = 120f + i * 256f, oy = 90f + i * 50f;
                float a = (i % 2 == 0 ? 1f : -1f) * 28f;
                for (int s = 0; s < 9; s++) Polygon(voidC, Box(ox + s * 14f, oy + s * 9f, 16f, 10f, a), farBlock);
            }
            Rim(voidC, farBlock, C("1c3a30"), -1, 1);
            Glyphs(voidC, 26, 50, C("1f5a4a"), C("4fd6a4"), 111);
            var l0 = Layer("vacio", voidC, 0.96f, 0.96f, -100, -1f);
            l0.FillAbove = RowColor(voidC, H - 1);
            l0.FillBelow = RowColor(voidC, 0);
            layers.Add(l0);

            var mono = new PixelCanvas(W, H);
            var monoCol = C("0c1714");
            for (int i = 0; i < 6; i++)
            {
                float cx = 40f + i * 128f + PixelCanvas.Hash(i, 1, 121) * 30f;
                float h = 160f + PixelCanvas.Hash(i, 2, 121) * 140f;
                float lean = (PixelCanvas.Hash(i, 3, 121) - 0.5f) * 24f;
                float w = 38f + PixelCanvas.Hash(i, 4, 121) * 26f;
                Polygon(mono, new List<Vector2> { new Vector2(cx - w * 0.5f, 0f), new Vector2(cx + w * 0.5f, 0f), new Vector2(cx + w * 0.42f + lean, h), new Vector2(cx - w * 0.5f + lean, h - 14f) }, monoCol);
                if (i % 2 == 1)
                {
                    // Dintel imposible apoyado en dos monolitos.
                    Polygon(mono, Box(cx - 64f + lean, h - 8f, 150f, 22f, -9f), monoCol);
                }
            }
            Silhouette(mono, x => 20f + Fbm1(x, 64, 131) * 24f, monoCol);
            Rim(mono, monoCol, C("254a3e"), -1, 1);
            Glyphs(mono, 22, 120, C("2a6e5a"), C("6cf7c8"), 141);
            var l1 = Layer("monolitos", mono, 0.85f, 0.9f, -88, 2.4f);
            l1.FillBelow = monoCol;
            layers.Add(l1);

            var near = new PixelCanvas(W, H);
            var nearCol = C("070d0b");
            Silhouette(near, x =>
            {
                float stairs = Mathf.Floor(Mathf.Repeat(x, 192f) / 16f) * 9f;
                return 24f + Mathf.Min(stairs, 70f) * (N1(x, 192, 151) > 0.5f ? 1f : 0.4f) + Fbm1(x, 32, 152) * 16f;
            }, nearCol);
            for (int i = 0; i < 4; i++)
            {
                int x = 60 + i * 192;
                Rect(near, x, 0, x + 22, 120 + (int)(PixelCanvas.Hash(i, 1, 161) * 70f), nearCol);
                Polygon(near, new List<Vector2> { new Vector2(x - 6, 120 + (int)(PixelCanvas.Hash(i, 1, 161) * 70f)), new Vector2(x + 28, 120 + (int)(PixelCanvas.Hash(i, 1, 161) * 70f)), new Vector2(x + 20, 140 + (int)(PixelCanvas.Hash(i, 1, 161) * 70f)), new Vector2(x + 4, 132 + (int)(PixelCanvas.Hash(i, 1, 161) * 70f)) }, nearCol);
            }
            Rim(near, nearCol, C("18302a"), -1, 1);
            var l2 = Layer("escalinata", near, 0.68f, 0.82f, -72, 2.2f);
            l2.FillBelow = nearCol;
            layers.Add(l2);

            var fog = Layer("niebla", Fog(C("3f8a6a"), 0.26f, 10, 200, 171), 0.55f, 0.78f, -60, 3f);
            fog.Scroll = new Vector2(0.08f, 0f);
            layers.Add(fog);
            return layers;
        }

        static List<Vector2> Box(float cx, float cy, float w, float h, float angle)
        {
            float a = angle * Mathf.Deg2Rad, cs = Mathf.Cos(a), sn = Mathf.Sin(a);
            var pts = new List<Vector2>();
            foreach (var (x, y) in new[] { (-w * 0.5f, -h * 0.5f), (w * 0.5f, -h * 0.5f), (w * 0.5f, h * 0.5f), (-w * 0.5f, h * 0.5f) })
                pts.Add(new Vector2(cx + x * cs - y * sn, cy + x * sn + y * cs));
            return pts;
        }

        /// <summary>Runas tenues: pequeños trazos de 3x5 en grupos.</summary>
        static void Glyphs(PixelCanvas c, int count, int yMin, Color32 dim, Color32 bright, int seed)
        {
            for (int i = 0; i < count; i++)
            {
                int x = (int)(PixelCanvas.Hash(i, 1, seed) * W);
                int y = yMin + (int)(PixelCanvas.Hash(i, 2, seed) * (c.Height - yMin - 10));
                if (Get(c, x, y).a == 0) continue;
                var col = PixelCanvas.Hash(i, 3, seed) > 0.85f ? bright : dim;
                int pattern = (int)(PixelCanvas.Hash(i, 4, seed) * 1000f);
                for (int k = 0; k < 15; k++)
                    if (((pattern >> (k % 10)) & 1) == 1 && Get(c, x + k % 3, y + k / 3).a > 0) Put(c, x + k % 3, y + k / 3, col);
            }
        }

        // ------------------------------------------------------------------
        // Santuario de las Mareas
        // ------------------------------------------------------------------

        static List<BackgroundLayer> Sanctuary()
        {
            var layers = new List<BackgroundLayer>();
            var nave = new PixelCanvas(W, H);
            Gradient(nave, 0, H, C("0d0b09"), C("1a140e"), C("120e0a"), C("060505"));
            // Bóvedas de crucería al fondo.
            var rib = C("221a12");
            for (int i = 0; i < 4; i++)
            {
                float cx = 96f + i * 192f;
                for (int s = -1; s <= 1; s += 2)
                    for (float t = 0f; t <= 1f; t += 0.004f)
                    {
                        float x = cx + s * 96f * (1f - t);
                        float y = 220f + Mathf.Sin(t * Mathf.PI * 0.5f) * 120f;
                        Put(nave, Mathf.RoundToInt(x), Mathf.RoundToInt(y), rib);
                        Put(nave, Mathf.RoundToInt(x), Mathf.RoundToInt(y) + 1, rib);
                    }
            }
            // Rosetón roto con vidrieras verdosas.
            RoseWindow(nave, 384f, 236f, 74f);
            RoseWindow(nave, 0f, 250f, 40f);
            var l0 = Layer("nave", nave, 0.96f, 0.96f, -100, -1f);
            l0.FillAbove = RowColor(nave, H - 1);
            l0.FillBelow = RowColor(nave, 0);
            layers.Add(l0);

            // Columnas colosales con arcos ojivales.
            var cols = new PixelCanvas(W, H);
            var colC = C("1d1611");
            for (int i = 0; i < 4; i++)
            {
                int cx = 96 + i * 192;
                Rect(cols, cx - 23, 0, cx + 23, 250, colC);
                Rect(cols, cx - 30, 0, cx + 30, 14, colC);
                Rect(cols, cx - 30, 250, cx + 30, 262, colC);
                // Arco apuntado hacia la siguiente columna.
                for (int x = cx + 23; x < cx + 192 - 23; x++)
                {
                    float t = (x - (cx + 23)) / (192f - 46f);
                    float arch = 262f + 70f * Mathf.Sin(t * Mathf.PI) - (t > 0.5f ? 1f - t : t) * 20f;
                    for (int y = (int)arch; y < H; y++) Put(cols, x, y, colC);
                }
                // Hornacina con estatua entre columnas.
                int nx = cx + 96;
                Polygon(cols, new List<Vector2> { new Vector2(nx - 14, 70), new Vector2(nx + 14, 70), new Vector2(nx + 14, 128), new Vector2(nx, 146), new Vector2(nx - 14, 128) }, C("0a0806"));
                Polygon(cols, new List<Vector2> { new Vector2(nx - 7, 72), new Vector2(nx + 7, 72), new Vector2(nx + 5, 118), new Vector2(nx, 128), new Vector2(nx - 5, 118) }, colC);
                Rect(cols, nx - 18, 66, nx + 18, 70, colC);
            }
            Silhouette(cols, x => 16f, colC);
            Rim(cols, colC, C("54402a"), -1, 0);
            Rim(cols, colC, C("2a2016"), 0, 1);
            var l1 = Layer("columnas", cols, 0.84f, 0.9f, -86, 2.4f);
            l1.FillBelow = colC;
            l1.FillAbove = colC;
            layers.Add(l1);

            // Cortinajes carmesí que cuelgan.
            var drapes = new PixelCanvas(W, H);
            var cloth = new[] { C("12040a"), C("1f070e"), C("2e0c14"), C("3d121a") };
            for (int i = 0; i < 6; i++)
            {
                int x0 = 20 + i * 128;
                int width = 70 + (int)(PixelCanvas.Hash(i, 1, 181) * 30f);
                int len = 90 + (int)(PixelCanvas.Hash(i, 2, 181) * 90f);
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)width;
                    float sag = Mathf.Sin(u * Mathf.PI) * 18f;
                    int bottom = H - len - (int)(Mathf.Sin(u * Mathf.PI * 3f) * 8f);
                    int top = H - 1;
                    if (u > 0.08f && u < 0.92f) top = H - 1 - (int)sag;
                    float fold = Mathf.Sin(u * Mathf.PI * 7f);
                    for (int y = bottom; y <= H - 1; y++)
                    {
                        if (y > top && u > 0.08f && u < 0.92f) continue;
                        int k = Mathf.Clamp(Mathf.FloorToInt((fold * 0.5f + 0.5f) * 3.99f + (Bayer[y & 3, (x0 + x) & 3] - 0.5f) * 0.6f), 0, 3);
                        Put(drapes, x0 + x, y, cloth[k]);
                    }
                }
                // Borla dorada.
                Rect(drapes, x0 + width / 2 - 1, H - len - 14, x0 + width / 2 + 1, H - len - 4, C("6e5020"));
            }
            var l2 = Layer("cortinajes", drapes, 0.68f, 0.84f, -70, 1.5f);
            layers.Add(l2);

            var dust = Layer("polvo", Fog(C("c9a066"), 0.14f, 30, 300, 191), 0.55f, 0.8f, -60, 2f);
            dust.Scroll = new Vector2(0.05f, 0.02f);
            layers.Add(dust);
            return layers;
        }

        static void RoseWindow(PixelCanvas c, float cx, float cy, float r)
        {
            var frame = C("0b0907");
            var glass = new[] { C("1f5a4a"), C("2e7a5e"), C("3f9a6e"), C("6a8a3a"), C("8a6a2a") };
            for (int y = Mathf.FloorToInt(cy - r - 4); y <= cy + r + 4; y++)
                for (int x = Mathf.FloorToInt(cx - r - 4); x <= cx + r + 4; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d > r + 4f) continue;
                    float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 180f;
                    bool tracery = d > r || Mathf.Abs(d - r * 0.5f) < 1.5f || d < r * 0.15f
                                   || Mathf.Abs(Mathf.Repeat(ang, 30f) - 15f) > 13.2f;
                    if (tracery) { Put(c, x, y, frame); continue; }
                    // Cristales rotos: algunos huecos oscuros.
                    int cell = Mathf.FloorToInt(ang / 30f) * 3 + (d > r * 0.5f ? 1 : 0);
                    if (PixelCanvas.Hash(cell, (int)cx, 201) > 0.72f) { Put(c, x, y, C("050404")); continue; }
                    int gi = (int)(PixelCanvas.Hash(cell, (int)(d / 6f), 203) * glass.Length);
                    Put(c, x, y, glass[Mathf.Min(gi, glass.Length - 1)]);
                }
            // Haces de luz que caen desde el rosetón.
            for (int y = (int)(cy - r); y > 0; y--)
            {
                float t = (cy - r - y) / (cy - r);
                float half = r * 0.5f + t * r * 0.5f;
                for (int x = (int)(cx - half); x <= cx + half; x++)
                {
                    float k = 1f - Mathf.Abs(x - cx) / half;
                    float a = k * (1f - t) * 0.16f;
                    if (Mathf.Repeat(x - cx + y * 0.3f, 18f) < 9f) a *= 0.5f;
                    a = Mathf.Floor(a * 10f + Bayer[y & 3, Wrap(x) & 3] * 0.7f) / 10f;
                    if (a > 0f) Blend(c, x, y, PixelCanvas.WithAlpha(C("9ad6a8"), a));
                }
            }
        }

        // ------------------------------------------------------------------
        // Arrecife del Diablo
        // ------------------------------------------------------------------

        static List<BackgroundLayer> Reef()
        {
            var layers = new List<BackgroundLayer>();
            var sky = new PixelCanvas(W, H);
            Gradient(sky, 0, H, C("2c2742"), C("1a1630"), C("0c0a1a"), C("05040b"));
            for (int y = 120; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float n = PixelCanvas.ValueNoise(x / 96f, y / 24f, W / 96, 211) * 0.75f + PixelCanvas.ValueNoise(x / 24f, y / 8f, W / 24, 212) * 0.25f;
                    float k = n + (Bayer[y & 3, x & 3] - 0.5f) * 0.05f;
                    if (k > 0.66f) Put(sky, x, y, C("0b0918"));
                    else if (k > 0.6f) Put(sky, x, y, C("141127"));
                    else if (k > 0.57f) Put(sky, x, y, C("26213f"));
                }
            // Relámpago lejano.
            var bolt = new List<Vector2> { new Vector2(560, 330) };
            for (int i = 0; i < 9; i++) bolt.Add(new Vector2(bolt[i].x + (PixelCanvas.Hash(i, 1, 221) - 0.5f) * 18f, bolt[i].y - 18f));
            for (int i = 0; i + 1 < bolt.Count; i++) Line(sky, bolt[i], bolt[i + 1], 1.2f, C("8f86d8"));
            var l0 = Layer("tormenta", sky, 0.985f, 0.985f, -100, -1f);
            l0.FillAbove = RowColor(sky, H - 1);
            layers.Add(l0);

            // R'lyeh emergiendo: torres de ángulos imposibles.
            var city = new PixelCanvas(W, H);
            var cityCol = C("1c1934");
            Silhouette(city, x => 56f + 3f * N1(x, 16, 231), C("0b0a16"));
            for (int i = 0; i < 11; i++)
            {
                float cx = i * (W / 11f) + PixelCanvas.Hash(i, 1, 241) * 30f;
                float h = 60f + PixelCanvas.Hash(i, 2, 241) * 150f;
                float w = 16f + PixelCanvas.Hash(i, 3, 241) * 34f;
                float lean = (PixelCanvas.Hash(i, 4, 241) - 0.5f) * 40f;
                Polygon(city, new List<Vector2> { new Vector2(cx - w * 0.5f, 56f), new Vector2(cx + w * 0.5f, 56f), new Vector2(cx + w * 0.3f + lean, 56f + h), new Vector2(cx - w * 0.2f + lean * 1.2f, 56f + h + 14f) }, cityCol);
                if (i % 4 == 2) Polygon(city, Box(cx + lean * 0.5f, 56f + h * 0.6f, 70f, 16f, 18f), cityCol);
            }
            Rim(city, cityCol, C("2a2850"), -1, 1);
            Glyphs(city, 90, 60, C("2f8a70"), C("6cf7c8"), 251);
            var l1 = Layer("rlyeh", city, 0.94f, 0.96f, -92, 2.8f);
            l1.FillBelow = C("0b0a16");
            layers.Add(l1);

            // Mar agitado con crestas de espuma.
            var waves = new PixelCanvas(W, H);
            var waveCol = C("0c1220");
            for (int x = 0; x < W; x++)
            {
                float a = x / (float)W * Mathf.PI * 2f;
                float crest = 70f + 10f * Mathf.Sin(a * 6f) + 6f * Mathf.Sin(a * 13f + 1f) + 4f * N1(x, 8, 261);
                for (int y = 0; y < crest; y++)
                {
                    float d = crest - y;
                    Color32 col = d < 1.5f ? C("7a90a8") : d < 4f && N1(x, 4, 262) > 0.5f ? C("3f5068") : waveCol;
                    if (d > 6f && (y % 5 == 0) && N1(x + y * 3, 16, 263) > 0.6f) col = C("1a2438");
                    Put(waves, x, y, col);
                }
            }
            var l2 = Layer("oleaje", waves, 0.78f, 0.88f, -80, 2.4f);
            l2.FillBelow = waveCol;
            layers.Add(l2);

            var rocks = new PixelCanvas(W, H);
            var rockCol = C("06070c");
            Silhouette(rocks, x =>
            {
                float spikes = Mathf.Abs(Mathf.Repeat(x, 48f) - 24f) / 24f;
                return 26f + Fbm1(x, 96, 271) * 70f + (1f - spikes) * N1(x, 48, 272) * 60f;
            }, rockCol);
            Rim(rocks, rockCol, C("1d2236"), -1, 1);
            // Espuma rompiendo contra las rocas.
            for (int i = 0; i < 40; i++)
            {
                int x = (int)(PixelCanvas.Hash(i, 1, 281) * W);
                int y = 26 + (int)(PixelCanvas.Hash(i, 2, 281) * 20f);
                Disc(rocks, x, y, 1.5f + PixelCanvas.Hash(i, 3, 281) * 2f, C("8aa0b8"), 0.6f);
            }
            var l3 = Layer("escollos", rocks, 0.6f, 0.78f, -66, 2.2f);
            l3.FillBelow = rockCol;
            layers.Add(l3);

            var mist = Layer("bruma", Fog(C("7a74a8"), 0.28f, 10, 160, 291), 0.5f, 0.75f, -60, 3f);
            mist.Scroll = new Vector2(0.3f, 0f);
            layers.Add(mist);
            return layers;
        }
    }
}
