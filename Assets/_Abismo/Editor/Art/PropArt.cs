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
        static readonly PixelMaterial Wax = new PixelMaterial(Ramp.Make("cfc4a0", 4, 0.08f, 0.45f, 1.25f)) { Rim = 0.5f, Ambient = 0.35f, Dither = 0f };
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

        static PropSprite Candelabra()
        {
            var c = Canvas(40, 72);
            int g = c.NewGroup();
            // Pie de trípode con garras.
            c.Capsule(V(0f, 7f), V(-12f, 1.5f), 2f, 1.3f, Gold, 1f, -0.1f, g);
            c.Capsule(V(0f, 7f), V(12f, 1.5f), 2f, 1.3f, Gold, 1f, -0.1f, g);
            c.Capsule(V(0f, 7f), V(2f, 1f), 2.2f, 1.5f, Gold, 1.2f, 0f, g);
            c.Ellipse(V(0f, 8.5f), 6.5f, 2.6f, 0f, Gold, 1.3f, 0f, g);
            // Fuste con nudos.
            c.Capsule(V(0f, 9f), V(0f, 47f), 1.6f, 1.3f, Gold, 1.4f, 0f, g);
            c.Ellipse(V(0f, 17f), 3.2f, 2.4f, 0f, Gold, 1.5f, 0f, g);
            c.Ellipse(V(0f, 30f), 2.6f, 2f, 0f, Gold, 1.5f, 0f, g);
            c.Ellipse(V(0f, 44f), 3.6f, 2.6f, 0f, Gold, 1.5f, 0f, g);
            // Brazos curvos y platillos.
            var flames = new List<Vector2>();
            float[] xs = { -14f, -7f, 0f, 7f, 14f };
            float[] cupY = { 52f, 49.5f, 56f, 49.5f, 52f };
            float[] candle = { 9f, 6f, 11f, 7.5f, 8f };
            for (int i = 0; i < 5; i++)
            {
                if (i != 2) c.Strand(Bezier(V(0f, 45f), V(xs[i] * 0.7f, 43f), V(xs[i], cupY[i]), 6), 1.3f, 1f, Gold, 1.45f, 0f, g);
                c.Ellipse(V(xs[i], cupY[i]), 3f, 1.3f, 0f, Gold, 1.6f, 0f, g);
                Vector2 top = V(xs[i] + (i % 2 == 0 ? 0f : 0.3f), cupY[i] + candle[i]);
                c.Capsule(V(xs[i], cupY[i] + 1f), top, 1.5f, 1.4f, Wax, 1.7f, 0f, c.NewGroup());
                if (i % 2 == 0) c.Capsule(Add(top, V(1.2f, -1.5f)), Add(top, V(1.6f, -4.5f)), 0.7f, 0.5f, Wax, 1.75f);
                flames.Add(Add(top, V(0f, 1f)));
            }
            return Finish("candelabro", c, false, false, flames.ToArray());
        }

        static PropSprite FloorCandles()
        {
            var c = Canvas(36, 18);
            c.Ellipse(V(0f, 1.5f), 16f, 2.4f, 0f, Wax, 0.5f, -0.1f, c.NewGroup());
            float[] xs = { -11f, -5.5f, 0.5f, 6f, 11.5f };
            float[] hs = { 10f, 6.5f, 13f, 5f, 8.5f };
            float[] rs = { 1.9f, 1.6f, 2.2f, 1.7f, 1.8f };
            var flames = new List<Vector2>();
            for (int i = 0; i < 5; i++)
            {
                int g = c.NewGroup();
                Vector2 top = V(xs[i], 1f + hs[i]);
                c.Capsule(V(xs[i], 1.5f), top, rs[i] + 0.4f, rs[i], Wax, 1f + (i % 2) * 0.3f, 0f, g);
                c.Capsule(Add(top, V(-rs[i] * 0.6f, -1f)), Add(top, V(-rs[i] * 0.8f, -hs[i] * 0.5f)), 0.7f, 0.9f, Wax, 1.05f + (i % 2) * 0.3f, 0f, g);
                flames.Add(Add(top, V(0f, 1f)));
            }
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

        // La Madre de las Profundidades: una sacerdotisa velada de piedra, sentada, con un ahogado en el regazo
        // del que cuelgan raíces y tentáculos hasta el suelo; halo de espinas y velas votivas en la peana.
        static PropSprite MotherStatue()
        {
            var c = Canvas(140, 150);
            // Peana escalonada.
            int gp = c.NewGroup();
            c.Poly(new[] { V(-62f, 0f), V(62f, 0f), V(62f, 8f), V(-62f, 8f) }, StoneDark, 1f, 2f, 0f, gp);
            c.Poly(new[] { V(-56f, 8f), V(56f, 8f), V(55f, 17f), V(-55f, 17f) }, Stone, 1.1f, 2.5f, 0f, gp);
            c.Poly(new[] { V(-58f, 17f), V(58f, 17f), V(57f, 20f), V(-57f, 20f) }, StoneGreen, 1.2f, 1f, 0f, gp);

            // Halo de espinas detrás de la cabeza.
            int gh = c.NewGroup();
            Vector2 head = V(6f, 118f);
            Ring(c, Add(head, V(-3f, 3f)), 20f, 20f, 4f, StoneDark, 0.6f, gh);
            for (int i = 0; i < 14; i++)
            {
                float a = 90f + (i - 6.5f) * 19f;
                Vector2 b0 = Add(Add(head, V(-3f, 3f)), Dir(a, 21f));
                c.Capsule(b0, Add(b0, Dir(a, 7f + (i % 2) * 4f)), 1.6f, 0.3f, StoneDark, 0.55f, 0f, gh);
            }

            // Cuerpo sentado bajo el manto: falda hasta la peana y torso inclinado sobre el regazo.
            int gb = c.NewGroup();
            c.Custom(-50f, 20f, 46f, 112f, (px, py) =>
            {
                // Silueta del manto: hombros a la altura 105, se ensancha hacia la peana.
                float t = Mathf.InverseLerp(108f, 20f, py);
                if (t < 0f || t > 1f) return (false, N3.Front);
                float left = Mathf.Lerp(-16f, -48f, Mathf.Pow(t, 0.8f)) + 2f * Mathf.Sin(py * 0.35f);
                float right = Mathf.Lerp(22f, 44f, Mathf.Pow(t, 1.4f));
                if (px < left || px > right) return (false, N3.Front);
                float u = (px - (left + right) * 0.5f) / ((right - left) * 0.5f);
                float folds = 0.3f * t * Mathf.Sin(px * 0.45f + py * 0.08f);
                return (true, Cylinder(u * 0.85f + folds, 0.25f * (1f - t)));
            }, StoneGreen, 2f, 0f, gb);
            // Velo sobre la cabeza que cae por la espalda.
            c.Ellipse(head, 12f, 13.5f, -18f, StoneGreen, 3.2f, 0f, gb);
            c.Custom(-22f, 70f, 10f, 128f, (px, py) =>
            {
                float t = Mathf.InverseLerp(126f, 72f, py);
                if (t < 0f || t > 1f) return (false, N3.Front);
                float left = Mathf.Lerp(-6f, -22f, t);
                float right = Mathf.Lerp(4f, -6f, t);
                if (px < left || px > right) return (false, N3.Front);
                float u = (px - (left + right) * 0.5f) / Mathf.Max(1f, (right - left) * 0.5f);
                return (true, Cylinder(u * 0.8f + 0.25f * Mathf.Sin(py * 0.5f), 0.2f));
            }, StoneGreen, 2.6f, -0.04f, gb);
            // Rostro en sombra bajo el velo (mira al ahogado).
            c.Ellipse(Add(head, V(6f, -4f)), 4.2f, 6f, -25f, StoneDark, 3.3f, -0.05f, gb);
            c.Capsule(Add(head, V(9f, 0f)), Add(head, V(10f, -6f)), 1.2f, 1f, StoneGreen, 3.35f, 0f, gb);

            // El ahogado en el regazo: cabeza caída a la izquierda, piernas colgando a la derecha.
            int gd = c.NewGroup();
            Vector2 dHead = V(-37f, 64f), dChest = V(-16f, 71f), dHip = V(12f, 66f), dKnee = V(30f, 62f), dFoot = V(36f, 30f);
            c.Ellipse(dHead, 6.5f, 6f, 30f, Flesh, 4.6f, 0f, gd);
            c.Capsule(Add(dHead, V(-4f, -3f)), Add(dHead, V(-7f, -10f)), 2.2f, 1.2f, TentacleSkin, 4.62f, 0f, gd);
            c.Capsule(dChest, dHip, 8.5f, 7f, Flesh, 4.1f, 0f, gd);
            c.Capsule(Add(dHead, V(4f, 2f)), dChest, 3f, 4.5f, Flesh, 4.58f, 0f, gd);
            c.Capsule(dHip, dKnee, 5.5f, 4.5f, Flesh, 4.2f, 0f, gd);
            c.Capsule(dKnee, dFoot, 4.4f, 3f, Flesh, 4.25f, 0f, gd);
            c.Capsule(dFoot, Add(dFoot, V(5f, -3f)), 2.6f, 2f, Flesh, 4.26f, 0f, gd);
            // Brazo del ahogado colgando hasta la peana.
            c.Strand(new[] { V(-18f, 68f), V(-22f, 52f), V(-24f, 36f), V(-25f, 25f) }, 3f, 2.2f, Flesh, 4.3f, 0f, gd);
            // Paño sobre la cadera.
            c.Poly(new[] { V(2f, 74f), V(22f, 70f), V(24f, 58f), V(16f, 54f), V(4f, 59f) }, StoneGreen, 4.35f, 2f, 0f, gd);
            // Venas púrpura en la carne.
            var vein = PixelCanvas.Hex("6a3a6e");
            for (int i = 0; i < 12; i++)
            {
                Vector2 q = Mix(dChest, dHip, i / 12f);
                c.Decal(q.x + (i % 3) - 1f, q.y + 2f - (i % 4), vein);
            }
            // Raíces y tentáculos que brotan del cuerpo y cuelgan hasta el suelo.
            float[] rx = { -34f, -27f, -10f, 4f, 18f, 24f };
            for (int i = 0; i < rx.Length; i++)
            {
                var pts = new List<Vector2>();
                Vector2 a = V(rx[i], i < 2 ? 64f : 62f);
                pts.Add(a);
                for (int k = 1; k <= 7; k++)
                {
                    a = Add(a, V(Mathf.Sin(k * 1.3f + i) * 2.2f, -(a.y - 20f) / (8f - k)));
                    pts.Add(a);
                }
                c.Strand(pts, 2.4f - (i % 2) * 0.6f, 0.7f, TentacleSkin, 4.4f + i * 0.01f, 0f, c.NewGroup());
            }

            // Manos de la Madre: una bajo los hombros del ahogado, otra sobre su pecho.
            int ga = c.NewGroup();
            c.Capsule(V(10f, 96f), V(-10f, 80f), 5f, 4f, StoneGreen, 4.5f, 0f, ga);
            c.Capsule(V(-10f, 80f), V(-20f, 74f), 4f, 3f, StoneGreen, 4.55f, 0f, ga);
            c.Capsule(V(24f, 98f), V(20f, 82f), 5f, 4f, StoneGreen, 3.9f, -0.03f, ga);
            c.Capsule(V(20f, 82f), V(4f, 78f), 4f, 3f, StoneGreen, 4.6f, 0f, ga);

            // Velas votivas en la peana.
            var flames = new List<Vector2>();
            float[] cx = { -50f, -43f, 40f, 47f, 53f };
            float[] ch = { 9f, 6f, 7f, 11f, 5f };
            for (int i = 0; i < cx.Length; i++)
            {
                Vector2 top = V(cx[i], 20f + ch[i]);
                c.Capsule(V(cx[i], 20f), top, 1.8f, 1.6f, Wax, 5f, 0f, c.NewGroup());
                flames.Add(Add(top, V(0f, 1f)));
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
            for (int k = 0; k < 2; k++)
            {
                float x = k == 0 ? -3.5f : 3.5f;
                int links = k == 0 ? 17 : 12;
                for (int i = 0; i < links; i++)
                {
                    float y = -2.5f - i * 5f;
                    if (i % 2 == 0) Ring(c, V(x, y), 2f, 3f, 1.4f, Iron, 1f + i * 0.01f);
                    else c.Capsule(V(x, y + 2.6f), V(x, y - 2.6f), 1f, 1f, Iron, 1.5f + i * 0.01f);
                }
                if (k == 1)
                {
                    float y = -2.5f - links * 5f;
                    c.Strand(new[] { V(x, y + 2f), V(x, y - 3f), V(x - 2.5f, y - 6f), V(x - 4.5f, y - 3.5f) }, 1.2f, 0.8f, Iron, 1.6f);
                }
            }
            return Finish("cadenas", c, hanging: true);
        }

        static PropSprite Cage()
        {
            var c = Canvas(32, 56, hanging: true);
            for (int i = 0; i < 3; i++) Ring(c, V(0f, -2.5f - i * 4.5f), 1.5f, 2.3f, 1.2f, Iron, 1f);
            int g = c.NewGroup();
            // Esqueleto dentro (detrás de los barrotes delanteros).
            c.Ellipse(V(-2f, -38f), 4.2f, 3.8f, 0f, Bone, 1f, 0f, g);
            c.Ellipse(V(-0.5f, -40.5f), 1f, 1.1f, 0f, PixelMaterial.Glow("120c0a"), 1.05f, 0f, g);
            for (int i = 0; i < 4; i++) c.Strand(Bezier(V(-1f, -43f - i * 2.4f), V(4f, -42f - i * 2.4f), V(5f, -46f - i * 2f), 4), 0.7f, 0.5f, Bone, 0.95f, 0f, g);
            c.Capsule(V(-1f, -42f), V(-2f, -51f), 0.9f, 0.9f, Bone, 0.96f, 0f, g);
            c.Capsule(V(3f, -45f), V(9f, -51f), 0.8f, 0.6f, Bone, 0.97f, 0f, g);
            // Cúpula, aros y barrotes.
            c.Custom(-12f, -20f, 12f, -12f, (px, py) =>
            {
                float d = (px * px) / 144f + ((py + 20f) * (py + 20f)) / 49f;
                if (d > 1f || py < -20f || d < 0.6f) return (false, N3.Front);
                return (true, Cylinder(px / 12f, 0.5f));
            }, Iron, 2f);
            Ring(c, V(0f, -20f), 12f, 2.2f, 1.6f, Iron, 2.1f);
            Ring(c, V(0f, -53f), 12f, 2.2f, 1.8f, Rust, 2.1f);
            for (int i = 0; i < 7; i++)
            {
                float x = -11f + i * (22f / 6f);
                bool front = i % 2 == 0;
                c.Capsule(V(x, -20f), V(x, -53f), 0.8f, 0.8f, front ? Iron : Rust, front ? 2f : 0.5f, front ? 0f : -0.12f);
            }
            return Finish("jaula", c, hanging: true);
        }

        static PropSprite Banner()
        {
            var c = Canvas(32, 88, hanging: true);
            int g = c.NewGroup();
            c.Capsule(V(-15f, -3f), V(15f, -3f), 1.4f, 1.4f, Gold, 2f, 0f, g);
            c.Ellipse(V(-15.5f, -3f), 2f, 2f, 0f, Gold, 2.1f, 0f, g);
            c.Ellipse(V(15.5f, -3f), 2f, 2f, 0f, Gold, 2.1f, 0f, g);
            c.Strand(new[] { V(0f, 0f), V(0f, -3f) }, 0.8f, 0.8f, Gold, 2f, 0f, g);
            // Tela con pliegues y cola de golondrina.
            System.Func<float, float, bool> inside = (px, py) =>
            {
                if (py > -4f || px < -12f || px > 12f) return false;
                float tail = -70f - Mathf.Abs(Mathf.Abs(px) - 6f) * -1.4f;
                float bottom = -82f + Mathf.Abs(px) * 0.9f - (Mathf.Abs(px) < 6f ? (6f - Mathf.Abs(px)) * 1.7f : 0f);
                return py > Mathf.Max(bottom, -84f) && py > tail - 14f;
            };
            c.Custom(-13f, -86f, 13f, -4f, (px, py) =>
            {
                if (!inside(px, py)) return (false, N3.Front);
                bool edge = !inside(px - 2f, py) || !inside(px + 2f, py) || !inside(px, py - 2f);
                if (edge) return (false, N3.Front);
                return (true, Cylinder(0.55f * Mathf.Sin(px * 0.32f + 0.6f) + px / 30f));
            }, Crimson, 1.5f, 0f, g);
            c.Custom(-13f, -86f, 13f, -4f, (px, py) =>
            {
                if (!inside(px, py)) return (false, N3.Front);
                return (true, Cylinder(0.55f * Mathf.Sin(px * 0.32f + 0.6f) + px / 30f));
            }, Gold, 1.45f, 0f, g);
            // El Signo Antiguo bordado.
            c.Poly(Star(V(0f, -36f), 7.5f, 3.2f, 90f), Gold, 1.6f, 1f, 0f, g);
            c.Capsule(V(0f, -34f), V(0f, -38.5f), 0.8f, 0.6f, Crimson, 1.65f, 0f, g);
            c.Capsule(V(-10f, -16f), V(10f, -16f), 0.6f, 0.6f, Gold, 1.6f, 0f, g);
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

        static PropSprite Altar()
        {
            var c = Canvas(48, 72);
            int g = c.NewGroup();
            c.Poly(new[] { V(-20f, 0f), V(20f, 0f), V(19f, 6f), V(-19f, 6f) }, StoneDark, 1f, 2f, 0f, g);
            c.Poly(new[] { V(-14f, 6f), V(14f, 6f), V(11f, 44f), V(-11f, 44f) }, Stone, 1.1f, 3f, 0f, g);
            c.Poly(new[] { V(-19f, 44f), V(19f, 44f), V(17f, 51f), V(-17f, 51f) }, Stone, 1.2f, 2f, 0f, g);
            // Pila con agua oscura.
            c.Ellipse(V(0f, 53f), 16f, 4f, 0f, StoneDark, 1.3f, 0f, g);
            c.Ellipse(V(0f, 54f), 12.5f, 2.2f, 0f, PixelMaterial.Glow("0f2a26"), 1.35f, 0f, g);
            // El Signo Antiguo grabado (emisivo) en el frente.
            c.Poly(Star(V(0f, 26f), 9f, 3.9f, 90f), GlowTealDim, 1.4f, 0f, 0f, g);
            c.Poly(Star(V(0f, 26f), 6.5f, 2.6f, 90f), GlowTeal, 1.45f, 0f, 0f, g);
            c.Capsule(V(0f, 23.5f), V(0f, 28.5f), 0.8f, 0.5f, GlowTealHot, 1.5f, 0f, g);
            // Velas a los lados.
            c.Capsule(V(-16f, 51f), V(-16f, 59f), 1.7f, 1.5f, Wax, 1.6f, 0f, c.NewGroup());
            c.Capsule(V(15f, 51f), V(15f, 56f), 1.7f, 1.5f, Wax, 1.6f, 0f, c.NewGroup());
            return Finish("altar", c, false, false, V(-16f, 60f), V(15f, 57f));
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
