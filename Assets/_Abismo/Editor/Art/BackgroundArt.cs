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
        /// <summary>Cuadro pintado (no un patrón): el constructor lo centra en la mitad de su zona para que se vea entero.</summary>
        public bool Centered;
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
            List<BackgroundLayer> layers;
            switch (zone)
            {
                case Zone.Ruins: layers = Ruins(); break;
                case Zone.Sanctuary: layers = Sanctuary(); break;
                case Zone.Reef: layers = Reef(); break;
                default: layers = Coast(); break;
            }
            // Si está el arte de la hoja conceptual, su fondo pintado sustituye al cielo y a las capas más lejanas;
            // las capas cercanas (casas, árboles, rocas, niebla) siguen delante para dar profundidad.
            var pintura = HojaArt.Fondo(zone);
            if (pintura != null)
            {
                string[] sustituidas;
                float subida;
                switch (zone)
                {
                    case Zone.Ruins: sustituidas = new[] { "abismo", "monolitos" }; subida = 5f; break;
                    case Zone.Sanctuary: sustituidas = new[] { "abside", "coloso" }; subida = 6f; break;
                    case Zone.Reef: sustituidas = new[] { "cielo" }; subida = 6f; break;
                    default: sustituidas = new[] { "cielo", "pueblo_lejano" }; subida = 0f; break;
                }
                layers.RemoveAll(l => System.Array.IndexOf(sustituidas, l.Name) >= 0);
                const float p = 0.97f;
                layers.Insert(0, new BackgroundLayer
                {
                    Name = "pintura", Canvas = pintura, Parallax = new Vector2(p, p), Order = -101, BottomOffset = subida * (1f - p),
                    FillAbove = RowAverage(pintura, pintura.Height - 1), FillBelow = RowAverage(pintura, 0), Centered = true,
                });
            }
            return layers;
        }

        /// <summary>Color medio (opaco) de una fila del lienzo.</summary>
        static Color32 RowAverage(PixelCanvas c, int y)
        {
            long r = 0, g = 0, b = 0;
            for (int x = 0; x < c.Width; x++)
            {
                var p = c.Pixels[y * c.Width + x];
                r += p.r; g += p.g; b += p.b;
            }
            return new Color32((byte)(r / c.Width), (byte)(g / c.Width), (byte)(b / c.Width), 255);
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

        static BackgroundLayer Layer(string name, PixelCanvas canvas, float px, float py, int order, float bottom)
        {
            var layer = new BackgroundLayer { Name = name, Canvas = canvas, Parallax = new Vector2(px, py), Order = order, BottomOffset = bottom };
            return layer;
        }

        // ------------------------------------------------------------------
        // Siluetas sombreadas: se pintan en "tono" (0 = sombra … 1 = luz) y al final se resuelven con la
        // paleta de la capa y su niebla. Así cada capa usa pocos colores (bandas nítidas, sin tramado) y la
        // perspectiva atmosférica se controla con la paleta y la niebla de la base.
        // ------------------------------------------------------------------

        /// <summary>Dirección de la luz principal de la capa (se normaliza) y luz ambiente mínima.</summary>
        struct Luz
        {
            public float X, Y, Z, Amb, Max;

            public Luz(float x, float y, float z, float amb, float max = 1f)
            {
                float m = Mathf.Sqrt(x * x + y * y + z * z);
                X = x / m; Y = y / m; Z = z / m; Amb = amb; Max = max;
            }

            public float Tono(float nx, float ny, float nz) => Amb + (Max - Amb) * Mathf.Max(0f, nx * X + ny * Y + nz * Z);
        }

        sealed class Silueta
        {
            public readonly float[] Tono = new float[W * H];
            public readonly Color32[] Fijo = new Color32[W * H];

            public Silueta()
            {
                for (int i = 0; i < Tono.Length; i++) Tono[i] = -1f;
            }

            public bool Lleno(int x, int y) => y >= 0 && y < H && Tono[y * W + Wrap(x)] >= 0f;
            public float Get(int x, int y) => y < 0 || y >= H ? -1f : Tono[y * W + Wrap(x)];

            public void Set(int x, int y, float t)
            {
                if (y < 0 || y >= H) return;
                int i = y * W + Wrap(x);
                Tono[i] = Mathf.Clamp01(t);
                Fijo[i] = default;
            }

            /// <summary>Color fijo que no sigue la paleta (ventanas encendidas, ojos, runas).</summary>
            public void Fix(int x, int y, Color32 c)
            {
                if (y < 0 || y >= H) return;
                int i = y * W + Wrap(x);
                Tono[i] = 0.5f;
                Fijo[i] = c;
            }

            public void Borrar(int x, int y)
            {
                if (y < 0 || y >= H) return;
                int i = y * W + Wrap(x);
                Tono[i] = -1f;
                Fijo[i] = default;
            }
        }

        /// <summary>Niebla de la base de una capa: opaca por debajo de Suelo y se desvanece en Fundido píxeles.</summary>
        sealed class Niebla
        {
            public Color32 Color;
            public int Suelo, Fundido = 60;
            /// <summary>Velo mínimo sobre toda la silueta (0 = nada).</summary>
            public float Base;
            /// <summary>Irregularidad horizontal de la niebla (jirones).</summary>
            public float Ruido = 0.35f;
            public int Semilla = 1;
            /// <summary>Si es false, la niebla solo tiñe la silueta (para componer sobre otro lienzo).</summary>
            public bool VelarVacio = true;

            public float En(int x, int y)
            {
                float g = Fundido <= 0 ? (y <= Suelo ? 1f : 0f) : Mathf.Clamp01(1f - (y - Suelo) / (float)Fundido);
                g = g * g * (3f - 2f * g);
                if (g > 0f && g < 1f)
                    g = Mathf.Clamp01(g + (N2(x, y, 64, Semilla, 12f) * 0.7f + N2(x, y, 16, Semilla + 1, 5f) * 0.3f - 0.5f) * Ruido * 4f * g * (1f - g) * 2f);
                return Mathf.Max(g, Base);
            }
        }

        const int PasosNiebla = 6;

        /// <summary>Convierte la silueta en píxeles: tono → paleta (de oscuro a claro), y niebla tramada en la base.</summary>
        static PixelCanvas Resolver(Silueta s, Color32[] rampa, Niebla niebla)
        {
            var c = new PixelCanvas(W, H);
            int n = rampa.Length;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    float f = niebla == null ? 0f : niebla.En(x, y);
                    float fq = Mathf.Clamp(Mathf.Floor(f * PasosNiebla + Bayer[y & 3, x & 3] * 0.999f), 0f, PasosNiebla) / PasosNiebla;
                    float t = s.Tono[i];
                    if (t < 0f)
                    {
                        if (niebla != null && niebla.VelarVacio && fq > 0f) c.Pixels[i] = PixelCanvas.WithAlpha(niebla.Color, fq);
                        continue;
                    }
                    Color32 col = s.Fijo[i].a > 0 ? s.Fijo[i] : rampa[Mathf.Clamp(Mathf.FloorToInt(t * n), 0, n - 1)];
                    if (niebla != null) col = PixelCanvas.Lerp(col, niebla.Color, s.Fijo[i].a > 0 ? fq * 0.5f : fq);
                    col.a = 255;
                    c.Pixels[i] = col;
                }
            return c;
        }

        /// <summary>Paleta de n colores repartida entre las claves (de oscuro a claro).</summary>
        static Color32[] Rampa(int n, params Color32[] claves)
        {
            var r = new Color32[n];
            for (int i = 0; i < n; i++)
            {
                float s = i / (float)(n - 1) * (claves.Length - 1);
                int k = Mathf.Min(claves.Length - 2, Mathf.FloorToInt(s));
                r[i] = PixelCanvas.Lerp(claves[k], claves[k + 1], s - k);
            }
            return r;
        }

        /// <summary>Acerca una paleta al color de la niebla (perspectiva atmosférica).</summary>
        static Color32[] Velar(Color32[] rampa, Color32 niebla, float k)
        {
            var r = new Color32[rampa.Length];
            for (int i = 0; i < r.Length; i++) r[i] = PixelCanvas.Lerp(rampa[i], niebla, k);
            return r;
        }

        /// <summary>Ruido 2D periódico en el ancho (scale debe dividir a W); yScale permite estirarlo en horizontal.</summary>
        static float N2(float x, float y, int scale, int seed, float yScale = -1f)
            => PixelCanvas.ValueNoise(x / scale, y / (yScale > 0f ? yScale : scale), W / scale, seed);

        static float Hs(int i, int k, int seed) => PixelCanvas.Hash(i, k, seed);

        /// <summary>Pinta src sobre dst (mismo tamaño), respetando la transparencia.</summary>
        static void Sobre(PixelCanvas dst, PixelCanvas src)
        {
            for (int i = 0; i < dst.Pixels.Length; i++)
            {
                var p = src.Pixels[i];
                if (p.a == 0) continue;
                if (p.a == 255) { dst.Pixels[i] = p; continue; }
                dst.Blend(i % W, i / W, p);
            }
        }

        static void PolyS(Silueta s, IList<Vector2> pts, System.Func<int, int, float> tono)
        {
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var p in pts) { minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y); }
            var xs = new List<float>();
            for (int y = Mathf.Max(0, Mathf.FloorToInt(minY)); y <= Mathf.Min(H - 1, Mathf.CeilToInt(maxY)); y++)
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
                    for (int x = Mathf.CeilToInt(xs[k] - 0.5f); x <= Mathf.FloorToInt(xs[k + 1] - 0.5f); x++) s.Set(x, y, tono(x, y));
            }
        }

        static void PolyS(Silueta s, IList<Vector2> pts, float tono) => PolyS(s, pts, (x, y) => tono);

        static void RectS(Silueta s, int x0, int y0, int x1, int y1, float tono)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++) s.Set(x, y, tono);
        }

        static void BorrarRect(Silueta s, int x0, int y0, int x1, int y1)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++) s.Borrar(x, y);
        }

        static void FixRect(Silueta s, int x0, int y0, int x1, int y1, Color32 c)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++) s.Fix(x, y, c);
        }

        static Vector2 V(float x, float y) => new Vector2(x, y);
        static float Dist(Vector2 a, Vector2 b) => Mathf.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));
        static Vector2 Dir(float grados) => new Vector2(Mathf.Cos(grados * Mathf.Deg2Rad), Mathf.Sin(grados * Mathf.Deg2Rad));

        /// <summary>Trazo grueso a lo largo de una polilínea con sombreado cilíndrico (troncos, ramas, brazos, cadenas).</summary>
        static void Trazo(Silueta s, IList<Vector2> p, IList<float> r, Luz luz, float rugosidad = 0f, int semilla = 0)
        {
            for (int i = 0; i + 1 < p.Count; i++)
            {
                Vector2 a = p[i], b = p[i + 1];
                float len = Dist(a, b);
                if (len < 0.001f) continue;
                var n = new Vector2((b.y - a.y) / len, -(b.x - a.x) / len);
                int steps = Mathf.Max(1, Mathf.CeilToInt(len * 1.5f));
                for (int k = 0; k <= steps; k++)
                {
                    float t = k / (float)steps;
                    Sello(s, a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, Mathf.Lerp(r[i], r[i + 1], t), n, luz, rugosidad, semilla);
                }
            }
        }

        static void Sello(Silueta s, float cx, float cy, float rad, Vector2 n, Luz luz, float rug, int semilla)
        {
            float rr = Mathf.Max(0.72f, rad);
            for (int y = Mathf.FloorToInt(cy - rr); y <= Mathf.CeilToInt(cy + rr); y++)
                for (int x = Mathf.FloorToInt(cx - rr); x <= Mathf.CeilToInt(cx + rr); x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    if (dx * dx + dy * dy > rr * rr) continue;
                    float u = Mathf.Clamp((dx * n.x + dy * n.y) / rr, -1f, 1f);
                    float t = luz.Tono(n.x * u, n.y * u, Mathf.Sqrt(1f - u * u));
                    if (rug > 0f && rad > 2f) t += (N2(x, y, 3, semilla, 18f) - 0.5f) * rug;
                    s.Set(x, y, t);
                }
        }

        /// <summary>Trazo de grosor constante.</summary>
        static void Linea(Silueta s, Vector2 a, Vector2 b, float r, Luz luz)
            => Trazo(s, new[] { a, b }, new[] { r, r }, luz);

        /// <summary>Elipse con sombreado esférico.</summary>
        static void Bulto(Silueta s, float cx, float cy, float rx, float ry, Luz luz, float rug = 0f, int semilla = 0)
        {
            for (int y = Mathf.FloorToInt(cy - ry); y <= Mathf.CeilToInt(cy + ry); y++)
                for (int x = Mathf.FloorToInt(cx - rx); x <= Mathf.CeilToInt(cx + rx); x++)
                {
                    float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                    float d2 = dx * dx + dy * dy;
                    if (d2 > 1f) continue;
                    float t = luz.Tono(dx, dy, Mathf.Sqrt(1f - d2));
                    if (rug > 0f) t += (N2(x, y, 4, semilla, 4f) - 0.5f) * rug;
                    s.Set(x, y, t);
                }
        }

        /// <summary>Columna vertical con sombreado cilíndrico y estrías opcionales.</summary>
        static void Fuste(Silueta s, float x0, float x1, int y0, int y1, Luz luz, int estrias = 0)
        {
            float cx = (x0 + x1) * 0.5f, r = (x1 - x0) * 0.5f;
            for (int y = y0; y <= y1; y++)
                for (int x = Mathf.FloorToInt(x0); x <= Mathf.CeilToInt(x1); x++)
                {
                    float u = (x + 0.5f - cx) / r;
                    if (u < -1f || u > 1f) continue;
                    float t = luz.Tono(u, 0f, Mathf.Sqrt(1f - u * u));
                    if (estrias > 0 && Mathf.Abs(u) < 0.85f && Mathf.Repeat((u + 1f) * estrias, 2f) < 0.45f) t -= 0.18f;
                    s.Set(x, y, t);
                }
        }

        /// <summary>
        /// Prisma: cara frontal (polígono convexo en sentido antihorario) extruida según "fondo". Se pintan las caras
        /// laterales visibles con su propio tono y la frontal encima. Sirve para bloques ciclópeos y sillares.
        /// </summary>
        static void Prisma(Silueta s, IList<Vector2> frente, Vector2 fondo, Luz luz, float tonoFrente)
        {
            for (int i = 0; i < frente.Count; i++)
            {
                Vector2 a = frente[i], b = frente[(i + 1) % frente.Count];
                var d = b - a;
                var n = new Vector2(d.y, -d.x).normalized;
                if (n.x * fondo.x + n.y * fondo.y <= 0f) continue;
                float t = luz.Tono(n.x, n.y, 0.35f);
                PolyS(s, new[] { a, b, b + fondo, a + fondo }, t);
            }
            PolyS(s, frente, tonoFrente);
        }

        /// <summary>Borde de 1 píxel donde la silueta toca el vacío en la dirección (dx, dy): luz de contorno.</summary>
        static void Borde(Silueta s, int dx, int dy, float tono, float minimo = 0f, int grosor = 1)
        {
            var copia = (float[])s.Tono.Clone();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    if (copia[i] < minimo || s.Fijo[i].a > 0) continue;
                    int ny = y + dy;
                    if (ny < 0 || ny >= H) continue;
                    if (copia[ny * W + Wrap(x + dx)] >= 0f) continue;
                    // Solo en partes con cuerpo: las ramitas finas se quedan oscuras.
                    bool cuerpo = true;
                    for (int k = 1; k < grosor && cuerpo; k++)
                    {
                        int by = y - dy * k;
                        cuerpo = by >= 0 && by < H && copia[by * W + Wrap(x - dx * k)] >= 0f;
                    }
                    if (cuerpo) s.Tono[i] = tono;
                }
        }

        /// <summary>Variación de tono en manchas (piedra, corteza) para romper las bandas planas.</summary>
        static void Grano(Silueta s, int escala, float cantidad, int semilla, float estiramiento = 1f)
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    if (s.Tono[i] < 0f || s.Fijo[i].a > 0) continue;
                    s.Tono[i] = Mathf.Clamp01(s.Tono[i] + (N2(x, y, escala, semilla, escala / estiramiento) - 0.5f) * cantidad);
                }
        }

        // ------------------------------------------------------------------
        // Cielo, luz y niebla (aquí sí hay tramado ordenado)
        // ------------------------------------------------------------------

        /// <summary>Resplandor radial cuantizado y tramado.</summary>
        static void Halo(PixelCanvas c, float cx, float cy, float r, Color32 col, float maxA, float ry = -1f, int pasos = 6)
        {
            if (ry <= 0f) ry = r;
            for (int y = Mathf.Max(0, Mathf.FloorToInt(cy - ry)); y <= Mathf.Min(H - 1, Mathf.CeilToInt(cy + ry)); y++)
                for (int x = Mathf.FloorToInt(cx - r); x <= Mathf.CeilToInt(cx + r); x++)
                {
                    float dx = (x + 0.5f - cx) / r, dy = (y + 0.5f - cy) / ry;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d >= 1f) continue;
                    float a = (1f - d) * (1f - d);
                    float q = Mathf.Floor(a * pasos + Bayer[y & 3, Wrap(x) & 3] * 0.999f) / pasos * maxA;
                    if (q > 0f) Blend(c, x, y, PixelCanvas.WithAlpha(col, q));
                }
        }

        /// <summary>
        /// Haz de luz que nace en (x0, y0) y cae con la inclinación dada (grados desde la vertical, + hacia la
        /// derecha). Semitransparente, con vetas a lo largo y tramado ordenado en los bordes.
        /// </summary>
        static void Haz(PixelCanvas c, float x0, float y0, float grados, float ancho, float largo, Color32 col, float maxA, int semilla, float abre = 0.5f)
        {
            float a = grados * Mathf.Deg2Rad;
            var d = new Vector2(Mathf.Sin(a), -Mathf.Cos(a));
            var n = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            float reach = largo + ancho * (1f + abre);
            for (int y = Mathf.Max(0, Mathf.FloorToInt(y0 - reach)); y <= Mathf.Min(H - 1, Mathf.CeilToInt(y0 + ancho)); y++)
                for (int x = Mathf.FloorToInt(x0 - reach); x <= Mathf.CeilToInt(x0 + reach); x++)
                {
                    float vx = x + 0.5f - x0, vy = y + 0.5f - y0;
                    float along = vx * d.x + vy * d.y;
                    if (along < 0f || along > largo) continue;
                    float t = along / largo;
                    float half = ancho * 0.5f * (1f + t * abre);
                    float across = vx * n.x + vy * n.y;
                    if (Mathf.Abs(across) > half) continue;
                    float edge = 1f - Mathf.Pow(Mathf.Abs(across) / half, 2f);
                    float fade = Mathf.Pow(1f - t, 1.3f) * Mathf.Clamp01(along / 24f);
                    float veta = 0.55f + 0.45f * PixelCanvas.ValueNoise((across + 1000f) / 5f, 0f, 0, semilla);
                    float alpha = maxA * edge * fade * veta;
                    float q = Mathf.Floor(alpha / maxA * 5f + Bayer[y & 3, Wrap(x) & 3] * 0.999f) / 5f * maxA;
                    if (q > 0f) Blend(c, x, y, PixelCanvas.WithAlpha(col, q));
                }
        }

        /// <summary>Motas de polvo o esporas que flotan en la luz.</summary>
        static void Motas(PixelCanvas c, int cuantas, int y0, int y1, Color32 col, float alpha, int semilla)
        {
            for (int i = 0; i < cuantas; i++)
            {
                int x = (int)(Hs(i, 1, semilla) * W);
                int y = y0 + (int)(Hs(i, 2, semilla) * (y1 - y0));
                float a = alpha * (0.4f + 0.6f * Hs(i, 3, semilla));
                Blend(c, x, y, PixelCanvas.WithAlpha(col, a));
                if (Hs(i, 4, semilla) > 0.8f) { Blend(c, x + 1, y, PixelCanvas.WithAlpha(col, a * 0.5f)); Blend(c, x, y + 1, PixelCanvas.WithAlpha(col, a * 0.5f)); }
            }
        }

        /// <summary>Estratos de nubes con el borde inferior iluminado; más claras cerca de la fuente de luz.</summary>
        static void Nubes(PixelCanvas c, int y0, int y1, Color32 oscura, Color32 clara, Color32 borde, float lx, float ly, float alcance, float umbral, int semilla)
        {
            var mask = new bool[W * H];
            for (int y = y0; y < y1; y++)
            {
                float env = Mathf.Sin(Mathf.InverseLerp(y0, y1, y) * Mathf.PI);
                for (int x = 0; x < W; x++)
                {
                    float n = N2(x, y, 128, semilla, 14f) * 0.55f + N2(x, y, 32, semilla + 1, 5f) * 0.3f + N2(x, y, 8, semilla + 2, 2f) * 0.15f;
                    mask[y * W + x] = n * (0.55f + 0.45f * env) > umbral;
                }
            }
            for (int y = y0; y < y1; y++)
                for (int x = 0; x < W; x++)
                {
                    if (!mask[y * W + x]) continue;
                    float dx = Mathf.Abs(x - lx); dx = Mathf.Min(dx, W - dx);
                    float d = Mathf.Sqrt(dx * dx + (y - ly) * (y - ly)) / alcance;
                    float k = Mathf.Clamp01(1f - d);
                    k = Mathf.Floor(k * 4f + Bayer[y & 3, x & 3] * 0.999f) / 4f;
                    var col = PixelCanvas.Lerp(oscura, clara, k);
                    bool bajo = y > 0 && !mask[(y - 1) * W + x];
                    if (bajo) col = PixelCanvas.Lerp(col, borde, 0.4f + 0.6f * k);
                    Put(c, x, y, col);
                }
        }

        /// <summary>Mar con vetas de oleaje y el reflejo de la luz bajo (lx).</summary>
        static void Mar(PixelCanvas c, int horizonte, Color32 cerca, Color32 lejos, Color32 veta, Color32 brillo, float lx, float anchoReflejo, int semilla)
        {
            for (int y = 0; y < horizonte; y++)
            {
                float t = y / (float)horizonte;
                float depth = horizonte - y;
                for (int x = 0; x < W; x++)
                {
                    float k = Mathf.Floor(t * 6f + Bayer[y & 3, x & 3] * 0.999f) / 6f;
                    var col = PixelCanvas.Lerp(cerca, lejos, k * k);
                    float sx = depth < 20f ? 32 : 16;
                    float w = N2(x + y * 7, y, (int)sx, semilla, 1.2f);
                    if (w > 0.7f) col = PixelCanvas.Lerp(col, veta, 0.5f);
                    else if (w < 0.22f) col = PixelCanvas.Lerp(col, cerca, 0.35f);
                    float dx = Mathf.Abs(x - lx); dx = Mathf.Min(dx, W - dx);
                    float spread = anchoReflejo * (0.35f + 0.65f * (1f - t));
                    if (dx < spread)
                    {
                        float g = N2(x, y, 4, semilla + 5, 1f) + (1f - dx / spread) * 0.45f;
                        if (g > 0.85f) col = brillo;
                        else if (g > 0.7f) col = PixelCanvas.Lerp(col, brillo, 0.5f);
                    }
                    c.Pixels[y * W + x] = col;
                }
            }
        }

        static BackgroundLayer Capa(string nombre, PixelCanvas lienzo, float px, float py, int orden, float subida)
        {
            // "subida": cuántas unidades por encima de la cámara inicial suele estar la cámara en esta zona; la capa se
            // coloca para que, vista desde ahí, quede como se pintó (el constructor coloca todas con la cámara inicial).
            return new BackgroundLayer { Name = nombre, Canvas = lienzo, Parallax = new Vector2(px, py), Order = orden, BottomOffset = subida * (1f - py) };
        }

        /// <summary>
        /// Funde las filas del borde (arriba o abajo) hacia un color uniforme con tramado, para que el relleno
        /// (FillAbove/FillBelow) empalme sin costura cuando la cámara sube o baja. Devuelve ese color.
        /// </summary>
        static Color32 Cerrar(PixelCanvas c, Color32 col, int filas, bool arriba)
        {
            for (int k = 0; k < filas; k++)
            {
                int y = arriba ? H - 1 - k : k;
                float t = 1f - k / (float)filas;
                for (int x = 0; x < W; x++)
                {
                    float q = Mathf.Floor(t * 4f + Bayer[y & 3, x & 3] * 0.999f) / 4f;
                    if (q <= 0f) continue;
                    var p = PixelCanvas.Lerp(c.Pixels[y * W + x], col, q);
                    p.a = 255;
                    c.Pixels[y * W + x] = p;
                }
            }
            return col;
        }

        // ------------------------------------------------------------------
        // Motivos
        // ------------------------------------------------------------------

        /// <summary>Árbol muerto retorcido: raíces que agarran el suelo, tronco con quiebros y copa de ramas desnudas.</summary>
        static void ArbolMuerto(Silueta s, float x0, float y0, float alto, float grosor, int semilla, Luz luz, float rug)
        {
            // Raíces.
            int nr = 4 + (int)(Hs(semilla, 1, 7) * 3f);
            for (int i = 0; i < nr; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float len = grosor * (2.2f + Hs(semilla, 10 + i, 7) * 2.6f);
                float h0 = grosor * (1.1f + Hs(semilla, 20 + i, 7) * 0.9f);
                var pts = new List<Vector2>();
                var rs = new List<float>();
                for (int k = 0; k <= 5; k++)
                {
                    float t = k / 5f;
                    pts.Add(V(x0 + side * (grosor * 0.25f + len * t), y0 + h0 * Mathf.Pow(1f - t, 1.6f) - t * 3f + Mathf.Sin(t * 5f + i) * 1.2f));
                    rs.Add(Mathf.Lerp(grosor * 0.6f, 0.6f, Mathf.Pow(t, 0.8f)));
                }
                Trazo(s, pts, rs, luz, rug, semilla);
            }
            // Tronco con quiebros.
            float horca = alto * (0.42f + Hs(semilla, 2, 7) * 0.18f);
            float lean = (Hs(semilla, 3, 7) - 0.5f) * 0.45f;
            var tp = new List<Vector2>();
            var tr = new List<float>();
            int nseg = 9;
            for (int k = 0; k <= nseg; k++)
            {
                float t = k / (float)nseg;
                float x = x0 + lean * horca * t + Mathf.Sin(t * Mathf.PI * 1.6f + semilla) * grosor * 0.7f + (Hs(semilla, 30 + k, 7) - 0.5f) * grosor * 0.4f;
                tp.Add(V(x, y0 - 2f + horca * t));
                tr.Add(grosor * (1.15f - 0.4f * t) * (1f + 0.7f * Mathf.Pow(1f - t, 5f)));
            }
            Trazo(s, tp, tr, luz, rug, semilla);
            var top = tp[nseg];
            float rTop = tr[nseg];
            // Nudo hueco en el tronco.
            if (Hs(semilla, 4, 7) > 0.35f)
            {
                var k = tp[3 + (int)(Hs(semilla, 5, 7) * 3f)];
                Bulto(s, k.x + grosor * 0.15f, k.y, grosor * 0.38f, grosor * 0.6f, new Luz(0.3f, -0.6f, 0.3f, 0.02f));
            }
            // Ramas bajas rotas.
            int nb = 1 + (int)(Hs(semilla, 6, 7) * 2f);
            for (int b = 0; b < nb; b++)
            {
                var o = tp[3 + b * 2];
                float side = Hs(semilla, 40 + b, 7) > 0.5f ? 1f : -1f;
                float ang = side > 0f ? 25f + Hs(semilla, 41 + b, 7) * 30f : 155f - Hs(semilla, 41 + b, 7) * 30f;
                Rama(s, o, ang, alto * 0.16f, grosor * 0.38f, 3, semilla * 13 + b, luz, rug);
            }
            // Copa.
            int nc = 2 + (Hs(semilla, 8, 7) > 0.8f ? 1 : 0);
            float baseAng = 90f - lean * 40f;
            for (int b = 0; b < nc; b++)
            {
                float ang = baseAng + (b - (nc - 1) * 0.5f) * 38f + (Hs(semilla, 50 + b, 7) - 0.5f) * 20f;
                Rama(s, top, ang, (alto - horca) * (0.4f + Hs(semilla, 60 + b, 7) * 0.18f), rTop * 0.78f, 1, semilla * 7 + b, luz, rug);
            }
        }

        static void Rama(Silueta s, Vector2 o, float ang, float largo, float r, int prof, int semilla, Luz luz, float rug)
        {
            var pts = new List<Vector2> { o };
            var rs = new List<float> { r };
            int nseg = Mathf.Max(2, Mathf.RoundToInt(largo / 9f));
            float a = ang;
            var p = o;
            float rEnd = Mathf.Max(0.5f, r * 0.62f);
            for (int k = 1; k <= nseg; k++)
            {
                // Quiebros bruscos (ramas nudosas) y tendencia leve a subir.
                a += (Hs(semilla, k, 17) - 0.5f) * 52f;
                a = Mathf.Lerp(a, 90f, 0.05f);
                p = p + Dir(a) * (largo / nseg);
                pts.Add(p);
                rs.Add(Mathf.Lerp(r, rEnd, k / (float)nseg));
            }
            Trazo(s, pts, rs, luz, rug, semilla);
            if (prof >= 4 || (rEnd <= 0.6f && prof >= 3)) return;
            int hijos = 2 + (prof < 2 && Hs(semilla, 90, 17) > 0.7f ? 1 : 0);
            for (int h = 0; h < hijos; h++)
            {
                float side = h == 0 ? -1f : h == 1 ? 1f : (Hs(semilla, 91, 17) > 0.5f ? 1f : -1f);
                float na = a + side * (18f + Hs(semilla, 92 + h, 17) * 34f);
                float nl = largo * (0.55f + Hs(semilla, 95 + h, 17) * 0.25f);
                // Algunas ramas están partidas: muñón corto que no sigue.
                if (prof >= 1 && Hs(semilla, 97 + h, 17) > 0.78f) { Rama(s, p, na, nl * 0.3f, rEnd * 0.9f, 9, semilla * 3 + h + 1, luz, rug); continue; }
                Rama(s, p, na, nl, rEnd * (h == 2 ? 0.7f : 0.92f), prof + 1, semilla * 3 + h + 1, luz, rug);
            }
            // Ramita lateral desde la mitad.
            if (prof < 3 && Hs(semilla, 99, 17) > 0.55f)
            {
                var m = pts[pts.Count / 2];
                float side = Hs(semilla, 98, 17) > 0.5f ? 1f : -1f;
                Rama(s, m, a + side * 55f, largo * 0.4f, Mathf.Max(0.5f, rEnd * 0.6f), prof + 2, semilla * 5 + 11, luz, rug);
            }
        }

        /// <summary>Dos postes torcidos con una red de pesca colgando entre ellos y boyas.</summary>
        static void PosteConRed(Silueta s, float x, float y0, float alto, float vano, int semilla, Luz luz)
        {
            float h2 = alto * (0.8f + Hs(semilla, 1, 23) * 0.15f);
            var a = V(x + (Hs(semilla, 2, 23) - 0.5f) * 6f, y0 + alto);
            var b = V(x + vano + (Hs(semilla, 3, 23) - 0.5f) * 6f, y0 + h2);
            Trazo(s, new[] { V(x, y0), V(x + 1f, y0 + alto * 0.5f), a }, new[] { 2.6f, 2.1f, 1.7f }, luz);
            Trazo(s, new[] { V(x + vano, y0), V(x + vano - 1f, y0 + h2 * 0.5f), b }, new[] { 2.6f, 2.1f, 1.7f }, luz);
            Linea(s, a + V(-5f, -3f), a + V(5f, -1f), 0.8f, luz);
            Linea(s, b + V(-5f, -2f), b + V(5f, -3f), 0.8f, luz);
            float sag = vano * 0.18f;
            for (int ix = Mathf.CeilToInt(a.x); ix <= Mathf.FloorToInt(b.x); ix++)
            {
                float t = (ix - a.x) / (b.x - a.x);
                float rope = Mathf.Lerp(a.y, b.y, t) - 2f - sag * 4f * t * (1f - t);
                float bottom = y0 + alto * 0.3f + Mathf.Sin(t * 7f + semilla) * 6f + N1(ix, 8, semilla) * 18f;
                s.Set(ix, Mathf.RoundToInt(rope), 0.75f);
                s.Set(ix, Mathf.RoundToInt(rope) - 1, 0.3f);
                for (int iy = Mathf.RoundToInt(bottom); iy < rope - 1; iy++)
                {
                    int u = ix + iy + (int)(Mathf.Sin(iy * 0.2f) * 2f), v = ix - iy;
                    if (((u % 7) + 7) % 7 == 0 || ((v % 7) + 7) % 7 == 0) s.Set(ix, iy, 0.35f);
                }
                // Jirones que cuelgan.
                if (Hs(ix, 3, semilla) > 0.9f)
                    for (int iy = Mathf.RoundToInt(bottom) - 1 - (int)(Hs(ix, 4, semilla) * 14f); iy < bottom; iy++) s.Set(ix, iy, 0.35f);
                if (Mathf.Abs(Mathf.Repeat(t * 5f, 1f) - 0.5f) < 0.06f) Bulto(s, ix, rope - 2f, 1.6f, 1.6f, luz);
            }
        }

        /// <summary>Casa de Innsmouth: muro de tablas, tejado a dos aguas (de frente, de lado o holandés), chimenea, ventanas.</summary>
        static void Casa(Silueta s, int x, int y0, int w, int h, int tejado, int estilo, int semilla, bool detalle, Color32 luzVentana, float probLuz)
        {
            int top = y0 + h;
            for (int y = y0; y < top; y++)
                for (int xx = x; xx <= x + w; xx++)
                {
                    float t = 0.46f;
                    if (detalle && (y - y0) % 3 == 0) t = 0.36f;
                    if (xx == x) t = 0.7f;
                    else if (xx == x + w) t = 0.3f;
                    s.Set(xx, y, t);
                }
            bool hundido = Hs(semilla, 1, 31) > 0.72f;
            if (estilo == 0)
            {
                // Hastial de frente: triángulo con tablas de borde.
                var pts = new[] { V(x - 2, top), V(x + w + 2, top), V(x + w * 0.5f + (hundido ? 3f : 0f), top + tejado * (hundido ? 0.75f : 1f)) };
                PolyS(s, pts, (px, py) => detalle && (py - top) % 3 == 0 ? 0.34f : 0.42f);
                Linea(s, pts[0], pts[2], 1.1f, new Luz(-0.7f, 0.7f, 0.2f, 0.55f));
                Linea(s, pts[1], pts[2], 1.1f, new Luz(0.7f, 0.7f, 0.2f, 0.2f));
                if (detalle) FixOrDark(s, x + w / 2 - 1, top + tejado / 3, 3, 4, luzVentana, Hs(semilla, 2, 31) < probLuz);
            }
            else if (estilo == 1)
            {
                // Tejado de lado: trapecio con hileras de tejas.
                float inset = tejado * 0.7f;
                var pts = new[] { V(x - 3, top), V(x + w + 3, top), V(x + w - inset, top + tejado), V(x + inset, top + tejado) };
                PolyS(s, pts, (px, py) =>
                {
                    int row = (py - top) / 3;
                    float t = (py - top) % 3 == 0 ? 0.44f : 0.56f;
                    if (Hs(px / 4 + row * 3, row, semilla) > 0.86f) t = 0.38f;
                    return t;
                });
                Linea(s, pts[3], pts[2], 0.9f, new Luz(-0.3f, 1f, 0.3f, 0.6f));
                if (hundido)
                {
                    // Tejado hundido: hueco con las vigas a la vista.
                    int hx = x + w / 3, hw = w / 3;
                    for (int yy = top + 2; yy < top + tejado - 1; yy++)
                        for (int xx = hx; xx < hx + hw; xx++)
                            if (s.Lleno(xx, yy) && (xx - hx) % 5 != 0) s.Set(xx, yy, 0.08f);
                }
            }
            else
            {
                // Tejado holandés (de dos pendientes).
                float k = tejado * 0.55f;
                var pts = new[] { V(x - 3, top), V(x + w + 3, top), V(x + w - 2, top + k), V(x + w * 0.5f, top + tejado), V(x + 2, top + k) };
                PolyS(s, pts, (px, py) => py < top + k ? (px < x + w * 0.5f ? 0.58f : 0.4f) : (px < x + w * 0.5f ? 0.66f : 0.46f));
            }
            // Chimenea.
            if (Hs(semilla, 3, 31) > 0.3f)
            {
                int cx = x + (int)(w * (0.2f + Hs(semilla, 4, 31) * 0.6f));
                int ch = tejado + 4 + (int)(Hs(semilla, 5, 31) * 8f);
                RectS(s, cx, top + tejado / 3, cx + 4, top + ch, 0.4f);
                RectS(s, cx, top + tejado / 3, cx, top + ch, 0.68f);
                RectS(s, cx - 1, top + ch, cx + 5, top + ch + 1, 0.6f);
            }
            // Ventanas.
            int ww = detalle ? 4 : 2, wh = detalle ? 6 : 3;
            int stepX = detalle ? 11 : 6, stepY = detalle ? 14 : 8;
            for (int wy = y0 + (detalle ? 8 : 4); wy + wh < top - 2; wy += stepY)
                for (int wx = x + (detalle ? 5 : 3); wx + ww < x + w - 2; wx += stepX)
                {
                    float hsh = Hs(wx, wy, semilla);
                    if (hsh < 0.25f) continue;
                    bool lit = hsh > 1f - probLuz;
                    if (detalle)
                    {
                        RectS(s, wx - 1, wy - 1, wx + ww, wy + wh, 0.66f);
                        RectS(s, wx - 1, wy - 2, wx + ww + 1, wy - 2, 0.58f);
                    }
                    FixOrDark(s, wx, wy, ww, wh, luzVentana, lit);
                    if (detalle && !lit && hsh < 0.4f) Linea(s, V(wx, wy), V(wx + ww, wy + wh), 0.6f, new Luz(0f, 1f, 0.5f, 0.5f));
                    else if (detalle) { RectS(s, wx + ww / 2, wy, wx + ww / 2, wy + wh - 1, lit ? 0.3f : 0.4f); }
                }
            // Puerta.
            if (detalle && Hs(semilla, 6, 31) > 0.3f)
            {
                int dx = x + (int)(w * (0.3f + Hs(semilla, 7, 31) * 0.4f));
                RectS(s, dx - 1, y0, dx + 6, y0 + 11, 0.66f);
                RectS(s, dx, y0, dx + 5, y0 + 10, 0.08f);
            }
        }

        static void FixOrDark(Silueta s, int x, int y, int w, int h, Color32 luz, bool lit)
        {
            for (int yy = y; yy < y + h; yy++)
                for (int xx = x; xx < x + w; xx++)
                {
                    if (lit) s.Fix(xx, yy, yy == y + h - 1 || xx == x ? PixelCanvas.Lerp(luz, new Color32(255, 240, 200, 255), 0.45f) : luz);
                    else s.Set(xx, yy, 0.05f);
                }
        }

        /// <summary>Campanario de la Orden Esotérica de Dagón: torre, cuerpo de campanas, aguja torcida y veleta-pez.</summary>
        static void Campanario(Silueta s, int cx, int y0, int ancho, int alto, Color32 luzVentana, Luz luz)
        {
            int half = ancho / 2;
            int bodyTop = y0 + alto;
            for (int y = y0; y < bodyTop; y++)
                for (int x = cx - half; x <= cx + half; x++)
                {
                    float t = x == cx - half ? 0.72f : x == cx + half ? 0.28f : 0.46f;
                    if ((y - y0) % 24 == 0) t = 0.62f;
                    s.Set(x, y, t);
                }
            // Contrafuertes.
            RectS(s, cx - half - 3, y0, cx - half - 1, y0 + alto / 2, 0.66f);
            RectS(s, cx + half + 1, y0, cx + half + 3, y0 + alto / 2, 0.3f);
            // Ventana alta encendida.
            FixOrDark(s, cx - 2, y0 + alto / 2 - 8, 4, 9, luzVentana, true);
            // Esfera del reloj rota.
            Bulto(s, cx, y0 + alto - 14, half * 0.6f, half * 0.6f, new Luz(-0.5f, 0.5f, 0.7f, 0.4f));
            Linea(s, V(cx, y0 + alto - 14), V(cx + 3, y0 + alto - 11), 0.6f, new Luz(0f, 0f, 1f, 0.05f));
            // Cuerpo de campanas con arco apuntado y la campana dentro.
            int bTop = bodyTop + 30;
            RectS(s, cx - half - 2, bodyTop, cx + half + 2, bodyTop + 2, 0.66f);
            for (int y = bodyTop + 3; y < bTop; y++)
                for (int x = cx - half + 1; x <= cx + half - 1; x++)
                {
                    float hgt = y - (bodyTop + 3);
                    float ox = Mathf.Abs(x - cx) / (half - 3f);
                    bool hueco = ox < 1f && hgt < 18f + Mathf.Sqrt(Mathf.Max(0f, 1f - ox)) * 6f;
                    s.Set(x, y, hueco ? 0.06f : x <= cx - half + 1 ? 0.72f : 0.46f);
                }
            Bulto(s, cx, bodyTop + 13, 4f, 6f, luz);
            RectS(s, cx - 5, bodyTop + 6, cx + 5, bodyTop + 7, 0.5f);
            RectS(s, cx - half - 2, bTop, cx + half + 2, bTop + 2, 0.66f);
            // Aguja torcida.
            var tip = V(cx + 5f, bTop + 64f);
            PolyS(s, new[] { V(cx - half - 1, bTop + 3), V(cx + half + 1, bTop + 3), tip }, (x, y) => x < cx + (y - bTop) * 0.07f ? 0.6f : 0.36f);
            Linea(s, V(cx - half - 1, bTop + 3), tip, 0.7f, new Luz(-0.7f, 0.7f, 0.3f, 0.6f));
            // Veleta: pez de Dagón.
            Linea(s, tip, tip + V(0f, 8f), 0.6f, luz);
            Bulto(s, tip.x, tip.y + 9f, 4f, 1.6f, luz);
            PolyS(s, new[] { tip + V(-4f, 9f), tip + V(-7f, 12f), tip + V(-7f, 6f) }, 0.5f);
        }

        /// <summary>Faro en ruinas sobre un peñasco.</summary>
        static void Faro(Silueta s, int cx, int y0, int alto, Luz luz)
        {
            for (int y = y0; y < y0 + alto; y++)
            {
                float t = (y - y0) / (float)alto;
                float half = Mathf.Lerp(9f, 5f, t);
                for (int x = Mathf.FloorToInt(cx - half); x <= Mathf.CeilToInt(cx + half); x++)
                {
                    float u = (x + 0.5f - cx) / half;
                    float tone = luz.Tono(u, 0f, Mathf.Sqrt(Mathf.Max(0f, 1f - u * u)));
                    if (((y - y0) / 10) % 2 == 1) tone -= 0.12f;
                    s.Set(x, y, tone);
                }
            }
            int top = y0 + alto;
            RectS(s, cx - 8, top, cx + 8, top + 2, 0.6f);
            for (int y = top + 3; y < top + 12; y++)
                for (int x = cx - 5; x <= cx + 5; x++) s.Set(x, y, (x - cx) % 3 == 0 ? 0.5f : 0.1f);
            PolyS(s, new[] { V(cx - 7, top + 12), V(cx + 7, top + 12), V(cx, top + 20) }, 0.45f);
        }

        // ------------------------------------------------------------------
        // Costa de Innsmouth
        // ------------------------------------------------------------------

        static List<BackgroundLayer> Coast()
        {
            var layers = new List<BackgroundLayer>();
            var nieblaHz = C("88a0a4");
            var luzLuna = new Luz(-0.55f, 0.5f, 0.65f, 0.18f);
            var luzArbol = new Luz(-0.6f, 0.45f, 0.65f, 0.12f, 0.62f);
            const int hz = 150;
            const float lunaX = 520f, lunaY = 288f;

            // 1) Cielo de crepúsculo brumoso, luna velada, el mar y el Durmiente emergiendo, casi fundido en la niebla.
            var sky = new PixelCanvas(W, H);
            Gradient(sky, hz, H, C("98aeab"), C("8098a0"), C("68808f"), C("546a7f"), C("455870"), C("394a64"));
            Halo(sky, lunaX, lunaY, 260f, C("b7c4b8"), 0.3f, 170f);
            Halo(sky, lunaX, lunaY, 90f, C("d6dcc4"), 0.34f);
            Nubes(sky, 200, H, C("4a596c"), C("a8b6b0"), C("cdd4c2"), lunaX, lunaY, 230f, 0.52f, 11);
            Luna(sky, lunaX, lunaY, 21f);
            // Velo de nubes finas delante de la luna.
            for (int y = (int)lunaY - 30; y < lunaY + 30; y++)
                for (int x = (int)lunaX - 70; x < lunaX + 70; x++)
                {
                    float n = N2(x, y, 32, 77, 3f);
                    if (n > 0.6f) Blend(sky, x, y, PixelCanvas.WithAlpha(C("9aa69c"), n > 0.7f ? 0.55f : 0.3f));
                }
            Mar(sky, hz, C("536671"), C("8b9da0"), C("a0b0ae"), C("dfe4cf"), lunaX, 34f, 13);
            for (int i = 0; i < 4; i++)
                Haz(sky, lunaX + 10f + i * 26f, lunaY - 18f, 16f + i * 5f, 22f + i * 4f, 190f, C("cdd4bd"), 0.12f, 300 + i, 0.8f);

            var sleeper = new Silueta();
            Durmiente(sleeper, 60f, hz - 6f, 0.74f, new Luz(-0.6f, 0.45f, 0.55f, 0.25f));
            var sleeperRamp = Rampa(5, C("44566a"), C("4f6274"), C("72879a"));
            Sobre(sky, Resolver(sleeper, sleeperRamp, new Niebla { Color = nieblaHz, Suelo = hz - 2, Fundido = 46, Ruido = 0.5f, Semilla = 5, VelarVacio = false }));
            // Ojos del Durmiente: dos brasas enfermizas bajo el manto.
            for (int side = -1; side <= 1; side += 2)
            {
                float ex = 60f + side * 8f, ey = hz + 108f;
                Halo(sky, ex, ey, 9f, C("e8d27a"), 0.5f);
                Rect(sky, (int)ex - 1, (int)ey, (int)ex + 1, (int)ey, C("f2e6a0"));
            }
            var skyLayer = Capa("cielo", sky, 0.97f, 0.97f, -100, 0f);
            skyLayer.FillAbove = Cerrar(sky, C("394a64"), 18, true);
            skyLayer.FillBelow = Cerrar(sky, C("536671"), 8, false);
            layers.Add(skyLayer);

            // 2) Innsmouth a lo lejos: tejados, la cúpula de la Orden y un faro roto sobre las rocas.
            var far = new Silueta();
            for (int x = 0; x < W; x++)
            {
                float hill = 128f + Fbm1(x, 192, 401) * 34f;
                for (int y = 0; y < hill; y++) far.Set(x, y, 0.45f);
            }
            int fx = 4, fi = 0;
            while (fx < 470)
            {
                int w = 12 + (int)(Hs(fi, 1, 411) * 20f);
                int hgt = 10 + (int)(Hs(fi, 2, 411) * 18f);
                int baseY = 128 + (int)(Fbm1(fx + w / 2, 192, 401) * 34f) - 3;
                Casa(far, fx, baseY, w, hgt, 7 + (int)(Hs(fi, 3, 411) * 8f), (int)(Hs(fi, 4, 411) * 3f), fi * 7 + 1, false, C("cf9a58"), 0.18f);
                fx += w + 1 + (int)(Hs(fi, 5, 411) * 5f);
                fi++;
            }
            Campanario(far, 300, 150, 12, 40, C("cf9a58"), luzLuna);
            for (int i = 0; i < 6; i++)
                ArbolMuerto(far, 40f + i * 128f + Hs(i, 1, 415) * 50f, 132f, 70f + Hs(i, 2, 415) * 40f, 2.6f + Hs(i, 3, 415) * 1.4f, 40 + i, new Luz(-0.6f, 0.45f, 0.65f, 0.2f, 0.55f), 0f);
            Faro(far, 640, 150, 54, luzLuna);
            Borde(far, -1, 1, 0.92f, 0f, 2);
            var farRamp = Velar(Rampa(5, C("3b4b58"), C("51626e"), C("8a9c9c")), nieblaHz, 0.55f);
            var farC = Resolver(far, farRamp, new Niebla { Color = nieblaHz, Suelo = 116, Fundido = 50, Base = 0.12f, Semilla = 3 });
            var farLayer = Capa("pueblo_lejano", farC, 0.9f, 0.94f, -92, 0f);
            farLayer.FillBelow = nieblaHz;
            layers.Add(farLayer);

            // 3) El pueblo cercano: casas de tablas, la Orden de Dagón con su campanario, muelles y faroles.
            var town = new Silueta();
            var warm = C("e9a95a");
            int tx = -10, ti = 0;
            while (tx < W - 40)
            {
                if (tx > 300 && tx < 420) { tx = 420; continue; }
                int w = 34 + (int)(Hs(ti, 1, 421) * 40f);
                if (tx + w > W - 14) w = W - 14 - tx;
                int hgt = 34 + (int)(Hs(ti, 2, 421) * 40f);
                int baseY = 98 + (int)(Hs(ti, 6, 421) * 8f);
                Casa(town, tx, baseY, w, hgt, 16 + (int)(Hs(ti, 3, 421) * 16f), (int)(Hs(ti, 4, 421) * 3f), ti * 11 + 3, true, warm, 0.14f);
                tx += w + 3 + (int)(Hs(ti, 5, 421) * 16f);
                ti++;
            }
            Campanario(town, 360, 96, 28, 118, warm, luzLuna);
            var luzMedia = new Luz(-0.6f, 0.45f, 0.65f, 0.15f, 0.5f);
            ArbolMuerto(town, 168f, 92f, 150f, 5f, 61, luzMedia, 0.2f);
            // Muelle: tablazón y pilotes.
            for (int x = 0; x < W; x++)
            {
                for (int y = 92; y < 98; y++) town.Set(x, y, y == 97 ? 0.72f : 0.42f);
                if (x % 24 < 3) for (int y = 40; y < 92; y++) town.Set(x, y, x % 24 == 0 ? 0.6f : 0.3f);
            }
            Borde(town, -1, 1, 0.92f, 0f, 2);
            Borde(town, -1, 0, 0.8f, 0f, 2);
            var townRamp = Velar(Rampa(5, C("26323d"), C("384754"), C("526471"), C("8a9c9e")), nieblaHz, 0.3f);
            var townC = Resolver(town, townRamp, new Niebla { Color = nieblaHz, Suelo = 84, Fundido = 64, Base = 0.04f, Semilla = 7 });
            // Halo cálido de las ventanas encendidas.
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    if (town.Fijo[y * W + x].a > 0 && Hs(x, y, 5) > 0.85f) Halo(townC, x, y, 10f, warm, 0.1f);
            var townLayer = Capa("innsmouth", townC, 0.8f, 0.9f, -84, 0f);
            townLayer.FillBelow = nieblaHz;
            layers.Add(townLayer);

            // 4) Árboles muertos retorcidos y postes de redes entre la bruma de la orilla.
            var trees = new Silueta();
            ArbolMuerto(trees, 70f, 56f, 290f, 14f, 3, luzArbol, 0.4f);
            ArbolMuerto(trees, 262f, 50f, 210f, 9f, 8, luzArbol, 0.4f);
            ArbolMuerto(trees, 470f, 58f, 260f, 11f, 12, luzArbol, 0.4f);
            ArbolMuerto(trees, 694f, 62f, 186f, 7.5f, 21, luzArbol, 0.4f);
            PosteConRed(trees, 380f, 50f, 120f, 70f, 4, luzArbol);
            PosteConRed(trees, 610f, 54f, 96f, 52f, 9, luzArbol);
            for (int i = 0; i < 7; i++)
            {
                float px = 160f + i * 97f + Hs(i, 1, 431) * 30f;
                float ph = 60f + Hs(i, 2, 431) * 50f;
                float lean = (Hs(i, 3, 431) - 0.5f) * 10f;
                Trazo(trees, new[] { V(px, 40f), V(px + lean, 40f + ph) }, new[] { 2.4f, 1.8f }, luzArbol);
            }
            Borde(trees, -1, 1, 0.95f, 0.05f, 3);
            var treeRamp = Rampa(5, C("1a222b"), C("232d38"), C("2e3a47"), C("3c4b59"), C("62788a"));
            var treeC = Resolver(trees, Velar(treeRamp, nieblaHz, 0.06f), new Niebla { Color = C("7f9196"), Suelo = 58, Fundido = 80, Ruido = 0.45f, Semilla = 9 });
            var treeLayer = Capa("arboles", treeC, 0.66f, 0.85f, -74, 0f);
            treeLayer.FillBelow = C("7f9196");
            layers.Add(treeLayer);

            var mist = Fog(C("a9b8b6"), 0.4f, 70, 210, 61);
            // Haces de luna que atraviesan la bruma.
            for (int i = 0; i < 3; i++)
                Haz(mist, 120f + i * 256f + Hs(i, 1, 63) * 60f, H + 10f, 22f, 34f + Hs(i, 2, 63) * 20f, 330f, C("cfd9cc"), 0.13f, 64 + i, 0.6f);
            var fog = Layer("bruma", mist, 0.5f, 0.8f, -62, 0f);
            fog.Scroll = new Vector2(0.15f, 0f);
            layers.Add(fog);
            return layers;
        }

        static void Luna(PixelCanvas c, float cx, float cy, float r)
        {
            for (int y = Mathf.FloorToInt(cy - r); y <= Mathf.CeilToInt(cy + r); y++)
                for (int x = Mathf.FloorToInt(cx - r); x <= Mathf.CeilToInt(cx + r); x++)
                {
                    float dx = (x + 0.5f - cx) / r, dy = (y + 0.5f - cy) / r;
                    float d2 = dx * dx + dy * dy;
                    if (d2 > 1f) continue;
                    float limb = Mathf.Sqrt(1f - d2);
                    float maria = PixelCanvas.ValueNoise(x / 7f, y / 7f, 0, 71);
                    float l = limb * 0.5f + 0.5f - (maria > 0.56f ? 0.18f : 0f) - (dx > 0.3f ? (dx - 0.3f) * 0.45f : 0f);
                    Color32 col = l > 0.86f ? C("eef0dc") : l > 0.7f ? C("dadfc6") : l > 0.55f ? C("c2c9b0") : C("a7b09a");
                    Put(c, x, y, col);
                }
        }

        /// <summary>El Durmiente: cabeza de pulpo, barba de tentáculos, alas membranosas y una garra que surge del mar.</summary>
        static void Durmiente(Silueta s, float cx, float sea, float k, Luz luz)
        {
            // Alas con dedos y membrana en paneles (más clara en el centro de cada panel).
            for (int side = -1; side <= 1; side += 2)
            {
                var hombro = V(cx + side * 40f * k, sea + 112f * k);
                float[] ang = { 68f, 44f, 22f, 2f };
                float[] len = { 150f, 182f, 172f, 132f };
                var codo = hombro + Dir(side > 0 ? 62f : 118f) * 44f * k;
                var tips = new List<Vector2>();
                for (int f = 0; f < 4; f++) tips.Add(codo + Dir(side > 0 ? ang[f] : 180f - ang[f]) * len[f] * k);
                var wing = new List<Vector2> { hombro, codo };
                for (int f = 0; f < 4; f++)
                {
                    wing.Add(tips[f]);
                    if (f == 3) break;
                    // Festón: curva cóncava entre dos dedos.
                    var ctrl = Vector2.Lerp(Vector2.Lerp(tips[f], tips[f + 1], 0.5f), codo, 0.55f);
                    for (int q = 1; q < 8; q++)
                    {
                        float t = q / 8f;
                        wing.Add(Vector2.Lerp(Vector2.Lerp(tips[f], ctrl, t), Vector2.Lerp(ctrl, tips[f + 1], t), t));
                    }
                }
                var bajo = V(cx + side * 66f * k, sea + 30f * k);
                var ctrlB = Vector2.Lerp(Vector2.Lerp(tips[3], bajo, 0.5f), codo, 0.4f);
                for (int q = 1; q < 8; q++)
                {
                    float t = q / 8f;
                    wing.Add(Vector2.Lerp(Vector2.Lerp(tips[3], ctrlB, t), Vector2.Lerp(ctrlB, bajo, t), t));
                }
                wing.Add(bajo);
                var c0 = codo;
                PolyS(s, wing, (x, y) =>
                {
                    float dd = Dist(V(x, y), c0) / (180f * k);
                    return 0.26f + 0.12f * dd + (N2(x, y, 16, 501, 6f) - 0.5f) * 0.1f;
                });
                foreach (var t in tips) Trazo(s, new[] { codo, Vector2.Lerp(codo, t, 0.55f), t }, new[] { 3f * k, 2f * k, 0.8f }, luz);
                Trazo(s, new[] { hombro, codo }, new[] { 6f * k, 4.5f * k }, luz);
                PolyS(s, new[] { codo + V(-3f, 0f), codo + V(3f, 0f), codo + V(side * 5f, 14f * k) }, 0.62f);
            }
            // Torso encorvado que sale del agua.
            Bulto(s, cx, sea + 50f * k, 76f * k, 88f * k, luz, 0.16f, 503);
            Bulto(s, cx - 34f * k, sea + 106f * k, 34f * k, 26f * k, luz);
            Bulto(s, cx + 34f * k, sea + 106f * k, 34f * k, 26f * k, luz);
            // Cabeza de pulpo: manto alto inclinado hacia atrás, con pliegues.
            float hy = sea + 172f * k;
            for (int y = (int)(hy - 64f * k); y < hy + 60f * k; y++)
                for (int x = (int)(cx - 64f * k); x < cx + 64f * k; x++)
                {
                    float dx = (x - (cx - 6f * k) + (y - hy) * 0.3f) / (37f * k), dy = (y - hy) / (54f * k);
                    float d2 = dx * dx + dy * dy;
                    if (d2 >= 1f) continue;
                    float t = luz.Tono(dx, dy * 0.6f, Mathf.Sqrt(1f - d2));
                    if (N2(x, y, 8, 509, 3f) > 0.66f && dy > -0.5f) t -= 0.12f;
                    s.Set(x, y, t);
                }
            // Barba de tentáculos que cuelga sobre el pecho y se riza.
            for (int i = 0; i < 9; i++)
            {
                float x0 = cx + (-26f + i * 6.4f) * k;
                var pts = new List<Vector2>();
                var rs = new List<float>();
                float largo = (66f + (i % 3) * 16f) * k;
                for (int q = 0; q <= 10; q++)
                {
                    float t = q / 10f;
                    float curl = t > 0.72f ? (t - 0.72f) * 3.6f : 0f;
                    pts.Add(V(x0 + Mathf.Sin(t * 4.5f + i * 1.3f) * 5f * k * t + (i < 4 ? -1f : 1f) * curl * 9f * k, sea + 142f * k - largo * t + curl * curl * 7f * k));
                    rs.Add(Mathf.Lerp((4.2f - Mathf.Abs(i - 4f) * 0.25f) * k, 0.8f, t));
                }
                Trazo(s, pts, rs, luz);
            }
            // Garra que surge del mar.
            var muneca = V(cx - 150f * k, sea + 52f * k);
            Trazo(s, new[] { V(cx - 128f * k, sea - 4f), V(cx - 140f * k, sea + 26f * k), muneca }, new[] { 8f * k, 6.5f * k, 5.5f * k }, luz);
            for (int f = 0; f < 4; f++)
            {
                float a = 70f + f * 22f;
                var k1 = muneca + Dir(a) * 16f * k;
                var k2 = k1 + Dir(a - 25f) * 13f * k;
                var k3 = k2 + Dir(a - 55f) * 9f * k;
                Trazo(s, new[] { muneca, k1, k2, k3 }, new[] { 3f * k, 2.2f * k, 1.4f * k, 0.6f }, luz);
            }
            // Monolitos de R'lyeh asomando a sus pies.
            for (int i = 0; i < 6; i++)
            {
                float mx = cx + (-130f + i * 52f) * k + Hs(i, 1, 507) * 16f;
                float mh = (14f + Hs(i, 2, 507) * 34f) * k;
                float lean = (Hs(i, 3, 507) - 0.5f) * 18f * k;
                PolyS(s, new[] { V(mx - 5f, sea - 6f), V(mx + 5f, sea - 6f), V(mx + 4f + lean, sea + mh), V(mx - 4f + lean, sea + mh - 4f) }, 0.42f);
            }
            Borde(s, -1, 1, 0.9f, 0f, 2);
        }

        // ------------------------------------------------------------------
        // Ruinas Ciclópeas: la geometría imposible de R'lyeh en una bruma verdosa luminosa
        // ------------------------------------------------------------------

        /// <summary>Monolito inclinado (prisma) con hileras de glifos tallados; algunos brillan débilmente.</summary>
        static void Monolito(Silueta s, float cx, float y0, float ancho, float alto, float inclinacion, Vector2 fondo, Luz luz, Color32 runa, int semilla)
        {
            float a = inclinacion * Mathf.Deg2Rad;
            var up = V(Mathf.Sin(a), Mathf.Cos(a));
            var right = V(up.y, -up.x);
            var b0 = V(cx, y0) - right * (ancho * 0.5f);
            var b1 = V(cx, y0) + right * (ancho * 0.5f);
            // Cima rota en bisel.
            float cut = (Hs(semilla, 1, 901) - 0.5f) * ancho * 0.6f;
            var t1 = b1 + up * (alto + cut);
            var t0 = b0 + up * (alto - cut);
            Prisma(s, new[] { b0, b1, t1, t0 }, fondo, luz, 0.5f);
            // Glifos: hileras de signos de 3×4 en la cara frontal.
            for (float h = 14f; h < alto - 16f; h += 9f)
                for (float w = 5f; w < ancho - 7f; w += 6f)
                {
                    float hsh = Hs((int)(h * 7f + w), semilla, 903);
                    if (hsh < 0.35f) continue;
                    var o = b0 + right * w + up * h;
                    int pat = (int)(hsh * 4096f);
                    for (int k = 0; k < 12; k++)
                    {
                        if (((pat >> k) & 1) == 0) continue;
                        var q = o + right * (k % 3) + up * (k / 3);
                        int qx = Mathf.RoundToInt(q.x), qy = Mathf.RoundToInt(q.y);
                        if (!s.Lleno(qx, qy)) continue;
                        if (hsh > 0.975f) s.Fix(qx, qy, runa);
                        else s.Set(qx, qy, 0.28f);
                    }
                }
        }

        /// <summary>Escalinata ciclópea que sube en diagonal y se interrumpe en el aire.</summary>
        static void Escalinata(Silueta s, float x0, float y0, int peldanos, float huella, float tabica, float sesgo, Vector2 fondo, Luz luz)
        {
            for (int i = 0; i < peldanos; i++)
            {
                float x = x0 + i * huella, y = y0 + i * tabica + i * sesgo;
                var pts = new[] { V(x, y0 - 20f + i * sesgo * 0.5f), V(x + huella + 1f, y0 - 20f + i * sesgo * 0.5f), V(x + huella + 1f, y + tabica + sesgo), V(x, y + tabica) };
                Prisma(s, pts, fondo, luz, 0.46f + (i % 2) * 0.04f);
            }
        }

        /// <summary>Arco imposible: dos jambas extruidas en sentidos opuestos unidas por un dintel retorcido.</summary>
        static void ArcoImposible(Silueta s, float cx, float y0, float luzArco, float alto, float grosor, Luz luz)
        {
            float l = cx - luzArco * 0.5f, r = cx + luzArco * 0.5f;
            Prisma(s, new[] { V(l - grosor, y0), V(l, y0), V(l, y0 + alto), V(l - grosor, y0 + alto) }, V(10f, 8f), luz, 0.5f);
            Prisma(s, new[] { V(r, y0), V(r + grosor, y0), V(r + grosor, y0 + alto + 10f), V(r, y0 + alto + 10f) }, V(-10f, 8f), luz, 0.5f);
            // Dintel que se tuerce: su cara superior pasa a ser la inferior a mitad de camino.
            var top = new[] { V(l - grosor, y0 + alto), V(cx, y0 + alto + 14f), V(r + grosor, y0 + alto + 10f), V(r + grosor, y0 + alto + 10f + grosor), V(cx, y0 + alto + 14f + grosor * 0.6f), V(l - grosor, y0 + alto + grosor) };
            PolyS(s, top, (x, y) => x < cx ? 0.62f : 0.34f);
            Prisma(s, new[] { V(l - grosor, y0 + alto + grosor), V(cx, y0 + alto + 14f + grosor * 0.6f), V(cx, y0 + alto + 20f + grosor * 0.6f), V(l - grosor, y0 + alto + grosor + 8f) }, V(0f, 6f), luz, 0.55f);
        }

        /// <summary>Relieve del Durmiente tallado en una losa: cabeza de pulpo, ojos y barba de tentáculos.</summary>
        static void RelieveDurmiente(Silueta s, float cx, float cy, float k, Color32 ojo)
        {
            // Rebaje oscuro alrededor.
            for (int y = Mathf.FloorToInt(cy - 30f * k); y <= cy + 34f * k; y++)
                for (int x = Mathf.FloorToInt(cx - 22f * k); x <= cx + 22f * k; x++)
                {
                    if (!s.Lleno(x, y)) continue;
                    float dx = (x - cx) / (18f * k), dy = (y - cy - 10f * k) / (20f * k);
                    float d2 = dx * dx + dy * dy;
                    if (d2 < 1f) s.Set(x, y, 0.62f - dy * 0.25f + dx * -0.15f);
                    else if (d2 < 1.25f) s.Set(x, y, 0.2f);
                }
            for (int i = 0; i < 7; i++)
            {
                float x0 = cx + (-12f + i * 4f) * k;
                for (int y = Mathf.FloorToInt(cy - 28f * k); y < cy - 2f * k; y++)
                {
                    float t = (cy - 2f * k - y) / (26f * k);
                    int x = Mathf.RoundToInt(x0 + Mathf.Sin(t * 4f + i) * 2f * k);
                    if (s.Lleno(x, y)) { s.Set(x, y, 0.58f); s.Set(x + 1, y, 0.28f); }
                }
            }
            s.Fix(Mathf.RoundToInt(cx - 6f * k), Mathf.RoundToInt(cy + 4f * k), ojo);
            s.Fix(Mathf.RoundToInt(cx + 6f * k), Mathf.RoundToInt(cy + 4f * k), ojo);
        }

        static List<BackgroundLayer> Ruins()
        {
            var layers = new List<BackgroundLayer>();
            const float subida = 5f;
            var niebla = C("90a89a");
            var runa = C("7ff0c0");

            // 1) El abismo: bruma verde luminosa, un resplandor pálido arriba y estructuras imposibles flotando.
            var sky = new PixelCanvas(W, H);
            Gradient(sky, 0, H, C("96ac9e"), C("88a093"), C("6f8a80"), C("566f6a"), C("435a59"), C("354a4c"));
            Halo(sky, 0f, 330f, 300f, C("d6ecd4"), 0.38f, 200f);
            Halo(sky, 0f, 340f, 90f, C("eef8e4"), 0.4f);
            var lejos = new Silueta();
            var luzL = new Luz(-0.4f, 0.6f, 0.6f, 0.3f, 0.9f);
            for (int i = 0; i < 9; i++)
            {
                float x = 40f + i * 85f + Hs(i, 1, 911) * 30f;
                float y = 150f + Hs(i, 2, 911) * 170f;
                float w = 26f + Hs(i, 3, 911) * 40f, h = 18f + Hs(i, 4, 911) * 60f;
                Prisma(lejos, Box(x, y, w, h, (Hs(i, 5, 911) - 0.5f) * 70f), V((Hs(i, 6, 911) - 0.5f) * 30f, 12f), luzL, 0.5f);
            }
            for (int i = 0; i < 3; i++)
                Escalinata(lejos, 120f + i * 256f, 170f + i * 40f, 8, 9f, 6f, i % 2 == 0 ? 3f : -2f, V(6f, 5f), luzL);
            for (int i = 0; i < 4; i++)
            {
                float x = 96f + i * 192f + Hs(i, 7, 911) * 40f;
                Monolito(lejos, x, 100f, 30f + Hs(i, 8, 911) * 16f, 140f + Hs(i, 9, 911) * 110f, (Hs(i, 10, 911) - 0.5f) * 30f, V(8f, 6f), luzL, runa, 920 + i);
            }
            Borde(lejos, -1, 1, 0.95f, 0f, 2);
            var lejosRamp = Velar(Rampa(4, C("52686a"), C("647c7a"), C("a8c2b4")), niebla, 0.4f);
            Sobre(sky, Resolver(lejos, lejosRamp, new Niebla { Color = niebla, Suelo = 110, Fundido = 90, Base = 0.1f, VelarVacio = false, Semilla = 41 }));
            for (int i = 0; i < 5; i++)
                Haz(sky, -60f + i * 40f, H + 10f, 14f + i * 6f, 30f + i * 6f, 360f, C("e4f4dc"), 0.16f, 930 + i, 0.8f);
            var l0 = Capa("abismo", sky, 0.96f, 0.97f, -100, subida);
            l0.FillAbove = Cerrar(sky, C("4a6260"), 20, true);
            l0.FillBelow = Cerrar(sky, C("96ac9e"), 8, false);
            layers.Add(l0);

            // 2) Monolitos colosales inclinados y un arco imposible.
            var mono = new Silueta();
            var luzM = new Luz(-0.45f, 0.55f, 0.6f, 0.2f, 0.85f);
            Monolito(mono, 30f, 90f, 54f, 250f, 9f, V(14f, 9f), luzM, runa, 941);
            Monolito(mono, 250f, 90f, 44f, 190f, -14f, V(-12f, 9f), luzM, runa, 942);
            Monolito(mono, 520f, 90f, 60f, 230f, 6f, V(14f, 10f), luzM, runa, 943);
            ArcoImposible(mono, 390f, 100f, 90f, 120f, 18f, luzM);
            Monolito(mono, 660f, 90f, 36f, 150f, -22f, V(-10f, 8f), luzM, runa, 944);
            RelieveDurmiente(mono, 520f + Mathf.Sin(6f * Mathf.Deg2Rad) * 150f, 230f, 1f, runa);
            Borde(mono, -1, 1, 0.95f, 0f, 2);
            var monoRamp = Velar(Rampa(5, C("374c4a"), C("455c58"), C("587068"), C("728a80"), C("c6dece")), niebla, 0.26f);
            var monoC = Resolver(mono, monoRamp, new Niebla { Color = niebla, Suelo = 100, Fundido = 80, Base = 0.05f, Semilla = 43 });
            var l1 = Capa("monolitos", monoC, 0.9f, 0.94f, -92, subida);
            l1.FillBelow = niebla;
            layers.Add(l1);

            // 3) Escalinatas gigantes que no llevan a ninguna parte, con bloques volcados.
            var stairs = new Silueta();
            var luzE = new Luz(-0.5f, 0.6f, 0.5f, 0.15f, 0.8f);
            Escalinata(stairs, 60f, 80f, 14, 16f, 11f, 1.5f, V(10f, 7f), luzE);
            Escalinata(stairs, 450f, 80f, 10, 18f, 13f, -1.2f, V(-9f, 7f), luzE);
            Prisma(stairs, Box(330f, 120f, 70f, 46f, 18f), V(12f, 9f), luzE, 0.5f);
            Prisma(stairs, Box(700f, 104f, 54f, 40f, -12f), V(-10f, 8f), luzE, 0.5f);
            for (int i = 0; i < 3; i++)
                Monolito(stairs, 300f + i * 22f, 80f, 14f, 60f + i * 30f, 4f - i * 6f, V(5f, 4f), luzE, runa, 950 + i);
            Borde(stairs, -1, 1, 0.95f, 0f, 2);
            Grano(stairs, 8, 0.12f, 951);
            var stairRamp = Velar(Rampa(5, C("26332f"), C("33433e"), C("455850"), C("5e7469"), C("a6c4b2")), niebla, 0.14f);
            var stairC = Resolver(stairs, stairRamp, new Niebla { Color = niebla, Suelo = 72, Fundido = 80, Semilla = 45 });
            var l2 = Capa("escalinata", stairC, 0.8f, 0.9f, -84, subida);
            l2.FillBelow = niebla;
            layers.Add(l2);

            // 4) Haces de luz verdosa que bajan desde la grieta del cielo, con esporas.
            var rays = new PixelCanvas(W, H);
            for (int i = 0; i < 4; i++)
                Haz(rays, -40f + i * 60f + Hs(i, 1, 961) * 20f, H + 20f, 18f + i * 7f, 36f + Hs(i, 2, 961) * 20f, 380f, C("d8f2d0"), 0.22f, 962 + i, 0.6f);
            Motas(rays, 160, 60, 340, C("c8f8dc"), 0.5f, 963);
            layers.Add(Capa("rayos", rays, 0.8f, 0.9f, -80, subida));

            // 5) Bloques cercanos volcados y columnas partidas, casi en silueta, con algas colgando.
            var near = new Silueta();
            var luzC = new Luz(-0.5f, 0.5f, 0.5f, 0.08f, 0.5f);
            Prisma(near, Box(150f, 70f, 110f, 80f, 14f), V(16f, 10f), luzC, 0.36f);
            Prisma(near, Box(560f, 60f, 90f, 60f, -9f), V(-14f, 10f), luzC, 0.36f);
            Monolito(near, 120f, 60f, 34f, 240f, -7f, V(10f, 6f), luzC, runa, 971);
            Monolito(near, 610f, 60f, 28f, 180f, 12f, V(-9f, 6f), luzC, runa, 972);
            foreach (float px in new[] { 120f, 610f })
                for (int i = 0; i < 6; i++)
                {
                    float ox = px - 14f + i * 6f;
                    float oy = (px < 300f ? 280f : 220f) - Hs(i, 2, 973) * 40f;
                    float largo = 20f + Hs(i, 3, 973) * 50f;
                    var pts = new List<Vector2>();
                    var rs = new List<float>();
                    for (int q = 0; q <= 8; q++) { float t = q / 8f; pts.Add(V(ox + Mathf.Sin(t * 5f + i) * 2f, oy - largo * t)); rs.Add(Mathf.Lerp(1.4f, 0.6f, t)); }
                    Trazo(near, pts, rs, new Luz(0f, 0f, 1f, 0.3f, 0.3f));
                }
            Borde(near, -1, 1, 0.92f, 0.05f, 3);
            var nearRamp = Rampa(5, C("19221f"), C("212d29"), C("2b3934"), C("3a4b44"), C("82a696"));
            var nearC = Resolver(near, nearRamp, new Niebla { Color = C("80988c"), Suelo = 50, Fundido = 80, Ruido = 0.45f, Semilla = 47 });
            var l4 = Capa("bloques", nearC, 0.66f, 0.88f, -74, subida);
            l4.FillBelow = C("80988c");
            layers.Add(l4);

            var fog = Layer("niebla", Fog(C("b4ccbc"), 0.34f, 50, 200, 171), 0.55f, 0.8f, -62, subida * 0.2f);
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

        // ------------------------------------------------------------------
        // Santuario de Dagón: catedral gótica anegada, vidrieras doradas y el Coloso Ahogado encadenado
        // ------------------------------------------------------------------

        /// <summary>Altura de un arco apuntado sobre su arranque en x (−1 fuera del vano). R ≥ a; R = 2a es equilátero.</summary>
        static float Ojiva(float x, float cx, float a, float R)
        {
            float dx = Mathf.Abs(x - cx);
            if (dx > a) return -1f;
            float ox = dx + (R - a);
            return Mathf.Sqrt(Mathf.Max(0f, R * R - ox * ox));
        }

        /// <summary>
        /// Vidriera lanceolada: vano apuntado con parteluz, travesaños, dos lancetas menores y un óculo en la cabeza;
        /// vidrio dorado más claro arriba, paneles con variaciones y algunos rotos.
        /// </summary>
        static void Vidriera(PixelCanvas c, float cx, int y0, float a, int recto, Color32 marco, Color32[] vidrio, Color32 roto, int semilla)
        {
            float R = a * 1.7f;
            float apex = Mathf.Sqrt(R * R - (R - a) * (R - a));
            int yTop = y0 + recto + Mathf.CeilToInt(apex);
            bool Dentro(float x, float y)
            {
                float hy = y - (y0 + recto);
                if (y < y0) return false;
                if (hy < 0f) return Mathf.Abs(x - cx) <= a;
                float o = Ojiva(x, cx, a, R);
                return o > hy;
            }
            float subA = a * 0.5f - 1f;
            for (int y = y0 - 3; y <= yTop + 3; y++)
                for (int x = Mathf.FloorToInt(cx - a - 3); x <= Mathf.CeilToInt(cx + a + 3); x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    bool inner = Dentro(px, py);
                    bool outer = inner || Dentro(px - 2f, py) || Dentro(px + 2f, py) || Dentro(px, py - 2f) || Dentro(px, py + 2f);
                    if (!outer) continue;
                    bool frame = !inner || !Dentro(px - 2f, py) || !Dentro(px + 2f, py) || !Dentro(px, py + 2f) || py < y0 + 2f;
                    float hy = py - (y0 + recto);
                    // Parteluz y travesaños.
                    if (Mathf.Abs(px - cx) < 1.1f && hy < apex * 0.45f) frame = true;
                    if (hy < 0f && Mathf.Repeat(py - y0, 26f) < 1.5f) frame = true;
                    // Cabeza: dos lancetas menores y un óculo.
                    if (hy >= 0f && hy < apex * 0.5f)
                    {
                        float l = Ojiva(px, cx - a * 0.5f, subA, subA * 1.7f), r = Ojiva(px, cx + a * 0.5f, subA, subA * 1.7f);
                        float sub = Mathf.Max(l, r);
                        if (sub >= 0f && Mathf.Abs(sub - hy) < 1.2f) frame = true;
                        if (sub < 0f || hy > sub) frame = true;
                    }
                    float ocx = cx, ocy = y0 + recto + apex * 0.62f, orr = a * 0.42f;
                    float od = Mathf.Sqrt((px - ocx) * (px - ocx) + (py - ocy) * (py - ocy));
                    if (hy >= apex * 0.45f)
                    {
                        if (Mathf.Abs(od - orr) < 1.2f) frame = true;
                        else if (od > orr) frame = true;
                        if (od < orr && Mathf.Repeat(Mathf.Atan2(py - ocy, px - ocx) * Mathf.Rad2Deg + 360f, 90f) < 10f) frame = true;
                    }
                    if (frame) { Put(c, x, y, marco); continue; }
                    // Vidrio: más claro arriba; paneles de 6×8 con variación; alguno roto.
                    int cell = Mathf.FloorToInt((px - cx + 200f) / 6f) * 131 + Mathf.FloorToInt((py - y0) / 8f);
                    float h = Hs(cell, semilla, 811);
                    float t = Mathf.Clamp01((py - y0) / (float)(yTop - y0));
                    float k = t * 0.75f + h * 0.35f;
                    if (Mathf.Repeat(px - cx + 200f, 6f) < 1f || Mathf.Repeat(py - y0, 8f) < 1f) k -= 0.28f;
                    if (h > 0.93f) { Put(c, x, y, roto); continue; }
                    int idx = Mathf.Clamp(Mathf.FloorToInt(k * vidrio.Length), 0, vidrio.Length - 1);
                    Put(c, x, y, vidrio[idx]);
                }
        }

        /// <summary>Cadena de eslabones (de frente y de canto alternos) con una ligera comba.</summary>
        static void Cadena(Silueta s, Vector2 a, Vector2 b, float eslabon, float comba, Luz luz)
        {
            float len = Dist(a, b);
            var d = (b - a) / len;
            var n = new Vector2(-d.y, d.x);
            if (n.y > 0f) n = -n;
            float step = eslabon * 1.5f;
            int count = Mathf.CeilToInt(len / step);
            for (int i = 0; i <= count; i++)
            {
                float t = i / (float)count;
                var p = a + (b - a) * t + n * (comba * 4f * t * (1f - t));
                // Dirección local (derivada de la comba).
                var tan = (d * len + n * (comba * 4f * (1f - 2f * t))).normalized;
                if (i % 2 == 0)
                {
                    float rx = eslabon, ry = eslabon * 0.62f;
                    for (int y = Mathf.FloorToInt(p.y - rx - 1); y <= Mathf.CeilToInt(p.y + rx + 1); y++)
                        for (int x = Mathf.FloorToInt(p.x - rx - 1); x <= Mathf.CeilToInt(p.x + rx + 1); x++)
                        {
                            float vx = x + 0.5f - p.x, vy = y + 0.5f - p.y;
                            float u = (vx * tan.x + vy * tan.y) / rx, v = (vx * -tan.y + vy * tan.x) / ry;
                            float e = u * u + v * v;
                            if (e > 1f || e < 0.3f) continue;
                            s.Set(x, y, luz.Tono(0f, v, Mathf.Sqrt(Mathf.Max(0f, 1f - v * v))));
                        }
                }
                else Trazo(s, new[] { p - tan * eslabon * 0.9f, p + tan * eslabon * 0.9f }, new[] { eslabon * 0.32f, eslabon * 0.32f }, luz);
            }
        }

        /// <summary>
        /// El Coloso Ahogado: figura humanoide con rasgos de pez colgada de las muñecas por cadenas, la cabeza vencida
        /// de perfil, aletas en antebrazos, pantorrillas y nuca, manos y pies palmeados, costillas marcadas, taparrabos
        /// hecho jirones, algas colgando y una cadena que le ciñe la cintura.
        /// </summary>
        static void ColosoEncadenado(Silueta s, float cx, float pies, float k, Luz luz)
        {
            Vector2 P(float x, float y) => V(cx + x * k, pies + y * k);
            float R(float r) => r * k;
            var manoI = P(-44f, 258f);
            var manoD = P(42f, 262f);
            // Cadenas de las muñecas hacia la bóveda (detrás del cuerpo).
            Cadena(s, manoI, V(manoI.x - 40f, H + 8f), 4f, 2f, luz);
            Cadena(s, manoD, V(manoD.x + 46f, H + 8f), 4f, 2f, luz);
            // Piernas colgando, una algo cruzada.
            Trazo(s, new[] { P(-10f, 98f), P(-15f, 54f), P(-8f, 16f) }, new[] { R(8.5f), R(6.5f), R(4.2f) }, luz);
            Trazo(s, new[] { P(10f, 98f), P(13f, 57f), P(17f, 19f) }, new[] { R(8.5f), R(6.5f), R(4.2f) }, luz);
            Bulto(s, cx - R(15f), pies + R(55f), R(6.5f), R(5f), luz);
            Bulto(s, cx + R(13f), pies + R(58f), R(6.5f), R(5f), luz);
            // Aletas de las pantorrillas.
            PolyS(s, new[] { P(-19f, 48f), P(-32f, 28f), P(-25f, 32f), P(-28f, 18f), P(-13f, 30f) }, 0.28f);
            PolyS(s, new[] { P(18f, 50f), P(32f, 30f), P(25f, 34f), P(29f, 20f), P(14f, 32f) }, 0.28f);
            // Pies palmeados apuntando abajo.
            for (int side = -1; side <= 1; side += 2)
            {
                var tob = side < 0 ? P(-8f, 16f) : P(17f, 19f);
                PolyS(s, new[] { tob + V(-R(4f), 0f), tob + V(R(4f), 0f), tob + V(R(7f), -R(15f)), tob + V(R(1f), -R(11f)), tob + V(-R(5f), -R(16f)) }, 0.2f);
                for (int f = 0; f < 3; f++)
                    Linea(s, tob, tob + V(R(-5f + f * 6f), -R(15f + (f % 2) * 2f)), R(0.9f), luz);
            }
            // Torso: caja torácica marcada y vientre hundido.
            var torso = new[] { P(-27f, 170f), P(27f, 172f), P(21f, 138f), P(13f, 102f), P(-13f, 102f), P(-21f, 136f) };
            PolyS(s, torso, (x, y) =>
            {
                float u = (x - cx) / R(26f);
                return luz.Tono(u, 0.25f, Mathf.Sqrt(Mathf.Max(0f, 1f - u * u)));
            });
            for (int r = 0; r < 5; r++)
            {
                float ry = 160f - r * 9f;
                for (int side = -1; side <= 1; side += 2)
                    for (float t = 0.12f; t <= 1f; t += 0.03f)
                    {
                        var q = P(side * (3f + t * (19f - r)), ry - t * t * 9f);
                        s.Set(Mathf.RoundToInt(q.x), Mathf.RoundToInt(q.y), 0.03f);
                    }
            }
            Linea(s, P(0f, 166f), P(0f, 106f), 0.6f, new Luz(0f, 0f, 1f, 0.03f, 0.03f));
            // Taparrabos hecho jirones.
            for (int x = Mathf.FloorToInt(cx - R(17f)); x <= cx + R(17f); x++)
            {
                float u = (x - cx) / R(17f);
                float bottom = pies + R(84f) - (Hs(x / 2, 1, 821) > 0.6f ? R(16f) * Hs(x, 2, 821) : 0f) - (1f - u * u) * R(8f);
                for (int y = Mathf.FloorToInt(bottom); y < pies + R(108f); y++)
                    s.Set(x, y, 0.16f + (Mathf.Repeat(x * 0.37f + y * 0.03f, 1f) < 0.25f ? 0.12f : 0f));
            }
            // Hombros y brazos alzados en V hacia los grilletes.
            Bulto(s, cx - R(25f), pies + R(168f), R(11f), R(10f), luz);
            Bulto(s, cx + R(25f), pies + R(170f), R(11f), R(10f), luz);
            Trazo(s, new[] { P(-25f, 170f), P(-40f, 214f), manoI }, new[] { R(6f), R(4.6f), R(3.4f) }, luz);
            Trazo(s, new[] { P(25f, 172f), P(39f, 216f), manoD }, new[] { R(6f), R(4.6f), R(3.4f) }, luz);
            Bulto(s, cx - R(40f), pies + R(214f), R(5f), R(5f), luz);
            Bulto(s, cx + R(39f), pies + R(216f), R(5f), R(5f), luz);
            for (int side = -1; side <= 1; side += 2)
            {
                var e = side < 0 ? P(-40f, 214f) : P(39f, 216f);
                var m = side < 0 ? manoI : manoD;
                // Espinas de la aleta del antebrazo (hacia fuera).
                for (int f = 0; f < 4; f++)
                {
                    var b = Vector2.Lerp(e, m, 0.1f + f * 0.22f);
                    Trazo(s, new[] { b, b + V(side * R(8f - f), -R(6f + f)) }, new[] { R(1.5f), R(0.5f) }, luz);
                }
                // Grillete y mano palmeada colgando, dedos hacia dentro.
                RectS(s, Mathf.RoundToInt(m.x - R(5f)), Mathf.RoundToInt(m.y - R(2f)), Mathf.RoundToInt(m.x + R(5f)), Mathf.RoundToInt(m.y + R(3f)), 0.66f);
                var palma = m + V(-side * R(3f), R(5f));
                Bulto(s, palma.x, palma.y, R(4f), R(5f), luz);
                for (int f = 0; f < 4; f++)
                {
                    float ang = 90f - side * (20f + f * 18f);
                    var k1 = palma + Dir(ang) * R(8f);
                    var k2 = k1 + Dir(ang - side * 30f) * R(7f);
                    Trazo(s, new[] { palma, k1, k2 }, new[] { R(1.8f), R(1.3f), R(0.6f) }, luz);
                }
            }
            // Cabeza de pez vencida, de perfil: hocico hacia abajo, ojo vidrioso, boca y cresta.
            // (vencida hacia un lado para que el perfil se recorte contra la vidriera)
            var cab = P(16f, 160f);
            float ang0 = -38f * Mathf.Deg2Rad;
            var ax = V(Mathf.Cos(ang0), Mathf.Sin(ang0));
            var ay = V(-ax.y, ax.x);
            for (int y = Mathf.FloorToInt(cab.y - R(24f)); y <= cab.y + R(24f); y++)
                for (int x = Mathf.FloorToInt(cab.x - R(24f)); x <= cab.x + R(24f); x++)
                {
                    var v = V(x + 0.5f - cab.x, y + 0.5f - cab.y);
                    float u = (v.x * ax.x + v.y * ax.y) / R(24f), w = (v.x * ay.x + v.y * ay.y) / R(13f);
                    // Hocico que se estrecha.
                    float wmax = u > 0f ? 1f - u * u * 0.55f : Mathf.Sqrt(Mathf.Max(0f, 1f - u * u));
                    if (u > 1f || u < -1f || Mathf.Abs(w) > wmax) continue;
                    float ww = w / Mathf.Max(0.2f, wmax);
                    float t = luz.Tono(ax.x * u * 0.5f + ay.x * ww, ax.y * u * 0.5f + ay.y * ww, Mathf.Sqrt(Mathf.Max(0f, 1f - ww * ww)));
                    // Boca entreabierta: mandíbula caída.
                    if (u > 0.15f && w < -0.05f && w > -0.05f - (u - 0.15f) * 0.5f) t = 0.02f;
                    s.Set(x, y, t);
                }
            var ojo = cab + ax * R(3f) + ay * R(5f);
            Bulto(s, ojo.x, ojo.y, R(3.4f), R(3.4f), new Luz(0f, 0f, 1f, 0.03f, 0.03f));
            Bulto(s, ojo.x - R(0.6f), ojo.y + R(0.6f), R(2.2f), R(2.2f), new Luz(-0.4f, 0.6f, 0.6f, 0.4f, 0.8f));
            // Cresta dorsal espinosa sobre la nuca.
            for (int f = 0; f < 7; f++)
            {
                var b = cab - ax * R(18f - f * 3f) + ay * R(9f + Mathf.Sin(f * 0.6f) * 2f);
                Trazo(s, new[] { b, b + ay * R(9f + (f % 2) * 6f) - ax * R(4f) }, new[] { R(1.6f), R(0.6f) }, luz);
            }
            // Agallas.
            for (int g = 0; g < 3; g++)
            {
                var b = cab - ax * R(6f + g * 3f) - ay * R(2f);
                Linea(s, b, b - ay * R(7f), 0.6f, new Luz(0f, 0f, 1f, 0.03f, 0.03f));
            }
            // Algas y jirones que cuelgan de los brazos y del pecho.
            for (int i = 0; i < 10; i++)
            {
                if (i == 1 || i == 5) continue;
                var o = i < 4 ? Vector2.Lerp(P(-40f, 214f), manoI, 0.1f + i * 0.25f) : i < 8 ? Vector2.Lerp(P(39f, 216f), manoD, 0.1f + (i - 4) * 0.25f) : P(-8f + (i - 8) * 14f, 140f);
                float largo = R(24f + Hs(i, 1, 823) * 56f);
                var pts = new List<Vector2>();
                var rs = new List<float>();
                for (int q = 0; q <= 9; q++)
                {
                    float t = q / 9f;
                    pts.Add(o + V(Mathf.Sin(t * 6f + i) * R(3f), -largo * t));
                    rs.Add(Mathf.Lerp(R(1.5f), 0.6f, t));
                }
                Trazo(s, pts, rs, new Luz(0f, 0f, 1f, 0.22f, 0.22f));
            }
            // Cadena que le ciñe la cintura y cuelga hacia la bruma (delante).
            Cadena(s, P(-16f, 100f), V(cx - 190f, pies - 30f), 3.4f, 14f, luz);
            Cadena(s, P(16f, 96f), V(cx + 176f, pies - 40f), 3.4f, 16f, luz);
            Cadena(s, P(-16f, 100f), P(16f, 96f), 3f, 3f, luz);
        }

        /// <summary>Pilar gótico de haces: fuste central, columnillas adosadas, basa y capitel con hojas.</summary>
        static void Pilar(Silueta s, float cx, int y0, int y1, float ancho, Luz luz, bool roto, int semilla)
        {
            float half = ancho * 0.5f;
            int top = y1;
            Fuste(s, cx - half * 0.62f, cx + half * 0.62f, y0, top, luz, 5);
            Fuste(s, cx - half, cx - half * 0.62f, y0, top, luz);
            Fuste(s, cx + half * 0.62f, cx + half, y0, top, luz);
            // Basa escalonada.
            RectS(s, Mathf.RoundToInt(cx - half - 6), y0, Mathf.RoundToInt(cx + half + 6), y0 + 10, 0.42f);
            RectS(s, Mathf.RoundToInt(cx - half - 3), y0 + 11, Mathf.RoundToInt(cx + half + 3), y0 + 15, 0.55f);
            RectS(s, Mathf.RoundToInt(cx - half - 6), y0 + 10, Mathf.RoundToInt(cx + half + 6), y0 + 10, 0.7f);
            if (roto)
            {
                // Fuste partido: borde superior dentado.
                for (int x = Mathf.FloorToInt(cx - half); x <= cx + half; x++)
                {
                    int cut = top - (int)(Hs(x / 3, 1, semilla) * 18f) - (int)(Mathf.Abs(x - cx) * 0.3f);
                    for (int y = cut; y <= top + 2; y++) s.Borrar(x, y);
                }
                return;
            }
            // Capitel con hojas (crochets) y ábaco.
            for (int y = top; y < top + 14; y++)
            {
                float t = (y - top) / 14f;
                float w = half + t * 8f;
                for (int x = Mathf.FloorToInt(cx - w); x <= cx + w; x++)
                {
                    float u = (x - cx) / w;
                    float tone = luz.Tono(u, 0.3f, Mathf.Sqrt(Mathf.Max(0f, 1f - u * u)));
                    if (Mathf.Repeat(x - cx + y * 0.5f, 6f) < 1.2f) tone -= 0.2f;
                    s.Set(x, y, tone);
                }
            }
            RectS(s, Mathf.RoundToInt(cx - half - 10), top + 14, Mathf.RoundToInt(cx + half + 10), top + 18, 0.5f);
            RectS(s, Mathf.RoundToInt(cx - half - 10), top + 18, Mathf.RoundToInt(cx + half + 10), top + 18, 0.75f);
        }

        /// <summary>Arcada de arcos apuntados entre pilares (arquivolta con molduras) y muro de enjuta hasta arriba.</summary>
        static void Arcada(Silueta s, float[] pilares, int arranque, float grosorPilar, int alturaMuro, Luz luz)
        {
            for (int i = 0; i < pilares.Length; i++)
            {
                float a0 = pilares[i] + grosorPilar * 0.5f + 8f;
                float a1 = (i + 1 < pilares.Length ? pilares[i + 1] : pilares[0] + W) - grosorPilar * 0.5f - 8f;
                float cx = (a0 + a1) * 0.5f, a = (a1 - a0) * 0.5f, R = a * 1.35f;
                for (int x = Mathf.FloorToInt(a0 - 12f); x <= Mathf.CeilToInt(a1 + 12f); x++)
                {
                    float inner = Ojiva(x + 0.5f, cx, a, R);
                    float outer = Ojiva(x + 0.5f, cx, a + 14f, R + 14f);
                    for (int y = arranque; y < alturaMuro; y++)
                    {
                        float hy = y + 0.5f - arranque;
                        if (inner >= 0f && hy < inner) continue;
                        float t;
                        if (outer >= 0f && hy < outer)
                        {
                            // Arquivolta: tres baquetones.
                            float dIn = inner >= 0f ? hy - inner : 99f;
                            float band = Mathf.Min(dIn, 14f);
                            t = band < 3f ? 0.62f : band < 5f ? 0.3f : band < 8f ? 0.55f : band < 10f ? 0.28f : 0.48f;
                        }
                        else
                        {
                            t = 0.36f;
                            if (Mathf.Repeat(y - arranque, 12f) < 1f) t = 0.28f;
                            if (Mathf.Repeat(x + (((y - arranque) / 12) % 2) * 12, 24f) < 1f) t = 0.28f;
                        }
                        s.Set(x, y, t);
                    }
                }
            }
            // Cornisa corrida arriba.
            for (int x = 0; x < W; x++)
                for (int y = alturaMuro; y < H; y++) s.Set(x, y, y < alturaMuro + 2 ? 0.66f : y < alturaMuro + 6 ? 0.42f : 0.34f);
        }

        /// <summary>Estatua de un sacerdote encapuchado en su hornacina, con un ídolo pisciforme en las manos.</summary>
        static void Hornacina(Silueta s, float cx, int y0, Luz luz)
        {
            // Nicho apuntado oscuro.
            for (int y = y0; y < y0 + 86; y++)
                for (int x = Mathf.FloorToInt(cx - 18); x <= cx + 18; x++)
                {
                    float hy = y - (y0 + 60);
                    float o = Ojiva(x + 0.5f, cx, 18f, 30f);
                    if (hy > 0f && (o < 0f || hy > o)) continue;
                    s.Set(x, y, 0.04f);
                }
            // Peana.
            RectS(s, Mathf.RoundToInt(cx - 16), y0 - 6, Mathf.RoundToInt(cx + 16), y0 + 2, 0.5f);
            RectS(s, Mathf.RoundToInt(cx - 16), y0 + 2, Mathf.RoundToInt(cx + 16), y0 + 2, 0.75f);
            // Figura con capucha y túnica de pliegues.
            PolyS(s, new[] { V(cx - 12, y0 + 3), V(cx + 12, y0 + 3), V(cx + 8, y0 + 46), V(cx - 8, y0 + 46) }, (x, y) =>
            {
                float u = (x - cx) / 12f;
                float t = luz.Tono(u, 0f, Mathf.Sqrt(Mathf.Max(0f, 1f - u * u)));
                if (Mathf.Repeat(x - cx + 30f, 4f) < 1f) t -= 0.18f;
                return t;
            });
            Bulto(s, cx, y0 + 52f, 8f, 9f, luz);
            Bulto(s, cx, y0 + 50f, 4f, 5f, new Luz(0f, 0f, 1f, 0.03f, 0.03f));
            // Ídolo en las manos.
            Bulto(s, cx, y0 + 34f, 5f, 7f, luz);
            PolyS(s, new[] { V(cx - 5, y0 + 40), V(cx + 5, y0 + 40), V(cx, y0 + 46) }, 0.6f);
        }

        static List<BackgroundLayer> Sanctuary()
        {
            var layers = new List<BackgroundLayer>();
            const float subida = 6f;   // la nave del Santuario se recorre unas 6 casillas por encima del inicio
            var niebla = C("8f8381");
            var oro = new[] { C("b07848"), C("c88e52"), C("dca868"), C("ecc684"), C("f6e2ac"), C("fff2cc") };
            var contra = new Luz(0.15f, 0.55f, 0.2f, 0.12f, 0.6f);

            // 1) Ábside del fondo: muro velado por la bruma y vidrieras altas de luz dorada polvorienta.
            var apse = new PixelCanvas(W, H);
            Gradient(apse, 0, H, C("938684"), C("8a7d7e"), C("786c74"), C("5f5466"), C("4a4056"));
            var wall = new Silueta();
            float[] lancetas = { 64f, 256f, 448f, 640f };
            foreach (float lx in lancetas)
            {
                Fuste(wall, lx - 70f, lx - 52f, 0, 300, new Luz(-0.4f, 0f, 0.9f, 0.3f, 0.8f), 3);
                Fuste(wall, lx + 52f, lx + 70f, 0, 300, new Luz(-0.4f, 0f, 0.9f, 0.3f, 0.8f), 3);
            }
            for (int x = 0; x < W; x++)
            {
                for (int y = 120; y < 126; y++) wall.Set(x, y, y == 125 ? 0.8f : 0.45f);
                for (int y = 300; y < 308; y++) wall.Set(x, y, y == 307 ? 0.8f : 0.45f);
            }
            Sobre(apse, Resolver(wall, Velar(Rampa(4, C("5a5060"), C("6c6170"), C("8a7e86")), niebla, 0.25f), new Niebla { Color = niebla, Suelo = 60, Fundido = 90, VelarVacio = false }));
            foreach (float lx in lancetas)
            {
                Halo(apse, lx, 236f, 120f, C("e8c48a"), 0.32f, 150f);
                Vidriera(apse, lx, 136, 34f, 96, C("8a7470"), oro, C("6e5e66"), (int)lx);
            }
            // Rosetones sobre las lancetas.
            foreach (float lx in lancetas)
            {
                Halo(apse, lx, 318f, 40f, C("f0d49a"), 0.3f);
                Roseton(apse, lx, 318f, 17f, C("8a7470"), oro);
            }
            var l0 = Capa("abside", apse, 0.96f, 0.97f, -100, subida);
            l0.FillAbove = Cerrar(apse, C("4a4056"), 12, true);
            l0.FillBelow = Cerrar(apse, C("938684"), 8, false);
            layers.Add(l0);

            // 2) El Coloso Ahogado encadenado, a contraluz entre las vidrieras.
            var giant = new Silueta();
            ColosoEncadenado(giant, 112f, 92f, 1.02f, contra);
            Borde(giant, -1, 0, 0.95f, 0f, 2);
            Borde(giant, 1, 0, 0.95f, 0f, 2);
            Borde(giant, 0, 1, 0.95f, 0f, 2);
            var giantRamp = Velar(Rampa(5, C("3a3038"), C("4a3e46"), C("5e5056"), C("786466"), C("e6c088")), niebla, 0.22f);
            var giantC = Resolver(giant, giantRamp, new Niebla { Color = niebla, Suelo = 92, Fundido = 80, Ruido = 0.5f, Semilla = 31 });
            var l1 = Capa("coloso", giantC, 0.93f, 0.95f, -95, subida);
            l1.FillBelow = niebla;
            layers.Add(l1);

            // 3) La nave: pilares de haces y arcos apuntados con lámparas colgantes.
            var nave = new Silueta();
            float[] pilares = { 144f, 336f, 528f, 720f };
            var luzNave = new Luz(-0.25f, 0.3f, 0.5f, 0.15f, 0.75f);
            Arcada(nave, pilares, 262, 44f, 344, luzNave);
            foreach (float px in pilares) Pilar(nave, px, 40, 248, 44f, luzNave, false, (int)px);
            for (int i = 0; i < pilares.Length; i++)
            {
                // Lámpara de aceite colgando del arco.
                float lx = pilares[i] + 96f;
                Trazo(nave, new[] { V(lx, 344f), V(lx, 300f) }, new[] { 0.6f, 0.6f }, luzNave);
                Bulto(nave, lx, 296f, 5f, 4f, luzNave);
                nave.Fix(Mathf.RoundToInt(lx), 293, C("ffd890"));
                nave.Fix(Mathf.RoundToInt(lx) - 1, 293, C("f0b060"));
                nave.Fix(Mathf.RoundToInt(lx) + 1, 293, C("f0b060"));
            }
            Borde(nave, -1, 0, 0.95f, 0.05f, 2);
            Borde(nave, 1, 0, 0.95f, 0.05f, 2);
            var naveRamp = Velar(Rampa(5, C("2f282f"), C("3d343c"), C("4f454c"), C("675a5e"), C("d2a86e")), niebla, 0.18f);
            var naveC = Resolver(nave, naveRamp, new Niebla { Color = niebla, Suelo = 70, Fundido = 80, Ruido = 0.4f, Semilla = 33 });
            for (int i = 0; i < pilares.Length; i++) Halo(naveC, pilares[i] + 96f, 293f, 14f, C("ffc878"), 0.35f);
            var l2 = Capa("nave", naveC, 0.85f, 0.9f, -88, subida);
            l2.FillBelow = niebla;
            l2.FillAbove = Cerrar(naveC, naveC.Pixels[(H - 1) * W], 4, true);
            layers.Add(l2);

            // 4) Haces de luz dorada que caen de las vidrieras, con polvo en suspensión.
            var rays = new PixelCanvas(W, H);
            for (int i = 0; i < 4; i++)
                Haz(rays, -10f + i * 192f + Hs(i, 1, 851) * 30f, H + 20f, 24f, 46f + Hs(i, 2, 851) * 22f, 400f, C("f6dca6"), 0.34f, 852 + i, 0.7f);
            Motas(rays, 260, 40, 340, C("ffe8b8"), 0.55f, 853);
            var l3 = Capa("rayos", rays, 0.8f, 0.88f, -84, subida);
            layers.Add(l3);

            // 5) Pilares cercanos, casi en silueta, con un santo en su hornacina y un pilar partido.
            var near = new Silueta();
            var luzCerca = new Luz(-0.3f, 0.2f, 0.5f, 0.08f, 0.5f);
            Pilar(near, 150f, 30, 330, 70f, luzCerca, false, 861);
            Hornacina(near, 150f, 120, luzCerca);
            Pilar(near, 560f, 30, 300, 62f, luzCerca, true, 862);
            // Juntas de los tambores, desconchones y algas secas colgando.
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float t = near.Get(x, y);
                    if (t < 0f) continue;
                    if (y % 38 == 0 && y > 50) near.Set(x, y, t - 0.2f);
                    else if (N2(x, y, 8, 865, 6f) > 0.72f) near.Set(x, y, t + 0.14f);
                }
            foreach (float px in new[] { 150f, 560f })
                for (int i = 0; i < 7; i++)
                {
                    float ox = px - 30f + i * 10f + Hs(i, 1, 866) * 6f;
                    float oy = (px < 300f ? 344f : 290f) - Hs(i, 2, 866) * 30f;
                    float largo = 20f + Hs(i, 3, 866) * 60f;
                    var pts = new List<Vector2>();
                    var rs = new List<float>();
                    for (int q = 0; q <= 8; q++) { float t = q / 8f; pts.Add(V(ox + Mathf.Sin(t * 5f + i) * 2.5f, oy - largo * t)); rs.Add(Mathf.Lerp(1.6f, 0.6f, t)); }
                    Trazo(near, pts, rs, new Luz(0f, 0f, 1f, 0.3f, 0.3f));
                }
            Borde(near, -1, 0, 0.92f, 0.05f, 3);
            Borde(near, 1, 0, 0.92f, 0.05f, 3);
            Borde(near, 0, 1, 0.92f, 0.05f, 3);
            var nearRamp = Rampa(5, C("1e181d"), C("2a2228"), C("3a3036"), C("524448"), C("b08a60"));
            var nearC = Resolver(near, nearRamp, new Niebla { Color = C("7e7378"), Suelo = 50, Fundido = 80, Ruido = 0.4f, Semilla = 35 });
            var l4 = Capa("pilares", nearC, 0.66f, 0.9f, -76, subida);
            l4.FillBelow = C("7e7378");
            layers.Add(l4);

            var dust = Fog(C("cbb294"), 0.26f, 40, 200, 191);
            Motas(dust, 120, 60, 320, C("ffe6b0"), 0.6f, 871);
            var l5 = Layer("polvo", dust, 0.55f, 0.85f, -66, subida * 0.15f);
            l5.Scroll = new Vector2(0.05f, 0.02f);
            layers.Add(l5);
            return layers;
        }

        /// <summary>Rosetón dorado con tracería radial.</summary>
        static void Roseton(PixelCanvas c, float cx, float cy, float r, Color32 marco, Color32[] vidrio)
        {
            for (int y = Mathf.FloorToInt(cy - r - 3); y <= cy + r + 3; y++)
                for (int x = Mathf.FloorToInt(cx - r - 3); x <= cx + r + 3; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d > r + 3f) continue;
                    float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 180f;
                    bool tracery = d > r || Mathf.Abs(d - r * 0.55f) < 1.2f || d < r * 0.18f || Mathf.Abs(Mathf.Repeat(ang, 30f) - 15f) > 13.4f;
                    if (tracery) { Put(c, x, y, marco); continue; }
                    float k = 1f - d / r * 0.6f + Hs(Mathf.FloorToInt(ang / 30f), d > r * 0.55f ? 1 : 0, 881) * 0.3f;
                    Put(c, x, y, vidrio[Mathf.Clamp(Mathf.FloorToInt(k * vidrio.Length), 0, vidrio.Length - 1)]);
                }
        }

        // ------------------------------------------------------------------
        // Arrecife del Abismo: crepúsculo púrpura y naranja, mar tempestuoso y R'lyeh emergiendo
        // ------------------------------------------------------------------

        /// <summary>Coral ramificado en asta (como un árbol muerto pero con las puntas romas y pólipos).</summary>
        static void CoralAsta(Silueta s, float x0, float y0, float alto, float grosor, int semilla, Luz luz)
        {
            Trazo(s, new[] { V(x0, y0 - 4f), V(x0 + (Hs(semilla, 1, 1001) - 0.5f) * 10f, y0 + alto * 0.3f) }, new[] { grosor * 1.4f, grosor }, luz, 0.2f, semilla);
            var top = V(x0 + (Hs(semilla, 1, 1001) - 0.5f) * 10f, y0 + alto * 0.3f);
            int ramas = 3 + (int)(Hs(semilla, 2, 1001) * 2f);
            for (int b = 0; b < ramas; b++)
            {
                float ang = 90f + (b - (ramas - 1) * 0.5f) * 26f + (Hs(semilla, 3 + b, 1001) - 0.5f) * 14f;
                CoralRama(s, top, ang, alto * (0.32f + Hs(semilla, 10 + b, 1001) * 0.2f), grosor * 0.8f, 0, semilla * 5 + b, luz);
            }
        }

        static void CoralRama(Silueta s, Vector2 o, float ang, float largo, float r, int prof, int semilla, Luz luz)
        {
            var pts = new List<Vector2> { o };
            var rs = new List<float> { r };
            var p = o;
            float a = ang;
            for (int k = 1; k <= 4; k++)
            {
                a = Mathf.Lerp(a + (Hs(semilla, k, 1003) - 0.5f) * 20f, 90f, 0.18f);
                p = p + Dir(a) * (largo / 4f);
                pts.Add(p);
                rs.Add(Mathf.Lerp(r, r * 0.75f, k / 4f));
            }
            Trazo(s, pts, rs, luz);
            if (prof >= 2 || r < 1.6f)
            {
                // Punta roma con pólipos.
                Bulto(s, p.x, p.y + 1f, r * 1.1f, r * 1.2f, luz);
                return;
            }
            for (int h = 0; h < 2; h++)
                CoralRama(s, p, a + (h == 0 ? -1f : 1f) * (18f + Hs(semilla, 20 + h, 1003) * 16f), largo * 0.7f, r * 0.72f, prof + 1, semilla * 3 + h + 1, luz);
        }

        /// <summary>Abanico de mar: celosía en semicírculo sobre un tallo corto.</summary>
        static void Abanico(Silueta s, float cx, float y0, float radio, int semilla, Luz luz)
        {
            Trazo(s, new[] { V(cx, y0 - 2f), V(cx, y0 + radio * 0.25f) }, new[] { 2.4f, 1.8f }, luz);
            float oy = y0 + radio * 0.25f;
            for (int y = Mathf.FloorToInt(oy); y <= oy + radio; y++)
                for (int x = Mathf.FloorToInt(cx - radio); x <= cx + radio; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - oy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float edge = radio * (0.85f + 0.15f * N2(x, y, 8, semilla, 8f));
                    if (d > edge || dy < 0f) continue;
                    float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    bool vena = Mathf.Repeat(ang, 9f) < 1.6f || Mathf.Repeat(d, 6f) < 1f;
                    s.Set(x, y, d > edge - 1.5f ? 0.62f : vena ? 0.4f : 0.22f);
                }
        }

        /// <summary>Peñasco dentado con sombreado por facetas y percebes.</summary>
        static void Penasco(Silueta s, float cx, float y0, float ancho, float alto, int semilla, Luz luz)
        {
            var pts = new List<Vector2>();
            int n = 14;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                float x = cx - ancho * 0.5f + ancho * t;
                float h = alto * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * Mathf.PI)), 0.7f) * (0.65f + 0.45f * Hs(i, semilla, 1011));
                if (i % 3 == 1) h += alto * 0.18f;
                pts.Add(V(x, y0 + h));
            }
            pts.Add(V(cx + ancho * 0.5f, y0 - 10f));
            pts.Add(V(cx - ancho * 0.5f, y0 - 10f));
            PolyS(s, pts, (x, y) =>
            {
                // Facetas: la normal cambia por bandas inclinadas.
                float f = N2(x * 0.7f + y * 0.5f, y, 16, semilla, 6f);
                return luz.Tono((f - 0.5f) * 1.6f, 0.4f, 0.6f);
            });
            for (int i = 0; i < 18; i++)
            {
                float x = cx + (Hs(i, 1, semilla) - 0.5f) * ancho * 0.8f;
                float y = y0 + Hs(i, 2, semilla) * alto * 0.5f;
                if (s.Lleno(Mathf.RoundToInt(x), Mathf.RoundToInt(y))) Bulto(s, x, y, 1.6f, 1.3f, luz);
            }
        }

        static List<BackgroundLayer> Reef()
        {
            var layers = new List<BackgroundLayer>();
            const float subida = 6f;
            const int hz = 150;
            var bruma = C("c08276");
            const float solX = 20f;

            // 1) Cielo de crepúsculo con nubes de tormenta encendidas por debajo y el mar con el reflejo del sol.
            var sky = new PixelCanvas(W, H);
            Gradient(sky, hz, H, C("f2ac6a"), C("dc8a62"), C("b46a6a"), C("82526e"), C("5a4068"), C("3b2e56"));
            Halo(sky, solX, hz + 18f, 320f, C("ffc286"), 0.45f, 130f);
            Halo(sky, solX, hz + 10f, 70f, C("ffe6b0"), 0.55f);
            Nubes(sky, 186, H, C("4c3a60"), C("e0906e"), C("ffd2a0"), solX, hz + 20f, 340f, 0.5f, 1021);
            // Relámpago lejano en la tormenta.
            var bolt = new List<Vector2> { V(560f, 330f) };
            for (int i = 0; i < 9; i++) bolt.Add(bolt[i] + V((Hs(i, 1, 1023) - 0.5f) * 16f, -16f));
            Halo(sky, 560f, 270f, 60f, C("c8b8f0"), 0.2f);
            for (int i = 0; i + 1 < bolt.Count; i++) Line(sky, bolt[i], bolt[i + 1], 1.2f, C("efe6ff"));
            Mar(sky, hz, C("3a2c48"), C("b47872"), C("c88a7a"), C("ffd8a0"), solX, 70f, 1025);
            var l0 = Capa("cielo", sky, 0.97f, 0.97f, -100, subida);
            l0.FillAbove = Cerrar(sky, C("3b2e56"), 18, true);
            l0.FillBelow = Cerrar(sky, C("3a2c48"), 8, false);
            layers.Add(l0);

            // 2) R'lyeh emergiendo en el horizonte: torres inclinadas, la gran puerta sellada y monolitos.
            var city = new Silueta();
            var luzSol = new Luz(-0.7f, 0.3f, 0.5f, 0.18f, 0.85f);
            float cc = 240f;
            Prisma(city, new[] { V(cc - 40f, hz - 8f), V(cc + 40f, hz - 8f), V(cc + 34f, hz + 96f), V(cc - 30f, hz + 104f) }, V(12f, 8f), luzSol, 0.42f);
            // La puerta: vano oscuro con dintel colosal.
            PolyS(city, new[] { V(cc - 16f, hz - 6f), V(cc + 14f, hz - 6f), V(cc + 12f, hz + 60f), V(cc - 14f, hz + 64f) }, 0.08f);
            RelieveDurmiente(city, cc, hz + 80f, 0.7f, C("7ff0c0"));
            for (int i = 0; i < 9; i++)
            {
                float x = cc - 190f + i * 48f + Hs(i, 1, 1031) * 18f;
                if (Mathf.Abs(x - cc) < 50f) continue;
                float h = 50f + Hs(i, 2, 1031) * 120f * (1f - Mathf.Abs(x - cc) / 260f);
                Monolito(city, x, hz - 8f, 14f + Hs(i, 3, 1031) * 16f, h, (Hs(i, 4, 1031) - 0.5f) * 40f, V(6f, 5f), luzSol, C("7ff0c0"), 1040 + i);
            }
            Borde(city, -1, 1, 0.95f, 0f, 2);
            Borde(city, -1, 0, 0.95f, 0f, 2);
            var cityRamp = Velar(Rampa(5, C("3e2c4c"), C("4c3658"), C("5e4262"), C("7a546c"), C("f4a874")), bruma, 0.35f);
            Sobre(sky, Resolver(city, cityRamp, new Niebla { Color = bruma, Suelo = hz - 2, Fundido = 40, Base = 0.08f, VelarVacio = false, Semilla = 51 }));

            // 3) Oleaje tempestuoso con crestas de espuma encendidas por el ocaso.
            var waves = new PixelCanvas(W, H);
            var agua = new[] { C("2c2240"), C("392b4c"), C("4c3a5a"), C("6a4e66") };
            for (int x = 0; x < W; x++)
            {
                float a = x / (float)W * Mathf.PI * 2f;
                float crest = 124f + 9f * Mathf.Sin(a * 5f) + 6f * Mathf.Sin(a * 11f + 1f) + 5f * N1(x, 16, 1051);
                for (int y = 0; y < crest; y++)
                {
                    float d = crest - y;
                    // La cara de la ola: más clara arriba (luz del ocaso) y con vetas de espuma.
                    float k = Mathf.Clamp01(1f - d / 30f) * 0.8f + (N2(x + y * 2, y, 8, 1052, 2f) - 0.5f) * 0.5f;
                    var col = agua[Mathf.Clamp(Mathf.FloorToInt(k * agua.Length), 0, agua.Length - 1)];
                    if (d < 1.5f) col = C("ffd2b0");
                    else if (d < 4f && N1(x, 4, 1053) > 0.45f) col = C("e8a888");
                    else if (d < 9f && N2(x, y, 4, 1054, 1f) > 0.72f) col = C("b8807e");
                    waves.Pixels[y * W + x] = col;
                }
                // Salpicaduras sobre las crestas.
                for (int k = 0; k < 3; k++)
                    if (Hs(x, k, 1055) > 0.93f) Blend(waves, x, Mathf.RoundToInt(crest + 2f + Hs(x, k + 5, 1055) * 10f), PixelCanvas.WithAlpha(C("ffd8c0"), 0.7f));
            }
            for (int y = 0; y < 40; y++)
                for (int x = 0; x < W; x++) waves.Pixels[y * W + x] = agua[0];
            var l2 = Capa("oleaje", waves, 0.84f, 0.9f, -86, subida);
            l2.FillBelow = agua[0];
            layers.Add(l2);

            // 4) El arrecife: corales gigantes en asta, abanicos y un arco de roca.
            var reef = new Silueta();
            var luzR = new Luz(-0.7f, 0.4f, 0.5f, 0.12f, 0.75f);
            Penasco(reef, 120f, 70f, 170f, 90f, 1061, luzR);
            Penasco(reef, 470f, 70f, 220f, 70f, 1062, luzR);
            var luzCoral = new Luz(-0.7f, 0.4f, 0.5f, 0.1f, 0.58f);
            CoralAsta(reef, 90f, 140f, 160f, 9f, 1063, luzCoral);
            CoralAsta(reef, 430f, 130f, 190f, 10f, 1064, luzCoral);
            CoralAsta(reef, 640f, 90f, 140f, 8f, 1065, luzCoral);
            Abanico(reef, 180f, 150f, 34f, 1066, luzR);
            Abanico(reef, 540f, 136f, 44f, 1067, luzR);
            Abanico(reef, 720f, 96f, 30f, 1068, luzR);
            // Arco de roca.
            for (int x = 260; x < 380; x++)
            {
                float t = (x - 260) / 120f;
                float outer = 70f + Mathf.Sin(t * Mathf.PI) * 150f + N1(x, 8, 1069) * 8f;
                float inner = t > 0.18f && t < 0.82f ? 70f + Mathf.Sin((t - 0.18f) / 0.64f * Mathf.PI) * 112f : -1f;
                for (int y = 60; y < outer; y++)
                {
                    if (inner > 0f && y < inner) continue;
                    float u = Mathf.Clamp((t - 0.5f) * 2f, -1f, 1f);
                    reef.Set(x, y, luzR.Tono(u, (y - inner) / 40f, 0.6f) + (N2(x, y, 4, 1070, 4f) - 0.5f) * 0.2f);
                }
            }
            Borde(reef, -1, 1, 0.95f, 0.05f, 2);
            Borde(reef, -1, 0, 0.9f, 0.05f, 2);
            var reefRamp = Velar(Rampa(5, C("261b30"), C("33243e"), C("46324e"), C("62465e"), C("f09a6c")), bruma, 0.12f);
            var reefC = Resolver(reef, reefRamp, new Niebla { Color = C("a8747a"), Suelo = 66, Fundido = 80, Ruido = 0.5f, Semilla = 53 });
            var l3 = Capa("arrecife", reefC, 0.72f, 0.87f, -78, subida);
            l3.FillBelow = C("a8747a");
            layers.Add(l3);

            // 5) Escollos cercanos casi negros-púrpura con percebes y algas, la espuma rompiendo en ellos.
            var rocks = new Silueta();
            var luzN = new Luz(-0.7f, 0.4f, 0.5f, 0.06f, 0.5f);
            Penasco(rocks, 40f, 40f, 130f, 170f, 1071, luzN);
            Penasco(rocks, 330f, 40f, 90f, 110f, 1072, luzN);
            Penasco(rocks, 600f, 40f, 160f, 140f, 1073, luzN);
            for (int i = 0; i < 9; i++)
            {
                float ox = 20f + i * 80f + Hs(i, 1, 1074) * 30f;
                float largo = 30f + Hs(i, 2, 1074) * 60f;
                var pts = new List<Vector2>();
                var rs = new List<float>();
                for (int q = 0; q <= 8; q++) { float t = q / 8f; pts.Add(V(ox + Mathf.Sin(t * 4f + i) * 4f, 60f + largo * t)); rs.Add(Mathf.Lerp(1.8f, 0.6f, t)); }
                Trazo(rocks, pts, rs, luzN);
            }
            Borde(rocks, -1, 1, 0.92f, 0.05f, 3);
            var rockRamp = Rampa(5, C("1c1521"), C("251c2b"), C("322637"), C("473547"), C("d2875e"));
            var rockC = Resolver(rocks, rockRamp, new Niebla { Color = C("9a6c76"), Suelo = 44, Fundido = 70, Ruido = 0.5f, Semilla = 55 });
            var l4 = Capa("escollos", rockC, 0.6f, 0.85f, -70, subida);
            l4.FillBelow = C("9a6c76");
            layers.Add(l4);

            var spray = Fog(C("e0b4a8"), 0.32f, 60, 200, 291);
            Motas(spray, 140, 60, 260, C("ffe0c8"), 0.5f, 1081);
            var l5 = Layer("espuma", spray, 0.5f, 0.8f, -62, subida * 0.2f);
            l5.Scroll = new Vector2(0.3f, 0f);
            layers.Add(l5);
            return layers;
        }
    }
}
