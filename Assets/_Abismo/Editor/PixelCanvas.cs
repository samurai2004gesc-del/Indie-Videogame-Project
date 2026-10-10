using System.Collections.Generic;
using UnityEngine;

namespace Abismo.EditorTools
{
    /// <summary>
    /// Lienzo de píxeles muy simple para dibujar el arte provisional por código.
    /// Coordenadas: (0,0) es la esquina INFERIOR izquierda, como en las texturas de Unity.
    /// </summary>
    public class PixelCanvas
    {
        public readonly int Width;
        public readonly int Height;
        public readonly Color32[] Pixels;
        readonly bool[] emissive;

        public static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        public PixelCanvas(int width, int height)
        {
            Width = width;
            Height = height;
            Pixels = new Color32[width * height];
            emissive = new bool[width * height];
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public Color32 Get(int x, int y) => InBounds(x, y) ? Pixels[y * Width + x] : Clear;

        public bool IsOpaque(int x, int y) => Get(x, y).a > 0;

        public void Set(int x, int y, Color32 c, bool glow = false)
        {
            if (!InBounds(x, y)) return;
            Pixels[y * Width + x] = c;
            emissive[y * Width + x] = glow;
        }

        /// <summary>Pinta encima mezclando con la transparencia del color.</summary>
        public void Blend(int x, int y, Color32 c)
        {
            if (!InBounds(x, y) || c.a == 0) return;
            Color32 dst = Pixels[y * Width + x];
            float a = c.a / 255f;
            float outA = a + dst.a / 255f * (1f - a);
            if (outA <= 0f) return;
            byte Mix(byte s, byte d) => (byte)Mathf.Clamp(Mathf.RoundToInt((s * a + d * (dst.a / 255f) * (1f - a)) / outA), 0, 255);
            Pixels[y * Width + x] = new Color32(Mix(c.r, dst.r), Mix(c.g, dst.g), Mix(c.b, dst.b), (byte)Mathf.RoundToInt(outA * 255f));
        }

        // ------------------------------------------------------------------
        // Formas
        // ------------------------------------------------------------------

        public void FillRect(int x0, int y0, int x1, int y1, Color32 c, bool glow = false)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    Set(x, y, c, glow);
        }

        public void FillEllipse(float cx, float cy, float rx, float ry, Color32 c, bool glow = false)
        {
            int x0 = Mathf.FloorToInt(cx - rx), x1 = Mathf.CeilToInt(cx + rx);
            int y0 = Mathf.FloorToInt(cy - ry), y1 = Mathf.CeilToInt(cy + ry);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                    if (dx * dx + dy * dy <= 1f) Set(x, y, c, glow);
                }
            }
        }

        public void FillCircle(float cx, float cy, float r, Color32 c, bool glow = false) => FillEllipse(cx, cy, r, r, c, glow);

        public void FillPolygon(Vector2[] points, Color32 c, bool glow = false)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var p in points)
            {
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }
            for (int y = Mathf.FloorToInt(minY); y <= Mathf.CeilToInt(maxY); y++)
            {
                for (int x = Mathf.FloorToInt(minX); x <= Mathf.CeilToInt(maxX); x++)
                {
                    if (Inside(points, x + 0.5f, y + 0.5f)) Set(x, y, c, glow);
                }
            }
        }

        static bool Inside(Vector2[] poly, float px, float py)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if ((poly[i].y > py) != (poly[j].y > py) &&
                    px < (poly[j].x - poly[i].x) * (py - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        /// <summary>Línea gruesa (se estampan círculos a lo largo).</summary>
        public void Line(float x0, float y0, float x1, float y1, float thickness, Color32 c, bool glow = false)
        {
            float length = Mathf.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
            int steps = Mathf.Max(1, Mathf.CeilToInt(length * 2f));
            float r = Mathf.Max(0.5f, thickness * 0.5f);
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                FillCircle(Mathf.Lerp(x0, x1, t), Mathf.Lerp(y0, y1, t), r, c, glow);
            }
        }

        // ------------------------------------------------------------------
        // Acabados: sombreado automático y contorno
        // ------------------------------------------------------------------

        /// <summary>
        /// Da volumen: aclara los bordes que miran arriba/derecha (luz de la luna) y oscurece
        /// los de abajo/izquierda. Los píxeles "emisivos" (ojos, brillos) no se tocan.
        /// </summary>
        public void Shade(float light = 0.22f, float dark = 0.3f)
        {
            var copy = (Color32[])Pixels.Clone();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int i = y * Width + x;
                    if (copy[i].a == 0 || emissive[i]) continue;
                    bool Transparent(int px, int py) => !InBounds(px, py) || copy[py * Width + px].a == 0;
                    Color32 c = copy[i];
                    if (Transparent(x + 1, y) || Transparent(x, y + 1)) c = Lerp(c, new Color32(235, 255, 240, 255), light);
                    else if (Transparent(x - 1, y) || Transparent(x, y - 1)) c = Lerp(c, new Color32(0, 0, 0, 255), dark);
                    // Un poco más oscuro abajo: los pies quedan en penumbra.
                    float floor = 0.82f + 0.18f * (y / (float)Mathf.Max(1, Height - 1));
                    Pixels[i] = new Color32((byte)(c.r * floor), (byte)(c.g * floor), (byte)(c.b * floor), c.a);
                }
            }
        }

        /// <summary>Añade un contorno de 1 píxel alrededor de la silueta.</summary>
        public void Outline(Color32 color)
        {
            var copy = (Color32[])Pixels.Clone();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (copy[y * Width + x].a != 0) continue;
                    bool Opaque(int px, int py) => InBounds(px, py) && copy[py * Width + px].a > 128;
                    if (Opaque(x + 1, y) || Opaque(x - 1, y) || Opaque(x, y + 1) || Opaque(x, y - 1)) Set(x, y, color);
                }
            }
        }

        public static Color32 Lerp(Color32 a, Color32 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color32(
                (byte)Mathf.RoundToInt(a.r + (b.r - a.r) * t),
                (byte)Mathf.RoundToInt(a.g + (b.g - a.g) * t),
                (byte)Mathf.RoundToInt(a.b + (b.b - a.b) * t),
                (byte)Mathf.RoundToInt(a.a + (b.a - a.a) * t));
        }

        public static Color32 WithAlpha(Color32 c, float alpha) => new Color32(c.r, c.g, c.b, (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f));

        /// <summary>"#RRGGBB" o "#RRGGBBAA" → color.</summary>
        public static Color32 Hex(string hex)
        {
            hex = hex.TrimStart('#');
            byte P(int i) => (byte)System.Convert.ToInt32(hex.Substring(i, 2), 16);
            return new Color32(P(0), P(2), P(4), hex.Length >= 8 ? P(6) : (byte)255);
        }

        // ------------------------------------------------------------------
        // Arte en texto (ASCII)
        // ------------------------------------------------------------------

        /// <summary>
        /// Convierte un dibujo hecho con letras en píxeles. Cada letra es un color de la paleta,
        /// '.' es transparente. La primera fila del texto es la de ARRIBA.
        /// Si <paramref name="padding"/> es true se deja 1 píxel libre alrededor para el contorno.
        /// </summary>
        /// <summary>
        /// Lee un PNG de 8 bits por canal (RGB o RGBA, sin entrelazar), como los que deja Tools/ExtraerHoja.
        /// Sin dependencias de Unity: sirve igual en el editor que en las herramientas de vista previa.
        /// </summary>
        public static PixelCanvas LoadPng(string path)
        {
            byte[] data = System.IO.File.ReadAllBytes(path);
            int pos = 8, width = 0, height = 0, colorType = 0;
            var idat = new System.IO.MemoryStream();
            while (pos + 8 <= data.Length)
            {
                int length = (data[pos] << 24) | (data[pos + 1] << 16) | (data[pos + 2] << 8) | data[pos + 3];
                string type = System.Text.Encoding.ASCII.GetString(data, pos + 4, 4);
                int body = pos + 8;
                if (type == "IHDR")
                {
                    width = (data[body] << 24) | (data[body + 1] << 16) | (data[body + 2] << 8) | data[body + 3];
                    height = (data[body + 4] << 24) | (data[body + 5] << 16) | (data[body + 6] << 8) | data[body + 7];
                    int bitDepth = data[body + 8];
                    colorType = data[body + 9];
                    if (bitDepth != 8 || (colorType != 2 && colorType != 6) || data[body + 12] != 0)
                        throw new System.Exception($"PNG no admitido ({path}): usa RGB/RGBA de 8 bits sin entrelazar.");
                }
                else if (type == "IDAT") idat.Write(data, body, length);
                else if (type == "IEND") break;
                pos = body + length + 4; // + CRC
            }
            int bpp = colorType == 6 ? 4 : 3, stride = width * bpp;
            var raw = new byte[(stride + 1) * height];
            idat.Position = 2; // cabecera zlib
            using (var inflate = new System.IO.Compression.DeflateStream(idat, System.IO.Compression.CompressionMode.Decompress))
            {
                int read = 0;
                while (read < raw.Length)
                {
                    int n = inflate.Read(raw, read, raw.Length - read);
                    if (n <= 0) break;
                    read += n;
                }
            }
            var canvas = new PixelCanvas(width, height);
            var prev = new byte[stride];
            var line = new byte[stride];
            for (int row = 0; row < height; row++)
            {
                int filter = raw[row * (stride + 1)];
                System.Array.Copy(raw, row * (stride + 1) + 1, line, 0, stride);
                for (int i = 0; i < stride; i++)
                {
                    int a = i >= bpp ? line[i - bpp] : 0, b = prev[i], c = i >= bpp ? prev[i - bpp] : 0;
                    int add = filter == 1 ? a : filter == 2 ? b : filter == 3 ? (a + b) / 2 : filter == 4 ? Paeth(a, b, c) : 0;
                    line[i] = (byte)(line[i] + add);
                }
                int y = height - 1 - row; // la primera fila del PNG es la de arriba
                for (int x = 0; x < width; x++)
                {
                    int o = x * bpp;
                    canvas.Pixels[y * width + x] = new Color32(line[o], line[o + 1], line[o + 2], bpp == 4 ? line[o + 3] : (byte)255);
                }
                var t = prev; prev = line; line = t;
            }
            return canvas;
        }

        static int Paeth(int a, int b, int c)
        {
            int p = a + b - c, pa = System.Math.Abs(p - a), pb = System.Math.Abs(p - b), pc = System.Math.Abs(p - c);
            return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
        }

        public static PixelCanvas FromAscii(string art, Dictionary<char, Color32> palette, string glowChars = "", bool padding = true)
        {
            var rows = new List<string>();
            foreach (var line in art.Replace("\r", "").Split('\n'))
            {
                if (line.Trim().Length > 0) rows.Add(line.Trim());
            }
            int width = 0;
            foreach (var r in rows) width = Mathf.Max(width, r.Length);
            int pad = padding ? 1 : 0;
            var canvas = new PixelCanvas(width + pad * 2, rows.Count + pad * 2);
            for (int row = 0; row < rows.Count; row++)
            {
                for (int col = 0; col < rows[row].Length; col++)
                {
                    char ch = rows[row][col];
                    if (!palette.TryGetValue(ch, out var color)) continue;
                    canvas.Set(col + pad, canvas.Height - 1 - pad - row, color, glowChars.IndexOf(ch) >= 0);
                }
            }
            return canvas;
        }

        // ------------------------------------------------------------------
        // Ruido (para piedra, niebla, agua...)
        // ------------------------------------------------------------------

        public static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7fffffff) / (float)int.MaxValue;
            }
        }

        /// <summary>Ruido suave. Si periodX &gt; 0 se repite en horizontal (para texturas que se enlosan).</summary>
        public static float ValueNoise(float x, float y, int periodX, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float tx = x - x0, ty = y - y0;
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);
            int xa = x0, xb = x0 + 1;
            if (periodX > 0)
            {
                xa = ((xa % periodX) + periodX) % periodX;
                xb = ((xb % periodX) + periodX) % periodX;
            }
            float a = Hash(xa, y0, seed), b = Hash(xb, y0, seed);
            float c = Hash(xa, y0 + 1, seed), d = Hash(xb, y0 + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        public static float Fbm(float x, float y, int octaves, int periodX, int seed)
        {
            float sum = 0f, amplitude = 0.5f, norm = 0f;
            for (int o = 0; o < octaves; o++)
            {
                sum += ValueNoise(x, y, periodX, seed + o * 31) * amplitude;
                norm += amplitude;
                amplitude *= 0.5f;
                x *= 2f;
                y *= 2f;
                if (periodX > 0) periodX *= 2;
            }
            return sum / norm;
        }
    }
}
