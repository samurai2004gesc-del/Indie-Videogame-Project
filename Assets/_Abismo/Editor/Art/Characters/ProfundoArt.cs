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
        // Piel húmeda: el relieve (escamas, verrugas, pliegues) va en cada pieza; el brillo especular puntual
        // salta en las crestas que miran a la luz y deja reflejos de 1 px, como piel mojada.
        static readonly PixelMaterial Skin = new PixelMaterial(SkinRamp)
        {
            Ambient = 0.24f, Rim = 0.62f, Dither = 0.02f, SpecularAt = 0.9f, SpecularPower = 30f, Specular = PixelCanvas.Hex("c9e6d6"),
        };
        static readonly PixelMaterial Belly = new PixelMaterial(BellyRamp)
        {
            Ambient = 0.3f, Rim = 0.4f, Dither = 0.02f, SpecularAt = 0.86f, SpecularPower = 30f, Specular = PixelCanvas.Hex("f4f1dc"),
        };
        static readonly PixelMaterial SkinDark = new PixelMaterial(Ramp.Make("2f3d3f", 4, 0.08f, 0.35f, 1.5f)) { Ambient = 0.26f, Rim = 0.5f };
        static readonly PixelMaterial FinMat = new PixelMaterial(Ramp.Make("4b3d4d", 4, 0.1f, 0.38f, 1.6f)) { Ambient = 0.3f, Rim = 0.8f, Dither = 0f };
        static readonly PixelMaterial SpineMat = new PixelMaterial(Ramp.Make("3b3139", 4, 0.06f, 0.4f, 1.6f)) { Ambient = 0.3f, Rim = 0.6f, Dither = 0f };
        // Ropa de pescador: lona con tejido en relieve pegado a cada pieza (no "nada" al moverse).
        static readonly PixelMaterial Shirt = new PixelMaterial(Ramp.Make("5a5343", 5, 0.1f, 0.33f, 1.4f))
        {
            Ambient = 0.26f, Rim = 0.5f, Dither = 0f, Bump = Patterns.Weave(2f, 0.4f), BumpStrength = 0.25f,
        };
        static readonly PixelMaterial Trousers = new PixelMaterial(Ramp.Make("323848", 4, 0.08f, 0.35f, 1.5f))
        {
            Ambient = 0.22f, Rim = 0.45f, Dither = 0f, Bump = Patterns.Weave(2f, 0.5f), BumpStrength = 0.35f,
        };
        /// <summary>Remiendo del pantalón: arpillera ocre.</summary>
        static readonly PixelMaterial TrouserPatch = new PixelMaterial(Ramp.Make("675637", 4, 0.1f, 0.36f, 1.45f))
        {
            Ambient = 0.24f, Rim = 0.45f, Dither = 0f, Bump = Patterns.Weave(1.5f, 0.6f), BumpStrength = 0.5f,
        };
        static readonly PixelMaterial Rope = new PixelMaterial(Ramp.Make("7d6c4a", 4, 0.08f)) { Ambient = 0.3f, Detail = HombrePez.Torcida };
        static readonly PixelMaterial Button = new PixelMaterial(Ramp.Make("b7ad8a", 3, 0.08f, 0.5f, 1.25f)) { Ambient = 0.4f, Gloss = 0.3f, Dither = 0f, Outline = false };
        static readonly PixelMaterial ClawMat = new PixelMaterial(Ramp.Make("b3a885", 4, 0.1f, 0.4f, 1.3f))
        {
            Ambient = 0.35f, Gloss = 0.3f, Dither = 0f, SpecularAt = 0.8f, SpecularPower = 16f, Specular = PixelCanvas.Hex("fffbe6"),
        };
        static readonly PixelMaterial MouthIn = new PixelMaterial(Ramp.Make("3a1a26", 3, 0.05f, 0.5f, 1.3f)) { Ambient = 0.5f, Outline = false };
        static readonly PixelMaterial Puddle = new PixelMaterial(Ramp.Make("1f2e2e", 4, 0.06f, 0.5f, 1.9f)) { Ambient = 0.2f, Gloss = 0.9f, Rim = 0.3f, Dither = 0f };
        static readonly PixelMaterial Water = new PixelMaterial(Ramp.Make("5f8a86", 3, 0.06f, 0.6f, 1.5f)) { Ambient = 0.4f, Outline = false, ReceivesContactShadow = false };

        static readonly PixelMaterial SmearCore = new PixelMaterial(new[] { new Color32(246, 242, 206, 245) }) { Emissive = true, Outline = false, ReceivesContactShadow = false };
        static readonly PixelMaterial SmearMid = new PixelMaterial(new[] { new Color32(196, 196, 150, 200) }) { Emissive = true, Outline = false, ReceivesContactShadow = false };
        static readonly PixelMaterial SmearSoft = new PixelMaterial(new[] { new Color32(110, 128, 104, 140) }) { Emissive = true, Outline = false, ReceivesContactShadow = false };

        // Relieves de la piel (coordenadas locales de cada pieza, así viajan pegados al cuerpo al animar).
        // Tronco: escamas grandes en el lomo y la joroba, más finas hacia el vientre, en racimos; piel lisa con
        // verrugas y manchas entre los racimos.
        static readonly HombrePez.Escamas TorsoScales = new HombrePez.Escamas
        {
            Grande = 6f, Fina = 4.5f, V0 = -3f, V1 = 7f, Racimo = 0.6f, LomoDesde = 6f, Hondo = 1f, Semilla = 3,
        };
        static readonly HombrePez.Escamas HeadScales = new HombrePez.Escamas
        {
            Grande = 4.5f, Fina = 4f, V0 = 2f, V1 = 6f, Racimo = 0.8f, LomoDesde = 5f, VMin = 2.4f, UMax = 9f, Hondo = 0.9f,
            Verrugas = 0.3f, Semilla = 9,
        };
        static readonly HombrePez.Escamas LimbScales = new HombrePez.Escamas
        {
            Grande = 4f, Fina = 4f, Racimo = 0.55f, HaciaU = true, Hondo = 0.8f, Verrugas = 0.18f, Semilla = 17,
        };
        static readonly HombrePez.Pliegues BellyFolds = new HombrePez.Pliegues { Periodo = 2.6f, Ondula = 0.6f, Alto = 0.8f, Semilla = 5 };
        static readonly HombrePez.Pliegues ThroatFolds = new HombrePez.Pliegues { Periodo = 2.2f, Ondula = 0.4f, Alto = 0.7f, Semilla = 2 };

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

            int gTorso = c.NewGroup(), gNeck = c.NewGroup(), gLegF = c.NewGroup(), gLegB = c.NewGroup();
            int gArmF = c.NewGroup(), gArmB = c.NewGroup(), gFin = c.NewGroup(), gShirt = c.NewGroup(), gBelt = c.NewGroup();

            // --- Charco (muerte) ---
            float pud = p["puddle"];
            if (pud > 0.01f)
            {
                var pc = V(hip.x + 12f, 0.6f);
                c.Ellipse(pc, 8f + 28f * pud, 1.6f + 1.6f * pud, 0f, Puddle, -2f, 0f, 0, 0.5f);
                // Reflejos alargados en el agua y una onda más clara.
                c.Glint(Add(pc, V(-4f - 6f * pud, 0.4f)), 0, 0, Puddle);
                c.Glint(Add(pc, V(5f + 9f * pud, 0.2f)), 0, 0, Puddle);
                if (pud > 0.4f) c.Ridge(new[] { Add(pc, V(-14f * pud, 0.9f)), Add(pc, V(-4f * pud, 1.3f)) }, 1, 0, Puddle);
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
            float sway = p["shirt"];
            float wave = p["finWave"];

            // Brazo y pierna traseros (lejos de la luz)
            DrawLeg(c, hip, kneeB, ankleB, p["fBa"], 1.5f, -0.13f, gLegB, false);
            DrawArm(c, shoulderB, elbowB, handB, armB2, p["wristB"], p["gripB"], p["palmB"], 1f, -0.13f, gArmB, false, 0f, wave);

            // --- Aleta dorsal (detrás del tronco) ---
            Vector2 Hd(float x, float y) => Add(hp, Rotate(V(x * HeadScale, y * HeadScale), V(0f, 0f), hr));
            DrawFin(c, new[] { Hd(-2f, 6.4f), T(21.5f, 6.8f), T(18f, 10.4f), T(14f, 11.8f), T(10f, 11.4f), T(6.5f, 9.6f) },
                    B, ta, p["fin"], wave, gFin);

            // --- Tronco: barriga, joroba, pecho (escamas grandes en el lomo, en racimos que siguen la espalda) ---
            HombrePez.Relieve(c.Capsule(T(0f, 0.5f), T(10f, 0f), 7.6f, 8.2f, Skin, 4f, 0f, gTorso), TorsoScales, hip, ta);
            HombrePez.Relieve(c.Ellipse(T(13f, 3f), 10.2f, 8.8f * squash, ta, Skin, 4f, 0f, gTorso), TorsoScales, hip, ta);
            HombrePez.Relieve(c.Ellipse(T(17f, -1.5f), 6.8f, 7f * squash, ta, Skin, 4f, 0f, gTorso), TorsoScales, hip, ta);
            // Barriga pálida que asoma por la camisa rota: placas ventrales finas, como el vientre de un pez.
            HombrePez.Relieve(c.Ellipse(T(5.5f, -4.8f), 7.2f, 5.4f * squash, ta, Belly, 4.2f, 0f, gTorso), BellyFolds, hip, ta);
            // Línea lateral: fila de poros a lo largo del flanco (solo se ve donde asoma la piel).
            for (int i = 0; i < 7; i++)
                c.Dot(T(1.5f + i * 2.3f, 2.6f + Mathf.Sin(i * 0.9f) * 0.4f), -2, gTorso, Skin);
            // Percebes y una lapa agarrados a la joroba.
            HombrePez.Percebes(c, T(15.5f, 9.4f), 3, 1.05f, 4.08f, 41);
            HombrePez.Percebes(c, T(8.2f, 7.6f), 1, 1.3f, 4.08f, 43);

            // Pantalón (cadera) y cinturón de cuerda; la tela se frunce bajo la cuerda.
            c.Ellipse(T(-1.8f, 0f), 4.6f, 8.6f * squash, ta, Trousers, 4.3f, 0f, gBelt).Frame(hip, ta);
            foreach (float v in new[] { -6.2f, -3f, 0.4f, 3.6f })
                c.Fold(new[] { T(1.6f, v), T(-1f, v + 0.6f), T(-3.4f, v + 0.9f) }, gBelt, 2, 1, Trousers);
            c.Capsule(T(2.6f, -8.8f), T(2.6f, 6f), 1.1f, 1.1f, Rope, 4.4f, 0f, gBelt);
            // Nudo y cabo colgando, deshilachado en la punta.
            int gKnot = c.NewGroup();
            var knot = T(2.4f, -9.1f);
            c.Ellipse(knot, 1.6f, 1.4f, ta, Rope, 4.46f, 0f, gKnot);
            var ropeEnd = Add(knot, V(1.2f + sway * 0.8f, -4.4f));
            c.Capsule(knot, ropeEnd, 0.8f, 0.6f, Rope, 4.45f, 0f, gKnot);
            c.Capsule(ropeEnd, Add(ropeEnd, V(-0.6f + sway * 0.4f, -1.6f)), 0.4f, 0.35f, Rope, 4.45f, 0.05f, gKnot);
            c.Capsule(ropeEnd, Add(ropeEnd, V(0.7f + sway * 0.5f, -1.3f)), 0.4f, 0.35f, Rope, 4.45f, -0.08f, gKnot);
            // Algas enganchadas en la cuerda (movimiento secundario, desfasado).
            HombrePez.Alga(c, T(2.3f, -2.6f), 8.5f, sway * 1.2f, wave * 0.5f, 4.47f, 51);
            HombrePez.Alga(c, T(2.5f, 4f), 6.5f, sway * 1.5f, wave * 0.5f + 1.3f, 4.47f, 52);

            // Camisa de pescador hecha jirones: cubre pecho y hombro; la espalda está reventada por la joroba y la aleta.
            var shirt = new List<Vector2> { T(21.4f, -2.8f), T(19f, -7.6f), T(14.5f, -9.4f), T(11f, -9.1f) };
            shirt.AddRange(Jagged(T(11f, -9.1f), T(6f, 3.5f), 4, 3.4f, 7, sway));
            shirt.Add(T(6f, 3.5f));
            shirt.AddRange(Jagged(T(6f, 3.5f), T(15.5f, 8.2f), 3, 1.6f, 12, 0f));
            shirt.AddRange(new[] { T(15.5f, 8.2f), T(19.5f, 7f), T(21.8f, 3.8f), T(22.4f, 0.6f) });
            c.Poly(shirt.ToArray(), Shirt, 4.5f, 2.2f, 0f, gShirt, 0.12f, 0f).Frame(hip, ta);
            DrawShirtDetail(c, T, hip, ta, sway, gShirt);

            // Cuello (sale de la camisa) y garganta pálida con pliegues
            HombrePez.Relieve(c.Capsule(T(19f, -0.5f), hp, 5.8f, 5.2f, Skin, 5f, 0f, gNeck), TorsoScales, hip, ta);
            var throatA = T(18.8f, -4.4f);
            HombrePez.Relieve(c.Capsule(throatA, Hd(1.6f, -4.6f), 3f, 2.7f, Belly, 5.1f, 0f, gNeck), ThroatFolds, throatA,
                              Mathf.Atan2(Hd(1.6f, -4.6f).y - throatA.y, Hd(1.6f, -4.6f).x - throatA.x) * Mathf.Rad2Deg);
            // Cuello de la camisa, con su canto iluminado
            int gCollar = c.NewGroup();
            c.Poly(new[] { T(22f, -2.4f), T(19.6f, -8f), T(17.8f, -6.4f), T(19.2f, -1.4f) }, Shirt, 5.2f, 1.2f, 0f, gCollar).Frame(hip, ta);
            c.Ridge(new[] { T(21.6f, -2.6f), T(19.4f, -7.6f) }, 1, gCollar, Shirt);

            // Pierna delantera
            DrawLeg(c, hip, kneeF, ankleF, p["fFa"], 6f, 0f, gLegF, true);

            // Cabeza
            HombrePez.Cabeza(c, hp, hr, HeadScale, p["jaw"], p["gill"], p["eye"], Skin, Belly, SkinDark, MouthIn, HeadScales, false, 0.5f);

            // Estela de las garras
            if (smear.On)
            {
                float reach = Mathf.Sqrt((handF.x - shoulderF.x) * (handF.x - shoulderF.x) + (handF.y - shoulderF.y) * (handF.y - shoulderF.y)) + 9f;
                DrawClawSmear(c, shoulderF, reach, smear);
            }

            // Brazo delantero
            DrawArm(c, shoulderF, elbowF, handF, armF2, p["wristF"], p["gripF"], p["palmF"], 9.5f, 0f, gArmF, true, sway, wave);

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

        /// <summary>
        /// Camisa: pliegues que caen del hombro y la joroba, costura del hombro, tapeta cosida con botones de hueso
        /// (falta uno), un desgarrón por el que asoma la piel y un hilo suelto.
        /// </summary>
        void DrawShirtDetail(ShadedCanvas c, System.Func<float, float, Vector2> T, Vector2 hip, float ta, float sway, int gShirt)
        {
            // Pliegues (surco oscuro + cresta clara del lado de la luz)
            c.Fold(new[] { T(18.5f, -6.6f), T(14.5f, -5.4f), T(10f, -5.2f) }, gShirt, 2, 1, Shirt);
            c.Fold(new[] { T(17.5f, 2.2f), T(13.5f, 1.2f), T(9f, 1.6f) }, gShirt, 2, 1, Shirt);
            // Costura del hombro (pespunte)
            c.Stitch(new[] { T(20.6f, -1.6f), T(19.2f, 2.4f), T(17.4f, 6.4f) }, gShirt, 1, 1, 1, Shirt);
            // Tapeta con pespunte y botones de hueso (uno se perdió: queda el ojal deshilachado)
            c.Capsule(T(20.6f, -4.6f), T(12.5f, -7.6f), 0.7f, 0.7f, Shirt, 4.55f, -0.22f, gShirt);
            c.Stitch(new[] { T(20.4f, -3.7f), T(12.6f, -6.7f) }, gShirt, 1, 1, 1, Shirt);
            int gButton = c.NewGroup();
            foreach (var u in new[] { 18.4f, 15.2f })
            {
                var bp = T(u, -5.4f - (20.6f - u) * 0.37f);
                c.Ellipse(bp, 0.85f, 0.85f, 0f, Button, 4.58f, 0f, gButton);
            }
            c.Dot(T(12.4f, -7.1f), -2, gShirt, Shirt);
            // Desgarrón junto al bajo: la piel asoma en sombra y el borde deshilachado recoge la luz
            var hole = new[] { T(7.2f, -0.6f), T(8.8f, -1.2f), T(9.6f, 0.2f), T(8.4f, 1.4f), T(7.4f, 0.9f) };
            HombrePez.Relieve(c.Poly(hole, Skin, 4.505f, 0.8f, -0.16f, gShirt), TorsoScales, hip, ta);
            c.Ridge(new[] { T(9.9f, 0.2f), T(8.6f, 1.8f) }, 1, gShirt, Shirt);
            // Hilo suelto colgando del bajo
            var th = T(7.4f, -6.2f);
            c.Capsule(th, Add(th, V(-0.4f - sway * 0.6f, -3.2f)), 0.42f, 0.38f, Shirt, 4.49f, 0.05f, gShirt);
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

        void DrawLeg(ShadedCanvas c, Vector2 hip, Vector2 knee, Vector2 ankle, float footA, float z, float shade, int group, bool front)
        {
            // Muslo de rana, espinilla fina con escamitas y pie largo palmeado.
            HombrePez.Relieve(c.Capsule(hip, knee, 5f, 3.5f, Skin, z, shade, group), LimbScales);
            HombrePez.Relieve(c.Capsule(knee, ankle, 3.2f, 1.9f, Skin, z + 0.02f, shade, group), LimbScales);
            // Pantalón roto por encima de la rodilla
            var cut = Mix(hip, knee, 0.7f);
            c.Capsule(hip, cut, 5.4f, 4.5f, Trousers, z + 0.05f, shade, group);
            var axis = Sub(knee, hip);
            float len = Mathf.Max(0.01f, Mathf.Sqrt(axis.x * axis.x + axis.y * axis.y));
            axis = Scale(axis, 1f / len);
            var side = V(-axis.y, axis.x);
            var hem = new List<Vector2> { Add(cut, Scale(side, 4.6f)), Add(Mix(hip, knee, 0.45f), Scale(side, 4.9f)), Add(Mix(hip, knee, 0.45f), Scale(side, -4.9f)), Add(cut, Scale(side, -4.6f)) };
            var frayed = new List<Vector2>();
            for (int i = 1; i < 6; i++)
            {
                float t = i / 6f;
                var e = Add(cut, Scale(side, Mathf.Lerp(-4.6f, 4.6f, t)));
                float d = (i % 2 == 1 ? 2.8f : 0.6f) * (0.6f + 0.7f * PixelCanvas.Hash(i, front ? 3 : 4, 11));
                hem.Add(Add(e, Scale(axis, d)));
                if (i % 2 == 1) frayed.Add(Add(e, Scale(axis, d - 0.4f)));
            }
            c.Poly(hem.ToArray(), Trousers, z + 0.06f, 1.4f, shade, group).Frame(hip, Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg);
            // Hilachas del bajo roto
            foreach (var f0 in frayed)
                c.Capsule(f0, Add(f0, Add(Scale(axis, 1.6f), V(0f, -0.8f))), 0.4f, 0.35f, Trousers, z + 0.055f, shade + 0.04f, group);
            // Costura lateral y pliegue del muslo
            c.Stitch(new[] { Add(Mix(hip, knee, 0.08f), Scale(side, 3.4f)), Add(Mix(hip, knee, 0.6f), Scale(side, 3.1f)) }, group, 1, 1, 1, Trousers);
            c.Fold(new[] { Add(Mix(hip, knee, 0.15f), Scale(side, -1.2f)), Add(Mix(hip, knee, 0.4f), Scale(side, -2.2f)), Add(Mix(hip, knee, 0.62f), Scale(side, -1.6f)) },
                   group, 2, 1, Trousers);
            if (front)
            {
                // Remiendo de arpillera cosido en el muslo
                int gPatch = c.NewGroup();
                var pq = new[]
                {
                    Add(Mix(hip, knee, 0.22f), Scale(side, 1.8f)), Add(Mix(hip, knee, 0.5f), Scale(side, 2.2f)),
                    Add(Mix(hip, knee, 0.52f), Scale(side, -1.2f)), Add(Mix(hip, knee, 0.24f), Scale(side, -1.6f)),
                };
                c.Poly(pq, TrouserPatch, z + 0.07f, 0.8f, shade, gPatch).Frame(hip, Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg);
                var pcen = Mix(Mix(pq[0], pq[1], 0.5f), Mix(pq[2], pq[3], 0.5f), 0.5f);
                var inset = new Vector2[5];
                for (int i = 0; i < 4; i++) inset[i] = Mix(pq[i], pcen, 0.25f);
                inset[4] = inset[0];
                c.Stitch(inset, gPatch, 1, 1, 1, TrouserPatch);
                // Verrugas y un racimo de percebes en la rodilla
                HombrePez.Percebes(c, Add(knee, Add(Scale(side, 1.2f), V(0.8f, -1.6f))), 2, 0.95f, z + 0.03f, 13);
            }
            // Pliegue de la rodilla
            c.Crease(new[] { Add(knee, Add(Scale(axis, 1.2f), Scale(side, -2.4f))), Add(knee, Add(Scale(axis, 0.4f), Scale(side, -0.6f))) }, 1, group, Skin);

            // Pie: metatarso hasta la bola, dedos palmeados y uñas.
            var ball = Add(ankle, Dir(footA, FootLen));
            HombrePez.Relieve(c.Capsule(ankle, ball, 2.2f, 1.5f, Skin, z + 0.03f, shade, group), LimbScales);
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
                c.Capsule(ball, tip, 1.1f, 0.75f, Skin, z + 0.04f + i * 0.001f, shade, group);
                c.Capsule(tip, Add(tip, Dir(ta2 - (tip.y < 2f ? 0f : 25f), 1.8f)), 0.75f, 0.3f, ClawMat, z + 0.045f, shade, group);
                // Nudillo del dedo
                c.Dot(Add(ball, Scale(dir, lenT[i] * 0.55f)), 1, group, Skin);
                web.Add(Add(ball, Scale(dir, lenT[i] * 0.85f)));
            }
            c.Poly(web.ToArray(), FinMat, z + 0.035f, 1f, shade, group).WithDetail(HombrePez.Membrana(ball, web[2], lenT[1]));
        }

        void DrawArm(ShadedCanvas c, Vector2 shoulder, Vector2 elbow, Vector2 hand, float foreAngle, float wrist, float grip, float palm,
                     float z, float shade, int group, bool front, float sway, float wave)
        {
            HombrePez.Relieve(c.Capsule(shoulder, elbow, 3.7f, 2.7f, Skin, z, shade, group), LimbScales);
            HombrePez.Relieve(c.Capsule(elbow, hand, 2.8f, 2.2f, Skin, z + 0.05f, shade, group), LimbScales);
            // Espinas membranosas en el antebrazo (lado de fuera)
            var fa = Sub(hand, elbow);
            float flen = Mathf.Max(0.01f, Mathf.Sqrt(fa.x * fa.x + fa.y * fa.y));
            fa = Scale(fa, 1f / flen);
            var outSide = V(fa.y, -fa.x); // lado "trasero" del antebrazo
            var f0 = Add(elbow, Scale(fa, 2f));
            var f1 = Add(elbow, Scale(fa, flen * 0.7f));
            var t0 = Add(Add(f0, Scale(outSide, 4.6f)), Scale(fa, -1.6f));
            var t1 = Add(Add(f1, Scale(outSide, 3.4f)), Scale(fa, -0.6f));
            HombrePez.Aleta(c, new[] { f0, f1 }, new[] { t0, t1 }, V(0f, 0f), FinMat, SpineMat, z - 0.02f, shade, group, 1f, 0.7f, 0f, 0f, front ? 61 : 62);
            // Pliegue del codo
            var across = V(-fa.y, fa.x);
            c.Crease(new[] { Add(elbow, Add(Scale(fa, 0.8f), Scale(across, 1.8f))), Add(elbow, Add(Scale(fa, 0.2f), Scale(across, 0.2f))) }, 1, group, Skin);
            if (front)
            {
                // Percebes en el hombro y un alga enredada en el antebrazo
                var ua = Sub(elbow, shoulder);
                HombrePez.Percebes(c, Add(Mix(shoulder, elbow, 0.22f), Scale(V(-ua.y, ua.x), 0.12f)), 2, 0.9f, z + 0.06f, 27);
                HombrePez.Alga(c, Mix(elbow, hand, 0.42f), 6f, sway * 1.4f, wave * 0.5f + 2.1f, z + 0.07f, 53);
            }
            DrawClaw(c, hand, foreAngle - 90f + wrist, grip, palm, z + 0.1f, shade, group);
        }

        /// <summary>Mano palmeada: palma, tres dedos con membrana y garras curvas.</summary>
        void DrawClaw(ShadedCanvas c, Vector2 wristPt, float ang, float grip, float palm, float z, float shade, int group)
        {
            var palmC = Add(wristPt, Dir(ang, 2.2f));
            HombrePez.Relieve(c.Ellipse(palmC, 3.3f, 2.6f, ang, Skin, z, shade, group), LimbScales);
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
                c.Capsule(k0, k1, 1.25f, 1.05f, Skin, z + 0.02f, shade, group);
                c.Capsule(k1, tip, 1.05f, 0.3f, ClawMat, z + 0.03f, shade, group);
                // Nudillo iluminado y cutícula oscura donde nace la uña
                c.Dot(k0, 1, group, Skin);
                c.Dot(Mix(k0, k1, 0.96f), -1, group, Skin);
                web.Add(Mix(k0, k1, 0.92f));
            }
            web.Add(Add(palmC, Dir(ang + 90f, 2f)));
            c.Poly(web.ToArray(), FinMat, z + 0.01f, 1f, shade, group).WithDetail(HombrePez.Membrana(palmC, Mix(web[2], web[3], 0.5f), 5f));
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
            HombrePez.Aleta(c, bases, tips, back, FinMat, SpineMat, 2f, 0f, group, 1.6f, 1.05f, -0.2f, 0.3f, 7);
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

    /// <summary>
    /// Detalle "a mano" compartido por los hombres-pez (el Profundo y el Guardián de la Concha): escamas imbricadas en
    /// racimos, pliegues, verrugas, aletas de membrana translúcida con venas, ojo saltón con reflejo, branquias con
    /// laminillas, dientes de aguja, percebes y algas. Todo va en coordenadas LOCALES de cada pieza (relieve y dibujo
    /// del motor v4), así viaja pegado al cuerpo al animar y también llega al normal map de las luces 2D.
    /// </summary>
    internal static class HombrePez
    {
        // ------------------------------------------------------------------
        // Relieves de piel
        // ------------------------------------------------------------------

        /// <summary>Relieve (altura en px) + dibujo (desplazamiento de banda) en coordenadas locales de una pieza.</summary>
        public interface IRelieve
        {
            float Altura(float u, float v);
            int Dibujo(float u, float v);
        }

        /// <summary>Aplica un relieve a una pieza conservando su marco local automático.</summary>
        public static ShadedCanvas.Shape Relieve(ShadedCanvas.Shape s, IRelieve r, float strength = 0.4f)
        {
            s.Bump = r.Altura;
            s.BumpStrength = strength;
            s.Detail = r.Dibujo;
            return s;
        }

        /// <summary>Aplica un relieve con un marco compartido (varias piezas del tronco con el mismo dibujo continuo).</summary>
        public static ShadedCanvas.Shape Relieve(ShadedCanvas.Shape s, IRelieve r, Vector2 origin, float angle, float strength = 0.4f) =>
            Relieve(s.Frame(origin, angle), r, strength);

        /// <summary>
        /// Escamas imbricadas: filas al tresbolillo cuyo tamaño crece del vientre (v0) al lomo (v1); las de delante
        /// tapan a las de detrás, así cada escama enseña su media luna con el borde libre hacia la cola. Aparecen en
        /// racimos (manchas de ruido por escama entera) y, entre ellos, piel lisa con verrugas y motas.
        /// u = a lo largo (hacia la cabeza, salvo <see cref="HaciaU"/>), v = a través (hacia el lomo).
        /// </summary>
        public sealed class Escamas : IRelieve
        {
            /// <summary>Alto de escama (px) en el lomo y en el vientre; el ancho es 0,85 veces el alto.</summary>
            public float Grande = 6f, Fina = 4f, V0 = -4f, V1 = 6f;
            public float Racimo = 0.55f, LomoDesde = 99f, VMin = -99f, UMin = -99f, UMax = 99f;
            public float Hondo = 1f, Verrugas = 0.22f, Motas = 0.84f;
            /// <summary>true: el borde libre de cada escama mira hacia +u (extremidades: hacia la mano o el pie).</summary>
            public bool HaciaU;
            public int Semilla = 1;

            /// <summary>
            /// Teselas en columnas a lo largo del cuerpo (cada columna desplazada media escama): en cada tesela, el arco
            /// del borde libre (media elipse que se abomba hacia la cola). f &lt; 1 dentro de la escama, 1 en el canto.
            /// </summary>
            bool Buscar(float u, float v, out float f, out float px, out float db)
            {
                float s = HaciaU ? u : -u;
                float h = v >= V1 ? Grande : v >= V0 ? (Grande + Fina) * 0.5f : Fina;
                float w = h * 0.85f;
                int col = Mathf.FloorToInt(s / w);
                float a = s - col * w;
                float off = (col & 1) == 0 ? 0f : h * 0.5f;
                int row = Mathf.FloorToInt((v + off) / h);
                db = (v + off - row * h) / h * 2f - 1f;           // −1..1 a través de la escama
                // El arco corta el borde de la tesela en ángulo (no tangente): las escamas de una columna se tocan en
                // picos y forman el festón ")))" típico.
                float we = w * 0.95f, q = db * 0.8f;
                float k = a / we;
                f = Mathf.Sqrt(k * k + q * q);
                // px = píxeles por unidad de f (gradiente), para que el canto mida 1 px en toda la curva.
                float gk = k / we, gb = q * 0.8f * 2f / h;
                px = Mathf.Max(0.01f, f) / Mathf.Max(0.05f, Mathf.Sqrt(gk * gk + gb * gb));
                float sc = (col + 0.5f) * w, bc = row * h - off + h * 0.5f;
                float uc = HaciaU ? sc : -sc;
                if (uc < UMin || uc > UMax || bc < VMin) return false;
                if (bc >= LomoDesde) return true;
                return PixelCanvas.ValueNoise(col * w / 6.5f, bc / 4.5f, 0, Semilla) > 1f - Racimo;
            }

            float Verruga(float u, float v)
            {
                const float cs = 4.5f;
                int ci = Mathf.FloorToInt(u / cs), cj = Mathf.FloorToInt(v / cs);
                if (PixelCanvas.Hash(ci, cj, Semilla + 7) > Verrugas) return 0f;
                float cx = (ci + 0.3f + 0.4f * PixelCanvas.Hash(ci, cj, Semilla + 8)) * cs;
                float cy = (cj + 0.3f + 0.4f * PixelCanvas.Hash(ci, cj, Semilla + 9)) * cs;
                float rr = 0.9f + 0.5f * PixelCanvas.Hash(ci, cj, Semilla + 10);
                float d2 = ((u - cx) * (u - cx) + (v - cy) * (v - cy)) / (rr * rr);
                return d2 < 1f ? 0.9f * Mathf.Sqrt(1f - d2) : 0f;
            }

            public float Altura(float u, float v)
            {
                // Cada escama es una placa apenas abombada: el volumen lo da el cuerpo; el dibujo, el canto y la luz.
                if (Buscar(u, v, out float f, out _, out _)) return f < 1f ? Hondo * 0.6f * (1f - f) : 0f;
                return Verruga(u, v);
            }

            public int Dibujo(float u, float v)
            {
                if (Buscar(u, v, out float f, out float px, out float db))
                {
                    float d = (f - 1f) * px;
                    // Canto: línea un tono más oscura (la sombra que deja sobre la siguiente)...
                    if (d > -0.55f && d < 0.55f) return -1;
                    // (Sin rayas de luz dentro: a ×1 se leerían como lluvia; el volumen lo pone la luz del cuerpo.)
                    return 0;
                }
                return PixelCanvas.ValueNoise(u / 2.6f, v / 2.6f, 0, Semilla + 3) > Motas ? -1 : 0;
            }
        }

        /// <summary>Pliegues paralelos (placas del vientre, garganta de sapo): surco oscuro y placa abombada.</summary>
        public sealed class Pliegues : IRelieve
        {
            public float Periodo = 2.4f, Ondula = 0.5f, Alto = 0.7f;
            public int Semilla;

            float W(float u, float v) => u + Mathf.Sin(v * 0.55f + Semilla) * Ondula;

            public float Altura(float u, float v)
            {
                float t = Mathf.Repeat(W(u, v), Periodo) / Periodo;
                return Mathf.Sin(t * Mathf.PI) * Alto;
            }

            public int Dibujo(float u, float v) => Mathf.Repeat(W(u, v), Periodo) < 0.75f ? -1 : 0;
        }

        static readonly Pliegues JawFolds = new Pliegues { Periodo = 2f, Ondula = 0.3f, Alto = 0.6f, Semilla = 4 };

        /// <summary>Cuerda torcida: estrías en diagonal (dibujo para el marco de una cápsula).</summary>
        public static readonly System.Func<float, float, int> Torcida = (u, v) => Mathf.Repeat(u + v * 0.9f, 2.2f) < 0.9f ? -1 : 0;

        /// <summary>Membrana palmeada (manos y pies): oscura en la raíz y fina y clara hacia el borde (coordenadas del lienzo).</summary>
        public static System.Func<float, float, int> Membrana(Vector2 root, Vector2 edge, float len) => (x, y) =>
        {
            float d = Mathf.Sqrt((x - root.x) * (x - root.x) + (y - root.y) * (y - root.y)) / Mathf.Max(0.5f, len);
            return d > 0.78f ? 1 : d < 0.3f ? -1 : 0;
        };

        // ------------------------------------------------------------------
        // Materiales compartidos
        // ------------------------------------------------------------------

        static readonly PixelMaterial EyeRing = PixelMaterial.Glow("5e5a1c");
        static readonly PixelMaterial EyeMid = PixelMaterial.Glow("a69e38");
        static readonly PixelMaterial EyeCore = PixelMaterial.Glow("ddd578");
        static readonly PixelMaterial EyeRingHot = PixelMaterial.Glow("948824");
        static readonly PixelMaterial EyeMidHot = PixelMaterial.Glow("e4d846");
        static readonly PixelMaterial EyeCoreHot = PixelMaterial.Glow("fffbcc");
        static readonly PixelMaterial EyeDull = PixelMaterial.Glow("3e3c26");
        static readonly PixelMaterial EyeDullMid = PixelMaterial.Glow("5e5a36");
        static readonly PixelMaterial Pupil = PixelMaterial.Glow("120f0c");
        static readonly PixelMaterial EyeGlint = PixelMaterial.Glow("fffdf0");
        static readonly PixelMaterial EyeGlintDim = PixelMaterial.Glow("efe7a8");
        static readonly PixelMaterial GillMat = new PixelMaterial(Ramp.Make("62262f", 4, 0.06f, 0.42f, 1.7f)) { Ambient = 0.3f, Rim = 0.1f, Dither = 0f, Outline = false };
        static readonly PixelMaterial ToothMat = new PixelMaterial(Ramp.Make("cfc8a6", 3, 0.06f, 0.55f, 1.22f)) { Ambient = 0.45f, Outline = false, Dither = 0f, Gloss = 0.2f };
        static readonly PixelMaterial BarnacleMat = new PixelMaterial(Ramp.Make("aca48c", 4, 0.08f, 0.38f, 1.32f)) { Ambient = 0.3f, Rim = 0.5f, Dither = 0f };
        static readonly PixelMaterial BarnacleHole = new PixelMaterial(Ramp.Make("2b2533", 3, 0.05f, 0.6f, 1.4f)) { Ambient = 0.55f, Outline = false, Dither = 0f };
        public static readonly PixelMaterial AlgaMat = new PixelMaterial(Ramp.Make("55672c", 4, 0.1f, 0.36f, 1.55f)) { Ambient = 0.28f, Rim = 0.7f, Dither = 0f };

        /// <summary>Laminillas de las branquias: rayas claras y oscuras alternas a lo largo de la hendidura.</summary>
        static readonly System.Func<float, float, int> Laminillas = (u, v) => Mathf.Repeat(u, 1.6f) < 0.8f ? 1 : -1;
        /// <summary>Juntas de los radios de una aleta (los radios de los peces son segmentados).</summary>
        static readonly System.Func<float, float, int> Segmentos = (u, v) => u > 1.6f && Mathf.Repeat(u, 2.4f) < 0.8f ? -1 : 0;

        // ------------------------------------------------------------------
        // Piezas
        // ------------------------------------------------------------------

        /// <summary>Exactamente un píxel (en el centro del píxel que contiene <paramref name="p"/>), con su Z (respeta la oclusión).</summary>
        public static void Punto(ShadedCanvas c, Vector2 p, PixelMaterial m, float z, int group)
        {
            float cx = Mathf.Floor(p.x + c.OriginX) + 0.5f - c.OriginX, cy = Mathf.Floor(p.y + c.OriginY) + 0.5f - c.OriginY;
            c.Ellipse(V(cx, cy), 0.55f, 0.55f, 0f, m, z, 0f, group);
        }

        /// <summary>Racimo de percebes: conos pálidos con el agujero oscuro y el canto de arriba iluminado.</summary>
        public static void Percebes(ShadedCanvas c, Vector2 at, int count, float size, float z, int seed)
        {
            for (int i = 0; i < count; i++)
            {
                float ang = PixelCanvas.Hash(i, seed, 1) * 360f;
                float dist = i == 0 ? 0f : size * (1.3f + PixelCanvas.Hash(i, seed, 2) * 0.9f);
                var pos = Add(at, Dir(ang, dist));
                float r = size * (0.85f + PixelCanvas.Hash(i, seed, 3) * 0.5f);
                int g = c.NewGroup();
                c.Ellipse(pos, r, r * 0.9f, 0f, BarnacleMat, z + i * 0.002f, 0f, g);
                c.Ellipse(Add(pos, V(0.2f, 0.4f)), Mathf.Max(0.55f, r * 0.42f), Mathf.Max(0.5f, r * 0.32f), 0f, BarnacleHole, z + 0.001f + i * 0.002f, 0f, g);
                if (r > 1.15f) c.Dot(Add(pos, V(0.6f, -0.5f * r)), -1, g, BarnacleMat);
            }
        }

        /// <summary>Alga colgante: tallo que se estrecha y ondula con frondes alternas (movimiento secundario).</summary>
        public static void Alga(ShadedCanvas c, Vector2 root, float len, float sway, float phase, float z, int seed)
        {
            int g = c.NewGroup();
            const int n = 6;
            var pts = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                float wig = Mathf.Sin(phase + t * 4.2f + seed) * 0.9f * t;
                var q = Add(root, V(-sway * 2.2f * t * t + wig, -len * t));
                pts[i] = V(q.x, Mathf.Max(q.y, 0.8f));
            }
            for (int i = 0; i < n - 1; i++)
            {
                float t0 = i / (float)(n - 1), t1 = (i + 1) / (float)(n - 1);
                c.Capsule(pts[i], pts[i + 1], Mathf.Lerp(0.8f, 0.45f, t0), Mathf.Lerp(0.8f, 0.45f, t1), AlgaMat, z, 0f, g);
            }
            for (int i = 1; i < n - 1; i += 2)
            {
                float side = (i / 2) % 2 == 0 ? 1f : -1f;
                var leaf = Add(pts[i], V(side * 1.2f, -0.9f));
                c.Ellipse(V(leaf.x, Mathf.Max(leaf.y, 1f)), 1.3f, 0.65f, side * -40f, AlgaMat, z - 0.005f, 0.06f, g);
            }
        }

        /// <summary>Abanico de radios: dónde cae un punto entre dos radios (para las venas de la membrana).</summary>
        sealed class Abanico
        {
            readonly Vector2[] b, d;
            readonly float[] len;
            readonly int n;

            public Abanico(Vector2[] bases, Vector2[] tips)
            {
                n = bases.Length;
                b = bases;
                d = new Vector2[n];
                len = new float[n];
                for (int i = 0; i < n; i++)
                {
                    float ex = tips[i].x - bases[i].x, ey = tips[i].y - bases[i].y;
                    float l = Mathf.Max(0.01f, Mathf.Sqrt(ex * ex + ey * ey));
                    d[i] = V(ex / l, ey / l);
                    len[i] = l;
                }
            }

            /// <summary>Raíz en sombra, una vena central entre cada par de radios (que se bifurca hacia el borde) y borde translúcido.</summary>
            public int Membrana(float x, float y)
            {
                float prevD = 0f, prevA = 0f;
                for (int i = 0; i < n; i++)
                {
                    float px = x - b[i].x, py = y - b[i].y;
                    float dd = d[i].x * py - d[i].y * px;
                    float al = (d[i].x * px + d[i].y * py) / len[i];
                    if (i > 0 && (dd > 0f) != (prevD > 0f))
                    {
                        float gap = Mathf.Abs(prevD) + Mathf.Abs(dd);
                        float w = Mathf.Abs(prevD) / Mathf.Max(0.01f, gap);
                        float a = Mathf.Lerp(prevA, al, w);
                        if (a < 0.14f) return -1;
                        if (gap > 3.2f)
                        {
                            float fork = a > 0.55f ? (a - 0.55f) * 0.55f : 0f;
                            if (Mathf.Abs(w - 0.5f + fork) * gap < 0.5f || (fork > 0f && Mathf.Abs(w - 0.5f - fork) * gap < 0.5f)) return -1;
                        }
                        return a > 0.7f ? 1 : 0;
                    }
                    prevD = dd;
                    prevA = al;
                }
                return 0;
            }
        }

        /// <summary>
        /// Aleta: membrana translúcida (más clara hacia el borde, venas que se bifurcan, escotaduras rasgadas) tendida
        /// entre radios óseos segmentados. <paramref name="back"/> ≠ 0 alarga la raíz por dentro del cuerpo.
        /// </summary>
        public static void Aleta(ShadedCanvas c, Vector2[] bases, Vector2[] tips, Vector2 back, PixelMaterial membrana, PixelMaterial espina,
                                 float z, float shade, int group, float bevel, float spineR, float tiltX, float tiltY, int seed)
        {
            int n = bases.Length;
            var poly = new List<Vector2> { bases[0] };
            for (int i = 0; i < n; i++)
            {
                poly.Add(tips[i]);
                if (i >= n - 1) continue;
                Vector2 M(float t, float s) => Mix(Mix(bases[i], tips[i], t), Mix(bases[i + 1], tips[i + 1], t), s);
                if (n > 2 && PixelCanvas.Hash(i, seed, 5) > 0.55f)
                {
                    // Desgarro en V asimétrico
                    poly.Add(M(0.66f, 0.28f));
                    poly.Add(M(0.36f, 0.56f));
                    poly.Add(M(0.6f, 0.78f));
                }
                else poly.Add(M(0.55f, 0.5f));
            }
            poly.Add(bases[n - 1]);
            bool deep = back.x != 0f || back.y != 0f;
            if (deep)
                for (int i = n - 1; i >= 1; i--) poly.Add(Sub(bases[i], Scale(back, 2.5f)));
            var fan = new Abanico(bases, tips);
            c.Poly(poly.ToArray(), membrana, z, bevel, shade, group, tiltX, tiltY).WithDetail(fan.Membrana);
            for (int i = 0; i < n; i++)
            {
                var b0 = deep ? Sub(bases[i], Scale(back, 1.5f)) : bases[i];
                c.Capsule(b0, tips[i], spineR, 0.4f, espina, z + 0.01f, shade, group).WithDetail(Segmentos);
            }
        }

        /// <summary>Ojo saltón: cuenca oscura, iris amarillo con luz interior abajo, pupila y reflejo húmedo de 1 px.</summary>
        public static void Ojo(ShadedCanvas c, Vector2 E, float er, float hr, float eye, PixelMaterial socket, float z, int g, float coreGrow)
        {
            bool hot = eye > 0.45f, dull = eye < -0.25f;
            float e = Mathf.Max(0f, eye);
            c.Ellipse(E, er + 1.1f, er + 0.9f, hr, socket, z, 0f, g);
            c.Ridge(new[] { Add(E, Dir(hr + 150f, er + 1.4f)), Add(E, Dir(hr + 95f, er + 1.2f)), Add(E, Dir(hr + 40f, er + 1.3f)) }, 1, g, socket);
            c.Ellipse(E, er, er - 0.1f, 0f, dull ? EyeDull : hot ? EyeRingHot : EyeRing, z + 0.1f, 0f, g);
            c.Ellipse(Add(E, V(0.2f, 0.3f)), er - 0.9f, er - 1f, 0f, dull ? EyeDullMid : hot ? EyeMidHot : EyeMid, z + 0.11f, 0f, g);
            // Luz interior: el iris se enciende abajo, como un farol tras un cristal empañado.
            if (!dull) c.Ellipse(Add(E, Rotate(V(0.4f, -0.9f), V(0f, 0f), hr)), 1.1f + coreGrow * e, 0.8f + 0.3f * e, hr, hot ? EyeCoreHot : EyeCore, z + 0.12f, 0f, g);
            float pr = dull ? 1.6f : 1.2f - 0.35f * e;
            var pupil = Add(E, Rotate(V(1f, -0.2f), V(0f, 0f), hr));
            c.Ellipse(pupil, pr, pr, 0f, Pupil, z + 0.2f, 0f, g);
            // Reflejos: uno vivo arriba (la luz de la escena en el ojo mojado) y otro tenue abajo.
            if (!dull)
            {
                Punto(c, Add(E, Rotate(V(-0.6f, 1.3f), V(0f, 0f), hr)), EyeGlint, z + 0.3f, g);
                Punto(c, Add(pupil, Rotate(V(0.9f, -1.1f), V(0f, 0f), hr)), EyeGlintDim, z + 0.3f, g);
            }
        }

        /// <summary>Tres hendiduras branquiales con laminillas rojas y el borde del opérculo recogiendo la luz.</summary>
        public static void Branquias(ShadedCanvas c, System.Func<float, float, Vector2> H, float gill, float z, int g, int gHead)
        {
            for (int i = 0; i < 3; i++)
            {
                var pts = Bezier(H(0.6f - i * 1.9f, -4f + i * 0.4f), H(1.6f - i * 1.9f, -1f), H(-0.2f - i * 1.9f, 2f - i * 0.5f), 4);
                float r0 = 0.35f + gill * 0.4f, r1 = 0.3f + gill * 0.25f;
                for (int s = 0; s < pts.Length - 1; s++)
                {
                    float ra = Mathf.Lerp(r0, r1, s / 3f), rb = Mathf.Lerp(r0, r1, (s + 1) / 3f);
                    c.Capsule(pts[s], pts[s + 1], ra, rb, GillMat, z, 0f, g).WithDetail(Laminillas);
                }
                c.Ridge(Bezier(H(1.5f - i * 1.9f, -3.8f + i * 0.4f), H(2.7f - i * 1.9f, -1f), H(0.9f - i * 1.9f, 1.9f - i * 0.5f), 4), 1, gHead);
            }
        }

        /// <summary>Dientes de aguja: dos filas alternas (largos y cortos) con la boca abierta; colmillos que asoman si está cerrada.</summary>
        public static void Dientes(ShadedCanvas c, System.Func<float, float, Vector2> H, System.Func<float, float, Vector2> J, float jaw, float z, int g)
        {
            if (jaw > 0.3f)
            {
                for (int t = 0; t < 6; t++)
                {
                    float l = t % 2 == 0 ? 1.8f : 1.1f;
                    c.Capsule(H(5.4f + t * 1.4f, -2.1f), H(5.6f + t * 1.4f, -2.1f - l), 0.5f, 0.2f, ToothMat, z, 0f, g);
                    c.Capsule(J(5f + t * 1.45f, 0.6f), J(5.1f + t * 1.45f, 0.6f + l * 0.9f), 0.5f, 0.2f, ToothMat, z, 0f, g);
                }
            }
            else
            {
                c.Capsule(J(10.8f, 0.3f), J(11.1f, 2f), 0.5f, 0.25f, ToothMat, z + 0.01f, 0f, g);
                c.Capsule(H(4.6f, -2.1f), H(4.8f, -3.4f), 0.45f, 0.2f, ToothMat, z + 0.01f, 0f, g);
            }
        }

        /// <summary>
        /// Cabeza de pez: cráneo con escamitas y verrugas, hocico liso con narina y poros sensoriales, mandíbula prognata
        /// con pliegues y labio claro, fauces con dientes, branquias y ojo saltón (más el ojo lejano asomando).
        /// Devuelve el grupo del cráneo (para marcas encima).
        /// </summary>
        public static int Cabeza(ShadedCanvas c, Vector2 hp, float hr, float k, float jaw, float gill, float eye,
                                 PixelMaterial skin, PixelMaterial belly, PixelMaterial skinDark, PixelMaterial mouthIn,
                                 Escamas escamas, bool veterano, float eyeGrow)
        {
            int gHead = c.NewGroup(), gJaw = c.NewGroup(), gMouth = c.NewGroup(), gEye = c.NewGroup(), gFar = c.NewGroup(), gGill = c.NewGroup(), gTeeth = c.NewGroup();
            Vector2 H(float x, float y) => Add(hp, Rotate(V(x * k, y * k), V(0f, 0f), hr));
            float jawA = -jaw * 30f;
            var hinge = V(0.5f, -1.8f);
            Vector2 J(float x, float y)
            {
                var l = Add(hinge, Rotate(V(x, y), V(0f, 0f), jawA));
                return Add(hp, Rotate(V(l.x * k, l.y * k), V(0f, 0f), hr));
            }
            bool hot = eye > 0.45f, dull = eye < -0.25f;

            // Ojo lejano: asoma por encima del cráneo
            var eyeFar = H(5.4f, 8.2f);
            c.Ellipse(eyeFar, 3f * k, 2.8f * k, hr, skinDark, 7.4f, -0.1f, gFar);
            c.Ellipse(Add(eyeFar, V(0.5f, 0.6f)), 1.8f * k, 1.6f * k, 0f, dull ? EyeDull : hot ? EyeMid : EyeRing, 7.45f, 0f, gFar);

            // Fauces: interior oscuro y mandíbula inferior pálida (prognata) con pliegues de la garganta y labio claro
            c.Ellipse(H(7f, -2.6f), 5.6f * k, (1.2f + jaw * 3.2f) * k, hr - jaw * 14f, mouthIn, 7.8f, 0f, gMouth);
            float d1 = veterano ? -2.8f : -2.6f, d2 = veterano ? -4.8f : -4.4f, d3 = veterano ? -4.5f : -4.1f;
            Relieve(c.Poly(new[] { J(-1.5f, 1.2f), J(6f, 0.8f), J(12.4f, 0.6f), J(13.8f, -0.6f), J(12.6f, d1), J(6.5f, d2), J(0f, d3), J(-2.6f, -2f) },
                           belly, 7.9f, 2f, 0f, gJaw), JawFolds, hp, hr + jawA + 90f, 0.8f);
            c.Ridge(new[] { J(-0.4f, 0.6f), J(6f, 0.2f), J(12.2f, 0f) }, 1, gJaw);

            // Cráneo y hocico (escamitas arriba, piel lisa con verrugas en la mejilla)
            Relieve(c.Ellipse(H(4.2f, 2.6f), 7.6f * k, 6.1f * k, hr - 8f, skin, 8f, 0f, gHead), escamas, hp, hr);
            Relieve(c.Ellipse(H(10.2f, 0.6f), 4.6f * k, 3.3f * k, hr - 6f, skin, 8.02f, 0f, gHead), escamas, hp, hr);
            // Narina y poros sensoriales en hilera bajo el ojo
            c.Dot(H(12.7f, 1.5f), -2, gHead);
            for (int i = 0; i < 4; i++) c.Dot(H(2.2f + i * 1.8f, 0.9f - (i == 1 || i == 2 ? 0.3f : 0f)), -2, gHead);
            // Comisura: línea de la boca, ancha y caída, con el labio de arriba iluminado
            if (jaw < 0.3f)
            {
                c.Strand(Bezier(H(1f, -2.6f), H(6f, -1.2f), H(13.2f, -0.9f), 5), 0.55f, 0.45f, mouthIn, 8.05f, 0f, gMouth);
                c.Ridge(Bezier(H(3f, -1.1f), H(7f, -0.2f), H(12.4f, 0f), 4), 1, gHead);
            }
            else
                c.Capsule(H(0.6f, -2.8f), H(2.4f, -2f), 0.6f, 0.5f, mouthIn, 8.05f, 0f, gMouth);

            Branquias(c, H, gill, 8.15f, gGill, gHead);
            Dientes(c, H, J, jaw, 8.06f, gTeeth);

            // Ojo saltón
            var E = H(8f, 5.6f);
            float er = 2.9f * k + eyeGrow * Mathf.Max(0f, eye);
            Ojo(c, E, er, hr, eye, skinDark, 8.2f, gEye, veterano ? 0.5f : 0.4f);
            return gHead;
        }
    }
}
