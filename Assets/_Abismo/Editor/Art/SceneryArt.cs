using System.Collections.Generic;
using UnityEngine;
using static Abismo.EditorTools.Rig;

namespace Abismo.EditorTools
{
    /// <summary>
    /// Decorado de la catedral sumergida, versión lovecraftiana de la catedral de Blasphemous:
    /// pilas de cadáveres (ahogados, peces y Profundos) atravesados por arpones y picas, vidrieras góticas cuyo
    /// vidrio brilla en la capa emisiva (el Ojo, Dagón, el Signo Antiguo y el Pez), un rosetón y el haz de luz
    /// polvoriento que cae de las ventanas.
    /// </summary>
    public static class SceneryArt
    {
        // ------------------------------------------------------------------
        // Materiales (mismo estilo que PropArt)
        // ------------------------------------------------------------------

        static PixelMaterial M(string hex, int steps, float hue, float dark, float light) => new PixelMaterial(Ramp.Make(hex, steps, hue, dark, light));

        // Carne de ahogado: gris verdosa hinchada y violácea, con manchas.
        static readonly PixelMaterial SkinGreen = new PixelMaterial(Ramp.Make("70766a", 5, 0.1f, 0.26f, 1.62f)) { Rim = 0.65f, Ambient = 0.2f, Dither = 0.03f, Texture = Flesh };
        static readonly PixelMaterial SkinViolet = new PixelMaterial(Ramp.Make("67636b", 5, 0.1f, 0.26f, 1.62f)) { Rim = 0.65f, Ambient = 0.2f, Dither = 0.03f, Texture = Flesh };
        static readonly PixelMaterial SkinGrey = new PixelMaterial(Ramp.Make("62645b", 5, 0.1f, 0.26f, 1.62f)) { Rim = 0.65f, Ambient = 0.18f, Dither = 0.03f, Texture = Flesh };
        static readonly PixelMaterial Hair = new PixelMaterial(Ramp.Make("2f2a2b", 4, 0.08f, 0.45f, 1.75f)) { Rim = 0.6f, Gloss = 0.3f, Ambient = 0.3f, Dither = 0f };
        static readonly PixelMaterial Socket = new PixelMaterial(new[] { PixelCanvas.Hex("1c1418"), PixelCanvas.Hex("261c22"), PixelCanvas.Hex("2e2228") }) { Ambient = 0.6f, Rim = 0f, Dither = 0f, Outline = false, ReceivesContactShadow = false };
        static readonly PixelMaterial Teeth = new PixelMaterial(Ramp.Make("b8b39a", 3, 0.06f, 0.6f, 1.15f)) { Ambient = 0.5f, Rim = 0f, Dither = 0f, Outline = false, ReceivesContactShadow = false };
        // Harapos pardos y oliva.
        static readonly PixelMaterial Rag = new PixelMaterial(Ramp.Make("5a4434", 5, 0.08f, 0.28f, 1.5f)) { Rim = 0.5f, Ambient = 0.24f, Dither = 0.03f, Texture = Weave };
        static readonly PixelMaterial RagOlive = new PixelMaterial(Ramp.Make("4d4a38", 5, 0.08f, 0.28f, 1.5f)) { Rim = 0.5f, Ambient = 0.24f, Dither = 0.03f, Texture = Weave };
        static readonly PixelMaterial RagDark = new PixelMaterial(Ramp.Make("45372f", 5, 0.08f, 0.3f, 1.55f)) { Rim = 0.5f, Ambient = 0.22f, Dither = 0.03f, Texture = Weave };
        // Hierro oxidado de arpones, picas y cadenas.
        static readonly PixelMaterial Iron = new PixelMaterial(Ramp.Make("46454a", 5, 0.06f, 0.28f, 1.95f)) { Gloss = 0.55f, Rim = 0.6f, Ambient = 0.22f, Dither = 0f, Texture = RustSpots };
        static readonly PixelMaterial Rope = new PixelMaterial(Ramp.Make("7a6648", 4, 0.06f, 0.4f, 1.3f)) { Rim = 0.4f, Ambient = 0.3f, Dither = 0f };
        static readonly PixelMaterial Cork = new PixelMaterial(Ramp.Make("8a6a42", 4, 0.08f, 0.4f, 1.35f)) { Rim = 0.5f, Ambient = 0.3f, Dither = 0f };
        static readonly PixelMaterial NetRope = new PixelMaterial(Ramp.Make("807054", 4, 0.06f, 0.45f, 1.3f)) { Rim = 0.3f, Ambient = 0.4f, Dither = 0f, ReceivesContactShadow = false };
        // Sangre seca, oscura.
        static readonly PixelMaterial Blood = new PixelMaterial(Ramp.Make("4c1518", 4, 0.06f, 0.45f, 1.4f)) { Gloss = 0.15f, Rim = 0.3f, Ambient = 0.3f, Dither = 0f, Outline = false };
        // Relleno oscuro entre los cuerpos (lo que no se ve de la pila).
        static readonly PixelMaterial Mass = new PixelMaterial(Ramp.Make("352f2d", 4, 0.1f, 0.4f, 1.35f)) { Rim = 0.4f, Ambient = 0.2f, Dither = 0.05f, Texture = Mottle };
        // Peces muertos.
        static readonly PixelMaterial FishBack = new PixelMaterial(Ramp.Make("40494c", 5, 0.1f, 0.28f, 1.9f)) { Gloss = 0.45f, Rim = 0.6f, Ambient = 0.22f, Dither = 0f, Texture = Scales };
        static readonly PixelMaterial FishBelly = new PixelMaterial(Ramp.Make("7d8078", 5, 0.08f, 0.3f, 1.45f)) { Gloss = 0.35f, Rim = 0.5f, Ambient = 0.28f, Dither = 0f, Texture = Scales };
        static readonly PixelMaterial FishFin = new PixelMaterial(Ramp.Make("5a4a52", 4, 0.08f, 0.32f, 1.55f)) { Rim = 0.6f, Ambient = 0.28f, Dither = 0f };
        static readonly PixelMaterial FishEye = new PixelMaterial(Ramp.Make("b9bcae", 3, 0.04f, 0.6f, 1.2f)) { Gloss = 0.6f, Ambient = 0.5f, Rim = 0f, Dither = 0f };
        static readonly PixelMaterial TentacleSkin = new PixelMaterial(Ramp.Make("4d3a57", 5, 0.12f, 0.3f, 1.6f)) { Rim = 0.7f, Gloss = 0.25f, Ambient = 0.22f, Dither = 0.02f };
        static readonly PixelMaterial Sucker = new PixelMaterial(Ramp.Make("a88aa0", 3, 0.06f, 0.5f, 1.2f)) { Ambient = 0.4f, Dither = 0f, Outline = false };
        static readonly PixelMaterial Bone = new PixelMaterial(Ramp.Make("c4b998", 5, 0.08f, 0.3f, 1.3f)) { Rim = 0.5f, Ambient = 0.26f, Dither = 0.02f };
        // Profundos: piel escamosa verde, vientre pálido, aletas membranosas violáceas.
        static readonly PixelMaterial DeepSkin = new PixelMaterial(Ramp.Make("4c5850", 5, 0.06f, 0.26f, 1.6f)) { Gloss = 0.35f, Rim = 0.65f, Ambient = 0.2f, Dither = 0.02f, Texture = ScalesDark };
        static readonly PixelMaterial DeepBelly = new PixelMaterial(Ramp.Make("7e7c6c", 5, 0.08f, 0.3f, 1.4f)) { Gloss = 0.25f, Rim = 0.5f, Ambient = 0.25f, Dither = 0.02f };
        static readonly PixelMaterial DeepFin = new PixelMaterial(Ramp.Make("534760", 4, 0.1f, 0.32f, 1.6f)) { Rim = 0.7f, Ambient = 0.26f, Dither = 0f };
        static readonly PixelMaterial FinRay = new PixelMaterial(Ramp.Make("2f2838", 3, 0.06f, 0.6f, 1.4f)) { Ambient = 0.4f, Rim = 0.3f, Dither = 0f, Outline = false, ReceivesContactShadow = false };
        static readonly PixelMaterial UrchinBody = new PixelMaterial(Ramp.Make("3c2a40", 4, 0.1f, 0.4f, 1.6f)) { Gloss = 0.3f, Rim = 0.6f, Ambient = 0.22f, Dither = 0f, Texture = RustSpots };
        static readonly PixelMaterial UrchinSpine = new PixelMaterial(Ramp.Make("5a4a62", 3, 0.08f, 0.5f, 1.5f)) { Ambient = 0.35f, Rim = 0.5f, Dither = 0f };
        static readonly PixelMaterial DeepEyeDead = PixelMaterial.Glow("8f8a3c");
        static readonly PixelMaterial DeepEyeCore = PixelMaterial.Glow("c9bb5a");
        // Piedra de las vidrieras: gris algo más fría y clara que el muro.
        static readonly PixelMaterial WindowStone = new PixelMaterial(Ramp.Make("645e58", 5, 0.1f, 0.28f, 1.62f)) { Rim = 0.6f, Ambient = 0.2f, Dither = 0.04f, Texture = StoneGrain };

        static float Mottle(int x, int y) => (PixelCanvas.ValueNoise(x / 4f, y / 4f, 0, 91) - 0.5f) * 0.22f;
        // Carne: manchas lívidas y un punto más oscura para que solo los bordes que miran a la luz se aclaren.
        static float Flesh(int x, int y) => Mottle(x, y) - 0.07f;
        static float ScalesDark(int x, int y) => Scales(x, y) - 0.06f;
        static float Weave(int x, int y) => (PixelCanvas.ValueNoise(x / 2f, y / 3f, 0, 17) - 0.5f) * 0.2f;
        static float StoneGrain(int x, int y) => (PixelCanvas.ValueNoise(x / 3f, y / 3f, 0, 77) - 0.5f) * 0.16f;
        static float RustSpots(int x, int y) => PixelCanvas.ValueNoise(x / 2f, y / 2f, 0, 57) > 0.7f ? -0.2f : 0f;

        // Escamas: medias lunas al tresbolillo (teselas de 4×3 px, cada fila desplazada 2 px).
        static float Scales(int x, int y)
        {
            int row = y / 3;
            int sx = (x + (row % 2) * 2) % 4;
            int sy = y % 3;
            bool arc = (sy == 0 && (sx == 1 || sx == 2)) || (sy == 1 && (sx == 0 || sx == 3));
            return arc ? -0.11f : (sy == 2 && (sx == 1 || sx == 2) ? 0.05f : 0f);
        }

        // ------------------------------------------------------------------
        // API
        // ------------------------------------------------------------------

        public static List<PropSprite> All() => new List<PropSprite>
        {
            DrownedPileA(), DrownedPileB(), FishPile(), DeepOnePile(),
            WindowEye(), WindowDagon(), WindowSign(), RoseWindow(), WindowRay(),
        };

        // ------------------------------------------------------------------
        // Utilidades
        // ------------------------------------------------------------------

        static ShadedCanvas Canvas(int w, int h) => new ShadedCanvas(w, h, w * 0.5f, 0f);

        static PropSprite Finish(string name, ShadedCanvas c)
        {
            var color = c.Render(out var normal, out var emission);
            return new PropSprite
            {
                Name = name,
                Color = color,
                Normal = normal,
                Emission = HasPixels(emission) ? emission : null,
                Pivot01 = new Vector2(0.5f, 0f),
            };
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

        /// <summary>Un píxel exacto (ojos, dientes...) que respeta la profundidad, a diferencia de un Decal.</summary>
        static void Dot(ShadedCanvas c, Vector2 p, PixelMaterial m, float z, int g, float size = 1f)
        {
            if (size <= 1f) c.Ellipse(V(Mathf.Floor(p.x) + 0.5f, Mathf.Floor(p.y) + 0.5f), 0.6f, 0.6f, 0f, m, z, 0f, g);
            else c.Ellipse(V(Mathf.Round(p.x), Mathf.Round(p.y)), 1f, 1f, 0f, m, z, 0f, g);
        }

        static Vector2 Perp(Vector2 d) => V(-d.y, d.x);

        // ------------------------------------------------------------------
        // Piezas de cadáver
        // ------------------------------------------------------------------

        /// <summary>
        /// Cabeza de ahogado vista de frente: cráneo hinchado, mejillas caídas, ojos hundidos, boca abierta y la
        /// melena empapada enmarcando la cara. <paramref name="up"/> es el ángulo hacia la coronilla; se redondea a
        /// múltiplos de 90° para que los rasgos caigan limpios en la rejilla de píxeles.
        /// </summary>
        static void Head(ShadedCanvas c, Vector2 at, float r, float up, PixelMaterial skin, float z, int g, float shade = 0f, bool openMouth = true, bool hair = true)
        {
            at = V(Mathf.Floor(at.x) + 0.5f, Mathf.Floor(at.y) + 0.5f);
            float q = Mathf.Round(up / 90f) * 90f;
            Vector2 u = Dir(q), s = Dir(q - 90f);
            Vector2 P(float a, float b) => Add(at, Add(Scale(s, a), Scale(u, b)));
            c.Ellipse(at, r * 0.9f, r * 1.05f, q - 90f, skin, z, shade, g);
            c.Ellipse(P(0f, -r * 0.5f), r * 0.72f, r * 0.55f, q - 90f, skin, z + 0.002f, shade, g);
            if (hair)
            {
                // Melena: casquete en la coronilla y dos cortinas a los lados de la cara.
                c.Custom(at.x - r - 2f, at.y - r - 2f, at.x + r + 2f, at.y + r + 2f, (px, py) =>
                {
                    float dx = px - at.x, dy = py - at.y;
                    float a = dx * s.x + dy * s.y, b = dx * u.x + dy * u.y;
                    float e = a * a / (r * r * 1.05f) + b * b / (r * r * 1.15f);
                    if (e > 1.12f) return (false, N3.Front);
                    bool cap = b > r * 0.45f + 0.6f * Mathf.Sin(a * 1.9f + 0.5f);
                    bool curtain = Mathf.Abs(a) > r * 0.68f && b > -r * 0.55f;
                    if (!cap && !curtain) return (false, N3.Front);
                    var w = Add(Scale(s, a / r), Scale(u, b / r));
                    return (true, new N3(w.x * 0.9f, w.y * 0.9f, 0.5f).Normalized());
                }, Hair, z + 0.004f, shade, g);
            }
            // Ojos hundidos (2×1) con el puente de la nariz entre ellos y la boca abierta (1×2).
            Block(c, at, s, u, -2.5f, -0.5f, -0.5f, 0.5f, Socket, z + 0.01f, g);
            Block(c, at, s, u, 0.5f, -0.5f, 2.5f, 0.5f, Socket, z + 0.01f, g);
            if (openMouth) Block(c, at, s, u, -1.5f, -3.5f, 0.5f, -2.5f, Socket, z + 0.01f, g);
        }

        /// <summary>Rectángulo en coordenadas locales de la cabeza (a lo ancho s, hacia arriba u), alineado a la rejilla.</summary>
        static void Block(ShadedCanvas c, Vector2 at, Vector2 s, Vector2 u, float a0, float b0, float a1, float b1, PixelMaterial m, float z, int g)
        {
            Vector2 p0 = Add(at, Add(Scale(s, a0), Scale(u, b0))), p1 = Add(at, Add(Scale(s, a1), Scale(u, b1)));
            float x0 = Mathf.Min(p0.x, p1.x) + 0.02f, x1 = Mathf.Max(p0.x, p1.x) - 0.02f;
            float y0 = Mathf.Min(p0.y, p1.y) + 0.02f, y1 = Mathf.Max(p0.y, p1.y) - 0.02f;
            c.Poly(new[] { V(x0, y0), V(x1, y0), V(x1, y1), V(x0, y1) }, m, z, 0f, 0f, g);
        }

        /// <summary>Pelo lacio y empapado que cae desde la cabeza en la dirección dada.</summary>
        static void HairFall(ShadedCanvas c, Vector2 crown, Vector2 fall, float spread, int strands, float z, int g, int seed)
        {
            Vector2 side = Perp(fall.normalized);
            for (int i = 0; i < strands; i++)
            {
                float k = strands == 1 ? 0f : i / (float)(strands - 1) - 0.5f;
                Vector2 a = Add(crown, Scale(side, k * spread));
                float len = 0.75f + 0.45f * PixelCanvas.Hash(i, seed, 1);
                Vector2 b = Add(a, Scale(fall, len));
                Vector2 m = Add(Mix(a, b, 0.5f), Scale(side, (PixelCanvas.Hash(i, seed, 2) - 0.5f) * 3f));
                c.Strand(Bezier(a, m, b, 5), 1.1f, 0.5f, Hair, z, 0f, g);
            }
        }

        /// <summary>Mano abierta y rígida: palma y tres dedos en abanico bien separados, más el pulgar.</summary>
        static void Hand(ShadedCanvas c, Vector2 wrist, float angle, PixelMaterial skin, float z, int g, float shade = 0f, float spread = 32f, float thumbSide = 1f, float size = 1f)
        {
            Vector2 palm = Add(wrist, Dir(angle, 1.4f * size));
            c.Ellipse(palm, 1.7f * size, 1.5f * size, angle, skin, z, shade, g);
            for (int i = 0; i < 3; i++)
            {
                float a = angle + (i - 1f) * spread * thumbSide;
                Vector2 b = Add(palm, Dir(a, 1.1f * size));
                float len = (i == 1 ? 4f : 3.4f) * size;
                Vector2 mid = Add(b, Dir(a, len * 0.55f));
                Vector2 tip = Add(mid, Dir(a - 18f * thumbSide, len * 0.5f));
                c.Capsule(b, mid, 0.62f, 0.55f, skin, z + 0.001f, shade, g);
                c.Capsule(mid, tip, 0.55f, 0.5f, skin, z + 0.001f, shade, g);
            }
            Vector2 tb = Add(palm, Dir(angle - 80f * thumbSide, 1.2f * size));
            c.Capsule(tb, Add(tb, Dir(angle - 62f * thumbSide, 2.4f * size)), 0.65f, 0.5f, skin, z + 0.001f, shade, g);
        }

        /// <summary>Pie descalzo: talón, planta y dedos.</summary>
        static void Foot(ShadedCanvas c, Vector2 ankle, float angle, PixelMaterial skin, float z, int g, float shade = 0f)
        {
            Vector2 d = Dir(angle), n = Perp(d);
            c.Ellipse(Add(ankle, Scale(d, -0.4f)), 1.9f, 1.7f, angle, skin, z, shade, g);
            Vector2 toe = Add(ankle, Scale(d, 4.2f));
            c.Capsule(ankle, toe, 1.6f, 1.25f, skin, z + 0.001f, shade, g);
            for (int i = 0; i < 2; i++)
                c.Capsule(Add(toe, Scale(n, (i - 0.5f) * 1.3f)), Add(toe, Add(Scale(d, 1.5f), Scale(n, (i - 0.5f) * 1.7f))), 0.55f, 0.5f, skin, z + 0.002f, shade, g);
        }

        static void Limb(ShadedCanvas c, Vector2 a, Vector2 b, Vector2 d, float r0, float r1, PixelMaterial skin, float z, int g, float shade = 0f)
        {
            c.Capsule(a, b, r0, Mathf.Lerp(r0, r1, 0.5f), skin, z, shade, g);
            c.Capsule(b, d, Mathf.Lerp(r0, r1, 0.5f) * 0.95f, r1, skin, z + 0.001f, shade, g);
        }

        /// <summary>Torso hinchado: cápsula de la cadera al pecho con el vientre abombado hacia <paramref name="bellySide"/>.</summary>
        static void Torso(ShadedCanvas c, Vector2 hip, Vector2 chest, float r, PixelMaterial skin, float z, int g, float shade = 0f, float bellySide = 1f, float bloat = 1f)
        {
            c.Capsule(hip, chest, r * 0.85f, r, skin, z, shade, g);
            Vector2 d = Sub(chest, hip);
            float len = d.magnitude;
            Vector2 n = Scale(Perp(d.normalized), bellySide);
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            c.Ellipse(Add(Mix(hip, chest, 0.38f), Scale(n, r * 0.3f * bloat)), len * 0.42f, r * (0.95f + 0.15f * bloat), ang, skin, z + 0.003f, shade, g);
        }

        /// <summary>Tela rasgada que cubre la cápsula a→b desde a hasta el dobladillo (fracción) con bordes deshilachados.</summary>
        static void Cloth(ShadedCanvas c, Vector2 a, Vector2 b, float ra, float rb, float hem, PixelMaterial m, float z, int seed, float shade = 0f, int group = 0)
        {
            Vector2 d = Sub(b, a);
            float len = Mathf.Max(0.001f, d.magnitude);
            Vector2 dn = Scale(d, 1f / len), n = Perp(dn);
            float pad = Mathf.Max(ra, rb) + 2f;
            c.Custom(Mathf.Min(a.x, b.x) - pad, Mathf.Min(a.y, b.y) - pad, Mathf.Max(a.x, b.x) + pad, Mathf.Max(a.y, b.y) + pad, (px, py) =>
            {
                float qx = px - a.x, qy = py - a.y;
                float t = (qx * dn.x + qy * dn.y) / len;
                float side = qx * n.x + qy * n.y;
                float r = Mathf.Lerp(ra, rb, Mathf.Clamp01(t));
                if (Mathf.Abs(side) > r) return (false, N3.Front);
                if (t < -ra / len) return (false, N3.Front);
                float jag = (PixelCanvas.ValueNoise(side * 0.9f + 3f, 0f, 0, seed) - 0.5f) * 0.35f + (Mathf.Repeat(side * 0.7f, 1f) < 0.5f ? 0.04f : -0.04f);
                if (t > hem + jag) return (false, N3.Front);
                float u = side / r;
                float fold = 0.35f * Mathf.Sin(t * len * 0.9f + side * 0.4f + seed);
                var nn = Cylinder(u);
                return (true, new N3(nn.x + dn.x * fold, nn.y + dn.y * fold, nn.z).Normalized());
            }, m, z, shade, group);
        }

        /// <summary>Herida con sangre seca y regueros que bajan.</summary>
        static void Wound(ShadedCanvas c, Vector2 at, float size, float z, int seed, float drip = 6f)
        {
            int g = c.NewGroup();
            c.Poly(Blob(at, size * 1.3f, size, 7, seed, 0.35f), Blood, z, 1f, 0f, g);
            int drips = drip > 0f ? 1 + seed % 2 : 0;
            for (int i = 0; i < drips; i++)
            {
                Vector2 s0 = Add(at, V((i - 0.5f) * size * 0.9f, -size * 0.4f));
                float l = drip * (0.6f + 0.6f * PixelCanvas.Hash(i, seed, 4));
                c.Capsule(s0, Add(s0, V((PixelCanvas.Hash(i, seed, 5) - 0.5f) * 1.5f, -l)), 0.75f, 0.55f, Blood, z, 0f, g);
            }
        }

        /// <summary>Charco de sangre seca en el suelo.</summary>
        static void Pool(ShadedCanvas c, Vector2 at, float rx, float z, int seed)
        {
            c.Poly(Blob(V(at.x, at.y + 0.6f), rx, 1.6f, 9, seed, 0.25f), Blood, z, 0.5f, 0f, c.NewGroup(), 0f, 0.6f);
        }

        /// <summary>Masa oscura que rellena el fondo de la pila.</summary>
        static void Mound(ShadedCanvas c, float cx, float halfW, float height, int seed, PixelMaterial m, float z)
        {
            c.Custom(cx - halfW - 1f, 0f, cx + halfW + 1f, height * 1.3f + 2f, (px, py) =>
            {
                float u = (px - cx) / halfW;
                if (Mathf.Abs(u) >= 1f) return (false, N3.Front);
                float top = height * Mathf.Pow(1f - u * u, 0.6f) * (0.8f + 0.4f * PixelCanvas.ValueNoise(px / 5f, 0f, 0, seed));
                if (py > top) return (false, N3.Front);
                float v = py / Mathf.Max(1f, top);
                return (true, new N3(u * 0.7f, v * v * 0.8f, 1f).Normalized());
            }, m, z, -0.08f);
        }

        /// <summary>
        /// Arpón de hierro: astil desde la contera hasta la punta, con virola y punta de lengüetas.
        /// Si <paramref name="entry"/> no es nulo, el tramo hasta ese punto se pinta delante (entra en el cuerpo)
        /// y el resto detrás de la pila (sale por arriba).
        /// </summary>
        static void Harpoon(ShadedCanvas c, Vector2 butt, Vector2 tip, float zBack, float zFront, float entry = -1f, bool pike = false, bool rope = false, int seed = 0)
        {
            int g = c.NewGroup();
            Vector2 d = Sub(tip, butt).normalized, n = Perp(d);
            float headLen = pike ? 11f : 8f;
            Vector2 neck = Sub(tip, Scale(d, headLen));
            if (entry > 0f)
            {
                Vector2 e = Mix(butt, neck, entry);
                c.Capsule(butt, e, 1.15f, 1.1f, Iron, zFront, 0f, g);
                c.Capsule(e, neck, 1.1f, 1.05f, Iron, zBack, 0f, g);
            }
            else
            {
                c.Capsule(butt, neck, 1.15f, 1.05f, Iron, zBack, 0f, g);
            }
            Vector2 L(float u, float v) => Add(neck, Add(Scale(d, u), Scale(n, v)));
            // Virola.
            c.Capsule(L(-2.2f, 0f), L(0.6f, 0f), 1.7f, 1.7f, Iron, zBack + 0.02f, 0f, g);
            if (pike)
            {
                // Pica: hoja larga en forma de hoja de sauce con dos orejetas.
                c.Poly(new[] { L(0f, 1.3f), L(4f, 2.4f), L(headLen, 0f), L(4f, -2.4f), L(0f, -1.3f) }, Iron, zBack + 0.03f, 1.2f, 0f, g);
                c.Capsule(L(1f, -4.2f), L(1f, 4.2f), 0.85f, 0.85f, Iron, zBack + 0.025f, 0f, g);
                c.Capsule(L(1f, 4.2f), L(-1.2f, 5.4f), 0.75f, 0.5f, Iron, zBack + 0.025f, 0f, g);
                c.Capsule(L(1f, -4.2f), L(-1.2f, -5.4f), 0.75f, 0.5f, Iron, zBack + 0.025f, 0f, g);
            }
            else
            {
                // Arpón ballenero: punta de flecha con lengüetas vueltas hacia atrás.
                c.Poly(new[] { L(0f, 0.9f), L(1.5f, 1.4f), L(-0.6f, 3.6f), L(3.2f, 2.6f), L(headLen, 0f), L(3.2f, -2.6f), L(-0.6f, -3.6f), L(1.5f, -1.4f), L(0f, -0.9f) },
                    Iron, zBack + 0.03f, 1.1f, 0f, g);
            }
            if (rope)
            {
                for (int i = 0; i < 3; i++) c.Capsule(L(-4.2f - i * 1.3f, -1.6f), L(-3.6f - i * 1.3f, 1.6f), 0.7f, 0.7f, Rope, zBack + 0.04f, 0f, g);
                Vector2 r0 = L(-5f, -1.4f);
                c.Strand(Bezier(r0, Add(r0, Add(Scale(n, -4f), V(0f, -3f))), Add(r0, Add(Scale(n, -2f), V(0f, -9f))), 6), 0.75f, 0.55f, Rope, zBack + 0.04f, 0f, g);
            }
        }

        /// <summary>Eslabón de cadena rotado (toro elíptico) a lo largo de la dirección dada.</summary>
        static void Link(ShadedCanvas c, Vector2 center, float angle, float rx, float ry, float thick, PixelMaterial m, float z, int g)
        {
            float half = thick * 0.5f, ca = Mathf.Cos(angle * Mathf.Deg2Rad), sa = Mathf.Sin(angle * Mathf.Deg2Rad);
            float ext = Mathf.Max(rx, ry) + half + 1f;
            c.Custom(center.x - ext, center.y - ext, center.x + ext, center.y + ext, (px, py) =>
            {
                float lx = (px - center.x) * ca + (py - center.y) * sa;
                float ly = -(px - center.x) * sa + (py - center.y) * ca;
                float dx = lx / rx, dy = ly / ry;
                float dd = Mathf.Sqrt(dx * dx + dy * dy);
                float rr = (rx + ry) * 0.5f;
                float v = (dd - 1f) * rr / half;
                if (Mathf.Abs(v) > 1f) return (false, N3.Front);
                float len = Mathf.Max(0.001f, dd);
                float nx = dx / len * v, ny = dy / len * v;
                return (true, new N3(nx * ca - ny * sa, nx * sa + ny * ca, Mathf.Sqrt(Mathf.Max(0.05f, 1f - v * v))).Normalized());
            }, m, z, 0f, g);
        }

        /// <summary>Cadena que sigue una polilínea: eslabones planos y de canto alternos.</summary>
        static void Chain(ShadedCanvas c, IList<Vector2> path, float z, PixelMaterial m)
        {
            int g = c.NewGroup();
            float step = 3.4f, carry = 0f;
            int k = 0;
            for (int i = 0; i + 1 < path.Count; i++)
            {
                Vector2 a = path[i], b = path[i + 1];
                float len = Sub(b, a).magnitude;
                Vector2 d = Sub(b, a).normalized;
                float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                for (float t = carry; t < len; t += step, k++)
                {
                    Vector2 p = Add(a, Scale(d, t));
                    if (k % 2 == 0) Link(c, p, ang, 2.3f, 1.5f, 1.1f, m, z + k * 0.0005f, g);
                    else c.Capsule(Sub(p, Scale(d, 1.9f)), Add(p, Scale(d, 1.9f)), 0.75f, 0.75f, m, z + 0.01f + k * 0.0005f, 0f, g);
                    carry = t + step - len;
                }
            }
        }

        /// <summary>Red de pesca: malla de rombos dentro de una silueta, colgando sobre los cuerpos.</summary>
        static void Net(ShadedCanvas c, Vector2[] outline, float mesh, float z, int seed)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var p in outline) { minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x); minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y); }
            c.Custom(minX - 1f, minY - 1f, maxX + 1f, maxY + 1f, (px, py) =>
            {
                if (!Inside(outline, px, py)) return (false, N3.Front);
                float sag = 1.2f * Mathf.Sin(px * 0.25f + seed);
                float a = Mathf.Repeat((px + py + sag) / mesh, 1f), b = Mathf.Repeat((px - py - sag) / mesh, 1f);
                bool knot = a < 0.2f && b < 0.2f;
                if (a > 0.17f && b > 0.17f) return (false, N3.Front);
                return (true, knot ? N3.Front : new N3(0.2f * Mathf.Sin(px * 0.5f), 0.4f, 1f).Normalized());
            }, NetRope, z, 0f);
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

        // ------------------------------------------------------------------
        // Pilas de cadáveres
        // ------------------------------------------------------------------

        // Pila grande de ahogados: uno sentado a la derecha, uno de cabeza resbalando por la izquierda, uno tendido
        // delante con el vientre hinchado y, detrás, otro con un brazo alzado en rigor mortis y otro boca abajo bajo
        // una red de pesca con sus corchos; cuatro arpones y picas los atraviesan.
        static PropSprite DrownedPileA()
        {
            var c = Canvas(120, 56);
            Mound(c, -2f, 52f, 24f, 3, Mass, 0.1f);

            // --- Fila de atrás ---
            int b1 = c.NewGroup();
            Torso(c, V(-26f, 19f), V(-6f, 25f), 5.2f, SkinGrey, 0.6f, b1, -0.12f, 1f, 1.2f);
            Head(c, V(3f, 27f), 4.3f, 0f, SkinGrey, 0.62f, b1, -0.1f, true, false);
            Limb(c, V(-5f, 27f), V(-11f, 36f), V(-9f, 43f), 2.4f, 1.9f, SkinGrey, 0.64f, b1, -0.06f);
            Hand(c, V(-9f, 43f), 85f, SkinGrey, 0.65f, b1, -0.06f, 30f, -1f, 1.1f);
            Limb(c, V(-26f, 19f), V(-36f, 24f), V(-47f, 18f), 3f, 2.2f, SkinGrey, 0.58f, b1, -0.14f);
            Foot(c, V(-47f, 18f), 200f, SkinGrey, 0.585f, b1, -0.14f);
            Cloth(c, V(-26f, 19f), V(-37f, 23.5f), 3.8f, 3.1f, 0.7f, RagOlive, 0.59f, 11, -0.12f, b1);

            int b2 = c.NewGroup();
            Torso(c, V(12f, 18f), V(28f, 22f), 5f, SkinViolet, 0.7f, b2, -0.12f, 1f, 0.8f);
            Cloth(c, V(28f, 22f), V(12f, 18f), 5.6f, 5.2f, 0.72f, Rag, 0.71f, 4, -0.12f, b2);
            Head(c, V(36f, 21f), 4f, 0f, SkinViolet, 0.72f, b2, -0.1f);
            Limb(c, V(12f, 19f), V(3f, 23f), V(-4f, 20f), 3f, 2.2f, SkinViolet, 0.68f, b2, -0.14f);

            // --- Red de pesca echada sobre el de la derecha, con sus corchos ---
            var netShape = new[] { V(8f, 25f), V(20f, 28.5f), V(32f, 27.5f), V(42f, 24f), V(44f, 14f), V(30f, 12f), V(14f, 14f) };
            Net(c, netShape, 6.5f, 0.9f, 2);
            for (int i = 0; i < 5; i++)
            {
                Vector2 f = Mix(V(10f, 25.5f), V(40f, 25f), i / 4f);
                f.y += Mathf.Sin(i * 1.7f) * 1.5f + (i == 2 ? 3f : 0f);
                c.Ellipse(f, 1.8f, 1.3f, 20f * (i % 2 == 0 ? 1f : -1f), Cork, 0.95f, 0f, c.NewGroup());
            }

            // --- Arpones y picas (el tramo de dentro queda oculto por los cuerpos) ---
            Harpoon(c, V(-12f, 12f), V(-31f, 54f), 0.3f, 2.9f, -1f, false, true, 1);
            Harpoon(c, V(6f, 6f), V(11f, 55f), 0.32f, 2.9f, -1f, true);
            Harpoon(c, V(21f, 2f), V(56f, 40f), 0.34f, 2.95f, 0.22f, false, false, 3);
            Harpoon(c, V(-20f, 9f), V(-58f, 33f), 0.31f, 2.95f, -1f, true);

            // --- Fila de delante ---
            int f1 = c.NewGroup();
            // Resbalando de cabeza por la ladera izquierda; el pelo se derrama por el suelo.
            Torso(c, V(-20f, 13f), V(-36f, 8.5f), 5.4f, SkinGreen, 2f, f1, 0f, -1f, 1f);
            Cloth(c, V(-20f, 13f), V(-36f, 8.5f), 6f, 5.6f, 0.62f, Rag, 2.02f, 7, 0f, f1);
            Head(c, V(-45f, 5.5f), 4.2f, 180f, SkinGreen, 2.05f, f1);
            HairFall(c, V(-49f, 5f), V(-8f, -3f), 6f, 4, 2.06f, f1, 5);
            Limb(c, V(-35f, 10.5f), V(-45f, 14f), V(-54f, 9f), 2.5f, 2f, SkinGreen, 2.08f, f1);
            Hand(c, V(-54f, 9f), 250f, SkinGreen, 2.09f, f1, 0f, 30f, 1f, 1.05f);
            Limb(c, V(-20f, 13f), V(-10f, 18f), V(-2f, 14f), 3.2f, 2.4f, SkinGreen, 1.9f, f1, -0.04f);
            Cloth(c, V(-20f, 13f), V(-2f, 14f), 3.8f, 3f, 0.9f, RagDark, 1.92f, 8, -0.04f, f1);

            int f2 = c.NewGroup();
            // Tendido delante, boca arriba, vientre hinchado y el brazo caído al suelo.
            Torso(c, V(-8f, 6f), V(10f, 7.5f), 5.6f, SkinViolet, 2.4f, f2, 0f, 1f, 1.1f);
            Cloth(c, V(11f, 7.8f), V(-8f, 6f), 6f, 6.2f, 0.45f, Rag, 2.43f, 19, 0f, f2);
            Wound(c, V(-1f, 8.5f), 1.7f, 2.44f, 8, 0f);
            Head(c, V(18.5f, 8.5f), 4.3f, 0f, SkinViolet, 2.45f, f2, 0f, true, false);
            Limb(c, V(10f, 9.5f), V(15f, 3f), V(22f, 2.2f), 2.5f, 2f, SkinViolet, 2.5f, f2);
            Hand(c, V(22f, 2.2f), 5f, SkinViolet, 2.51f, f2, 0f, 30f, 1f);
            Cloth(c, V(-8f, 6f), V(-16f, 4.5f), 5.4f, 4.4f, 1f, RagOlive, 2.42f, 9, 0f, f2);
            Limb(c, V(-14f, 5f), V(-21f, 3.5f), V(-28f, 3f), 3f, 2.2f, SkinViolet, 2.38f, f2);
            Foot(c, V(-28f, 3f), 175f, SkinViolet, 2.385f, f2);

            int f3 = c.NewGroup();
            // Sentado contra la pila a la derecha, cabeza ladeada, piernas estiradas.
            Torso(c, V(36f, 6f), V(32f, 17f), 5.2f, SkinGreen, 2.7f, f3, 0f, 1f, 0.9f);
            Cloth(c, V(31.5f, 18f), V(36f, 6f), 5.8f, 5.6f, 0.55f, RagDark, 2.72f, 13, 0f, f3);
            Head(c, V(33.5f, 24.5f), 4.3f, 90f, SkinGreen, 2.75f, f3);
            HairFall(c, V(31f, 25f), V(-1f, -8f), 3f, 2, 2.76f, f3, 9);
            Limb(c, V(37f, 6f), V(46f, 9f), V(54f, 3f), 3.2f, 2.3f, SkinGreen, 2.8f, f3);
            Cloth(c, V(37f, 6f), V(48f, 8f), 3.9f, 3.2f, 0.85f, Rag, 2.81f, 15, 0f, f3);
            Foot(c, V(54f, 3f), -15f, SkinGreen, 2.81f, f3);
            Limb(c, V(28.5f, 15f), V(26.5f, 8.5f), V(27.5f, 3.5f), 2.4f, 2f, SkinGreen, 2.85f, f3);
            Hand(c, V(27.5f, 3.5f), 275f, SkinGreen, 2.86f, f3, 0f, 30f, 1f);

            // Heridas donde entran y salen los hierros, y sangre en el suelo.
            Wound(c, V(-16.5f, 23f), 1.6f, 0.66f, 1, 4f);
            Wound(c, V(28.5f, 12f), 1.6f, 2.96f, 2, 5f);
            Wound(c, V(8f, 24f), 1.4f, 0.75f, 4, 3f);
            Pool(c, V(4f, 0f), 12f, 2.3f, 5);
            Pool(c, V(-44f, 0f), 9f, 2.1f, 6);
            return Finish("pila_ahogados_a", c);
        }

        // Variante pequeña: un ahogado boca abajo de través sobre otro encogido, unas piernas asomando por detrás
        // y tres hierros.
        static PropSprite DrownedPileB()
        {
            var c = Canvas(96, 44);
            Mound(c, 0f, 42f, 17f, 7, Mass, 0.1f);

            int b1 = c.NewGroup();
            // Detrás: encogido, con las rodillas al pecho.
            Torso(c, V(6f, 12f), V(-9f, 17f), 5f, SkinViolet, 0.6f, b1, -0.12f, 1f, 1f);
            Head(c, V(-16.5f, 19.5f), 4.1f, 90f, SkinViolet, 0.62f, b1, -0.1f, true, false);
            Limb(c, V(6f, 12f), V(15f, 20f), V(8f, 17f), 3f, 2.2f, SkinViolet, 0.64f, b1, -0.08f);
            Cloth(c, V(6f, 12f), V(15f, 20f), 3.8f, 3.2f, 0.95f, RagOlive, 0.66f, 3, -0.08f, b1);
            int b2 = c.NewGroup();
            Limb(c, V(14f, 14f), V(25f, 25f), V(34f, 20f), 3f, 2.2f, SkinGrey, 0.5f, b2, -0.14f);
            Foot(c, V(34f, 20f), 55f, SkinGrey, 0.51f, b2, -0.12f);
            Cloth(c, V(14f, 14f), V(24f, 24f), 3.8f, 3.2f, 0.8f, RagDark, 0.52f, 5, -0.14f, b2);
            Limb(c, V(16f, 13f), V(29f, 18f), V(37f, 12f), 2.9f, 2.1f, SkinGrey, 0.48f, b2, -0.18f);
            Foot(c, V(37f, 12f), 40f, SkinGrey, 0.485f, b2, -0.16f);

            Harpoon(c, V(-4f, 8f), V(-18f, 42f), 0.3f, 2.9f, -1f, false, true, 2);
            Harpoon(c, V(8f, 4f), V(23f, 41f), 0.32f, 2.9f, -1f, true);
            Harpoon(c, V(-35f, 1f), V(-5f, 28f), 0.31f, 2.95f, 0.3f);

            int f1 = c.NewGroup();
            // Delante: de través, con el brazo colgando hasta el suelo.
            Torso(c, V(-24f, 7f), V(-4f, 11f), 5.4f, SkinGreen, 2f, f1, 0f, -1f, 1.1f);
            Cloth(c, V(-4f, 11f), V(-24f, 7f), 6f, 5.8f, 0.75f, Rag, 2.02f, 5, 0f, f1);
            Head(c, V(4.5f, 10f), 4.2f, 0f, SkinGreen, 2.05f, f1);
            HairFall(c, V(8f, 9f), V(4f, -6f), 5f, 3, 2.06f, f1, 2);
            Limb(c, V(-4f, 9.5f), V(1f, 3.5f), V(8f, 2f), 2.6f, 2f, SkinGreen, 2.1f, f1);
            Hand(c, V(8f, 2f), -5f, SkinGreen, 2.11f, f1, 0f, 30f, 1f);
            Limb(c, V(-24f, 7f), V(-32f, 4f), V(-40f, 2.5f), 3f, 2.3f, SkinGreen, 1.95f, f1);
            Cloth(c, V(-24f, 7f), V(-40f, 2.5f), 3.8f, 3f, 0.7f, RagDark, 1.97f, 6, 0f, f1);
            Foot(c, V(-40f, 2.5f), 185f, SkinGreen, 1.96f, f1);

            int f2 = c.NewGroup();
            // A la derecha, recostado contra la pila mirando al frente, con un brazo colgando hasta el suelo.
            Torso(c, V(30f, 5f), V(22f, 14f), 4.9f, SkinGrey, 2.3f, f2, 0f, 1f, 1.1f);
            Cloth(c, V(22f, 14.5f), V(30f, 5f), 5.4f, 5.4f, 0.5f, RagOlive, 2.31f, 12, 0f, f2);
            Head(c, V(20f, 20.5f), 4.1f, 90f, SkinGrey, 2.35f, f2, 0f, true, false);
            Limb(c, V(26f, 12f), V(31f, 9f), V(34f, 2.5f), 2.2f, 1.9f, SkinGrey, 2.4f, f2);
            Hand(c, V(34f, 2.5f), 285f, SkinGrey, 2.41f, f2, 0f, 30f, -1f);
            Limb(c, V(31f, 4.5f), V(39f, 8f), V(45f, 2.5f), 2.7f, 2.1f, SkinGrey, 2.26f, f2);
            Foot(c, V(45f, 2.5f), -5f, SkinGrey, 2.265f, f2);

            Wound(c, V(-12f, 16f), 1.6f, 0.67f, 3, 4f);
            Wound(c, V(-27f, 9f), 1.5f, 2.95f, 6, 3f);
            Pool(c, V(-6f, 0f), 11f, 2.2f, 4);
            return Finish("pila_ahogados_b", c);
        }

        // Peces muertos enormes amontonados, un pez escorpión erizado de espinas, tentáculos que se derraman por el
        // suelo, erizos y un arpón que atraviesa al pez del fondo.
        static PropSprite FishPile()
        {
            var c = Canvas(80, 32);
            Mound(c, 0f, 30f, 11f, 5, Mass, 0.1f);

            // Tentáculos de un pulpo enterrado bajo los peces.
            Tentacle(c, new[] { V(-8f, 10f), V(-18f, 8f), V(-26f, 3f), V(-33f, 2.5f), V(-36f, 5f), V(-33.5f, 7f) }, 2.8f, 0.8f, 0.4f, 1);
            Tentacle(c, new[] { V(12f, 9f), V(22f, 6f), V(29f, 2.5f), V(35f, 3f), V(36.5f, 6f) }, 2.6f, 0.7f, 2.95f, 2);
            Tentacle(c, new[] { V(4f, 14f), V(12f, 22f), V(9f, 27f), V(4f, 26f) }, 2.2f, 0.7f, 0.5f, 3);

            // Pez del fondo (cabeza arriba a la derecha, boca abierta).
            Fish(c, V(14f, 14f), 15f, 5.6f, 25f, true, 0.8f, -0.08f, 4, false);
            // Pez escorpión arriba a la izquierda, con la dorsal erizada.
            Fish(c, V(-15f, 15f), 11f, 4.4f, -12f, false, 0.9f, -0.1f, 5, true);

            // El arpón atraviesa el pez del fondo.
            Harpoon(c, V(30f, 1f), V(4f, 31f), 0.7f, 2.9f, 0.45f, false, true, 4);

            // Pez grande delante, tendido de lado, cabeza a la izquierda.
            Fish(c, V(-5f, 8f), 21f, 7.4f, 0f, false, 2f, 0f, 6, false);
            Wound(c, V(17.5f, 10f), 1.5f, 2.95f, 7, 3f);
            Urchin(c, V(-29f, 0f), 2.6f, 3f, 1);
            Urchin(c, V(26f, 0f), 2f, 3f, 2);
            Pool(c, V(-2f, 0f), 14f, 1.5f, 8);
            return Finish("pila_peces", c);
        }

        static void Tentacle(ShadedCanvas c, Vector2[] path, float r0, float r1, float z, int seed)
        {
            int g = c.NewGroup();
            var pts = new List<Vector2>();
            for (int i = 0; i + 1 < path.Length; i++)
            {
                var seg = Bezier(path[i], Mix(path[i], path[i + 1], 0.5f), path[i + 1], 4);
                for (int k = i == 0 ? 0 : 1; k < seg.Length; k++) pts.Add(seg[k]);
            }
            c.Strand(pts, r0, r1, TentacleSkin, z, 0f, g);
            // Ventosas en el lado de abajo.
            for (int k = 1; k + 1 < pts.Count; k += 2)
            {
                Vector2 d = Sub(pts[k + 1], pts[k]).normalized;
                Vector2 side = V(d.y, -d.x);
                if (side.y > 0f) side = Scale(side, -1f);
                float r = Mathf.Lerp(r0, r1, k / (float)(pts.Count - 1));
                if (r < 1f) continue;
                c.Ellipse(Add(pts[k], Scale(side, r * 0.6f)), r * 0.35f + 0.4f, r * 0.3f + 0.35f, 0f, Sucker, z + 0.01f, 0f, g);
            }
        }

        /// <summary>
        /// Pez muerto de costado: lomo escamoso oscuro, vientre plateado, línea lateral, aleta dorsal de radios con
        /// púas (muy alta si <paramref name="spiny"/>), ojo vidrioso y boca abierta llena de dientes.
        /// </summary>
        static void Fish(ShadedCanvas c, Vector2 center, float rx, float ry, float angle, bool headRight, float z, float shade, int seed, bool spiny)
        {
            int g = c.NewGroup();
            float dir = headRight ? 1f : -1f;
            Vector2 ax = Dir(angle), up = Dir(angle + 90f);
            Vector2 P(float u, float v) => Add(center, Add(Scale(ax, u * dir), Scale(up, v)));
            // Cola.
            c.Poly(new[] { P(-rx * 0.82f, 0.8f), P(-rx - 6f, ry * 0.95f + 1f), P(-rx - 4f, 0f), P(-rx - 6.5f, -ry * 0.85f - 1f), P(-rx * 0.82f, -0.8f) }, FishFin, z - 0.01f, 1f, shade, g);
            c.Capsule(P(-rx * 0.9f, 0f), P(-rx - 4.5f, ry * 0.6f), 0.4f, 0.3f, FinRay, z - 0.009f, shade, g);
            c.Capsule(P(-rx * 0.9f, 0f), P(-rx - 4.8f, -ry * 0.55f), 0.4f, 0.3f, FinRay, z - 0.009f, shade, g);
            // Aleta dorsal: membrana con radios oscuros y púas de hueso.
            int rays = spiny ? 6 : 5;
            float tall = spiny ? 2.2f : 1f;
            var dorsal = new List<Vector2> { P(rx * 0.4f, ry * 0.6f) };
            var tips = new List<Vector2>();
            for (int i = 0; i <= rays; i++)
            {
                float u = rx * 0.32f - i * rx * 0.75f / rays;
                float h = ry * 0.75f + (2.2f + 1.6f * Mathf.Sin(Mathf.PI * (0.2f + 0.8f * i / rays))) * tall;
                Vector2 tip = P(u - 1.2f, h);
                tips.Add(tip);
                dorsal.Add(tip);
                if (i < rays) dorsal.Add(P(u - rx * 0.06f, ry * 0.75f + (h - ry * 0.75f) * 0.45f));
            }
            dorsal.Add(P(-rx * 0.55f, ry * 0.5f));
            c.Poly(dorsal.ToArray(), FishFin, z - 0.005f, 0.8f, shade, g);
            for (int i = 0; i < tips.Count; i++)
            {
                float u = rx * 0.32f - i * rx * 0.75f / rays;
                c.Capsule(P(u, ry * 0.6f), tips[i], 0.42f, 0.32f, FinRay, z - 0.004f, shade, g);
                if (spiny) c.Capsule(tips[i], Add(tips[i], Scale(Sub(tips[i], P(u, ry * 0.6f)).normalized, 1.6f)), 0.42f, 0.3f, Bone, z - 0.003f, shade, g);
            }
            // Cuerpo: lomo y vientre.
            c.Ellipse(center, rx, ry, angle, FishBack, z, shade, g);
            c.Custom(center.x - rx - 1f, center.y - rx - 1f, center.x + rx + 1f, center.y + rx + 1f, (px, py) =>
            {
                float dx = px - center.x, dy = py - center.y;
                float lu = (dx * ax.x + dy * ax.y) / rx, lv = (dx * up.x + dy * up.y) / ry;
                float d2 = lu * lu + lv * lv;
                if (d2 > 1f) return (false, N3.Front);
                float belly = -0.18f + 0.1f * Mathf.Sin(lu * 5f * dir + seed);
                if (lv > belly) return (false, N3.Front);
                float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - d2));
                Vector2 w = Add(Scale(ax, lu), Scale(up, lv));
                return (true, new N3(w.x, w.y, nz).Normalized());
            }, FishBelly, z + 0.001f, shade, g);
            // Línea lateral.
            var line = new List<Vector2>();
            for (int k = 0; k <= 6; k++)
            {
                float u = Mathf.Lerp(rx * 0.45f, -rx * 0.88f, k / 6f);
                line.Add(P(u, ry * (0.12f + 0.08f * Mathf.Sin(k * 1.1f))));
            }
            c.Strand(line, 0.45f, 0.4f, FinRay, z + 0.003f, 0f, g);
            // Aleta pectoral caída.
            c.Poly(new[] { P(rx * 0.45f, -ry * 0.25f), P(rx * 0.18f, -ry * 0.95f), P(rx * 0.02f, -ry * 0.78f), P(rx * 0.25f, -ry * 0.1f) }, FishFin, z + 0.004f, 0.8f, shade, g);
            // Opérculo (agalla) con carne roja asomando.
            c.Strand(Bezier(P(rx * 0.52f, ry * 0.7f), P(rx * 0.42f, 0f), P(rx * 0.52f, -ry * 0.7f), 5), 0.6f, 0.6f, Socket, z + 0.005f, 0f, g);
            c.Strand(Bezier(P(rx * 0.47f, ry * 0.45f), P(rx * 0.39f, 0f), P(rx * 0.47f, -ry * 0.45f), 5), 0.5f, 0.5f, Blood, z + 0.006f, 0f, g);
            // Ojo vidrioso con cerco oscuro.
            Vector2 eye = P(rx * 0.72f, ry * 0.3f);
            float er = Mathf.Max(1.5f, ry * 0.3f);
            c.Ellipse(eye, er + 0.8f, er + 0.8f, 0f, Socket, z + 0.0055f, 0f, g);
            c.Ellipse(eye, er, er, 0f, FishEye, z + 0.006f, 0f, g);
            Dot(c, Add(eye, V(0.4f * dir, -0.3f)), Socket, z + 0.007f, g);
            // Boca abierta con dientes en ambas mandíbulas.
            c.Poly(new[] { P(rx * 0.76f, -ry * 0.16f), P(rx * 1.1f, ry * 0.14f), P(rx * 1.13f, -ry * 0.66f) }, Socket, z + 0.006f, 0f, 0f, g);
            for (int k = 0; k < 3; k++)
            {
                float t = 0.3f + k * 0.3f;
                Dot(c, Mix(P(rx * 0.8f, -ry * 0.1f), P(rx * 1.08f, ry * 0.08f), t), Teeth, z + 0.007f, g);
                Dot(c, Mix(P(rx * 0.82f, -ry * 0.3f), P(rx * 1.1f, -ry * 0.58f), t), Teeth, z + 0.007f, g);
            }
            Vector2 m0 = P(rx * 1.02f, -ry * 0.15f);
            c.Capsule(P(rx * 0.8f, -ry * 0.5f), Add(m0, Scale(up, -ry * 0.62f)), 0.9f, 0.8f, FishBelly, z + 0.0065f, shade, g);
        }

        // Profundos muertos: uno tendido boca arriba en la cima (atravesado por una pica, con un ojo que aún
        // brilla apagado), otro resbalando de cabeza por la izquierda con su aleta dorsal, otro de costado delante
        // con el vientre pálido y otro desplomado a la derecha; una cadena con grillete los ata.
        static PropSprite DeepOnePile()
        {
            var c = Canvas(104, 48);
            Mound(c, 0f, 44f, 19f, 9, Mass, 0.1f);

            int b1 = c.NewGroup();
            Torso(c, V(-18f, 19f), V(2f, 24f), 5.6f, DeepSkin, 0.6f, b1, -0.1f, 1f, 1f);
            c.Ellipse(V(-7f, 24f), 8f, 3f, 14f, DeepBelly, 0.605f, -0.1f, b1);
            DeepHead(c, V(9.5f, 25.5f), 5.2f, 20f, 0.62f, b1, -0.08f, true);
            Limb(c, V(0f, 25f), V(-3f, 33f), V(2f, 39f), 2.4f, 2f, DeepSkin, 0.64f, b1, -0.06f);
            WebbedHand(c, V(2f, 39f), 70f, 0.65f, b1, -0.06f);
            Limb(c, V(-18f, 19f), V(-28f, 25f), V(-38f, 20f), 3.2f, 2.4f, DeepSkin, 0.58f, b1, -0.14f);
            WebbedHand(c, V(-38f, 20f), 200f, 0.585f, b1, -0.14f, 1.1f);
            Cloth(c, V(-18f, 19f), V(-29f, 24.5f), 4f, 3.4f, 0.7f, RagDark, 0.59f, 3, -0.12f, b1);

            // Pica clavada en el pecho del de la cima.
            Harpoon(c, V(-8f, 10f), V(-21f, 47f), 0.3f, 2.9f, -1f, true);
            Wound(c, V(-13.5f, 26.5f), 1.6f, 0.66f, 2, 4f);

            int f1 = c.NewGroup();
            // Izquierda: resbalando de cabeza, con la aleta dorsal erizada y la mano palmeada en el suelo.
            Torso(c, V(-18f, 12f), V(-34f, 7f), 5.4f, DeepSkin, 2f, f1, 0f, -1f, 0.9f);
            DorsalFin(c, V(-34f, 10.5f), V(-19f, 16f), 5.5f, 1.99f, f1);
            Cloth(c, V(-18f, 12f), V(-26f, 9.5f), 6f, 5.8f, 0.95f, Rag, 2.03f, 6, 0f, f1);
            DeepHead(c, V(-41.5f, 5f), 4.8f, 165f, 2.05f, f1, 0f, false);
            Limb(c, V(-33f, 6f), V(-37f, 1.8f), V(-46f, 2f), 2.3f, 1.9f, DeepSkin, 2.1f, f1);
            WebbedHand(c, V(-46f, 2f), 185f, 2.11f, f1, 0f);
            Limb(c, V(-18f, 12f), V(-8f, 16f), V(-1f, 11f), 3.2f, 2.4f, DeepSkin, 1.9f, f1, -0.04f);
            c.Capsule(V(-1f, 11f), V(3f, 6.5f), 2.2f, 1.6f, DeepSkin, 1.91f, -0.04f, f1);
            c.Poly(new[] { V(2f, 7.5f), V(7.5f, 3f), V(5.5f, 8.5f) }, DeepFin, 1.905f, 0.8f, -0.04f, f1);

            int f2 = c.NewGroup();
            // Delante en el centro: de costado, vientre pálido, boca abierta.
            Torso(c, V(2f, 5.5f), V(18f, 7f), 5.2f, DeepSkin, 2.4f, f2, 0f, 1f, 1.2f);
            c.Ellipse(V(9f, 3.8f), 7.5f, 2.6f, 4f, DeepBelly, 2.41f, 0f, f2);
            DeepHead(c, V(25.5f, 7.5f), 5f, -10f, 2.45f, f2, 0f, false, true);
            Limb(c, V(5f, 6f), V(-3f, 3f), V(-11f, 2.5f), 2.8f, 2.2f, DeepSkin, 2.39f, f2);
            WebbedHand(c, V(-11f, 2.5f), 180f, 2.395f, f2, 0f, 0.9f);

            int f3 = c.NewGroup();
            // Derecha: desplomado de espaldas sobre la pila, aleta dorsal y la cabeza colgando.
            Torso(c, V(37f, 6f), V(31f, 17f), 5.4f, DeepSkin, 2.7f, f3, 0f, -1f, 0.9f);
            DorsalFin(c, V(33f, 21f), V(39.5f, 8f), 5f, 2.69f, f3);
            DeepHead(c, V(31f, 24f), 5f, 120f, 2.75f, f3, 0f, false);
            Limb(c, V(28f, 15f), V(22f, 13.5f), V(20f, 7f), 2.3f, 1.9f, DeepSkin, 2.8f, f3);
            WebbedHand(c, V(20f, 7f), 275f, 2.81f, f3, 0f);
            Limb(c, V(39f, 5f), V(46f, 9f), V(50f, 3f), 3f, 2.2f, DeepSkin, 2.6f, f3);
            c.Poly(new[] { V(49f, 4.5f), V(53f, 0.5f), V(46.5f, 1f) }, DeepFin, 2.61f, 0.8f, 0f, f3);
            Cloth(c, V(37f, 6f), V(45f, 8.5f), 5f, 3.4f, 0.8f, RagOlive, 2.62f, 14, 0f, f3);

            // Cadena por delante, de la argolla del suelo al grillete de la muñeca.
            Chain(c, new[] { V(-50f, 1.5f), V(-45f, 6f), V(-38f, 12.5f), V(-28f, 15.5f), V(-16f, 15f), V(-5f, 13.5f), V(6f, 13f), V(14f, 11.5f), V(19.5f, 9.5f) }, 3.2f, Iron);
            Link(c, V(20.2f, 9.3f), 0f, 2.6f, 1.6f, 1.3f, Iron, 3.25f, c.NewGroup());

            Wound(c, V(29f, 11f), 1.5f, 2.95f, 5, 4f);
            Pool(c, V(-6f, 0f), 13f, 2.2f, 9);
            return Finish("pila_profundos", c);
        }

        /// <summary>Cabeza de Profundo: cráneo de pez, boca enorme, ojo saltón y agallas.</summary>
        static void DeepHead(ShadedCanvas c, Vector2 at, float r, float angle, float z, int g, float shade, bool glowEye, bool flipFace = false)
        {
            Vector2 f = Dir(angle), u = Dir(angle + (flipFace ? -90f : 90f));
            Vector2 P(float a, float b) => Add(at, Add(Scale(f, a), Scale(u, b)));
            // Cráneo alargado hacia el morro.
            c.Ellipse(at, r * 1.15f, r * 0.85f, angle, DeepSkin, z, shade, g);
            c.Ellipse(P(r * 0.4f, -r * 0.35f), r * 0.85f, r * 0.55f, angle, DeepBelly, z + 0.002f, shade, g);
            // Cresta.
            c.Poly(new[] { P(-r * 0.8f, r * 0.5f), P(-r * 0.2f, r * 1.25f), P(r * 0.1f, r * 0.75f), P(r * 0.4f, r * 1.05f), P(r * 0.5f, r * 0.6f) }, DeepFin, z - 0.002f, 0.8f, shade, g);
            // Boca abierta de oreja a oreja.
            c.Poly(new[] { P(r * 0.15f, -r * 0.15f), P(r * 1.2f, r * 0.05f), P(r * 1.18f, -r * 0.42f) }, Socket, z + 0.005f, 0f, 0f, g);
            Dot(c, P(r * 0.95f, -r * 0.05f), Teeth, z + 0.006f, g);
            Dot(c, P(r * 0.7f, -r * 0.32f), Teeth, z + 0.006f, g);
            // Agallas.
            for (int i = 0; i < 2; i++) c.Capsule(P(-r * 0.25f - i * 1.4f, r * 0.35f), P(-r * 0.35f - i * 1.4f, -r * 0.35f), 0.45f, 0.45f, Socket, z + 0.004f, 0f, g);
            // Ojo saltón.
            Vector2 eye = P(r * 0.35f, r * 0.32f);
            c.Ellipse(eye, 1.7f, 1.7f, 0f, glowEye ? DeepEyeDead : FishEye, z + 0.006f, 0f, g);
            if (glowEye) Dot(c, eye, DeepEyeCore, z + 0.007f, g);
            else Dot(c, Add(eye, Scale(f, 0.4f)), Socket, z + 0.007f, g);
        }

        /// <summary>Aleta dorsal de Profundo: membrana festoneada que se inclina hacia la cola, con radios oscuros y púas.</summary>
        static void DorsalFin(ShadedCanvas c, Vector2 head, Vector2 tail, float height, float z, int g, float shade = 0f)
        {
            Vector2 d = Sub(tail, head).normalized, n = Perp(d);
            if (n.y < 0f) n = Scale(n, -1f);
            const int rays = 5;
            var pts = new List<Vector2> { Add(head, Scale(n, -1.2f)) };
            var tips = new Vector2[rays + 1];
            for (int i = 0; i <= rays; i++)
            {
                float t = i / (float)rays;
                float hgt = height * (0.5f + 0.5f * Mathf.Sin(Mathf.PI * Mathf.Min(1f, 0.25f + t * 0.9f)));
                Vector2 b0 = Mix(head, tail, t);
                tips[i] = Add(b0, Add(Scale(n, hgt), Scale(d, hgt * 0.4f)));
                pts.Add(tips[i]);
                if (i < rays) pts.Add(Add(Mix(head, tail, t + 0.55f / rays), Scale(n, hgt * 0.62f)));
            }
            pts.Add(Add(tail, Scale(n, -1.2f)));
            c.Poly(pts.ToArray(), DeepFin, z, 0.8f, shade, g);
            for (int i = 0; i <= rays; i++)
            {
                Vector2 b0 = Mix(head, tail, i / (float)rays);
                c.Capsule(b0, tips[i], 0.42f, 0.32f, FinRay, z + 0.001f, shade, g);
                c.Capsule(tips[i], Add(tips[i], Add(Scale(n, 0.9f), Scale(d, 0.5f))), 0.4f, 0.3f, Bone, z + 0.002f, shade, g);
            }
        }

        /// <summary>Erizo de mar: bola oscura con púas en abanico.</summary>
        static void Urchin(ShadedCanvas c, Vector2 at, float r, float z, int seed)
        {
            int g = c.NewGroup();
            c.Ellipse(V(at.x, at.y + r * 0.7f), r, r * 0.8f, 0f, UrchinBody, z, 0f, g);
            for (int i = 0; i < 9; i++)
            {
                float a = 10f + i * 20f + (PixelCanvas.Hash(i, seed, 1) - 0.5f) * 10f;
                Vector2 b0 = Add(V(at.x, at.y + r * 0.7f), Dir(a, r * 0.6f));
                c.Capsule(b0, Add(b0, Dir(a, r * (1.2f + PixelCanvas.Hash(i, seed, 2) * 0.6f))), 0.45f, 0.3f, UrchinSpine, z + 0.01f, 0f, g);
            }
        }

        /// <summary>Mano palmeada de Profundo: tres dedos largos con membrana y garras.</summary>
        static void WebbedHand(ShadedCanvas c, Vector2 wrist, float angle, float z, int g, float shade, float size = 1f)
        {
            Vector2 palm = Add(wrist, Dir(angle, 1.8f * size));
            c.Ellipse(palm, 2f * size, 1.6f * size, angle, DeepSkin, z, shade, g);
            var tips = new Vector2[3];
            for (int i = 0; i < 3; i++)
            {
                float a = angle + (i - 1f) * 28f;
                Vector2 b0 = Add(palm, Dir(a, 1.2f * size));
                tips[i] = Add(b0, Dir(a, 3.6f * size));
                c.Capsule(b0, tips[i], 0.75f, 0.5f, DeepSkin, z + 0.002f, shade, g);
                c.Capsule(tips[i], Add(tips[i], Dir(a - 25f, 1.4f)), 0.5f, 0.3f, Bone, z + 0.003f, shade, g);
            }
            c.Poly(new[] { palm, Mix(palm, tips[0], 0.8f), Mix(palm, tips[1], 0.7f), Mix(palm, tips[2], 0.8f) }, DeepFin, z + 0.001f, 0.5f, shade, g);
        }

        // ------------------------------------------------------------------
        // Vidrieras
        // ------------------------------------------------------------------

        // Colores de vidrio: [grisalla, oscuro, medio, claro]. El sprite usa los apagados; la capa emisiva los intensos.
        const int Red = 0, Teal = 1, Amber = 2, Violet = 3, Pale = 4, Night = 5, Green = 6;

        static readonly string[][] GlassDim =
        {
            new[] { "220b10", "3a1217", "561a20", "70262a" },
            new[] { "0c2222", "123230", "1b4643", "285c57" },
            new[] { "2a1c0c", "463014", "62441f", "7e5c2a" },
            new[] { "160f22", "261b38", "37284e", "4a3864" },
            new[] { "2c2e28", "4a4e44", "62675a", "7e866e" },
            new[] { "0a0e18", "131a29", "1b263c", "26344e" },
            new[] { "0f1c12", "1d321f", "29482b", "385e39" },
        };

        static readonly string[][] GlassGlow =
        {
            new[] { "4a0e14", "7c1820", "c02c34", "ec5a4e" },
            new[] { "0e3a38", "175c58", "2a9c8e", "62d8c0" },
            new[] { "5a3410", "9a5a18", "dc9430", "ffd27a" },
            new[] { "2a1648", "42266e", "6e46aa", "a480dc" },
            new[] { "4c5244", "828c70", "b6c09e", "e6eecf" },
            new[] { "101a3a", "1a2850", "2a4080", "4862a8" },
            new[] { "143a18", "26602c", "44984a", "7ccc78" },
        };

        static readonly Color32 Lead = PixelCanvas.Hex("17131a");

        /// <summary>
        /// Una celda de vidrio: color, pieza (los cambios de pieza se emploman), si lleva grisalla pintada y su
        /// sesgo de brillo (el fondo va un tono más apagado que las figuras para que estas se lean).
        /// </summary>
        struct Glass
        {
            public int Key;
            public int Piece;
            public bool Paint;
            public bool LeadLine;
            public int Bias;

            public Glass(int key, int piece, bool paint = false, int bias = 0)
            {
                Key = key;
                Piece = piece * 8 + key;
                Paint = paint;
                LeadLine = false;
                Bias = bias;
            }

            public static Glass Leaded => new Glass { Key = -1, Piece = -1, LeadLine = true };
        }

        /// <summary>Vidrio de fondo (un tono más apagado).</summary>
        static Glass Bg(int key, int piece) => new Glass(key, piece, false, -1);

        delegate Glass GlassDesign(float x, float y);

        /// <summary>Teselas en rombo (losanges) típicas del vidrio de fondo.</summary>
        static int Quarry(float x, float y, float size, int salt)
        {
            int u = Mathf.FloorToInt((x + y * 0.75f) / size), v = Mathf.FloorToInt((x - y * 0.75f) / size);
            return salt * 4096 + (u + 64) * 128 + (v + 64);
        }

        /// <summary>
        /// Pinta el vidrio en los huecos de la tracería: <paramref name="opening"/> dice qué píxeles son ventana,
        /// <paramref name="design"/> da color y pieza. Rellena el color (apagado) y la emisión (intensa).
        /// </summary>
        static void PaintGlass(PixelCanvas color, PixelCanvas normal, PixelCanvas emission, float originX, float originY,
                               System.Func<float, float, bool> opening, GlassDesign design, int seed)
        {
            int w = color.Width, h = color.Height;
            var cells = new Glass[w * h];
            var used = new bool[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    if (color.Pixels[i].a > 0) continue;
                    float lx = x + 0.5f - originX, ly = y + 0.5f - originY;
                    if (!opening(lx, ly)) continue;
                    cells[i] = design(lx, ly);
                    used[i] = true;
                }
            }
            var frontNormal = new Color32(128, 128, 255, 255);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    if (!used[i]) continue;
                    var g = cells[i];
                    bool lead = g.LeadLine;
                    if (!lead && x + 1 < w && used[i + 1] && cells[i + 1].Piece != g.Piece && !cells[i + 1].LeadLine) lead = true;
                    if (!lead && y + 1 < h && used[i + w] && cells[i + w].Piece != g.Piece && !cells[i + w].LeadLine) lead = true;
                    normal.Pixels[i] = frontNormal;
                    if (lead)
                    {
                        color.Pixels[i] = Lead;
                        continue;
                    }
                    // Tono: cada pieza tiene su matiz; las figuras llevan vetas claras del vidrio soplado; mugre abajo.
                    float ph = PixelCanvas.Hash(g.Piece, 7, seed);
                    int tone = 2 + g.Bias;
                    if (g.Bias < 0)
                    {
                        if (ph > 0.66f) tone++;
                    }
                    else
                    {
                        if (ph < 0.22f) tone--;
                        else if (ph > 0.78f) tone++;
                        if (PixelCanvas.ValueNoise(x / 2.5f, y / 4f, 0, seed + g.Key) > 0.74f) tone++;
                    }
                    if (y < originY + 24 && PixelCanvas.ValueNoise(x / 3f, y / 2f, 0, seed + 5) > 0.5f + (y - originY) * 0.02f) tone--;
                    tone = Mathf.Clamp(tone, 1, 3);
                    if (g.Paint) tone = 0;
                    color.Pixels[i] = PixelCanvas.Hex(GlassDim[g.Key][tone]);
                    emission.Pixels[i] = PixelCanvas.Hex(GlassGlow[g.Key][tone]);
                }
            }
        }

        // Geometría común de las lancetas (48×128): hueco de 30 px, arco apuntado, alféizar, archivolta y pináculo.
        const float WinW = 15f, WinSpring = 76f, WinRadius = 33f, WinSill = 11f, WinFrame = 9f;
        // Las dos luces con óculo arrancan más abajo para dejar sitio a un óculo grande.
        const float WinSub = 66f, WinOculusR = 8f;
        static readonly float SubW = (WinW - 1.4f) * 0.5f;
        static readonly float OculusY = WinSub + SubW * 1.73f + WinOculusR + 1.6f;

        /// <summary>Distancia (con signo) al intradós de un arco apuntado de semiancho w y radio r, y la dirección hacia fuera.</summary>
        static float ArchDist(float x, float y, float w, float spring, float r, out float ox, out float oy)
        {
            float ax = Mathf.Abs(x), sx = x < 0f ? -1f : 1f;
            if (y <= spring)
            {
                ox = sx;
                oy = 0f;
                return ax - w;
            }
            float cx = w - r;
            float dx = ax - cx, dy = y - spring;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            ox = dx / d * sx;
            oy = dy / d;
            return d - r;
        }

        static float ArchDist(float x, float y, float w, float spring, float r) => ArchDist(x, y, w, spring, r, out _, out _);

        sealed class WindowSpec
        {
            public string Name;
            public bool Mullion = true;     // dos luces con parteluz y óculo en la cabeza del arco
            public float TransomY = -1f;    // travesaño: si existe y no hay dos luces, el parteluz solo llega hasta él
            public bool Cusps;              // lóbulos (cúspides) en el arco de una sola luz
            public GlassDesign Design;
            public int Seed;
        }

        static PropSprite GothicWindow(WindowSpec spec)
        {
            const int W = 48, H = 128;
            var c = new ShadedCanvas(W, H, W * 0.5f, 0f);
            float subCx = 1.4f + SubW, subR = SubW * 2f;

            // Hiladas y dovelas del marco (llagas de mortero) y manchas de humedad.
            System.Func<int, int, float> joints = (ix, iy) =>
            {
                float px = ix + 0.5f - W * 0.5f, py = iy + 0.5f;
                float g = StoneGrain(ix, iy) + (PixelCanvas.ValueNoise(ix / 5f, iy / 7f, 0, 13) > 0.68f ? -0.12f : 0f);
                float rho = ArchDist(px, py, WinW, WinSpring, WinRadius, out float ox, out float oy);
                if (rho < 4.5f || rho > 7.5f) return g;
                if (py <= WinSpring)
                {
                    float course = Mathf.Repeat(py - WinSill, 10.5f);
                    if (course < 1f) return g - 0.22f;
                    return g;
                }
                float ang = Mathf.Atan2(oy, Mathf.Abs(ox)) * Mathf.Rad2Deg;
                if (Mathf.Repeat(ang, 15f) < 1.4f + 3f / Mathf.Max(1f, rho)) return g - 0.22f;
                return g;
            };
            var stone = new PixelMaterial(WindowStone.Ramp) { Rim = WindowStone.Rim, Ambient = WindowStone.Ambient, Dither = WindowStone.Dither, Texture = joints };

            int gf = c.NewGroup();
            // Marco: derrame interior, bocel, paramento y chaflán exterior.
            c.Custom(-W * 0.5f, WinSill - 1f, W * 0.5f, H, (px, py) =>
            {
                float rho = ArchDist(px, py, WinW, WinSpring, WinRadius, out float ox, out float oy);
                if (rho < 0f || rho > WinFrame) return (false, N3.Front);
                if (py < WinSill) return (false, N3.Front);
                float nx, ny, nz;
                if (rho < 1.5f) { nx = -ox * 0.75f; ny = -oy * 0.75f; nz = 0.65f; }
                else if (rho < 4.5f)
                {
                    float u = (rho - 3f) / 1.5f;
                    float k = Mathf.Sqrt(Mathf.Max(0.05f, 1f - u * u));
                    nx = ox * u; ny = oy * u; nz = k;
                }
                else if (rho < 7.5f) { nx = 0f; ny = 0f; nz = 1f; }
                else { nx = ox * 0.7f; ny = oy * 0.7f; nz = 0.7f; }
                return (true, new N3(nx, ny, nz).Normalized());
            }, stone, 1f, 0f, gf);
            // Capiteles del bocel en el arranque y basas sobre el alféizar.
            for (int s = -1; s <= 1; s += 2)
            {
                float xs = s * (WinW + 3f);
                c.Poly(new[] { V(xs - 2.6f, WinSpring - 3.5f), V(xs + 2.6f, WinSpring - 3.5f), V(xs + 3.2f, WinSpring), V(xs - 3.2f, WinSpring) }, stone, 1.2f, 1.2f, 0f, gf);
                c.Poly(new[] { V(xs - 2.8f, WinSill), V(xs + 2.8f, WinSill), V(xs + 2.2f, WinSill + 3f), V(xs - 2.2f, WinSill + 3f) }, stone, 1.2f, 1.2f, 0f, gf);
            }
            // Guardapolvo (moldura del arco) con remates.
            c.Custom(-W * 0.5f, WinSpring - 4f, W * 0.5f, H, (px, py) =>
            {
                float rho = ArchDist(px, py, WinW, WinSpring, WinRadius, out float ox, out float oy);
                if (py < WinSpring - 1f || rho < WinFrame - 1.2f || rho > WinFrame + 1.6f) return (false, N3.Front);
                float u = (rho - (WinFrame + 0.2f)) / 1.4f;
                return (true, new N3(ox * u, oy * u + 0.2f, Mathf.Sqrt(Mathf.Max(0.05f, 1f - u * u))).Normalized());
            }, WindowStone, 1.3f, 0f, gf);
            for (int s = -1; s <= 1; s += 2)
            {
                float xs = s * (WinW + WinFrame + 0.2f);
                c.Ellipse(V(xs, WinSpring - 2.5f), 1.9f, 2.4f, 0f, WindowStone, 1.35f, 0f, gf);
            }
            // Pináculo (florón) sobre la clave.
            float apex = WinSpring + Mathf.Sqrt((WinRadius + WinFrame + 1.6f) * (WinRadius + WinFrame + 1.6f) - (WinRadius - WinW) * (WinRadius - WinW));
            c.Poly(new[] { V(-1.6f, apex - 2f), V(1.6f, apex - 2f), V(0.6f, apex + 7f), V(-0.6f, apex + 7f) }, WindowStone, 1.4f, 1f, 0f, gf);
            for (int i = 0; i < 2; i++)
            {
                float yy = apex + 1f + i * 3.4f;
                c.Capsule(V(-0.5f, yy), V(-3.2f + i, yy + 1.8f), 0.9f, 0.6f, WindowStone, 1.45f, 0f, gf);
                c.Capsule(V(0.5f, yy), V(3.2f - i, yy + 1.8f), 0.9f, 0.6f, WindowStone, 1.45f, 0f, gf);
            }
            c.Ellipse(V(0f, apex + 8.5f), 1.6f, 1.8f, 0f, WindowStone, 1.5f, 0f, gf);
            // Alféizar: bloque y vierteaguas inclinado.
            c.Poly(new[] { V(-22f, 0f), V(22f, 0f), V(22f, 7f), V(-22f, 7f) }, stone, 1.1f, 1.5f, 0f, gf);
            c.Poly(new[] { V(-23.5f, 7f), V(23.5f, 7f), V(22f, WinSill), V(-22f, WinSill) }, WindowStone, 1.15f, 0f, 0f, gf, 0f, 0.8f);
            for (int i = -2; i <= 2; i++) c.Capsule(V(i * 9f, 0.5f), V(i * 9f, 6.5f), 0.4f, 0.4f, Socket, 1.12f, 0f, gf);

            // Tracería: parteluz, arquillos, óculo y, si hay una sola luz, travesaño y lóbulos.
            int gt = c.NewGroup();
            float mullionTop = spec.Mullion ? WinSub : spec.TransomY;
            if (mullionTop > 0f)
            {
                c.Custom(-2f, WinSill, 2f, mullionTop + 1f, (px, py) =>
                {
                    if (Mathf.Abs(px) > 1.4f || py > mullionTop + 0.5f) return (false, N3.Front);
                    return (true, Cylinder(px / 1.6f));
                }, WindowStone, 1.6f, 0f, gt);
                c.Poly(new[] { V(-2.2f, WinSill), V(2.2f, WinSill), V(1.6f, WinSill + 2.5f), V(-1.6f, WinSill + 2.5f) }, WindowStone, 1.65f, 1f, 0f, gt);
            }
            if (!spec.Mullion && spec.TransomY > 0f)
            {
                c.Poly(new[] { V(-WinW - 0.5f, spec.TransomY), V(WinW + 0.5f, spec.TransomY), V(WinW + 0.5f, spec.TransomY + 3f), V(-WinW - 0.5f, spec.TransomY + 3f) }, WindowStone, 1.62f, 1.2f, 0f, gt);
            }
            if (spec.Mullion)
            {
                c.Custom(-WinW, WinSub - 1f, WinW, H, (px, py) =>
                {
                    if (py < WinSub) return (false, N3.Front);
                    if (ArchDist(px, py, WinW, WinSpring, WinRadius) > 0f) return (false, N3.Front);
                    float dl = ArchDist(px + subCx, py, SubW, WinSub, subR);
                    float dr = ArchDist(px - subCx, py, SubW, WinSub, subR);
                    float doc = Mathf.Sqrt(px * px + (py - OculusY) * (py - OculusY)) - WinOculusR;
                    if (dl < 0f || dr < 0f || doc < 0f) return (false, N3.Front);
                    float edge = Mathf.Min(Mathf.Min(dl, dr), doc);
                    float k = Mathf.Clamp01(1f - edge / 1.6f);
                    return (true, new N3(0f, 0f, 1f - k * 0.3f).Normalized());
                }, WindowStone, 1.55f, 0f, gt);
                // Anillo moldurado del óculo.
                c.Custom(-WinOculusR - 2f, OculusY - WinOculusR - 2f, WinOculusR + 2f, OculusY + WinOculusR + 2f, (px, py) =>
                {
                    float d = Mathf.Sqrt(px * px + (py - OculusY) * (py - OculusY));
                    if (d < WinOculusR || d > WinOculusR + 1.5f) return (false, N3.Front);
                    float u = (d - WinOculusR - 0.75f) / 0.75f;
                    return (true, new N3(px / d * u, (py - OculusY) / d * u, Mathf.Sqrt(Mathf.Max(0.05f, 1f - u * u))).Normalized());
                }, WindowStone, 1.6f, 0f, gt);
            }
            if (spec.Cusps)
            {
                // Dos lóbulos que dan al arco de una sola luz forma trilobulada.
                for (int s = -1; s <= 1; s += 2)
                {
                    c.Poly(new[] { V(s * (WinW + 1f), WinSpring + 1f), V(s * (WinW - 4.5f), WinSpring + 9.5f), V(s * (WinW - 0.5f), WinSpring + 14f) }, WindowStone, 1.55f, 1f, 0f, gt);
                    c.Ellipse(V(s * (WinW - 4.2f), WinSpring + 9.5f), 1.1f, 1.1f, 0f, WindowStone, 1.56f, 0f, gt);
                }
            }

            var color = c.Render(out var normal, out _);
            var emission = new PixelCanvas(W, H);
            PaintGlass(color, normal, emission, W * 0.5f, 0f,
                (px, py) => py > WinSill && ArchDist(px, py, WinW, WinSpring, WinRadius) < 0f,
                spec.Design, spec.Seed);
            return new PropSprite { Name = spec.Name, Color = color, Normal = normal, Emission = emission, Pivot01 = new Vector2(0.5f, 0f) };
        }

        // Centro de cada luz del par de lancetas.
        static float LightCenter(float x) => x < 0f ? -(1.4f + SubW) : (1.4f + SubW);

        /// <summary>Ojo de pupila rasgada dentro de un círculo (óculo o centro del rosetón).</summary>
        static Glass EyeGlass(float dx, float dy, float size, int bgKey, int salt)
        {
            float wx = dx / (6.2f * size);
            float half = 3.3f * size * Mathf.Max(0f, 1f - wx * wx);
            float ir = 2.8f * size;
            if (Mathf.Abs(dy) <= half)
            {
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (Mathf.Abs(dx) < 0.5f * size + 0.1f && Mathf.Abs(dy) < ir - 0.4f) return Glass.Leaded;
                if (d < ir) return new Glass(Red, salt + 1 + (dy > 0f ? 0 : 1), d > ir - 0.9f, 1);
                return new Glass(Amber, salt + 3 + (dx > 0f ? 1 : 0), Mathf.Abs(dy) > half - 0.7f, 1);
            }
            // Párpados violetas y rayos alrededor.
            float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            int ray = Mathf.FloorToInt(Mathf.Repeat(ang + 22.5f, 360f) / 45f);
            return ray % 2 == 0 ? Bg(bgKey, salt + 10 + ray) : new Glass(Violet, salt + 10 + ray);
        }

        /// <summary>El Signo Antiguo: estrella de cinco puntas con el ojo-llama en el centro.</summary>
        static Glass ElderSignGlass(float dx, float dy, float r, int bgKey, int salt)
        {
            float ang = Mathf.Atan2(dy, dx) - Mathf.PI * 0.5f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float sector = Mathf.Repeat(ang, Mathf.PI * 2f / 5f) - Mathf.PI / 5f;
            float k = Mathf.Abs(sector) / (Mathf.PI / 5f);
            float edge = Mathf.Lerp(r * 0.98f, r * 0.4f, k);
            if (d < edge)
            {
                // Llama-ojo en el centro.
                if (Mathf.Abs(dx) < 1.1f && dy > -r * 0.32f && dy < r * 0.38f) return new Glass(Red, salt + 1, Mathf.Abs(dx) < 0.4f && Mathf.Abs(dy) < 0.6f, 1);
                int arm = Mathf.FloorToInt(Mathf.Repeat(ang + Mathf.PI / 5f, Mathf.PI * 2f) / (Mathf.PI * 2f / 5f));
                return new Glass(Amber, salt + 2 + arm, false, 1);
            }
            int ring = Mathf.FloorToInt(Mathf.Repeat(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 18f, 360f) / 36f);
            return Bg(bgKey, salt + 20 + ring);
        }

        // Ventana del Ojo: el ojo de pupila rasgada en el óculo y, en cada luz, un tentáculo turquesa que sube
        // hacia él desde las olas, sobre losanges rojo sangre.
        static PropSprite WindowEye()
        {
            var spec = new WindowSpec { Name = "vidriera_a", Seed = 11 };
            spec.Design = (x, y) =>
            {
                if (y > WinSub + SubW * 1.73f + 0.6f) return EyeGlass(x, y - OculusY, 1.22f, Night, 900);
                float lc = LightCenter(x), u = x - lc, side = x < 0f ? -1f : 1f;
                int L = x < 0f ? 0 : 100;
                // Cenefa de olas en la base.
                if (y < WinSill + 9f)
                {
                    if (y < WinSill + 2f) return new Glass(Amber, 1 + L);
                    float wave = WinSill + 5.5f + 1.6f * Mathf.Sin(x * 0.9f);
                    return y < wave ? Bg(Night, 2 + Mathf.FloorToInt((x + 20f) / 4f)) : new Glass(Violet, 3 + Mathf.FloorToInt((x + 20f) / 5f), Mathf.Abs(y - wave) < 0.6f);
                }
                // Tentáculo que sube ondulando y se curva hacia el óculo.
                float y0 = WinSill + 9f, y1 = WinSub + 8f;
                float t = (y - y0) / (y1 - y0);
                if (t < 1f)
                {
                    float cu = side * 2.2f * Mathf.Sin(t * 7.5f + 0.4f) * (1f - t * 0.5f) - side * t * t * 3.5f;
                    float th = Mathf.Lerp(3.4f, 0.7f, t);
                    float du = (u - cu) * side;
                    if (Mathf.Abs(du) < th)
                    {
                        bool sucker = du > th - 1.5f && Mathf.Repeat(y, 4f) < 1.5f && th > 1.4f;
                        int seg = Mathf.FloorToInt((y - du * 0.6f) / 7f);
                        return new Glass(Teal, 40 + seg + L, false, sucker ? 1 : 0);
                    }
                }
                // Burbujas pálidas que suben al lado.
                for (int i = 0; i < 3; i++)
                {
                    float bx = -side * 3.6f + (i % 2) * side * 1.4f, by = 28f + i * 12f;
                    float rr = i == 1 ? 1.5f : 1.1f;
                    if ((u - bx) * (u - bx) + (y - by) * (y - by) < rr * rr) return new Glass(Pale, 70 + i + L);
                }
                // Gota ámbar en la cabeza del arquillo.
                if (Mathf.Abs(u) * 1.3f + Mathf.Abs(y - (WinSub + 3.5f)) < 2.6f) return new Glass(Amber, 60 + L, false, 1);
                return Bg(Red, Quarry(x, y, 5.5f, 1));
            };
            return GothicWindow(spec);
        }

        // Ventana de Dagón: una sola luz trilobulada con el dios-pez en oración (brazos alzados, barba de tentáculos,
        // nimbo ámbar) y, bajo el travesaño, dos luces con olas y peces separadas por el parteluz.
        static PropSprite WindowDagon()
        {
            var spec = new WindowSpec { Name = "vidriera_b", Mullion = false, TransomY = WinSill + 17f, Cusps = true, Seed = 23 };
            float tY = spec.TransomY;
            spec.Design = (x, y) =>
            {
                float ax = Mathf.Abs(x);
                int L = x < 0f ? 0 : 1;
                if (y < tY + 1f)
                {
                    // Olas y un pez ámbar en cada luz baja.
                    float u = x - LightCenter(x);
                    float fx = u * (x < 0f ? 1f : -1f);
                    float fy = y - (WinSill + 9f);
                    if ((fx + 0.5f) * (fx + 0.5f) / 12f + fy * fy / 3.4f < 1f) return new Glass(Amber, 1 + L, Mathf.Abs(fx - 2.2f) < 0.5f && Mathf.Abs(fy - 0.5f) < 0.6f, 1);
                    if (fx < -3.4f && fx > -6.4f && Mathf.Abs(fy) < (fx + 6.4f) * 0.8f) return new Glass(Amber, 3 + L, false, 1);
                    float wy = y - WinSill + 1.5f * Mathf.Sin(x * 0.8f);
                    int band = Mathf.FloorToInt(wy / 5f);
                    return band % 2 == 0 ? Bg(Night, 10 + band * 4 + L) : Bg(Teal, 10 + band * 4 + L);
                }
                // Cabeza de pez (verde, mandíbula pálida) con ojos saltones ámbar a los lados.
                float hcy = 87f;
                for (int s = -1; s <= 1; s += 2)
                {
                    float ex = x - s * 3.9f, ey = y - (hcy + 1.2f);
                    if (ex * ex + ey * ey < 2.1f) return new Glass(Amber, 3 + (s < 0 ? 0 : 1), ex * ex + ey * ey < 0.5f, 1);
                }
                float hx = x / 4.4f, hy = (y - hcy) / 5.8f;
                if (hx * hx + hy * hy < 1f)
                {
                    bool mouth = Mathf.Abs(y - (hcy - 3f)) < 0.5f && ax < 3.2f;
                    if (y < hcy - 2.5f) return new Glass(Pale, 2, mouth, 1);
                    return new Glass(Green, 1, mouth || (Mathf.Abs(ax - 2.4f) < 0.45f && y < hcy + 0.5f), 1);
                }
                // Barba de tentáculos violeta.
                if (y > 69f && y < hcy - 4f && ax < 4.6f)
                {
                    float lane = x + 1.1f * Mathf.Sin(y * 0.7f + x);
                    float k = Mathf.Repeat(lane + 4.5f, 2.25f);
                    if (k < 1.4f && y > 69f + ax * 1.2f) return new Glass(Violet, 5 + Mathf.FloorToInt((lane + 4.5f) / 2.25f), false, 1);
                }
                // Brazos verdes alzados en oración, con manos palmeadas pálidas.
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector2 sh = V(s * 7f, 77f), hand = V(s * 12.2f, 89.5f);
                    Vector2 d = Sub(hand, sh);
                    float tt = Mathf.Clamp01(((x - sh.x) * d.x + (y - sh.y) * d.y) / Vector2.Dot(d, d));
                    Vector2 q = Add(sh, Scale(d, tt));
                    float dist = Mathf.Sqrt((x - q.x) * (x - q.x) + (y - q.y) * (y - q.y));
                    if (dist < 1.9f && tt < 0.98f) return new Glass(Green, 7 + (s < 0 ? 0 : 1), false, 0);
                    float fx2 = x - hand.x, fy2 = y - hand.y;
                    if (Mathf.Abs(fx2) < 2.1f && fy2 > -0.8f && fy2 < 4.2f && (fy2 < 1.2f || Mathf.Repeat(fx2 + 2.1f, 1.4f) < 0.8f)) return new Glass(Pale, 12 + (s < 0 ? 0 : 1), false, 1);
                }
                // Nimbo ámbar tras la cabeza.
                float hd = Mathf.Sqrt(x * x + (y - hcy) * (y - hcy));
                if (hd < 9.8f && y > 78f) return new Glass(Amber, 20 + Mathf.FloorToInt(Mathf.Repeat(Mathf.Atan2(y - hcy, x) * Mathf.Rad2Deg + 360f, 360f) / 30f), hd > 9f, hd > 8f ? 0 : -1);
                // Túnica roja con estola ámbar y pliegues.
                float robeTop = 79.5f - ax * ax * 0.07f;
                float hw = Mathf.Lerp(12f, 7.5f, (y - 40f) / 39f);
                if (y < robeTop && y > 40f && ax < hw)
                {
                    if (ax < 1.6f && y < 78f) return new Glass(Amber, 30 + Mathf.FloorToInt(y / 8f), Mathf.Abs(Mathf.Repeat(y, 8f) - 4f) < 0.5f, 1);
                    if (ax > hw - 1.6f && y < 74f) return new Glass(Amber, 40 + L, false, 0);
                    int fold = Mathf.FloorToInt((x + 12f) / 3.6f);
                    return new Glass(Red, 50 + fold, false, fold % 2 == 0 ? 0 : 1);
                }
                // Peana violeta.
                if (y <= 40f && y > tY + 3f) return new Glass(Violet, 60 + Mathf.FloorToInt((x + 16f) / 6f), Mathf.Abs(y - 37f) < 0.5f);
                // Fondo: losanges turquesa y noche, con estrellas ámbar en la cabeza del arco.
                if (y > 97f && Mathf.Abs(x) + Mathf.Abs(y - 101f) < 2f) return new Glass(Amber, 70, false, 1);
                int q2 = Quarry(x, y, 5.5f, 2);
                return Bg(PixelCanvas.Hash(q2, 3, 5) > 0.75f ? Night : Teal, q2);
            };
            return GothicWindow(spec);
        }

        // Ventana del Signo: el Signo Antiguo en el óculo y un pez que asciende en cada luz, sobre violeta.
        static PropSprite WindowSign()
        {
            var spec = new WindowSpec { Name = "vidriera_c", Seed = 37 };
            spec.Design = (x, y) =>
            {
                if (y > WinSub + SubW * 1.73f + 0.6f) return ElderSignGlass(x, y - OculusY, WinOculusR, Teal, 800);
                float lc = LightCenter(x), u = x - lc, side = x < 0f ? -1f : 1f;
                int L = x < 0f ? 0 : 10;
                // Cenefa roja con dientes ámbar.
                if (y < WinSill + 6f)
                {
                    bool tooth = Mathf.Repeat(x, 4f) < 2f && y > WinSill + 3f;
                    return new Glass(tooth ? Amber : Red, 1 + Mathf.FloorToInt((x + 20f) / 4f));
                }
                // Pez que asciende (cabeza arriba), ligeramente curvado.
                float fy = 40f, fh = 17f;
                float bend = side * 1.1f * Mathf.Sin((y - fy) * 0.12f);
                float lu = u - bend;
                float t = (y - fy) / fh;
                if (Mathf.Abs(t) < 1f)
                {
                    float half = 4.6f * Mathf.Sqrt(Mathf.Max(0f, 1f - t * t)) * (t < 0f ? 1f + t * 0.3f : 1f);
                    if (Mathf.Abs(lu) < half)
                    {
                        if (t > 0.6f && Mathf.Abs(lu - side * 1.4f) < 0.9f && Mathf.Abs(y - (fy + fh * 0.7f)) < 0.9f) return Glass.Leaded;
                        bool gill = Mathf.Abs(t - 0.48f) < 0.035f;
                        bool scale = t < 0.4f && t > -0.75f && Mathf.Repeat(y + Mathf.Abs(lu) * 0.8f, 3.4f) < 0.6f && Mathf.Abs(lu) < half - 1f;
                        int piece = t > 0.48f ? 1 : (lu < 0f ? 2 : 3);
                        return new Glass(t > 0.48f ? Green : Teal, 10 + piece + L, gill || scale, 1);
                    }
                    // Aletas ámbar.
                    if (t > -0.25f && t < 0.2f && Mathf.Abs(lu) < half + 2.6f - (t + 0.25f) * 4f && Mathf.Abs(lu) > half) return new Glass(Amber, 30 + (lu < 0f ? 0 : 1) + L, false, 1);
                }
                // Cola.
                float ty = fy - fh - 1f;
                if (y < ty + 1f && y > ty - 5.5f && Mathf.Abs(u - bend) < (ty + 1f - y) * 0.85f) return new Glass(Amber, 34 + L, Mathf.Abs(u - bend) < 0.4f, 1);
                // Burbujas que salen de la boca.
                for (int i = 0; i < 3; i++)
                {
                    float bx = side * (i % 2 == 0 ? 1.2f : -1.6f), by = fy + fh + 3f + i * 4f;
                    float rr = i == 1 ? 1.5f : 1.1f;
                    if ((u - bx) * (u - bx) + (y - by) * (y - by) < rr * rr) return new Glass(Pale, 40 + i + L);
                }
                // Lengua de fuego roja en la cabeza del arquillo.
                if (y > WinSub - 4f && Mathf.Abs(u) < (WinSub + 9f - y) * 0.32f) return new Glass(Red, 50 + L, Mathf.Abs(u) < 0.4f && y < WinSub + 2f, 1);
                int qq = Quarry(x, y, 5.5f, 3);
                return Bg(PixelCanvas.Hash(qq, 1, 9) > 0.72f ? Night : Violet, qq);
            };
            return GothicWindow(spec);
        }

        // Rosetón: anillo moldurado, doce pétalos alternos rojo/turquesa, rondeles violeta y el Ojo en el centro.
        static PropSprite RoseWindow()
        {
            const int S = 96;
            var c = new ShadedCanvas(S, S, S * 0.5f, S * 0.5f);
            const float rGlass = 38f, rOuter = 47f, rHub = 10.5f, rHubRing = 13f, rPetal = 30.5f, rRondel = 34.8f;
            const int spokes = 12;
            float sector = Mathf.PI * 2f / spokes;

            System.Func<float, float, (float r, float a, float local)> Polar = (px, py) =>
            {
                float r = Mathf.Sqrt(px * px + py * py);
                float a = Mathf.Atan2(py, px);
                float local = Mathf.Repeat(a + Mathf.PI * 0.5f, sector) - sector * 0.5f; // 0 = centro del pétalo
                return (r, a, local);
            };
            // Pétalo apuntado: semiancho tangencial en función del radio.
            System.Func<float, float, float> PetalHalf = (r, dummy) =>
            {
                float full = r * Mathf.Sin(sector * 0.5f) - 1.3f;
                if (r < rHubRing + 1f) return -1f;
                if (r > rPetal) return -1f;
                float tip = Mathf.InverseLerp(rPetal - 9f, rPetal, r);
                return Mathf.Min(full, full * Mathf.Sqrt(Mathf.Max(0f, 1f - tip * tip)) + (tip > 0f ? 0f : 0f));
            };
            System.Func<float, float, bool> RondelHole = (px, py) =>
            {
                var p = Polar(px, py);
                float cx = rRondel, tx = p.r * Mathf.Cos(p.local) - cx, ty = p.r * Mathf.Sin(p.local);
                return tx * tx + ty * ty < 3.3f * 3.3f;
            };
            System.Func<float, float, bool> Opening = (px, py) =>
            {
                var p = Polar(px, py);
                if (p.r < rHub) return true;
                if (p.r > rGlass) return false;
                float hw = PetalHalf(p.r, 0f);
                if (hw > 0f && Mathf.Abs(p.r * Mathf.Sin(p.local)) < hw) return true;
                // Rondeles entre las puntas de los pétalos (desfasados medio sector).
                var q = Polar(px * Mathf.Cos(sector * 0.5f) - py * Mathf.Sin(sector * 0.5f), px * Mathf.Sin(sector * 0.5f) + py * Mathf.Cos(sector * 0.5f));
                float tx = q.r * Mathf.Cos(q.local) - (rRondel - 4f), ty = q.r * Mathf.Sin(q.local);
                if (tx * tx + ty * ty < 2.6f * 2.6f) return true;
                return RondelHole(px, py);
            };

            int g = c.NewGroup();
            // Anillo exterior moldurado (bocel + paramento con dovelas).
            c.Custom(-rOuter - 1f, -rOuter - 1f, rOuter + 1f, rOuter + 1f, (px, py) =>
            {
                float r = Mathf.Sqrt(px * px + py * py);
                if (r < rGlass || r > rOuter) return (false, N3.Front);
                float ox = px / r, oy = py / r;
                float rho = r - rGlass;
                if (rho < 1.5f) return (true, new N3(-ox * 0.75f, -oy * 0.75f, 0.65f).Normalized());
                if (rho < 4.5f)
                {
                    float u = (rho - 3f) / 1.5f;
                    return (true, new N3(ox * u, oy * u, Mathf.Sqrt(Mathf.Max(0.05f, 1f - u * u))).Normalized());
                }
                if (rho < 7.5f) return (true, N3.Front);
                return (true, new N3(ox * 0.7f, oy * 0.7f, 0.7f).Normalized());
            }, new PixelMaterial(WindowStone.Ramp)
            {
                Rim = WindowStone.Rim, Ambient = WindowStone.Ambient, Dither = WindowStone.Dither,
                Texture = (ix, iy) =>
                {
                    float px = ix + 0.5f - S * 0.5f, py = iy + 0.5f - S * 0.5f;
                    float r = Mathf.Sqrt(px * px + py * py);
                    float a = Mathf.Atan2(py, px) * Mathf.Rad2Deg;
                    float gr = StoneGrain(ix, iy);
                    if (r > rGlass + 4.5f && Mathf.Repeat(a + 7.5f, 15f) < 1.4f) return gr - 0.22f;
                    return gr;
                },
            }, 1f, 0f, g);
            // Tracería interior: todo lo que no es vidrio dentro del anillo.
            int gt = c.NewGroup();
            c.Custom(-rGlass, -rGlass, rGlass, rGlass, (px, py) =>
            {
                float r = Mathf.Sqrt(px * px + py * py);
                if (r >= rGlass) return (false, N3.Front);
                if (Opening(px, py)) return (false, N3.Front);
                // Bisel suave: busca vidrio cerca para inclinar la normal.
                float nx = 0f, ny = 0f;
                for (int k = 0; k < 8; k++)
                {
                    float a = k * Mathf.PI / 4f;
                    float sx = Mathf.Cos(a) * 1.5f, sy = Mathf.Sin(a) * 1.5f;
                    float rr = Mathf.Sqrt((px + sx) * (px + sx) + (py + sy) * (py + sy));
                    if (rr < rGlass && Opening(px + sx, py + sy)) { nx -= sx; ny -= sy; }
                }
                return (true, new N3(nx * 0.25f, ny * 0.25f, 1f).Normalized());
            }, WindowStone, 1.5f, 0f, gt);
            // Anillo del cubo central.
            c.Custom(-rHubRing - 1f, -rHubRing - 1f, rHubRing + 1f, rHubRing + 1f, (px, py) =>
            {
                float r = Mathf.Sqrt(px * px + py * py);
                if (r < rHub || r > rHubRing) return (false, N3.Front);
                float u = (r - (rHub + rHubRing) * 0.5f) / ((rHubRing - rHub) * 0.5f);
                return (true, new N3(px / r * u, py / r * u, Mathf.Sqrt(Mathf.Max(0.05f, 1f - u * u))).Normalized());
            }, WindowStone, 1.6f, 0f, gt);

            var color = c.Render(out var normal, out _);
            var emission = new PixelCanvas(S, S);
            PaintGlass(color, normal, emission, S * 0.5f, S * 0.5f, (px, py) => Mathf.Sqrt(px * px + py * py) < rGlass && Opening(px, py), (x, y) =>
            {
                var p = Polar(x, y);
                if (p.r < rHub + 0.5f) return EyeGlass(x, y, 1.5f, Night, 900);
                int petal = Mathf.FloorToInt(Mathf.Repeat(p.a + Mathf.PI * 0.5f, Mathf.PI * 2f) / sector);
                if (p.r > rPetal - 0.5f)
                {
                    return new Glass(Violet, 200 + petal * 3 + (p.r > rRondel + 1f ? 1 : 0), false, 0);
                }
                // Pétalo: llama clara en el eje, cuerpo rojo o turquesa.
                float tang = p.r * Mathf.Sin(p.local);
                int key = petal % 2 == 0 ? Red : Teal;
                float flame = Mathf.Lerp(0.4f, 2.4f, Mathf.InverseLerp(16f, 23f, p.r)) * (p.r > 22f ? Mathf.Clamp01((27f - p.r) / 5f) : 1f);
                if (p.r > 16f && Mathf.Abs(tang) < flame) return new Glass(petal % 2 == 0 ? Amber : Pale, 100 + petal, false, 1);
                return new Glass(key, 20 + petal * 4 + (tang < 0f ? 0 : 1), false, 0);
            }, 51);
            return new PropSprite { Name = "vidriera_rosa", Color = color, Normal = normal, Emission = emission, Pivot01 = new Vector2(0.5f, 0.5f) };
        }

        // ------------------------------------------------------------------
        // Haz de luz de la vidriera
        // ------------------------------------------------------------------

        // Haz polvoriento que cae en diagonal desde la ventana (se dibuja con mezcla aditiva): luz cálida con dos
        // lenguas separadas por la sombra tenue del parteluz, teñida de rojo y turquesa en los bordes por el vidrio,
        // que se abre y se desvanece hacia abajo, con motas de polvo.
        static PropSprite WindowRay()
        {
            const int w = 64, h = 160;
            var canvas = new PixelCanvas(w, h);
            var warm = PixelCanvas.Hex("ffd8a2");
            var red = PixelCanvas.Hex("ff9078");
            var teal = PixelCanvas.Hex("96e8d6");
            System.Func<float, float> Center = t => w * 0.5f - t * 26f;
            System.Func<float, float> Half = t => 13f + t * 9f;
            for (int y = 0; y < h; y++)
            {
                float t = 1f - (y + 0.5f) / h; // 0 arriba, 1 abajo
                float center = Center(t), half = Half(t);
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f - center) / half;
                    if (Mathf.Abs(u) > 1f) continue;
                    float edge = Mathf.Clamp01((1f - Mathf.Abs(u)) * 2.6f);
                    float fall = Mathf.Pow(1f - t, 1.15f) * Mathf.Clamp01(t / 0.05f);
                    float mull = Mathf.Abs(u) < 0.08f ? 0.7f : 1f;
                    float bands = 0.88f + 0.12f * Mathf.Sin(u * 7.5f + 0.8f);
                    float border = Mathf.Clamp01((x + 0.5f) / 7f) * Mathf.Clamp01((w - x - 0.5f) / 4f);
                    float a = 0.42f * edge * fall * mull * bands * border;
                    a = Mathf.Floor(a * 14f) / 14f;
                    if (a <= 0f) continue;
                    Color32 col = u < -0.55f ? PixelCanvas.Lerp(warm, red, 0.6f) : (u > 0.55f ? PixelCanvas.Lerp(warm, teal, 0.55f) : warm);
                    canvas.Pixels[y * w + x] = PixelCanvas.WithAlpha(col, a);
                }
            }
            // Motas de polvo en suspensión (más brillantes arriba, cerca de la ventana).
            for (int i = 0; i < 52; i++)
            {
                float t = PixelCanvas.Hash(i, 1, 71) * 0.9f;
                float u = PixelCanvas.Hash(i, 2, 71) * 1.6f - 0.8f;
                int y = Mathf.FloorToInt((1f - t) * h);
                int x = Mathf.FloorToInt(Center(t) + u * Half(t));
                if (!canvas.InBounds(x, y) || canvas.Get(x, y).a < 10) continue;
                float bright = Mathf.Lerp(0.8f, 0.3f, t);
                canvas.Pixels[y * w + x] = new Color32(255, 244, 220, (byte)Mathf.RoundToInt(bright * 255f));
                if (PixelCanvas.Hash(i, 3, 71) > 0.75f && canvas.InBounds(x + 1, y)) canvas.Pixels[y * w + x + 1] = new Color32(255, 236, 205, (byte)Mathf.RoundToInt(bright * 110f));
            }
            return new PropSprite { Name = "rayo_vidriera", Color = canvas, Unlit = true, Pivot01 = new Vector2(0.5f, 1f) };
        }
    }
}
