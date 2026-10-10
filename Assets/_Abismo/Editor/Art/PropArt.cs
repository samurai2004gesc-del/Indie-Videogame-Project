using System.Collections.Generic;
using UnityEngine;
using static Abismo.EditorTools.Rig;

namespace Abismo.EditorTools
{
    /// <summary>Un sprite estático de decorado o de juego.</summary>
    public sealed class PropSprite
    {
        public string Name;
        public PixelCanvas Color;
        /// <summary>Normal map para las luces 2D (null si el sprite no se ilumina).</summary>
        public PixelCanvas Normal;
        /// <summary>Partes que brillan solas (null si no hay): se dibujan encima sin iluminar.</summary>
        public PixelCanvas Emission;
        /// <summary>Pivote normalizado: (0.5, 0) = centro de la base; (0.5, 1) = colgado del techo.</summary>
        public Vector2 Pivot01 = new Vector2(0.5f, 0f);
        /// <summary>true = no recibe luces (llamas, brillos, siluetas de primer plano).</summary>
        public bool Unlit;
        /// <summary>Píxeles desde el pivote donde va una llama animada (y una luz).</summary>
        public List<Vector2> FlameAnchors = new List<Vector2>();
        /// <summary>9-slice / mosaico: izquierda, abajo, derecha, arriba (px).</summary>
        public Vector4 Border;
    }

    /// <summary>Animación en bucle de decorado (llamas, agua, rayos de luz).</summary>
    public sealed class PropAnimation
    {
        public string Name;
        public List<PixelCanvas> Frames = new List<PixelCanvas>();
        public List<PixelCanvas> Normals;
        public float Fps = 10f;
        public Vector2 Pivot01 = new Vector2(0.5f, 0f);
        public bool Unlit;
    }

    /// <summary>
    /// Atrezo pintado por código con el mismo motor 2.5D que los personajes: candelabros, la estatua de la Madre
    /// (la pieza estrella, eco de la Piedad de Blasphemous), el ídolo del Durmiente, cadenas, jaulas, estandartes,
    /// corales, faroles... además de los objetos de juego (monedas, plataformas, rejas, orbes) y las animaciones
    /// de llamas, agua y rayos de luz.
    /// </summary>
    public static class PropArt
    {
        // ------------------------------------------------------------------
        // Materiales
        // ------------------------------------------------------------------

        static readonly PixelMaterial Stone = new PixelMaterial(Ramp.Make("67706a", 5, 0.1f, 0.3f, 1.5f)) { Rim = 0.55f, Ambient = 0.22f, Dither = 0.05f, Texture = StoneGrain };
        static readonly PixelMaterial StoneDark = new PixelMaterial(Ramp.Make("444c49", 5, 0.1f, 0.3f, 1.5f)) { Rim = 0.5f, Ambient = 0.22f, Dither = 0.05f, Texture = StoneGrain };
        static readonly PixelMaterial StoneGreen = new PixelMaterial(Ramp.Make("5f7268", 5, 0.12f, 0.3f, 1.55f)) { Rim = 0.6f, Ambient = 0.22f, Dither = 0.04f, Texture = StoneGrain };
        static readonly PixelMaterial Soapstone = new PixelMaterial(Ramp.Make("3c5248", 5, 0.12f, 0.3f, 1.7f)) { Rim = 0.6f, Gloss = 0.3f, Ambient = 0.22f, Dither = 0.03f };
        static readonly PixelMaterial Flesh = new PixelMaterial(Ramp.Make("b9b39b", 5, 0.12f, 0.3f, 1.3f)) { Rim = 0.5f, Ambient = 0.25f, Dither = 0.03f };
        static readonly PixelMaterial Gold = new PixelMaterial(Ramp.Make("a8822f", 5, 0.12f, 0.3f, 1.65f)) { Gloss = 0.65f, Rim = 0.5f, Ambient = 0.25f, Dither = 0f };
        static readonly PixelMaterial Wax = new PixelMaterial(Ramp.Make("c2b28c", 5, 0.1f, 0.38f, 1.28f)) { Rim = 0.5f, Ambient = 0.26f, Dither = 0f };
        static readonly PixelMaterial Iron = new PixelMaterial(Ramp.Make("4a4a4c", 4, 0.06f, 0.35f, 1.7f)) { Gloss = 0.4f, Rim = 0.5f, Ambient = 0.25f, Dither = 0f };
        static readonly PixelMaterial Rust = new PixelMaterial(Ramp.Make("6b4a38", 5, 0.08f, 0.3f, 1.5f)) { Gloss = 0.2f, Rim = 0.45f, Ambient = 0.24f, Dither = 0.04f, Texture = RustNoise };
        static readonly PixelMaterial Wood = new PixelMaterial(Ramp.Make("51402f", 5, 0.08f, 0.3f, 1.5f)) { Rim = 0.5f, Ambient = 0.25f, Dither = 0.03f, Texture = WoodGrain };
        static readonly PixelMaterial Rope = new PixelMaterial(Ramp.Make("7a6648", 4, 0.06f, 0.4f, 1.3f)) { Rim = 0.4f, Ambient = 0.3f, Dither = 0f };
        static readonly PixelMaterial Bone = new PixelMaterial(Ramp.Make("c4b998", 5, 0.08f, 0.3f, 1.3f)) { Rim = 0.5f, Ambient = 0.26f, Dither = 0.02f };
        static readonly PixelMaterial Crimson = new PixelMaterial(Ramp.Make("7a1626", 5, 0.1f, 0.3f, 1.6f)) { Rim = 0.65f, Ambient = 0.22f, Dither = 0.04f };
        static readonly PixelMaterial CoralPink = new PixelMaterial(Ramp.Make("b65a74", 5, 0.12f, 0.3f, 1.5f)) { Rim = 0.6f, Ambient = 0.26f, Dither = 0.02f };
        static readonly PixelMaterial CoralOrange = new PixelMaterial(Ramp.Make("c47a48", 5, 0.12f, 0.3f, 1.45f)) { Rim = 0.6f, Ambient = 0.26f, Dither = 0.02f };
        static readonly PixelMaterial CoralRed = new PixelMaterial(Ramp.Make("8f2a3a", 5, 0.1f, 0.3f, 1.6f)) { Rim = 0.6f, Ambient = 0.24f, Dither = 0f };
        static readonly PixelMaterial TentacleSkin = new PixelMaterial(Ramp.Make("4d3a57", 5, 0.12f, 0.3f, 1.6f)) { Rim = 0.7f, Gloss = 0.25f, Ambient = 0.22f, Dither = 0.02f };
        static readonly PixelMaterial Sucker = new PixelMaterial(Ramp.Make("a88aa0", 3, 0.06f, 0.5f, 1.2f)) { Ambient = 0.4f, Dither = 0f, Outline = false };
        static readonly PixelMaterial Silhouette = new PixelMaterial(new[] { PixelCanvas.Hex("06080a"), PixelCanvas.Hex("0a0e10"), PixelCanvas.Hex("0f1517"), PixelCanvas.Hex("17201f") }) { Rim = 1.2f, Ambient = 0.05f, Dither = 0.04f };
        static readonly PixelMaterial GlowAmber = PixelMaterial.Glow("ffb45a");
        static readonly PixelMaterial GlowAmberCore = PixelMaterial.Glow("fff0c0");
        static readonly PixelMaterial GlowTeal = PixelMaterial.Glow("6cf7c8");
        static readonly PixelMaterial GlowTealDim = PixelMaterial.Glow("2f9c80");
        static readonly PixelMaterial GlowTealHot = PixelMaterial.Glow("d8fff0");
        static readonly PixelMaterial GlowGreen = PixelMaterial.Glow("8dff9a");

        // Materiales de detalle (v4): rampas de 6 tonos, transición tramada de 1 px y relieve propio.
        static readonly PixelMaterial Marble = new PixelMaterial(Ramp.Make("4c5b53", 6, 0.16f, 0.2f, 1.55f))
        {
            Rim = 0.65f, Ambient = 0.13f, Dither = 0f, BandDither = 0.14f,
        };
        static readonly PixelMaterial MarbleDeep = new PixelMaterial(Ramp.Make("3b4a45", 5, 0.14f, 0.3f, 1.5f)) { Rim = 0.4f, Ambient = 0.18f, Dither = 0f };
        static readonly PixelMaterial PedestalStone = new PixelMaterial(Ramp.Make("474c47", 6, 0.12f, 0.24f, 1.55f))
        {
            Rim = 0.5f, Ambient = 0.15f, Dither = 0f, BandDither = 0.12f,
            Bump = Patterns.Lumps(3f, 0.9f, 41), BumpStrength = 0.6f,
            Detail = (u, v) => PixelCanvas.ValueNoise(u / 2.2f, v / 1.6f, 0, 43) > 0.78f ? -1 : 0,
        };
        static readonly PixelMaterial OldGold = new PixelMaterial(Ramp.Make("6e5c30", 5, 0.12f, 0.3f, 1.62f))
        {
            Gloss = 0.45f, Rim = 0.5f, Ambient = 0.22f, Dither = 0f, SpecularAt = 0.88f, Specular = PixelCanvas.Hex("fff1c4"),
        };
        static readonly PixelMaterial Corpse = new PixelMaterial(Ramp.Make("a2a290", 6, 0.16f, 0.26f, 1.3f)) { Rim = 0.55f, Ambient = 0.24f, Dither = 0f, BandDither = 0.14f };
        static readonly PixelMaterial WaxPool = new PixelMaterial(Ramp.Make("b8a77c", 4, 0.08f, 0.45f, 1.2f)) { Rim = 0.3f, Ambient = 0.35f, Dither = 0f, Outline = false };
        static readonly PixelMaterial GoldOrnate = new PixelMaterial(Ramp.Make("8c6c2a", 6, 0.14f, 0.24f, 1.75f))
        {
            Gloss = 0.5f, Rim = 0.55f, Ambient = 0.18f, Dither = 0f, SpecularAt = 0.9f, Specular = PixelCanvas.Hex("fff3c8"),
        };
        static readonly PixelMaterial IronDark = new PixelMaterial(Ramp.Make("3c3d42", 5, 0.08f, 0.32f, 1.85f))
        {
            Gloss = 0.4f, Rim = 0.55f, Ambient = 0.18f, Dither = 0f, SpecularAt = 0.92f, Specular = PixelCanvas.Hex("c8d0d8"),
        };
        static readonly PixelMaterial CrimsonCloth = new PixelMaterial(Ramp.Make("6e1624", 6, 0.12f, 0.24f, 1.6f))
        {
            Rim = 0.65f, Ambient = 0.18f, Dither = 0f, BandDither = 0.15f, Bump = Patterns.Weave(2f, 0.25f), BumpStrength = 0.5f,
        };
        static readonly Color32 Verdigris = PixelCanvas.Hex("3f8566");
        static readonly Color32 VeinPurple = PixelCanvas.Hex("6b3b72");

        static float StoneGrain(int x, int y) => (PixelCanvas.ValueNoise(x / 3f, y / 3f, 0, 77) - 0.5f) * 0.16f;
        static float RustNoise(int x, int y) => PixelCanvas.ValueNoise(x / 2.5f, y / 2.5f, 0, 31) > 0.62f ? -0.16f : 0f;
        static float WoodGrain(int x, int y) => (PixelCanvas.ValueNoise(x / 1.2f, y / 9f, 0, 13) - 0.5f) * 0.22f;

        // ------------------------------------------------------------------
        // Utilidades
        // ------------------------------------------------------------------

        /// <summary>Lienzo con el origen en el centro de la base (o arriba si <paramref name="hanging"/>).</summary>
        static ShadedCanvas Canvas(int w, int h, bool hanging = false) => new ShadedCanvas(w, h, w * 0.5f, hanging ? h : 0f);

        static PropSprite Finish(string name, ShadedCanvas c, bool hanging = false, bool unlit = false, params Vector2[] flames)
        {
            var color = c.Render(out var normal, out var emission);
            var sprite = new PropSprite
            {
                Name = name,
                Color = color,
                Normal = unlit ? null : normal,
                Emission = HasPixels(emission) && !unlit ? emission : null,
                Pivot01 = hanging ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f),
                Unlit = unlit,
            };
            sprite.FlameAnchors.AddRange(flames);
            return sprite;
        }

        static bool HasPixels(PixelCanvas c)
        {
            foreach (var p in c.Pixels) if (p.a > 0) return true;
            return false;
        }

        static N3 Cylinder(float u, float tiltY = 0f)
        {
            u = Mathf.Clamp(u, -1f, 1f);
            return new N3(u, tiltY, Mathf.Sqrt(Mathf.Max(0.05f, 1f - u * u))).Normalized();
        }

        /// <summary>Anillo (eslabón, aro) con volumen de toro.</summary>
        static void Ring(ShadedCanvas c, Vector2 center, float rx, float ry, float thickness, PixelMaterial m, float z, int group = 0)
        {
            float half = thickness * 0.5f;
            c.Custom(center.x - rx - half - 1f, center.y - ry - half - 1f, center.x + rx + half + 1f, center.y + ry + half + 1f, (px, py) =>
            {
                float dx = (px - center.x) / rx, dy = (py - center.y) / ry;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float r = (rx + ry) * 0.5f;
                float v = (d - 1f) * r / half;
                if (Mathf.Abs(v) > 1f) return (false, N3.Front);
                float len = Mathf.Max(0.001f, d);
                return (true, new N3(dx / len * v, dy / len * v, Mathf.Sqrt(Mathf.Max(0.05f, 1f - v * v))).Normalized());
            }, m, z, 0f, group);
        }

        // ---- Utilidades de detalle (v4) ----

        /// <summary>Perfil de pliegue: cresta redondeada y valle en V (el valle es donde la fase es entera).</summary>
        static float FoldWave(float phase) => Mathf.Abs(Mathf.Sin(phase * Mathf.PI));

        /// <summary>Campana suave (bulto de una rodilla, de un pecho...).</summary>
        static float Bell(float x, float y, float cx, float cy, float rx, float ry)
        {
            float dx = (x - cx) / rx, dy = (y - cy) / ry;
            return Mathf.Exp(-(dx * dx + dy * dy));
        }

        /// <summary>Interpolación lineal por tramos: pares (t, valor) ordenados por t.</summary>
        static float Pw(float t, params float[] kv)
        {
            if (t <= kv[0]) return kv[1];
            for (int i = 2; i < kv.Length; i += 2)
                if (t <= kv[i]) return kv[i - 1] + (kv[i + 1] - kv[i - 1]) * (t - kv[i - 2]) / (kv[i] - kv[i - 2]);
            return kv[kv.Length - 1];
        }

        /// <summary>
        /// Pieza definida por su borde izquierdo y derecho en cada altura (mantos, faldas, velos), con volumen de cilindro.
        /// El marco local es el del lienzo, así que su Bump/Detail reciben coordenadas del lienzo.
        /// </summary>
        static ShadedCanvas.Shape Span(ShadedCanvas c, float y0, float y1, System.Func<float, float> left, System.Func<float, float> right,
                                       PixelMaterial m, float z, float shade = 0f, int group = 0, float round = 0.85f,
                                       System.Func<float, float, bool> clip = null)
        {
            float minX = float.MaxValue, maxX = float.MinValue;
            for (float y = y0; y <= y1 + 0.5f; y += 1f)
            {
                minX = Mathf.Min(minX, left(y));
                maxX = Mathf.Max(maxX, right(y));
            }
            return c.Custom(minX - 1f, y0 - 1f, maxX + 1f, y1 + 1f, (px, py) =>
            {
                if (py < y0 || py > y1) return (false, N3.Front);
                float l = left(py), r = right(py);
                if (px < l || px > r) return (false, N3.Front);
                if (clip != null && !clip(px, py)) return (false, N3.Front);
                float u = (px - (l + r) * 0.5f) / Mathf.Max(0.5f, (r - l) * 0.5f);
                return (true, Cylinder(u * round));
            }, m, z, shade, group);
        }

        /// <summary>
        /// Píxeles dibujados a mano (un rostro, una calavera...) con los tonos de la rampa de un material: '0'…'9' = tono,
        /// 'k' = tono hondo, '.' = no tocar. La primera fila es la de arriba y (x, y) su esquina superior izquierda.
        /// </summary>
        static void Stamp(ShadedCanvas c, float x, float y, PixelMaterial m, params string[] rows)
        {
            for (int r = 0; r < rows.Length; r++)
            {
                for (int k = 0; k < rows[r].Length; k++)
                {
                    char ch = rows[r][k];
                    if (ch == '.') continue;
                    c.Decal(x + k, y - r, m.Tone(ch == 'k' ? -1 : ch - '0'));
                }
            }
        }

        /// <summary>Vela de cera con cabo, borde derretido y goterones; devuelve la posición de la mecha (ancla de la llama).</summary>
        static Vector2 Candle(ShadedCanvas c, Vector2 baseAt, float height, float radius, float z, int seed, float lean = 0f)
        {
            int g = c.NewGroup();
            Vector2 top = V(baseAt.x + lean, baseAt.y + height);
            c.Capsule(baseAt, top, radius + 0.25f, radius, Wax, z, 0f, g, 0.25f);
            // Borde superior hundido: la cera se derrite alrededor de la mecha.
            c.Ellipse(Add(top, V(0f, 0.6f)), radius * 0.9f, 0.9f, 0f, WaxPool, z + 0.01f, 0f, g, 0.5f);
            // Goterones que bajan por el cuerpo.
            for (int k = 0; k < 2; k++)
            {
                float side = (k == 0 ? -0.55f : 0.45f) * radius + (PixelCanvas.Hash(seed, k, 3) - 0.5f) * 0.6f;
                float len = 1.5f + PixelCanvas.Hash(seed, k, 5) * height * 0.45f;
                Vector2 d0 = V(top.x + side, top.y - 0.2f);
                c.Capsule(d0, Add(d0, V(0f, -len)), 0.8f, 0.7f, Wax, z + 0.02f, 0f, g);
                c.Ellipse(Add(d0, V(0f, -len)), 0.95f, 0.85f, 0f, Wax, z + 0.02f, 0f, g);
            }
            // Charco de cera al pie.
            c.Ellipse(Add(baseAt, V(0.4f, 0.4f)), radius + 1.8f, 1.1f, 0f, Wax, z - 0.01f, -0.06f, g, 0.6f);
            c.Dot(Add(top, V(0f, 0.9f)), -3, g);
            return V(top.x, top.y + 1f);
        }

        static Vector2[] Star(Vector2 c, float rOut, float rIn, float rotation)
        {
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++) pts[i] = Add(c, Dir(rotation + i * 36f, i % 2 == 0 ? rOut : rIn));
            return pts;
        }

        static Vector2[] Blob(Vector2 center, float rx, float ry, int points, int seed, float jag = 0.3f)
        {
            var pts = new Vector2[points];
            for (int i = 0; i < points; i++)
            {
                float a = i / (float)points * 360f + PixelCanvas.Hash(i, seed, 3) * 20f;
                float r = 1f - jag * PixelCanvas.Hash(i, seed, 7);
                pts[i] = Add(center, V(Mathf.Cos(a * Mathf.Deg2Rad) * rx * r, Mathf.Sin(a * Mathf.Deg2Rad) * ry * r));
            }
            return pts;
        }

        // ------------------------------------------------------------------
        // Decorado
        // ------------------------------------------------------------------

        public static List<PropSprite> Statics() => new List<PropSprite>
        {
            Candelabra(), FloorCandles(), BrokenColumn(),
            Rubble("escombros_a", 24, 1), Rubble("escombros_b", 40, 2), Rubble("escombros_c", 56, 3),
            MotherStatue(), Idol(), Chains(), Cage(), Banner(),
            Coral("coral_a", 1), Coral("coral_b", 2), Ribs(), Lantern(), Net(), Anchor(),
            ForegroundColumn(), ForegroundRubble(),
        };

        // Candelabro de oro labrado: trípode de volutas con garras, fuste con nudos gallonados, cuatro brazos en S con
        // volutas debajo, platillos con cera que rebosa y cinco velas derretidas.
        static PropSprite Candelabra()
        {
            var c = Canvas(40, 72);
            int gFeet = c.NewGroup(), gStem = c.NewGroup(), gArms = c.NewGroup();
            // Patas en voluta con garra y bola.
            foreach (float sx in new[] { -1f, 1f })
            {
                c.Strand(new[] { V(sx * 2f, 8f), V(sx * 6f, 6.5f), V(sx * 10f, 3.5f), V(sx * 12.5f, 1.8f) }, 1.7f, 1.1f, GoldOrnate, 1f, -0.04f, gFeet);
                c.Strand(new[] { V(sx * 5f, 7.4f), V(sx * 5.5f, 9.6f), V(sx * 7.4f, 9.8f), V(sx * 7.6f, 8.2f) }, 0.8f, 0.6f, GoldOrnate, 1.02f, 0f, gFeet);
                c.Ellipse(V(sx * 13.5f, 1.6f), 1.8f, 1.6f, 0f, GoldOrnate, 1.05f, 0f, gFeet);
                c.Crease(V(sx * 13.5f - 0.6f, 0.6f), V(sx * 13.5f - 0.6f, 2.4f), 1, gFeet);
            }
            c.Strand(new[] { V(0f, 8f), V(0.5f, 4f), V(1f, 1.6f) }, 1.8f, 1.3f, GoldOrnate, 1.1f, 0f, gFeet);
            c.Ellipse(V(1f, 1.4f), 1.9f, 1.4f, 0f, GoldOrnate, 1.12f, 0f, gFeet);
            // Base gallonada y fuste con nudos.
            c.Ellipse(V(0f, 9f), 6.6f, 2.6f, 0f, GoldOrnate, 1.3f, 0f, gStem).WithBump(Patterns.Ridges(1.8f, 0.6f), 1f);
            c.Ellipse(V(0f, 11.5f), 3.6f, 1.6f, 0f, GoldOrnate, 1.32f, 0f, gStem);
            c.Capsule(V(0f, 11f), V(0f, 46f), 1.4f, 1.2f, GoldOrnate, 1.25f, 0f, gStem);
            c.Ellipse(V(0f, 18f), 3.4f, 2.8f, 0f, GoldOrnate, 1.4f, 0f, c.NewGroup()).WithBump(Patterns.Ridges(1.6f, 0.5f), 1f);
            c.Ellipse(V(0f, 29f), 2.3f, 3.6f, 0f, GoldOrnate, 1.4f, 0f, c.NewGroup());
            c.Ellipse(V(0f, 37f), 2.9f, 1.1f, 0f, GoldOrnate, 1.4f, 0f, c.NewGroup());
            c.Ellipse(V(0f, 44f), 3.8f, 2.3f, 0f, GoldOrnate, 1.45f, 0f, c.NewGroup()).WithBump(Patterns.Ridges(1.5f, 0.5f), 1f);
            c.Glint(V(1.4f, 19.2f), 0, 0, GoldOrnate);
            c.Glint(V(0.8f, 30.6f), 0, 0, GoldOrnate);
            // Brazos en S con voluta y platillos.
            var flames = new List<Vector2>();
            float[] xs = { -14f, -7f, 0f, 7f, 14f };
            float[] cupY = { 52f, 50f, 56f, 50f, 52f };
            float[] candle = { 8.5f, 5.5f, 10.5f, 7f, 7.5f };
            for (int i = 0; i < 5; i++)
            {
                float x = xs[i], sx = Mathf.Sign(x);
                if (i != 2)
                {
                    c.Strand(new[] { V(sx * 1f, 45f), V(x * 0.45f, 42.5f), V(x * 0.9f, 45f), V(x, cupY[i] - 1.5f) }, 1.2f, 1f, GoldOrnate, 1.5f, 0f, gArms);
                    Vector2 cv = V(x * 0.55f, 42.3f);
                    c.Strand(new[] { cv, Add(cv, V(sx * 0.4f, -2.2f)), Add(cv, V(sx * 1.8f, -2.6f)), Add(cv, V(sx * 2f, -1.2f)) }, 0.7f, 0.5f, GoldOrnate, 1.52f, 0f, gArms);
                }
                else c.Capsule(V(0f, 45f), V(0f, cupY[i] - 1f), 1.2f, 1f, GoldOrnate, 1.5f, 0f, gArms);
                int gCup = c.NewGroup();
                c.Ellipse(V(x, cupY[i] - 1.2f), 1.4f, 1.3f, 0f, GoldOrnate, 1.6f, 0f, gCup);
                c.Ellipse(V(x, cupY[i]), 3.4f, 1.2f, 0f, GoldOrnate, 1.62f, 0f, gCup, 0.3f);
                c.Glint(V(x + 1.5f, cupY[i] + 0.4f), gCup, 0);
                // Cera que rebosa del platillo.
                float dx = (i % 2 == 0 ? -2.4f : 2.2f);
                c.Capsule(V(x + dx, cupY[i] - 0.2f), V(x + dx, cupY[i] - 2.8f - (i % 3)), 0.7f, 0.55f, Wax, 1.7f, 0f, gCup);
                flames.Add(Candle(c, V(x, cupY[i] + 0.8f), candle[i], 1.35f, 1.8f + i * 0.01f, 300 + i, (i % 2) * 0.2f));
            }
            return Finish("candelabro", c, false, false, flames.ToArray());
        }

        // Velas en el suelo: un montículo de cera acumulada con cabos de distinta altura (uno ya apagado).
        static PropSprite FloorCandles()
        {
            var c = Canvas(36, 18);
            int gm = c.NewGroup();
            c.Poly(Blob(V(0f, 1.6f), 16.5f, 2.6f, 14, 91, 0.25f), Wax, 0.5f, 1.2f, -0.12f, gm).WithBump(Patterns.Lumps(2.5f, 1.2f, 93), 1f);
            c.Ellipse(V(-6f, 2.6f), 5f, 1.6f, 0f, Wax, 0.52f, -0.06f, gm);
            c.Ellipse(V(7f, 2.4f), 5.5f, 1.5f, 0f, Wax, 0.52f, -0.06f, gm);
            float[] xs = { -12f, -6.5f, -1f, 4.5f, 10f };
            float[] hs = { 9f, 5.5f, 12.5f, 4f, 8f };
            float[] rs = { 1.9f, 1.7f, 2.2f, 1.6f, 1.9f };
            var flames = new List<Vector2>();
            for (int i = 0; i < 5; i++)
                flames.Add(Candle(c, V(xs[i], 1.6f + (i % 2) * 0.6f), hs[i], rs[i], 1f + (i % 2) * 0.2f + i * 0.01f, 500 + i, (i - 2) * 0.15f));
            // Cabo apagado y consumido.
            int gs = c.NewGroup();
            c.Capsule(V(14.5f, 1.8f), V(14.6f, 3.6f), 1.5f, 1.4f, Wax, 1.3f, -0.05f, gs, 0.25f);
            c.Dot(V(14.6f, 5.4f), -2, gs);
            c.Capsule(V(14.6f, 4.8f), V(15.2f, 6f), 0.3f, 0.3f, Iron, 1.31f, 0f, gs);
            return Finish("velas_suelo", c, false, false, flames.ToArray());
        }


        static PropSprite BrokenColumn()
        {
            var c = Canvas(56, 120);
            int g = c.NewGroup();
            // Plinto y toro.
            c.Poly(new[] { V(-21f, 0f), V(21f, 0f), V(21f, 7f), V(-21f, 7f) }, Stone, 1f, 2f, 0f, g);
            c.Ellipse(V(0f, 9f), 18f, 3.4f, 0f, Stone, 1.1f, 0f, g);
            // Fuste estriado con la rotura dentada arriba.
            c.Custom(-15f, 9f, 15f, 100f, (px, py) =>
            {
                if (Mathf.Abs(px) > 14f) return (false, N3.Front);
                float top = 88f + 9f * PixelCanvas.ValueNoise(px / 3f, 0f, 0, 9) + (px > 0f ? 3f : 0f);
                if (py > top) return (false, N3.Front);
                float u = px / 14f;
                float flute = 0.28f * Mathf.Sin(px * Mathf.PI / 3.5f);
                return (true, Cylinder(u + flute, py > top - 2f ? 0.6f : 0f));
            }, Stone, 1.2f, 0f, g);
            // Grietas.
            c.Strand(new[] { V(-4f, 90f), V(-2f, 78f), V(-5f, 66f), V(-3f, 58f) }, 0.6f, 0.4f, StoneDark, 1.25f, 0f, g);
            // Capitel caído a la derecha.
            int g2 = c.NewGroup();
            c.Poly(new[] { V(5f, 0f), V(27f, 0f), V(26f, 9f), V(22f, 14f), V(8f, 14f), V(4f, 9f) }, Stone, 1.5f, 2f, -0.04f, g2, 0.1f, 0f);
            c.Ellipse(V(8f, 10f), 3.5f, 3.5f, 0f, Stone, 1.55f, 0f, g2);
            c.Ellipse(V(24f, 10f), 3.5f, 3.5f, 0f, Stone, 1.55f, 0f, g2);
            Rubble(c, V(-18f, 0f), 10f, 6f, 41);
            return Finish("columna_rota", c);
        }

        static void Rubble(ShadedCanvas c, Vector2 at, float width, float height, int seed)
        {
            int n = Mathf.Max(2, Mathf.RoundToInt(width / 7f));
            for (int i = 0; i < n; i++)
            {
                float t = n == 1 ? 0.5f : i / (float)(n - 1);
                float x = at.x + (t - 0.5f) * width;
                float h = height * (1f - Mathf.Abs(t - 0.5f) * 1.2f) * (0.6f + 0.6f * PixelCanvas.Hash(i, seed, 1));
                float r = Mathf.Max(2.5f, h * 0.9f);
                c.Poly(Blob(V(x, r * 0.7f), r * 1.2f, r, 6, seed * 13 + i, 0.35f), i % 3 == 0 ? StoneDark : Stone, 1f + PixelCanvas.Hash(i, seed, 5), 1.5f, 0f, c.NewGroup());
            }
        }

        static PropSprite Rubble(string name, int width, int seed)
        {
            var c = Canvas(width + 4, 20);
            Rubble(c, V(0f, 0f), width - 6f, Mathf.Min(14f, width * 0.3f), seed);
            return Finish(name, c);
        }

        // La Madre de las Profundidades (eco de la Piedad de Blasphemous): una sacerdotisa de piedra verdosa, velada y
        // sentada, con el ahogado de carne pálida y vetas púrpura en el regazo; de su costado brotan tentáculos que
        // cuelgan hasta la peana. Halo de espinas de oro viejo, peana tallada con molduras, verdín y velas derretidas.
        static PropSprite MotherStatue()
        {
            var c = Canvas(140, 150);

            // ---------------- Peana ----------------
            System.Func<float, float, float> streaks = (u, v) => PixelCanvas.ValueNoise(u / 1.6f, v / 6f, 0, 61) > 0.68f ? 1f : 0f;
            int gPl = c.NewGroup(), gTor = c.NewGroup(), gDie = c.NewGroup(), gCor = c.NewGroup(), gTop = c.NewGroup();
            c.Poly(new[] { V(-64f, 0f), V(64f, 0f), V(64f, 3.5f), V(62.5f, 5f), V(20f, 5f), V(18f, 4.2f), V(15f, 5f), V(-46f, 5f), V(-48f, 4f), V(-50f, 5f), V(-62.5f, 5f), V(-64f, 3.5f) },
                   PedestalStone, 0.5f, 1.5f, -0.06f, gPl).WithTint(PixelCanvas.Hex("9fae9f"), 0.22f, streaks);
            c.Capsule(V(-60.5f, 6.4f), V(60.5f, 6.4f), 1.7f, 1.7f, PedestalStone, 0.58f, 0f, gTor);
            // Neto con tres paneles rehundidos: en el central, un friso de olas con el Signo Antiguo; en los laterales, un ojo tallado.
            System.Func<float, float, float> panels = (x, y) =>
            {
                float ax = Mathf.Abs(x), h = 0f;
                bool center = ax < 27f, side = ax > 32f && ax < 54f;
                if ((center || side) && y > 9.5f && y < 17f)
                {
                    float ex = center ? 27f - ax : Mathf.Min(ax - 32f, 54f - ax);
                    float ey = Mathf.Min(y - 9.5f, 17f - y);
                    h -= Mathf.Clamp01(Mathf.Min(ex, ey)) * 1.6f;
                    if (center)
                    {
                        // Olas enroscadas a ambos lados del medallón.
                        float wave = 13.2f + 1.6f * Mathf.Sin(ax * 0.62f);
                        if (ax > 6f && Mathf.Abs(y - wave) < 0.75f) h += 1.1f;
                        if (ax > 6f && Mathf.Abs(y - wave + 2.6f) < 0.55f && Mathf.Sin(ax * 0.62f) > 0.2f) h += 0.8f;
                        float d = Mathf.Sqrt(x * x + (y - 13.25f) * (y - 13.25f));
                        if (d < 3.6f) h += 1.6f - Mathf.Max(0f, d - 2.6f);
                    }
                    else
                    {
                        float dx = (ax - 43f) / 8f, lid = 2.6f * (1f - dx * dx);
                        float dy = y - 13.25f;
                        if (lid > 0f && Mathf.Abs(dy) < lid) h += 0.9f;
                        if (lid > 0f && Mathf.Abs(dy) < lid - 1.1f) h -= 0.9f;
                        float pd = Mathf.Sqrt((ax - 43f) * (ax - 43f) + dy * dy);
                        if (pd < 1.6f) h += 1.4f;
                    }
                }
                return h;
            };
            c.Poly(new[] { V(-57f, 7.5f), V(57f, 7.5f), V(57f, 18f), V(-57f, 18f) }, PedestalStone, 0.55f, 1.2f, 0f, gDie)
             .WithBump(panels, 1.1f).WithTint(Verdigris, 0.32f, streaks);
            // Dentículos bajo la cornisa.
            for (float x = -55f; x <= 55f; x += 4f)
                c.Poly(new[] { V(x - 1f, 17f), V(x + 1f, 17f), V(x + 1f, 18.6f), V(x - 1f, 18.6f) }, PedestalStone, 0.59f, 0.6f, 0f, gCor);
            // Cornisa (gola inclinada) y losa superior con desconchones.
            c.Poly(new[] { V(-60f, 18.6f), V(60f, 18.6f), V(62.5f, 22.4f), V(-62.5f, 22.4f) }, PedestalStone, 0.6f, 1f, 0f, gCor, 0f, -0.45f);
            c.Poly(new[] { V(-62f, 22.4f), V(62f, 22.4f), V(62f, 25f), V(60.5f, 26f), V(31f, 26f), V(29f, 25.2f), V(26f, 26f), V(-38f, 26f), V(-40f, 25f), V(-42.5f, 26f), V(-60.5f, 26f), V(-62f, 25f) },
                   PedestalStone, 0.62f, 1.3f, 0f, gTop, 0f, 0.2f);
            // Grieta que baja por el neto y glifos de una inscripción en el friso de la losa.
            c.Crease(new[] { V(-20f, 18f), V(-19f, 15f), V(-21f, 12.5f), V(-20f, 9f), V(-22f, 7.5f) }, 2, gDie);
            c.Crease(new[] { V(-19f, 15f), V(-16.5f, 13.5f) }, 1, gDie);
            for (int i = 0; i < 26; i++)
            {
                float gx = -51f + i * 4f;
                if (Mathf.Abs(gx) < 8f) continue;
                float hsh = PixelCanvas.Hash(i, 3, 97);
                if (hsh < 0.5f) c.Crease(V(gx, 23.4f), V(gx, 24.6f), 1, gTop);
                else c.Crease(V(gx - 0.8f, 24.2f), V(gx + 0.8f, 24.2f), 1, gTop);
                if (hsh > 0.25f && hsh < 0.75f) c.Dot(V(gx + 1.2f, 23.4f), -1, gTop);
            }

            // ---------------- Halo de espinas (oro viejo) ----------------
            Vector2 hc = V(3f, 120f);
            int gh = c.NewGroup();
            Ring(c, hc, 18.5f, 18.5f, 3.2f, OldGold, 0.9f, gh);
            Ring(c, hc, 15.2f, 15.2f, 1.1f, OldGold, 0.89f, gh);
            for (int i = 0; i < 18; i++)
            {
                float a = i * 20f + 10f;
                Vector2 b0 = Add(hc, Dir(a, 19.5f));
                float len = i % 2 == 0 ? 9f : 5f;
                c.Capsule(b0, Add(b0, Dir(a, len)), 1.5f, 0.2f, OldGold, 0.88f, 0f, gh);
                c.Ellipse(Add(hc, Dir(a + 10f, 18.5f)), 1.1f, 1.1f, 0f, OldGold, 0.92f, 0f, gh);
            }

            // ---------------- Manto (detrás) ----------------
            float[] cloakL = { 26f, -55f, 34f, -53f, 50f, -48f, 70f, -40f, 88f, -31f, 100f, -24f, 109f, -16f };
            float[] cloakR = { 26f, 57f, 34f, 55f, 50f, 50f, 70f, 43f, 88f, 35f, 100f, 29f, 109f, 21f };
            System.Func<float, float> clL = y => Pw(y, cloakL), clR = y => Pw(y, cloakR);
            System.Func<float, float, float> cloakPhase = (x, y) =>
                6f * (x - clL(y)) / (clR(y) - clL(y)) + (PixelCanvas.ValueNoise(x / 8f, y / 16f, 0, 71) - 0.5f) * 0.8f;
            int gCloak = c.NewGroup();
            Span(c, 26f, 109f, clL, clR, Marble, 1f, -0.05f, gCloak, 0.9f, (x, y) => y >= 26f + 2f * (1f - FoldWave(cloakPhase(x, 27f))))
                .WithBump((x, y) => FoldWave(cloakPhase(x, y)) * 2.8f, 1f)
                .WithDetail((x, y) => FoldWave(cloakPhase(x, y)) < 0.1f ? -1 : 0)
                .WithTint(Verdigris, 0.38f, (x, y) => FoldWave(cloakPhase(x, y)) < 0.35f && PixelCanvas.ValueNoise(x / 3f, y / 4f, 0, 73) > 0.55f ? 1f : 0f);

            // ---------------- Falda: rodillas, hueco del regazo y pliegues que caen hasta la peana ----------------
            float[] skirtL = { 26f, -47f, 40f, -44f, 56f, -38f, 66f, -31f, 80f, -21f };
            float[] skirtR = { 26f, 49f, 40f, 46f, 56f, 41f, 66f, 35f, 80f, 25f };
            System.Func<float, float> skL = y => Pw(y, skirtL), skR = y => Pw(y, skirtR);
            float SkirtField(float x, float y, out float fold)
            {
                float n = PixelCanvas.ValueNoise(x / 7f, y / 11f, 0, 5) - 0.5f;
                float t = (x - skL(y)) / (skR(y) - skL(y));
                float vPhase = t * 6f + n * 0.8f;
                float vert = FoldWave(vPhase);
                float cat = 38f + 0.06f * (x - 2f) * (x - 2f);
                float cPhase = (y - cat) / 7.5f + n * 0.4f;
                float catf = FoldWave(cPhase);
                float w = Mathf.Clamp01(1f - Mathf.Abs(x - 2f) / 17f) * Mathf.Clamp01((y - 33f) / 8f) * Mathf.Clamp01((63f - y) / 5f);
                fold = Mathf.Lerp(vert, catf, w);
                float amp = Mathf.Lerp(4f, 1.6f, Mathf.InverseLerp(26f, 70f, y));
                float knees = 7f * Bell(x, y, -22f, 59f, 11f, 8f) + 7f * Bell(x, y, 26f, 57f, 11f, 8f) - 3f * Bell(x, y, 2f, 60f, 9f, 6f);
                return fold * amp + knees;
            }
            int gSkirt = c.NewGroup();
            Span(c, 26f, 80f, skL, skR, Marble, 2f, 0f, gSkirt, 0.8f, (x, y) =>
            {
                SkirtField(x, 27f, out float f0);
                return y >= 26f + 2.5f * (1f - f0);
            })
                .WithBump((x, y) => SkirtField(x, y, out _), 1f)
                .WithDetail((x, y) => { SkirtField(x, y, out float f); return f < 0.09f && y < 66f ? -1 : 0; })
                .WithTint(Verdigris, 0.42f, (x, y) =>
                {
                    SkirtField(x, y, out float f);
                    float n = PixelCanvas.ValueNoise(x / 3f, y / 4f, 0, 79);
                    return (f < 0.3f && n > 0.5f) || (y < 31f && n > 0.45f) ? 1f : 0f;
                });

            // ---------------- Busto con cíngulo ----------------
            float[] torsoL = { 74f, -20f, 86f, -19f, 96f, -18f, 106f, -15f };
            float[] torsoR = { 74f, 24f, 86f, 24f, 96f, 25f, 106f, 22f };
            int gTorso = c.NewGroup();
            System.Func<float, float, float> torsoPhase = (x, y) => (0.8f * x - 0.6f * y) / 7f + (PixelCanvas.ValueNoise(x / 5f, y / 5f, 0, 83) - 0.5f) * 0.6f;
            Span(c, 74f, 106f, y => Pw(y, torsoL), y => Pw(y, torsoR), Marble, 2.4f, 0f, gTorso, 0.8f)
                .WithBump((x, y) => FoldWave(torsoPhase(x, y)) * 1.4f + 3f * Bell(x, y, -5f, 93f, 6f, 5f) + 3f * Bell(x, y, 12f, 93f, 6f, 5f), 1f)
                .WithDetail((x, y) => FoldWave(torsoPhase(x, y)) < 0.08f ? -1 : 0);
            // ---------------- Velo: cae desde la coronilla en pliegues radiales y enmarca el rostro ----------------
            float[] veilL = { 95f, -25f, 102f, -21f, 110f, -17f, 118f, -15f, 124f, -13f, 129f, -10f, 132.5f, -6f, 134.5f, -1f };
            float[] veilR = { 95f, 31f, 102f, 28f, 110f, 23f, 118f, 19.5f, 124f, 17f, 129f, 13.5f, 132.5f, 9f, 134.5f, 4f };
            Vector2 crown = V(1f, 134f);
            System.Func<float, float, float> veilPhase = (x, y) =>
                Mathf.Atan2(x - crown.x, crown.y - y + 2f) * Mathf.Rad2Deg / 17f + (PixelCanvas.ValueNoise(x / 5f, y / 7f, 0, 89) - 0.5f) * 0.5f;
            int gVeil = c.NewGroup();
            Span(c, 95f, 134f, y => Pw(y, veilL), y => Pw(y, veilR), Marble, 3f, 0f, gVeil, 0.9f, (x, y) => y >= 95f + 3f * (1f - FoldWave(veilPhase(x, 96f))))
                .WithBump((x, y) => FoldWave(veilPhase(x, y)) * Mathf.Clamp01((crown.y - y) / 18f) * 1.8f, 1f)
                .WithDetail((x, y) => FoldWave(veilPhase(x, y)) < 0.09f && y < 126f ? -1 : 0);

            // Rostro inclinado hacia el ahogado, a tres cuartos y dibujado a mano píxel a píxel (es el foco de la estatua):
            // el borde del velo le echa sombra en la frente, párpados cerrados, la nariz y la mejilla con luz, la boca,
            // una lágrima de agua negra y el hueco oscuro del velo detrás de la mandíbula.
            c.Ellipse(V(-1.5f, 112.5f), 7f, 8.5f, 20f, MarbleDeep, 3.05f, 0f, gVeil);
            Stamp(c, -9f, 121f, Marble,
                "...34455543...",
                "..345555554443",
                ".3442111112443",
                ".34112222211k3",
                "34112233332kk4",
                "341223344432k4",
                "341233000442k4",
                "341003431542k4",
                "341234341542k3",
                ".34243441431k3",
                ".34102344431k3",
                ".34233443321k3",
                "..340023321kk3",
                "..34344321kk3.",
                "...313321kk3..",
                "....3100kk3...");

            // ---------------- Brazos de la Madre ----------------
            int gArmR = c.NewGroup(), gArmL = c.NewGroup();
            c.Capsule(V(-15f, 100f), V(-26f, 85f), 5.2f, 4.6f, Marble, 3.5f, 0f, gArmR);
            c.Capsule(V(-26f, 85f), V(-33f, 75f), 4f, 3.2f, Marble, 3.45f, 0f, gArmR);
            // Manga ancha que cuelga bajo el codo.
            c.Poly(new[] { V(-22f, 88f), V(-31f, 80f), V(-37f, 72f), V(-35f, 69f), V(-29f, 71f), V(-23f, 79f) }, Marble, 3.4f, 1.5f, -0.03f, gArmR);
            c.Fold(new[] { V(-12f, 96f), V(-20f, 90f), V(-28f, 87f) }, gArmR, 2, 1);
            c.Fold(new[] { V(-14f, 91f), V(-22f, 84f) }, gArmR, 2, 1);
            c.Fold(new[] { V(-25f, 82f), V(-31f, 79f) }, gArmR, 1, 1);
            c.Fold(new[] { V(-27f, 80f), V(-33f, 72f) }, gArmR, 2, 1);
            c.Capsule(V(23f, 100f), V(35f, 86f), 6f, 5f, Marble, 3.5f, 0f, gArmL).WithBump(Patterns.Lumps(4f, 1.6f, 157), 1f);
            c.Capsule(V(35f, 86f), V(46f, 80.5f), 4.2f, 3.2f, Marble, 3.6f, 0f, gArmL);
            c.Fold(new[] { V(25f, 95f), V(31f, 92f), V(36f, 89f) }, gArmL, 2, 1);
            c.Fold(new[] { V(28f, 99f), V(35f, 93f) }, gArmL, 1, 1);
            c.Fold(new[] { V(38f, 86f), V(41f, 82f) }, gArmL, 2, 1);
            // Manga ancha que cuelga bajo el antebrazo.
            c.Poly(new[] { V(34f, 84f), V(45f, 79f), V(46f, 76f), V(42f, 70f), V(38f, 72f), V(34f, 76f) }, Marble, 3.55f, 1.5f, -0.02f, gArmL);
            c.Fold(new[] { V(38f, 82f), V(39f, 74f) }, gArmL, 2, 1);
            c.Fold(new[] { V(42f, 80f), V(42.5f, 73f) }, gArmL, 2, 1);
            // Mano abierta (el gesto de la Piedad), palma hacia arriba.
            int gHandL = c.NewGroup();
            c.Ellipse(V(49.5f, 80.5f), 3.1f, 2.1f, 12f, Marble, 3.7f, 0f, gHandL);
            c.Capsule(V(48.5f, 82f), V(49.5f, 85f), 0.95f, 0.75f, Marble, 3.72f, 0f, gHandL);
            float[] fy = { 82.2f, 81.0f, 79.8f, 78.6f };
            float[] fl = { 4.4f, 4.8f, 4.4f, 3.6f };
            for (int i = 0; i < 4; i++)
                c.Capsule(V(51.5f, fy[i]), V(51.5f + fl[i], fy[i] + 1.2f - i * 0.8f), 0.8f, 0.6f, Marble, 3.71f + i * 0.001f, 0f, gHandL);

            // ---------------- El ahogado ----------------
            int gBody = c.NewGroup(), gHead = c.NewGroup(), gArmD = c.NewGroup(), gLegN = c.NewGroup(), gLegF = c.NewGroup(), gLoin = c.NewGroup();
            System.Func<float, float, float> veins = (u, v) => Mathf.Abs(PixelCanvas.ValueNoise(u / 6f, v / 5f, 0, 101) - 0.5f) < 0.045f ? 1f : 0f;
            // Pierna lejana (más oscura, detrás).
            c.Capsule(V(5f, 65f), V(27f, 62f), 4.6f, 3.9f, Corpse, 4.55f, -0.12f, gLegF);
            c.Capsule(V(27f, 62f), V(36f, 38f), 3.5f, 2.1f, Corpse, 4.54f, -0.12f, gLegF).WithTint(VeinPurple, 0.5f, veins);
            c.Capsule(V(36f, 38f), V(39.5f, 32.5f), 2.1f, 1.3f, Corpse, 4.53f, -0.12f, gLegF);
            // Torso: costillas marcadas y vientre hundido.
            c.Capsule(V(-33f, 70.5f), V(2f, 66.5f), 6.4f, 5.4f, Corpse, 4.7f, 0f, gBody)
             .WithBump((u, v) => (u > 4f && u < 18f && v > -2f ? (1f - Mathf.Abs(Mathf.Repeat(u, 2.4f) / 1.2f - 1f)) * 0.9f : 0f) - (u > 21f ? 1.2f * Bell(u, v, 26f, 1f, 5f, 3f) : 0f), 1f)
             .WithTint(VeinPurple, 0.55f, veins);
            c.Ridge(new[] { V(-27f, 75.5f), V(-12f, 74.5f) }, 1, gBody);
            // Herida del costado de la que brotan los tentáculos.
            c.Ellipse(V(-5f, 64.5f), 2.4f, 1.3f, -5f, TentacleSkin, 4.71f, -0.1f, gBody);
            // Paño de pureza (piedra) anudado a la cadera.
            c.Poly(new[] { V(-3f, 71f), V(11f, 70.5f), V(14f, 66f), V(12f, 60f), V(6f, 58f), V(0f, 62f), V(-3f, 63f) }, Marble, 4.8f, 1.5f, 0f, gLoin);
            c.Fold(new[] { V(1f, 70f), V(5f, 64f), V(6f, 59f) }, gLoin, 2, 1);
            c.Fold(new[] { V(7f, 70f), V(10f, 64f) }, gLoin, 2, 1);
            // Pierna cercana: muslo sobre la rodilla de la Madre y la pantorrilla colgando.
            c.Capsule(V(4f, 67f), V(26f, 66f), 5f, 4.2f, Corpse, 4.68f, 0f, gLegN).WithTint(VeinPurple, 0.5f, veins);
            c.Ellipse(V(26.5f, 65.5f), 3.6f, 3.4f, 0f, Corpse, 4.69f, 0f, gLegN);
            c.Capsule(V(27f, 65f), V(31f, 37f), 3.8f, 2.3f, Corpse, 4.7f, 0f, gLegN).WithTint(VeinPurple, 0.5f, veins);
            c.Capsule(V(31f, 37f), V(34f, 31f), 2.2f, 1.4f, Corpse, 4.71f, 0f, gLegN);
            c.Crease(new[] { V(28.5f, 60f), V(29.5f, 50f) }, 1, gLegN);
            // Brazo inerte que cuelga hasta la peana.
            c.Capsule(V(-30f, 69f), V(-32f, 51f), 2.9f, 2.3f, Corpse, 4.66f, 0f, gArmD).WithTint(VeinPurple, 0.5f, veins);
            c.Capsule(V(-32f, 51f), V(-31f, 35f), 2.3f, 1.8f, Corpse, 4.67f, 0f, gArmD);
            c.Capsule(V(-31f, 35f), V(-30.2f, 29.5f), 1.9f, 1.4f, Corpse, 4.68f, 0f, gArmD);
            for (int i = 0; i < 3; i++) c.Capsule(V(-31.5f + i * 1.1f, 30.5f), V(-31.8f + i * 1.3f, 27.2f), 0.6f, 0.5f, Corpse, 4.69f, 0f, gArmD);
            c.Crease(new[] { V(-32f, 52f), V(-30.5f, 51f) }, 1, gArmD);
            // Cuello y cabeza echada hacia atrás.
            c.Capsule(V(-37.5f, 68.5f), V(-32f, 70.5f), 2.6f, 3f, Corpse, 4.72f, 0f, gHead);
            c.Ellipse(V(-41.5f, 68f), 5f, 5.6f, -25f, Corpse, 4.75f, 0f, gHead).WithTint(VeinPurple, 0.45f, veins);
            c.Crease(new[] { V(-44.2f, 71.2f), V(-42.4f, 72.6f) }, 2, gHead);   // ojo cerrado
            c.Dot(V(-46f, 68.5f), -3, gHead);                                       // boca abierta
            c.Dot(V(-45.5f, 69.5f), -1, gHead);
            c.Ridge(new[] { V(-45.6f, 72.6f), V(-46.6f, 71f) }, 1, gHead);         // nariz
            // Pelo empapado que se vuelve tentáculo.
            for (int i = 0; i < 4; i++)
            {
                Vector2 h0 = V(-39f - i * 1.6f, 64.5f + i * 0.4f);
                c.Strand(new[] { h0, Add(h0, V(-1f - i * 0.4f, -4f)), Add(h0, V(-0.2f - i * 0.8f, -8f - i)), Add(h0, V(-1.6f - i, -11f - i * 1.5f)) },
                         1.1f, 0.35f, TentacleSkin, 4.73f, 0f, gHead);
            }
            // Mano de la Madre que lo sostiene por el costado (los dedos asoman sobre el pecho).
            int gHandR = c.NewGroup();
            for (int i = 0; i < 4; i++)
                c.Capsule(V(-31f + i * 2.1f, 69.5f + i * 0.2f), V(-29.6f + i * 2f, 75.8f - Mathf.Abs(i - 1.5f) * 0.6f), 0.95f, 0.75f, Marble, 5.2f + i * 0.001f, 0f, gHandR);

            // ---------------- Tentáculos que cuelgan del costado hasta la peana ----------------
            float[] tx = { -9f, -4.5f, 0.5f, 5f };
            float[] tEnd = { -24f, -9f, 10f, 22f };
            float[] tY = { 22f, 19.5f, 23.5f, 20.5f };
            for (int i = 0; i < tx.Length; i++)
            {
                int gt = c.NewGroup();
                var pts = new List<Vector2>();
                float baseY = 63.5f - (i % 2) * 0.6f;
                for (int k = 0; k <= 9; k++)
                {
                    float t = k / 9f;
                    float y = Mathf.Lerp(baseY, tY[i], t);
                    float x = Mathf.Lerp(tx[i], tEnd[i], t * t) + Mathf.Sin(t * 7f + i * 1.7f) * 2.2f * t;
                    pts.Add(V(x, y));
                }
                // La punta se enrosca sobre el canto de la peana.
                Vector2 tip = pts[pts.Count - 1];
                float curl = i % 2 == 0 ? 1f : -1f;
                pts.Add(Add(tip, V(curl * 2.4f, -1.6f)));
                pts.Add(Add(tip, V(curl * 3.6f, 0.4f)));
                c.Strand(pts, 2.9f - (i % 2) * 0.5f, 0.55f, TentacleSkin, 4.5f + i * 0.01f, 0f, gt);
                for (int k = 1; k < 9; k++)
                {
                    Vector2 d = Sub(pts[k + 1], pts[k]).normalized;
                    float r = Mathf.Lerp(2.5f, 0.8f, k / 9f);
                    c.Dot(Add(pts[k], Scale(V(d.y, -d.x), r * 0.6f)), k % 2 == 0 ? 2 : 1, gt);
                }
                c.Glint(Add(pts[2], V(-1f, 0.5f)), gt, 0);
            }
            // Dos más asoman bajo el borde del manto y reptan por la peana.
            c.Strand(new[] { V(-44f, 28f), V(-49f, 27.2f), V(-52f, 29.5f), V(-50.5f, 32f) }, 2f, 0.5f, TentacleSkin, 2.1f, 0f, c.NewGroup());
            c.Strand(new[] { V(44f, 27.5f), V(50f, 27f), V(53f, 29f), V(51.5f, 31.5f) }, 2f, 0.5f, TentacleSkin, 2.1f, 0f, c.NewGroup());

            // Grietas en la piedra de la Madre.
            c.Crease(new[] { V(-36f, 46f), V(-33f, 40f), V(-35f, 34f), V(-34f, 29f) }, 2, gSkirt);
            c.Crease(new[] { V(-33f, 40f), V(-30f, 37f) }, 1, gSkirt);
            c.Crease(new[] { V(30f, 100f), V(27f, 95f), V(28f, 90f) }, 1, gCloak);

            // ---------------- Velas votivas derretidas sobre la peana ----------------
            var flames = new List<Vector2>();
            float[] cx = { -60f, -54.5f, -49.5f, 49f, 54.5f, 60f };
            float[] ch = { 12f, 7f, 15f, 9f, 14f, 6f };
            float[] cr = { 2.3f, 1.9f, 2.4f, 2f, 2.4f, 1.8f };
            for (int i = 0; i < cx.Length; i++)
                flames.Add(Candle(c, V(cx[i], 26.2f), ch[i], cr[i], 6f + i * 0.01f, 700 + i, (i % 3 - 1) * 0.3f));
            // Cera que rebosa por el canto de la cornisa.
            int gw = c.NewGroup();
            foreach (float wx in new[] { -58f, -51f, 52f, 58.5f })
            {
                float len = 3f + PixelCanvas.Hash(Mathf.RoundToInt(wx), 1, 9) * 4f;
                c.Capsule(V(wx, 25.6f), V(wx + 0.2f, 25.6f - len), 0.85f, 0.7f, Wax, 5.9f, 0f, gw);
                c.Ellipse(V(wx + 0.2f, 25.6f - len), 1f, 0.9f, 0f, Wax, 5.9f, 0f, gw);
            }
            return Finish("estatua_madre", c, false, false, flames.ToArray());
        }

        static PropSprite Idol()
        {
            var c = Canvas(48, 80);
            int gp = c.NewGroup();
            c.Poly(new[] { V(-17f, 0f), V(17f, 0f), V(16f, 22f), V(-16f, 22f) }, StoneDark, 1f, 2.5f, 0f, gp);
            c.Poly(new[] { V(-19f, 22f), V(19f, 22f), V(18f, 26f), V(-18f, 26f) }, Stone, 1.1f, 1f, 0f, gp);
            // Glifos tallados.
            var glyph = PixelCanvas.Hex("2a3230");
            for (int i = 0; i < 5; i++)
                for (int k = 0; k < 4; k++)
                    if (PixelCanvas.Hash(i, k, 21) > 0.4f) c.Decal(-10f + i * 5f, 6f + k * 4f, glyph);

            int g = c.NewGroup();
            // Alas plegadas detrás.
            c.Poly(new[] { V(-4f, 50f), V(-18f, 70f), V(-15f, 58f), V(-20f, 56f), V(-12f, 44f) }, Soapstone, 1.5f, 1.5f, -0.12f, g);
            c.Poly(new[] { V(2f, 52f), V(-6f, 74f), V(-2f, 62f), V(-8f, 60f), V(-2f, 48f) }, Soapstone, 1.6f, 1.5f, -0.06f, g);
            // Cuerpo agazapado, patas con garras sobre el borde.
            c.Ellipse(V(-1f, 38f), 12f, 11f, 10f, Soapstone, 2f, 0f, g);
            c.Capsule(V(-8f, 32f), V(-12f, 27f), 4.5f, 3f, Soapstone, 2.2f, 0f, g);
            c.Capsule(V(6f, 32f), V(10f, 27f), 4.5f, 3f, Soapstone, 2.2f, 0f, g);
            for (int i = 0; i < 3; i++)
            {
                c.Capsule(V(10f + i * 1.8f, 27f), V(11f + i * 2.2f, 23f), 1f, 0.4f, Soapstone, 2.3f, 0f, g);
                c.Capsule(V(-12f - i * 1.8f, 27f), V(-13f - i * 2.2f, 23f), 1f, 0.4f, Soapstone, 2.25f, 0f, g);
            }
            // Cabeza de pulpo y barba de tentáculos.
            c.Ellipse(V(3f, 56f), 9.5f, 9f, -15f, Soapstone, 2.5f, 0f, g);
            c.Ellipse(V(-2f, 61f), 7f, 7f, 0f, Soapstone, 2.45f, 0f, g);
            for (int i = 0; i < 5; i++)
            {
                float x = -2f + i * 2.6f;
                var pts = new[] { V(x, 50f), V(x + 1f, 45f), V(x - 0.5f + (i % 2), 40f), V(x + 0.5f, 35f) };
                c.Strand(pts, 1.6f, 0.6f, Soapstone, 2.6f, 0f, g);
            }
            c.Ellipse(V(8f, 58f), 1.2f, 1.2f, 0f, GlowGreen, 2.7f, 0f, g);
            c.Ellipse(V(2f, 59f), 1f, 1f, 0f, GlowGreen, 2.7f, 0f, g);
            return Finish("idolo", c);
        }

        static PropSprite Chains()
        {
            var c = Canvas(16, 96, hanging: true);
            System.Func<float, float, float> rust = Patterns.Spots(2f, 0.7f, 211);
            for (int k = 0; k < 2; k++)
            {
                float x = k == 0 ? -3.5f : 3.5f;
                int links = k == 0 ? 17 : 12;
                for (int i = 0; i < links; i++)
                {
                    float y = -2.5f - i * 5f;
                    int g = c.NewGroup();
                    if (i % 2 == 0)
                    {
                        // Eslabón de frente: aro con brillo arriba a la derecha y óxido.
                        Ring(c, V(x, y), 2f, 3.1f, 1.45f, IronDark, 1f + i * 0.01f, g);
                        c.Glint(V(x + 1.6f, y + 1.6f), g, 0);
                        c.Dot(V(x - 2f, y - 1.4f), -1, g);
                    }
                    else
                    {
                        // Eslabón de canto: barra con su arista iluminada.
                        c.Capsule(V(x, y + 2.9f), V(x, y - 2.9f), 1.05f, 1.05f, IronDark, 1.5f + i * 0.01f, 0f, g, 0.2f)
                         .WithTint(PixelCanvas.Hex("6b3a22"), 0.32f, rust);
                        c.Ridge(new[] { V(x + 0.6f, y + 2.2f), V(x + 0.6f, y - 1.6f) }, 1, g);
                    }
                }
                float ye = -2.5f - links * 5f;
                int gh = c.NewGroup();
                if (k == 1)
                {
                    // Garfio al final.
                    c.Strand(new[] { V(x, ye + 2.5f), V(x, ye - 2.5f), V(x - 1f, ye - 5.5f), V(x - 3.5f, ye - 6.2f), V(x - 4.8f, ye - 4f), V(x - 4.2f, ye - 2.4f) }, 1.3f, 0.6f, IronDark, 1.6f, 0f, gh);
                    c.Glint(V(x + 0.4f, ye - 3f), gh, 0);
                }
                else
                {
                    // Eslabón partido.
                    c.Strand(new[] { V(x - 0.5f, ye + 2.5f), V(x - 1.6f, ye - 0.5f), V(x - 0.8f, ye - 2.4f) }, 0.9f, 0.6f, IronDark, 1.6f, 0f, gh);
                    c.Strand(new[] { V(x + 0.6f, ye + 2.4f), V(x + 1.6f, ye + 0.2f) }, 0.9f, 0.6f, IronDark, 1.6f, 0f, gh);
                }
            }
            return Finish("cadenas", c, hanging: true);
        }

        // Jaula colgante: cadena y argolla, cúpula de flejes remachados, barrotes con óxido y, dentro, un esqueleto
        // sentado (calavera dibujada a mano, costillar, brazo colgando entre los barrotes).
        static PropSprite Cage()
        {
            var c = Canvas(32, 56, hanging: true);
            System.Func<float, float, float> rust = Patterns.Spots(2.4f, 0.6f, 223);
            var rustColor = PixelCanvas.Hex("6e3b22");
            for (int i = 0; i < 3; i++)
            {
                int gl = c.NewGroup();
                if (i % 2 == 0) { Ring(c, V(0f, -2.5f - i * 4.2f), 1.5f, 2.3f, 1.2f, IronDark, 3f, gl); c.Glint(V(1f, -1.5f - i * 4.2f), gl, 0); }
                else c.Capsule(V(0f, -0.4f - i * 4.2f), V(0f, -4.6f - i * 4.2f), 0.9f, 0.9f, IronDark, 3.05f, 0f, gl, 0.2f);
            }
            // Barrotes traseros (más oscuros, oxidados).
            int gBack = c.NewGroup();
            for (int i = 0; i < 6; i++)
            {
                float x = -9.2f + i * (18.4f / 5f);
                c.Capsule(V(x, -21f), V(x, -52f), 0.65f, 0.65f, Rust, 0.4f, -0.14f, gBack);
            }
            // Esqueleto sentado.
            int gSk = c.NewGroup(), gSkull = c.NewGroup();
            c.Capsule(V(-1.5f, -42f), V(-2.5f, -50.5f), 0.8f, 0.8f, Bone, 1f, 0f, gSk);                       // columna
            for (int i = 0; i < 4; i++)
                c.Strand(Bezier(V(-1.6f, -43f - i * 1.9f), V(3.8f - i * 0.3f, -42.4f - i * 1.9f), V(4.2f - i * 0.4f, -45.6f - i * 1.7f), 4), 0.55f, 0.45f, Bone, 1.02f, 0f, gSk);
            c.Ellipse(V(-1.5f, -51f), 3.2f, 1.6f, 0f, Bone, 1.01f, 0f, gSk);                                   // pelvis
            c.Capsule(V(-1f, -51.5f), V(6f, -50f), 0.85f, 0.7f, Bone, 1.03f, 0f, gSk);                          // fémur
            c.Capsule(V(6f, -50f), V(5f, -54f), 0.7f, 0.6f, Bone, 1.03f, 0f, gSk);                              // tibia
            c.Capsule(V(-3.5f, -43f), V(-6.5f, -48f), 0.7f, 0.6f, Bone, 1.04f, 0f, gSk);                        // húmero
            c.Capsule(V(-6.5f, -48f), V(-11.5f, -50f), 0.6f, 0.5f, Bone, 2.2f, 0f, gSk);                        // antebrazo entre barrotes
            for (int i = 0; i < 3; i++) c.Capsule(V(-11.5f, -50f), V(-12.8f + i * 0.4f, -52.6f + i * 0.3f), 0.35f, 0.3f, Bone, 2.21f, 0f, gSk);
            c.Ellipse(V(-1f, -38.5f), 3.6f, 3.4f, 0f, Bone, 1.05f, 0f, gSkull);
            Stamp(c, -4f, -35f, Bone,
                ".2333.",
                "233443",
                "2k33k3",
                "2kk3kk",
                ".22k2.",
                "..121.");
            // Cúpula de flejes.
            int gDome = c.NewGroup();
            for (int i = 0; i < 5; i++)
            {
                float x = -11f + i * 5.5f;
                var pts = new List<Vector2>();
                for (int k = 0; k <= 6; k++)
                {
                    float t = k / 6f;
                    pts.Add(V(Mathf.Lerp(x, 0f, t) * (1f - 0.15f * Mathf.Sin(t * Mathf.PI)), -20f + Mathf.Sin(t * Mathf.PI * 0.5f) * 8.5f));
                }
                c.Strand(pts, 0.85f, 0.7f, i == 2 ? IronDark : Iron, 2f + (i == 2 ? 0.02f : 0f), Mathf.Abs(i - 2) * -0.04f, gDome);
            }
            c.Ellipse(V(0f, -11f), 2.2f, 1.6f, 0f, IronDark, 2.1f, 0f, c.NewGroup());
            // Aros remachados (arriba, centro y abajo) y barrotes delanteros.
            int gHoop = c.NewGroup();
            c.Capsule(V(-12.2f, -20.5f), V(12.2f, -20.5f), 1.4f, 1.4f, IronDark, 2.4f, 0f, gHoop, 0.4f).WithBump(Patterns.Rivets(4f, 0.9f, 0f, 1f), 1.4f).WithTint(rustColor, 0.45f, rust);
            c.Capsule(V(-12.2f, -37f), V(12.2f, -37f), 0.9f, 0.9f, IronDark, 2.4f, 0f, gHoop, 0.4f);
            c.Capsule(V(-12.6f, -52.8f), V(12.6f, -52.8f), 1.6f, 1.6f, IronDark, 2.4f, 0f, gHoop, 0.4f).WithBump(Patterns.Rivets(4f, 0.9f, 0f, 1f), 1.4f).WithTint(rustColor, 0.5f, rust);
            for (int i = 0; i < 7; i++)
            {
                float x = -11.4f + i * (22.8f / 6f);
                int gb = c.NewGroup();
                c.Capsule(V(x, -20.5f), V(x, -52.8f), 0.75f, 0.75f, IronDark, 2.2f, 0f, gb, 0.2f).WithTint(rustColor, 0.5f, rust);
                c.Ridge(new[] { V(x + 0.4f, -22f), V(x + 0.4f, -51f) }, 1, gb);
                c.Glint(V(x + 0.3f, -24f - (i % 3) * 7f), gb, 0);
            }
            // Fondo y pincho de remate.
            c.Poly(new[] { V(-11.5f, -53.5f), V(11.5f, -53.5f), V(6f, -55.6f), V(-6f, -55.6f) }, Rust, 2.3f, 1f, -0.05f, c.NewGroup());
            return Finish("jaula", c, hanging: true);
        }

        // Estandarte carmesí: barra de oro con remates de lanza, tela con pliegues, cenefa bordada en oro, el Signo
        // Antiguo bordado con filigrana de tentáculos, flecos, cola de golondrina raída y algún agujero de polilla.
        static PropSprite Banner()
        {
            var c = Canvas(32, 88, hanging: true);
            int gRod = c.NewGroup();
            c.Capsule(V(-14f, -3.5f), V(14f, -3.5f), 1.3f, 1.3f, GoldOrnate, 2f, 0f, gRod).WithBump(Patterns.Ridges(2.4f, 0.5f), 1f);
            foreach (float sx in new[] { -1f, 1f })
            {
                c.Ellipse(V(sx * 14.6f, -3.5f), 1.7f, 1.7f, 0f, GoldOrnate, 2.1f, 0f, gRod);
                c.Poly(new[] { V(sx * 15.5f, -2.3f), V(sx * 15.5f, -4.7f), V(sx * 18.8f, -3.5f) }, GoldOrnate, 2.1f, 0.8f, 0f, gRod);
            }
            c.Strand(new[] { V(-9f, -3f), V(0f, 0f), V(9f, -3f) }, 0.5f, 0.5f, Rope, 1.9f, 0f, c.NewGroup());
            c.Glint(V(6f, -2.9f), gRod, 0);
            // Silueta de la tela: cola de golondrina con el borde inferior raído y dos agujeros.
            System.Func<float, float, bool> inside = (px, py) =>
            {
                if (py > -4.5f || px < -12f || px > 12f) return false;
                float ax = Mathf.Abs(px);
                float bottom = -83f + ax * 0.2f + (ax < 6f ? (6f - ax) * 2f : 0f) + (PixelCanvas.Hash(Mathf.FloorToInt(px + 20f), 1, 301) > 0.6f ? 1.2f : 0f);
                if (py < bottom) return false;
                if ((px + 6f) * (px + 6f) + (py + 66f) * (py + 66f) < 1.3f) return false;
                if ((px - 7f) * (px - 7f) * 0.6f + (py + 73.5f) * (py + 73.5f) < 1.1f) return false;
                return true;
            };
            System.Func<float, float, float> folds = (x, y) => 1.4f * FoldWave((x + 12f) / 6f + 0.3f * Mathf.Sin(y * 0.05f));
            int gCloth = c.NewGroup();
            c.Custom(-13f, -86f, 13f, -4f, (px, py) => inside(px, py) ? (true, Cylinder(px / 26f)) : (false, N3.Front), CrimsonCloth, 1.5f, 0f, gCloth)
             .WithBump(folds, 1f)
             .WithTint(PixelCanvas.Hex("2a1418"), 0.35f, (x, y) => y < -72f && PixelCanvas.ValueNoise(x / 2f, y / 3f, 0, 307) > 0.5f ? 1f : 0f);
            // Pliegues marcados en los valles.
            foreach (float fx in new[] { -6f, 0f, 6f })
                c.Fold(new[] { V(fx - 0.2f, -6f), V(fx + 0.2f, -40f), V(fx - 0.2f, -76f) }, gCloth, 1, 1);
            // Cenefa bordada de oro a 1-2 px del borde (con pespunte oscuro) y banda superior con perlas.
            int gTrim = c.NewGroup();
            c.Custom(-13f, -86f, 13f, -4f, (px, py) =>
            {
                if (!inside(px, py)) return (false, N3.Front);
                bool edge = !inside(px - 1.5f, py) || !inside(px + 1.5f, py) || !inside(px, py - 1.5f);
                bool inner = !inside(px - 2.6f, py) || !inside(px + 2.6f, py) || !inside(px, py - 2.6f);
                bool band = py < -8f && py > -10f;
                return (!edge && (inner || band), Cylinder(px / 26f));
            }, GoldOrnate, 1.55f, -0.05f, gTrim).WithBump(folds, 1f);
            for (int i = 0; i < 7; i++) c.Dot(V(-7.5f + i * 2.5f, -9f), 1, gTrim);
            // El Signo Antiguo bordado, con la llama-ojo carmesí y filigrana de tentáculos.
            int gSign = c.NewGroup();
            c.Poly(Star(V(0f, -38f), 7.6f, 3.3f, 90f), GoldOrnate, 1.6f, 1.2f, 0f, gSign);
            c.Capsule(V(0f, -35.6f), V(0f, -40.2f), 0.9f, 0.6f, CrimsonCloth, 1.65f, 0f, gSign);
            c.Glint(V(1.5f, -34f), gSign, 0);
            for (int k = 0; k < 2; k++)
            {
                float sx = k == 0 ? -1f : 1f;
                c.Strand(new[] { V(sx * 2f, -47f), V(sx * 5f, -50f), V(sx * 7f, -48f), V(sx * 6f, -45.5f), V(sx * 4.5f, -46.5f) }, 0.55f, 0.45f, GoldOrnate, 1.6f, 0f, gSign);
                c.Strand(new[] { V(sx * 2f, -28f), V(sx * 5f, -25f), V(sx * 7f, -27f), V(sx * 6f, -29.5f) }, 0.55f, 0.45f, GoldOrnate, 1.6f, 0f, gSign);
            }
            c.Poly(new[] { V(0f, -55f), V(1.8f, -57.5f), V(0f, -60f), V(-1.8f, -57.5f) }, GoldOrnate, 1.6f, 0.8f, 0f, gSign);
            // Flecos de oro a lo largo de la cola.
            int gFr = c.NewGroup();
            for (float fx = -11f; fx <= 11f; fx += 1.5f)
            {
                float ax = Mathf.Abs(fx);
                float by = -83f + ax * 0.2f + (ax < 6f ? (6f - ax) * 2f : 0f);
                if (PixelCanvas.Hash(Mathf.RoundToInt(fx * 2f), 5, 311) < 0.25f) continue;
                c.Capsule(V(fx, by + 0.5f), V(fx + 0.2f, by - 1.8f - PixelCanvas.Hash(Mathf.RoundToInt(fx * 2f), 7, 311)), 0.4f, 0.35f, GoldOrnate, 1.45f, -0.06f, gFr);
            }
            return Finish("estandarte", c, hanging: true);
        }

        static PropSprite Coral(string name, int seed)
        {
            var c = Canvas(seed == 1 ? 34 : 40, seed == 1 ? 34 : 40);
            var mat = seed == 1 ? CoralPink : CoralOrange;
            void Branch(Vector2 from, float angle, float length, float radius, int depth, int s)
            {
                var pts = new List<Vector2> { from };
                Vector2 p = from;
                for (int i = 0; i < 4; i++)
                {
                    angle += (PixelCanvas.Hash(s, i, seed) - 0.5f) * 30f;
                    p = Add(p, Dir(angle, length / 4f));
                    pts.Add(p);
                }
                c.Strand(pts, radius, radius * 0.65f, mat, 1f + depth * 0.1f + s * 0.001f, 0f);
                if (depth >= 3) { c.Ellipse(p, radius * 0.9f, radius * 0.9f, 0f, mat, 1.5f, 0f); return; }
                Branch(p, angle + 28f, length * 0.72f, radius * 0.7f, depth + 1, s * 2 + 1);
                Branch(p, angle - 30f, length * 0.68f, radius * 0.7f, depth + 1, s * 2 + 2);
            }
            if (seed == 1)
            {
                Branch(V(0f, 0f), 95f, 12f, 3f, 0, 1);
            }
            else
            {
                // Anémona: bulbo con tentáculos y una rama de coral al lado.
                c.Ellipse(V(-4f, 6f), 9f, 6.5f, 0f, CoralRed, 1f, 0f, c.NewGroup());
                for (int i = 0; i < 9; i++)
                {
                    float a = 40f + i * 12.5f;
                    Vector2 b0 = Add(V(-4f, 10f), Dir(a, 4f));
                    var pts = new List<Vector2> { b0 };
                    Vector2 p = b0;
                    for (int k = 0; k < 4; k++)
                    {
                        p = Add(p, Dir(a + Mathf.Sin(k + i) * 20f, 3.2f));
                        pts.Add(p);
                    }
                    c.Strand(pts, 1.4f, 0.6f, CoralPink, 1.2f + i * 0.01f, 0f);
                }
                Branch(V(10f, 0f), 85f, 11f, 2.6f, 1, 3);
            }
            Rubble(c, V(0f, 0f), 18f, 4f, seed + 30);
            return Finish(name, c);
        }

        static PropSprite Ribs()
        {
            var c = Canvas(72, 48);
            int g = c.NewGroup();
            c.Strand(new[] { V(-32f, 3f), V(-10f, 5f), V(12f, 4f), V(32f, 2f) }, 3f, 2f, Bone, 1f, 0f, g);
            for (int i = 0; i < 6; i++)
            {
                float x = -26f + i * 10f;
                float h = 38f - Mathf.Abs(i - 2.5f) * 6f;
                var pts = Bezier(V(x, 4f), V(x + 3f, h + 10f), V(x + 17f, h - 6f), 8);
                c.Strand(pts, 3f, 1.1f, Bone, 1.2f + (i % 2) * 0.2f, (i % 2) * -0.1f);
            }
            Rubble(c, V(0f, 0f), 50f, 4f, 9);
            return Finish("huesos", c);
        }

        static PropSprite Lantern()
        {
            var c = Canvas(24, 72);
            int g = c.NewGroup();
            c.Capsule(V(-3f, 0f), V(-3f, 66f), 2.6f, 2.2f, Wood, 1f, 0f, g);
            c.Capsule(V(-3f, 62f), V(7f, 62f), 1.4f, 1.2f, Iron, 1.2f, 0f, g);
            c.Capsule(V(-3f, 54f), V(5f, 62f), 0.8f, 0.8f, Iron, 1.1f, 0f, g);
            c.Capsule(V(7f, 62f), V(7f, 56f), 0.6f, 0.6f, Iron, 1.2f, 0f, g);
            int gl = c.NewGroup();
            c.Poly(new[] { V(3f, 56f), V(11f, 56f), V(9f, 59f), V(5f, 59f) }, Iron, 1.5f, 1f, 0f, gl);
            c.Poly(new[] { V(3.5f, 43f), V(10.5f, 43f), V(11f, 55f), V(3f, 55f) }, GlowAmber, 1.4f, 0f, 0f, gl);
            c.Ellipse(V(7f, 48f), 2f, 3f, 0f, GlowAmberCore, 1.45f, 0f, gl);
            c.Capsule(V(3f, 43f), V(3f, 56f), 0.6f, 0.6f, Iron, 1.6f, 0f, gl);
            c.Capsule(V(11f, 43f), V(11f, 56f), 0.6f, 0.6f, Iron, 1.6f, 0f, gl);
            c.Capsule(V(7f, 43f), V(7f, 56f), 0.5f, 0.5f, Iron, 1.6f, 0f, gl);
            c.Poly(new[] { V(2f, 41f), V(12f, 41f), V(11f, 43.5f), V(3f, 43.5f) }, Iron, 1.5f, 1f, 0f, gl);
            // Cuerda enrollada en el poste.
            for (int i = 0; i < 4; i++) c.Capsule(V(-5.5f, 20f + i * 2f), V(-0.5f, 21f + i * 2f), 0.8f, 0.8f, Rope, 1.3f, 0f, g);
            return Finish("farol", c, false, false, V(7f, 46f));
        }

        static PropSprite Net()
        {
            var c = Canvas(48, 56);
            int g = c.NewGroup();
            c.Capsule(V(-18f, 0f), V(-18f, 52f), 2.4f, 2f, Wood, 1f, 0f, g);
            c.Capsule(V(-20f, 50f), V(14f, 46f), 1.4f, 1.2f, Wood, 1.1f, 0f, g);
            // Red: rombos de cuerda dentro de una forma que cuelga.
            c.Custom(-18f, 4f, 18f, 48f, (px, py) =>
            {
                float topY = 49f - (px + 18f) * 0.12f;
                float bottomY = 10f + 16f * Mathf.Abs(Mathf.Sin((px + 18f) * 0.09f)) + (px > 6f ? (px - 6f) * 1.2f : 0f);
                if (py > topY || py < bottomY || px < -16f) return (false, N3.Front);
                float a = Mathf.Repeat(px + py * 0.9f, 6f), b = Mathf.Repeat(px - py * 0.9f, 6f);
                if (a > 1.1f && b > 1.1f) return (false, N3.Front);
                return (true, Cylinder(0.3f * Mathf.Sin(px * 0.3f)));
            }, Rope, 1.2f, 0f, g);
            c.Ellipse(V(4f, 16f), 2.6f, 2.6f, 0f, PixelMaterial.Glow("2f5f5a"), 1.3f, 0f, g);
            return Finish("red", c);
        }

        static PropSprite Anchor()
        {
            var c = Canvas(40, 48);
            int g = c.NewGroup();
            float tilt = -14f;
            Vector2 R(float x, float y) => Rotate(V(x, y), V(0f, 0f), tilt);
            c.Capsule(R(0f, 2f), R(0f, 40f), 2.2f, 2f, Rust, 1f, 0f, g);
            Ring(c, R(0f, 44f), 3.4f, 3.4f, 1.8f, Rust, 1.1f, g);
            c.Capsule(R(-11f, 36f), R(11f, 36f), 1.6f, 1.6f, Rust, 1.2f, 0f, g);
            c.Strand(Bezier(R(0f, 4f), R(-14f, 2f), R(-15f, 14f), 6), 2f, 1.4f, Rust, 1.15f, 0f, g);
            c.Poly(new[] { R(-15f, 14f), R(-19f, 10f), R(-13f, 18f) }, Rust, 1.2f, 1f, 0f, g);
            c.Strand(Bezier(R(0f, 4f), R(12f, 0f), R(14f, 8f), 5), 2f, 1.4f, Rust, 1.15f, 0f, g);
            Rubble(c, V(0f, 0f), 30f, 5f, 17);
            return Finish("ancla", c);
        }

        static PropSprite ForegroundColumn()
        {
            var c = Canvas(64, 360);
            int g = c.NewGroup();
            c.Custom(-22f, 0f, 22f, 300f, (px, py) =>
            {
                if (Mathf.Abs(px) > 20f) return (false, N3.Front);
                return (true, Cylinder(px / 20f + 0.25f * Mathf.Sin(px * Mathf.PI / 4f)));
            }, Silhouette, 1f, 0f, g);
            c.Poly(new[] { V(-31f, 300f), V(31f, 300f), V(27f, 318f), V(-27f, 318f) }, Silhouette, 1.1f, 2f, 0f, g);
            c.Poly(new[] { V(-28f, 318f), V(28f, 318f), V(30f, 360f), V(-30f, 360f) }, Silhouette, 1.1f, 2f, 0f, g);
            c.Poly(new[] { V(-28f, 0f), V(28f, 0f), V(26f, 14f), V(-26f, 14f) }, Silhouette, 1.2f, 2f, 0f, g);
            // Hiedra / algas colgando.
            for (int i = 0; i < 5; i++)
            {
                float x = -18f + i * 9f;
                var pts = new List<Vector2>();
                for (int k = 0; k <= 8; k++) pts.Add(V(x + Mathf.Sin(k * 0.9f + i) * 2f, 318f - k * (8f + i * 2f)));
                c.Strand(pts, 1.4f, 0.6f, Silhouette, 1.3f, 0f);
            }
            var s = Finish("primer_plano_columna", c, false, true);
            return s;
        }

        static PropSprite ForegroundRubble()
        {
            var c = Canvas(160, 64);
            for (int i = 0; i < 9; i++)
            {
                float x = -70f + i * 17f + PixelCanvas.Hash(i, 2, 5) * 8f;
                float r = 10f + PixelCanvas.Hash(i, 4, 5) * 14f - Mathf.Abs(i - 4) * 1.5f;
                c.Poly(Blob(V(x, r * 0.6f), r * 1.2f, r, 7, i + 40, 0.3f), Silhouette, 1f + i * 0.01f, 2f);
            }
            for (int i = 0; i < 6; i++)
            {
                float x = -58f + i * 21f + (i % 2) * 5f;
                float h = 30f + (i % 3) * 7f;
                c.Strand(new[] { V(x, 16f), V(x + 3f, h * 0.7f), V(x + 7f + (i % 2) * 3f, h) }, 1.3f, 0.4f, Silhouette, 1.2f);
                c.Strand(new[] { V(x + 2f, 16f), V(x - 2f, h * 0.6f), V(x - 6f, h * 0.8f) }, 1.1f, 0.4f, Silhouette, 1.2f);
            }
            return Finish("primer_plano_escombros", c, false, true);
        }

        // ------------------------------------------------------------------
        // Objetos de juego
        // ------------------------------------------------------------------

        public static List<PropSprite> Gameplay()
        {
            var list = new List<PropSprite>
            {
                CoralSpikes(), Platform(), Gate(), Coin(), Fragment(), Orb(), ElderSign(), Tentacle(), Altar(), Inscription(), Mote(), Glow(),
            };
            return list;
        }

        static PropSprite CoralSpikes()
        {
            var c = new ShadedCanvas(32, 32, 0f, 0f);
            c.Custom(0f, 0f, 32f, 10f, (px, py) => (py < 6f + 2f * Mathf.Sin(px * Mathf.PI / 8f), Cylinder(Mathf.Sin(px * Mathf.PI / 8f) * 0.5f, 0.6f)), CoralRed, 1f);
            float[] xs = { 3f, 10f, 16.5f, 23f, 29f };
            float[] hs = { 18f, 24f, 15f, 26f, 19f };
            for (int i = 0; i < xs.Length; i++)
            {
                float lean = (i % 2 == 0 ? -1f : 1f) * 2.5f;
                c.Capsule(V(xs[i], 4f), V(xs[i] + lean, hs[i]), 3f, 0.4f, CoralRed, 1.2f + i * 0.01f, 0f);
                c.Capsule(V(xs[i] + lean * 0.75f, hs[i] - 5f), V(xs[i] + lean, hs[i]), 1.2f, 0.4f, Bone, 1.3f + i * 0.01f, 0f);
            }
            var s = Finish("coral_espinas", c);
            s.Pivot01 = new Vector2(0.5f, 0f);
            return s;
        }

        static PropSprite Platform()
        {
            var c = new ShadedCanvas(32, 16, 0f, 0f);
            c.Custom(0f, 6f, 32f, 16f, (px, py) =>
            {
                if (py < 7f || py > 15f) return (false, N3.Front);
                float plank = Mathf.Repeat(px, 16f);
                bool gap = plank < 0.8f;
                if (gap) return (false, N3.Front);
                return (true, new N3(0f, py > 13.5f ? 0.8f : (py < 8.5f ? -0.6f : 0f), 1f).Normalized());
            }, Wood, 1f);
            // Herrajes y clavos.
            c.Poly(new[] { V(5f, 7f), V(9f, 7f), V(9f, 15f), V(5f, 15f) }, Iron, 1.1f, 1f);
            c.Poly(new[] { V(21f, 7f), V(25f, 7f), V(25f, 15f), V(21f, 15f) }, Iron, 1.1f, 1f);
            c.Capsule(V(7f, 7f), V(4f, 1f), 1f, 0.7f, Wood, 0.9f, -0.1f);
            c.Capsule(V(23f, 7f), V(26f, 1f), 1f, 0.7f, Wood, 0.9f, -0.1f);
            var s = Finish("plataforma", c);
            s.Pivot01 = new Vector2(0.5f, 1f);
            return s;
        }

        static PropSprite Gate()
        {
            var c = new ShadedCanvas(32, 32, 0f, 0f);
            for (int i = 0; i < 3; i++)
            {
                float x = 5f + i * 11f;
                c.Custom(x - 2f, 0f, x + 2f, 32f, (px, py) => (Mathf.Abs(px - x) < 1.8f, Cylinder((px - x) / 1.8f)), Iron, 1f);
            }
            c.Custom(0f, 13f, 32f, 19f, (px, py) => (py > 13.5f && py < 18.5f, new N3(0f, (py - 16f) / 3f, 1f).Normalized()), Rust, 1.1f);
            for (int i = 0; i < 3; i++) c.Ellipse(V(5f + i * 11f, 16f), 1.4f, 1.4f, 0f, Iron, 1.2f);
            var s = Finish("reja", c);
            s.Pivot01 = new Vector2(0.5f, 0.5f);
            return s;
        }

        static PropSprite Coin()
        {
            var c = new ShadedCanvas(12, 12, 6f, 6f);
            c.Ellipse(V(0f, 0f), 5.4f, 5.4f, 0f, Gold, 1f);
            Ring(c, V(0f, 0f), 3.6f, 3.6f, 1f, Gold, 1.1f);
            c.Decal(-1f, 1f, PixelCanvas.Hex("fff3c0"));
            var s = Finish("moneda", c);
            s.Pivot01 = new Vector2(0.5f, 0.5f);
            return s;
        }

        static PropSprite Fragment()
        {
            var c = new ShadedCanvas(20, 28, 10f, 14f);
            var outer = PixelMaterial.Glow("7a3cc0");
            var mid = PixelMaterial.Glow("b07aff");
            var core = PixelMaterial.Glow("f0e0ff");
            c.Poly(new[] { V(0f, 13f), V(6f, 3f), V(4f, -8f), V(-1f, -13f), V(-6f, -4f), V(-5f, 6f) }, outer, 1f, 0f);
            c.Poly(new[] { V(0f, 10f), V(4f, 2f), V(2.5f, -7f), V(-1f, -10f), V(-4f, -3f), V(-3.5f, 5f) }, mid, 1.1f, 0f);
            c.Poly(new[] { V(-0.5f, 7f), V(1.5f, 1f), V(0f, -5f), V(-2f, 1f) }, core, 1.2f, 0f);
            var s = Finish("fragmento", c, false, true);
            s.Pivot01 = new Vector2(0.5f, 0.5f);
            return s;
        }

        static PropSprite RadialGlow(string name, int size, float hardness, float alphaScale)
        {
            var canvas = new PixelCanvas(size, size);
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - r) / r, dy = (y + 0.5f - r) / r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d >= 1f) continue;
                    float a = Mathf.Pow(1f - d, hardness) * alphaScale;
                    // Cuantizado en 6 escalones: halo de pixel art, no un degradado liso.
                    a = Mathf.Ceil(a * 6f) / 6f;
                    canvas.Pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
                }
            }
            return new PropSprite { Name = name, Color = canvas, Unlit = true, Pivot01 = new Vector2(0.5f, 0.5f) };
        }

        static PropSprite Orb()
        {
            var s = RadialGlow("orbe", 20, 0.6f, 1.2f);
            // Núcleo sólido.
            for (int y = 7; y < 13; y++)
                for (int x = 7; x < 13; x++)
                    if ((x - 9.5f) * (x - 9.5f) + (y - 9.5f) * (y - 9.5f) < 9f) s.Color.Pixels[y * 20 + x] = new Color32(255, 255, 255, 255);
            return s;
        }

        static PropSprite ElderSign()
        {
            var c = new ShadedCanvas(32, 32, 16f, 16f);
            var white = PixelMaterial.Glow("ffffff");
            var soft = PixelMaterial.Glow("ffffff80");
            c.Poly(Star(V(0f, 0f), 15f, 6.5f, 90f), soft, 1f, 0f);
            c.Poly(Star(V(0f, 0f), 12f, 5f, 90f), white, 1.1f, 0f);
            c.Capsule(V(0f, -3f), V(0f, 4f), 1.2f, 0.6f, PixelMaterial.Glow("ffffff00"), 1.2f);
            var s = Finish("signo_arcano", c, false, true);
            s.Pivot01 = new Vector2(0.5f, 0.5f);
            // Ojo-llama en el centro (hueco).
            for (int y = 13; y < 21; y++) s.Color.Pixels[y * 32 + 16] = new Color32(255, 255, 255, 60);
            return s;
        }

        static PropSprite Tentacle()
        {
            var c = Canvas(28, 120);
            var pts = new List<Vector2>();
            for (int k = 0; k <= 12; k++)
            {
                float t = k / 12f;
                pts.Add(V(Mathf.Sin(t * 3.4f) * 8f * t + t * t * 4f, t * 112f + 2f));
            }
            c.Strand(pts, 10f, 1.2f, TentacleSkin, 1f, 0f);
            for (int k = 1; k < 11; k++)
            {
                Vector2 d = Sub(pts[k + 1], pts[k]).normalized;
                Vector2 side = V(d.y, -d.x);
                float r = Mathf.Lerp(10f, 1.2f, k / 12f);
                c.Ellipse(Add(pts[k], Scale(side, r * 0.55f)), r * 0.28f + 0.6f, r * 0.24f + 0.5f, 0f, Sucker, 1.1f);
            }
            Rubble(c, V(0f, 0f), 24f, 5f, 61);
            return Finish("tentaculo", c);
        }

        // Altar del Signo Antiguo: zócalo y grada desconchados, fuste con columnillas en las esquinas y un panel
        // rehundido con el Signo tallado en relieve (la runa brilla), cornisa moldurada con dentículos, pila de agua
        // negra, paño carmesí con cenefa de oro y flecos, y dos velas derretidas.
        static PropSprite Altar()
        {
            var c = Canvas(48, 72);
            System.Func<float, float, float> streaks = (u, v) => PixelCanvas.ValueNoise(u / 1.6f, v / 6f, 0, 331) > 0.68f ? 1f : 0f;
            int gPl = c.NewGroup(), gStep = c.NewGroup(), gBody = c.NewGroup(), gCol = c.NewGroup(), gCor = c.NewGroup(), gTop = c.NewGroup();
            c.Poly(new[] { V(-21f, 0f), V(21f, 0f), V(21f, 3.5f), V(19.5f, 5f), V(6f, 5f), V(4.5f, 4.2f), V(3f, 5f), V(-19.5f, 5f), V(-21f, 4f) }, PedestalStone, 1f, 1.4f, -0.06f, gPl)
             .WithTint(PixelCanvas.Hex("9fae9f"), 0.2f, streaks);
            c.Poly(new[] { V(-18f, 5f), V(18f, 5f), V(17.5f, 8f), V(-17.5f, 8f) }, PedestalStone, 1.05f, 1f, 0f, gStep);
            // Fuste con panel rehundido y el Signo tallado.
            System.Func<float, float, float> panel = (x, y) =>
            {
                float ex = 8.5f - Mathf.Abs(x), ey = Mathf.Min(y - 12f, 40f - y);
                float h = ex > 0f && ey > 0f ? -Mathf.Clamp01(Mathf.Min(ex, ey)) * 1.6f : 0f;
                // Relieve de la estrella (sobresale del fondo del panel).
                float a = Mathf.Atan2(y - 26f, x) * Mathf.Rad2Deg - 90f;
                float k = Mathf.Abs(Mathf.Repeat(a + 36f, 72f) - 36f) / 36f;
                float r = Mathf.Lerp(7.6f, 3.4f, k);
                float d = Mathf.Sqrt(x * x + (y - 26f) * (y - 26f));
                if (d < r + 0.6f) h += Mathf.Clamp01(r + 0.6f - d) * 1.4f;
                return h;
            };
            c.Poly(new[] { V(-14f, 8f), V(14f, 8f), V(11.5f, 43f), V(-11.5f, 43f) }, PedestalStone, 1.1f, 1.6f, 0f, gBody)
             .WithBump(panel, 1.1f).WithTint(Verdigris, 0.2f, streaks);
            foreach (float sx in new[] { -1f, 1f })
            {
                c.Capsule(V(sx * 12.6f, 9.5f), V(sx * 10.6f, 41.5f), 1.5f, 1.3f, PedestalStone, 1.2f, 0f, gCol).WithBump(Patterns.Ridges(2.2f, 0.6f), 1f);
                c.Ellipse(V(sx * 12.6f, 9.8f), 2.4f, 1.2f, 0f, PedestalStone, 1.22f, 0f, gCol);
                c.Ellipse(V(sx * 10.6f, 41.4f), 2.4f, 1.2f, 0f, PedestalStone, 1.22f, 0f, gCol);
            }
            // La runa: brillo del Signo dentro del relieve.
            c.Poly(Star(V(0f, 26f), 6.2f, 2.6f, 90f), GlowTealDim, 1.3f, 0f, 0f, gBody);
            c.Poly(Star(V(0f, 26f), 4.4f, 1.8f, 90f), GlowTeal, 1.32f, 0f, 0f, gBody);
            c.Capsule(V(0f, 24.2f), V(0f, 28f), 0.7f, 0.4f, GlowTealHot, 1.35f, 0f, gBody);
            c.Crease(new[] { V(8f, 40f), V(6.5f, 35f), V(7.5f, 31f) }, 2, gBody);
            c.Crease(new[] { V(-9f, 18f), V(-7f, 14f), V(-8f, 10f) }, 1, gBody);
            // Cornisa: dentículos, gola y losa.
            for (float x = -15f; x <= 15f; x += 3f)
                c.Poly(new[] { V(x - 0.8f, 42.2f), V(x + 0.8f, 42.2f), V(x + 0.8f, 43.8f), V(x - 0.8f, 43.8f) }, PedestalStone, 1.3f, 0.5f, 0f, gCor);
            c.Poly(new[] { V(-17f, 43.8f), V(17f, 43.8f), V(19.5f, 47.5f), V(-19.5f, 47.5f) }, PedestalStone, 1.32f, 1f, 0f, gCor, 0f, -0.45f);
            c.Poly(new[] { V(-19.5f, 47.5f), V(19.5f, 47.5f), V(19.5f, 50.2f), V(13f, 51f), V(11f, 50.3f), V(-19f, 51f), V(-19.5f, 50.2f) }, PedestalStone, 1.34f, 1.2f, 0f, gTop, 0f, 0.2f);
            // Pila con agua negra.
            int gBowl = c.NewGroup();
            c.Ellipse(V(0f, 52.5f), 13.5f, 3.6f, 0f, PedestalStone, 1.4f, 0f, gBowl).WithBump(Patterns.Ridges(2f, 0.4f), 1f);
            c.Ellipse(V(0f, 53.6f), 11f, 1.9f, 0f, PixelMaterial.Glow("0d2421"), 1.45f, 0f, gBowl);
            c.Capsule(V(-5f, 53.8f), V(-1f, 53.8f), 0.4f, 0.4f, PixelMaterial.Glow("2f6a5e"), 1.46f, 0f, gBowl);
            // Paño carmesí colgando de la losa, con cenefa de oro y flecos.
            int gCloth = c.NewGroup();
            System.Func<float, float, bool> cloth = (x, y) => y < 50.5f && y > 37f + Mathf.Abs(x) * 0.55f && Mathf.Abs(x) < 9f;
            c.Custom(-10f, 36f, 10f, 51f, (px, py) => cloth(px, py) ? (true, Cylinder(px / 14f)) : (false, N3.Front), CrimsonCloth, 1.5f, 0f, gCloth)
             .WithBump((x, y) => FoldWave((x + 9f) / 4.5f) * 1.1f, 1f);
            c.Custom(-10f, 36f, 10f, 51f, (px, py) => cloth(px, py) && !cloth(px, py - 1.6f) ? (true, Cylinder(px / 14f)) : (false, N3.Front), GoldOrnate, 1.55f, 0f, gCloth);
            for (float fx = -8f; fx <= 8f; fx += 1.5f)
                c.Capsule(V(fx, 37.2f + Mathf.Abs(fx) * 0.55f), V(fx, 35.4f + Mathf.Abs(fx) * 0.55f), 0.35f, 0.3f, GoldOrnate, 1.56f, -0.05f, gCloth);
            c.Poly(Star(V(0f, 46f), 2.6f, 1.1f, 90f), GoldOrnate, 1.57f, 0.5f, 0f, gCloth);
            // Velas.
            var f1 = Candle(c, V(-16f, 50.8f), 8f, 1.8f, 1.7f, 901, -0.2f);
            var f2 = Candle(c, V(15.5f, 50.8f), 5f, 1.7f, 1.71f, 902, 0.2f);
            return Finish("altar", c, false, false, f1, f2);
        }

        static PropSprite Inscription()
        {
            var c = Canvas(32, 44);
            int g = c.NewGroup();
            c.Custom(-13f, 0f, 13f, 42f, (px, py) =>
            {
                float top = 32f + Mathf.Sqrt(Mathf.Max(0f, 144f - px * px)) * 0.75f;
                if (Mathf.Abs(px) > 12f || py > top) return (false, N3.Front);
                float edge = Mathf.Min(12f - Mathf.Abs(px), top - py);
                float u = edge < 2.5f ? Mathf.Sign(px) * (1f - edge / 2.5f) : 0f;
                float v = top - py < 2.5f ? 1f - (top - py) / 2.5f : 0f;
                return (true, new N3(u, v, 1f).Normalized());
            }, Stone, 1f, 0f, g);
            var carve = PixelCanvas.Hex("2a302e");
            for (int row = 0; row < 6; row++)
                for (int i = 0; i < 8; i++)
                    if (PixelCanvas.Hash(i, row, 51) > 0.3f) c.Decal(-8f + i * 2.2f, 30f - row * 4.2f, carve);
            Rubble(c, V(0f, 0f), 24f, 3f, 71);
            return Finish("inscripcion", c);
        }

        static PropSprite Mote()
        {
            var canvas = new PixelCanvas(3, 3);
            var w = new Color32(255, 255, 255, 255);
            var a = new Color32(255, 255, 255, 110);
            canvas.Pixels[4] = w;
            canvas.Pixels[1] = a; canvas.Pixels[3] = a; canvas.Pixels[5] = a; canvas.Pixels[7] = a;
            return new PropSprite { Name = "mota", Color = canvas, Unlit = true, Pivot01 = new Vector2(0.5f, 0.5f) };
        }

        static PropSprite Glow() => RadialGlow("brillo", 64, 2.2f, 1f);

        // ------------------------------------------------------------------
        // Animaciones de decorado
        // ------------------------------------------------------------------

        public static List<PropAnimation> Animated() => new List<PropAnimation>
        {
            Flame("llama", 6, 12, 6), Flame("llama_grande", 10, 18, 6), Water(), DeepWater(), LightShaft(),
        };

        static PropAnimation Flame(string name, int w, int h, int frames)
        {
            var anim = new PropAnimation { Name = name, Fps = 10f, Unlit = true, Pivot01 = new Vector2(0.5f, 0f) };
            var outer = PixelCanvas.Hex("e8642a");
            var mid = PixelCanvas.Hex("ffae44");
            var core = PixelCanvas.Hex("fff2c4");
            var blue = PixelCanvas.Hex("5a7aff");
            for (int f = 0; f < frames; f++)
            {
                var canvas = new PixelCanvas(w, h);
                float ph = f / (float)frames * Mathf.PI * 2f;
                float height = h * (0.86f + 0.12f * Mathf.Sin(ph * 2f + 0.5f));
                float sway = Mathf.Sin(ph) * w * 0.12f;
                float cx = w * 0.5f;
                for (int y = 0; y < h; y++)
                {
                    float t = y / height;
                    if (t > 1f) continue;
                    // Gota: ancha abajo, punta arriba que se balancea.
                    float half = w * 0.48f * Mathf.Sin(Mathf.PI * Mathf.Pow(Mathf.Clamp01(t * 0.95f + 0.05f), 0.65f)) * (1f - 0.2f * t);
                    float center = cx + sway * t * t;
                    for (int x = 0; x < w; x++)
                    {
                        float dx = Mathf.Abs(x + 0.5f - center);
                        if (dx > half) continue;
                        float k = dx / Mathf.Max(0.5f, half);
                        Color32 col = k > 0.7f || t > 0.82f ? outer : (k > 0.35f || t > 0.6f ? mid : core);
                        if (t < 0.12f && k > 0.3f) col = blue;
                        canvas.Pixels[y * w + x] = col;
                    }
                }
                anim.Frames.Add(canvas);
            }
            return anim;
        }

        static PropAnimation Water()
        {
            var anim = new PropAnimation { Name = "agua", Fps = 8f, Pivot01 = new Vector2(0.5f, 0.5f), Unlit = false };
            anim.Normals = new List<PixelCanvas>();
            var deep = PixelCanvas.Hex("061a1c");
            var body = PixelCanvas.Hex("0c2e30");
            var band = PixelCanvas.Hex("16494a");
            var crest = PixelCanvas.Hex("4fa89a");
            var foam = PixelCanvas.Hex("bfe8d8");
            for (int f = 0; f < 8; f++)
            {
                var canvas = new PixelCanvas(32, 32);
                var normal = new PixelCanvas(32, 32);
                float ph = f / 8f * Mathf.PI * 2f;
                for (int x = 0; x < 32; x++)
                {
                    float a = x / 32f * Mathf.PI * 2f;
                    float surface = 27f + 1.6f * Mathf.Sin(a + ph) + 0.8f * Mathf.Sin(a * 2f - ph * 2f);
                    for (int y = 0; y < 32; y++)
                    {
                        if (y > surface) continue;
                        float depth = surface - y;
                        Color32 col = depth < 1f ? (Mathf.Sin(a * 3f + ph) > 0.4f ? foam : crest) : depth < 3f ? band : depth < 12f ? body : deep;
                        // Reflejos ondulados en el cuerpo del agua.
                        if (depth > 3f && depth < 20f && Mathf.Abs(Mathf.Sin(a * 2f + y * 0.45f - ph)) > 0.97f) col = band;
                        col.a = (byte)(depth < 3f ? 240 : 225);
                        canvas.Pixels[y * 32 + x] = col;
                        float slope = Mathf.Cos(a + ph) * 0.5f + Mathf.Cos(a * 2f - ph * 2f) * 0.5f;
                        normal.Pixels[y * 32 + x] = depth < 3f
                            ? new Color32((byte)(128 - slope * 60f), 200, 200, 255)
                            : new Color32(128, 128, 255, 255);
                    }
                }
                anim.Frames.Add(canvas);
                anim.Normals.Add(normal);
            }
            return anim;
        }

        static PropAnimation DeepWater()
        {
            var anim = new PropAnimation { Name = "agua_profunda", Fps = 5f, Pivot01 = new Vector2(0.5f, 0.5f), Unlit = true };
            var deep = PixelCanvas.Hex("051416");
            var body = PixelCanvas.Hex("0a2426");
            var glint = PixelCanvas.Hex("1f5a52");
            for (int f = 0; f < 4; f++)
            {
                var canvas = new PixelCanvas(32, 32);
                for (int y = 0; y < 32; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        float n = PixelCanvas.ValueNoise(x / 6f + f * 0.9f, y / 5f, 32 / 6, 3);
                        Color32 col = n > 0.62f ? body : deep;
                        if (PixelCanvas.Hash(x, y + f * 7, 5) > 0.992f) col = glint;
                        col.a = 235;
                        canvas.Pixels[y * 32 + x] = col;
                    }
                }
                anim.Frames.Add(canvas);
            }
            return anim;
        }

        static PropAnimation LightShaft()
        {
            var anim = new PropAnimation { Name = "rayo_luz", Fps = 4f, Unlit = true, Pivot01 = new Vector2(0.5f, 1f) };
            const int w = 96, h = 200;
            for (int f = 0; f < 4; f++)
            {
                var canvas = new PixelCanvas(w, h);
                for (int y = 0; y < h; y++)
                {
                    float t = 1f - y / (float)h; // 0 arriba, 1 abajo
                    float center = w * 0.64f - t * w * 0.3f;
                    float half = 14f + t * 26f;
                    for (int x = 0; x < w; x++)
                    {
                        float d = Mathf.Abs(x + 0.5f - center) / half;
                        if (d > 1f) continue;
                        float a = Mathf.Sqrt(1f - d) * (1f - t * 0.8f) * 0.34f;
                        a = Mathf.Floor(a * 8f) / 8f;
                        if (a <= 0f) continue;
                        canvas.Pixels[y * w + x] = new Color32(255, 236, 190, (byte)Mathf.RoundToInt(a * 255f));
                    }
                }
                // Polvo en suspensión que sube despacio.
                for (int i = 0; i < 14; i++)
                {
                    float px = PixelCanvas.Hash(i, 1, 33) * w;
                    float py = Mathf.Repeat(PixelCanvas.Hash(i, 2, 33) * h + f * 3f, h);
                    int ix = Mathf.FloorToInt(px), iy = Mathf.FloorToInt(py);
                    if (canvas.Get(ix, iy).a > 20) canvas.Set(ix, iy, new Color32(255, 248, 220, 200));
                }
                anim.Frames.Add(canvas);
            }
            return anim;
        }
    }
}
