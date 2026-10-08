using System.Collections.Generic;
using UnityEngine;
using static Abismo.EditorTools.Rig;

namespace Abismo.EditorTools
{
    /// <summary>
    /// El Profundo: hombre-pez de Innsmouth. Encorvado, cabeza de pez con ojos saltones amarillo enfermizo,
    /// branquias, aleta dorsal de espinas membranosas, brazos larguísimos con garras palmeadas y patas de rana.
    /// Aún viste los restos de su camisa de pescador y los pantalones rotos: fue humano.
    /// Mira a la derecha; el juego lo voltea al girar.
    ///
    /// Convenciones de pose:
    ///  - brazos: 0 = colgando, 90 = hacia delante, 180 = arriba, -90 = hacia atrás (Rig.Limb)
    ///  - piernas por IK: posición del tobillo (fFx/fFy, fBx/fBy) y ángulo del pie (fFa/fBa, matemático)
    ///  - lean: inclinación del tronco hacia delante (grados desde la vertical)
    ///  - jaw 0..1 boca abierta · gill 0..1 branquias abiertas · fin -1..1 aleta plegada/erizada
    ///  - eye: -1 apagado/aturdido, 0 normal (tenue), 1 telegrafiando (brilla y crece)
    /// </summary>
    public sealed class ProfundoArt : CharacterArt
    {
        public override string Id => "profundo";
        public override int FrameWidth => 144;
        public override int FrameHeight => 104;
        public override Vector2 Pivot => new Vector2(72f, 8f);

        // ------------------------------------------------------------------
        // Materiales
        // ------------------------------------------------------------------

        static readonly Color32[] SkinRamp = Ramp.Make("46625a", 5, 0.1f, 0.3f, 1.5f);
        static readonly Color32[] BellyRamp = Ramp.Make("a19c78", 5, 0.1f, 0.34f, 1.35f);
        static readonly PixelMaterial SkinDark = new PixelMaterial(Ramp.Make("2f3d3f", 4, 0.08f, 0.35f, 1.5f)) { Ambient = 0.26f, Rim = 0.5f };
        static readonly PixelMaterial FinMat = new PixelMaterial(Ramp.Make("4b3d4d", 4, 0.1f, 0.38f, 1.6f)) { Ambient = 0.3f, Rim = 0.8f, Dither = 0f };
        static readonly PixelMaterial SpineMat = new PixelMaterial(Ramp.Make("3b3139", 4, 0.06f, 0.4f, 1.6f)) { Ambient = 0.3f, Rim = 0.6f, Dither = 0f };
        static readonly PixelMaterial Shirt = new PixelMaterial(Ramp.Make("5a5343", 5, 0.1f, 0.33f, 1.4f)) { Ambient = 0.24f, Rim = 0.5f, Texture = ClothNoise };
        static readonly PixelMaterial Trousers = new PixelMaterial(Ramp.Make("323848", 4, 0.08f, 0.35f, 1.5f)) { Ambient = 0.22f, Rim = 0.45f, Texture = ClothNoise };
        static readonly PixelMaterial Rope = new PixelMaterial(Ramp.Make("7d6c4a", 4, 0.08f)) { Ambient = 0.3f };
        static readonly PixelMaterial ClawMat = new PixelMaterial(Ramp.Make("b3a885", 4, 0.1f, 0.4f, 1.3f)) { Ambient = 0.35f, Gloss = 0.3f, Dither = 0f };
        static readonly PixelMaterial GillMat = new PixelMaterial(Ramp.Make("5a2430", 4, 0.06f, 0.45f, 1.3f)) { Ambient = 0.3f, Rim = 0.1f, Dither = 0f, Outline = false };
        static readonly PixelMaterial MouthIn = new PixelMaterial(Ramp.Make("3a1a26", 3, 0.05f, 0.5f, 1.3f)) { Ambient = 0.5f, Outline = false };
        static readonly PixelMaterial Puddle = new PixelMaterial(Ramp.Make("1f2e2e", 4, 0.06f, 0.5f, 1.9f)) { Ambient = 0.2f, Gloss = 0.9f, Rim = 0.3f, Dither = 0f };
        static readonly PixelMaterial Water = new PixelMaterial(Ramp.Make("5f8a86", 3, 0.06f, 0.6f, 1.5f)) { Ambient = 0.4f, Outline = false, ReceivesContactShadow = false };

        static readonly PixelMaterial EyeRing = PixelMaterial.Glow("5e5a1c");
        static readonly PixelMaterial EyeMid = PixelMaterial.Glow("a69e38");
        static readonly PixelMaterial EyeCore = PixelMaterial.Glow("ddd578");
        static readonly PixelMaterial EyeRingHot = PixelMaterial.Glow("948824");
        static readonly PixelMaterial EyeMidHot = PixelMaterial.Glow("e4d846");
        static readonly PixelMaterial EyeCoreHot = PixelMaterial.Glow("fffbcc");
        static readonly PixelMaterial EyeDull = PixelMaterial.Glow("3e3c26");
        static readonly PixelMaterial EyeDullMid = PixelMaterial.Glow("5e5a36");
        static readonly PixelMaterial Pupil = PixelMaterial.Glow("120f0c");

        static readonly PixelMaterial SmearCore = new PixelMaterial(new[] { new Color32(246, 242, 206, 245) }) { Emissive = true, Outline = false, ReceivesContactShadow = false };
        static readonly PixelMaterial SmearMid = new PixelMaterial(new[] { new Color32(196, 196, 150, 200) }) { Emissive = true, Outline = false, ReceivesContactShadow = false };
        static readonly PixelMaterial SmearSoft = new PixelMaterial(new[] { new Color32(110, 128, 104, 140) }) { Emissive = true, Outline = false, ReceivesContactShadow = false };

        static float ClothNoise(int x, int y) => (PixelCanvas.Hash(x / 2, y / 3, 41) - 0.5f) * 0.08f;

        /// <summary>Piel con manchas en grupos (no píxeles sueltos) que se mueven con el cuerpo.</summary>
        static PixelMaterial SkinFor(int ox, int oy) => new PixelMaterial(SkinRamp)
        {
            Ambient = 0.24f, Rim = 0.62f, Dither = 0.04f,
            Texture = (x, y) =>
            {
                float n = PixelCanvas.ValueNoise((x - ox) / 2.6f, (y - oy) / 2.6f, 0, 913);
                if (n > 0.74f) return -0.2f;
                float m = PixelCanvas.ValueNoise((x - ox) / 1.8f, (y - oy) / 1.8f, 0, 377);
                return m > 0.82f ? 0.1f : 0f;
            },
        };

        static PixelMaterial BellyFor(int ox, int oy) => new PixelMaterial(BellyRamp)
        {
            Ambient = 0.3f, Rim = 0.4f, Dither = 0.04f,
            Texture = (x, y) => PixelCanvas.ValueNoise((x - ox) / 2.2f, (y - oy) / 2.2f, 0, 51) > 0.76f ? -0.16f : 0f,
        };

        // ------------------------------------------------------------------
        // Poses base
        // ------------------------------------------------------------------

        const float Ground = 1.24f;      // altura de la bola del pie apoyado
        const float FootLen = 5.2f;

        static Pose Idle => new Pose().With(
            ("hipX", -3f), ("hipY", 21f), ("lean", 32f), ("neck", 0f), ("head", 0f),
            ("fFx", 5f), ("fFy", 5.5f), ("fFa", -55f),
            ("fBx", -10f), ("fBy", 5.5f), ("fBa", -55f),
            ("armF1", 12f), ("armF2", 58f), ("wristF", -8f), ("gripF", 0.3f), ("palmF", 1f),
            ("armB1", -4f), ("armB2", 40f), ("wristB", -6f), ("gripB", 0.4f), ("palmB", 1f),
            ("jaw", 0.08f), ("gill", 0.3f), ("fin", 0f), ("finWave", 0f), ("shirt", 0f), ("eye", 0f));

        public override List<AnimSpec> Animations() => new List<AnimSpec>
        {
            new AnimSpec("idle", 6, 8f, true, f => DrawIdle(f)),
            new AnimSpec("walk", 8, 10f, true, f => DrawWalk(f)),
            new AnimSpec("run", 8, 14f, true, f => DrawRun(f)),
            new AnimSpec("windup", 6, 11f, false, f => DrawWindup(f)),
            new AnimSpec("attack", 4, 20f, false, f => DrawAttack(f)),
            new AnimSpec("recover", 6, 8f, false, f => DrawRecover(f)),
            new AnimSpec("hurt", 3, 12f, false, f => DrawHurt(f)),
            new AnimSpec("stagger", 6, 8f, true, f => DrawStagger(f)),
            new AnimSpec("death", 10, 14f, false, f => DrawDeath(f)),
        };

        // ------------------------------------------------------------------
        // Animaciones
        // ------------------------------------------------------------------

        ShadedCanvas DrawIdle(int f)
        {
            float t = f / 6f * Mathf.PI * 2f;
            var p = Idle.With(
                ("hipY", 21f - 0.5f + Mathf.Cos(t) * 0.5f), ("lean", 32f + Mathf.Sin(t) * 1.5f),
                ("head", Mathf.Sin(t - 0.8f) * 2.5f), ("jaw", 0.1f + 0.1f * Mathf.Sin(t + 0.5f)),
                ("gill", 0.35f + 0.35f * Mathf.Sin(t + 1.2f)), ("finWave", t), ("fin", 0.1f * Mathf.Sin(t)),
                ("armF1", 12f + Mathf.Sin(t - 1f) * 3f), ("armF2", 58f + Mathf.Sin(t - 1.6f) * 4f),
                ("armB1", -4f + Mathf.Sin(t - 1.4f) * 2.5f), ("armB2", 40f + Mathf.Sin(t - 2f) * 3.5f),
                ("gripF", 0.3f + 0.15f * Mathf.Sin(t)), ("shirt", Mathf.Sin(t - 1f) * 0.4f));
            return Draw(p);
        }

        /// <summary>Ciclo de una pierna: apoyo (el pie retrocede pegado al suelo) y vuelo (avanza levantándose).</summary>
        static void Step(Pose p, string leg, float phase, float center, float stride, float lift, float stance = 0.55f)
        {
            float u = phase - Mathf.Floor(phase);
            float x, y, a;
            if (u < stance)
            {
                float w = u / stance;
                x = center + stride * (1f - 2f * w);
                a = w > 0.7f ? -55f - 27f * (w - 0.7f) / 0.3f : -55f;
                y = Ground + FootLen * Mathf.Sin(-a * Mathf.Deg2Rad);
            }
            else
            {
                float w = (u - stance) / (1f - stance);
                float s = w * w * (3f - 2f * w);
                x = center - stride + 2f * stride * s;
                a = -82f + 27f * w;
                y = Ground + FootLen * Mathf.Sin(-a * Mathf.Deg2Rad) + Mathf.Sin(w * Mathf.PI) * lift;
            }
            p["f" + leg + "x"] = x;
            p["f" + leg + "y"] = y;
            p["f" + leg + "a"] = a;
        }

        ShadedCanvas DrawWalk(int f)
        {
            float ph = f / 8f;
            float t = ph * Mathf.PI * 2f;
            var p = Idle.With(("lean", 35f), ("hipX", -2f));
            Step(p, "F", ph, 2f, 7f, 4f);
            Step(p, "B", ph + 0.5f, -5f, 7f, 4f);
            p["hipY"] = 19.6f + Mathf.Cos(t * 2f) * 0.8f;
            p["lean"] = 35f + Mathf.Cos(t * 2f + 0.5f) * 1.2f;
            p["head"] = Mathf.Sin(t * 2f - 0.6f) * 2.5f;
            p["armF1"] = 12f - 16f * Mathf.Sin(t - 0.4f);
            p["armF2"] = 30f - 12f * Mathf.Sin(t - 1.1f);
            p["armB1"] = 6f + 14f * Mathf.Sin(t - 0.4f);
            p["armB2"] = 26f + 12f * Mathf.Sin(t - 1.1f);
            p["finWave"] = t * 2f;
            p["shirt"] = Mathf.Sin(t * 2f - 1f) * 0.5f;
            p["gill"] = 0.4f + 0.3f * Mathf.Sin(t * 2f);
            p["jaw"] = 0.15f;
            return Draw(p);
        }

        ShadedCanvas DrawRun(int f)
        {
            float ph = f / 8f;
            float t = ph * Mathf.PI * 2f;
            var p = Idle.With(("lean", 52f), ("hipX", 0f), ("neck", 14f), ("head", 12f), ("jaw", 0.45f), ("fin", -0.6f), ("gill", 0.8f));
            Step(p, "F", ph, 3f, 11f, 7f, 0.5f);
            Step(p, "B", ph + 0.5f, -3f, 11f, 7f, 0.5f);
            p["hipY"] = 18.6f + Mathf.Abs(Mathf.Sin(t)) * 2f;
            p["lean"] = 52f + Mathf.Cos(t * 2f) * 2f;
            p["armF1"] = -10f - 42f * Mathf.Sin(t - 0.3f);
            p["armF2"] = 10f - 34f * Mathf.Sin(t - 0.9f);
            p["armB1"] = -10f + 42f * Mathf.Sin(t - 0.3f);
            p["armB2"] = 10f + 34f * Mathf.Sin(t - 0.9f);
            p["gripF"] = 0.6f;
            p["gripB"] = 0.6f;
            p["finWave"] = t * 2f;
            p["shirt"] = 1f + Mathf.Sin(t * 2f) * 0.4f;
            return Draw(p);
        }

        static Pose Gather => Idle.With(("lean", 40f), ("hipY", 18.4f), ("hipX", -2f), ("head", -6f), ("neck", -4f),
                                         ("armF1", -24f), ("armF2", 4f), ("gripF", 0.6f), ("armB1", 14f), ("armB2", 44f),
                                         ("jaw", 0f), ("fin", -0.3f), ("eye", 0.25f), ("gill", 0.1f), ("shirt", 0.3f));

        static Pose Rear => Idle.With(("lean", 14f), ("hipX", -5.5f), ("hipY", 21.4f), ("neck", 6f), ("head", 8f),
                                       ("fFx", 6f), ("fBx", -11f),
                                       ("armF1", -192f), ("armF2", -146f), ("wristF", -18f), ("gripF", 0.15f), ("palmF", -1f),
                                       ("armB1", 34f), ("armB2", 70f), ("wristB", -10f), ("gripB", 0.5f),
                                       ("jaw", 0.95f), ("fin", 1f), ("gill", 1f), ("eye", 1f), ("shirt", -0.6f));

        // Telegrafiado: se encoge, se echa atrás alzando la garra, abre las fauces y los ojos se encienden.
        ShadedCanvas DrawWindup(int f)
        {
            var keys = new Keyframes()
                .Key(0, Pose.Lerp(Idle, Gather, 0.55f), Ease.Out)
                .Key(1, Gather, Ease.InOut)
                .Key(3, Pose.Lerp(Gather, Rear, 0.85f), Ease.Out)
                .Key(4, Rear)
                .Key(5, Rear.With(("lean", 11f), ("armF1", -196f), ("armF2", -150f), ("head", 10f), ("hipY", 21.8f), ("gripF", 0f)));
            var p = keys.Evaluate(f);
            p["finWave"] = f * 1.4f;
            return Draw(p);
        }

        static Pose Swipe => Idle.With(("lean", 46f), ("hipX", 1f), ("hipY", 18.8f), ("fFx", 11f), ("fBx", -9f),
                                        ("neck", -2f), ("head", -2f),
                                        ("armF1", 116f), ("armF2", 112f), ("wristF", -6f), ("gripF", 0f), ("palmF", -1f),
                                        ("armB1", -40f), ("armB2", -16f), ("jaw", 0.75f), ("fin", 0.8f), ("gill", 0.9f), ("eye", 0.6f), ("shirt", 0.9f));

        static Pose Strike => Swipe.With(("lean", 56f), ("hipX", 4f), ("hipY", 17.8f), ("armF1", 62f), ("armF2", 46f), ("wristF", 4f),
                                          ("neck", -6f), ("head", -6f), ("armB1", -54f), ("armB2", -30f), ("shirt", 1.2f), ("eye", 0.4f));

        static Pose Follow => Strike.With(("lean", 62f), ("hipX", 4.5f), ("hipY", 16.6f), ("armF1", -42f), ("armF2", -24f), ("wristF", 10f),
                                           ("gripF", 0.7f), ("palmF", 0.4f), ("neck", -4f), ("head", -2f), ("jaw", 0.5f), ("fin", 0.4f),
                                           ("armB1", -30f), ("armB2", -6f), ("shirt", 0.6f), ("eye", 0.15f));

        // Zarpazo: dos fotogramas con estela de tres líneas (las garras) y continuación.
        ShadedCanvas DrawAttack(int f)
        {
            Pose p;
            var smear = default(SmearSpec);
            switch (f)
            {
                case 0:
                    p = Swipe;
                    smear = new SmearSpec { On = true, From = 118f, To = 28f, Strength = 1f };
                    break;
                case 1:
                    p = Strike;
                    smear = new SmearSpec { On = true, From = 40f, To = -32f, Strength = 0.8f };
                    break;
                case 2:
                    p = Pose.Lerp(Strike, Follow, 0.75f);
                    break;
                default:
                    p = Follow;
                    break;
            }
            p["finWave"] = f * 1.3f;
            return Draw(p, smear);
        }

        ShadedCanvas DrawRecover(int f)
        {
            var pant = Follow.With(("lean", 50f), ("hipY", 17.6f), ("jaw", 0.6f), ("gill", 1f), ("fin", 0f), ("eye", 0f),
                                   ("armF1", 6f), ("armF2", 14f), ("palmF", 1f), ("head", 4f));
            var stepBack = Pose.Lerp(pant, Idle, 0.6f).With(("fFy", 8.5f), ("fFa", -70f));
            var keys = new Keyframes()
                .Key(0, Follow, Ease.Out)
                .Key(2, pant)
                .Key(3.5f, stepBack)
                .Key(5, Idle);
            var p = keys.Evaluate(f);
            p["finWave"] = f * 1.2f;
            return Draw(p);
        }

        static Pose Hit => Idle.With(("lean", 12f), ("hipX", -6f), ("hipY", 20.6f), ("neck", 16f), ("head", 22f),
                                      ("armF1", 52f), ("armF2", 96f), ("gripF", 0f), ("palmF", -0.5f), ("armB1", -54f), ("armB2", -24f),
                                      ("jaw", 0.9f), ("fin", -0.7f), ("gill", 1f), ("shirt", 1f), ("eye", -0.2f));

        ShadedCanvas DrawHurt(int f)
        {
            var keys = new Keyframes().Key(0, Hit, Ease.Out).Key(1, Pose.Lerp(Hit, Idle, 0.35f)).Key(2, Pose.Lerp(Hit, Idle, 0.7f));
            var p = keys.Evaluate(f);
            p["finWave"] = f * 2f;
            return Draw(p);
        }

        ShadedCanvas DrawStagger(int f)
        {
            float t = f / 6f * Mathf.PI * 2f;
            var p = Idle.With(
                ("lean", 24f + 7f * Mathf.Sin(t)), ("hipX", -4.5f + 1.2f * Mathf.Sin(t)), ("hipY", 19.6f + 0.5f * Mathf.Cos(t * 2f)),
                ("neck", 8f + 6f * Mathf.Sin(t + 1f)), ("head", 4f + 12f * Mathf.Sin(t + 1.7f)),
                ("armF1", 4f + 9f * Mathf.Sin(t + 2f)), ("armF2", 8f + 10f * Mathf.Sin(t + 2.6f)), ("gripF", 0.9f),
                ("armB1", -4f + 7f * Mathf.Sin(t + 2.3f)), ("armB2", 6f + 9f * Mathf.Sin(t + 2.9f)), ("gripB", 0.9f),
                ("jaw", 0.55f + 0.25f * Mathf.Sin(t * 2f)), ("gill", 0.7f + 0.3f * Mathf.Sin(t * 2f + 1f)),
                ("fin", -0.5f), ("finWave", t), ("shirt", Mathf.Sin(t + 1f) * 0.6f),
                ("eye", f == 2 || f == 3 ? -1f : -0.6f));
            return Draw(p);
        }

        ShadedCanvas DrawDeath(int f)
        {
            var hit = Hit.With(("lean", 6f), ("head", 28f), ("jaw", 1f), ("hipX", -7f));
            var reel = hit.With(("lean", 2f), ("hipX", -8f), ("hipY", 20f), ("armF1", 110f), ("armF2", 150f), ("armB1", -90f), ("armB2", -60f), ("head", 20f), ("eye", -0.5f));
            var buckle = Idle.With(("lean", 30f), ("hipX", -5f), ("hipY", 12f), ("neck", 6f), ("head", -14f), ("jaw", 0.7f),
                                   ("armF1", 30f), ("armF2", 40f), ("gripF", 1f), ("armB1", 20f), ("armB2", 30f), ("gripB", 1f),
                                   ("fin", -0.6f), ("eye", -0.8f), ("shirt", 0.2f));
            var topple = buckle.With(("lean", 70f), ("hipX", -4f), ("hipY", 9.5f), ("neck", 18f), ("head", -6f), ("armF1", 80f), ("armF2", 90f),
                                     ("armB1", 70f), ("armB2", 80f), ("fFx", -6f), ("fFy", 4f), ("fBx", -14f), ("fBy", 4f), ("shirt", -0.6f));
            var prone = topple.With(("lean", 92f), ("hipX", -6f), ("hipY", 7.6f), ("neck", 26f), ("head", 4f), ("jaw", 0.5f),
                                    ("armF1", 98f), ("armF2", 92f), ("armB1", 92f), ("armB2", 84f),
                                    ("fFx", -22f), ("fFy", 3f), ("fFa", 175f), ("fBx", -25f), ("fBy", 3.2f), ("fBa", 175f),
                                    ("eye", -1f), ("fin", -0.2f), ("shirt", 0f));
            var keys = new Keyframes()
                .Key(0, hit, Ease.Out)
                .Key(1, reel)
                .Key(3, buckle, Ease.In)
                .Key(4, topple, Ease.In)
                .Key(5, prone)
                .Key(6, prone.With(("hipY", 8.6f), ("lean", 89f), ("jaw", 0.7f)), Ease.In)
                .Key(7, prone.With(("slump", 0.3f), ("puddle", 0.3f)), Ease.Out)
                .Key(9, prone.With(("slump", 0.85f), ("puddle", 1f), ("hipY", 7f), ("jaw", 0.35f)));
            var p = keys.Evaluate(f);
            p["finWave"] = f * 1.1f;
            p["splash"] = f == 5 ? 1f : f == 6 ? 2f : 0f;
            return Draw(p);
        }

        // ------------------------------------------------------------------
        // Dibujo del personaje a partir de una pose
        // ------------------------------------------------------------------

        struct SmearSpec
        {
            public bool On;
            public float From, To, Strength;
        }

        const float Thigh = 11.5f, Shin = 11.5f, UpperArm = 12.5f, Forearm = 12.5f, HeadScale = 1.14f;

        ShadedCanvas Draw(Pose p, SmearSpec smear = default)
        {
            var c = NewFrame();
            float lean = p["lean"];
            float ta = 90f - lean;
            float slump = p["slump"];
            var hip = V(p["hipX"], p["hipY"]);
            var U = Dir(ta);
            var B = Dir(ta + 90f);
            float squash = 1f - 0.22f * slump;
            Vector2 T(float u, float v) => Add(hip, Add(Scale(U, u), Scale(B, v * squash)));

            var Skin = SkinFor(Mathf.RoundToInt(hip.x), Mathf.RoundToInt(hip.y));
            var Belly = BellyFor(Mathf.RoundToInt(hip.x), Mathf.RoundToInt(hip.y));

            int gTorso = c.NewGroup(), gNeck = c.NewGroup(), gLegF = c.NewGroup(), gLegB = c.NewGroup();
            int gArmF = c.NewGroup(), gArmB = c.NewGroup(), gFin = c.NewGroup(), gShirt = c.NewGroup(), gBelt = c.NewGroup();

            // --- Charco (muerte) ---
            float pud = p["puddle"];
            if (pud > 0.01f)
            {
                var pc = V(hip.x + 12f, 0.6f);
                c.Ellipse(pc, 8f + 28f * pud, 1.6f + 1.6f * pud, 0f, Puddle, -2f, 0f, 0, 0.5f);
            }

            // --- Piernas (IK con rodillas hacia delante, como una rana) ---
            var ankleF = V(p["fFx"], p["fFy"]);
            var ankleB = V(p["fBx"], p["fBy"]);
            var kneeF = Knee(hip, ref ankleF, Thigh, Shin, 1f - 2f * p["flipF"]);
            var kneeB = Knee(hip, ref ankleB, Thigh, Shin, 1f - 2f * p["flipB"]);

            // --- Hombros, brazos ---
            var shoulderF = T(16.5f, -3f);
            var shoulderB = T(17.5f, 1.5f);
            float armF2 = p["armF2"], armB2 = p["armB2"];
            var elbowF = Limb(shoulderF, p["armF1"], UpperArm);
            var handF = Limb(elbowF, KeepClawUp(elbowF, ref armF2), Forearm);
            var elbowB = Limb(shoulderB, p["armB1"], UpperArm);
            var handB = Limb(elbowB, KeepClawUp(elbowB, ref armB2), Forearm);

            // --- Cuello y cabeza ---
            float neckA = ta - 34f + p["neck"];
            var neckBase = T(19.5f, -1.5f);
            var hp = Add(neckBase, Dir(neckA, 6f));
            float hr = -(lean - 32f) * 0.35f + p["head"] + p["neck"] * 0.5f;

            // Brazo y pierna traseros (lejos de la luz)
            DrawLeg(c, hip, kneeB, ankleB, p["fBa"], 1.5f, -0.13f, gLegB, Skin, false);
            DrawArm(c, shoulderB, elbowB, handB, armB2, p["wristB"], p["gripB"], p["palmB"], 1f, -0.13f, gArmB, Skin, false, 0f);

            // --- Aleta dorsal (detrás del tronco) ---
            Vector2 Hd(float x, float y) => Add(hp, Rotate(V(x * HeadScale, y * HeadScale), V(0f, 0f), hr));
            DrawFin(c, new[] { Hd(-2f, 6.4f), T(21.5f, 6.8f), T(18f, 10.4f), T(14f, 11.8f), T(10f, 11.4f), T(6.5f, 9.6f) },
                    B, ta, p["fin"], p["finWave"], gFin);

            // --- Tronco: barriga, joroba, pecho ---
            c.Capsule(T(0f, 0.5f), T(10f, 0f), 7.6f, 8.2f, Skin, 4f, 0f, gTorso);
            c.Ellipse(T(13f, 3f), 10.2f, 8.8f * squash, ta, Skin, 4f, 0f, gTorso);
            c.Ellipse(T(17f, -1.5f), 6.8f, 7f * squash, ta, Skin, 4f, 0f, gTorso);
            // Barriga pálida que asoma por la camisa rota
            c.Ellipse(T(5.5f, -4.8f), 7.2f, 5.4f * squash, ta, Belly, 4.2f, 0f, gTorso);

            // Pantalón (cadera) y cinturón de cuerda
            c.Ellipse(T(-1.8f, 0f), 4.6f, 8.6f * squash, ta, Trousers, 4.3f, 0f, gBelt);
            c.Capsule(T(2.6f, -8.8f), T(2.6f, 6f), 1.1f, 1.1f, Rope, 4.4f, 0f, gBelt);
            c.Capsule(T(2.4f, -9.1f), Add(T(2.4f, -9.1f), V(1.2f + p["shirt"] * 0.8f, -4.4f)), 0.8f, 0.6f, Rope, 4.45f, 0f, gBelt);

            // Camisa de pescador hecha jirones: cubre pecho y hombro; la espalda está reventada por la joroba y la aleta.
            float sway = p["shirt"];
            var shirt = new List<Vector2> { T(21.4f, -2.8f), T(19f, -7.6f), T(14.5f, -9.4f), T(11f, -9.1f) };
            shirt.AddRange(Jagged(T(11f, -9.1f), T(6f, 3.5f), 4, 3.4f, 7, sway));
            shirt.Add(T(6f, 3.5f));
            shirt.AddRange(Jagged(T(6f, 3.5f), T(15.5f, 8.2f), 3, 1.6f, 12, 0f));
            shirt.AddRange(new[] { T(15.5f, 8.2f), T(19.5f, 7f), T(21.8f, 3.8f), T(22.4f, 0.6f) });
            c.Poly(shirt.ToArray(), Shirt, 4.5f, 2.2f, 0f, gShirt, 0.12f, 0f);
            // Tapeta con botones: lo poco que queda de "humano"
            c.Capsule(T(20.6f, -4.6f), T(12.5f, -7.6f), 0.7f, 0.7f, Shirt, 4.55f, -0.22f, gShirt);
            var button = PixelCanvas.Hex("b7ad8a");
            foreach (var u in new[] { 18.4f, 15.2f })
            {
                var bp = T(u, -5.4f - (20.6f - u) * 0.37f);
                c.Decal(bp.x, bp.y, button);
            }

            // Cuello (sale de la camisa) y garganta pálida
            c.Capsule(T(19f, -0.5f), hp, 5.8f, 5.2f, Skin, 5f, 0f, gNeck);
            c.Capsule(T(18.8f, -4.4f), Hd(1.6f, -4.6f), 3f, 2.7f, Belly, 5.1f, 0f, gNeck);
            // Cuello de la camisa
            c.Poly(new[] { T(22f, -2.4f), T(19.6f, -8f), T(17.8f, -6.4f), T(19.2f, -1.4f) }, Shirt, 5.2f, 1.2f, 0f, gShirt);

            // Pierna delantera
            DrawLeg(c, hip, kneeF, ankleF, p["fFa"], 6f, 0f, gLegF, Skin, true);

            // Cabeza
            DrawHead(c, hp, hr, p, Skin, Belly);

            // Estela de las garras
            if (smear.On)
            {
                float reach = Mathf.Sqrt((handF.x - shoulderF.x) * (handF.x - shoulderF.x) + (handF.y - shoulderF.y) * (handF.y - shoulderF.y)) + 9f;
                DrawClawSmear(c, shoulderF, reach, smear);
            }

            // Brazo delantero con jirón de manga
            DrawArm(c, shoulderF, elbowF, handF, armF2, p["wristF"], p["gripF"], p["palmF"], 9.5f, 0f, gArmF, Skin, false, sway);

            // Salpicadura al caer
            float splash = p["splash"];
            if (splash > 0.5f)
            {
                float k = splash > 1.5f ? 1f : 0f;
                var at = V(hip.x + 12f, 1f);
                for (int i = 0; i < 7; i++)
                {
                    float ang = 20f + i * 23f + PixelCanvas.Hash(i, 3, 9) * 10f;
                    float dist = 7f + i % 3 * 3f + k * 6f;
                    var d = Add(at, Dir(ang, dist));
                    d = Add(d, V(0f, -k * (3f + i % 2 * 2f)));
                    float r = (1.4f - k * 0.5f) * (0.7f + PixelCanvas.Hash(i, 5, 2) * 0.5f);
                    c.Ellipse(d, r, r * 1.2f, 0f, Water, 12f);
                }
            }

            return c;
        }

        /// <summary>Si la garra se hundiría en el suelo, dobla el antebrazo hacia la horizontal más cercana.</summary>
        static float KeepClawUp(Vector2 elbow, ref float fore)
        {
            const float minY = 8.5f; // la mano necesita ~8 px para los dedos y las uñas
            for (int i = 0; i < 40; i++)
            {
                if (Limb(elbow, fore, Forearm).y >= minY) break;
                fore += fore >= 0f ? 3f : -3f;
            }
            return fore;
        }

        static Vector2 Knee(Vector2 hip, ref Vector2 ankle, float l1, float l2, float bend)
        {
            float dx = ankle.x - hip.x, dy = ankle.y - hip.y;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float max = l1 + l2 - 0.05f;
            if (d > max)
            {
                ankle = V(hip.x + dx / d * max, hip.y + dy / d * max);
                dx = ankle.x - hip.x;
                dy = ankle.y - hip.y;
                d = max;
            }
            d = Mathf.Max(d, 0.5f);
            float a = (l1 * l1 - l2 * l2 + d * d) / (2f * d);
            float h = Mathf.Sqrt(Mathf.Max(0f, l1 * l1 - a * a));
            float ux = dx / d, uy = dy / d;
            return V(hip.x + ux * a - uy * h * bend, hip.y + uy * a + ux * h * bend);
        }

        void DrawLeg(ShadedCanvas c, Vector2 hip, Vector2 knee, Vector2 ankle, float footA, float z, float shade, int group, PixelMaterial skin, bool front)
        {
            // Muslo de rana, espinilla fina y pie largo palmeado.
            c.Capsule(hip, knee, 5f, 3.5f, skin, z, shade, group);
            c.Capsule(knee, ankle, 3.2f, 1.9f, skin, z + 0.02f, shade, group);
            // Pantalón roto por encima de la rodilla
            var cut = Mix(hip, knee, 0.7f);
            c.Capsule(hip, cut, 5.4f, 4.5f, Trousers, z + 0.05f, shade, group);
            var axis = Sub(knee, hip);
            float len = Mathf.Max(0.01f, Mathf.Sqrt(axis.x * axis.x + axis.y * axis.y));
            axis = Scale(axis, 1f / len);
            var side = V(-axis.y, axis.x);
            var hem = new List<Vector2> { Add(cut, Scale(side, 4.6f)), Add(Mix(hip, knee, 0.45f), Scale(side, 4.9f)), Add(Mix(hip, knee, 0.45f), Scale(side, -4.9f)), Add(cut, Scale(side, -4.6f)) };
            for (int i = 1; i < 6; i++)
            {
                float t = i / 6f;
                var e = Add(cut, Scale(side, Mathf.Lerp(-4.6f, 4.6f, t)));
                float d = (i % 2 == 1 ? 2.8f : 0.6f) * (0.6f + 0.7f * PixelCanvas.Hash(i, front ? 3 : 4, 11));
                hem.Add(Add(e, Scale(axis, d)));
            }
            c.Poly(hem.ToArray(), Trousers, z + 0.06f, 1.4f, shade, group);

            // Pie: metatarso hasta la bola, dedos palmeados y uñas.
            var ball = Add(ankle, Dir(footA, FootLen));
            c.Capsule(ankle, ball, 2.2f, 1.5f, skin, z + 0.03f, shade, group);
            float toeA = footA + 52f;
            float[] spread = { -4f, 9f, 22f };
            float[] lenT = { 5.6f, 5f, 4f };
            var web = new List<Vector2> { ball };
            for (int i = 0; i < 3; i++)
            {
                float ta2 = toeA + spread[i];
                // Los dedos nunca atraviesan el suelo: se aplanan al apoyarse.
                float sinMin = (0.9f - ball.y) / lenT[i];
                if (Mathf.Sin(ta2 * Mathf.Deg2Rad) < sinMin) ta2 = Mathf.Asin(Mathf.Clamp(sinMin, -1f, 1f)) * Mathf.Rad2Deg;
                var dir = Dir(ta2);
                var tip = Add(ball, Scale(dir, lenT[i]));
                c.Capsule(ball, tip, 1.1f, 0.75f, skin, z + 0.04f + i * 0.001f, shade, group);
                c.Capsule(tip, Add(tip, Dir(ta2 - (tip.y < 2f ? 0f : 25f), 1.8f)), 0.75f, 0.3f, ClawMat, z + 0.045f, shade, group);
                web.Add(Add(ball, Scale(dir, lenT[i] * 0.85f)));
            }
            c.Poly(web.ToArray(), FinMat, z + 0.035f, 1f, shade, group);
        }

        void DrawArm(ShadedCanvas c, Vector2 shoulder, Vector2 elbow, Vector2 hand, float foreAngle, float wrist, float grip, float palm,
                     float z, float shade, int group, PixelMaterial skin, bool sleeve, float sway)
        {
            c.Capsule(shoulder, elbow, 3.7f, 2.7f, skin, z, shade, group);
            c.Capsule(elbow, hand, 2.8f, 2.2f, skin, z + 0.05f, shade, group);
            // Espinas membranosas en el antebrazo (lado de fuera)
            var fa = Sub(hand, elbow);
            float flen = Mathf.Max(0.01f, Mathf.Sqrt(fa.x * fa.x + fa.y * fa.y));
            fa = Scale(fa, 1f / flen);
            var outSide = V(fa.y, -fa.x); // lado "trasero" del antebrazo
            var f0 = Add(elbow, Scale(fa, 2f));
            var f1 = Add(elbow, Scale(fa, flen * 0.7f));
            var t0 = Add(Add(f0, Scale(outSide, 4.6f)), Scale(fa, -1.6f));
            var t1 = Add(Add(f1, Scale(outSide, 3.4f)), Scale(fa, -0.6f));
            c.Poly(new[] { f0, t0, Mix(Mix(f0, t0, 0.6f), Mix(f1, t1, 0.6f), 0.5f), t1, f1 }, FinMat, z - 0.02f, 1f, shade, group);
            c.Capsule(f0, t0, 0.7f, 0.3f, SpineMat, z - 0.01f, shade, group);
            c.Capsule(f1, t1, 0.6f, 0.3f, SpineMat, z - 0.01f, shade, group);
            if (sleeve)
            {
                // Jirón de manga
                var axis = Sub(elbow, shoulder);
                float len = Mathf.Max(0.01f, Mathf.Sqrt(axis.x * axis.x + axis.y * axis.y));
                axis = Scale(axis, 1f / len);
                var side = V(-axis.y, axis.x);
                var end = Add(shoulder, Scale(axis, len * 0.48f));
                var pts = new List<Vector2> { Add(shoulder, Add(Scale(side, 4.4f), Scale(axis, -2.8f))), Add(shoulder, Add(Scale(side, -4.4f), Scale(axis, -2.8f))) };
                for (int i = 0; i <= 4; i++)
                {
                    float t = i / 4f;
                    var e = Add(end, Scale(side, Mathf.Lerp(-4.2f, 4.2f, t)));
                    float d = (i % 2 == 0 ? 3f : 0.4f) * (0.7f + 0.6f * PixelCanvas.Hash(i, 8, 21));
                    pts.Add(Add(Add(e, Scale(axis, d)), V(-sway * 0.8f * (i % 2 == 0 ? 1f : 0f), 0f)));
                }
                c.Poly(pts.ToArray(), Shirt, z + 0.08f, 1.5f, shade, group);
            }
            DrawClaw(c, hand, foreAngle - 90f + wrist, grip, palm, z + 0.1f, shade, group, skin);
        }

        /// <summary>Mano palmeada: palma, tres dedos con membrana y garras curvas.</summary>
        void DrawClaw(ShadedCanvas c, Vector2 wristPt, float ang, float grip, float palm, float z, float shade, int group, PixelMaterial skin)
        {
            var palmC = Add(wristPt, Dir(ang, 2.2f));
            c.Ellipse(palmC, 3.3f, 2.6f, ang, skin, z, shade, group);
            float[] spread = { -28f, 0f, 26f };
            float[] len1 = { 4f, 5f, 4.2f };
            float curl = palm * (20f + 50f * grip);
            var web = new List<Vector2> { Add(palmC, Dir(ang - 90f, 2f)) };
            for (int i = 0; i < 3; i++)
            {
                float fa = ang + spread[i] * (1f - 0.55f * grip);
                var k0 = Add(palmC, Dir(fa, 2f));
                var k1 = Add(k0, Dir(fa + curl * 0.35f, len1[i]));
                var tip = Add(k1, Dir(fa + curl, 4.2f));
                c.Capsule(k0, k1, 1.25f, 1.05f, skin, z + 0.02f, shade, group);
                c.Capsule(k1, tip, 1.05f, 0.3f, ClawMat, z + 0.03f, shade, group);
                web.Add(Mix(k0, k1, 0.92f));
            }
            web.Add(Add(palmC, Dir(ang + 90f, 2f)));
            c.Poly(web.ToArray(), FinMat, z + 0.01f, 1f, shade, group);
        }

        void DrawHead(ShadedCanvas c, Vector2 hp, float hr, Pose p, PixelMaterial skin, PixelMaterial belly)
        {
            int gHead = c.NewGroup(), gJaw = c.NewGroup(), gMouth = c.NewGroup(), gEye = c.NewGroup(), gFar = c.NewGroup(), gGill = c.NewGroup();
            const float k = HeadScale;
            Vector2 H(float x, float y) => Add(hp, Rotate(V(x * k, y * k), V(0f, 0f), hr));
            float jaw = p["jaw"];
            float jawA = -jaw * 30f;
            var hinge = V(0.5f, -1.8f);
            Vector2 J(float x, float y)
            {
                var l = Add(hinge, Rotate(V(x, y), V(0f, 0f), jawA));
                return Add(hp, Rotate(V(l.x * k, l.y * k), V(0f, 0f), hr));
            }
            float eye = p["eye"];
            bool hot = eye > 0.45f, dull = eye < -0.25f;

            // Ojo lejano: asoma por encima del cráneo
            var eyeFar = H(5.4f, 8.2f);
            c.Ellipse(eyeFar, 3f * k, 2.8f * k, hr, SkinDark, 7.4f, -0.1f, gFar);
            c.Ellipse(Add(eyeFar, V(0.5f, 0.6f)), 1.8f * k, 1.6f * k, 0f, dull ? EyeDull : hot ? EyeMid : EyeRing, 7.45f, 0f, gFar);

            // Fauces: interior oscuro y mandíbula inferior pálida (prognata)
            c.Ellipse(H(7f, -2.6f), 5.6f * k, (1.2f + jaw * 3.2f) * k, hr - jaw * 14f, MouthIn, 7.8f, 0f, gMouth);
            c.Poly(new[] { J(-1.5f, 1.2f), J(6f, 0.8f), J(12.4f, 0.6f), J(13.8f, -0.6f), J(12.6f, -2.6f), J(6.5f, -4.4f), J(0f, -4.1f), J(-2.6f, -2f) },
                   belly, 7.9f, 2f, 0f, gJaw);

            // Cráneo y hocico
            c.Ellipse(H(4.2f, 2.6f), 7.6f * k, 6.1f * k, hr - 8f, skin, 8f, 0f, gHead);
            c.Ellipse(H(10.2f, 0.6f), 4.6f * k, 3.3f * k, hr - 6f, skin, 8.02f, 0f, gHead);
            // Comisura: línea de la boca, ancha y caída
            if (jaw < 0.3f)
                c.Strand(Bezier(H(1f, -2.6f), H(6f, -1.2f), H(13.2f, -0.9f), 5), 0.55f, 0.45f, MouthIn, 8.05f, 0f, gMouth);
            else
                c.Capsule(H(0.6f, -2.8f), H(2.4f, -2f), 0.6f, 0.5f, MouthIn, 8.05f, 0f, gMouth);

            // Branquias: tres hendiduras rojizas tras la mandíbula que se abren al respirar
            float gill = p["gill"];
            for (int i = 0; i < 3; i++)
            {
                var a = H(0.6f - i * 1.9f, -4f + i * 0.4f);
                var m = H(1.6f - i * 1.9f, -1f);
                var b = H(-0.2f - i * 1.9f, 2f - i * 0.5f);
                c.Strand(Bezier(a, m, b, 4), 0.35f + gill * 0.4f, 0.3f + gill * 0.25f, GillMat, 8.15f, 0f, gGill);
            }

            // Dientes de aguja cuando abre la boca
            if (jaw > 0.3f)
            {
                var tooth = PixelCanvas.Hex("d6d0b0");
                for (int t = 0; t < 4; t++)
                {
                    var t0 = H(6f + t * 1.8f, -1.9f);
                    c.Decal(t0.x, t0.y, tooth);
                    var t1 = J(5.4f + t * 1.9f, 1f);
                    c.Decal(t1.x, t1.y, tooth);
                }
            }

            // Ojo saltón
            var E = H(8f, 5.6f);
            float er = 2.9f * k + 0.5f * Mathf.Max(0f, eye);
            c.Ellipse(E, er + 1.1f, er + 0.9f, hr, SkinDark, 8.2f, 0f, gEye);
            c.Ellipse(E, er, er - 0.1f, 0f, dull ? EyeDull : hot ? EyeRingHot : EyeRing, 8.3f, 0f, gEye);
            c.Ellipse(Add(E, V(0.2f, 0.3f)), er - 0.9f, er - 1f, 0f, dull ? EyeDullMid : hot ? EyeMidHot : EyeMid, 8.31f, 0f, gEye);
            if (!dull) c.Ellipse(Add(E, V(-0.6f, 1f)), 1f + 0.4f * Mathf.Max(0f, eye), 0.8f, 0f, hot ? EyeCoreHot : EyeCore, 8.32f, 0f, gEye);
            float pr = dull ? 1.6f : 1.2f - 0.35f * Mathf.Max(0f, eye);
            c.Ellipse(Add(E, Rotate(V(1f, -0.2f), V(0f, 0f), hr)), pr, pr, 0f, Pupil, 8.4f, 0f, gEye);
        }

        /// <summary>Aleta dorsal: espinas óseas unidas por membrana, con ondulación desfasada (movimiento secundario).</summary>
        void DrawFin(ShadedCanvas c, Vector2[] bases, Vector2 back, float ta, float flare, float wave, int group)
        {
            int n = bases.Length;
            float[] lens = { 6f, 10f, 12.5f, 12f, 9f, 6f };
            float[] sweeps = { -18f, -24f, -16f, -6f, 6f, 18f };
            var tips = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float sweep = sweeps[i] - 20f * flare;
                float a = ta + 90f + sweep + 6f * Mathf.Sin(wave - i * 0.9f);
                tips[i] = Add(bases[i], Dir(a, lens[i] * (1f + 0.15f * flare)));
            }
            var poly = new List<Vector2> { bases[0] };
            for (int i = 0; i < n; i++)
            {
                poly.Add(tips[i]);
                if (i < n - 1) poly.Add(Mix(Mix(bases[i], tips[i], 0.55f), Mix(bases[i + 1], tips[i + 1], 0.55f), 0.5f));
            }
            poly.Add(bases[n - 1]);
            for (int i = n - 1; i >= 1; i--) poly.Add(Sub(bases[i], Scale(back, 2.5f)));
            c.Poly(poly.ToArray(), FinMat, 2f, 1.6f, 0f, group, -0.2f, 0.3f);
            for (int i = 0; i < n; i++)
                c.Capsule(Sub(bases[i], Scale(back, 1.5f)), tips[i], 1.05f, 0.4f, SpineMat, 2.1f, 0f, group);
        }

        /// <summary>Estela de tres líneas (una por garra): fina en la cola, gruesa en la punta.</summary>
        void DrawClawSmear(ShadedCanvas c, Vector2 center, float outer, SmearSpec s)
        {
            float r = outer + 2f;
            for (int i = 0; i < 3; i++)
            {
                float rad = outer - i * 3.8f;
                float th = (s.Strength >= 1f ? 2.6f : 1.9f) - i * 0.35f;
                c.Custom(center.x - r, center.y - r, center.x + r, center.y + r, ClawArc(center, rad + 0.6f, 0.6f, th + 1.2f, s.From, s.To), SmearSoft, 8.9f);
                c.Custom(center.x - r, center.y - r, center.x + r, center.y + r, ClawArc(center, rad, 0.4f, th * 0.75f, s.From, s.To), SmearMid, 8.95f);
                c.Custom(center.x - r, center.y - r, center.x + r, center.y + r, ClawArc(center, rad, 0.2f, th * 0.4f, s.From, s.To), SmearCore, 8.97f);
            }
        }

        static System.Func<float, float, (bool, N3)> ClawArc(Vector2 center, float radius, float thickTail, float thickLead, float fromAngle, float toAngle)
        {
            float a0 = Mathf.Min(fromAngle, toAngle), a1 = Mathf.Max(fromAngle, toAngle);
            bool leadAtEnd = toAngle >= fromAngle;
            return (px, py) =>
            {
                float dx = px - center.x, dy = py - center.y;
                float rr = Mathf.Sqrt(dx * dx + dy * dy);
                if (rr > radius || rr < radius - thickLead - 0.5f) return (false, N3.Front);
                float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                while (ang < a0 - 180f) ang += 360f;
                while (ang > a0 + 180f) ang -= 360f;
                if (ang < a0 || ang > a1) return (false, N3.Front);
                float t = (ang - a0) / Mathf.Max(0.001f, a1 - a0);
                if (!leadAtEnd) t = 1f - t;
                float th = Mathf.Lerp(thickTail, thickLead, t * t);
                return (rr >= radius - th, N3.Front);
            };
        }

        /// <summary>Borde roto que cuelga (gravedad + vaivén) entre dos puntos.</summary>
        static IEnumerable<Vector2> Jagged(Vector2 from, Vector2 to, int teeth, float depth, int seed, float sway)
        {
            int count = teeth * 2;
            for (int i = 1; i < count; i++)
            {
                float t = i / (float)count;
                var p = Mix(from, to, t);
                float d = (i % 2 == 1 ? depth : 0.3f) * (0.6f + 0.8f * PixelCanvas.Hash(i, seed, 3));
                yield return Add(p, V(-sway * d * 0.5f, -d));
            }
        }
    }
}
