using System.Collections.Generic;
using UnityEngine;

namespace Abismo.EditorTools
{
    /// <summary>Normal 3D sencilla (x a la derecha, y arriba, z hacia la cámara).</summary>
    public struct N3
    {
        public float x, y, z;

        public N3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static readonly N3 Front = new N3(0f, 0f, 1f);

        public N3 Normalized()
        {
            float l = Mathf.Sqrt(x * x + y * y + z * z);
            return l > 0.0001f ? new N3(x / l, y / l, z / l) : Front;
        }

        public float Dot(float ox, float oy, float oz) => x * ox + y * oy + z * oz;
    }

    /// <summary>
    /// Material de pixel art: una rampa de colores (oscuro→claro) y cómo reacciona a la luz.
    /// </summary>
    public sealed class PixelMaterial
    {
        public Color32[] Ramp;
        /// <summary>Luz mínima (0..1). Más alto = menos contraste.</summary>
        public float Ambient = 0.28f;
        /// <summary>Fuerza del contraluz (borde iluminado por detrás, típico de Blasphemous).</summary>
        public float Rim = 0.4f;
        /// <summary>Brillo especular para metal (latón, acero). 0 = mate.</summary>
        public float Gloss;
        /// <summary>Tramado (dithering) en el límite entre tonos. 0 = bandas limpias.</summary>
        public float Dither = 0.05f;
        /// <summary>Material que brilla por sí mismo (ojos, llamas, runas): ignora la iluminación.</summary>
        public bool Emissive;
        public int EmissiveIndex = -1;
        /// <summary>Textura procedural que suma/resta luz por píxel (piedra, tela...). Recibe coordenadas del lienzo.</summary>
        public System.Func<int, int, float> Texture;
        public bool Outline = true;
        /// <summary>Si es false, no aparece la línea oscura cuando otra pieza queda delante.</summary>
        public bool ReceivesContactShadow = true;

        public PixelMaterial(Color32[] ramp)
        {
            Ramp = ramp;
        }

        public static PixelMaterial From(string hex, int steps = 5, float hueShift = 0.08f) => new PixelMaterial(Abismo.EditorTools.Ramp.Make(hex, steps, hueShift));

        public static PixelMaterial Glow(params string[] hexes)
        {
            var ramp = new Color32[hexes.Length];
            for (int i = 0; i < hexes.Length; i++) ramp[i] = PixelCanvas.Hex(hexes[i]);
            return new PixelMaterial(ramp) { Emissive = true, Outline = false, ReceivesContactShadow = false };
        }

        public Color32 Lit(int index) => Ramp[Mathf.Clamp(index, 0, Ramp.Length - 1)];
    }

    /// <summary>Dirección de las luces con las que se "pinta" el sprite.</summary>
    public struct LightRig
    {
        public float KeyX, KeyY, KeyZ;   // luz principal (normalizada)
        public float RimX, RimY;         // dirección 2D del contraluz

        /// <summary>Luz desde arriba y por delante del personaje (que mira a la derecha) y contraluz desde atrás.</summary>
        public static LightRig Default
        {
            get
            {
                var key = new N3(0.42f, 0.7f, 0.58f).Normalized();
                return new LightRig { KeyX = key.x, KeyY = key.y, KeyZ = key.z, RimX = -0.92f, RimY = 0.38f };
            }
        }
    }

    /// <summary>
    /// "Escultor" de sprites 2.5D: se añaden piezas con volumen (cápsulas, elipses, polígonos biselados),
    /// cada una con su material y profundidad, y Render() las ilumina con bandas de color (cel shading),
    /// dibuja contornos selectivos y sombras de contacto, y genera también el normal map para las luces 2D.
    /// Es la misma idea que usó Dead Cells (modelos 3D renderizados como pixel art), pero en 2D y por código.
    ///
    /// Coordenadas: en píxeles, relativas al ORIGEN del lienzo (normalmente los pies), con Y hacia arriba.
    /// </summary>
    public sealed class ShadedCanvas
    {
        public readonly int Width;
        public readonly int Height;
        public readonly float OriginX;
        public readonly float OriginY;

        readonly List<Shape> shapes = new List<Shape>();
        readonly List<(int x, int y, Color32 c)> decals = new List<(int, int, Color32)>();
        int nextGroup = 1;

        static readonly float[,] Bayer =
        {
            { 0f / 16f, 8f / 16f, 2f / 16f, 10f / 16f },
            { 12f / 16f, 4f / 16f, 14f / 16f, 6f / 16f },
            { 3f / 16f, 11f / 16f, 1f / 16f, 9f / 16f },
            { 15f / 16f, 7f / 16f, 13f / 16f, 5f / 16f },
        };

        public ShadedCanvas(int width, int height, float originX, float originY)
        {
            Width = width;
            Height = height;
            OriginX = originX;
            OriginY = originY;
        }

        /// <summary>Cada pieza (brazo, cabeza...) debería tener su grupo para que las demás le proyecten sombra.</summary>
        public int NewGroup() => nextGroup++;

        // ------------------------------------------------------------------
        // Piezas
        // ------------------------------------------------------------------

        public abstract class Shape
        {
            public PixelMaterial Material;
            public float Z;
            public float Shade;
            public int Group;
            public float MinX, MinY, MaxX, MaxY;
            public abstract bool Sample(float px, float py, out N3 normal);
        }

        sealed class CapsuleShape : Shape
        {
            public float Ax, Ay, Bx, By, Ra, Rb, Flat;

            public override bool Sample(float px, float py, out N3 normal)
            {
                normal = N3.Front;
                float vx = Bx - Ax, vy = By - Ay;
                float len2 = vx * vx + vy * vy;
                float t = len2 > 0.0001f ? Mathf.Clamp01(((px - Ax) * vx + (py - Ay) * vy) / len2) : 0f;
                float cx = Ax + vx * t, cy = Ay + vy * t;
                float r = Ra + (Rb - Ra) * t;
                float dx = px - cx, dy = py - cy;
                float d2 = dx * dx + dy * dy;
                if (d2 > r * r || r <= 0f) return false;
                float nx = dx / r, ny = dy / r;
                float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - nx * nx - ny * ny));
                normal = new N3(nx * (1f - Flat), ny * (1f - Flat), nz + Flat).Normalized();
                return true;
            }
        }

        sealed class EllipseShape : Shape
        {
            public float Cx, Cy, Rx, Ry, Cos, Sin, Flat;

            public override bool Sample(float px, float py, out N3 normal)
            {
                normal = N3.Front;
                float dx = px - Cx, dy = py - Cy;
                float lx = (dx * Cos + dy * Sin) / Rx;
                float ly = (-dx * Sin + dy * Cos) / Ry;
                float d2 = lx * lx + ly * ly;
                if (d2 > 1f) return false;
                float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - d2));
                float wx = lx * Cos - ly * Sin, wy = lx * Sin + ly * Cos;
                normal = new N3(wx * (1f - Flat), wy * (1f - Flat), nz + Flat).Normalized();
                return true;
            }
        }

        sealed class PolyShape : Shape
        {
            public Vector2[] Points;
            public float Bevel;
            public float TiltX, TiltY; // inclinación de toda la cara (p. ej. una capa que se curva)

            public override bool Sample(float px, float py, out N3 normal)
            {
                normal = N3.Front;
                bool inside = false;
                var p = Points;
                for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
                {
                    if ((p[i].y > py) != (p[j].y > py) &&
                        px < (p[j].x - p[i].x) * (py - p[i].y) / (p[j].y - p[i].y) + p[i].x)
                    {
                        inside = !inside;
                    }
                }
                if (!inside) return false;

                float nx = TiltX, ny = TiltY;
                if (Bevel > 0f)
                {
                    float best = float.MaxValue, ox = 0f, oy = 0f;
                    for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
                    {
                        float ex = p[i].x - p[j].x, ey = p[i].y - p[j].y;
                        float len2 = ex * ex + ey * ey;
                        if (len2 < 0.0001f) continue;
                        float t = Mathf.Clamp01(((px - p[j].x) * ex + (py - p[j].y) * ey) / len2);
                        float qx = p[j].x + ex * t - px, qy = p[j].y + ey * t - py;
                        float d = Mathf.Sqrt(qx * qx + qy * qy);
                        if (d < best)
                        {
                            best = d;
                            ox = -qx;
                            oy = -qy;
                        }
                    }
                    if (best < Bevel && best > 0.0001f)
                    {
                        float k = 1f - best / Bevel;
                        nx -= ox / best * k * 0.85f;
                        ny -= oy / best * k * 0.85f;
                    }
                }
                normal = new N3(nx, ny, 1f).Normalized();
                return true;
            }
        }

        sealed class FuncShape : Shape
        {
            public System.Func<float, float, (bool inside, N3 normal)> Func;

            public override bool Sample(float px, float py, out N3 normal)
            {
                var r = Func(px, py);
                normal = r.normal;
                return r.inside;
            }
        }

        T Add<T>(T shape, PixelMaterial material, float z, float shade, int group) where T : Shape
        {
            shape.Material = material;
            shape.Z = z;
            shape.Shade = shade;
            shape.Group = group == 0 ? NewGroup() : group;
            shapes.Add(shape);
            return shape;
        }

        /// <summary>Cápsula (extremidad, cuello, tentáculo): de A a B con radios que pueden estrecharse.</summary>
        public Shape Capsule(Vector2 a, Vector2 b, float radiusA, float radiusB, PixelMaterial material, float z,
                             float shade = 0f, int group = 0, float flat = 0f)
        {
            float r = Mathf.Max(radiusA, radiusB) + 1f;
            var s = new CapsuleShape
            {
                Ax = a.x, Ay = a.y, Bx = b.x, By = b.y, Ra = radiusA, Rb = radiusB, Flat = flat,
                MinX = Mathf.Min(a.x, b.x) - r, MaxX = Mathf.Max(a.x, b.x) + r,
                MinY = Mathf.Min(a.y, b.y) - r, MaxY = Mathf.Max(a.y, b.y) + r,
            };
            return Add(s, material, z, shade, group);
        }

        /// <summary>Elipse con volumen (cabeza, torso, vientre...). angle en grados.</summary>
        public Shape Ellipse(Vector2 center, float rx, float ry, float angle, PixelMaterial material, float z,
                             float shade = 0f, int group = 0, float flat = 0f)
        {
            float rad = angle * Mathf.Deg2Rad;
            float r = Mathf.Max(rx, ry) + 1f;
            var s = new EllipseShape
            {
                Cx = center.x, Cy = center.y, Rx = Mathf.Max(0.5f, rx), Ry = Mathf.Max(0.5f, ry),
                Cos = Mathf.Cos(rad), Sin = Mathf.Sin(rad), Flat = flat,
                MinX = center.x - r, MaxX = center.x + r, MinY = center.y - r, MaxY = center.y + r,
            };
            return Add(s, material, z, shade, group);
        }

        /// <summary>Polígono plano con bisel en los bordes (armadura, capa, piedra...).</summary>
        public Shape Poly(Vector2[] points, PixelMaterial material, float z, float bevel = 2f,
                          float shade = 0f, int group = 0, float tiltX = 0f, float tiltY = 0f)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var p in points)
            {
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }
            var s = new PolyShape
            {
                Points = (Vector2[])points.Clone(), Bevel = bevel, TiltX = tiltX, TiltY = tiltY,
                MinX = minX - 1f, MaxX = maxX + 1f, MinY = minY - 1f, MaxY = maxY + 1f,
            };
            return Add(s, material, z, shade, group);
        }

        /// <summary>Hebra de cápsulas encadenadas (tentáculos, pelo, cadenas, tela fina).</summary>
        public void Strand(IList<Vector2> points, float radiusStart, float radiusEnd, PixelMaterial material, float z,
                           float shade = 0f, int group = 0)
        {
            if (group == 0) group = NewGroup();
            int segments = points.Count - 1;
            for (int i = 0; i < segments; i++)
            {
                float r0 = Mathf.Lerp(radiusStart, radiusEnd, i / (float)segments);
                float r1 = Mathf.Lerp(radiusStart, radiusEnd, (i + 1) / (float)segments);
                Capsule(points[i], points[i + 1], r0, r1, material, z, shade, group);
            }
        }

        /// <summary>Forma arbitraria definida por una función (estelas de espada, efectos...).</summary>
        public Shape Custom(float minX, float minY, float maxX, float maxY, System.Func<float, float, (bool, N3)> func,
                            PixelMaterial material, float z, float shade = 0f, int group = 0)
        {
            var s = new FuncShape { Func = func, MinX = minX, MinY = minY, MaxX = maxX, MaxY = maxY };
            return Add(s, material, z, shade, group);
        }

        /// <summary>Píxel suelto pintado DESPUÉS de iluminar (brillo en un ojo, chispa...). Coordenadas relativas al origen.</summary>
        public void Decal(float x, float y, Color32 color)
        {
            int px = Mathf.FloorToInt(x + OriginX), py = Mathf.FloorToInt(y + OriginY);
            decals.Add((px, py, color));
        }

        // ------------------------------------------------------------------
        // Render
        // ------------------------------------------------------------------

        public PixelCanvas Render(out PixelCanvas normalMap) => Render(LightRig.Default, out normalMap, out _);

        public PixelCanvas Render(LightRig light, out PixelCanvas normalMap) => Render(light, out normalMap, out _);

        /// <summary>
        /// Igual que <see cref="Render(out PixelCanvas)"/>, y además devuelve <paramref name="emission"/>: solo los píxeles
        /// de materiales emisivos (ojos, brasas, runas). El juego los dibuja encima sin iluminar para que brillen
        /// aunque la escena esté a oscuras (y el bloom los realce).
        /// </summary>
        public PixelCanvas Render(out PixelCanvas normalMap, out PixelCanvas emission) => Render(LightRig.Default, out normalMap, out emission);

        public PixelCanvas Render(LightRig light, out PixelCanvas normalMap, out PixelCanvas emission)
        {
            int n = Width * Height;
            var top = new Shape[n];
            var normals = new N3[n];
            var zbuf = new float[n];
            for (int i = 0; i < n; i++) zbuf[i] = float.MinValue;

            // 1) Rasterizar: gana la pieza con mayor Z (a igual Z, la añadida después).
            foreach (var s in shapes)
            {
                int x0 = Mathf.Max(0, Mathf.FloorToInt(s.MinX + OriginX)), x1 = Mathf.Min(Width - 1, Mathf.CeilToInt(s.MaxX + OriginX));
                int y0 = Mathf.Max(0, Mathf.FloorToInt(s.MinY + OriginY)), y1 = Mathf.Min(Height - 1, Mathf.CeilToInt(s.MaxY + OriginY));
                for (int y = y0; y <= y1; y++)
                {
                    float py = y + 0.5f - OriginY;
                    for (int x = x0; x <= x1; x++)
                    {
                        int i = y * Width + x;
                        if (s.Z < zbuf[i]) continue;
                        if (!s.Sample(x + 0.5f - OriginX, py, out var normal)) continue;
                        top[i] = s;
                        normals[i] = normal;
                        zbuf[i] = s.Z;
                    }
                }
            }

            // 2) Iluminación cuantizada en bandas.
            var index = new int[n];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int i = y * Width + x;
                    var s = top[i];
                    if (s == null) continue;
                    var m = s.Material;
                    int steps = m.Ramp.Length;
                    if (m.Emissive)
                    {
                        index[i] = m.EmissiveIndex < 0 ? steps - 1 : m.EmissiveIndex;
                        continue;
                    }
                    var nn = normals[i];
                    float diffuse = Mathf.Max(0f, nn.Dot(light.KeyX, light.KeyY, light.KeyZ));
                    float l = m.Ambient + (1f - m.Ambient) * diffuse;
                    float rimDot = nn.x * light.RimX + nn.y * light.RimY;
                    if (rimDot > 0f) l += Mathf.Pow(1f - nn.z, 2f) * rimDot * m.Rim;
                    if (m.Gloss > 0f)
                    {
                        // Medio vector entre la luz y la cámara.
                        var h = new N3(light.KeyX, light.KeyY, light.KeyZ + 1f).Normalized();
                        l += Mathf.Pow(Mathf.Max(0f, nn.Dot(h.x, h.y, h.z)), 18f) * m.Gloss;
                    }
                    l += s.Shade;
                    if (m.Texture != null) l += m.Texture(x, y);
                    l += (Bayer[y & 3, x & 3] - 0.5f) * m.Dither;
                    index[i] = Mathf.Clamp(Mathf.FloorToInt(l * steps), 0, steps - 1);
                }
            }

            // 3) Sombras de contacto: lo que queda detrás de otra pieza se oscurece en el borde.
            var shaded = (int[])index.Clone();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int i = y * Width + x;
                    var s = top[i];
                    if (s == null || s.Material.Emissive || !s.Material.ReceivesContactShadow) continue;
                    int darken = 0;
                    for (int k = 0; k < 4; k++)
                    {
                        int nx = x + (k == 0 ? 1 : k == 1 ? -1 : 0), ny = y + (k == 2 ? 1 : k == 3 ? -1 : 0);
                        if (nx < 0 || ny < 0 || nx >= Width || ny >= Height) continue;
                        var o = top[ny * Width + nx];
                        if (o != null && o.Group != s.Group && o.Z > s.Z + 0.001f && !o.Material.Emissive) darken = 2;
                    }
                    if (darken == 0 && y + 2 < Height)
                    {
                        // Sombra suave proyectada desde arriba (la luz viene de arriba).
                        var o = top[(y + 2) * Width + x];
                        if (o != null && o.Group != s.Group && o.Z > s.Z + 0.001f && !o.Material.Emissive) darken = 1;
                    }
                    shaded[i] = Mathf.Max(0, index[i] - darken);
                }
            }

            // 4) Color + normal map.
            var color = new PixelCanvas(Width, Height);
            normalMap = new PixelCanvas(Width, Height);
            emission = new PixelCanvas(Width, Height);
            for (int i = 0; i < n; i++)
            {
                var s = top[i];
                if (s == null) continue;
                color.Pixels[i] = s.Material.Lit(shaded[i]);
                if (s.Material.Emissive) emission.Pixels[i] = color.Pixels[i];
                var nn = s.Material.Emissive ? N3.Front : normals[i];
                normalMap.Pixels[i] = EncodeNormal(nn);
            }

            // 5) Contorno selectivo: oscuro en el lado de sombra, más suave en el lado iluminado.
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int i = y * Width + x;
                    if (top[i] != null) continue;
                    Shape best = null;
                    int bestIndex = 0;
                    for (int k = 0; k < 4; k++)
                    {
                        int nx = x + (k == 0 ? 1 : k == 1 ? -1 : 0), ny = y + (k == 2 ? 1 : k == 3 ? -1 : 0);
                        if (nx < 0 || ny < 0 || nx >= Width || ny >= Height) continue;
                        var o = top[ny * Width + nx];
                        if (o == null || !o.Material.Outline) continue;
                        if (best == null || o.Z > best.Z)
                        {
                            best = o;
                            bestIndex = shaded[ny * Width + nx];
                        }
                    }
                    if (best == null) continue;
                    var ramp = best.Material.Ramp;
                    bool litSide = bestIndex >= ramp.Length - 2;
                    color.Pixels[i] = Ramp.Shadow(ramp[0], litSide ? 0.35f : 0.62f);
                    normalMap.Pixels[i] = EncodeNormal(N3.Front);
                }
            }

            foreach (var d in decals)
            {
                if (d.x < 0 || d.y < 0 || d.x >= Width || d.y >= Height) continue;
                color.Pixels[d.y * Width + d.x] = d.c;
                normalMap.Pixels[d.y * Width + d.x] = EncodeNormal(N3.Front);
            }
            return color;
        }

        static Color32 EncodeNormal(N3 n) => new Color32(
            (byte)Mathf.Clamp(Mathf.RoundToInt((n.x * 0.5f + 0.5f) * 255f), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt((n.y * 0.5f + 0.5f) * 255f), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt((n.z * 0.5f + 0.5f) * 255f), 0, 255),
            255);
    }
}
