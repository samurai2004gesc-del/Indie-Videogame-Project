using System.Collections.Generic;
using UnityEngine;
using static Abismo.EditorTools.Rig;

namespace Abismo.EditorTools
{
    /// <summary>
    /// Ojo del Vacío: un globo ocular del tamaño de una cabeza, de esclerótica amarillenta surcada de venas, con iris rojo
    /// (el ÚNICO acento de su paleta) y pupila horizontal de cabra. Lo sostienen dos alas membranosas de murciélago y le
    /// cuelgan seis tentáculos con ventosas que se mueven con retraso (movimiento secundario). Mira a la derecha.
    ///
    /// Claves de pose:
    ///  - "x","y": desplazamiento del cuerpo; "sx","sy": estiramiento del globo (0 = redondo); "tilt": giro (grados)
    ///  - "lid","lidLow": párpado superior / inferior (0 abierto, 1 cerrado, negativo = desorbitado)
    ///  - "dil": dilatación de la pupila (0 = rendija, 1 = redonda); "vein": brillo de las venas (0..1)
    ///  - "wing": aleteo (−10 alas arriba … 55 abajo, 75 plegadas); "tPhase","tAmp","spread","trail","droop": tentáculos
    ///  - "lookX","lookY": hacia dónde mira el iris (píxeles)
    /// </summary>
    public sealed class OjoArt : CharacterArt
    {
        public override string Id => "ojo";
        public override int FrameWidth => 96;
        public override int FrameHeight => 96;
        public override Vector2 Pivot => new Vector2(48f, 48f);

        const float R = 13.5f;

        // ------------------------------------------------------------------
        // Materiales
        // ------------------------------------------------------------------

        static readonly PixelMaterial Sclera = new PixelMaterial(Ramp.Make("d3c59c", 5, 0.12f, 0.3f, 1.32f)) { Gloss = 0.6f, Rim = 0.55f, Ambient = 0.22f, Dither = 0.03f };
        static readonly PixelMaterial Flesh = new PixelMaterial(Ramp.Make("5b3c5c", 5, 0.1f, 0.32f, 1.6f)) { Rim = 0.65f, Ambient = 0.24f, Dither = 0.04f };
        static readonly PixelMaterial LidRim = new PixelMaterial(Ramp.Make("8e4a5f", 4, 0.08f, 0.4f, 1.4f)) { Rim = 0.4f, Ambient = 0.3f, Dither = 0f, Gloss = 0.3f };
        static readonly PixelMaterial Membrane = new PixelMaterial(Ramp.Make("3d2b47", 4, 0.1f, 0.4f, 1.75f)) { Rim = 0.55f, Ambient = 0.22f, Dither = 0.05f };
        static readonly PixelMaterial Bone = new PixelMaterial(Ramp.Make("6e5b73", 4, 0.06f, 0.45f, 1.45f)) { Rim = 0.5f, Ambient = 0.3f, Dither = 0f };
        static readonly PixelMaterial Vein = new PixelMaterial(Ramp.Make("8b2533", 3, 0.06f, 0.55f, 1.3f)) { Outline = false, Dither = 0f, Ambient = 0.4f, ReceivesContactShadow = false };
        static readonly PixelMaterial VeinHot = PixelMaterial.Glow("ff5a3e");
        static readonly PixelMaterial VeinWarm = PixelMaterial.Glow("c42a30");
        static readonly PixelMaterial IrisOuter = PixelMaterial.Glow("5a0b1a");
        static readonly PixelMaterial IrisMid = PixelMaterial.Glow("a8182b");
        static readonly PixelMaterial IrisInner = PixelMaterial.Glow("ec4630");
        static readonly PixelMaterial IrisHot = PixelMaterial.Glow("ffb27a");
        static readonly PixelMaterial Pupil = PixelMaterial.Glow("0c0509");
        static readonly PixelMaterial Ichor = new PixelMaterial(Ramp.Make("4a1236", 4, 0.08f, 0.4f, 1.6f)) { Gloss = 0.5f, Rim = 0.3f, Ambient = 0.35f, Dither = 0f };
        static readonly PixelMaterial IchorGlow = PixelMaterial.Glow("d0407a");
        static readonly PixelMaterial FlashCore = PixelMaterial.Glow("fff1e0");
        static readonly PixelMaterial FlashMid = PixelMaterial.Glow("ff7a5ac0");
        static readonly PixelMaterial FlashOut = PixelMaterial.Glow("a02a5a70");
        static readonly PixelMaterial Streak = PixelMaterial.Glow("bfaad060");
        static readonly Color32 Glint = PixelCanvas.Hex("fff6e6");
        static readonly Color32 Sucker = PixelCanvas.Hex("b48aa0");

        // ------------------------------------------------------------------
        // Poses
        // ------------------------------------------------------------------

        static Pose Hover => new Pose().With(
            ("x", 0f), ("y", 0f), ("sx", 0f), ("sy", 0f), ("tilt", 0f),
            ("lid", 0.14f), ("lidLow", 0.1f), ("dil", 0.25f), ("vein", 0f),
            ("wing", 20f), ("tPhase", 0f), ("tAmp", 12f), ("spread", 1f), ("trail", 0f), ("droop", 0f),
            ("lookX", 0f), ("lookY", 0f));

        public override List<AnimSpec> Animations() => new List<AnimSpec>
        {
            new AnimSpec("fly", 6, 10f, true, DrawFly),
            new AnimSpec("windup", 6, 11f, false, DrawWindup),
            new AnimSpec("dive", 4, 12f, true, DrawDive),
            new AnimSpec("hurt", 2, 12f, false, DrawHurt),
            new AnimSpec("stagger", 4, 8f, true, DrawStagger),
            new AnimSpec("death", 8, 14f, false, DrawDeath),
        };

        // ------------------------------------------------------------------
        // Animaciones
        // ------------------------------------------------------------------

        // Aleteo: las alas bajan mientras el cuerpo sube (con un poco de retraso); los tentáculos ondulan detrás.
        ShadedCanvas DrawFly(int f)
        {
            float t = f / 6f * Mathf.PI * 2f;
            var p = Hover.With(("wing", 22f - 32f * Mathf.Cos(t)), ("y", -1.6f * Mathf.Cos(t - 0.9f)),
                               ("sy", 0.025f * Mathf.Cos(t - 0.9f)), ("sx", -0.02f * Mathf.Cos(t - 0.9f)),
                               ("tPhase", t), ("lookX", 0.6f * Mathf.Sin(t)), ("lookY", 0.5f * Mathf.Cos(t * 2f)),
                               ("tilt", 2f * Mathf.Sin(t - 0.5f)));
            return Draw(p, f / 6f);
        }

        // Carga del picado: se echa atrás, la pupila se dilata y las venas se encienden. Tiembla al final.
        ShadedCanvas DrawWindup(int f)
        {
            var coil = Hover.With(("x", -4f), ("y", 2f), ("sx", -0.05f), ("sy", 0.07f), ("tilt", 8f), ("lid", -0.25f), ("lidLow", -0.15f),
                                  ("dil", 1f), ("vein", 1f), ("wing", -12f), ("tAmp", 20f), ("spread", 1.8f), ("droop", -0.3f));
            var keys = new Keyframes()
                .Key(0, Hover.With(("x", -0.5f), ("sx", 0.04f), ("sy", -0.04f), ("lid", 0.3f), ("lidLow", 0.25f), ("wing", 40f), ("dil", 0.3f)))
                .Key(1, Hover.With(("x", -1.5f), ("y", -1f), ("sx", 0.08f), ("sy", -0.07f), ("lid", 0.45f), ("lidLow", 0.35f), ("wing", 55f), ("dil", 0.2f), ("tAmp", 16f)), Ease.Out)
                .Key(3, coil.With(("vein", 0.7f), ("dil", 0.8f)), Ease.Out)
                .Key(5, coil);
            var p = keys.Evaluate(f);
            p["tPhase"] = f * 0.9f;
            if (f >= 4) p["x"] += f % 2 == 0 ? 0.7f : -0.7f;
            return Draw(p, f / 6f);
        }

        // Picado: el globo se estira hacia delante, las alas se pliegan y los tentáculos se arrastran detrás.
        ShadedCanvas DrawDive(int f)
        {
            float t = f / 4f * Mathf.PI * 2f;
            var p = Hover.With(("x", 3f), ("y", 0f), ("sx", 0.24f), ("sy", -0.17f), ("tilt", 0f), ("lid", 0.3f), ("lidLow", 0.28f),
                               ("dil", 0.7f), ("vein", 0.65f), ("wing", 72f + 4f * Mathf.Sin(t)), ("tPhase", t * 1.5f), ("tAmp", 8f),
                               ("spread", 0.5f), ("trail", 1f), ("lookX", 1.5f));
            return Draw(p, f / 4f, streaks: f);
        }

        ShadedCanvas DrawHurt(int f)
        {
            var p = f == 0
                ? Hover.With(("x", -3f), ("y", 1f), ("sx", 0.13f), ("sy", -0.11f), ("tilt", -6f), ("lid", 0.85f), ("lidLow", 0.85f),
                             ("wing", -18f), ("tAmp", 26f), ("tPhase", 2.4f), ("spread", 1.6f), ("dil", 0.1f))
                : Hover.With(("x", -1.5f), ("y", 0.5f), ("sx", 0.04f), ("sy", -0.03f), ("tilt", -3f), ("lid", 0.45f), ("lidLow", 0.4f),
                             ("wing", 5f), ("tAmp", 18f), ("tPhase", 3.6f), ("spread", 1.3f), ("dil", 0.15f));
            return Draw(p, f / 2f);
        }

        // Aturdido: cae un poco, mira al suelo con la pupila contraída, alas lacias y tentáculos colgando.
        ShadedCanvas DrawStagger(int f)
        {
            float t = f / 4f * Mathf.PI * 2f;
            var p = Hover.With(("x", 1.2f * Mathf.Sin(t)), ("y", -2.5f + 0.6f * Mathf.Cos(t)), ("tilt", -14f + 6f * Mathf.Sin(t + 0.6f)),
                               ("lid", 0.52f + 0.08f * Mathf.Sin(t)), ("lidLow", 0.3f), ("dil", 0f), ("wing", 52f + 6f * Mathf.Sin(t * 2f)),
                               ("tAmp", 5f), ("tPhase", t * 0.5f), ("droop", 1f), ("spread", 0.7f), ("lookX", -1f), ("lookY", -1.5f));
            return Draw(p, f / 4f);
        }

        // Muerte: se hincha, las venas arden, revienta en un fogonazo de icor y los restos caen.
        ShadedCanvas DrawDeath(int f)
        {
            if (f <= 2)
            {
                var keys = new Keyframes()
                    .Key(0, Hover.With(("x", -2f), ("sx", 0.1f), ("sy", -0.08f), ("lid", 0.8f), ("lidLow", 0.8f), ("wing", -15f), ("tAmp", 24f), ("tPhase", 2f), ("spread", 1.6f)))
                    .Key(1, Hover.With(("x", -1f), ("sx", 0.12f), ("sy", 0.12f), ("lid", -0.3f), ("lidLow", -0.2f), ("dil", 1f), ("vein", 1f), ("wing", 30f), ("tAmp", 20f), ("tPhase", 3f), ("spread", 1.9f)))
                    .Key(2, Hover.With(("x", -0.5f), ("sx", 0.22f), ("sy", 0.22f), ("lid", -0.5f), ("lidLow", -0.4f), ("dil", 1f), ("vein", 1f), ("wing", 45f), ("tAmp", 24f), ("tPhase", 4f), ("spread", 2.1f)));
                var p = keys.Evaluate(f);
                if (f == 2) p["x"] += 0.8f;
                return Draw(p, f / 8f, cracks: f == 2);
            }
            return DrawBurst(f - 3);
        }

        // ------------------------------------------------------------------
        // Dibujo
        // ------------------------------------------------------------------

        struct Frame
        {
            public Vector2 C;
            public float Sx, Sy, Tilt;

            /// <summary>Punto en coordenadas del globo (sin deformar) → lienzo.</summary>
            public Vector2 P(float lx, float ly) => Rotate(V(C.x + lx * Sx, C.y + ly * Sy), C, Tilt);
        }

        ShadedCanvas Draw(Pose p, float phase, bool cracks = false, int streaks = -1)
        {
            var c = NewFrame();
            var fr = new Frame { C = V(p["x"], 6f + p["y"]), Sx = 1f + p["sx"], Sy = 1f + p["sy"], Tilt = p["tilt"] };

            if (streaks >= 0) DrawStreaks(c, fr, streaks);

            DrawWing(c, fr, p, far: true);
            DrawTentacles(c, fr, p, front: false);
            DrawRoot(c, fr, p, phase);
            DrawGlobe(c, fr, p, cracks);
            DrawTentacles(c, fr, p, front: true);
            DrawWing(c, fr, p, far: false);
            return c;
        }

        // Masa carnosa detrás del globo (de ahí salen alas y tentáculos) y un nervio óptico que se agita como una cola.
        static void DrawRoot(ShadedCanvas c, Frame fr, Pose p, float phase)
        {
            int g = c.NewGroup();
            c.Ellipse(fr.P(-8.5f, -3.5f), 8.6f * fr.Sx, 8f * fr.Sy, fr.Tilt - 20f, Flesh, 1.4f, 0f, g);
            c.Ellipse(fr.P(-4f, -9f), 6.5f * fr.Sx, 4.5f * fr.Sy, fr.Tilt - 8f, Flesh, 1.5f, -0.05f, g);

            float wave = Mathf.Sin(phase * Mathf.PI * 2f) * (1f - p["trail"] * 0.6f);
            var tail = new List<Vector2>();
            Vector2 a = fr.P(-14f, -2f);
            float ang = 175f + 10f * p["trail"] + fr.Tilt;
            tail.Add(a);
            for (int i = 1; i <= 6; i++)
            {
                ang += 9f * wave * (i / 6f) + 4f;
                a = Add(a, Dir(ang, 2.4f));
                tail.Add(a);
            }
            c.Strand(tail, 3f, 0.7f, Flesh, 1.2f, -0.06f, g);
        }

        static void DrawGlobe(ShadedCanvas c, Frame fr, Pose p, bool cracks)
        {
            int g = c.NewGroup();
            c.Ellipse(fr.C, R * fr.Sx, R * fr.Sy, fr.Tilt, Sclera, 2f, 0f, g);

            // Venas: nacen detrás y se arrastran hacia el iris, con alguna rama.
            float vein = p["vein"];
            var veinMat = vein > 0.75f ? VeinHot : vein > 0.3f ? VeinWarm : Vein;
            for (int v = 0; v < VeinPaths.Length; v++)
            {
                var path = VeinPaths[v];
                var pts = new List<Vector2>(path.Length);
                foreach (var q in path) pts.Add(fr.P(q.x, q.y));
                c.Strand(pts, 0.62f, 0.42f, veinMat, 2.1f, 0f, g);
            }

            if (cracks)
            {
                // A punto de reventar: grietas incandescentes que cruzan el globo.
                foreach (var crack in Cracks)
                {
                    var pts = new List<Vector2>();
                    foreach (var q in crack) pts.Add(fr.P(q.x, q.y));
                    c.Strand(pts, 0.9f, 0.6f, IrisHot, 2.2f, 0f, g);
                }
            }

            // Iris en tres anillos (emisivo) y pupila horizontal de cabra.
            float lx = 6.4f + p["lookX"], ly = 0.6f + p["lookY"];
            Vector2 ic = fr.P(lx, ly);
            float irx = 5.6f * fr.Sx, iry = 7.4f * fr.Sy;
            c.Ellipse(ic, irx, iry, fr.Tilt, IrisOuter, 2.3f, 0f, g);
            c.Ellipse(ic, irx - 1.2f, iry - 1.4f, fr.Tilt, IrisMid, 2.35f, 0f, g);
            c.Ellipse(fr.P(lx + 0.4f, ly - 0.6f), irx - 2.6f, iry - 3.2f, fr.Tilt, vein > 0.6f ? IrisHot : IrisInner, 2.4f, 0f, g);
            float dil = Mathf.Clamp01(p["dil"]);
            c.Ellipse(fr.P(lx + 0.5f, ly), (2.4f + 1.4f * dil) * fr.Sx, (0.9f + 3.6f * dil) * fr.Sy, fr.Tilt, Pupil, 2.45f, 0f, g);

            // Brillo húmedo en la córnea.
            Vector2 glint = fr.P(lx - 2.2f, ly + 3.4f);
            c.Decal(glint.x, glint.y, Glint);
            c.Decal(glint.x + 1f, glint.y, Glint);
            c.Decal(glint.x, glint.y - 1f, Glint);

            DrawLids(c, fr, p, g);
        }

        // Párpados membranosos: casquetes de piel sobre la esfera con un borde húmedo más claro.
        static void DrawLids(ShadedCanvas c, Frame fr, Pose p, int g)
        {
            float top0 = Mathf.Lerp(10.8f, 0.6f, p["lid"]);
            float bot0 = Mathf.Lerp(11.2f, 0.6f, p["lidLow"]);
            float rl = R + 1.3f;
            float minX = fr.C.x - rl * 1.4f, maxX = fr.C.x + rl * 1.4f, minY = fr.C.y - rl * 1.4f, maxY = fr.C.y + rl * 1.4f;

            (bool, N3) Sample(float px, float py, bool upper, bool rim)
            {
                Vector2 q = Rotate(V(px, py), fr.C, -fr.Tilt);
                float u = (q.x - fr.C.x) / fr.Sx, v = (q.y - fr.C.y) / fr.Sy;
                float d2 = u * u + v * v;
                if (d2 > rl * rl) return (false, N3.Front);
                float edge = upper ? top0 + 0.022f * (u - 5f) * (u - 5f) : -(bot0 + 0.02f * (u - 5f) * (u - 5f));
                float into = upper ? v - edge : edge - v;
                if (into < 0f) return (false, N3.Front);
                bool isRim = into < 1.4f;
                if (isRim != rim) return (false, N3.Front);
                float z = Mathf.Sqrt(Mathf.Max(0f, rl * rl - d2));
                var n = new N3(u, v, z).Normalized();
                return (true, n);
            }

            c.Custom(minX, minY, maxX, maxY, (x, y) => Sample(x, y, true, false), Flesh, 2.6f, 0f, g);
            c.Custom(minX, minY, maxX, maxY, (x, y) => Sample(x, y, true, true), LidRim, 2.6f, 0f, g);
            c.Custom(minX, minY, maxX, maxY, (x, y) => Sample(x, y, false, false), Flesh, 2.6f, -0.04f, g);
            c.Custom(minX, minY, maxX, maxY, (x, y) => Sample(x, y, false, true), LidRim, 2.6f, -0.04f, g);
        }

        // Ala de murciélago: tres dedos de hueso y membrana con el borde festoneado.
        static void DrawWing(ShadedCanvas c, Frame fr, Pose p, bool far)
        {
            int g = c.NewGroup();
            float w = p["wing"];
            Vector2 shoulder = far ? fr.P(-1f, 11f) : fr.P(-6.5f, 9f);
            float scale = far ? 0.85f : 1f;
            float z = far ? 0.2f : 3.2f, shade = far ? -0.16f : 0f;
            float[] baseAngles = { 98f, 130f, 162f };
            float[] lengths = { 21f, 26f, 18f };
            var tips = new Vector2[3];
            var knuckles = new Vector2[3];
            for (int i = 0; i < 3; i++)
            {
                float a = baseAngles[i] + w * (0.8f + 0.2f * i) + fr.Tilt * 0.5f;
                knuckles[i] = Add(shoulder, Dir(a - 8f, lengths[i] * 0.45f * scale));
                tips[i] = Add(knuckles[i], Dir(a + 6f, lengths[i] * 0.55f * scale));
            }
            Vector2 back = Add(shoulder, Dir(baseAngles[2] + w + 38f, 7f * scale));
            Vector2 Scallop(Vector2 a, Vector2 b) => Mix(Mix(a, b, 0.5f), shoulder, 0.32f);
            var membrane = new[] { shoulder, tips[0], Scallop(tips[0], tips[1]), tips[1], Scallop(tips[1], tips[2]), tips[2], Mix(tips[2], back, 0.55f), back };
            c.Poly(membrane, Membrane, z, 1.5f, shade, g);
            for (int i = 0; i < 3; i++)
            {
                c.Capsule(shoulder, knuckles[i], 1.2f * scale, 0.9f * scale, Bone, z + 0.05f, shade, g);
                c.Capsule(knuckles[i], tips[i], 0.9f * scale, 0.45f, Bone, z + 0.05f, shade, g);
            }
            c.Capsule(Add(tips[0], Dir(baseAngles[0] + w, 0.5f)), Add(tips[0], Dir(baseAngles[0] + w - 40f, 2.5f)), 0.6f, 0.3f, Bone, z + 0.06f, shade, g);
        }

        static readonly Vector2[] TentacleAnchors = { V(-12f, -5f), V(-9f, -10f), V(-4.5f, -12.5f), V(0.5f, -12.8f), V(5f, -11.5f), V(-6.5f, -8f) };
        static readonly float[] TentacleLength = { 19f, 25f, 28f, 23f, 17f, 21f };
        static readonly float[] TentacleBase = { -128f, -108f, -95f, -82f, -68f, -112f };

        static void DrawTentacles(ShadedCanvas c, Frame fr, Pose p, bool front)
        {
            float amp = p["tAmp"], phase = p["tPhase"], spread = p["spread"], trail = p["trail"], droop = p["droop"];
            for (int i = 0; i < TentacleAnchors.Length; i++)
            {
                bool isFront = i == 2 || i == 4 || i == 5;
                if (isFront != front) continue;
                int g = c.NewGroup();
                Vector2 a = fr.P(TentacleAnchors[i].x, TentacleAnchors[i].y);
                float baseAngle = -90f + (TentacleBase[i] + 90f) * spread + fr.Tilt * 0.6f;
                float len = TentacleLength[i] * (1f + 0.12f * droop + 0.15f * trail);
                const int segments = 9;
                var pts = new List<Vector2> { a };
                for (int k = 1; k <= segments; k++)
                {
                    float t = k / (float)segments;
                    float ang = baseAngle + amp * Mathf.Sin(phase - t * 3.2f + i * 1.3f) * t;
                    ang = Mathf.Lerp(ang, -90f, droop * t * 0.7f);
                    ang = Mathf.Lerp(ang, 176f + (i - 2.5f) * 13f, trail * Mathf.Pow(t, 0.5f));
                    a = Add(a, Dir(ang, len / segments));
                    pts.Add(a);
                }
                float r0 = (i == 2 || i == 3) ? 2.6f : 2.1f;
                c.Strand(pts, r0, 0.5f, Flesh, isFront ? 2.7f : 0.6f, isFront ? 0f : -0.13f, g);
                if (isFront)
                {
                    // Ventosas: puntitos claros en la cara interior.
                    for (int k = 2; k < segments - 1; k += 2)
                    {
                        Vector2 d = Sub(pts[k + 1], pts[k]);
                        Vector2 side = new Vector2(d.y, -d.x).normalized;
                        float r = Mathf.Lerp(r0, 0.5f, k / (float)segments) * 0.6f;
                        c.Decal(pts[k].x + side.x * r, pts[k].y + side.y * r, Sucker);
                    }
                }
            }
        }

        // Estelas de velocidad detrás del picado.
        static void DrawStreaks(ShadedCanvas c, Frame fr, int f)
        {
            for (int i = 0; i < 4; i++)
            {
                float y = fr.C.y + (i - 1.5f) * 7f + ((f + i) % 2) * 2f;
                float x1 = fr.C.x - 14f - ((i * 5 + f * 3) % 6);
                float x0 = x1 - 12f - (i % 2) * 6f;
                float yy = y;
                c.Custom(x0, yy - 1f, x1, yy + 1f, (px, py) => (Mathf.Abs(py - yy) < 0.5f && px >= x0 && px <= x1, N3.Front), Streak, -1f);
            }
        }

        // Reventón: fogonazo, gotas de icor que salen despedidas y restos que caen.
        ShadedCanvas DrawBurst(int k)
        {
            var c = NewFrame();
            Vector2 center = V(0.5f, 6f);
            float t = k / 4f;

            if (k == 0)
            {
                c.Ellipse(center, 20f, 19f, 0f, FlashOut, -1f);
                c.Ellipse(center, 14f, 13f, 0f, FlashMid, -0.5f);
                c.Ellipse(center, 8f, 7.5f, 0f, FlashCore, 4f);
            }
            else if (k == 1)
            {
                c.Ellipse(center, 16f, 15f, 0f, FlashOut, -1f);
                c.Ellipse(center, 6f, 5.5f, 0f, FlashMid, -0.5f);
            }

            // Gotas en abanico (deterministas).
            for (int i = 0; i < 16; i++)
            {
                float ang = i * 22.5f + PixelCanvas.Hash(i, 3, 9) * 14f;
                float speed = 13f + PixelCanvas.Hash(i, 5, 9) * 14f;
                float dist = speed * (0.35f + t * 1.1f);
                Vector2 pos = Add(center, Dir(ang, dist));
                pos.y -= 22f * t * t;
                float size = Mathf.Lerp(2.4f, 0.9f, t) * (0.7f + 0.6f * PixelCanvas.Hash(i, 7, 9));
                if (size < 0.7f) continue;
                c.Ellipse(pos, size, size * 0.85f, 0f, i % 3 == 0 ? IchorGlow : Ichor, 3f, 0f, c.NewGroup());
            }

            // Pedazos de esclerótica y el iris apagándose, cayendo.
            for (int i = 0; i < 5; i++)
            {
                float ang = 40f + i * 70f;
                Vector2 pos = Add(center, Dir(ang, 6f + t * 16f));
                pos.y -= 26f * t * t;
                float s = 3.2f - t * 1.2f;
                var shard = new[] { Add(pos, Dir(ang + i * 37f, s)), Add(pos, Dir(ang + 130f + i * 37f, s * 0.8f)), Add(pos, Dir(ang + 240f + i * 37f, s * 0.9f)) };
                c.Poly(shard, i == 2 ? Flesh : Sclera, 2f, 1f, 0f, c.NewGroup());
            }
            if (k < 3)
            {
                Vector2 iris = Add(center, V(4f + t * 6f, -t * 18f - 2f));
                c.Ellipse(iris, 3.2f - k * 0.6f, 4f - k * 0.7f, 20f * k, k == 0 ? IrisInner : IrisMid, 3.2f, 0f, c.NewGroup());
            }

            // Tentáculos y alas sueltos cayendo.
            for (int i = 0; i < 3; i++)
            {
                Vector2 a = Add(center, V(-8f + i * 7f, -8f - 30f * t * t - 4f * t));
                var pts = new List<Vector2> { a };
                float ang = -100f + i * 25f + 50f * t;
                for (int s = 0; s < 6; s++)
                {
                    ang += 14f * Mathf.Sin(k * 1.7f + s + i);
                    a = Add(a, Dir(ang, (TentacleLength[i * 2] / 6f) * (1f - 0.25f * t)));
                    pts.Add(a);
                }
                c.Strand(pts, 1.9f, 0.5f, Flesh, 1f, -0.05f);
            }
            {
                Vector2 wingRoot = Add(center, V(-9f - 6f * t, 10f - 30f * t * t));
                var wing = new[] { wingRoot, Add(wingRoot, Dir(120f + 60f * t, 12f)), Add(wingRoot, Dir(150f + 60f * t, 9f)), Add(wingRoot, Dir(185f + 60f * t, 11f)) };
                c.Poly(wing, Membrane, 0.5f, 1.2f, -0.1f, c.NewGroup());
            }
            return c;
        }

        // ------------------------------------------------------------------
        // Datos fijos (venas y grietas en coordenadas del globo sin deformar)
        // ------------------------------------------------------------------

        static readonly Vector2[][] VeinPaths = BuildVeins();
        static readonly Vector2[][] Cracks =
        {
            new[] { V(-3f, 12f), V(-1f, 7f), V(-3f, 3f), V(0f, -1f), V(-1f, -5f) },
            new[] { V(-10f, 5f), V(-6f, 3f), V(-7f, -2f), V(-3f, -6f) },
            new[] { V(2f, -12f), V(3f, -8f), V(1f, -5f) },
        };

        static Vector2[][] BuildVeins()
        {
            // Salen del borde trasero (ángulos 110°–250°) y avanzan serpenteando hacia el iris sin llegar a tocarlo.
            float[] starts = { 118f, 150f, 184f, 214f, 240f };
            float[] reach = { 0.6f, 0.72f, 0.78f, 0.58f, 0.66f };
            var iris = V(6.4f, 0.6f);
            var veins = new Vector2[starts.Length + 2][];
            for (int v = 0; v < starts.Length; v++)
            {
                Vector2 a = Dir(starts[v], R - 0.8f);
                var pts = new List<Vector2>();
                const int n = 8;
                for (int i = 0; i <= n; i++)
                {
                    float t = i / (float)n * reach[v];
                    Vector2 p = Mix(a, iris, t);
                    Vector2 d = Sub(iris, a).normalized;
                    Vector2 side = new Vector2(-d.y, d.x);
                    p = Add(p, Scale(side, 1.4f * Mathf.Sin(t * 13f + v * 2.1f)));
                    pts.Add(p);
                }
                veins[v] = pts.ToArray();
            }
            // Dos ramas cortas.
            veins[starts.Length] = new[] { veins[1][4], Add(veins[1][4], V(1.5f, 3f)), Add(veins[1][4], V(4f, 4f)) };
            veins[starts.Length + 1] = new[] { veins[3][4], Add(veins[3][4], V(2f, -2.5f)), Add(veins[3][4], V(4.5f, -3f)) };
            return veins;
        }
    }
}
