using System.Collections.Generic;
using UnityEngine;
using static Abismo.EditorTools.Rig;

namespace Abismo.EditorTools
{
    /// <summary>
    /// El Arrepentido: penitente esquelético de la hoja conceptual. Piel curtida color cobre pegada al hueso
    /// (costillas, vértebras y articulaciones a la vista), harapos y vendas oscuras, una corona penitencial de clavos
    /// y astas en la cabeza y una lanza larga que sostiene en horizontal con las dos manos. Ojos vacíos con un punto
    /// de brasa. Mira a la derecha; el juego lo voltea al girar.
    ///
    /// Convenciones de pose:
    ///  - hipX/hipY: cadera · lean: inclinación del tronco (grados desde la vertical, + = hacia delante)
    ///  - neck/head: giro del cuello y de la cabeza (+ = hacia arriba/atrás) · jaw 0..1 mandíbula
    ///  - piernas por IK: fFx/fBx = x del tobillo, fFl/fBl = altura sobre el suelo, fFa/fBa = cabeceo del pie
    ///    (+ = talón levantado, punta abajo)
    ///  - lanza en las manos: sX/sY = puño delantero, spear = ángulo (matemático, hacia la punta), gap = distancia
    ///    entre los dos puños a lo largo del asta
    ///  - spFree = 1: la lanza ya no está en las manos (muerte): dX/dY/dA = puño de la lanza suelta y su ángulo;
    ///    hFx/hFy, hBx/hBy = manos libres respecto a cada hombro; elbFUp/elbBUp = 1 dobla ese codo hacia arriba
    ///  - eye: -1 apagado, 0 brasa tenue, 1 telegrafiando (brilla y crece) · rag: vaivén de harapos (+ = hacia atrás)
    ///  - smoke: hilo de humo de la brasa al morir (0 = nada, sube con el valor)
    ///
    /// Medidas que usa el juego: alto ≈ 63 px hasta el cráneo (≈ 80 con las astas de la corona); en la estocada
    /// (ataque, fotogramas 1–2) la punta de la lanza está en <see cref="SpearReachX"/>/<see cref="SpearReachY"/>
    /// = (+88, 34) px desde el pivote.
    /// </summary>
    public sealed class ArrepentidoArt : CharacterArt
    {
        public override string Id => "arrepentido";
        public override int FrameWidth => 192;
        public override int FrameHeight => 112;
        public override Vector2 Pivot => new Vector2(80f, 8f);

        // ------------------------------------------------------------------
        // Materiales
        // ------------------------------------------------------------------

        /// <summary>Piel curtida color cobre/óxido: manchas de suciedad en grupos que viajan con cada pieza.</summary>
        static readonly PixelMaterial Skin = new PixelMaterial(Ramp.Make("88604a", 5, 0.12f, 0.3f, 1.58f))
        {
            Ambient = 0.24f, Rim = 0.62f, Dither = 0f, BandDither = 0.14f,
            Detail = Patterns.Mottle(2.2f, 0.78f, -1, 313),
        };
        /// <summary>Hueso que asoma: rótulas, codos, vértebras, nudillos, pómulos.</summary>
        static readonly PixelMaterial Bone = new PixelMaterial(Ramp.Make("bfa486", 4, 0.1f, 0.36f, 1.28f)) { Ambient = 0.3f, Rim = 0.5f, Dither = 0f };
        static readonly PixelMaterial Teeth = new PixelMaterial(Ramp.Make("cdb894", 3, 0.08f, 0.45f, 1.2f)) { Ambient = 0.4f, Rim = 0.2f, Dither = 0f };
        /// <summary>Cuencas y fauces: hondo, frío, casi negro (nunca negro puro).</summary>
        static readonly PixelMaterial Hollow = new PixelMaterial(Ramp.Make("2a1a1c", 3, 0.05f, 0.6f, 1.5f)) { Ambient = 0.5f, Rim = 0f, Outline = false, Dither = 0f };
        static readonly PixelMaterial Linen = new PixelMaterial(Ramp.Make("5d5346", 4, 0.08f, 0.36f, 1.5f)) { Ambient = 0.26f, Rim = 0.5f, Dither = 0f };
        static readonly PixelMaterial Rag = new PixelMaterial(Ramp.Make("3a3230", 4, 0.1f, 0.38f, 1.65f))
        {
            Ambient = 0.22f, Rim = 0.6f, Dither = 0f, Bump = Patterns.Weave(3f, 0.5f), BumpStrength = 0.4f,
        };
        static readonly PixelMaterial Bandage = new PixelMaterial(Ramp.Make("4c4038", 4, 0.1f, 0.38f, 1.6f)) { Ambient = 0.24f, Rim = 0.55f, Dither = 0f };
        static readonly PixelMaterial RagDark = new PixelMaterial(Ramp.Make("2b2529", 4, 0.1f, 0.4f, 1.6f)) { Ambient = 0.22f, Rim = 0.55f, Dither = 0f };
        static readonly PixelMaterial Rope = new PixelMaterial(Ramp.Make("6e5a40", 4, 0.08f, 0.36f, 1.4f)) { Ambient = 0.3f, Dither = 0f };
        static readonly PixelMaterial Wood = new PixelMaterial(Ramp.Make("4f3828", 4, 0.08f, 0.36f, 1.62f))
        {
            Ambient = 0.26f, Rim = 0.5f, Dither = 0f,
            Detail = (u, v) => PixelCanvas.ValueNoise(u / 4f, v * 2f, 0, 141) > 0.76f ? -1 : 0,
        };
        static readonly PixelMaterial Iron = new PixelMaterial(Ramp.Make("6a6f72", 5, 0.08f, 0.32f, 1.45f))
        {
            Gloss = 0.45f, Ambient = 0.3f, Rim = 0.45f, Dither = 0f, Specular = PixelCanvas.Hex("f4f6f0"),
            Detail = (u, v) => PixelCanvas.ValueNoise(u / 2.5f, v / 2f, 0, 77) > 0.74f ? -1 : 0,
        };
        static readonly PixelMaterial IronDark = new PixelMaterial(Ramp.Make("4b4644", 4, 0.08f, 0.34f, 1.5f))
        {
            Gloss = 0.25f, Ambient = 0.28f, Rim = 0.5f, Dither = 0f,
            Detail = (u, v) => PixelCanvas.ValueNoise(u / 2f, v / 2f, 0, 91) > 0.72f ? -1 : 0,
        };
        /// <summary>Estacas de la corona: madera seca, blanquecina como el hueso.</summary>
        static readonly PixelMaterial Stake = new PixelMaterial(Ramp.Make("8e7a64", 4, 0.08f, 0.36f, 1.42f)) { Ambient = 0.3f, Rim = 0.7f, Dither = 0f };
        static readonly Color32 RustTint = PixelCanvas.Hex("8a4426");
        static readonly PixelMaterial Ribbon = new PixelMaterial(Ramp.Make("6c2427", 4, 0.1f, 0.38f, 1.55f)) { Ambient = 0.26f, Rim = 0.6f, Dither = 0f };

        // Brasa de los ojos (emisivo): tenue, normal y al telegrafiar.
        static readonly PixelMaterial EmberDim = PixelMaterial.Glow("6a2410");
        static readonly PixelMaterial EmberMid = PixelMaterial.Glow("d0581c");
        static readonly PixelMaterial EmberCore = PixelMaterial.Glow("ffb45a");

        static readonly PixelMaterial Smoke = new PixelMaterial(Ramp.Make("6a6260", 3, 0.06f, 0.6f, 1.25f)) { Ambient = 0.5f, Rim = 0.3f, Outline = false, ReceivesContactShadow = false, Dither = 0f };
        static readonly PixelMaterial SmokeFaint = new PixelMaterial(Ramp.Make("4a4446", 3, 0.06f, 0.65f, 1.25f)) { Ambient = 0.5f, Rim = 0.3f, Outline = false, ReceivesContactShadow = false, Dither = 0f };
        static readonly PixelMaterial SmearCore = new PixelMaterial(new[] { new Color32(252, 240, 222, 245) }) { Emissive = true, Outline = false, ReceivesContactShadow = false };
        static readonly PixelMaterial SmearMid = new PixelMaterial(new[] { new Color32(214, 196, 176, 200) }) { Emissive = true, Outline = false, ReceivesContactShadow = false };
        static readonly PixelMaterial SmearSoft = new PixelMaterial(new[] { new Color32(140, 120, 108, 140) }) { Emissive = true, Outline = false, ReceivesContactShadow = false };

        // ------------------------------------------------------------------
        // Medidas
        // ------------------------------------------------------------------

        const float Thigh = 17f, Shin = 16.8f, UpperArm = 14f, Forearm = 13.5f;
        /// <summary>La lanza, a lo largo del asta desde el puño delantero: punta y regatón.</summary>
        const float SpearTip = 50f, SpearButt = 32f;

        // ------------------------------------------------------------------
        // Poses base
        // ------------------------------------------------------------------

        static Pose Idle => new Pose().With(
            ("hipX", -2f), ("hipY", 32f), ("lean", 25f), ("neck", 0f), ("head", 0f), ("jaw", 0.1f),
            ("fFx", 10f), ("fFl", 0f), ("fFa", 0f),
            ("fBx", -11f), ("fBl", 0f), ("fBa", 0f),
            ("sX", 19f), ("sY", 28.5f), ("spear", 0f), ("gap", 17f),
            ("eye", 0f), ("rag", 0f));

        public override List<AnimSpec> Animations() => new List<AnimSpec>
        {
            new AnimSpec("idle", 6, 8f, true, f => DrawIdle(f)),
            new AnimSpec("walk", 8, 9f, true, f => DrawWalk(f)),
            new AnimSpec("run", 8, 12f, true, f => DrawRun(f)),
            new AnimSpec("windup", 6, 10f, false, f => DrawWindup(f)),
            new AnimSpec("attack", 4, 20f, false, f => DrawAttack(f)),
            new AnimSpec("recover", 5, 9f, false, f => DrawRecover(f)),
            new AnimSpec("hurt", 3, 12f, false, f => DrawHurt(f)),
            new AnimSpec("stagger", 6, 8f, true, f => DrawStagger(f)),
            new AnimSpec("death", 10, 14f, false, f => DrawDeath(f)),
        };

        // ------------------------------------------------------------------
        // Animaciones
        // ------------------------------------------------------------------

        // Respira hundiendo el pecho; la lanza sube y baja con un leve retraso; la brasa titila.
        ShadedCanvas DrawIdle(int f)
        {
            float t = f / 6f * Mathf.PI * 2f;
            var p = Idle.With(
                ("hipY", 32f - 0.4f + Mathf.Cos(t) * 0.4f), ("lean", 25f + Mathf.Sin(t) * 1.2f),
                ("neck", Mathf.Sin(t - 0.7f) * 2f), ("head", Mathf.Sin(t - 1.2f) * 2.5f), ("jaw", 0.1f + 0.1f * Mathf.Sin(t + 0.4f)),
                ("sY", 28.5f + Mathf.Cos(t - 0.8f) * 0.6f), ("sX", 19f + Mathf.Sin(t - 0.8f) * 0.4f), ("spear", Mathf.Sin(t - 1.3f) * 1.2f),
                ("rag", Mathf.Sin(t - 1f) * 0.35f), ("eye", f == 3 ? -0.2f : 0f));
            return Draw(p);
        }

        /// <summary>
        /// Ciclo de una pierna: apoyo (el pie retrocede pegado al suelo y al final despega el talón) y vuelo
        /// (avanza levantándose con la punta hacia arriba).
        /// </summary>
        static void Step(Pose p, string leg, float phase, float center, float stride, float lift, float stance = 0.55f)
        {
            float u = phase - Mathf.Floor(phase);
            float x, l, a;
            if (u < stance)
            {
                float w = u / stance;
                x = center + stride * (1f - 2f * w);
                a = w > 0.65f ? 34f * (w - 0.65f) / 0.35f : 0f;
                l = 0f;
            }
            else
            {
                float w = (u - stance) / (1f - stance);
                float s = w * w * (3f - 2f * w);
                x = center - stride + 2f * stride * s;
                a = Mathf.Lerp(34f, -12f, Mathf.Clamp01(w * 1.4f));
                l = Mathf.Sin(w * Mathf.PI) * lift;
            }
            p["f" + leg + "x"] = x;
            p["f" + leg + "l"] = l;
            p["f" + leg + "a"] = a;
        }

        // Paso lento de penitente: arrastra los pies, el tronco cabecea y la lanza se balancea a contratiempo.
        ShadedCanvas DrawWalk(int f)
        {
            float ph = f / 8f;
            float t = ph * Mathf.PI * 2f;
            var p = Idle.With(("lean", 27f), ("hipX", -1f));
            Step(p, "F", ph, 1f, 9f, 3.5f);
            Step(p, "B", ph + 0.5f, -1.5f, 9f, 3.5f);
            p["hipY"] = 31.3f + Mathf.Cos(t * 2f) * 0.9f;
            p["lean"] = 27f + Mathf.Cos(t * 2f + 0.6f) * 1.2f;
            p["neck"] = Mathf.Sin(t * 2f - 0.5f) * 2f;
            p["head"] = Mathf.Sin(t * 2f - 1.1f) * 3f;
            p["sX"] = 20f + Mathf.Sin(t) * 1f;
            p["sY"] = 28.5f + Mathf.Cos(t * 2f - 0.9f) * 0.9f;
            p["spear"] = Mathf.Sin(t - 0.5f) * 2f;
            p["rag"] = 0.25f + Mathf.Sin(t * 2f - 1f) * 0.45f;
            p["jaw"] = 0.15f;
            return Draw(p);
        }

        // Carga: tronco muy inclinado, la lanza por delante a la altura del pecho, zancadas largas.
        ShadedCanvas DrawRun(int f)
        {
            float ph = f / 8f;
            float t = ph * Mathf.PI * 2f;
            var p = Idle.With(("lean", 36f), ("hipX", 1f), ("gap", 15f));
            Step(p, "F", ph, 3f, 13f, 6f, 0.45f);
            Step(p, "B", ph + 0.5f, 0f, 13f, 6f, 0.45f);
            p["hipY"] = 30.6f + Mathf.Cos(t * 2f) * 1.3f;
            p["lean"] = 36f + Mathf.Cos(t * 2f + 0.5f) * 1.5f;
            p["neck"] = 6f + Mathf.Sin(t * 2f - 0.5f) * 2f;
            p["head"] = 4f + Mathf.Sin(t * 2f - 1f) * 2f;
            p["sX"] = 29f + Mathf.Sin(t) * 1f;
            p["sY"] = 33.5f + Mathf.Cos(t * 2f - 0.8f) * 1f;
            p["spear"] = 1f + Mathf.Sin(t * 2f - 0.6f) * 1.5f;
            p["rag"] = 1f + Mathf.Sin(t * 2f) * 0.3f;
            p["jaw"] = 0.4f;
            p["eye"] = 0.2f;
            return Draw(p);
        }

        static Pose Coil => Idle.With(("hipX", -6f), ("hipY", 31f), ("lean", 13f), ("neck", 4f), ("head", 3f), ("jaw", 0.3f),
                                       ("fFx", 12f), ("fBx", -14f),
                                       ("sX", 10f), ("sY", 32f), ("spear", -2f), ("gap", 15f), ("rag", -0.4f), ("eye", 0.5f));

        static Pose Cocked => Coil.With(("hipX", -10f), ("hipY", 29.5f), ("lean", 3f), ("neck", 8f), ("head", 6f), ("jaw", 0.6f),
                                         ("fFa", -10f), ("fBx", -16f),
                                         ("sX", 4f), ("sY", 33.5f), ("spear", -3f), ("gap", 14f), ("rag", -0.6f), ("eye", 1f));

        // Telegrafiado: recoge la lanza hacia atrás a la altura del pecho, carga el peso atrás; la brasa se aviva.
        ShadedCanvas DrawWindup(int f)
        {
            var keys = new Keyframes()
                .Key(0, Pose.Lerp(Idle, Coil, 0.35f), Ease.InOut)
                .Key(2, Coil, Ease.InOut)
                .Key(4, Cocked, Ease.Out)
                .Key(5, Cocked.With(("hipX", -10.5f), ("sX", 3f), ("sY", 33.8f), ("lean", 2f), ("head", 7f)));
            var p = keys.Evaluate(f);
            p["eye"] = f >= 4 ? 1f : Mathf.Lerp(0.2f, 0.7f, f / 3f);
            return Draw(p);
        }

        static Pose Lunge => Idle.With(("hipX", 1f), ("hipY", 29.5f), ("lean", 36f), ("neck", 4f), ("head", 2f), ("jaw", 0.8f),
                                        ("fFx", 22f), ("fFa", 0f), ("fBx", -15f), ("fBa", 26f),
                                        ("sX", 28f), ("sY", 34.5f), ("spear", 0f), ("gap", 14f), ("rag", -1f), ("eye", 0.8f));

        static Pose Thrust => Lunge.With(("hipX", 3f), ("hipY", 29f), ("lean", 42f), ("neck", 2f), ("head", 0f),
                                          ("sX", SpearReachX - SpearTip), ("sY", SpearReachY), ("spear", 0f), ("gap", 12f), ("rag", -1.2f), ("eye", 0.6f));

        /// <summary>Punta de la lanza en la estocada (fotogramas 1–2 del ataque): lo que espera la caja de golpe del juego.</summary>
        const float SpearReachX = 88f, SpearReachY = 34f;

        // Estocada larga: la punta llega a (+88, 34) en los fotogramas 1-2; estela en los dos primeros.
        ShadedCanvas DrawAttack(int f)
        {
            Pose p;
            var thrust = default(ThrustSpec);
            switch (f)
            {
                case 0:
                    p = Lunge;
                    thrust = new ThrustSpec { On = true, Back = 30f, Strength = 1f };
                    break;
                case 1:
                    p = Thrust;
                    thrust = new ThrustSpec { On = true, Back = 24f, Strength = 0.75f };
                    break;
                case 2:
                    p = Thrust.With(("hipY", 29.2f), ("lean", 41f), ("rag", -0.8f), ("jaw", 0.6f), ("eye", 0.4f));
                    break;
                default:
                    p = Pose.Lerp(Thrust, Lunge, 0.55f).With(("rag", -0.3f), ("eye", 0.2f));
                    break;
            }
            var c = Draw(p, thrust);
            if (f == 0) Dust(c, -19f, -13f, 0f, 3, 3);
            if (f == 1) Dust(c, -21f, -12f, 0.6f, 2, 5);
            return c;
        }

        ShadedCanvas DrawRecover(int f)
        {
            var pull = Lunge.With(("hipX", -1f), ("hipY", 31f), ("lean", 28f), ("sX", 19f), ("sY", 30.5f), ("spear", -6f), ("gap", 16f),
                                  ("jaw", 0.4f), ("eye", 0f), ("rag", 0.4f), ("fFx", 16f));
            var stepBack = Pose.Lerp(pull, Idle, 0.6f).With(("fFl", 2.5f), ("fFa", -6f));
            var keys = new Keyframes()
                .Key(0, Pose.Lerp(Thrust, Lunge, 0.8f).With(("eye", 0f)), Ease.Out)
                .Key(1.5f, pull)
                .Key(3f, stepBack)
                .Key(4, Idle);
            return Draw(keys.Evaluate(f));
        }

        static Pose Hit => Idle.With(("hipX", -5f), ("hipY", 32.5f), ("lean", 8f), ("neck", 16f), ("head", 18f), ("jaw", 0.9f),
                                      ("sX", 14f), ("sY", 32f), ("spear", 12f), ("gap", 16f), ("rag", 1f), ("eye", -0.3f),
                                      ("fFx", 11f), ("fFa", -8f), ("fBx", -13f));

        ShadedCanvas DrawHurt(int f)
        {
            var keys = new Keyframes().Key(0, Hit, Ease.Out).Key(1, Pose.Lerp(Hit, Idle, 0.4f)).Key(2, Pose.Lerp(Hit, Idle, 0.75f));
            return Draw(keys.Evaluate(f));
        }

        // Aturdido: se apoya en la lanza con la punta clavada delante, la cabeza le cuelga y oscila; la brasa se apaga y vuelve.
        ShadedCanvas DrawStagger(int f)
        {
            float t = f / 6f * Mathf.PI * 2f;
            var p = Idle.With(
                ("hipX", -3f + Mathf.Sin(t) * 1.2f), ("hipY", 31f + Mathf.Cos(t * 2f) * 0.5f), ("lean", 30f + Mathf.Sin(t) * 5f),
                ("neck", -14f + Mathf.Sin(t + 1f) * 6f), ("head", -12f + Mathf.Sin(t + 1.8f) * 10f), ("jaw", 0.6f + 0.3f * Mathf.Sin(t * 2f)),
                ("fFx", 9f), ("fBx", -12f),
                ("sX", 15f + Mathf.Sin(t + 0.5f) * 1.2f), ("sY", 25.5f + Mathf.Sin(t + 0.9f) * 0.8f), ("spear", -27f + Mathf.Sin(t + 1.2f) * 3f), ("gap", 14f),
                ("rag", Mathf.Sin(t + 1f) * 0.6f), ("eye", f == 2 || f == 3 ? -1f : -0.6f));
            return Draw(p);
        }

        // Muerte: retrocede, la lanza se le escapa de las manos (cae de punta y luego de plano), cae de rodillas y se
        // desploma de bruces. La brasa se apaga.
        ShadedCanvas DrawDeath(int f)
        {
            var hit = Hit.With(("lean", 6f), ("head", 26f), ("neck", 20f), ("jaw", 1f), ("hipX", -6f));
            var reel = hit.With(("spFree", 1f), ("lean", 2f), ("hipX", -7f), ("hipY", 30f), ("head", 20f), ("eye", -0.4f),
                                ("hFx", 6f), ("hFy", -14f), ("hBx", -2f), ("hBy", -16f));
            var buckle = Idle.With(("spFree", 1f), ("hipX", -5f), ("hipY", 22f), ("lean", 16f), ("neck", -10f), ("head", -14f), ("jaw", 0.7f),
                                   ("fFx", 7f), ("fBx", -14f), ("fBa", 40f),
                                   ("hFx", 3f), ("hFy", -19f), ("hBx", -2f), ("hBy", -19f), ("eye", -0.6f), ("rag", 0.4f));
            var kneel = buckle.With(("hipX", -6f), ("hipY", 17f), ("lean", 22f), ("neck", -18f), ("head", -20f),
                                    ("fFx", -17f), ("fFa", 160f), ("fBx", -20f), ("fBa", 160f),
                                    ("hFx", 2f), ("hFy", -21f), ("hBx", -4f), ("hBy", -20f), ("rag", 0.2f));
            var topple = kneel.With(("hipY", 16f), ("lean", 60f), ("neck", -8f), ("head", -14f),
                                    ("hFx", 12f), ("hFy", -17f), ("hBx", 8f), ("hBy", -19f), ("rag", -0.4f), ("eye", -0.7f));
            var prone = topple.With(("hipX", -14f), ("hipY", 8f), ("lean", 88f), ("neck", 40f), ("head", -60f), ("jaw", 0.5f),
                                    ("fFx", -50f), ("fFa", 175f), ("fBx", -51f), ("fBa", 175f),
                                    ("hFx", -24f), ("hFy", -4.5f), ("hBx", 22f), ("hBy", -5f), ("elbBUp", 1f), ("rag", 0f), ("eye", -0.8f));
            var keys = new Keyframes()
                .Key(0, hit, Ease.Out)
                .Key(1, reel)
                .Key(3, buckle, Ease.In)
                .Key(4, kneel, Ease.Out)
                .Key(5, topple, Ease.In)
                .Key(6, prone, Ease.Out)
                .Key(7, prone.With(("hipY", 8.6f), ("lean", 87f), ("head", -56f)), Ease.In)
                .Key(8, prone);
            var p = keys.Evaluate(f);
            p["eye"] = f >= 9 ? -1f : f >= 7 ? -0.9f : p["eye"];
            p["smoke"] = f >= 7 ? f - 6f : 0f;

            // La lanza: se escapa y gira (f1), cae de punta (f2), bascula sobre la punta (f3) y queda tendida (f4+).
            var spearKeys = new Keyframes()
                .Key(1, new Pose().With(("dX", 8f), ("dY", 31f), ("dA", 20f)), Ease.In)
                .Key(2, new Pose().With(("dX", 17f), ("dY", 18.8f), ("dA", -20f)), Ease.In)
                .Key(3, new Pose().With(("dX", 14.4f), ("dY", 8f), ("dA", -7f)), Ease.In)
                .Key(4, new Pose().With(("dX", 14f), ("dY", 3.6f), ("dA", -1.5f)))
                .Key(5, new Pose().With(("dX", 14f), ("dY", 2.7f), ("dA", 0f)));
            if (f >= 1)
            {
                var s = spearKeys.Evaluate(f);
                foreach (var k in s.Keys) p[k] = s[k];
            }
            var c = Draw(p);
            // Polvo al golpear el suelo: rodillas (f4) y el cuerpo entero (f6-7).
            if (f == 4) Dust(c, -14f, 2f, 0f, 4, 11);
            if (f == 6) Dust(c, -40f, 30f, 0f, 6, 13);
            if (f == 7) Dust(c, -40f, 30f, 0.7f, 6, 13);
            return c;
        }

        // ------------------------------------------------------------------
        // Dibujo del personaje a partir de una pose
        // ------------------------------------------------------------------

        struct ThrustSpec
        {
            public bool On;
            public float Back, Strength;
        }

        /// <summary>
        /// Cinemática inversa de dos huesos (como en el Guardián): codo/rodilla para que el extremo llegue a
        /// <paramref name="end"/> (si no alcanza, lo acerca). bend = +1 dobla hacia delante (rodillas), −1 hacia atrás (codos).
        /// </summary>
        static Vector2 Knee(Vector2 root, ref Vector2 end, float l1, float l2, float bend)
        {
            float dx = end.x - root.x, dy = end.y - root.y;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float max = l1 + l2 - 0.05f;
            if (d > max)
            {
                end = V(root.x + dx / d * max, root.y + dy / d * max);
                dx = end.x - root.x;
                dy = end.y - root.y;
                d = max;
            }
            d = Mathf.Max(d, 0.5f);
            float a = (l1 * l1 - l2 * l2 + d * d) / (2f * d);
            float h = Mathf.Sqrt(Mathf.Max(0f, l1 * l1 - a * a));
            float ux = dx / d, uy = dy / d;
            return V(root.x + ux * a - uy * h * bend, root.y + uy * a + ux * h * bend);
        }

        /// <summary>Contorno del pie (marco local: x hacia la punta, y hacia arriba, origen en el tobillo).</summary>
        static readonly Vector2[] FootHull =
        {
            new Vector2(-2.6f, -3.2f), new Vector2(0f, -3.4f), new Vector2(4.6f, -3.3f), new Vector2(7.8f, -3.0f), new Vector2(8.4f, -2.2f),
            new Vector2(7.6f, -1.4f), new Vector2(4.8f, -0.6f), new Vector2(1.6f, 1.3f), new Vector2(-0.8f, 1.3f), new Vector2(-3f, -1.6f),
        };

        /// <summary>Altura del tobillo para que el pie con ese cabeceo apoye justo en el suelo.</summary>
        static float FootLift(float pitch)
        {
            float min = float.MaxValue;
            foreach (var s in FootHull) min = Mathf.Min(min, Rotate(s, V(0f, 0f), -pitch).y);
            return 0.15f - min;
        }

        ShadedCanvas Draw(Pose p, ThrustSpec thrust = default)
        {
            var c = NewFrame();
            float lean = p["lean"];
            float ta = 90f - lean;
            var hip = V(p["hipX"], p["hipY"]);
            var U = Dir(ta);
            var F = Dir(ta - 90f);
            Vector2 T(float u, float v) => Add(hip, Add(Scale(U, u), Scale(F, v)));
            float rag = p["rag"];

            int gTorso = c.NewGroup(), gNeck = c.NewGroup(), gLegF = c.NewGroup(), gLegB = c.NewGroup();
            int gArmF = c.NewGroup(), gArmB = c.NewGroup(), gSpear = c.NewGroup(), gBelt = c.NewGroup();
            int gRagF = c.NewGroup(), gRagB = c.NewGroup(), gHandF = c.NewGroup(), gHandB = c.NewGroup();

            // --- Piernas (IK, rodillas hacia delante); de rodillas, la espinilla se tiende en el suelo ---
            // Si una rodilla se hundiría en el suelo (al arrodillarse o caer), sube la cadera lo justo.
            Vector2 hipF = hip, hipB = hip, ankleF = hip, ankleB = hip, kneeF = hip, kneeB = hip;
            for (int it = 0; it < 4; it++)
            {
                hipF = T(0f, 1.2f);
                hipB = T(0.6f, -1.8f);
                ankleF = V(p["fFx"], FootLift(p["fFa"]) + p["fFl"]);
                ankleB = V(p["fBx"], FootLift(p["fBa"]) + p["fBl"]);
                kneeF = Knee(hipF, ref ankleF, Thigh, Shin, 1f);
                kneeB = Knee(hipB, ref ankleB, Thigh, Shin, 1f);
                float low = Mathf.Min(kneeF.y, kneeB.y);
                if (low >= 2.2f) break;
                hip = V(hip.x, hip.y + 2.2f - low);
            }

            // --- Hombros, manos (en el asta) y brazos (IK, codos hacia atrás) ---
            var shoulderF = T(19.6f, -0.2f);
            var shoulderB = T(19.9f, -2.6f);
            bool held = p["spFree"] < 0.5f;
            float spear = held ? p["spear"] : p["dA"];
            Vector2 handF, handB, grip;
            if (held)
            {
                handF = V(p["sX"], p["sY"]);
                handB = Sub(handF, Dir(spear, p["gap"]));
            }
            else
            {
                handF = Floor(Add(shoulderF, V(p["hFx"], p["hFy"])), 3.2f);
                handB = Floor(Add(shoulderB, V(p["hBx"], p["hBy"])), 3.2f);
            }
            var elbowF = Knee(shoulderF, ref handF, UpperArm, Forearm, p["elbFUp"] > 0.5f ? 1f : -1f);
            var elbowB = Knee(shoulderB, ref handB, UpperArm, Forearm, p["elbBUp"] > 0.5f ? 1f : -1f);
            // La lanza se dibuja desde el puño de la pose (no desde la mano tras el IK): así la punta queda siempre donde
            // dice la pose (p. ej. en la estocada, donde la espera la caja de golpe del juego).
            grip = held ? V(p["sX"], p["sY"]) : V(p["dX"], p["dY"]);

            // --- Cuello y cabeza ---
            var neckBase = T(20.6f, 2.8f);
            float neckA = ta - 22f + p["neck"];
            var neckTop = Add(neckBase, Dir(neckA, 5.6f));
            float hr = -(lean - 25f) * 0.55f + p["head"] + p["neck"] * 0.4f;
            var head = Add(neckTop, Rotate(V(1.4f, 4.6f), V(0f, 0f), hr));

            // --- Harapo trasero (detrás de todo) ---
            DrawRag(c, T(4.2f, -6.2f), T(4.2f, -1.2f), 16f, rag * 1.3f, lean, 0.6f, -0.1f, gRagB, 29, RagDark);

            // --- Pierna y brazo traseros (lejos de la luz) ---
            DrawLeg(c, hipB, kneeB, ankleB, p["fBa"], 1f, -0.14f, gLegB, false);
            DrawArm(c, shoulderB, elbowB, handB, 2f, -0.13f, gArmB, false);

            // --- Tronco: pelvis, cintura hundida, costillar, joroba, vértebras ---
            DrawTorso(c, T, ta, gTorso, rag);

            // --- Cuello: tendones y vértebras ---
            DrawNeck(c, neckBase, neckTop, neckA, gNeck);
            if (lean < 70f) DrawScapular(c, T(20.6f, 4.6f), T(21.4f, 0.6f), rag, c.NewGroup());

            // --- Pierna delantera y harapo delantero ---
            DrawLeg(c, hipF, kneeF, ankleF, p["fFa"], 6f, 0f, gLegF, true);
            DrawRag(c, T(4.4f, 0.6f), T(4.4f, 5.8f), 13f, rag, lean, 6.5f, 0f, gRagF, 7, Rag);
            DrawBelt(c, T, ta, gBelt, rag);

            // --- Cabeza con la corona de clavos ---
            DrawHead(c, head, hr, p);

            // --- Mano trasera (tras el asta), lanza, brazo y mano delanteros ---
            if (held) DrawGrip(c, handB, spear, 7.6f, -0.08f, gHandB, false);
            else DrawLooseHand(c, elbowB, handB, 2.1f, -0.13f, gHandB);
            DrawSpear(c, grip, spear, held ? 8f : 0.4f, gSpear, rag);
            DrawArm(c, shoulderF, elbowF, handF, 9f, 0f, gArmF, true);
            if (held) DrawGrip(c, handF, spear, 9.3f, 0f, gHandF, true);
            else DrawLooseHand(c, elbowF, handF, 9.1f, 0f, gHandF);

            // --- Estela de la estocada ---
            if (thrust.On)
            {
                var tip = Add(grip, Dir(spear, SpearTip));
                DrawThrustSmear(c, tip, spear, thrust);
            }
            return c;
        }

        // ------------------------------------------------------------------
        // Cuerpo
        // ------------------------------------------------------------------

        /// <summary>
        /// Tronco esquelético: pelvis con la cresta ilíaca marcada, cintura hundida y fajada con vendas oscuras, costillar
        /// con las costillas en relieve (piezas propias: la sombra de contacto oscurece los huecos entre ellas), esternón,
        /// clavícula, joroba con el omóplato y una hilera de vértebras que asoma por la espalda.
        /// </summary>
        void DrawTorso(ShadedCanvas c, System.Func<float, float, Vector2> T, float ta, int g, float rag)
        {
            // Pelvis y cadera.
            c.Ellipse(T(1.6f, -0.2f), 4.6f, 6f, ta, Skin, 4f, 0f, g);
            // Cintura hundida (más estrecha y en sombra).
            c.Capsule(T(3.5f, -1f), T(10.5f, -0.6f), 4.2f, 3.9f, Skin, 3.95f, -0.06f, g);
            // Costillar.
            c.Ellipse(T(15f, 0.4f), 7.4f, 6.4f, ta, Skin, 4f, -0.03f, g);
            // Joroba (parte alta de la espalda) y hombros.
            c.Ellipse(T(18.6f, -2.2f), 5.4f, 4.6f, ta, Skin, 4f, 0f, g);
            c.Ellipse(T(20.2f, 0.8f), 3.6f, 5.4f, ta, Skin, 4.02f, 0f, g);

            // Costillas: arcos que bajan desde la espalda hacia el esternón, cada vez más cortos hacia abajo.
            int gRib = c.NewGroup();
            for (int k = 0; k < 5; k++)
            {
                float u = 10.6f + k * 2.3f;
                var a = T(u + 0.9f, -4.8f + k * 0.1f);
                var m = T(u - 0.8f, 1f);
                var b = T(u - 1.5f + k * 0.25f, (k == 0 ? 4.2f : 5.6f) - k * 0.1f);
                c.Strand(Bezier(a, m, b, 5), 0.72f, 0.55f, Skin, 4.1f, 0.1f, gRib);
            }
            // Esternón, clavícula y reborde costal (el arco bajo las costillas donde se hunde el vientre).
            c.Ridge(new[] { T(20.2f, 5.6f), T(12.8f, 6.2f) }, 1, gRib, Skin);
            c.Ridge(new[] { T(21.6f, 4.4f), T(20.4f, 1.6f) }, 1, g, Skin);
            c.Fold(Bezier(T(9.6f, 5.8f), T(8.6f, 2f), T(10.2f, -2.4f), 5), g, 2, 1, Skin);
            // Cresta ilíaca.
            c.Ridge(Bezier(T(3.8f, -4.6f), T(5f, 0.6f), T(3.4f, 5.4f), 5), 1, g, Skin);
            // Omóplato en la joroba.
            c.Fold(Bezier(T(21.4f, -4.4f), T(18f, -2.4f), T(14.6f, -3.6f), 5), g, 2, 1, Skin);
            // Latigazos de la penitencia: verdugones cruzados en la espalda (surco con su labio claro).
            c.Fold(new[] { T(20.2f, -5.8f), T(15.6f, -1.6f) }, g, 2, 1, Skin);
            c.Fold(new[] { T(17.4f, -6.4f), T(12.6f, -2.6f) }, g, 2, 1, Skin);
            c.Fold(new[] { T(13.4f, -5.6f), T(16.8f, -2.2f) }, g, 1, 1, Skin);

            // Vendas oscuras en la cintura: tres vueltas sueltas, cada una con su volumen (la sombra de contacto las separa).
            int gWrap = c.NewGroup();
            for (int k = 0; k < 3; k++)
            {
                float u = 5f + k * 1.9f;
                float tilt = k == 1 ? -0.7f : 0.6f;
                c.Capsule(T(u + tilt, -5f + k * 0.2f), T(u - tilt, 5.2f - k * 0.3f), 1.2f, 1.1f, Bandage, 4.5f + k * 0.01f, k == 1 ? -0.05f : 0f, c.NewGroup());
            }
            // Cabo de venda que cuelga por delante.
            var w0 = T(6.2f, 4.2f);
            var w1 = Add(w0, V(0.6f - rag * 2f, -7.5f));
            c.Strand(Bezier(w0, Add(Mix(w0, w1, 0.5f), V(1.2f, 0f)), Floor(w1), 4), 0.9f, 0.6f, Bandage, 4.55f, -0.04f, gWrap);

            // Vértebras que asoman por la espalda (de la cadera a la nuca): nudos con su canto y su sombra.
            float[] us = { 2.6f, 5f, 7.4f, 9.8f, 12.2f, 14.6f, 17f, 19.4f, 21.6f };
            float[] vs = { -5.5f, -4.8f, -4.4f, -4.5f, -5.2f, -6f, -6.5f, -6.4f, -5.6f };
            int gv = c.NewGroup();
            for (int i = 0; i < us.Length; i++)
                c.Ellipse(T(us[i], vs[i]), 1f, 1.2f, ta, Bone, 4.3f + i * 0.001f, -0.1f, gv);
        }

        /// <summary>Escapulario: tablilla de madera con una cruz tallada, colgando del cuello con un cordel.</summary>
        void DrawScapular(ShadedCanvas c, Vector2 neckFront, Vector2 neckBack, float sway, int g)
        {
            var top = Add(neckFront, V(0.8f - sway * 0.6f, -7.2f));
            c.Strand(new[] { neckBack, Mix(neckBack, top, 0.55f), top }, 0.4f, 0.4f, Rope, 5.5f, -0.05f, g);
            c.Strand(new[] { neckFront, top }, 0.4f, 0.4f, Rope, 5.52f, 0f, g);
            var a = Add(top, V(-1.6f, -0.4f));
            c.Poly(new[] { a, Add(a, V(3.4f, 0f)), Add(a, V(3.2f, -4.4f)), Add(a, V(-0.2f, -4.4f)) }, Wood, 5.55f, 0.8f, 0.05f, g);
            c.Crease(new[] { Add(a, V(1.6f, -0.9f)), Add(a, V(1.6f, -3.6f)) }, 2, g, Wood);
            c.Crease(new[] { Add(a, V(0.6f, -1.8f)), Add(a, V(2.6f, -1.8f)) }, 2, g, Wood);
        }

        /// <summary>Nada se hunde en el suelo: colgajos, cabos y cuentas se quedan apoyados encima.</summary>
        static Vector2 Floor(Vector2 v, float min = 1.1f) => V(v.x, Mathf.Max(v.y, min));

        /// <summary>Cuello flaco: tráquea, tendón y dos vértebras en la nuca.</summary>
        void DrawNeck(ShadedCanvas c, Vector2 a, Vector2 b, float angle, int g)
        {
            c.Capsule(a, b, 2.7f, 2.2f, Skin, 5f, 0f, g);
            var side = Dir(angle + 90f);   // hacia la nuca
            c.Ridge(new[] { Add(Mix(a, b, 0.1f), Scale(side, -1.4f)), Add(Mix(a, b, 0.95f), Scale(side, -1.2f)) }, 1, g, Skin);
            c.Crease(new[] { Add(Mix(a, b, 0.15f), Scale(side, 0.2f)), Add(Mix(a, b, 0.9f), Scale(side, 0.6f)) }, 1, g, Skin);
            int gv = c.NewGroup();
            c.Ellipse(Add(Mix(a, b, 0.25f), Scale(side, 2.2f)), 1.1f, 1.2f, 0f, Bone, 4.9f, -0.05f, gv);
            c.Ellipse(Add(Mix(a, b, 0.7f), Scale(side, 2f)), 1f, 1.1f, 0f, Bone, 4.9f, -0.05f, gv);
        }

        /// <summary>Cíngulo de cuerda con nudos (cilicio) y el cabo colgando delante.</summary>
        void DrawBelt(ShadedCanvas c, System.Func<float, float, Vector2> T, float ta, int g, float rag)
        {
            var b0 = T(4.6f, -6f);
            var b1 = T(4.6f, 6f);
            c.Capsule(b0, b1, 1.1f, 1.1f, Rope, 6.6f, 0f, g).WithDetail((u, v) => Mathf.Repeat(u + v * 1.2f, 2.4f) < 0.9f ? -1 : 0);
            var knot = T(4.4f, 5.4f);
            c.Ellipse(knot, 1.5f, 1.4f, 0f, Rope, 6.65f, 0f, g);
            // Cabo con dos nudos que se mece.
            var end = Add(knot, V(1.2f - rag * 2.5f, -10f));
            end = Floor(end);
            var pts = Bezier(knot, Floor(Add(knot, V(1.6f - rag * 0.8f, -5f))), end, 5);
            c.Strand(pts, 0.75f, 0.6f, Rope, 6.62f, 0f, g);
            c.Ellipse(pts[2], 1.05f, 1f, 0f, Rope, 6.63f, 0f, g);
            c.Ellipse(end, 1.1f, 1f, 0f, Rope, 6.63f, 0f, g);
        }

        void DrawLeg(ShadedCanvas c, Vector2 hip, Vector2 knee, Vector2 ankle, float pitch, float z, float shade, int g, bool front)
        {
            // Muslo enjuto (hueso y tendón) y espinilla fina.
            c.Capsule(hip, knee, 3.1f, 2.1f, Skin, z, shade, g);
            c.Capsule(knee, ankle, 2.1f, 1.4f, Skin, z + 0.02f, shade, g);
            var thigh = Sub(knee, hip);
            float tl = Mathf.Max(0.01f, Mathf.Sqrt(thigh.x * thigh.x + thigh.y * thigh.y));
            var tf = V(-thigh.y / tl, thigh.x / tl);
            if (tf.x < 0f) tf = Scale(tf, -1f);
            // Tendón del muslo y surco del fémur.
            c.Fold(new[] { Add(Mix(hip, knee, 0.2f), Scale(tf, 0.6f)), Add(Mix(hip, knee, 0.85f), Scale(tf, 0.4f)) }, g, 2, 1, Skin);
            // Rótula huesuda.
            c.Ellipse(Add(knee, Scale(tf, 0.9f)), 1.9f, 2.1f, 0f, Bone, z + 0.05f, shade, g);
            // Vendas en la espinilla (vueltas inclinadas) con un cabo suelto.
            var s0 = Mix(knee, ankle, 0.3f);
            var s1 = Mix(knee, ankle, front ? 0.86f : 0.78f);
            int gw = c.NewGroup();
            c.Capsule(s0, s1, 2.3f, 1.9f, Linen, z + 0.04f, shade - 0.04f, gw)
             .WithDetail((u, v) => Mathf.Repeat(u * 0.9f + v * 0.7f, 2.6f) < 0.8f ? -1 : 0);
            var tail = Mix(s0, s1, 0.15f);
            c.Strand(Bezier(tail, Floor(Add(tail, V(-2.5f, -1.5f))), Floor(Add(tail, V(-3.4f, -4.6f))), 3), 0.7f, 0.45f, Linen, z - 0.02f, shade - 0.06f, gw);
            // Pie.
            DrawFoot(c, ankle, pitch, z + 0.06f, shade - 0.07f, g);
        }

        /// <summary>Pie descalzo y huesudo: talón, empeine con los metatarsos marcados, dedos largos y el tobillo.</summary>
        void DrawFoot(ShadedCanvas c, Vector2 ankle, float pitch, float z, float shade, int g)
        {
            Vector2 L(float x, float y) => Add(ankle, Rotate(V(x, y), V(0f, 0f), -pitch));
            c.Ellipse(L(-1.2f, -1.7f), 1.8f, 1.6f, -pitch, Skin, z, shade, g);
            c.Capsule(L(0.2f, -0.6f), L(4.8f, -2f), 1.7f, 1.25f, Skin, z + 0.01f, shade, g);
            c.Capsule(L(4.4f, -2.1f), L(7.6f, -2.2f), 1f, 0.8f, Skin, z + 0.02f, shade, g);
            c.Capsule(L(3.6f, -2.5f), L(6.6f, -2.6f), 0.85f, 0.7f, Skin, z + 0.015f, shade - 0.1f, g);
            c.Ridge(new[] { L(0.8f, 0.4f), L(4.6f, -1f) }, 1, g, Skin);
            c.Ellipse(L(0.4f, 0.2f), 1.1f, 1.1f, 0f, Bone, z + 0.03f, shade, g);
            // Nudillos de los dedos, separación entre ellos y planta en sombra.
            c.Dot(L(4.6f, -1.3f), 1, g, Skin);
            c.Crease(new[] { L(4.8f, -2.6f), L(7.6f, -2.9f) }, 1, g, Skin);
            c.Mark(new[] { L(-2.2f, -3f), L(7f, -3f) }, -1, g, Skin);
        }

        /// <summary>Brazo de hueso y tendón: hombro huesudo, codo nudoso y vendas en el antebrazo (cuerda en el lejano).</summary>
        void DrawArm(ShadedCanvas c, Vector2 shoulder, Vector2 elbow, Vector2 hand, float z, float shade, int g, bool front)
        {
            c.Capsule(shoulder, elbow, 2.4f, 1.7f, Skin, z, shade, g);
            c.Capsule(elbow, hand, 1.8f, 1.25f, Skin, z + 0.02f, shade, g);
            // Cabeza del húmero y codo.
            c.Ellipse(Add(shoulder, V(0f, 0.6f)), 2.6f, 2.4f, 0f, Skin, z + 0.01f, shade + 0.04f, g);
            c.Ellipse(elbow, 1.5f, 1.5f, 0f, Bone, z + 0.04f, shade, g);
            var up = Sub(elbow, shoulder);
            float ul = Mathf.Max(0.01f, Mathf.Sqrt(up.x * up.x + up.y * up.y));
            var across = V(-up.y / ul, up.x / ul);
            c.Fold(new[] { Add(Mix(shoulder, elbow, 0.25f), Scale(across, 0.5f)), Add(Mix(shoulder, elbow, 0.85f), Scale(across, 0.3f)) }, g, 2, 1, Skin);
            var f0 = Mix(elbow, hand, 0.22f);
            var f1 = Mix(elbow, hand, 0.74f);
            int gw = c.NewGroup();
            if (front)
            {
                c.Capsule(f0, f1, 2.1f, 1.7f, Linen, z + 0.05f, shade - 0.03f, gw)
                 .WithDetail((u, v) => Mathf.Repeat(u * 0.9f - v * 0.7f, 2.4f) < 0.8f ? -1 : 0);
                var tail = Mix(f0, f1, 0.2f);
                c.Strand(Bezier(tail, Floor(Add(tail, V(-1f, -2.6f))), Floor(Add(tail, V(-0.4f, -5f))), 3), 0.65f, 0.4f, Linen, z + 0.03f, shade - 0.06f, gw);
            }
            else
            {
                c.Capsule(Mix(elbow, hand, 0.66f), Mix(elbow, hand, 0.8f), 1.7f, 1.6f, Rope, z + 0.05f, shade, gw);
            }
        }

        /// <summary>Puño sobre el asta: dorso huesudo con nudillos, dedos largos que la rodean y el pulgar.</summary>
        void DrawGrip(ShadedCanvas c, Vector2 hand, float shaft, float z, float shade, int g, bool near)
        {
            var axis = Dir(shaft);
            var side = Dir(shaft + 90f);
            Vector2 At(float a, float s) => Add(hand, Add(Scale(axis, a), Scale(side, s)));
            if (near)
            {
                // Se ve el dorso: tapa el asta; los dedos asoman por debajo.
                c.Ellipse(At(-0.3f, 0.6f), 2.5f, 2.1f, shaft, Skin, z, shade, g);
                for (int i = 0; i < 3; i++)
                {
                    float a = -1.6f + i * 1.5f;
                    c.Capsule(At(a, 1.4f), At(a + 0.4f, -2.1f), 0.8f, 0.65f, Skin, z + 0.02f + i * 0.001f, shade, g);
                    c.Dot(At(a, 1.6f), 1, g, Skin);
                }
                c.Capsule(At(-2.4f, 0.8f), At(-0.4f, 2.6f), 0.8f, 0.6f, Skin, z + 0.03f, shade, g);
            }
            else
            {
                // Mano lejana: la palma queda tras el asta y los dedos la cruzan por delante.
                c.Ellipse(At(0f, 0.9f), 2.3f, 1.9f, shaft, Skin, z, shade, g);
                for (int i = 0; i < 3; i++)
                {
                    float a = -1.5f + i * 1.5f;
                    c.Capsule(At(a, 1.6f), At(a + 0.3f, -1.4f), 0.75f, 0.6f, Skin, z + 0.6f + i * 0.001f, shade, g);
                }
            }
        }

        /// <summary>Mano abierta y colgante (sin lanza): palma y dedos largos.</summary>
        void DrawLooseHand(ShadedCanvas c, Vector2 elbow, Vector2 hand, float z, float shade, int g)
        {
            var d = Sub(hand, elbow);
            float a = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            // A ras de suelo la mano se apoya plana en vez de clavarse.
            if (hand.y < 7f && a < 0f) a = Mathf.Lerp(a, d.x >= 0f ? -6f : -174f, Mathf.Clamp01((7f - hand.y) / 3.5f));
            var palm = Add(hand, Dir(a, 1.6f));
            c.Ellipse(palm, 2.2f, 1.6f, a, Skin, z, shade, g);
            for (int i = 0; i < 3; i++)
            {
                float fa = a - 18f + i * 16f;
                var k0 = Add(palm, Dir(fa, 1.4f));
                var k1 = Add(k0, Dir(fa + 14f, 3.4f));
                c.Capsule(k0, k1, 0.75f, 0.5f, Skin, z + 0.01f, shade, g);
            }
        }

        /// <summary>Harapo colgando del cíngulo: tiras de arpillera con borde roto y vaivén (como los del Guardián).</summary>
        void DrawRag(ShadedCanvas c, Vector2 a, Vector2 b, float length, float sway, float lean, float z, float shade, int g, int seed, PixelMaterial mat)
        {
            var down = Rotate(V(0f, -1f), V(0f, 0f), -Mathf.Clamp(lean, 0f, 95f) * 0.6f);
            Vector2 Fl(Vector2 v) => V(v.x, Mathf.Max(v.y, 0.8f));
            const int strips = 3;
            for (int i = 0; i < strips; i++)
            {
                float t0 = i / (float)strips, t1 = (i + 1) / (float)strips;
                var top0 = Mix(a, b, t0 - (i > 0 ? 0.08f : 0f));
                var top1 = Mix(a, b, t1);
                float len = length * (0.65f + 0.45f * PixelCanvas.Hash(i, seed, 1));
                float sw = sway * (1f + 0.35f * i);
                var mid = Fl(Add(Add(Mix(top0, top1, 0.5f), Scale(down, len * 0.55f)), V(-sw * 1.4f, 0f)));
                var end = Fl(Add(Add(Mix(top0, top1, 0.5f), Scale(down, len)), V(-sw * 3.4f - 0.4f * i, 0f)));
                float half = Mathf.Sqrt((top1.x - top0.x) * (top1.x - top0.x) + (top1.y - top0.y) * (top1.y - top0.y)) * 0.5f;
                var across = V(-down.y, down.x);
                var pts = new List<Vector2> { top0, Fl(Add(mid, Scale(across, half * 0.9f))), Fl(Add(Add(end, Scale(across, half * 0.5f)), Scale(down, -1.2f))), end,
                                              Fl(Add(Add(end, Scale(across, -half * 0.4f)), Scale(down, -2.2f))), Fl(Add(mid, Scale(across, -half * 0.8f))), top1 };
                c.Poly(pts.ToArray(), mat, z + i * 0.01f, 1.4f, shade - 0.04f * (i % 2), g, 0.1f, 0f);
                c.Crease(new[] { Mix(top0, top1, 0.5f), Mix(Mix(top0, top1, 0.5f), end, 0.8f) }, 1, g, mat);
            }
        }

        // ------------------------------------------------------------------
        // Cabeza
        // ------------------------------------------------------------------

        /// <summary>
        /// Calavera forrada de piel: cráneo, arco ciliar, cuenca honda con la brasa, pómulo, hueco de la nariz, dientes
        /// descarnados y mandíbula. Encima, la corona penitencial: un aro de alambre retorcido con clavos y astas que
        /// salen hacia arriba y atrás (una con una crucecita y otra bifurcada).
        /// </summary>
        void DrawHead(ShadedCanvas c, Vector2 hc, float hr, Pose p)
        {
            Vector2 H(float x, float y) => Add(hc, Rotate(V(x, y), V(0f, 0f), hr));
            int gHead = c.NewGroup(), gJaw = c.NewGroup(), gCrown = c.NewGroup(), gSpikes = c.NewGroup(), gEye = c.NewGroup();
            float jaw = p["jaw"];
            float eye = p["eye"];

            // Corona: el aro rodea el cráneo a la altura de la frente; de él salen estacas de madera seca hacia arriba y
            // atrás (una con una crucecita atada, otra bifurcada como una rama) y clavos cortos de hierro entre medias.
            var band = Bezier(H(-6f, 2.6f), H(-1.2f, 6.6f), H(3.6f, 4.6f), 9);
            Vector2 OnBand(float t)
            {
                float q = Mathf.Clamp(t, 0f, 0.999f) * (band.Length - 1);
                int k = Mathf.FloorToInt(q);
                return Mix(band[k], band[k + 1], q - k);
            }
            float[] at = { 0.08f, 0.27f, 0.47f, 0.67f, 0.86f };
            float[] tilt = { 66f, 44f, 22f, 4f, -14f };   // grados hacia atrás desde la vertical de la cabeza
            float[] len = { 9f, 14.5f, 17.5f, 12.5f, 7f };
            for (int i = 0; i < at.Length; i++)
            {
                var b = OnBand(at[i]);
                float a = 90f + hr + tilt[i];
                var tip = Add(b, Dir(a, len[i]));
                // Estacas algo torcidas: el tramo alto se dobla un poco hacia atrás.
                var mid = Add(Mix(b, tip, 0.55f), Dir(a + 90f, 0.5f));
                c.Capsule(b, mid, 0.85f, 0.6f, Stake, 6.8f + i * 0.001f, i % 2 == 0 ? 0f : -0.06f, gSpikes);
                c.Capsule(mid, tip, 0.6f, 0.3f, Stake, 6.8f + i * 0.001f, i % 2 == 0 ? 0f : -0.06f, gSpikes);
                if (i == 2)
                {
                    // Crucecita atada en la estaca más larga.
                    var cx = Add(b, Dir(a, len[i] * 0.74f));
                    c.Capsule(Add(cx, Dir(a + 90f, 2.2f)), Add(cx, Dir(a - 90f, 2.2f)), 0.5f, 0.5f, Stake, 6.81f, 0f, gSpikes);
                }
                if (i == 1)
                {
                    // Estaca bifurcada.
                    var fk = Add(b, Dir(a, len[i] * 0.5f));
                    c.Capsule(fk, Add(fk, Dir(a + 38f, 4.4f)), 0.55f, 0.3f, Stake, 6.79f, -0.06f, gSpikes);
                }
            }
            // Clavos cortos de hierro (radiales al cráneo) entre las estacas.
            int gNails = c.NewGroup();
            foreach (float t in new[] { 0.18f, 0.37f, 0.57f, 0.77f })
            {
                var b = OnBand(t);
                var d = Sub(b, H(-0.8f, 1.6f));
                float dl = Mathf.Max(0.01f, Mathf.Sqrt(d.x * d.x + d.y * d.y));
                c.Capsule(b, Add(b, Scale(d, 3.6f / dl)), 0.6f, 0.3f, IronDark, 6.82f, 0f, gNails);
            }

            // Cráneo (bóveda) y cara (maxilar), con la piel pegada al hueso.
            c.Ellipse(H(-0.8f, 1.6f), 5.6f, 5.3f, hr, Skin, 7f, 0f, gHead).Soft();
            c.Ellipse(H(3.2f, -0.8f), 3.3f, 3.5f, hr - 12f, Skin, 7.02f, 0f, gHead);
            // Arco ciliar saliente sobre la cuenca.
            c.Capsule(H(1.4f, 2.9f), H(5.6f, 2f), 1.15f, 0.85f, Skin, 7.05f, 0.1f, gHead);
            // Sien hundida, sutura del cráneo y el agujero del oído.
            c.Fold(Bezier(H(-0.6f, 4.4f), H(0.4f, 1.8f), H(1.2f, -0.6f), 4), gHead, 2, 1, Skin);
            c.Crease(new[] { H(-4.6f, 3.6f), H(-2.6f, 5.6f) }, 1, gHead, Skin);
            c.Ellipse(H(-1.4f, -0.6f), 0.7f, 0.9f, hr, Hollow, 7.06f, 0f, gHead);
            // Pómulo de hueso.
            c.Capsule(H(1.8f, -1.2f), H(4.4f, -1.5f), 0.75f, 0.6f, Bone, 7.07f, 0f, gHead);

            // Mandíbula descarnada (se abre con jaw) y la boca sin labios.
            float jawA = -jaw * 24f;
            var hinge = V(0.4f, -1.9f);
            Vector2 J(float x, float y)
            {
                var r = Rotate(V(x, y), V(0f, 0f), jawA);
                return H(hinge.x + r.x, hinge.y + r.y);
            }
            c.Ellipse(H(4.4f, -3f), 2.4f, 0.7f + jaw * 1.6f, hr - jaw * 10f, Hollow, 7.08f, 0f, gJaw);
            c.Poly(new[] { J(-0.8f, 0.8f), J(1.6f, -0.2f), J(5.8f, -0.8f), J(6.2f, -2f), J(4.8f, -3.1f), J(1.4f, -2.9f), J(-0.6f, -1.4f) }, Skin, 7.1f, 1.2f, 0f, gJaw);
            c.Ridge(new[] { J(1.2f, -2.6f), J(4.6f, -2.8f) }, 1, gJaw, Skin);
            // Dientes: fila de arriba larga (las encías se han retirado) y fila de abajo; huecos oscuros entre ellos.
            int gTeeth = c.NewGroup();
            c.Capsule(H(3.2f, -2.5f), H(6.1f, -2.7f), 0.85f, 0.75f, Teeth, 7.12f, 0f, gTeeth).WithDetail((u, v) => Mathf.Repeat(u + 0.3f, 1.5f) < 0.5f ? -1 : 0);
            c.Capsule(J(3f, -1.1f), J(5.4f, -1.3f), 0.7f, 0.6f, Teeth, 7.13f, -0.06f, gTeeth).WithDetail((u, v) => Mathf.Repeat(u + 0.9f, 1.5f) < 0.5f ? -1 : 0);
            // Hueco de la nariz.
            c.Ellipse(H(5.6f, -0.5f), 0.75f, 1.05f, hr - 10f, Hollow, 7.14f, 0f, gHead);

            // Cuenca del ojo: honda, con la brasa al fondo.
            var E = H(3.4f, 0.8f);
            c.Ellipse(E, 1.75f, 1.55f, hr, Hollow, 7.2f, 0f, gEye);
            if (eye > -0.95f)
            {
                var ember = Add(E, Rotate(V(0.5f, -0.1f), V(0f, 0f), hr));
                if (eye > 0.45f)
                {
                    // Telegrafiado: la brasa crece y llena la cuenca.
                    c.Ellipse(ember, 1.3f + eye * 0.5f, 1.1f + eye * 0.5f, 0f, EmberMid, 7.25f, 0f, gEye);
                    c.Ellipse(ember, 0.75f + eye * 0.35f, 0.7f + eye * 0.3f, 0f, EmberCore, 7.28f, 0f, gEye);
                    c.Decal(ember.x, ember.y, PixelCanvas.Hex("fff0b8"));
                    if (eye > 0.95f)
                    {
                        // Destello: rayos cortos que salen de la cuenca.
                        var flare = PixelCanvas.Hex("ffc070");
                        var flare2 = PixelCanvas.Hex("ff8a30");
                        c.Decal(ember.x + 3f, ember.y, flare);
                        c.Decal(ember.x + 4f, ember.y, flare2);
                        c.Decal(ember.x + 5f, ember.y, flare2);
                        c.Decal(ember.x, ember.y + 3f, flare2);
                        c.Decal(ember.x, ember.y - 3f, flare2);
                        c.Decal(ember.x - 2.5f, ember.y + 2.5f, flare2);
                        c.Decal(ember.x + 2.5f, ember.y + 2.5f, flare2);
                    }
                }
                else if (eye > -0.45f)
                {
                    c.Ellipse(ember, 0.9f, 0.8f, 0f, EmberDim, 7.24f, 0f, gEye);
                    c.Decal(ember.x, ember.y, PixelCanvas.Hex("ff9a40"));
                    c.Decal(ember.x - 1f, ember.y, PixelCanvas.Hex("b8461a"));
                }
                else
                {
                    c.Decal(ember.x, ember.y, PixelCanvas.Hex("7a2c10"));
                }
            }
            // Al morir, la brasa se apaga y suelta un hilo de humo que sube y se deshace.
            float smoke = p["smoke"];
            if (smoke > 0f)
            {
                int gs = c.NewGroup();
                for (int k = 0; k < 4; k++)
                {
                    float rise = 2.2f + k * 2.6f + smoke * 1.6f;
                    float r = Mathf.Max(0.5f, 1.3f - k * 0.2f - smoke * 0.15f);
                    if (k == 3 && smoke < 2f) break;
                    var puff = Add(E, V(0.8f + Mathf.Sin(k * 1.7f + smoke) * 1.4f, rise));
                    c.Ellipse(puff, r * 1.2f, r, 0f, k == 0 && smoke < 2.5f ? Smoke : SmokeFaint, 7.5f, 0f, gs);
                }
            }

            // Aro de alambre retorcido (encima del cráneo), con clavos cortos que lo atraviesan.
            c.Strand(band, 0.8f, 0.8f, IronDark, 7.4f, 0f, gCrown);
            for (int k = 1; k < band.Length - 1; k += 2) c.Dot(band[k], 1, gCrown, IronDark);
            // Cinta raída atada a la corona, colgando por la nuca.
            float sway = p["rag"];
            var r0 = H(-4.8f, 1.4f);
            c.Strand(Bezier(r0, Add(r0, V(-2.4f - sway * 1.5f, -3f)), Add(r0, V(-3.6f - sway * 3f, -8.5f)), 4), 1f, 0.6f, RagDark, 6.7f, -0.05f, c.NewGroup());
        }

        // ------------------------------------------------------------------
        // La lanza
        // ------------------------------------------------------------------

        /// <summary>
        /// Lanza larga: asta de madera oscura con vetas y nudos, regatón de hierro, ligaduras de cuerda, un adorno de
        /// hierro en forma de bola (como en la hoja) del que cuelga un rosario de cuentas de hueso con su medalla, una
        /// cinta roja desteñida y la moharra en hoja de laurel con nervio, filo y óxido.
        /// <paramref name="grip"/> es el puño delantero; la punta queda a <see cref="SpearTip"/> px a lo largo del asta.
        /// </summary>
        void DrawSpear(ShadedCanvas c, Vector2 grip, float angle, float z, int g, float sway)
        {
            var axis = Dir(angle);
            var side = Dir(angle + 90f);
            Vector2 At(float a, float s) => Add(grip, Add(Scale(axis, a), Scale(side, s)));
            int gIron = c.NewGroup(), gBlade = c.NewGroup(), gBead = c.NewGroup(), gRib = c.NewGroup();

            // Asta.
            c.Capsule(At(-SpearButt + 2f, 0f), At(35f, 0f), 1.2f, 1.1f, Wood, z, 0f, g);
            c.Ridge(new[] { At(-SpearButt + 3f, 0.6f), At(-4f, 0.6f) }, 1, g, Wood);
            c.Ridge(new[] { At(4f, 0.6f), At(26f, 0.6f) }, 1, g, Wood);
            // Regatón.
            c.Capsule(At(-SpearButt, 0f), At(-SpearButt + 2.4f, 0f), 0.8f, 1.4f, IronDark, z + 0.01f, 0f, gIron);
            // Ligaduras de cuerda.
            foreach (float a in new[] { -21f, 19f })
                c.Capsule(At(a - 1.6f, 0f), At(a + 1.6f, 0f), 1.6f, 1.6f, Rope, z + 0.02f, 0f, c.NewGroup())
                 .WithDetail((u, v) => Mathf.Repeat(u + v * 0.8f, 1.6f) < 0.6f ? -1 : 0);
            // Adorno: collarines y bola de hierro.
            c.Capsule(At(27.4f, 0f), At(28.6f, 0f), 1.7f, 1.7f, Iron, z + 0.03f, 0f, gIron);
            c.Ellipse(At(31f, 0f), 2.4f, 2.6f, angle, Iron, z + 0.035f, 0f, gIron);
            c.Capsule(At(33.4f, 0f), At(34.4f, 0f), 1.6f, 1.6f, Iron, z + 0.04f, 0f, gIron);
            c.Glint(At(30.4f, 1.2f), gIron, 0, Iron);
            // Cubo de la moharra.
            c.Capsule(At(34.5f, 0f), At(39.5f, 0f), 1.5f, 1.05f, Iron, z + 0.045f, 0f, gIron);
            // Moharra en hoja de laurel.
            c.Poly(new[] { At(38.6f, -1.2f), At(41.5f, -2.5f), At(45f, -2.2f), At(SpearTip, 0f), At(45f, 2.2f), At(41.5f, 2.5f), At(38.6f, 1.2f) },
                   Iron, z + 0.05f, 1.2f, 0f, gBlade).Frame(At(38.6f, 0f), angle).WithTint(RustTint, 0.4f, (u, v) => u < 3.2f && PixelCanvas.ValueNoise(u, v * 1.5f, 0, 55) > 0.5f ? 1f : 0f);
            c.Crease(new[] { At(39.5f, -0.2f), At(47.5f, -0.1f) }, 2, gBlade, Iron);
            c.Ridge(new[] { At(40f, 1.4f), At(44.8f, 1.6f), At(48.6f, 0.4f) }, 1, gBlade, Iron);
            c.Glint(At(44f, 1.2f), gBlade, 0, Iron);

            // Cinta roja desteñida atada bajo la moharra: dos colas que ondean hacia atrás.
            var k0 = At(35.5f, -1.2f);
            c.Ellipse(k0, 1.2f, 1.1f, 0f, Ribbon, z + 0.06f, 0f, gRib);
            float fl = Mathf.Clamp(sway, -1.5f, 1.5f);
            c.Strand(Bezier(k0, Floor(Add(k0, V(-2.4f - fl * 2f, -2.2f + fl * 0.8f))), Floor(Add(k0, V(-4f - fl * 5f, -6f + fl * 2.5f))), 4), 0.9f, 0.55f, Ribbon, z + 0.055f, 0f, gRib);
            c.Strand(Bezier(k0, Floor(Add(k0, V(-0.6f - fl * 1.5f, -3f))), Floor(Add(k0, V(-1.6f - fl * 3.6f, -7.6f + fl * 2f))), 4), 0.8f, 0.5f, Ribbon, z + 0.052f, -0.08f, gRib);

            // Rosario de cuentas de hueso colgando del adorno, con su medalla.
            var r0 = At(31f, -2.4f);
            var r1 = Add(r0, V(-1.2f - sway * 1.6f, -7.5f));
            if (r1.y < 3f) r1 = V(r1.x - (3f - r1.y), 3f);
            var beads = Bezier(r0, Add(Mix(r0, r1, 0.5f), V(0.8f, 0f)), r1, 5);
            for (int k = 1; k < beads.Length; k++) c.Ellipse(beads[k], 0.75f, 0.75f, 0f, Bone, z - 0.02f + k * 0.001f, 0f, gBead);
            c.Ellipse(Add(r1, V(0f, -1.8f)), 1.2f, 1.5f, 0f, Iron, z - 0.01f, 0f, gBead);
        }

        static readonly PixelMaterial DustMat = new PixelMaterial(Ramp.Make("5e564e", 3, 0.06f, 0.6f, 1.35f)) { Ambient = 0.45f, Rim = 0.3f, Outline = false, ReceivesContactShadow = false, Dither = 0f };

        /// <summary>Polvo levantado del suelo: bolitas que se abren hacia los lados (spread = cuánto se han alejado).</summary>
        static void Dust(ShadedCanvas c, float x0, float x1, float spread, int count, int seed)
        {
            for (int i = 0; i < count; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = (side < 0f ? x0 : x1) + side * (1.5f + (i / 2) * 3.2f + spread * 4f);
                float y = 1.4f + (i / 2) * 1.3f + spread * 1.4f + PixelCanvas.Hash(i, seed, 1);
                float r = (1.7f - spread * 0.5f) * (0.7f + PixelCanvas.Hash(i, seed, 2) * 0.5f);
                c.Ellipse(V(x, y), r * 1.3f, r, 0f, DustMat, 11f);
            }
        }

        /// <summary>Estela de la estocada: haz recto detrás de la punta, grueso delante y fino detrás, y dos líneas de velocidad.</summary>
        void DrawThrustSmear(ShadedCanvas c, Vector2 tip, float angle, ThrustSpec s)
        {
            var axis = Dir(angle);
            var side = Dir(angle + 90f);
            float len = s.Back;
            var tail = Sub(tip, Scale(axis, len));
            float w = s.Strength >= 1f ? 3f : 2f;
            Streak(c, tail, Add(tip, Scale(axis, 1f)), len, w + 1.4f, SmearSoft, 12f);
            Streak(c, Add(tail, Scale(axis, len * 0.15f)), Sub(tip, Scale(axis, 0.5f)), len * 0.85f, w * 0.7f, SmearMid, 12.05f);
            Streak(c, Add(tail, Scale(axis, len * 0.4f)), Sub(tip, Scale(axis, 1f)), len * 0.6f, w * 0.35f, SmearCore, 12.1f);
            for (int i = -1; i <= 1; i += 2)
            {
                var o = Scale(side, i * (w + 3f));
                var a = Add(Add(tail, Scale(axis, len * 0.1f)), o);
                var b = Add(Sub(tip, Scale(axis, len * (i < 0 ? 0.35f : 0.2f))), o);
                Streak(c, a, b, len * 0.6f, 0.8f, SmearMid, 12.02f);
            }
        }

        static void Streak(ShadedCanvas c, Vector2 a, Vector2 b, float len, float width, PixelMaterial m, float z)
        {
            float dx = b.x - a.x, dy = b.y - a.y;
            float l2 = Mathf.Max(0.001f, dx * dx + dy * dy);
            float r = width + 1f;
            c.Custom(Mathf.Min(a.x, b.x) - r, Mathf.Min(a.y, b.y) - r, Mathf.Max(a.x, b.x) + r, Mathf.Max(a.y, b.y) + r, (px, py) =>
            {
                float t = ((px - a.x) * dx + (py - a.y) * dy) / l2;
                if (t < 0f || t > 1f) return (false, N3.Front);
                float qx = a.x + dx * t - px, qy = a.y + dy * t - py;
                float d = Mathf.Sqrt(qx * qx + qy * qy);
                float th = Mathf.Lerp(0.3f, width, t * t) * (t > 0.92f ? Mathf.Sqrt((1f - t) / 0.08f) : 1f);
                return (d <= th, N3.Front);
            }, m, z);
        }
    }
}
