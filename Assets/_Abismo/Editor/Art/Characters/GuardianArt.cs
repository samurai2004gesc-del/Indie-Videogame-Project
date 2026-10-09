using System.Collections.Generic;
using UnityEngine;
using static Abismo.EditorTools.Rig;

namespace Abismo.EditorTools
{
    /// <summary>
    /// El Guardián de la Concha: Profundo veterano de la Orden Esotérica de Dagón. Más corpulento y erguido que el
    /// Profundo raso, con la piel verde-azulada oscura cubierta de placas de concha y racimos de percebes a modo de
    /// armadura. Con el brazo delantero sostiene un ENORME escudo hecho de una vieira gigante (nácar iridiscente,
    /// percebes y el sello de Dagón tallado) que bloquea todo golpe frontal; con el trasero empuña un arpón largo
    /// incrustado de coral. Mira a la derecha; el juego lo voltea al girar.
    ///
    /// Convenciones de pose (las mismas que el Profundo, más las del escudo y el arpón):
    ///  - piernas por IK: posición del tobillo (fFx/fFy, fBx/fBy) y ángulo del pie (fFa/fBa, matemático)
    ///  - lean: inclinación del tronco hacia delante (grados desde la vertical)
    ///  - hFx/hFy: mano del escudo respecto al hombro delantero · shR: giro del escudo (+ = la parte de arriba hacia atrás)
    ///  - hBx/hBy: mano del arpón respecto al hombro trasero · spear: ángulo del arpón (matemático, hacia la punta)
    ///    · slide: cuánto se desliza el arpón en el puño (+ = hacia la punta)
    ///  - jaw 0..1 boca · gill 0..1 branquias · fin -1..1 aleta plegada/erizada · cloth: vaivén de los harapos
    ///  - eye: -1 apagado/aturdido, 0 normal (tenue), 1 telegrafiando (brilla y crece)
    ///  - shFree: el escudo ya no está en la mano (muerte): sdX/sdY/sdR/sdF = posición, giro y aplastamiento
    /// </summary>
    public sealed class GuardianArt : CharacterArt
    {
        public override string Id => "guardian";
        public override int FrameWidth => 168;
        public override int FrameHeight => 120;
        public override Vector2 Pivot => new Vector2(PivotX, PivotY);

        const float PivotX = 84f, PivotY = 8f;

        // ------------------------------------------------------------------
        // Materiales
        // ------------------------------------------------------------------

        static readonly Color32[] SkinRamp = Ramp.Make("36504e", 5, 0.1f, 0.3f, 1.55f);
        static readonly Color32[] BellyRamp = Ramp.Make("8a876a", 5, 0.1f, 0.34f, 1.35f);
        static readonly PixelMaterial SkinDark = new PixelMaterial(Ramp.Make("26323a", 4, 0.08f, 0.35f, 1.5f)) { Ambient = 0.26f, Rim = 0.5f };
        static readonly PixelMaterial FinMat = new PixelMaterial(Ramp.Make("453b52", 4, 0.1f, 0.38f, 1.6f)) { Ambient = 0.3f, Rim = 0.8f, Dither = 0f };
        static readonly PixelMaterial SpineMat = new PixelMaterial(Ramp.Make("362f3c", 4, 0.06f, 0.4f, 1.6f)) { Ambient = 0.3f, Rim = 0.6f, Dither = 0f };
        static readonly PixelMaterial ShellMat = new PixelMaterial(Ramp.Make("625a56", 5, 0.1f, 0.32f, 1.48f)) { Ambient = 0.26f, Rim = 0.6f, Dither = 0.03f, Texture = PitNoise };
        static readonly PixelMaterial BarnacleMat = new PixelMaterial(Ramp.Make("aca48c", 4, 0.08f, 0.38f, 1.32f)) { Ambient = 0.3f, Rim = 0.5f, Dither = 0f };
        static readonly PixelMaterial BarnacleHole = new PixelMaterial(Ramp.Make("2b2533", 3, 0.05f, 0.6f, 1.4f)) { Ambient = 0.55f, Outline = false, Dither = 0f };
        static readonly PixelMaterial Cloth = new PixelMaterial(Ramp.Make("34362b", 4, 0.1f, 0.35f, 1.6f)) { Ambient = 0.22f, Rim = 0.55f, Texture = ClothNoise };
        static readonly PixelMaterial Rope = new PixelMaterial(Ramp.Make("6e5f45", 4, 0.08f)) { Ambient = 0.3f };
        static readonly PixelMaterial Wood = new PixelMaterial(Ramp.Make("564538", 4, 0.08f, 0.36f, 1.55f)) { Ambient = 0.26f, Rim = 0.5f, Dither = 0f, Texture = GrainNoise };
        static readonly PixelMaterial Iron = new PixelMaterial(Ramp.Make("67716f", 5, 0.08f, 0.32f, 1.42f)) { Gloss = 0.45f, Ambient = 0.3f, Rim = 0.45f, Dither = 0f, Texture = RustNoise };
        static readonly PixelMaterial CoralMat = new PixelMaterial(Ramp.Make("7a4a47", 4, 0.08f, 0.38f, 1.45f)) { Ambient = 0.3f, Rim = 0.5f, Dither = 0f };
        static readonly PixelMaterial ClawMat = new PixelMaterial(Ramp.Make("b3a885", 4, 0.1f, 0.4f, 1.3f)) { Ambient = 0.35f, Gloss = 0.3f, Dither = 0f };
        static readonly PixelMaterial GillMat = new PixelMaterial(Ramp.Make("5a2430", 4, 0.06f, 0.45f, 1.3f)) { Ambient = 0.3f, Rim = 0.1f, Dither = 0f, Outline = false };
        static readonly PixelMaterial MouthIn = new PixelMaterial(Ramp.Make("3a1a26", 3, 0.05f, 0.5f, 1.3f)) { Ambient = 0.5f, Outline = false };

        static readonly PixelMaterial EyeRing = PixelMaterial.Glow("5e5a1c");
        static readonly PixelMaterial EyeMid = PixelMaterial.Glow("a69e38");
        static readonly PixelMaterial EyeCore = PixelMaterial.Glow("ddd578");
        static readonly PixelMaterial EyeRingHot = PixelMaterial.Glow("948824");
        static readonly PixelMaterial EyeMidHot = PixelMaterial.Glow("e4d846");
        static readonly PixelMaterial EyeCoreHot = PixelMaterial.Glow("fffbcc");
        static readonly PixelMaterial EyeDull = PixelMaterial.Glow("3e3c26");
        static readonly PixelMaterial EyeDullMid = PixelMaterial.Glow("5e5a36");
        static readonly PixelMaterial Pupil = PixelMaterial.Glow("120f0c");

        /// <summary>Nácar: rampa iridiscente hecha a mano (índigo → verde mar → rosa → blanco perla).</summary>
        static readonly Color32[] NacreRamp =
        {
            PixelCanvas.Hex("231d37"), PixelCanvas.Hex("34405d"), PixelCanvas.Hex("4b7876"),
            PixelCanvas.Hex("86ad99"), PixelCanvas.Hex("cd9db0"), PixelCanvas.Hex("f2e4e8"),
        };

        static readonly PixelMaterial SmearCore = new PixelMaterial(new[] { new Color32(250, 244, 240, 245) }) { Emissive = true, Outline = false, ReceivesContactShadow = false };
        static readonly PixelMaterial SmearMid = new PixelMaterial(new[] { new Color32(204, 214, 210, 200) }) { Emissive = true, Outline = false, ReceivesContactShadow = false };
        static readonly PixelMaterial SmearSoft = new PixelMaterial(new[] { new Color32(118, 140, 140, 140) }) { Emissive = true, Outline = false, ReceivesContactShadow = false };

        static float ClothNoise(int x, int y) => (PixelCanvas.Hash(x / 2, y / 3, 41) - 0.5f) * 0.08f;
        static float RustNoise(int x, int y) => PixelCanvas.ValueNoise(x / 3f, y / 3f, 0, 77) > 0.72f ? -0.22f : 0f;
        static float PitNoise(int x, int y) => PixelCanvas.ValueNoise(x / 2.2f, y / 2.2f, 0, 619) > 0.74f ? -0.18f : 0f;
        static float GrainNoise(int x, int y) => (PixelCanvas.Hash(x / 3, y, 5) - 0.5f) * 0.1f;

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
        const float FootLen = 6f;

        static Pose Idle => new Pose().With(
            ("hipX", -4f), ("hipY", 23.5f), ("lean", 12f), ("neck", 0f), ("head", 0f),
            ("fFx", 6f), ("fFy", 6.2f), ("fFa", -55f),
            ("fBx", -11f), ("fBy", 6.2f), ("fBa", -55f),
            ("hFx", 14f), ("hFy", -17f), ("shR", 3f),
            ("hBx", -10f), ("hBy", -14f), ("spear", 80f), ("slide", 0f),
            ("jaw", 0.08f), ("gill", 0.3f), ("fin", 0f), ("finWave", 0f), ("cloth", 0f), ("eye", 0f));

        public override List<AnimSpec> Animations() => new List<AnimSpec>
        {
            new AnimSpec("idle", 6, 8f, true, f => DrawIdle(f)),
            new AnimSpec("walk", 8, 9f, true, f => DrawWalk(f)),
            new AnimSpec("block", 3, 14f, false, f => DrawBlock(f)),
            new AnimSpec("blockhit", 3, 16f, false, f => DrawBlockHit(f)),
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

        ShadedCanvas DrawIdle(int f)
        {
            float t = f / 6f * Mathf.PI * 2f;
            var p = Idle.With(
                ("hipY", 23.5f - 0.5f + Mathf.Cos(t) * 0.5f), ("lean", 12f + Mathf.Sin(t) * 1f),
                ("head", Mathf.Sin(t - 0.8f) * 2f), ("jaw", 0.08f + 0.08f * Mathf.Sin(t + 0.5f)),
                ("gill", 0.35f + 0.35f * Mathf.Sin(t + 1.2f)), ("finWave", t), ("fin", 0.1f * Mathf.Sin(t)),
                ("hFy", -17f + Mathf.Cos(t - 0.7f) * 0.6f), ("shR", 3f + Mathf.Sin(t - 1f) * 0.8f),
                ("spear", 80f + Mathf.Sin(t - 1.2f) * 1.2f), ("hBy", -14f + Mathf.Cos(t - 0.9f) * 0.5f),
                ("cloth", Mathf.Sin(t - 1f) * 0.4f));
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

        // Avanza pesado tras el escudo: pasos cortos, mucho peso, el escudo se balancea a contratiempo.
        ShadedCanvas DrawWalk(int f)
        {
            float ph = f / 8f;
            float t = ph * Mathf.PI * 2f;
            var p = Idle.With(("lean", 14f), ("hipX", -3f));
            Step(p, "F", ph, 2f, 6f, 3.5f);
            Step(p, "B", ph + 0.5f, -6f, 6f, 3.5f);
            p["hipY"] = 22.4f + Mathf.Cos(t * 2f) * 1f;
            p["lean"] = 14f + Mathf.Cos(t * 2f + 0.5f) * 1f;
            p["head"] = Mathf.Sin(t * 2f - 0.6f) * 2f;
            p["hFx"] = 14.5f + Mathf.Sin(t) * 0.8f;
            p["hFy"] = -17f + Mathf.Cos(t * 2f - 0.9f) * 0.9f;
            p["shR"] = 2f + Mathf.Sin(t * 2f - 1.2f) * 1.2f;
            p["spear"] = 78f + Mathf.Sin(t - 0.5f) * 2.5f;
            p["hBx"] = -10f + Mathf.Sin(t - 0.4f) * 1.5f;
            p["finWave"] = t * 2f;
            p["cloth"] = 0.3f + Mathf.Sin(t * 2f - 1f) * 0.6f;
            p["gill"] = 0.4f + 0.3f * Mathf.Sin(t * 2f);
            p["jaw"] = 0.12f;
            return Draw(p);
        }

        static Pose Guard => Idle.With(("hipX", -4.5f), ("hipY", 20f), ("lean", 19f), ("neck", -6f), ("head", -6f),
                                        ("fFx", 9f), ("fBx", -13.5f),
                                        ("hFx", 12.5f), ("hFy", -9.5f), ("shR", -5f),
                                        ("hBx", -5f), ("hBy", -13f), ("spear", 74f), ("slide", -2f),
                                        ("jaw", 0.2f), ("gill", 0.5f), ("fin", 0.5f), ("cloth", 0.4f), ("eye", 0.2f));

        // Alza el escudo: se agacha, mete la cabeza detrás y lo levanta (el último fotograma es la guardia sostenida).
        ShadedCanvas DrawBlock(int f)
        {
            Pose p;
            if (f == 0) p = Pose.Lerp(Idle, Guard, 0.45f).With(("hipY", 21.5f), ("cloth", 0.7f), ("fin", 0.2f));
            else if (f == 1) p = Guard.With(("hipY", 19.4f), ("hFy", -8.5f), ("shR", -6.5f), ("lean", 20f), ("cloth", 0.8f), ("fin", 0.7f));
            else p = Guard;
            p["finWave"] = f * 1.3f;
            p["gill"] = 0.5f + 0.2f * f;
            return Draw(p);
        }

        // Recibe un golpe en el escudo: el impacto lo empuja atrás y el escudo vibra.
        ShadedCanvas DrawBlockHit(int f)
        {
            Pose p;
            if (f == 0) p = Guard.With(("hipX", -6.8f), ("lean", 13f), ("hFx", 9.5f), ("hFy", -9f), ("shR", 7f), ("neck", -2f), ("head", 6f),
                                       ("fFx", 8.5f), ("fFy", 7.4f), ("fFa", -64f), ("jaw", 0.55f), ("gill", 1f), ("cloth", -0.6f), ("fin", 0.9f), ("spear", 80f));
            else if (f == 1) p = Guard.With(("hipX", -6f), ("lean", 16f), ("hFx", 11.8f), ("hFy", -9.8f), ("shR", -7f), ("head", 0f),
                                            ("jaw", 0.35f), ("gill", 0.8f), ("cloth", 0.9f), ("fin", 0.6f), ("spear", 72f));
            else p = Pose.Lerp(Guard, Guard.With(("hipX", -5.4f), ("shR", -3f)), 0.6f).With(("cloth", 0.2f));
            p["finWave"] = 1f + f * 1.6f;
            var c = Draw(p);
            if (f <= 1) ShieldImpact(c, ShieldCenter(p), p["shR"], f);
            return c;
        }

        static Pose Gather => Guard.With(("hipY", 19.5f), ("lean", 16f), ("hipX", -5f), ("head", -4f),
                                          ("hBx", 2f), ("hBy", -6f), ("spear", 55f), ("slide", -4f), ("eye", 0.3f), ("fin", 0.4f), ("cloth", 0.2f));

        static Pose Cocked => Guard.With(("hipY", 20.5f), ("lean", 6f), ("hipX", -6.5f), ("neck", 4f), ("head", 2f),
                                          ("fFx", 10f), ("fBx", -14f),
                                          ("hFx", 12f), ("hFy", -16f), ("shR", -2f),
                                          ("hBx", -11f), ("hBy", 15f), ("spear", 8f), ("slide", 0f), ("elbBUp", 1f),
                                          ("jaw", 0.7f), ("gill", 1f), ("fin", 1f), ("eye", 0.6f), ("cloth", -0.5f));

        // Telegrafiado: alza el arpón por encima del escudo y lo echa atrás; en los dos últimos el ojo brilla y crece.
        ShadedCanvas DrawWindup(int f)
        {
            var keys = new Keyframes()
                .Key(0, Pose.Lerp(Idle, Gather, 0.6f), Ease.Out)
                .Key(1, Gather, Ease.InOut)
                .Key(3, Pose.Lerp(Gather, Cocked, 0.9f), Ease.Out)
                .Key(4, Cocked)
                .Key(5, Cocked.With(("lean", 3f), ("hipX", -7.5f), ("hBx", -13f), ("hBy", 15.5f), ("spear", 10f), ("head", 4f)));
            var p = keys.Evaluate(f);
            p["eye"] = f >= 4 ? 1f : Mathf.Lerp(0.2f, 0.6f, f / 3f);
            p["finWave"] = f * 1.4f;
            return Draw(p);
        }

        static Pose Lunge => Cocked.With(("hipY", 19.2f), ("lean", 24f), ("hipX", 2.5f), ("neck", -4f), ("head", -4f),
                                          ("fFx", 15f), ("fFy", 6.2f), ("fBx", -10f), ("fBy", 8.5f), ("fBa", -78f),
                                          ("hFx", 15f), ("hFy", -16f), ("shR", -10f),
                                          ("hBx", 13f), ("hBy", 20f), ("spear", -10f), ("slide", 2f), ("elbBUp", 1f),
                                          ("jaw", 0.85f), ("eye", 0.8f), ("fin", 0.7f), ("cloth", -1f));

        static Pose Thrust => Lunge.With(("hipX", 4.5f), ("lean", 27f), ("hipY", 18.6f), ("hBx", 15f), ("hBy", 19f), ("spear", -11f), ("slide", 4f),
                                          ("hFx", 16f), ("shR", -12f), ("eye", 0.6f), ("cloth", -1.2f));

        static Pose Follow => Thrust.With(("hipX", 4f), ("lean", 25f), ("hipY", 18.8f), ("hBx", 12f), ("hBy", 17f), ("spear", -14f), ("slide", 2f),
                                           ("jaw", 0.5f), ("eye", 0.2f), ("fin", 0.3f), ("cloth", -0.4f));

        // Lanzada: embestida con el escudo por delante y el arpón por encima; estela en los dos primeros fotogramas.
        ShadedCanvas DrawAttack(int f)
        {
            Pose p;
            var thrust = default(ThrustSpec);
            switch (f)
            {
                case 0:
                    p = Lunge;
                    thrust = new ThrustSpec { On = true, Back = 34f, Strength = 1f };
                    break;
                case 1:
                    p = Thrust;
                    thrust = new ThrustSpec { On = true, Back = 18f, Strength = 0.7f };
                    break;
                case 2:
                    p = Pose.Lerp(Thrust, Follow, 0.6f);
                    break;
                default:
                    p = Follow;
                    break;
            }
            p["finWave"] = f * 1.3f;
            return Draw(p, thrust);
        }

        ShadedCanvas DrawRecover(int f)
        {
            var pull = Follow.With(("hipX", 0f), ("lean", 20f), ("hBx", 6f), ("hBy", -6f), ("spear", 40f), ("slide", 0f),
                                   ("jaw", 0.6f), ("gill", 1f), ("eye", 0f), ("fin", 0f), ("cloth", 0.3f));
            var stepBack = Pose.Lerp(pull, Idle, 0.6f).With(("fFy", 9f), ("fFa", -70f));
            var keys = new Keyframes()
                .Key(0, Follow, Ease.Out)
                .Key(1.5f, pull)
                .Key(3f, stepBack)
                .Key(4, Idle);
            var p = keys.Evaluate(f);
            p["finWave"] = f * 1.2f;
            return Draw(p);
        }

        static Pose Hit => Idle.With(("lean", 2f), ("hipX", -6.5f), ("hipY", 22.6f), ("neck", 14f), ("head", 18f),
                                      ("hFx", 15f), ("hFy", -12f), ("shR", -16f),
                                      ("hBx", -8f), ("hBy", -11f), ("spear", 98f),
                                      ("jaw", 0.9f), ("fin", -0.7f), ("gill", 1f), ("cloth", 1f), ("eye", -0.2f));

        ShadedCanvas DrawHurt(int f)
        {
            var keys = new Keyframes().Key(0, Hit, Ease.Out).Key(1, Pose.Lerp(Hit, Idle, 0.4f)).Key(2, Pose.Lerp(Hit, Idle, 0.75f));
            var p = keys.Evaluate(f);
            p["finWave"] = f * 2f;
            return Draw(p);
        }

        // Aturdido: el escudo cuelga apartado y apoyado en el suelo, la guardia está abierta (ventana de ejecución).
        ShadedCanvas DrawStagger(int f)
        {
            float t = f / 6f * Mathf.PI * 2f;
            var p = Idle.With(
                ("lean", 4f + 4f * Mathf.Sin(t)), ("hipX", -5f + 1f * Mathf.Sin(t)), ("hipY", 21.5f + 0.5f * Mathf.Cos(t * 2f)),
                ("neck", 10f + 5f * Mathf.Sin(t + 1f)), ("head", 6f + 10f * Mathf.Sin(t + 1.7f)),
                ("hFx", -13f + 1.5f * Mathf.Sin(t + 2f)), ("hFy", -19f + 0.6f * Mathf.Sin(t + 2.4f)), ("shR", 22f + 4f * Mathf.Sin(t + 2.2f)),
                ("hBx", 9f + 1f * Mathf.Sin(t + 2.3f)), ("hBy", -19f), ("spear", -22f + 3f * Mathf.Sin(t + 2.6f)),
                ("jaw", 0.55f + 0.25f * Mathf.Sin(t * 2f)), ("gill", 0.7f + 0.3f * Mathf.Sin(t * 2f + 1f)),
                ("fin", -0.6f), ("finWave", t), ("cloth", Mathf.Sin(t + 1f) * 0.6f),
                ("eye", f == 2 || f == 3 ? -1f : -0.6f));
            return Draw(p);
        }

        ShadedCanvas DrawDeath(int f)
        {
            var hit = Hit.With(("lean", -2f), ("head", 24f), ("jaw", 1f), ("hipX", -7f), ("hFx", 17f), ("hFy", -6f), ("shR", -30f));
            var reel = hit.With(("lean", -6f), ("hipX", -8f), ("hipY", 22f), ("hFx", 4f), ("hFy", 10f), ("head", 18f), ("eye", -0.5f),
                                ("hBx", -10f), ("hBy", -6f), ("spear", 120f), ("shFree", 1f));
            var buckle = Idle.With(("lean", 26f), ("hipX", -5f), ("hipY", 13f), ("neck", 6f), ("head", -14f), ("jaw", 0.7f),
                                   ("hFx", 8f), ("hFy", -14f), ("hBx", 4f), ("hBy", -16f), ("spear", 30f),
                                   ("fin", -0.6f), ("eye", -0.8f), ("cloth", 0.2f), ("shFree", 1f));
            var topple = buckle.With(("lean", 68f), ("hipX", -4f), ("hipY", 13.5f), ("neck", 16f), ("head", -6f),
                                     ("hFx", 14f), ("hFy", -10f), ("hBx", 16f), ("hBy", -12f), ("spear", 5f), ("elbFUp", 1f), ("elbBUp", 1f),
                                     ("fFx", -7f), ("fFy", 4.5f), ("fBx", -11f), ("fBy", 4.5f), ("cloth", -0.6f));
            var prone = topple.With(("lean", 91f), ("hipX", -8f), ("hipY", 11f), ("neck", 24f), ("head", 4f), ("jaw", 0.5f),
                                    ("hFx", 18f), ("hFy", -1.5f), ("hBx", 20f), ("hBy", -2f), ("spear", -1f), ("slide", -9f),
                                    ("fFx", -26f), ("fFy", 3.6f), ("fFa", 175f), ("fBx", -29f), ("fBy", 3.8f), ("fBa", 175f), ("flipF", 1f), ("flipB", 1f),
                                    ("eye", -1f), ("fin", -0.2f), ("cloth", 0f));
            var keys = new Keyframes()
                .Key(0, hit, Ease.Out)
                .Key(1, reel)
                .Key(3, buckle, Ease.In)
                .Key(4, topple, Ease.In)
                .Key(5, prone)
                .Key(6, prone.With(("hipY", 12f), ("lean", 88f), ("jaw", 0.7f)), Ease.In)
                .Key(7, prone.With(("slump", 0.3f)), Ease.Out)
                .Key(9, prone.With(("slump", 0.8f), ("hipY", 10f), ("jaw", 0.35f)));
            var p = keys.Evaluate(f);
            p["finWave"] = f * 1.1f;

            // El escudo sale despedido hacia atrás, gira en el aire y cae de plano.
            var shieldKeys = new Keyframes()
                .Key(1, new Pose().With(("sdX", 6f), ("sdY", 46f), ("sdR", 35f), ("sdF", 0f)), Ease.Linear)
                .Key(2, new Pose().With(("sdX", -14f), ("sdY", 48f), ("sdR", 70f), ("sdF", 0.05f)), Ease.In)
                .Key(3, new Pose().With(("sdX", -30f), ("sdY", 30f), ("sdR", 100f), ("sdF", 0.2f)), Ease.In)
                .Key(4, new Pose().With(("sdX", -38f), ("sdY", 9.5f), ("sdR", 93f), ("sdF", 0.42f)), Ease.Out)
                .Key(5, new Pose().With(("sdX", -40f), ("sdY", 11f), ("sdR", 86f), ("sdF", 0.45f)), Ease.In)
                .Key(6, new Pose().With(("sdX", -41f), ("sdY", 9.6f), ("sdR", 90f), ("sdF", 0.4f)));
            var s = shieldKeys.Evaluate(f);
            foreach (var k in s.Keys) p[k] = s[k];
            p["dust"] = f == 4 ? 1f : f == 5 ? 2f : 0f;
            return Draw(p);
        }

        // ------------------------------------------------------------------
        // Dibujo del personaje a partir de una pose
        // ------------------------------------------------------------------

        struct ThrustSpec
        {
            public bool On;
            public float Back, Strength;
        }

        const float Thigh = 13f, Shin = 12.5f, UpperArm = 12.5f, Forearm = 12f, HeadScale = 1.22f;

        static Vector2 ShoulderF(Pose p)
        {
            float ta = 90f - p["lean"];
            return Add(V(p["hipX"], p["hipY"]), Add(Dir(ta, 21f), Dir(ta + 90f, -3f)));
        }

        /// <summary>Centro del escudo para una pose (útil para efectos encima del escudo).</summary>
        static Vector2 ShieldCenter(Pose p)
        {
            if (p["shFree"] > 0.5f) return V(p["sdX"], p["sdY"]);
            FrontHand(p, ShoulderF(p), out var hand, out _);
            return ShieldGrip(hand, p["shR"]);
        }

        /// <summary>Del puño (tras el escudo) al centro del escudo.</summary>
        static Vector2 ShieldGrip(Vector2 hand, float rot) => Add(hand, Rotate(V(2.5f, 1.5f), V(0f, 0f), rot));

        /// <summary>
        /// Mano del escudo por IK. Si el escudo está en la mano y su charnela se hundiría en el suelo,
        /// la mano sube lo necesario (el escudo roza el suelo pero nunca lo atraviesa).
        /// </summary>
        static void FrontHand(Pose p, Vector2 shoulder, out Vector2 hand, out Vector2 elbow)
        {
            float bend = p["elbFUp"] > 0.5f ? 1f : -1f;
            hand = Add(shoulder, V(p["hFx"], p["hFy"]));
            elbow = Knee(shoulder, ref hand, UpperArm, Forearm, bend);
            if (p["shFree"] > 0.5f) return;
            float lift = 1.5f - ShieldBottom(ShieldGrip(hand, p["shR"]), p["shR"], 1f);
            if (lift <= 0f) return;
            hand = Add(hand, V(0f, lift));
            elbow = Knee(shoulder, ref hand, UpperArm, Forearm, bend);
        }

        /// <summary>Altura del punto más bajo del escudo.</summary>
        static float ShieldBottom(Vector2 center, float rot, float across)
        {
            float cr = Mathf.Cos(rot * Mathf.Deg2Rad), sr = Mathf.Sin(rot * Mathf.Deg2Rad), min = float.MaxValue;
            foreach (var o in ShieldOutline) min = Mathf.Min(min, center.y + o.x * across * sr + o.y * cr);
            return min;
        }

        ShadedCanvas Draw(Pose p, ThrustSpec thrust = default)
        {
            var c = NewFrame();
            float lean = p["lean"];
            float ta = 90f - lean;
            float slump = p["slump"];
            var hip = V(p["hipX"], p["hipY"]);
            var U = Dir(ta);
            var B = Dir(ta + 90f);
            float squash = 1f - 0.2f * slump;
            Vector2 T(float u, float v) => Add(hip, Add(Scale(U, u), Scale(B, v * squash)));

            var Skin = SkinFor(Mathf.RoundToInt(hip.x), Mathf.RoundToInt(hip.y));
            var Belly = BellyFor(Mathf.RoundToInt(hip.x), Mathf.RoundToInt(hip.y));

            int gTorso = c.NewGroup(), gNeck = c.NewGroup(), gLegF = c.NewGroup(), gLegB = c.NewGroup(), gPlates = c.NewGroup();
            int gArmF = c.NewGroup(), gArmB = c.NewGroup(), gFin = c.NewGroup(), gClothF = c.NewGroup(), gClothB = c.NewGroup();
            int gBelt = c.NewGroup(), gSpear = c.NewGroup(), gPadF = c.NewGroup(), gPadB = c.NewGroup();

            // --- Piernas (IK con rodillas hacia delante, de rana) ---
            var ankleF = V(p["fFx"], p["fFy"]);
            var ankleB = V(p["fBx"], p["fBy"]);
            var kneeF = Knee(hip, ref ankleF, Thigh, Shin, 1f - 2f * p["flipF"]);
            var kneeB = Knee(hip, ref ankleB, Thigh, Shin, 1f - 2f * p["flipB"]);

            // --- Hombros y brazos (IK hacia las manos) ---
            var shoulderF = T(21f, -3f);
            var shoulderB = T(22f, 2.5f);
            FrontHand(p, shoulderF, out var handF, out var elbowF);
            var handB = Add(shoulderB, V(p["hBx"], p["hBy"]));
            var elbowB = Knee(shoulderB, ref handB, UpperArm, Forearm, p["elbBUp"] > 0.5f ? 1f : -1f);
            bool shieldInHand = p["shFree"] < 0.5f;

            // --- Cuello y cabeza ---
            float neckA = ta - 14f + p["neck"];
            var neckBase = T(24f, -1.5f);
            var hp = Add(neckBase, Dir(neckA, 6.5f));
            float hr = -(lean - 12f) * 0.35f + p["head"] + p["neck"] * 0.5f;
            Vector2 Hd(float x, float y) => Add(hp, Rotate(V(x * HeadScale, y * HeadScale), V(0f, 0f), hr));

            // --- Polvo al caer (muerte) ---
            float dust = p["dust"];
            if (dust > 0.5f) Dust(c, V(p["sdX"], 1.5f), dust > 1.5f);

            // --- Arpón (lo empuña la mano trasera: queda detrás del cuerpo) ---
            float spear = p["spear"];
            DrawHarpoon(c, Add(handB, Dir(spear, p["slide"])), spear, 2.55f, gSpear, p["cloth"]);

            // --- Brazo y pierna traseros (lejos de la luz) ---
            DrawLeg(c, hip, kneeB, ankleB, p["fBa"], 1.5f, -0.13f, gLegB, Skin, false);
            DrawArm(c, shoulderB, elbowB, handB, 2.5f, -0.13f, gArmB, Skin, true, true, spear);
            DrawPauldron(c, shoulderB, ta, 3.5f, -0.12f, gPadB);

            // Harapo trasero (detrás de la pierna trasera)
            float sway = p["cloth"];
            DrawRag(c, T(3.5f, 1.5f), T(3.5f, 8.5f), 15f, sway * 1.4f, lean, 1.3f, -0.1f, gClothB, 23);

            // --- Aleta dorsal (detrás del tronco) ---
            DrawFin(c, new[] { Hd(-2.5f, 6f), T(24.5f, 6.5f), T(21f, 9.6f), T(17f, 11f), T(13f, 10.8f), T(9f, 9.2f) },
                    B, ta, p["fin"], p["finWave"], gFin);

            // --- Tronco: vientre, pecho ancho, joroba ---
            c.Capsule(T(0f, 0.5f), T(12f, 0f), 8.6f, 9.4f, Skin, 4f, 0f, gTorso);
            c.Ellipse(T(16f, 1.5f), 9.6f, 10.4f * squash, ta, Skin, 4f, 0f, gTorso);
            c.Ellipse(T(20.5f, 3f), 6.8f, 8f * squash, ta, Skin, 4f, 0f, gTorso);
            c.Ellipse(T(5f, -5.4f), 6.4f, 4.6f * squash, ta, Belly, 4.2f, 0f, gTorso);

            // Coraza de conchas: escamas superpuestas en el pecho (de abajo arriba, cada fila tapa la anterior)
            DrawPlate(c, T(10.5f, -6.4f), ta, 4.6f, 5.2f * squash, 4.5f, gPlates);
            DrawPlate(c, T(14.8f, -6.8f), ta, 4.8f, 5.6f * squash, 4.55f, gPlates);
            DrawPlate(c, T(19f, -5.6f), ta, 4.6f, 5.6f * squash, 4.6f, gPlates);
            DrawPlate(c, T(16.5f, -1.5f), ta, 4.4f, 4.6f * squash, 4.52f, gPlates);
            Barnacles(c, T(13f, -3.2f), 3, 1.2f, 4.7f, 31);

            // Cinturón de cuerda con conchas colgando
            c.Capsule(T(3f, -9.6f), T(3f, 8.6f), 1.3f, 1.3f, Rope, 4.8f, 0f, gBelt);
            for (int i = 0; i < 3; i++)
            {
                var at = T(2.2f, -7.5f + i * 4.6f);
                var tip = Add(at, V(0.6f - sway * 0.8f, -3.4f - i % 2));
                c.Capsule(at, tip, 0.5f, 0.5f, Rope, 4.85f, 0f, gBelt);
                c.Ellipse(Add(tip, V(0f, -1.4f)), 1.7f, 1.9f, 0f, ShellMat, 4.9f, 0f, gBelt, 0.2f);
            }

            // Cuello y garganta pálida
            c.Capsule(T(20f, -0.5f), hp, 7f, 6f, Skin, 5f, 0f, gNeck);
            c.Capsule(T(21f, -5.2f), Hd(1.6f, -4.4f), 2.6f, 2.4f, Belly, 5.1f, 0f, gNeck);

            // Pierna delantera y harapo delantero (cuelga por delante del muslo)
            DrawLeg(c, hip, kneeF, ankleF, p["fFa"], 6f, 0f, gLegF, Skin, true);
            DrawRag(c, T(3.5f, -8.5f), T(3.5f, -1f), 13f, sway, lean, 6.5f, 0f, gClothF, 7);

            // Cabeza
            DrawHead(c, hp, hr, p, Skin, Belly);

            // --- Brazo delantero (la mano queda tras el escudo) y hombrera ---
            DrawArm(c, shoulderF, elbowF, handF, 9f, 0f, gArmF, Skin, !shieldInHand, false, 0f);
            DrawPauldron(c, shoulderF, ta, 9.6f, 0f, gPadF);

            // --- El escudo ---
            if (shieldInHand)
            {
                DrawShield(c, ShieldGrip(handF, p["shR"]), p["shR"], 1f, 11f);
            }
            else
            {
                // Suelto (muerte): sale despedido por detrás del cuerpo
                DrawShield(c, V(p["sdX"], p["sdY"]), p["sdR"], 1f - p["sdF"], 0.4f);
            }

            // --- Estela de la lanzada ---
            if (thrust.On)
            {
                var tip = Add(Add(handB, Dir(spear, p["slide"])), Dir(spear, HarpoonTip));
                DrawThrustSmear(c, tip, spear, thrust);
            }

            return c;
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
            // Muslo grueso de rana, espinilla con greba de concha y pie largo palmeado.
            c.Capsule(hip, knee, 6f, 4.2f, skin, z, shade, group);
            c.Capsule(knee, ankle, 3.9f, 2.3f, skin, z + 0.02f, shade, group);
            // Greba: una concha alargada atada sobre la espinilla
            var sAxis = Sub(ankle, knee);
            float sLen = Mathf.Max(0.01f, Mathf.Sqrt(sAxis.x * sAxis.x + sAxis.y * sAxis.y));
            sAxis = Scale(sAxis, 1f / sLen);
            var sSide = V(sAxis.y, -sAxis.x); // hacia delante (la espinilla mira atrás en las patas de rana)
            if (sSide.x < 0f) sSide = Scale(sSide, -1f);
            var g0 = Add(Add(knee, Scale(sAxis, 1.5f)), Scale(sSide, 1.6f));
            var g1 = Add(Add(knee, Scale(sAxis, sLen * 0.78f)), Scale(sSide, 1f));
            c.Capsule(g0, g1, 2.5f, 1.6f, ShellMat, z + 0.03f, shade - 0.08f, group);
            c.Capsule(Mix(g0, g1, 0.25f), Mix(g0, g1, 0.6f), 0.5f, 0.5f, ShellMat, z + 0.035f, shade - 0.3f, group);
            if (front) Barnacles(c, Add(knee, V(1.5f, 1.8f)), 2, 1.1f, z + 0.04f, 13);

            // Pie: metatarso hasta la bola, dedos palmeados y uñas.
            var ball = Add(ankle, Dir(footA, FootLen));
            c.Capsule(ankle, ball, 2.5f, 1.7f, skin, z + 0.03f, shade, group);
            float toeA = footA + 52f;
            float[] spread = { -4f, 9f, 22f };
            float[] lenT = { 6.2f, 5.6f, 4.4f };
            var web = new List<Vector2> { ball };
            for (int i = 0; i < 3; i++)
            {
                float ta2 = toeA + spread[i];
                // Los dedos nunca atraviesan el suelo: se aplanan al apoyarse.
                float sinMin = (0.9f - ball.y) / lenT[i];
                if (Mathf.Sin(ta2 * Mathf.Deg2Rad) < sinMin) ta2 = Mathf.Asin(Mathf.Clamp(sinMin, -1f, 1f)) * Mathf.Rad2Deg;
                var dir = Dir(ta2);
                var tip = Add(ball, Scale(dir, lenT[i]));
                c.Capsule(ball, tip, 1.2f, 0.8f, skin, z + 0.04f + i * 0.001f, shade, group);
                c.Capsule(tip, Add(tip, Dir(ta2 - (tip.y < 2f ? 0f : 25f), 2f)), 0.8f, 0.3f, ClawMat, z + 0.045f, shade, group);
                web.Add(Add(ball, Scale(dir, lenT[i] * 0.85f)));
            }
            c.Poly(web.ToArray(), FinMat, z + 0.035f, 1f, shade, group);
        }

        /// <summary>Brazo con aleta espinosa en el antebrazo; la mano empuña el arpón o queda libre (garra).</summary>
        void DrawArm(ShadedCanvas c, Vector2 shoulder, Vector2 elbow, Vector2 hand, float z, float shade, int group, PixelMaterial skin,
                     bool showHand, bool fist, float fistAngle)
        {
            c.Capsule(shoulder, elbow, 4.4f, 3.3f, skin, z, shade, group);
            c.Capsule(elbow, hand, 3.4f, 2.7f, skin, z + 0.05f, shade, group);
            // Percebes incrustados en el codo
            Barnacles(c, Mix(elbow, hand, 0.12f), 2, 1f, z + 0.07f, group * 5 + 1);
            // Espinas membranosas en el antebrazo (lado de fuera)
            var fa = Sub(hand, elbow);
            float flen = Mathf.Max(0.01f, Mathf.Sqrt(fa.x * fa.x + fa.y * fa.y));
            fa = Scale(fa, 1f / flen);
            var outSide = V(fa.y, -fa.x);
            if (outSide.x > 0.3f) outSide = Scale(outSide, -1f);
            var f0 = Add(elbow, Scale(fa, 1.5f));
            var f1 = Add(elbow, Scale(fa, flen * 0.65f));
            var t0 = Add(Add(f0, Scale(outSide, 5.6f)), Scale(fa, -2f));
            var t1 = Add(Add(f1, Scale(outSide, 4.2f)), Scale(fa, -0.8f));
            c.Poly(new[] { f0, t0, Mix(Mix(f0, t0, 0.6f), Mix(f1, t1, 0.6f), 0.5f), t1, f1 }, FinMat, z - 0.02f, 1f, shade, group);
            c.Capsule(f0, t0, 0.8f, 0.3f, SpineMat, z - 0.01f, shade, group);
            c.Capsule(f1, t1, 0.7f, 0.3f, SpineMat, z - 0.01f, shade, group);
            if (!showHand) return;
            if (fist)
                DrawFist(c, hand, fistAngle, z + 0.1f, shade, group, skin);
            else
            {
                // Garra libre: si está a ras de suelo, la palma se apoya plana en vez de clavarse.
                float clawA = Mathf.Atan2(fa.y, fa.x) * Mathf.Rad2Deg;
                if (hand.y < 11f && clawA < 0f && clawA > -90f) clawA = Mathf.Lerp(clawA, -6f, Mathf.Clamp01((11f - hand.y) / 5f));
                DrawClaw(c, hand, clawA, 0.4f, 1f, z + 0.1f, shade, group, skin);
            }
        }

        /// <summary>Puño cerrado sobre el asta: palma y tres dedos que la rodean, con las uñas asomando.</summary>
        void DrawFist(ShadedCanvas c, Vector2 hand, float shaft, float z, float shade, int group, PixelMaterial skin)
        {
            var axis = Dir(shaft);
            var side = Dir(shaft + 90f);
            Vector2 At(float a, float s) => Add(hand, Add(Scale(axis, a), Scale(side, s)));
            c.Ellipse(hand, 3.4f, 3f, shaft, skin, z, shade, group);
            for (int i = 0; i < 3; i++)
            {
                float a = -2.2f + i * 2.2f;
                c.Capsule(At(a, 2.6f), At(a + 0.4f, -2.2f), 1.25f, 1.05f, skin, z + 0.2f + i * 0.001f, shade, group);
                c.Capsule(At(a + 0.4f, -2.2f), At(a + 1.2f, -3.6f), 0.8f, 0.3f, ClawMat, z + 0.21f, shade, group);
            }
        }

        /// <summary>Mano palmeada abierta: palma, tres dedos con membrana y garras curvas.</summary>
        void DrawClaw(ShadedCanvas c, Vector2 wristPt, float ang, float grip, float palm, float z, float shade, int group, PixelMaterial skin)
        {
            var palmC = Add(wristPt, Dir(ang, 2.4f));
            c.Ellipse(palmC, 3.6f, 2.9f, ang, skin, z, shade, group);
            float[] spread = { -28f, 0f, 26f };
            float[] len1 = { 4.4f, 5.4f, 4.6f };
            float curl = palm * (20f + 50f * grip);
            var web = new List<Vector2> { Add(palmC, Dir(ang - 90f, 2.2f)) };
            for (int i = 0; i < 3; i++)
            {
                float fa = ang + spread[i] * (1f - 0.55f * grip);
                var k0 = Add(palmC, Dir(fa, 2.2f));
                var k1 = Add(k0, Dir(fa + curl * 0.35f, len1[i]));
                var tip = Add(k1, Dir(fa + curl, 4.4f));
                c.Capsule(k0, k1, 1.35f, 1.1f, skin, z + 0.02f, shade, group);
                c.Capsule(k1, tip, 1.1f, 0.3f, ClawMat, z + 0.03f, shade, group);
                web.Add(Mix(k0, k1, 0.92f));
            }
            web.Add(Add(palmC, Dir(ang + 90f, 2.2f)));
            c.Poly(web.ToArray(), FinMat, z + 0.01f, 1f, shade, group);
        }

        /// <summary>Hombrera: una lapa gigante (cono de concha con estrías) cubierta de percebes.</summary>
        void DrawPauldron(ShadedCanvas c, Vector2 shoulder, float ta, float z, float shade, int group)
        {
            var center = Add(shoulder, Dir(ta, 1.4f));
            // Cono de la lapa: base ancha sobre el hombro, ápice alto
            var apex = Add(center, Add(Dir(ta, 4.4f), Dir(ta + 90f, 1.2f)));
            var baseF = Add(center, Add(Dir(ta, -3.4f), Dir(ta + 90f, -7.2f)));
            var baseB = Add(center, Add(Dir(ta, -2.4f), Dir(ta + 90f, 6.6f)));
            c.Poly(new[] { apex, Add(Mix(apex, baseB, 0.55f), Dir(ta + 90f, 1.6f)), baseB, Add(Mix(baseB, baseF, 0.5f), Dir(ta, -2.4f)), baseF,
                           Add(Mix(apex, baseF, 0.5f), Dir(ta + 90f, -1.6f)) }, ShellMat, z, 2.4f, shade, group, 0.1f, 0.25f);
            // Estrías radiales desde el ápice (alternan luz y sombra)
            for (int i = 0; i < 6; i++)
            {
                var foot = Mix(baseF, baseB, (i + 0.5f) / 6f);
                foot = Add(foot, Dir(ta, -1.4f * Mathf.Sin((i + 0.5f) / 6f * Mathf.PI)));
                c.Capsule(Mix(apex, foot, 0.2f), Mix(apex, foot, 0.92f), 0.4f, 0.7f, ShellMat, z + 0.01f, shade + (i % 2 == 0 ? 0.22f : -0.25f), group);
            }
            c.Ellipse(apex, 1.2f, 1.1f, 0f, ShellMat, z + 0.02f, shade + 0.2f, group);
            Barnacles(c, Add(center, Add(Dir(ta, -1.2f), Dir(ta + 90f, 4.6f))), 2, 1.1f, z + 0.03f, group * 7 + 3);
        }

        /// <summary>Placa de la coraza: concha con borde inferior claro y estrías.</summary>
        void DrawPlate(ShadedCanvas c, Vector2 center, float ta, float rx, float ry, float z, int group)
        {
            c.Ellipse(center, rx, ry, ta, ShellMat, z, 0f, 0, 0.15f);
            c.Capsule(Add(center, Dir(ta, rx * 0.2f)), Add(center, Dir(ta, -rx * 0.7f)), 0.45f, 0.45f, ShellMat, z + 0.001f, -0.22f, 0);
        }

        /// <summary>Racimo de percebes: conos pálidos con el agujero oscuro.</summary>
        static void Barnacles(ShadedCanvas c, Vector2 at, int count, float size, float z, int seed)
        {
            for (int i = 0; i < count; i++)
            {
                float ang = PixelCanvas.Hash(i, seed, 1) * 360f;
                float dist = i == 0 ? 0f : size * (1.3f + PixelCanvas.Hash(i, seed, 2) * 0.9f);
                var pos = Add(at, Dir(ang, dist));
                float r = size * (0.85f + PixelCanvas.Hash(i, seed, 3) * 0.5f);
                c.Ellipse(pos, r, r * 0.9f, 0f, BarnacleMat, z + i * 0.002f);
                c.Ellipse(Add(pos, V(0.2f, 0.4f)), Mathf.Max(0.55f, r * 0.42f), Mathf.Max(0.5f, r * 0.32f), 0f, BarnacleHole, z + 0.001f + i * 0.002f);
            }
        }

        /// <summary>Harapo de algas/arpillera colgando del cinturón, con borde roto y vaivén.</summary>
        void DrawRag(ShadedCanvas c, Vector2 a, Vector2 b, float length, float sway, float lean, float z, float shade, int group, int seed)
        {
            // Cuelgan hacia abajo; si el cuerpo cae tendido, quedan extendidos por el suelo.
            var down = Rotate(V(0f, -1f), V(0f, 0f), -Mathf.Clamp(lean, 0f, 95f) * 0.85f);
            Vector2 Floor(Vector2 v) => V(v.x, Mathf.Max(v.y, 0.8f));
            // Tres tiras de arpillera y algas de distinto largo; cada una se mece con su propio retraso.
            const int strips = 3;
            for (int i = 0; i < strips; i++)
            {
                float t0 = i / (float)strips, t1 = (i + 1) / (float)strips;
                var top0 = Mix(a, b, t0 - (i > 0 ? 0.08f : 0f));
                var top1 = Mix(a, b, t1);
                float len = length * (0.7f + 0.45f * PixelCanvas.Hash(i, seed, 1));
                float sw = sway * (1f + 0.35f * i);
                var mid = Floor(Add(Add(Mix(top0, top1, 0.5f), Scale(down, len * 0.55f)), V(-sw * 1.2f, 0f)));
                var end = Floor(Add(Add(Mix(top0, top1, 0.5f), Scale(down, len)), V(-sw * 3f - 0.4f * i, 0f)));
                float half = Mathf.Sqrt((top1.x - top0.x) * (top1.x - top0.x) + (top1.y - top0.y) * (top1.y - top0.y)) * 0.5f;
                var across = V(-down.y, down.x);
                var pts = new List<Vector2> { top0, Floor(Add(mid, Scale(across, half * 0.9f))), Floor(Add(Add(end, Scale(across, half * 0.5f)), Scale(down, -1.2f))), end,
                                              Floor(Add(Add(end, Scale(across, -half * 0.4f)), Scale(down, -2.2f))), Floor(Add(mid, Scale(across, -half * 0.8f))), top1 };
                c.Poly(pts.ToArray(), Cloth, z + i * 0.01f, 1.4f, shade - 0.04f * (i % 2), group, 0.1f, 0f);
            }
        }

        void DrawHead(ShadedCanvas c, Vector2 hp, float hr, Pose p, PixelMaterial skin, PixelMaterial belly)
        {
            int gHead = c.NewGroup(), gJaw = c.NewGroup(), gMouth = c.NewGroup(), gEye = c.NewGroup(), gFar = c.NewGroup(), gGill = c.NewGroup(), gCheek = c.NewGroup();
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

            // Aleta de la mejilla: abanico espinoso tras la mandíbula
            float wave = p["finWave"];
            var cb = H(-1.5f, -1.5f);
            var cheek = new List<Vector2> { H(0.5f, 1f) };
            for (int i = 0; i < 4; i++)
            {
                float a = hr + 200f + i * 22f + 5f * Mathf.Sin(wave - i * 0.8f);
                cheek.Add(Add(cb, Dir(a, 6.5f - i * 0.6f)));
            }
            cheek.Add(H(0f, -3.5f));
            c.Poly(cheek.ToArray(), FinMat, 7.3f, 1.2f, -0.05f, gCheek, -0.2f, 0.1f);
            for (int i = 0; i < 4; i++)
            {
                float a = hr + 200f + i * 22f + 5f * Mathf.Sin(wave - i * 0.8f);
                c.Capsule(cb, Add(cb, Dir(a, 6.8f - i * 0.6f)), 0.7f, 0.3f, SpineMat, 7.31f, -0.05f, gCheek);
            }

            // Ojo lejano: asoma por encima del cráneo
            var eyeFar = H(5.4f, 8.2f);
            c.Ellipse(eyeFar, 3f * k, 2.8f * k, hr, SkinDark, 7.4f, -0.1f, gFar);
            c.Ellipse(Add(eyeFar, V(0.5f, 0.6f)), 1.8f * k, 1.6f * k, 0f, dull ? EyeDull : hot ? EyeMid : EyeRing, 7.45f, 0f, gFar);

            // Fauces: interior oscuro y mandíbula inferior pálida (prognata)
            c.Ellipse(H(7f, -2.6f), 5.6f * k, (1.2f + jaw * 3.2f) * k, hr - jaw * 14f, MouthIn, 7.8f, 0f, gMouth);
            c.Poly(new[] { J(-1.5f, 1.2f), J(6f, 0.8f), J(12.4f, 0.6f), J(13.8f, -0.6f), J(12.6f, -2.8f), J(6.5f, -4.8f), J(0f, -4.5f), J(-2.6f, -2f) },
                   belly, 7.9f, 2f, 0f, gJaw);

            // Cráneo y hocico
            c.Ellipse(H(4.2f, 2.6f), 7.6f * k, 6.1f * k, hr - 8f, skin, 8f, 0f, gHead);
            c.Ellipse(H(10.2f, 0.6f), 4.6f * k, 3.3f * k, hr - 6f, skin, 8.02f, 0f, gHead);
            // Ceja ósea (veterano): un reborde sobre el ojo
            c.Capsule(H(4.6f, 8.2f), H(10.4f, 7f), 1.3f, 0.9f, skin, 8.05f, 0.08f, gHead);
            // Comisura: línea de la boca, ancha y caída
            if (jaw < 0.3f)
                c.Strand(Bezier(H(1f, -2.6f), H(6f, -1.2f), H(13.2f, -0.9f), 5), 0.55f, 0.45f, MouthIn, 8.05f, 0f, gMouth);
            else
                c.Capsule(H(0.6f, -2.8f), H(2.4f, -2f), 0.6f, 0.5f, MouthIn, 8.05f, 0f, gMouth);

            // Branquias
            float gill = p["gill"];
            for (int i = 0; i < 3; i++)
            {
                var a = H(0.6f - i * 1.9f, -4f + i * 0.4f);
                var m = H(1.6f - i * 1.9f, -1f);
                var b = H(-0.2f - i * 1.9f, 2f - i * 0.5f);
                c.Strand(Bezier(a, m, b, 4), 0.35f + gill * 0.4f, 0.3f + gill * 0.25f, GillMat, 8.15f, 0f, gGill);
            }

            // Percebes incrustados en el cráneo
            Barnacles(c, H(-0.5f, 6.8f), 3, 1.2f, 8.5f, 57);
            Barnacles(c, H(3.6f, 9f), 1, 1.1f, 8.5f, 91);

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
            float er = 2.9f * k + 0.6f * Mathf.Max(0f, eye);
            c.Ellipse(E, er + 1.1f, er + 0.9f, hr, SkinDark, 8.2f, 0f, gEye);
            c.Ellipse(E, er, er - 0.1f, 0f, dull ? EyeDull : hot ? EyeRingHot : EyeRing, 8.3f, 0f, gEye);
            c.Ellipse(Add(E, V(0.2f, 0.3f)), er - 0.9f, er - 1f, 0f, dull ? EyeDullMid : hot ? EyeMidHot : EyeMid, 8.31f, 0f, gEye);
            if (!dull) c.Ellipse(Add(E, V(-0.6f, 1f)), 1f + 0.5f * Mathf.Max(0f, eye), 0.8f + 0.3f * Mathf.Max(0f, eye), 0f, hot ? EyeCoreHot : EyeCore, 8.32f, 0f, gEye);
            float pr = dull ? 1.6f : 1.2f - 0.4f * Mathf.Max(0f, eye);
            c.Ellipse(Add(E, Rotate(V(1f, -0.2f), V(0f, 0f), hr)), pr, pr, 0f, Pupil, 8.4f, 0f, gEye);
            if (eye > 0.95f)
            {
                // Destello del telegrafiado: cruz de luz alrededor del ojo
                var glint = PixelCanvas.Hex("fff6b8");
                float g = er + 2.5f;
                c.Decal(E.x - 0.5f, E.y + g, glint);
                c.Decal(E.x - 0.5f, E.y + g + 1f, glint);
                c.Decal(E.x + g, E.y, glint);
                c.Decal(E.x + g + 1f, E.y, glint);
                c.Decal(E.x - g - 1f, E.y, glint);
            }
        }

        /// <summary>Aleta dorsal: espinas óseas unidas por membrana, con ondulación desfasada (movimiento secundario).</summary>
        void DrawFin(ShadedCanvas c, Vector2[] bases, Vector2 back, float ta, float flare, float wave, int group)
        {
            int n = bases.Length;
            float[] lens = { 7f, 11f, 13.5f, 12.5f, 9.5f, 6.5f };
            float[] sweeps = { -14f, -22f, -16f, -6f, 6f, 18f };
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
                c.Capsule(Sub(bases[i], Scale(back, 1.5f)), tips[i], 1.1f, 0.4f, SpineMat, 2.1f, 0f, group);
        }

        // ------------------------------------------------------------------
        // El arpón
        // ------------------------------------------------------------------

        const float HarpoonButt = 27f, HarpoonTip = 47f;

        /// <summary>
        /// Arpón largo: asta de madera de deriva con ligaduras de cuerda, coral y percebes crecidos en el tercio
        /// superior, cubo de hierro y punta dentada con lengüetas. <paramref name="grip"/> es el punto del puño.
        /// </summary>
        void DrawHarpoon(ShadedCanvas c, Vector2 grip, float angle, float z, int group, float sway)
        {
            var axis = Dir(angle);
            var side = Dir(angle + 90f);
            Vector2 At(float a, float s) => Add(grip, Add(Scale(axis, a), Scale(side, s)));

            // Asta
            c.Capsule(At(-HarpoonButt, 0f), At(31f, 0f), 1.35f, 1.2f, Wood, z, 0f, group);
            c.Ellipse(At(-HarpoonButt - 0.5f, 0f), 1.8f, 1.8f, 0f, Iron, z + 0.01f, 0f, group);
            // Ligaduras de cuerda
            foreach (var a in new[] { -6.5f, 6f, 19f })
                c.Capsule(At(a - 1.2f, 0f), At(a + 1.2f, 0f), 1.75f, 1.75f, Rope, z + 0.02f, 0f, group);
            // Coral ramificado y percebes crecidos sobre el asta
            c.Strand(Bezier(At(27f, 0.8f), At(26f, 3.4f), At(23.5f, 4.8f), 4), 1.05f, 0.6f, CoralMat, z + 0.03f, 0f, group);
            c.Strand(Bezier(At(26f, 2.8f), At(27.5f, 4.4f), At(28f, 6f), 3), 0.75f, 0.5f, CoralMat, z + 0.031f, 0f, group);
            c.Strand(Bezier(At(25f, -0.8f), At(24.5f, -3.2f), At(22.5f, -4.4f), 4), 0.95f, 0.55f, CoralMat, z + 0.03f, 0f, group);
            c.Ellipse(At(23.2f, 5.1f), 0.95f, 0.95f, 0f, CoralMat, z + 0.032f, 0.1f, group);
            c.Ellipse(At(28.2f, 6.3f), 0.85f, 0.85f, 0f, CoralMat, z + 0.032f, 0.1f, group);
            Barnacles(c, At(22f, 0.8f), 2, 0.95f, z + 0.035f, 77);
            // Cubo de hierro
            c.Capsule(At(29f, 0f), At(33.5f, 0f), 2f, 1.5f, Iron, z + 0.04f, 0f, group);
            // Punta: hoja estrecha con dos pares de lengüetas hacia atrás
            c.Poly(new[] { At(33f, -1.7f), At(40f, -1.4f), At(HarpoonTip, 0f), At(40f, 1.4f), At(33f, 1.7f) }, Iron, z + 0.05f, 1.2f, 0.05f, group);
            c.Poly(new[] { At(38f, 1f), At(34f, 4.6f), At(35.5f, 1.4f) }, Iron, z + 0.06f, 0.8f, 0f, group);
            c.Poly(new[] { At(38f, -1f), At(34f, -4.6f), At(35.5f, -1.4f) }, Iron, z + 0.06f, 0.8f, -0.05f, group);
            c.Poly(new[] { At(43f, 0.6f), At(40.5f, 3f), At(41f, 0.8f) }, Iron, z + 0.06f, 0.6f, 0f, group);
            // Cabo deshilachado colgando del cubo (movimiento secundario)
            var r0 = At(29.5f, 0f);
            var hang = V(-1.5f - sway * 2.5f, -4.5f);
            c.Strand(Bezier(r0, Add(r0, V(hang.x * 0.4f + 1.5f, hang.y * 0.6f)), Add(r0, hang), 4), 0.75f, 0.6f, Rope, z - 0.01f, 0f, group);
        }

        /// <summary>Estela de la lanzada: un haz recto detrás de la punta, grueso delante y fino detrás, y dos líneas de velocidad.</summary>
        void DrawThrustSmear(ShadedCanvas c, Vector2 tip, float angle, ThrustSpec s)
        {
            var axis = Dir(angle);
            var side = Dir(angle + 90f);
            float len = s.Back;
            var tail = Sub(tip, Scale(axis, len));
            float w = s.Strength >= 1f ? 3.2f : 2.2f;
            Streak(c, Add(tail, Scale(side, 0f)), tip, len, w + 1.4f, SmearSoft, 12f);
            Streak(c, Add(tail, Scale(axis, len * 0.15f)), Sub(tip, Scale(axis, 0.5f)), len * 0.85f, w * 0.7f, SmearMid, 12.05f);
            Streak(c, Add(tail, Scale(axis, len * 0.4f)), Sub(tip, Scale(axis, 1f)), len * 0.6f, w * 0.35f, SmearCore, 12.1f);
            // Líneas de velocidad
            for (int i = -1; i <= 1; i += 2)
            {
                var o = Scale(side, i * (w + 3.2f));
                var a = Add(Add(tail, Scale(axis, len * 0.1f)), o);
                var b = Add(Sub(tip, Scale(axis, len * (i < 0 ? 0.35f : 0.2f))), o);
                Streak(c, a, b, len * 0.6f, 0.9f, SmearMid, 12.02f);
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
                // Fina en la cola, gruesa cerca de la punta y redondeada al final.
                float th = Mathf.Lerp(0.3f, width, t * t) * (t > 0.92f ? Mathf.Sqrt((1f - t) / 0.08f) : 1f);
                return (d <= th, N3.Front);
            }, m, z);
        }

        // ------------------------------------------------------------------
        // El escudo: vieira gigante
        // ------------------------------------------------------------------

        const int RibCount = 9;
        const float RimA = -26f, RimB = 206f;          // arco del borde (grados, elipse del borde)
        static readonly Vector2 RimCenter = new Vector2(0f, 1.5f);
        const float RimRx = 15f, RimRy = 21.5f;
        static readonly Vector2 Umbo = new Vector2(0f, -19f); // charnela: de aquí nacen las costillas
        static readonly Vector2[] ShieldOutline = BuildOutline();
        static readonly float[] RibAngles = BuildRibAngles();

        static float RibPhase(float theta) => (theta - RimA) / (RimB - RimA) * RibCount - 0.5f;

        static Vector2 RimPoint(float theta, float scale) =>
            V(RimCenter.x + RimRx * Mathf.Cos(theta * Mathf.Deg2Rad) * scale, RimCenter.y + RimRy * Mathf.Sin(theta * Mathf.Deg2Rad) * scale);

        static Vector2[] BuildOutline()
        {
            var pts = new List<Vector2> { V(0f, -23.4f), V(7.6f, -23f), V(9.4f, -19.6f), V(5.6f, -17f) };
            const int samples = 96;
            for (int i = 0; i <= samples; i++)
            {
                float th = Mathf.Lerp(RimA, RimB, i / (float)samples);
                float ph = RibPhase(th);
                float fr = ph - Mathf.Round(ph);
                // Borde festoneado: cada costilla empuja el borde hacia fuera.
                float lobe = Mathf.Cos(fr * Mathf.PI);
                pts.Add(RimPoint(th, 0.95f + 0.05f * lobe * lobe));
            }
            pts.AddRange(new[] { V(-5.6f, -17f), V(-8.6f, -19.4f), V(-7f, -23f) });
            return pts.ToArray();
        }

        static float[] BuildRibAngles()
        {
            var a = new float[RibCount];
            for (int i = 0; i < RibCount; i++)
            {
                float th = RimA + (RimB - RimA) * (i + 0.5f) / RibCount;
                var r = RimPoint(th, 1f);
                a[i] = Mathf.Atan2(r.y - Umbo.y, r.x - Umbo.x) * Mathf.Rad2Deg;
            }
            return a;
        }

        static bool InsideOutline(float u, float w)
        {
            bool inside = false;
            var p = ShieldOutline;
            for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
            {
                if ((p[i].y > w) != (p[j].y > w) && u < (p[j].x - p[i].x) * (w - p[i].y) / (p[j].y - p[i].y) + p[i].x)
                    inside = !inside;
            }
            return inside;
        }

        /// <summary>Índice fraccionario de costilla (0..RibCount-1) para un ángulo visto desde la charnela.</summary>
        static float RibIndex(float phi)
        {
            var a = RibAngles;
            if (phi <= a[0]) return (phi - a[0]) / (a[1] - a[0]);
            for (int i = 0; i < a.Length - 1; i++)
                if (phi <= a[i + 1]) return i + (phi - a[i]) / (a[i + 1] - a[i]);
            int n = a.Length - 1;
            return n + (phi - a[n]) / (a[n] - a[n - 1]);
        }

        /// <summary>Relieve de la concha en un punto (coordenadas del escudo, w hacia arriba).</summary>
        struct ShellSample
        {
            public float Value;   // luz extra "pintada" (degradado de volumen, cresta, surco)
            public float Bump;    // inclinación de la normal hacia el flanco de la costilla
            public float Tx, Ty;  // dirección tangencial (de una costilla a la siguiente)
        }

        static ShellSample SampleShell(float u, float w)
        {
            var r = new ShellSample();
            // Degradado de volumen: arriba-delante más claro, abajo-detrás más oscuro.
            float grad = 0.11f * (u / RimRx) + 0.13f * ((w - 1f) / RimRy);
            float du = u - Umbo.x, dw = w - Umbo.y;
            float rho = Mathf.Sqrt(du * du + dw * dw);
            if (w < -16.2f && Mathf.Abs(u) > 4.4f)
            {
                // Orejas de la charnela: lisas, separadas del abanico por un surco
                r.Value = Mathf.Abs(u) < 5.5f ? grad - 0.3f : grad + 0.02f + 0.04f * Mathf.Clamp01((w + 23f) / 6f);
                return r;
            }
            if (rho < 6.5f)
            {
                r.Value = grad + 0.04f; // umbo liso
                return r;
            }
            float phi = Mathf.Atan2(dw, du) * Mathf.Rad2Deg;
            float s = RibIndex(phi);
            float fr = s - Mathf.Round(s);
            const float half = 0.33f;
            r.Tx = -Mathf.Sin(phi * Mathf.Deg2Rad);
            r.Ty = Mathf.Cos(phi * Mathf.Deg2Rad);
            if (Mathf.Abs(fr) > half)
            {
                r.Value = grad - 0.3f; // surco
                return r;
            }
            float q = fr / half;
            r.Bump = q * 0.42f;
            r.Value = grad + 0.06f * (1f - q * q);
            // Líneas de crecimiento concéntricas sobre las costillas
            if (Mathf.Repeat(rho, 6.5f) < 0.75f && rho > 10f) r.Value -= 0.08f;
            return r;
        }

        /// <summary>
        /// Escudo de vieira: cúpula de nácar con 9 costillas que nacen de la charnela (con sus "orejas"), borde
        /// festoneado, líneas de crecimiento, percebes en la parte baja y el sello de Dagón tallado.
        /// <paramref name="across"/> &lt; 1 lo aplasta (visto de canto o tirado en el suelo).
        /// </summary>
        void DrawShield(ShadedCanvas c, Vector2 center, float rot, float across, float z)
        {
            int g = c.NewGroup();
            float cr = Mathf.Cos(rot * Mathf.Deg2Rad), sr = Mathf.Sin(rot * Mathf.Deg2Rad);
            across = Mathf.Max(0.12f, across);
            Vector2 S(float u, float w) => Add(center, V(u * across * cr - w * sr, u * across * sr + w * cr));
            var nacre = NacreFor(center, cr, sr, across);

            // La normal solo lleva una cúpula suave y el relieve de las costillas (para las luces 2D);
            // el volumen principal va "pintado" en la textura del nácar.
            c.Custom(center.x - 25f, center.y - 25f, center.x + 25f, center.y + 25f, (px, py) =>
            {
                float dx = px - center.x, dy = py - center.y;
                float u = (dx * cr + dy * sr) / across, w = -dx * sr + dy * cr;
                if (!InsideOutline(u, w)) return (false, N3.Front);
                var sh = SampleShell(u, w);
                float nx = u / RimRx * 0.15f + sh.Tx * sh.Bump;
                float ny = w / RimRy * 0.15f + sh.Ty * sh.Bump;
                return (true, new N3(nx * cr - ny * sr, nx * sr + ny * cr, 1f).Normalized());
            }, nacre, z, 0f, g);

            // Sello de Dagón tallado: anillo y tridente (surco en sombra con un labio que recoge la luz debajo)
            var sealC = V(0f, 4.5f);
            const float sealR = 5.4f;
            void Groove(Vector2 a, Vector2 b)
            {
                c.Capsule(S(a.x, a.y), S(b.x, b.y), 0.5f, 0.5f, nacre, z + 0.05f, -0.34f, g, 1f);
                var o = Rotate(V(0.5f, -0.8f), V(0f, 0f), rot);
                c.Capsule(Add(S(a.x, a.y), o), Add(S(b.x, b.y), o), 0.45f, 0.45f, nacre, z + 0.045f, 0.2f, g, 1f);
            }
            for (int i = 0; i < 16; i++)
            {
                float a0 = i / 16f * 360f, a1 = (i + 1) / 16f * 360f;
                Groove(Add(sealC, Dir(a0, sealR)), Add(sealC, Dir(a1, sealR)));
            }
            Groove(Add(sealC, V(0f, -3.6f)), Add(sealC, V(0f, 3.4f)));
            Groove(Add(sealC, V(-2.8f, 3f)), Add(sealC, V(-2.5f, 0.2f)));
            Groove(Add(sealC, V(-2.5f, 0.2f)), Add(sealC, V(0f, -1f)));
            Groove(Add(sealC, V(2.8f, 3f)), Add(sealC, V(2.5f, 0.2f)));
            Groove(Add(sealC, V(2.5f, 0.2f)), Add(sealC, V(0f, -1f)));

            // Percebes en la parte baja
            if (across > 0.5f)
            {
                Barnacles(c, S(-8.5f, -9f), 4, 1.3f, z + 0.08f, 5);
                Barnacles(c, S(5.5f, -12.5f), 2, 1.1f, z + 0.08f, 8);
                Barnacles(c, S(-12.5f, 0.5f), 2, 1f, z + 0.08f, 12);
            }
        }

        /// <summary>
        /// Nácar del escudo: el volumen "pintado" de <see cref="SampleShell"/> más manchas suaves que desplazan el
        /// tono por la rampa iridiscente (rosa, blanco y verde en las luces).
        /// </summary>
        static PixelMaterial NacreFor(Vector2 center, float cr, float sr, float across) => new PixelMaterial(NacreRamp)
        {
            Ambient = 0.1f, Rim = 0.4f, Gloss = 0.1f, Dither = 0f,
            Texture = (x, y) =>
            {
                float px = x + 0.5f - PivotX - center.x, py = y + 0.5f - PivotY - center.y;
                float u = (px * cr + py * sr) / across, w = -px * sr + py * cr;
                float iri = (PixelCanvas.ValueNoise(u / 7f, w / 7f, 0, 2027) - 0.5f) * 0.12f;
                return SampleShell(u, w).Value + iri;
            },
        };

        static readonly PixelMaterial QuiverMat = new PixelMaterial(new[] { new Color32(236, 226, 232, 210) }) { Emissive = true, Outline = false, ReceivesContactShadow = false };
        static readonly PixelMaterial ChipMat = PixelMaterial.Glow("f2e4e8");

        /// <summary>
        /// Golpe parado: líneas de vibración que siguen el borde del escudo (fuera del canto) y esquirlas de nácar.
        /// </summary>
        static void ShieldImpact(ShadedCanvas c, Vector2 center, float rot, int f)
        {
            Vector2 W(Vector2 l) => Add(center, Rotate(l, V(0f, 0f), rot));
            void Arc(float from, float to, float scale, float width)
            {
                const int n = 6;
                for (int i = 0; i < n; i++)
                {
                    float t0 = Mathf.Lerp(from, to, i / (float)n), t1 = Mathf.Lerp(from, to, (i + 1) / (float)n);
                    float taper = Mathf.Sin((i + 0.5f) / n * Mathf.PI);
                    c.Capsule(W(RimPoint(t0, scale)), W(RimPoint(t1, scale)), width * taper, width * taper, QuiverMat, 13f);
                }
            }
            if (f == 0)
            {
                Arc(-20f, 50f, 1.16f, 0.75f);
                Arc(-5f, 35f, 1.3f, 0.6f);
            }
            else
            {
                Arc(0f, 40f, 1.22f, 0.6f);
                Arc(140f, 180f, 1.18f, 0.55f);
            }
            for (int i = 0; i < 4; i++)
            {
                float ang = 5f + i * 24f + PixelCanvas.Hash(i, 4, 7) * 14f;
                float dist = 19f + i % 2 * 3f + f * 8f;
                var d = Add(Add(center, V(2f, 6f)), Dir(ang, dist));
                d = Add(d, V(0f, -f * (1.5f + i * 0.8f)));
                float r = 1.3f - f * 0.35f;
                c.Ellipse(d, r, r * 0.7f, ang, ChipMat, 13f);
            }
        }

        /// <summary>Polvo y gotas al caer el escudo al suelo.</summary>
        static void Dust(ShadedCanvas c, Vector2 at, bool late)
        {
            var mat = new PixelMaterial(Ramp.Make("6b6a5c", 3, 0.06f, 0.6f, 1.4f)) { Ambient = 0.4f, Outline = false, ReceivesContactShadow = false };
            float k = late ? 1f : 0f;
            for (int i = 0; i < 6; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                var d = Add(at, V(side * (16f + i * 2.5f + k * 7f), 1.5f + (i / 2) * 1.8f + k * 1.5f));
                float r = (1.8f - k * 0.6f) * (0.7f + PixelCanvas.Hash(i, 5, 2) * 0.5f);
                c.Ellipse(d, r * 1.3f, r, 0f, mat, 12f);
            }
        }
    }
}
