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

        // ---- Opciones de detalle (v4). Todas desactivadas por defecto: sin tocarlas, el resultado es idéntico. ----

        /// <summary>
        /// Relieve del material (escamas, remaches, tejido, vetas...): altura en píxeles en las coordenadas LOCALES de
        /// cada pieza (ver <see cref="ShadedCanvas.Shape.Frame"/>), así el dibujo viaja pegado a la pieza al animar.
        /// Inclina la normal (luz arriba-izquierda de cada bulto y sombra abajo-derecha, también en el normal map).
        /// Se usa en las piezas que no tengan su propio <see cref="ShadedCanvas.Shape.Bump"/>. Ver <see cref="Patterns"/>.
        /// </summary>
        public System.Func<float, float, float> Bump;
        /// <summary>Fuerza del relieve del material (1 = una pendiente de 1 px de altura por píxel ≈ 45°).</summary>
        public float BumpStrength = 1f;
        /// <summary>
        /// Dibujo "pintado" del material en coordenadas locales de la pieza: desplaza la banda de color
        /// (+1 = un tono más claro, −1 = uno más oscuro; por debajo del más oscuro sale un tono "hondo", nunca negro).
        /// Para costuras, rayas, manchas... Se aplica después de la luz y de las sombras de contacto.
        /// </summary>
        public System.Func<float, float, int> Detail;
        /// <summary>
        /// Transición entre bandas tramada de 1 px: en la franja (fracción de banda, p. ej. 0.18) justo encima de cada
        /// límite, los píxeles alternos en damero bajan un tono. 0 = desactivado (bandas limpias).
        /// </summary>
        public float BandDither;
        /// <summary>
        /// Brillo especular puntual (metal, cristal, superficies húmedas): donde el término especular
        /// pow(N·H, <see cref="SpecularPower"/>) supera este umbral (0..1, p. ej. 0.6) el píxel toma el color
        /// <see cref="Specular"/>, más claro que la rampa. 0 = desactivado.
        /// </summary>
        public float SpecularAt;
        public float SpecularPower = 24f;
        /// <summary>Color del brillo especular y de los tonos "por encima" de la rampa. Alfa 0 = el tono más claro de la rampa aclarado hacia blanco.</summary>
        public Color32 Specular;
        /// <summary>Antialias manual de la silueta: las esquinas salientes de los escalones se oscurecen un tono.</summary>
        public bool SoftEdges;

        public PixelMaterial(Color32[] ramp)
        {
            Ramp = ramp;
        }

        /// <summary>Copia superficial (misma rampa) para variar opciones sin afectar a quien comparte el material.</summary>
        public PixelMaterial Clone() => (PixelMaterial)MemberwiseClone();

        /// <summary>
        /// Tono para un índice de banda extendido: dentro de la rampa, el de la rampa; por debajo (−1), un tono "hondo"
        /// más oscuro que la sombra pero frío y nunca negro; por encima (≥ número de tonos), el color especular.
        /// </summary>
        public Color32 Tone(int index)
        {
            if (index < 0) return Abismo.EditorTools.Ramp.Shadow(Ramp[0], 0.3f);
            if (index >= Ramp.Length)
            {
                if (Specular.a > 0) return Specular;
                var top = Ramp[Ramp.Length - 1];
                return PixelCanvas.Lerp(top, new Color32(255, 250, 236, top.a), 0.55f);
            }
            return Ramp[index];
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

            // ---- Opciones de detalle por pieza (v4). Desactivadas por defecto. ----

            /// <summary>
            /// Marco local de la pieza para <see cref="Bump"/>, <see cref="Detail"/> y <see cref="TintMask"/>: origen y
            /// ángulo (u = a lo largo, v = a través). Capsule lo pone en A mirando hacia B; Ellipse en el centro con su
            /// ángulo; Poly/Custom en el origen del lienzo sin girar (cámbialo con <see cref="Frame"/>).
            /// </summary>
            public float FrameX, FrameY, FrameCos = 1f, FrameSin;
            /// <summary>Relieve propio de la pieza (altura en px en coordenadas locales u, v). Ver <see cref="Patterns"/>.</summary>
            public System.Func<float, float, float> Bump;
            public float BumpStrength = 1f;
            /// <summary>Dibujo propio de la pieza (desplazamiento de banda en coordenadas locales). Ver <see cref="PixelMaterial.Detail"/>.</summary>
            public System.Func<float, float, int> Detail;
            /// <summary>Acento de color por pieza: mezcla el color final con <see cref="Tint"/> en esta proporción (0 = nada).</summary>
            public Color32 Tint;
            public float TintAmount;
            /// <summary>Máscara 0..1 del tinte en coordenadas locales (manchas de verdín, óxido, sangre...). null = uniforme.</summary>
            public System.Func<float, float, float> TintMask;
            /// <summary>Antialias manual de la silueta solo en esta pieza (ver <see cref="PixelMaterial.SoftEdges"/>).</summary>
            public bool SoftEdges;

            /// <summary>Coloca el marco local: origen (relativo al origen del lienzo) y ángulo en grados (0 = u hacia la derecha).</summary>
            public Shape Frame(Vector2 origin, float angle)
            {
                float a = angle * Mathf.Deg2Rad;
                FrameX = origin.x;
                FrameY = origin.y;
                FrameCos = Mathf.Cos(a);
                FrameSin = Mathf.Sin(a);
                return this;
            }

            public Shape WithBump(System.Func<float, float, float> height, float strength = 1f)
            {
                Bump = height;
                BumpStrength = strength;
                return this;
            }

            public Shape WithDetail(System.Func<float, float, int> detail)
            {
                Detail = detail;
                return this;
            }

            public Shape WithTint(Color32 color, float amount, System.Func<float, float, float> mask = null)
            {
                Tint = color;
                TintAmount = amount;
                TintMask = mask;
                return this;
            }

            public Shape Soft(bool on = true)
            {
                SoftEdges = on;
                return this;
            }

            /// <summary>Coordenadas locales (u, v) de un punto relativo al origen del lienzo.</summary>
            public void ToLocal(float px, float py, out float u, out float v)
            {
                float dx = px - FrameX, dy = py - FrameY;
                u = dx * FrameCos + dy * FrameSin;
                v = -dx * FrameSin + dy * FrameCos;
            }

            internal bool HasDetailOptions =>
                Bump != null || Detail != null || TintAmount > 0f || SoftEdges ||
                Material.Bump != null || Material.Detail != null || Material.SoftEdges;
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
            // Marco local para el detalle opcional: u a lo largo de A→B, v a través (no afecta si no se usa).
            float len = Mathf.Sqrt((b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y));
            s.FrameX = a.x;
            s.FrameY = a.y;
            if (len > 0.0001f)
            {
                s.FrameCos = (b.x - a.x) / len;
                s.FrameSin = (b.y - a.y) / len;
            }
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
            s.FrameX = center.x;
            s.FrameY = center.y;
            s.FrameCos = s.Cos;
            s.FrameSin = s.Sin;
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
        // Marcas de detalle (v4): líneas interiores y brillos que se pintan SOBRE las piezas ya iluminadas,
        // en la rampa del material que haya debajo (nunca fuera de la silueta, nunca negro puro).
        // ------------------------------------------------------------------

        struct MarkOp
        {
            public int X, Y, Delta, Group;
            public PixelMaterial Only;
            public bool Hot;
            public float MinZ;
        }

        readonly List<MarkOp> marks = new List<MarkOp>();
        const int TopTone = 1000; // marca que fuerza el tono más claro de la rampa

        /// <summary>
        /// Línea de 1 px (polilínea "pixel-perfect", sin escalones dobles) que desplaza la banda de color de lo que haya
        /// debajo: <paramref name="delta"/> −2 = pliegue/junta/grieta (más oscuro que la sombra del material), +1 = arista
        /// iluminada. Solo pinta sobre píxeles cubiertos; con <paramref name="group"/> ≠ 0 solo sobre esa pieza, con
        /// <paramref name="only"/> solo sobre ese material y con <paramref name="minZ"/> solo sobre piezas con Z ≥ minZ.
        /// <paramref name="dash"/>/<paramref name="gap"/> &gt; 0 = trazo discontinuo (costuras, pespuntes).
        /// Coordenadas relativas al origen del lienzo, como las piezas.
        /// </summary>
        public void Mark(IList<Vector2> points, int delta, int group = 0, PixelMaterial only = null, int dash = 0, int gap = 0,
                         float minZ = float.MinValue)
        {
            var pixels = Rasterize(points);
            for (int k = 0; k < pixels.Count; k++)
            {
                if (dash > 0 && gap > 0 && k % (dash + gap) >= dash) continue;
                marks.Add(new MarkOp { X = pixels[k].x, Y = pixels[k].y, Delta = delta, Group = group, Only = only, MinZ = minZ });
            }
        }

        public void Mark(Vector2 a, Vector2 b, int delta, int group = 0, PixelMaterial only = null, int dash = 0, int gap = 0,
                         float minZ = float.MinValue) =>
            Mark(new[] { a, b }, delta, group, only, dash, gap, minZ);

        /// <summary>Un solo píxel que cambia de banda (agujero de remache, poro, gota...).</summary>
        public void Dot(Vector2 at, int delta, int group = 0, PixelMaterial only = null, float minZ = float.MinValue) =>
            marks.Add(new MarkOp { X = Mathf.FloorToInt(at.x + OriginX), Y = Mathf.FloorToInt(at.y + OriginY), Delta = delta, Group = group, Only = only, MinZ = minZ });

        /// <summary>Línea interior oscura (pliegue, junta, costura, grieta): por defecto 2 tonos por debajo.</summary>
        public void Crease(IList<Vector2> points, int darken = 2, int group = 0, PixelMaterial only = null) =>
            Mark(points, -Mathf.Abs(darken), group, only);

        public void Crease(Vector2 a, Vector2 b, int darken = 2, int group = 0, PixelMaterial only = null) =>
            Mark(new[] { a, b }, -Mathf.Abs(darken), group, only);

        /// <summary>Arista iluminada de 1 px (filo, canto de placa, cresta de un pliegue): por defecto 1 tono por encima.</summary>
        public void Ridge(IList<Vector2> points, int lighten = 1, int group = 0, PixelMaterial only = null) =>
            Mark(points, Mathf.Abs(lighten), group, only);

        /// <summary>
        /// Pliegue de tela completo: surco oscuro en la línea y, pegado a él por el lado que mira a la luz
        /// (arriba-delante), una cresta 1 tono más clara.
        /// </summary>
        public void Fold(IList<Vector2> points, int group = 0, int darken = 2, int lighten = 1, PixelMaterial only = null)
        {
            Mark(points, -Mathf.Abs(darken), group, only);
            if (lighten == 0 || points.Count < 2) return;
            var lit = new Vector2[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                var a = points[Mathf.Max(0, i - 1)];
                var b = points[Mathf.Min(points.Count - 1, i + 1)];
                float dx = b.x - a.x, dy = b.y - a.y, l = Mathf.Sqrt(dx * dx + dy * dy);
                float nx = l > 0.0001f ? -dy / l : 0f, ny = l > 0.0001f ? dx / l : 1f;
                if (nx * 0.42f + ny * 0.7f < 0f) { nx = -nx; ny = -ny; }
                lit[i] = new Vector2(points[i].x + nx, points[i].y + ny);
            }
            // La cresta no pisa el surco.
            var crease = new HashSet<int>();
            foreach (var p in Rasterize(points)) crease.Add(p.y * 65536 + p.x);
            foreach (var p in Rasterize(lit))
            {
                if (crease.Contains(p.y * 65536 + p.x)) continue;
                marks.Add(new MarkOp { X = p.x, Y = p.y, Delta = Mathf.Abs(lighten), Group = group, Only = only, MinZ = float.MinValue });
            }
        }

        /// <summary>Pespunte: trazos cortos claros alternos (costuras, cordones, vendas).</summary>
        public void Stitch(IList<Vector2> points, int group = 0, int dash = 1, int gap = 1, int delta = 1, PixelMaterial only = null) =>
            Mark(points, delta, group, only, dash, gap);

        /// <summary>
        /// Brillo especular puntual (metal, cristal, ojo húmedo): el píxel toma el color especular del material.
        /// <paramref name="arm"/> &gt; 0 añade una cruz de ese radio con el tono más claro de la rampa (destello).
        /// </summary>
        public void Glint(Vector2 at, int group = 0, int arm = 0, PixelMaterial only = null)
        {
            int x = Mathf.FloorToInt(at.x + OriginX), y = Mathf.FloorToInt(at.y + OriginY);
            for (int r = 1; r <= arm; r++)
            {
                marks.Add(new MarkOp { X = x + r, Y = y, Delta = TopTone, Group = group, Only = only, MinZ = float.MinValue });
                marks.Add(new MarkOp { X = x - r, Y = y, Delta = TopTone, Group = group, Only = only, MinZ = float.MinValue });
                marks.Add(new MarkOp { X = x, Y = y + r, Delta = TopTone, Group = group, Only = only, MinZ = float.MinValue });
                marks.Add(new MarkOp { X = x, Y = y - r, Delta = TopTone, Group = group, Only = only, MinZ = float.MinValue });
            }
            marks.Add(new MarkOp { X = x, Y = y, Hot = true, Group = group, Only = only, MinZ = float.MinValue });
        }

        /// <summary>Píxeles de una polilínea (coordenadas de lienzo), sin repetir y sin esquinas en L ("pixel-perfect").</summary>
        List<(int x, int y)> Rasterize(IList<Vector2> points)
        {
            var raw = new List<(int x, int y)>();
            if (points == null || points.Count == 0) return raw;
            for (int s = 0; s < Mathf.Max(1, points.Count - 1); s++)
            {
                var a = points[s];
                var b = points.Count > 1 ? points[s + 1] : a;
                int x0 = Mathf.FloorToInt(a.x + OriginX), y0 = Mathf.FloorToInt(a.y + OriginY);
                int x1 = Mathf.FloorToInt(b.x + OriginX), y1 = Mathf.FloorToInt(b.y + OriginY);
                int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0);
                int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
                while (true)
                {
                    if (raw.Count == 0 || raw[raw.Count - 1].x != x0 || raw[raw.Count - 1].y != y0) raw.Add((x0, y0));
                    if (x0 == x1 && y0 == y1) break;
                    int e2 = 2 * err;
                    if (e2 >= dy) { err += dy; x0 += sx; }
                    if (e2 <= dx) { err += dx; y0 += sy; }
                }
            }
            // Quita el píxel central de cada esquina en L (línea más limpia, como la dibujaría un pixel artist).
            var clean = new List<(int x, int y)>();
            for (int i = 0; i < raw.Count; i++)
            {
                if (clean.Count > 0 && i + 1 < raw.Count)
                {
                    var p = clean[clean.Count - 1];
                    var n = raw[i + 1];
                    if (Mathf.Abs(p.x - n.x) == 1 && Mathf.Abs(p.y - n.y) == 1 &&
                        (raw[i].x == p.x || raw[i].y == p.y)) continue;
                }
                clean.Add(raw[i]);
            }
            return clean;
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

            // ¿Hay algo de las opciones de detalle v4? Si no, todo sigue exactamente el camino de siempre.
            bool detail = marks.Count > 0;
            foreach (var s in shapes)
            {
                if (detail) break;
                var m = s.Material;
                detail = s.HasDetailOptions || m.BandDither > 0f || m.SpecularAt > 0f;
            }

            // 1b) (opcional) Relieve: la altura local de la pieza/material inclina la normal según su gradiente.
            if (detail) ApplyBump(top, normals);

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
                    int band = Mathf.Clamp(Mathf.FloorToInt(l * steps), 0, steps - 1);
                    if (m.BandDither > 0f)
                    {
                        // Transición tramada de 1 px: justo encima de cada límite, damero con el tono de abajo.
                        float q = l * steps;
                        if (q >= 1f && q < steps && q - Mathf.Floor(q) < m.BandDither && ((x + y) & 1) == 0) band = Mathf.Max(0, band - 1);
                    }
                    if (m.SpecularAt > 0f)
                    {
                        var h = new N3(light.KeyX, light.KeyY, light.KeyZ + 1f).Normalized();
                        if (Mathf.Pow(Mathf.Max(0f, nn.Dot(h.x, h.y, h.z)), m.SpecularPower) >= m.SpecularAt) band = steps;
                    }
                    index[i] = band;
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

            // 3b) (opcional) Dibujo por pieza/material, marcas (pliegues, costuras, brillos) y antialias de silueta.
            if (detail) ApplyMarks(top, zbuf, shaded);

            // 4) Color + normal map.
            var color = new PixelCanvas(Width, Height);
            normalMap = new PixelCanvas(Width, Height);
            emission = new PixelCanvas(Width, Height);
            for (int i = 0; i < n; i++)
            {
                var s = top[i];
                if (s == null) continue;
                color.Pixels[i] = detail && !s.Material.Emissive ? s.Material.Tone(shaded[i]) : s.Material.Lit(shaded[i]);
                if (detail && s.TintAmount > 0f)
                {
                    // Acento de color por pieza (opcional), con máscara en coordenadas locales.
                    float amount = s.TintAmount;
                    if (s.TintMask != null)
                    {
                        s.ToLocal(i % Width + 0.5f - OriginX, i / Width + 0.5f - OriginY, out float u, out float v);
                        amount *= Mathf.Clamp01(s.TintMask(u, v));
                    }
                    var c0 = color.Pixels[i];
                    var tinted = PixelCanvas.Lerp(c0, s.Tint, amount);
                    color.Pixels[i] = new Color32(tinted.r, tinted.g, tinted.b, c0.a);
                }
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

        // ------------------------------------------------------------------
        // Pasadas opcionales de detalle (v4)
        // ------------------------------------------------------------------

        void ApplyBump(Shape[] top, N3[] normals)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int i = y * Width + x;
                    var s = top[i];
                    if (s == null || s.Material.Emissive) continue;
                    var bump = s.Bump ?? s.Material.Bump;
                    if (bump == null) continue;
                    float strength = s.Bump != null ? s.BumpStrength : s.Material.BumpStrength;
                    s.ToLocal(x + 0.5f - OriginX, y + 0.5f - OriginY, out float u, out float v);
                    // Gradiente local por diferencias centrales, girado al marco del lienzo.
                    float gu = bump(u + 0.5f, v) - bump(u - 0.5f, v);
                    float gv = bump(u, v + 0.5f) - bump(u, v - 0.5f);
                    float gx = gu * s.FrameCos - gv * s.FrameSin;
                    float gy = gu * s.FrameSin + gv * s.FrameCos;
                    var nn = normals[i];
                    normals[i] = new N3(nn.x - gx * strength, nn.y - gy * strength, nn.z).Normalized();
                }
            }
        }

        static int MaxTone(PixelMaterial m) =>
            m.Gloss > 0f || m.SpecularAt > 0f || m.Specular.a > 0 ? m.Ramp.Length : m.Ramp.Length - 1;

        void ApplyMarks(Shape[] top, float[] zbuf, int[] shaded)
        {
            // Dibujo propio de la pieza o del material.
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int i = y * Width + x;
                    var s = top[i];
                    if (s == null || s.Material.Emissive) continue;
                    var d = s.Detail ?? s.Material.Detail;
                    if (d == null) continue;
                    s.ToLocal(x + 0.5f - OriginX, y + 0.5f - OriginY, out float u, out float v);
                    int delta = d(u, v);
                    if (delta != 0) shaded[i] = Mathf.Clamp(shaded[i] + delta, -1, MaxTone(s.Material));
                }
            }

            // Marcas sueltas (pliegues, aristas, costuras, brillos), en el orden en que se añadieron.
            foreach (var op in marks)
            {
                if (op.X < 0 || op.Y < 0 || op.X >= Width || op.Y >= Height) continue;
                int i = op.Y * Width + op.X;
                var s = top[i];
                if (s == null || s.Material.Emissive) continue;
                if (op.Group != 0 && s.Group != op.Group) continue;
                if (op.Only != null && s.Material != op.Only) continue;
                if (zbuf[i] < op.MinZ) continue;
                int steps = s.Material.Ramp.Length;
                if (op.Hot) shaded[i] = steps;
                else if (op.Delta == TopTone) shaded[i] = Mathf.Max(shaded[i], steps - 1);
                else shaded[i] = Mathf.Clamp(shaded[i] + op.Delta, -1, MaxTone(s.Material));
            }

            // Antialias manual: las esquinas salientes de la silueta (5 o más vecinos vacíos de 8) bajan un tono.
            var soft = new List<int>();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int i = y * Width + x;
                    var s = top[i];
                    if (s == null || s.Material.Emissive || !(s.SoftEdges || s.Material.SoftEdges)) continue;
                    int empty = 0;
                    for (int oy = -1; oy <= 1; oy++)
                    {
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            if (ox == 0 && oy == 0) continue;
                            int nx = x + ox, ny = y + oy;
                            if (nx < 0 || ny < 0 || nx >= Width || ny >= Height || top[ny * Width + nx] == null) empty++;
                        }
                    }
                    if (empty >= 5 && empty <= 6) soft.Add(i);
                }
            }
            foreach (int i in soft) shaded[i] = Mathf.Max(-1, Mathf.Min(shaded[i], top[i].Material.Ramp.Length - 1) - 1);
        }

        static Color32 EncodeNormal(N3 n) => new Color32(
            (byte)Mathf.Clamp(Mathf.RoundToInt((n.x * 0.5f + 0.5f) * 255f), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt((n.y * 0.5f + 0.5f) * 255f), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt((n.z * 0.5f + 0.5f) * 255f), 0, 255),
            255);
    }

    /// <summary>
    /// Patrones de relieve y dibujo listos para <see cref="ShadedCanvas.Shape.WithBump"/>, <see cref="PixelMaterial.Bump"/>,
    /// <see cref="ShadedCanvas.Shape.WithDetail"/> y <see cref="PixelMaterial.Detail"/> (v4). Todos reciben coordenadas
    /// LOCALES de la pieza (u a lo largo, v a través, en píxeles) y son deterministas. Las alturas van en píxeles:
    /// el relieve ilumina el lado de arriba-delante de cada bulto y sombrea el contrario, como un bisel pintado a mano.
    /// </summary>
    public static class Patterns
    {
        /// <summary>Remaches en fila a lo largo de u (en v = <paramref name="row"/>), cada <paramref name="spacing"/> px.</summary>
        public static System.Func<float, float, float> Rivets(float spacing, float radius = 1.2f, float row = 0f, float offset = 0f) => (u, v) =>
        {
            float cu = Mathf.Repeat(u - offset + spacing * 0.5f, spacing) - spacing * 0.5f, cv = v - row;
            float d2 = radius * radius - cu * cu - cv * cv;
            return d2 > 0f ? Mathf.Sqrt(d2) : 0f;
        };

        /// <summary>Crestas paralelas perpendiculares a u (anillas de manguera, costillas, pliegues de fuelle).</summary>
        public static System.Func<float, float, float> Ridges(float period, float height = 0.8f) => (u, v) =>
        {
            float t = Mathf.Repeat(u, period) / period;
            return (1f - Mathf.Abs(t * 2f - 1f)) * height;
        };

        /// <summary>Escamas imbricadas (filas desplazadas, cada escama abombada y cayendo hacia abajo en v).</summary>
        public static System.Func<float, float, float> Scales(float width, float height, float depth = 1f) => (u, v) =>
        {
            float row = Mathf.Floor(v / height);
            float shift = (Mathf.Repeat(row, 2f) > 0.5f) ? width * 0.5f : 0f;
            float cu = Mathf.Repeat(u + shift, width) / width - 0.5f;
            float cv = Mathf.Repeat(v, height) / height;
            // Abombada en el centro, con el borde inferior (cv→0) más alto: el canto de la escama.
            return Mathf.Max(0f, 1f - cu * cu * 4f) * (0.35f + 0.65f * (1f - cv)) * depth;
        };

        /// <summary>Tejido: damero de hilos (urdimbre/trama) de <paramref name="period"/> px; sutil a 2–3 px.</summary>
        public static System.Func<float, float, float> Weave(float period = 3f, float height = 0.5f) => (u, v) =>
        {
            float a = Mathf.Repeat(u, period * 2f) < period ? 1f : 0f;
            float b = Mathf.Repeat(v, period * 2f) < period ? 1f : 0f;
            float t = Mathf.Repeat(a + b, 2f) > 0.5f ? Mathf.Repeat(v, period) / period : Mathf.Repeat(u, period) / period;
            return Mathf.Sin(t * Mathf.PI) * height;
        };

        /// <summary>Vetas de madera / cuero curtido a lo largo de u, onduladas con ruido.</summary>
        public static System.Func<float, float, float> Grain(float spacing = 3f, float height = 0.7f, int seed = 1) => (u, v) =>
        {
            float w = v + (PixelCanvas.ValueNoise(u / 9f, v / 5f, 0, seed) - 0.5f) * spacing * 1.6f;
            float t = Mathf.Repeat(w, spacing) / spacing;
            return (1f - Mathf.Abs(t * 2f - 1f)) * height;
        };

        /// <summary>Placas/sillares en rejilla con juntas biseladas (filas alternas desplazadas si <paramref name="stagger"/>).</summary>
        public static System.Func<float, float, float> Plates(float width, float height, float bevel = 1.5f, bool stagger = true) => (u, v) =>
        {
            float row = Mathf.Floor(v / height);
            float shift = stagger && Mathf.Repeat(row, 2f) > 0.5f ? width * 0.5f : 0f;
            float du = Mathf.Repeat(u + shift, width), dv = Mathf.Repeat(v, height);
            float e = Mathf.Min(Mathf.Min(du, width - du), Mathf.Min(dv, height - dv));
            return Mathf.Clamp01(e / Mathf.Max(0.01f, bevel));
        };

        /// <summary>Abolladuras/arrugas suaves en grupos (cuero gastado, metal batido, piel).</summary>
        public static System.Func<float, float, float> Lumps(float scale = 4f, float height = 1f, int seed = 7) => (u, v) =>
            PixelCanvas.ValueNoise(u / scale, v / scale, 0, seed) * height;

        /// <summary>Suma de relieves.</summary>
        public static System.Func<float, float, float> Sum(params System.Func<float, float, float>[] parts) => (u, v) =>
        {
            float h = 0f;
            foreach (var p in parts) h += p(u, v);
            return h;
        };

        /// <summary>Dibujo: manchas en grupos (óxido, salitre, mugre) que desplazan la banda <paramref name="delta"/>.</summary>
        public static System.Func<float, float, int> Mottle(float scale = 3f, float threshold = 0.7f, int delta = -1, int seed = 11) => (u, v) =>
            PixelCanvas.ValueNoise(u / scale, v / scale, 0, seed) > threshold ? delta : 0;

        /// <summary>Dibujo: rayas perpendiculares a u de <paramref name="width"/> px cada <paramref name="period"/> px.</summary>
        public static System.Func<float, float, int> Stripes(float period, float width = 1f, int delta = -1, float offset = 0f) => (u, v) =>
            Mathf.Repeat(u - offset, period) < width ? delta : 0;

        /// <summary>Máscara 0..1 en manchas, para <see cref="ShadedCanvas.Shape.WithTint"/> (verdín, sangre seca...).</summary>
        public static System.Func<float, float, float> Spots(float scale = 3f, float threshold = 0.65f, int seed = 23) => (u, v) =>
            PixelCanvas.ValueNoise(u / scale, v / scale, 0, seed) > threshold ? 1f : 0f;
    }
}
