using System.Collections.Generic;
using UnityEngine;
using static Abismo.EditorTools.Rig;

namespace Abismo.EditorTools
{
    /// <summary>
    /// Horror Costero: engendro bajo de las pozas de marea, mitad pulpo y mitad medusa, que anda sobre cuatro
    /// tentáculos. Un manto bulboso de carne morada con pliegues, venas y manchas gris azuladas corona un tronco corto
    /// y anillado de carne roja húmeda; bajo la "cara" cuelga una barba de tentáculos finos y le brilla un único ojo
    /// naranja de pupila horizontal. Dos tentáculos-brazo se agitan en alto y una corona de cirros rizados le eriza el
    /// manto. Mira a la derecha; el juego lo voltea al girar.
    ///
    /// Convenciones de pose (lo no definido vale lo de <see cref="Base"/>):
    ///  - hx/hy: base del tronco (de donde nacen las patas) · lean: inclinación hacia delante (grados) · tl: largo del tronco
    ///  - mt: inclinación del manto hacia atrás · mp: tamaño (se infla/desinfla) · msq: aplastamiento vertical
    ///  - eye: -1 apagado, 0 normal, 1 telegrafiando (brilla y crece) · lid: párpado 0 abierto..1 cerrado
    ///  - ph: fase de la onda que recorre los tentáculos (radianes) · amp: amplitud de la onda
    ///  - aFa/aFc/aFl (brazo delantero) y aBa/aBc/aBl (trasero): ángulo matemático de salida, rizo total (grados,
    ///    + = antihorario) y largo
    ///  - fNx/fNy/cN (N = 0..3): pie de cada pata (x en el suelo, altura al levantarlo) y rizo de su punta
    ///  - beard/cirri: vaivén de la barba y de los cirros · melt: tinta (muerte) · pool/poolX: charco · thin: grosor
    /// </summary>
    public sealed class HorrorCosteroArt : CharacterArt
    {
        public override string Id => "horror";
        public override int FrameWidth => 144;
        public override int FrameHeight => 96;
        public override Vector2 Pivot => new Vector2(72f, 8f);

        // ------------------------------------------------------------------
        // Materiales
        // ------------------------------------------------------------------

        /// <summary>Carne roja oscura y húmeda (tronco, patas, brazos): brillo especular de 1 px.</summary>
        static readonly PixelMaterial Flesh = new PixelMaterial(Ramp.Make("6e2b3a", 6, 0.1f, 0.26f, 1.5f))
        {
            Ambient = 0.22f, Rim = 0.62f, Dither = 0f, BandDither = 0.14f,
            SpecularAt = 0.82f, SpecularPower = 22f, Specular = PixelCanvas.Hex("f6d6dc"),
            Detail = Patterns.Mottle(1.8f, 0.76f, -1, 97),
        };
        /// <summary>La misma carne para los tentáculos (sin tramado: en piezas finas se leería como ruido).</summary>
        static readonly PixelMaterial TentFlesh = new PixelMaterial(Ramp.Make("6e2b3a", 6, 0.1f, 0.26f, 1.48f))
        {
            Ambient = 0.22f, Rim = 0.7f, Dither = 0f,
            SpecularAt = 0.8f, SpecularPower = 20f, Specular = PixelCanvas.Hex("f6d6dc"),
            // Cromatóforos: manchas oscuras en grupos, pegadas a cada tramo del tentáculo.
            Detail = Patterns.Mottle(1.6f, 0.74f, -1, 131),
        };
        /// <summary>Vientre más claro y rosado del tronco.</summary>
        static readonly PixelMaterial Belly = new PixelMaterial(Ramp.Make("86404c", 5, 0.1f, 0.3f, 1.45f))
        {
            Ambient = 0.26f, Rim = 0.5f, Dither = 0f, BandDither = 0.14f,
            SpecularAt = 0.8f, SpecularPower = 22f, Specular = PixelCanvas.Hex("ffe6e6"),
        };
        /// <summary>Manto: carne morado-rosada.</summary>
        static readonly PixelMaterial Mantle = new PixelMaterial(Ramp.Make("6a3460", 6, 0.1f, 0.27f, 1.55f))
        {
            Ambient = 0.22f, Rim = 0.66f, Dither = 0f, BandDither = 0.16f,
            SpecularAt = 0.9f, SpecularPower = 20f, Specular = PixelCanvas.Hex("ffe0f0"),
        };
        /// <summary>Gris azulado frío (manchas del manto, cirros y barba): el eco del primer Horror de la hoja.</summary>
        static readonly PixelMaterial Slate = new PixelMaterial(Ramp.Make("53637a", 5, 0.07f, 0.3f, 1.42f))
        {
            Ambient = 0.26f, Rim = 0.5f, Dither = 0f,
            SpecularAt = 0.92f, SpecularPower = 22f, Specular = PixelCanvas.Hex("eef6ff"),
        };
        static readonly PixelMaterial SlateTent = new PixelMaterial(Ramp.Make("586a80", 5, 0.07f, 0.3f, 1.62f))
        {
            Ambient = 0.28f, Rim = 0.75f, Dither = 0f,
            SpecularAt = 0.84f, SpecularPower = 22f, Specular = PixelCanvas.Hex("eef6ff"),
        };
        /// <summary>Ventosas: anillos de carne pálida.</summary>
        static readonly PixelMaterial Sucker = new PixelMaterial(Ramp.Make("d39aa4", 4, 0.06f, 0.42f, 1.3f))
        {
            Ambient = 0.34f, Rim = 0.4f, Dither = 0f, SpecularAt = 0.9f, Specular = PixelCanvas.Hex("fff4f4"),
        };
        /// <summary>Hondo de la cuenca del ojo y de los sifones.</summary>
        static readonly PixelMaterial Socket = new PixelMaterial(Ramp.Make("3c1828", 3, 0.05f, 0.5f, 1.4f)) { Ambient = 0.5f, Rim = 0.2f, Dither = 0f };
        /// <summary>Tinta de la muerte: negro violáceo, espeso y brillante.</summary>
        static readonly PixelMaterial Ink = new PixelMaterial(Ramp.Make("261d38", 4, 0.06f, 0.5f, 1.55f))
        {
            Ambient = 0.2f, Rim = 0.7f, Dither = 0f, SpecularAt = 0.8f, SpecularPower = 18f, Specular = PixelCanvas.Hex("b9a8ee"),
        };
        static readonly Color32 InkColor = PixelCanvas.Hex("1c1530");
        static readonly Color32 TipPale = PixelCanvas.Hex("c99aa6");
        static readonly Color32 TipSlate = PixelCanvas.Hex("7f93a8");

        // Ojo (emisivo): normal, telegrafiando y apagado.
        static readonly PixelMaterial EyeRing = PixelMaterial.Glow("9a3212");
        static readonly PixelMaterial EyeMid = PixelMaterial.Glow("f2741c");
        static readonly PixelMaterial EyeCore = PixelMaterial.Glow("ffc45c");
        static readonly PixelMaterial EyeRingHot = PixelMaterial.Glow("d4501a");
        static readonly PixelMaterial EyeMidHot = PixelMaterial.Glow("ffa236");
        static readonly PixelMaterial EyeCoreHot = PixelMaterial.Glow("fff2a6");
        static readonly PixelMaterial EyeRingDull = PixelMaterial.Glow("4c2016");
        static readonly PixelMaterial EyeMidDull = PixelMaterial.Glow("7a3a1e");
        static readonly PixelMaterial EyeCoreDull = PixelMaterial.Glow("9c5428");
        static readonly PixelMaterial Pupil = PixelMaterial.Glow("1c0a0a");
        static readonly Color32 EyeGlint = PixelCanvas.Hex("fff8ec");

        // Estela del latigazo (carne rosada translúcida).
        static readonly PixelMaterial SmearCore = new PixelMaterial(new[] { new Color32(246, 214, 222, 235) }) { Emissive = true, Outline = false, ReceivesContactShadow = false };
        static readonly PixelMaterial SmearMid = new PixelMaterial(new[] { new Color32(206, 128, 156, 190) }) { Emissive = true, Outline = false, ReceivesContactShadow = false };
        static readonly PixelMaterial SmearSoft = new PixelMaterial(new[] { new Color32(128, 64, 98, 130) }) { Emissive = true, Outline = false, ReceivesContactShadow = false };

        // ------------------------------------------------------------------
        // Poses
        // ------------------------------------------------------------------

        static Pose Base => new Pose().With(
            ("hx", 0f), ("hy", 14f), ("lean", 4f), ("tl", 1f),
            ("mt", 14f), ("mp", 1f), ("msq", 1f), ("eye", 0f), ("lid", 0f),
            ("ph", 0f), ("amp", 1f),
            ("aFa", 2f), ("aFc", 150f), ("aFl", 23f),
            ("aBa", 200f), ("aBc", -140f), ("aBl", 24f),
            ("f0x", -14.5f), ("f0y", 0f), ("c0", 210f),
            ("f1x", 13.5f), ("f1y", 0f), ("c1", 220f),
            ("f2x", -6.5f), ("f2y", 0f), ("c2", 160f),
            ("f3x", 7.5f), ("f3y", 0f), ("c3", 180f),
            ("beard", 0f), ("cirri", 0f), ("bristle", 0f), ("melt", 0f), ("pool", 0f), ("poolX", 0f), ("thin", 1f), ("gone", 0f));

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

        /// <summary>Reposo: respira inflando el manto; la onda recorre brazos, barba, cirros y puntas de las patas.</summary>
        static Pose IdlePose(float t) => Base.With(
            ("hy", 14f + Mathf.Sin(t) * 0.6f), ("lean", 4f + Mathf.Sin(t - 0.8f) * 1.5f),
            ("mp", 1f + Mathf.Sin(t + 0.4f) * 0.035f), ("mt", 14f + Mathf.Sin(t - 0.6f) * 3f),
            ("ph", t), ("aFa", 2f + Mathf.Sin(t + 0.3f) * 7f), ("aFc", 150f + Mathf.Sin(t) * 35f),
            ("aBa", 200f - Mathf.Sin(t + 1.4f) * 7f), ("aBc", -140f - Mathf.Sin(t + 1.1f) * 35f),
            ("c0", 200f + Mathf.Sin(t + 0.5f) * 25f), ("c1", 210f + Mathf.Sin(t + 2.1f) * 25f),
            ("c2", 150f + Mathf.Sin(t + 3.3f) * 20f), ("c3", 170f + Mathf.Sin(t + 4.4f) * 20f),
            ("beard", Mathf.Sin(t + 1f) * 6f), ("cirri", Mathf.Sin(t + 0.4f) * 10f));

        ShadedCanvas DrawIdle(int f) => Draw(IdlePose(f / 6f * Mathf.PI * 2f));

        /// <summary>
        /// Ciclo de una pata: apoyo (el pie retrocede pegado al suelo) y vuelo (avanza levantándose y enroscando la punta).
        /// </summary>
        static void Step(Pose p, int leg, float phase, float center, float stride, float lift, float stance = 0.6f)
        {
            float u = phase - Mathf.Floor(phase);
            float x, y, curl = 0f;
            if (u < stance)
            {
                float w = u / stance;
                x = center + stride * (1f - 2f * w);
                y = 0f;
            }
            else
            {
                float w = (u - stance) / (1f - stance);
                float s = w * w * (3f - 2f * w);
                x = center - stride + 2f * stride * s;
                y = Mathf.Sin(w * Mathf.PI) * lift;
                curl = Mathf.Sin(w * Mathf.PI) * 70f;
            }
            p["f" + leg + "x"] = x;
            p["f" + leg + "y"] = y;
            p["c" + leg] = p["c" + leg] + curl;
        }

        /// <summary>Andar rastrero: las cuatro patas en onda (como un pulpo que repta), el cuerpo cabecea.</summary>
        ShadedCanvas DrawWalk(int f)
        {
            float ph = f / 8f, t = ph * Mathf.PI * 2f;
            var p = Base.With(
                ("lean", 9f + Mathf.Sin(t * 2f) * 1.5f), ("hx", 0.5f), ("hy", 13.6f + Mathf.Cos(t * 2f) * 0.8f),
                ("mt", 16f + Mathf.Sin(t * 2f - 0.8f) * 3f), ("mp", 1f + Mathf.Sin(t * 2f + 0.5f) * 0.03f),
                ("ph", t * 2f), ("aFa", 2f + Mathf.Sin(t) * 9f), ("aFc", 135f + Mathf.Sin(t + 0.5f) * 35f),
                ("aBa", 196f + Mathf.Sin(t + 2f) * 10f), ("aBc", -130f - Mathf.Sin(t + 2.5f) * 40f),
                ("beard", Mathf.Sin(t * 2f + 1f) * 8f - 4f), ("cirri", Mathf.Sin(t * 2f) * 12f - 6f));
            Step(p, 0, ph + 0.25f, -14f, 3.5f, 3f);
            Step(p, 1, ph + 0.75f, 13.5f, 3.5f, 3f);
            Step(p, 2, ph + 0.5f, -6f, 3.5f, 3.5f);
            Step(p, 3, ph, 8f, 3.5f, 3.5f);
            return Draw(p);
        }

        /// <summary>Carrera: agachado y con la cabeza por delante; el brazo delantero tantea el suelo y el trasero se arrastra.</summary>
        ShadedCanvas DrawRun(int f)
        {
            float ph = f / 8f, t = ph * Mathf.PI * 2f;
            var p = Base.With(
                ("lean", 18f + Mathf.Sin(t * 2f) * 2f), ("hx", 2f), ("hy", 12.4f + Mathf.Cos(t * 2f) * 1.2f), ("tl", 0.95f),
                ("mt", -4f + Mathf.Sin(t * 2f - 0.7f) * 4f), ("mp", 0.97f), ("msq", 0.92f),
                ("ph", t * 2f), ("amp", 1.4f),
                ("aFa", -14f + Mathf.Sin(t * 2f) * 9f), ("aFc", 115f + Mathf.Sin(t * 2f + 0.6f) * 35f), ("aFl", 24f),
                ("aBa", 178f + Mathf.Sin(t * 2f + 1.5f) * 8f), ("aBc", -80f - Mathf.Sin(t * 2f + 2f) * 40f), ("aBl", 22f),
                ("beard", -18f + Mathf.Sin(t * 2f) * 6f), ("cirri", 30f + Mathf.Sin(t * 2f + 0.8f) * 10f), ("eye", 0.2f));
            Step(p, 0, ph + 0.25f, -14f, 6.5f, 4f, 0.5f);
            Step(p, 1, ph + 0.75f, 16f, 6.5f, 4f, 0.5f);
            Step(p, 2, ph + 0.5f, -6f, 6.5f, 5f, 0.5f);
            Step(p, 3, ph, 11f, 6.5f, 5f, 0.5f);
            return Draw(p);
        }

        /// <summary>Encogido: el cuerpo se agacha, el manto se aplasta y enrosca los brazos contra el cuerpo (anticipación).</summary>
        static Pose Coil => Base.With(
            ("hy", 11.6f), ("lean", 6f), ("hx", -1f), ("tl", 0.85f), ("mt", 14f), ("mp", 1f), ("msq", 0.86f),
            ("aFa", 40f), ("aFc", 240f), ("aFl", 19f), ("aBa", 205f), ("aBc", -240f), ("aBl", 20f),
            ("f0x", -16f), ("f1x", 15f), ("f2x", -9f), ("f3x", 10f), ("c0", 250f), ("c1", 250f), ("c2", 200f), ("c3", 210f),
            ("beard", 6f), ("cirri", 10f), ("bristle", 0.3f), ("eye", 0.4f));

        /// <summary>Abierto y amartillado: los dos brazos desplegados en alto, los cirros erizados, el ojo encendido.</summary>
        static Pose Cocked => Coil.With(
            ("hy", 12.6f), ("lean", 0f), ("hx", -2f), ("mt", 6f), ("mp", 1.1f), ("msq", 0.96f),
            ("aFa", 56f), ("aFc", -110f), ("aFl", 28f), ("aBa", 128f), ("aBc", 120f), ("aBl", 30f),
            ("beard", 14f), ("cirri", 12f), ("bristle", 1f), ("eye", 1f), ("amp", 1.2f));

        ShadedCanvas DrawWindup(int f)
        {
            var keys = new Keyframes()
                .Key(0, Pose.Lerp(IdlePose(0f), Coil, 0.5f), Ease.Out)
                .Key(2, Coil, Ease.InOut)
                .Key(4, Cocked, Ease.Out)
                .Key(5, Cocked.With(("lean", -3f), ("aFa", 62f), ("aFc", -120f), ("aBa", 124f), ("aBc", 130f), ("mp", 1.13f), ("hx", -2.6f)));
            var p = keys.Evaluate(f);
            p["ph"] = f * 1.1f;
            p["eye"] = f >= 4 ? 1f : Mathf.Lerp(0.15f, 0.7f, f / 3f);
            return Draw(p);
        }

        /// <summary>Latigazo: se lanza hacia delante y azota con el brazo delantero estirado (alcance ≈ +60 px).</summary>
        static Pose Lash => Base.With(
            ("hx", 4f), ("hy", 13.2f), ("lean", 24f), ("tl", 1.05f), ("mt", 36f), ("mp", 1f), ("msq", 0.96f),
            ("aFa", 6f), ("aFc", -10f), ("aFl", 47.5f), ("aBa", 50f), ("aBc", -30f), ("aBl", 26f),
            ("f0x", -18f), ("f1x", 19f), ("f1y", 1.5f), ("f2x", -11f), ("f3x", 13f), ("c0", 160f), ("c1", 120f), ("c2", 120f), ("c3", 130f),
            ("beard", -14f), ("cirri", -18f), ("bristle", 0.6f), ("eye", 0.9f), ("amp", 0.35f));

        static Pose LashEnd => Lash.With(("aFa", 6f), ("aFc", 90f), ("aFl", 36f), ("aBa", 40f), ("aBc", -80f), ("hx", 3.5f), ("lean", 20f),
                                          ("amp", 1.1f), ("eye", 0.3f), ("beard", -4f), ("cirri", -8f), ("bristle", 0.2f));

        ShadedCanvas DrawAttack(int f)
        {
            Pose p;
            var smear = default(SmearSpec);
            switch (f)
            {
                case 0:
                    p = Pose.Lerp(Cocked, Lash, 0.6f).With(("aFa", 50f), ("aFc", 15f), ("aFl", 40f), ("aBa", 90f), ("amp", 0.6f));
                    smear = new SmearSpec { On = true, FromA = 62f, FromC = -120f, FromL = 28f, Strength = 0.7f };
                    break;
                case 1:
                    p = Lash;
                    smear = new SmearSpec { On = true, FromA = 50f, FromC = 15f, FromL = 40f, Strength = 1f };
                    break;
                case 2:
                    p = Lash.With(("aFa", 1f), ("aFc", 28f), ("aFl", 47.5f), ("aBa", 44f), ("hx", 4.5f), ("lean", 25f), ("amp", 0.7f), ("eye", 0.6f));
                    break;
                default:
                    p = LashEnd;
                    break;
            }
            p["ph"] = 2f + f * 1.3f;
            return Draw(p, smear);
        }

        ShadedCanvas DrawRecover(int f)
        {
            var pull = Base.With(("hx", 2f), ("hy", 14f), ("lean", 12f), ("aFa", 10f), ("aFc", 140f), ("aFl", 28f), ("aBa", 150f), ("aBc", -150f),
                                 ("mt", 18f), ("eye", 0.1f), ("beard", 4f), ("cirri", 8f));
            var keys = new Keyframes()
                .Key(0, LashEnd, Ease.Out)
                .Key(2, pull)
                .Key(5, IdlePose(0f));
            var p = keys.Evaluate(f);
            p["ph"] = 6f + f * 1.05f;
            return Draw(p);
        }

        static Pose Hit => Base.With(
            ("hx", -3f), ("hy", 14.5f), ("lean", -18f), ("mt", 40f), ("mp", 0.94f), ("msq", 1.04f), ("eye", -0.2f), ("lid", 0.65f),
            ("aFa", 120f), ("aFc", 200f), ("aFl", 22f), ("aBa", 172f), ("aBc", -200f), ("amp", 1.6f),
            ("f0x", -17f), ("f1x", 13f), ("f3x", 7f), ("f1y", 2f), ("c1", 260f), ("c3", 230f),
            ("beard", 22f), ("cirri", 30f));

        ShadedCanvas DrawHurt(int f)
        {
            var keys = new Keyframes().Key(0, Hit, Ease.Out).Key(1, Pose.Lerp(Hit, IdlePose(0f), 0.4f)).Key(2, Pose.Lerp(Hit, IdlePose(0f), 0.75f));
            var p = keys.Evaluate(f);
            p["ph"] = 1f + f * 1.7f;
            return Draw(p);
        }

        /// <summary>Aturdido: el manto se le cae hacia delante, el ojo se apaga a medias, los brazos cuelgan flácidos.</summary>
        ShadedCanvas DrawStagger(int f)
        {
            float t = f / 6f * Mathf.PI * 2f;
            var p = Base.With(
                ("hx", Mathf.Sin(t) * 1.5f), ("hy", 12.6f + Mathf.Cos(t * 2f) * 0.4f), ("lean", 10f + Mathf.Sin(t) * 7f),
                ("mt", 2f + Mathf.Sin(t + 0.8f) * 6f), ("mp", 0.93f), ("msq", 0.94f),
                ("eye", f == 2 || f == 3 ? -1f : -0.6f), ("lid", 0.5f + 0.15f * Mathf.Sin(t)),
                ("ph", t), ("amp", 0.6f),
                ("aFa", -55f + Mathf.Sin(t + 1f) * 10f), ("aFc", 70f + Mathf.Sin(t) * 20f), ("aFl", 22f),
                ("aBa", -125f + Mathf.Sin(t + 2f) * 10f), ("aBc", -60f - Mathf.Sin(t + 0.5f) * 20f), ("aBl", 20f),
                ("f0x", -17f), ("f1x", 13f), ("f2x", -9f), ("f3x", 10f),
                ("c0", 120f), ("c1", 130f), ("c2", 100f), ("c3", 110f),
                ("beard", Mathf.Sin(t + 1.5f) * 10f), ("cirri", -10f + Mathf.Sin(t) * 8f));
            return Draw(p);
        }

        /// <summary>Muerte: retrocede, se desinfla, se desploma y se deshace en un charco de tinta.</summary>
        ShadedCanvas DrawDeath(int f)
        {
            var hit = Hit.With(("lid", 0.8f), ("eye", -0.4f));
            var reel = hit.With(("hx", -4f), ("lean", -22f), ("mt", 46f), ("mp", 0.9f), ("aFa", 140f), ("aBa", 176f), ("lid", 0.9f));
            var sag = Base.With(("hx", -3f), ("hy", 9f), ("lean", 8f), ("tl", 0.85f), ("mt", 50f), ("mp", 0.78f), ("msq", 0.72f),
                                ("eye", -0.7f), ("lid", 0.9f), ("aFa", -20f), ("aFc", 90f), ("aBa", -150f), ("aBc", -90f), ("amp", 0.5f),
                                ("f0x", -19f), ("f1x", 18f), ("f2x", -11f), ("f3x", 12f), ("c0", 150f), ("c1", 150f), ("c2", 110f), ("c3", 120f),
                                ("beard", 5f), ("cirri", -25f), ("melt", 0.2f), ("pool", 8f));
            var slump = sag.With(("hy", 4.5f), ("lean", 14f), ("tl", 0.6f), ("mt", 62f), ("mp", 0.62f), ("msq", 0.5f), ("eye", -1f), ("lid", 1f),
                                 ("aFa", -40f), ("aFc", 60f), ("aBa", -160f), ("aBc", -60f), ("amp", 0.3f),
                                 ("f0x", -22f), ("f1x", 22f), ("f2x", -14f), ("f3x", 15f), ("c0", 100f), ("c1", 100f), ("c2", 80f), ("c3", 80f),
                                 ("melt", 0.55f), ("pool", 18f), ("thin", 0.85f));
            var melted = slump.With(("hy", 2.5f), ("tl", 0.4f), ("mp", 0.42f), ("msq", 0.34f), ("lean", 18f),
                                    ("f0x", -24f), ("f1x", 24f), ("f2x", -16f), ("f3x", 17f), ("c0", 60f), ("c1", 60f), ("c2", 50f), ("c3", 50f),
                                    ("aFl", 16f), ("aBl", 14f), ("melt", 0.85f), ("pool", 24f), ("thin", 0.6f));
            var keys = new Keyframes()
                .Key(0, hit, Ease.Out)
                .Key(1, reel)
                .Key(3, sag, Ease.In)
                .Key(5, slump)
                .Key(7, melted, Ease.Out)
                .Key(8, melted.With(("mp", 0.3f), ("msq", 0.26f), ("melt", 1f), ("pool", 26f), ("thin", 0.45f), ("gone", 0.6f)))
                .Key(9, melted.With(("mp", 0.2f), ("msq", 0.2f), ("melt", 1f), ("pool", 27f), ("thin", 0.3f), ("gone", 1f)));
            var p = keys.Evaluate(f);
            p["ph"] = f * 0.9f;
            return Draw(p);
        }

        // ------------------------------------------------------------------
        // Dibujo del personaje a partir de una pose
        // ------------------------------------------------------------------

        struct SmearSpec
        {
            public bool On;
            /// <summary>Ángulo, rizo y largo del brazo delantero en el fotograma anterior (de donde viene el latigazo).</summary>
            public float FromA, FromC, FromL;
            public float Strength;
        }

        const float TrunkLen = 10.5f, TrunkR = 5.4f;

        /// <summary>Tinta de la muerte del fotograma en curso (0..1): tiñe todas las piezas que se dibujan.</summary>
        float melt;

        /// <summary>Aplica el tinte (de la punta del tentáculo o, al morir, de la tinta) a una pieza.</summary>
        ShadedCanvas.Shape Tinted(ShadedCanvas.Shape s, Color32 tint = default, float amount = 0f)
        {
            if (melt > 0.01f) return s.WithTint(InkColor, Mathf.Clamp01(melt * 0.9f));
            if (amount > 0.001f) return s.WithTint(tint, amount);
            return s;
        }

        ShadedCanvas.Shape Cap(ShadedCanvas c, Vector2 a, Vector2 b, float ra, float rb, PixelMaterial m, float z, float shade, int g) =>
            Tinted(c.Capsule(a, b, ra, rb, m, z, shade, g));

        ShadedCanvas.Shape Ell(ShadedCanvas c, Vector2 at, float rx, float ry, float angle, PixelMaterial m, float z, float shade, int g, float flat = 0f) =>
            Tinted(c.Ellipse(at, rx, ry, angle, m, z, shade, g, flat));

        /// <summary>Marco del manto: centro, ejes y radios actuales (para colocar ojo, barba, cirros y dibujos).</summary>
        struct MantleFrame
        {
            /// <summary>Axis: del cuello a la coronilla (hacia arriba y atrás). Front: hacia la cara.</summary>
            public Vector2 Center, Axis, Front;
            public float Rx, Ry, Angle;
            /// <summary>Punto en coordenadas normalizadas del manto (u: −1 cuello .. +1 coronilla; v: −1 nuca .. +1 cara).</summary>
            public Vector2 At(float u, float v) => Add(Center, Add(Scale(Axis, u * Rx), Scale(Front, v * Ry)));
        }

        ShadedCanvas Draw(Pose p, SmearSpec smear = default)
        {
            var c = NewFrame();
            melt = p["melt"];
            float lean = p["lean"], ta = 90f - lean, ph = p["ph"], amp = p["amp"], thin = p["thin"];
            bool gone = p["gone"] > 0.5f;
            var hip = V(p["hx"], p["hy"]);
            Vector2 Body(float along, float fwd) => Add(hip, Add(Dir(ta, along), Dir(ta - 90f, fwd)));
            // Nada se hunde en el suelo: si el tronco bajaría de él (al desplomarse), sube la base.
            float trunkR = TrunkR * Mathf.Lerp(0.55f, 1f, Mathf.Clamp01((p["msq"] - 0.3f) / 0.7f));
            float low = Body(-1.5f, 0f).y - trunkR;
            if (low < 0.4f) hip = V(hip.x, hip.y + 0.4f - low);
            var chest = Body(TrunkLen * p["tl"], 0f);

            // --- Charco de tinta (muerte) ---
            if (p["pool"] > 0.5f) DrawPool(c, p, gone);
            if (gone)
            {
                DrawRemains(c, p);
                return c;
            }

            // --- Patas: las dos lejanas detrás (más oscuras), las dos cercanas delante del tronco ---
            for (int i = 0; i < 4; i++)
            {
                var root = Body(LegRoot[i].x, LegRoot[i].y);
                DrawLeg(c, root, V(p["f" + i + "x"], p["f" + i + "y"]), LegOut[i], p["c" + i], ph + i * 1.37f, amp, thin,
                        LegZ[i], LegShade[i], LegR[i]);
            }

            // --- Brazo trasero (detrás de todo el cuerpo) ---
            var shoulderB = Body(TrunkLen * p["tl"] * 0.66f, -4.4f);
            var armB = Chain(shoulderB, p["aBa"], p["aBl"], p["aBc"], 70f * amp, ph + 2.2f, 12, 1.7f, 4.6f);
            DrawTentacle(c, armB, 2.3f * thin, 0.5f, TentFlesh, 1f, -0.13f, c.NewGroup(), p["aBc"] >= 0f ? 1 : -1, 0.12f, TipSlate, 0.45f);

            // --- Tronco anillado ---
            int gTrunk = c.NewGroup();
            DrawTrunk(c, p, hip, chest, ta, lean, trunkR, gTrunk);

            // --- Manto, cirros y cara ---
            var mf = DrawMantle(c, p, chest, lean, ph, amp);

            // --- Barba de tentáculos bajo la cara ---
            DrawBeard(c, p, mf, ph, amp, thin);

            // --- Ojo ---
            DrawEye(c, p, mf);

            // --- Brazo delantero (delante de todo) y su estela ---
            var shoulderF = Body(TrunkLen * p["tl"] * 0.7f, 4.4f);
            var armF = Chain(shoulderF, p["aFa"], p["aFl"], p["aFc"], 70f * amp, ph + 0.7f, 14, 1.7f, 4.6f);
            if (smear.On) DrawWhipTrail(c, p, shoulderF, armF, smear, ph, amp);
            DrawTentacle(c, armF, 2.5f * thin, 0.55f, TentFlesh, 8f, 0f, c.NewGroup(), p["aFc"] >= 0f ? 1 : -1, 0.12f, TipSlate, 0.4f);
            return c;
        }

        // ------------------------------------------------------------------
        // Tentáculos
        // ------------------------------------------------------------------

        /// <summary>
        /// Tentáculo por ángulos acumulados: sale de <paramref name="root"/> con <paramref name="angle"/> (matemático) y
        /// gira <paramref name="curl"/> grados en total, más hacia la punta (<paramref name="bias"/>). Encima, una onda
        /// viajera (<paramref name="amp"/> grados, <paramref name="k"/> radianes a lo largo) que avanza con la fase
        /// <paramref name="ph"/> de la raíz a la punta: al animar la fase, el tentáculo ondula de forma fluida.
        /// </summary>
        static Vector2[] Chain(Vector2 root, float angle, float length, float curl, float amp, float ph, int n, float bias, float k)
        {
            var pts = new Vector2[n + 1];
            pts[0] = root;
            float norm = 0f;
            for (int i = 1; i <= n; i++) norm += Mathf.Pow(i / (float)n, bias);
            float a = angle, seg = length / n;
            for (int i = 1; i <= n; i++)
            {
                float s = i / (float)n;
                a += curl * Mathf.Pow(s, bias) / norm + amp * Mathf.Sin(ph - k * s) * (0.35f + s) / n;
                pts[i] = Add(pts[i - 1], Dir(a, seg));
            }
            return pts;
        }

        static readonly Vector2[] LegRoot = { V(0.6f, -3.6f), V(0.6f, 3.2f), V(-0.8f, -2.6f), V(-0.8f, 2.6f) };
        static readonly float[] LegOut = { -1f, 1f, -1f, 1f };
        static readonly float[] LegZ = { 0.6f, 0.5f, 5f, 5.2f };
        static readonly float[] LegShade = { -0.15f, -0.15f, 0f, 0f };
        static readonly float[] LegR = { 2.5f, 2.5f, 2.9f, 2.9f };

        /// <summary>
        /// Pata-tentáculo: de la raíz baja arqueándose hacia fuera hasta el pie (Bézier cúbica con una onda que no mueve
        /// ni la raíz ni el pie) y la punta sigue enroscándose hacia fuera y arriba. Después se ajusta para que el punto
        /// más bajo toque el suelo (o quede a la altura <c>foot.y</c> si la pata está en el aire).
        /// </summary>
        void DrawLeg(ShadedCanvas c, Vector2 root, Vector2 foot, float outward, float curl, float ph, float amp, float thin,
                     float z, float shade, float r0)
        {
            var pts = new List<Vector2>();
            var p1 = Add(root, V(outward * 5f, -3f));
            var footPt = V(foot.x, 1.6f);
            var p2 = Add(footPt, V(-outward * 2.5f, 5f));
            const int n = 8;
            var chord = Sub(footPt, root);
            float cl = Mathf.Max(0.001f, Mathf.Sqrt(chord.x * chord.x + chord.y * chord.y));
            var across = V(-chord.y / cl, chord.x / cl);
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n, u = 1f - t;
                var q = V(u * u * u * root.x + 3f * u * u * t * p1.x + 3f * u * t * t * p2.x + t * t * t * footPt.x,
                          u * u * u * root.y + 3f * u * u * t * p1.y + 3f * u * t * t * p2.y + t * t * t * footPt.y);
                q = Add(q, Scale(across, Mathf.Sin(t * Mathf.PI) * Mathf.Sin(ph - 3f * t) * 1.3f * amp));
                pts.Add(q);
            }
            // Rizo de la punta.
            var d = Sub(footPt, p2);
            float a = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            const int m = 7;
            float norm = 0f;
            for (int j = 1; j <= m; j++) norm += j / (float)m;
            for (int j = 1; j <= m; j++)
            {
                float s = j / (float)m;
                a += outward * curl * s / norm + amp * 18f * Mathf.Sin(ph - 2.5f - 3f * s) / m;
                pts.Add(Add(pts[pts.Count - 1], Dir(a, 1.55f * Mathf.Lerp(1f, 0.75f, s))));
            }

            // Radios por longitud de arco.
            var radii = Radii(pts, r0 * thin, 0.5f, 0.85f);

            // Apoyo: el punto más bajo del tramo del pie toca el suelo (y = foot.y); la raíz no se mueve.
            float lowest = float.MaxValue;
            for (int i = n - 3; i < pts.Count; i++) lowest = Mathf.Min(lowest, pts[i].y - radii[i]);
            float dy = foot.y - lowest;
            for (int i = 1; i < pts.Count; i++)
            {
                float w = Mathf.Clamp01(i / (float)n);
                w = w * w * (3f - 2f * w);
                pts[i] = V(pts[i].x, Mathf.Max(radii[i], pts[i].y + dy * w));
            }
            DrawTentacle(c, pts.ToArray(), r0 * thin, 0.5f, TentFlesh, z, shade, c.NewGroup(), outward > 0f ? 1 : -1, 0.22f, TipPale, 0.3f, radii);
        }

        static float[] Radii(IList<Vector2> pts, float r0, float r1, float power)
        {
            var cum = new float[pts.Count];
            for (int i = 1; i < pts.Count; i++)
            {
                var d = Sub(pts[i], pts[i - 1]);
                cum[i] = cum[i - 1] + Mathf.Sqrt(d.x * d.x + d.y * d.y);
            }
            float total = Mathf.Max(0.001f, cum[pts.Count - 1]);
            var r = new float[pts.Count];
            for (int i = 0; i < pts.Count; i++) r[i] = Mathf.Lerp(r0, r1, Mathf.Pow(cum[i] / total, power));
            return r;
        }

        /// <summary>
        /// Dibuja un tentáculo: cápsulas encadenadas que se afinan, ventosas claras en un costado (<paramref name="side"/>:
        /// +1 a la izquierda del avance, −1 a la derecha, 0 sin ventosas), la punta teñida y un brillo húmedo.
        /// </summary>
        void DrawTentacle(ShadedCanvas c, Vector2[] pts, float r0, float r1, PixelMaterial mat, float z, float shade, int g,
                          int side, float suckFrom, Color32 tipColor, float tipAmount, float[] radii = null)
        {
            if (radii == null) radii = Radii(pts, r0, r1, 0.9f);
            int n = pts.Length - 1;
            // Nada atraviesa el suelo: los tramos que bajarían de él se quedan apoyados.
            for (int i = 0; i <= n; i++)
                if (pts[i].y < radii[i]) pts[i] = V(pts[i].x, radii[i]);
            var cum = new float[pts.Length];
            for (int i = 1; i <= n; i++)
            {
                var d = Sub(pts[i], pts[i - 1]);
                cum[i] = cum[i - 1] + Mathf.Sqrt(d.x * d.x + d.y * d.y);
            }
            float total = Mathf.Max(0.001f, cum[n]);
            for (int i = 0; i < n; i++)
            {
                float s = (cum[i] + cum[i + 1]) * 0.5f / total;
                float tint = tipAmount * Mathf.SmoothStep(0f, 1f, (s - 0.45f) / 0.55f);
                Tinted(c.Capsule(pts[i], pts[i + 1], radii[i], radii[i + 1], mat, z, shade, g), tipColor, tint);
            }

            // Ventosas: botones pálidos que asoman por el costado, más pequeños hacia la punta.
            if (side == 0 || melt > 0.7f) return;
            float at = suckFrom * total;
            int seg = 0, count = 0;
            while (at < total * 0.96f && count < 40)
            {
                while (seg < n - 1 && cum[seg + 1] < at) seg++;
                float segLen = Mathf.Max(0.001f, cum[seg + 1] - cum[seg]);
                float t = Mathf.Clamp01((at - cum[seg]) / segLen);
                var pos = Mix(pts[seg], pts[seg + 1], t);
                var dir = Scale(Sub(pts[seg + 1], pts[seg]), 1f / segLen);
                var left = V(-dir.y, dir.x);
                float r = Mathf.Lerp(radii[seg], radii[seg + 1], t);
                float rs = 0.42f + r * 0.3f;
                var center = Add(pos, Scale(left, side * r * 0.8f));
                Tinted(c.Ellipse(center, rs * 1.05f, rs * 0.85f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, Sucker, z + 0.02f, shade, g));
                if (rs >= 0.8f) c.Dot(Add(center, Scale(left, side * 0.25f)), -2, g, Sucker);
                at += 1.1f + r * 1.0f;
                count++;
            }
        }

        // ------------------------------------------------------------------
        // Tronco
        // ------------------------------------------------------------------

        /// <summary>Tronco corto y anillado: carne roja con vientre rosado, anillos en relieve, pliegues y venas.</summary>
        void DrawTrunk(ShadedCanvas c, Pose p, Vector2 hip, Vector2 chest, float ta, float lean, float r, int g)
        {
            Vector2 T(float along, float fwd) => Add(hip, Add(Dir(ta, along), Dir(ta - 90f, fwd)));
            float len = TrunkLen * p["tl"];
            var ribs = Patterns.Sum(Patterns.Ridges(2.6f, 0.9f), Patterns.Lumps(2.5f, 0.5f, 41));
            float kr = r / TrunkR;
            Cap(c, T(-1.5f, 0f), chest, r, r * 1.18f, Flesh, 3f, 0f, g).WithBump(ribs, 0.9f);
            Cap(c, T(-0.8f, 2.4f * kr), T(len - 0.5f, 2.1f * kr), 3.1f * kr, 3.7f * kr, Belly, 3.05f, 0f, g)
                .WithBump(ribs, 0.8f)
                .WithDetail(Patterns.Mottle(1.8f, 0.78f, 1, 53));
            // Anillos: surcos curvados (se abomban hacia abajo por delante, como aros vistos un poco desde arriba).
            for (int k = 0; k < 3; k++)
            {
                float a = 1.5f + k * 2.8f;
                c.Fold(new[] { T(a + 0.6f, -6f), T(a - 0.3f, -2f), T(a - 0.9f, 2.5f), T(a - 0.6f, 6.4f) }, g, 2, 1, null);
            }
            // Venas: una gruesa que sube por el costado y se ramifica.
            c.Mark(new[] { T(0.5f, -4.5f), T(2.5f, -3.2f), T(4.5f, -3.8f), T(6.5f, -2.6f), T(8.5f, -3.4f) }, -1, g, Flesh);
            c.Mark(new[] { T(4.5f, -3.8f), T(5.5f, -5.4f) }, -1, g, Flesh);
            c.Mark(new[] { T(1f, 4.5f), T(3f, 5.2f), T(5f, 4.4f) }, -1, g, Belly);
            // Brillo húmedo.
            c.Glint(T(len * 0.75f, 3.8f), g, 0, Belly);
            c.Glint(T(len * 0.35f, 4.5f), g, 0, Belly);
        }

        // ------------------------------------------------------------------
        // Manto
        // ------------------------------------------------------------------

        /// <summary>
        /// Manto bulboso: lóbulo trasero, cuerpo principal con costillas en relieve que siguen su contorno, manchas
        /// gris azuladas, pliegues, venas que se ramifican, el faldón lobulado de abajo, cirros rizados en la coronilla,
        /// un sifón y el arco de la ceja sobre el ojo.
        /// </summary>
        MantleFrame DrawMantle(ShadedCanvas c, Pose p, Vector2 chest, float lean, float ph, float amp)
        {
            float mp = p["mp"], msq = p["msq"];
            // Rx: del cuello a la coronilla; Ry: de la nuca a la cara. Al aplastarse se ensancha.
            float rx = 11.2f * mp * msq, ry = 9.3f * mp * (1f + (1f - msq) * 0.7f);
            float angle = 90f + p["mt"] - lean;
            var axis = Dir(angle);
            var front = Dir(angle - 90f);
            var center = Add(Add(chest, Scale(axis, rx * 0.64f)), Scale(front, -1.2f));
            // Si al desplomarse el manto bajaría del suelo, se apoya en él (con sitio para el faldón).
            float ext = Mathf.Sqrt(rx * rx * axis.y * axis.y + ry * ry * front.y * front.y);
            if (center.y - ext < 1.5f) center = V(center.x, 1.5f + ext);
            var mf = new MantleFrame { Center = center, Rx = rx, Ry = ry, Angle = angle, Axis = axis, Front = front };
            int g = c.NewGroup(), gSkirt = c.NewGroup();

            // Cirros: rizos finos gris azulados que nacen de la coronilla y la nuca, detrás del manto.
            if (melt < 0.8f)
            {
                float[] phi = { 52f, 28f, 4f, -20f, -46f, -74f, -102f };
                float[] len = { 5.5f, 7f, 8f, 9f, 9.5f, 8.5f, 6.5f };
                float bristle = p["bristle"];
                for (int k = 0; k < phi.Length; k++)
                {
                    float f = phi[k] * Mathf.Deg2Rad;
                    float u = Mathf.Cos(f), v = Mathf.Sin(f);
                    var root = mf.At(u * 0.88f, v * 0.88f);
                    // Normal hacia fuera de la elipse en ese punto.
                    var nrm = Add(Scale(axis, u / rx), Scale(front, v / ry));
                    float out0 = Mathf.Atan2(nrm.y, nrm.x) * Mathf.Rad2Deg;
                    float sign = k % 2 == 0 ? -1f : 1f;
                    var pts = Chain(root, out0 + p["cirri"] - sign * 25f * (1f - bristle), len[k] * mp * (1f + 0.25f * bristle),
                                    sign * 330f * (1f - 0.55f * bristle), 70f * amp, ph * 1.3f + k * 1.1f, 10, 2.2f, 3.5f);
                    DrawTentacle(c, pts, 1.15f, 0.42f, SlateTent, 3.6f, -0.03f, c.NewGroup(), 0, 0f, TipPale, 0.25f);
                }
            }

            // Faldón lobulado alrededor del cuello (pliegues del manto que cuelgan sobre el tronco).
            for (int k = 0; k < 4; k++)
            {
                var at = mf.At(-0.86f + (k == 0 || k == 3 ? 0.08f : 0f), -0.72f + k * 0.42f);
                Ell(c, at, 3.4f * mp, 2.7f * mp, angle + 90f - k * 12f, Mantle, 3.9f, -0.05f, gSkirt)
                    .WithBump(Patterns.Lumps(2f, 0.6f, 71 + k), 0.8f);
            }

            // Sifón (embudo) que asoma por la nuca, hacia abajo y atrás.
            var siphon = mf.At(-0.62f, -0.78f);
            var siphonEnd = Add(siphon, Add(Scale(front, -2.6f), Scale(axis, -2.2f)));
            Cap(c, siphon, siphonEnd, 1.8f, 1.4f, Flesh, 3.95f, -0.04f, gSkirt).WithBump(Patterns.Ridges(1.6f, 0.5f), 1f);
            Ell(c, siphonEnd, 1.05f, 0.85f, 0f, Socket, 3.96f, 0f, gSkirt);

            // Relieve: costillas que van del cuello a la coronilla (meridianos del bulbo) y abultamientos.
            System.Func<float, float, float> bump = (u, v) =>
            {
                float U = u / rx, Vn = v / ry;
                float lat = Vn / Mathf.Sqrt(Mathf.Max(0.08f, 1f - U * U));
                return 0.75f * (0.5f + 0.5f * Mathf.Cos(lat * Mathf.PI * 3.6f)) + PixelCanvas.ValueNoise(U * 6f, Vn * 5f, 0, 613) * 0.7f;
            };

            // Lóbulo de la nuca (silueta orgánica, no una elipse perfecta).
            Ell(c, mf.At(-0.1f, -0.42f), rx * 0.74f, ry * 0.66f, angle + 12f, Mantle, 3.98f, 0f, g).WithBump(Patterns.Lumps(2.4f, 0.8f, 617), 0.9f);

            // Cuerpo principal del manto.
            Ell(c, center, rx, ry, angle, Mantle, 4f, 0f, g).Soft().WithBump(bump, 0.95f);

            // Papilas: verrugas carnosas que rompen el contorno de la nuca y la coronilla.
            float[] papU = { 0.62f, 0.18f, -0.22f, -0.58f };
            float[] papV = { -0.76f, -0.97f, -0.96f, -0.8f };
            for (int k = 0; k < papU.Length; k++)
            {
                var at = mf.At(papU[k], papV[k]);
                Ell(c, at, 1.4f * mp, 1.15f * mp, angle, Mantle, 3.99f, -0.02f, g);
            }

            // Manchas gris azuladas en grupos, sobre todo en la coronilla y la nuca (misma luz y relieve que el manto).
            float ca = Mathf.Cos(angle * Mathf.Deg2Rad), sa = Mathf.Sin(angle * Mathf.Deg2Rad);
            float rr = Mathf.Max(rx, ry) + 1f;
            var patch = c.Custom(center.x - rr, center.y - rr, center.x + rr, center.y + rr, (px, py) =>
            {
                float dx = px - center.x, dy = py - center.y;
                float lx = (dx * ca + dy * sa) / rx, ly = (-dx * sa + dy * ca) / ry;   // ly > 0 = nuca
                float d2 = lx * lx + ly * ly;
                if (d2 > 0.97f) return (false, N3.Front);
                float thr = 0.6f - lx * 0.16f - ly * 0.12f + Mathf.Max(0f, -ly - 0.2f) * 0.5f + Mathf.Max(0f, -lx) * 0.25f;
                if (PixelCanvas.ValueNoise(lx * 3.4f + 7f, ly * 3.4f + 3f, 0, 911) < thr) return (false, N3.Front);
                float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - d2));
                return (true, new N3(lx * ca - ly * sa, lx * sa + ly * ca, nz).Normalized());
            }, Slate, 4.01f, 0f, g);
            Tinted(patch.Frame(center, angle).WithBump(bump, 0.95f));

            // Cara: abultamiento delante-abajo donde se hunde el ojo.
            Ell(c, mf.At(-0.28f, 0.5f), rx * 0.5f, ry * 0.5f, angle, Mantle, 4.02f, 0f, g).WithBump(Patterns.Lumps(2f, 0.5f, 619), 0.8f);

            // Pliegues del manto: surcos que siguen las costillas, del cuello a la coronilla.
            foreach (float lat in new[] { -0.75f, -0.2f, 0.32f })
            {
                var pts = new List<Vector2>();
                for (float U = -0.86f; U <= 0.92f; U += 0.12f)
                {
                    if (lat > 0f && U < -0.1f) continue; // no cruzan la cara
                    pts.Add(mf.At(U, lat * Mathf.Sqrt(Mathf.Max(0f, 1f - U * U)) * 0.97f));
                }
                c.Fold(pts, g, 2, 1, null);
            }

            // Crestas claras de las costillas (franjas pálidas entre los pliegues, como en la hoja).
            foreach (float lat in new[] { 0.06f, 0.56f })
            {
                var pts = new List<Vector2>();
                for (float U = -0.5f; U <= 0.86f; U += 0.12f)
                {
                    if (lat > 0.3f && U < 0.05f) continue;
                    pts.Add(mf.At(U, lat * Mathf.Sqrt(Mathf.Max(0f, 1f - U * U)) * 0.97f));
                }
                c.Mark(pts, 1, g, Mantle, 3, 1);
            }

            // Venas que se ramifican (relieve fino: surco y cresta).
            DrawVein(c, mf, g, new[] { V(0.05f, 0.62f), V(0.25f, 0.46f), V(0.42f, 0.22f), V(0.58f, 0.02f), V(0.72f, -0.2f), V(0.8f, -0.42f) });
            DrawVein(c, mf, g, new[] { V(0.42f, 0.22f), V(0.62f, 0.3f), V(0.78f, 0.24f) });
            DrawVein(c, mf, g, new[] { V(-0.55f, -0.5f), V(-0.3f, -0.62f), V(-0.02f, -0.58f), V(0.22f, -0.7f), V(0.46f, -0.66f) });
            DrawVein(c, mf, g, new[] { V(-0.02f, -0.58f), V(0.08f, -0.36f), V(0.2f, -0.26f) });

            // Poros y papilas en grupos (puntos hondos con un píxel claro encima).
            for (int k = 0; k < 8; k++)
            {
                float u = -0.3f + PixelCanvas.Hash(k, 3, 707) * 1.1f, v = -0.85f + PixelCanvas.Hash(k, 5, 709) * 1.2f;
                if (u * u + v * v > 0.72f) continue;
                var at = mf.At(u, v);
                c.Dot(at, -2, g, null);
                c.Dot(Add(at, V(0f, 1f)), 1, g, null);
            }

            // Brillos húmedos en la cúpula.
            c.Glint(mf.At(0.55f, 0.42f), g, 0, null);
            c.Glint(mf.At(0.78f, 0.05f), g, 0, null);
            c.Dot(mf.At(0.5f, 0.32f), 1, g, null);
            return mf;
        }

        void DrawVein(ShadedCanvas c, MantleFrame mf, int g, Vector2[] path)
        {
            var pts = new Vector2[path.Length];
            for (int i = 0; i < path.Length; i++) pts[i] = mf.At(path[i].x, path[i].y);
            c.Fold(pts, g, 2, 1, null);
        }

        // ------------------------------------------------------------------
        // Barba y ojo
        // ------------------------------------------------------------------

        void DrawBeard(ShadedCanvas c, Pose p, MantleFrame mf, float ph, float amp, float thin)
        {
            float[] u = { -0.6f, -0.72f, -0.82f, -0.9f, -0.95f };
            float[] v = { 0.8f, 0.64f, 0.46f, 0.26f, 0.06f };
            float[] len = { 10f, 12.5f, 13.5f, 11.5f, 9f };
            float[] curl = { 60f, -40f, 70f, -55f, 45f };
            for (int k = 0; k < u.Length; k++)
            {
                var root = mf.At(u[k], v[k]);
                float a0 = -90f + (v[k] - 0.4f) * 30f + p["beard"] - p["lean"] * 0.6f;
                var pts = Chain(root, a0, len[k] * Mathf.Lerp(0.6f, 1f, thin), curl[k], 55f * amp, ph * 1.2f + k * 0.9f, 8, 1.8f, 4f);
                bool slate = k % 2 == 0;
                DrawTentacle(c, pts, (slate ? 1.35f : 1.25f) * thin, 0.4f, slate ? SlateTent : TentFlesh, 6f + k * 0.02f, k % 2 == 0 ? 0f : -0.05f,
                             c.NewGroup(), 0, 0f, slate ? TipPale : TipSlate, 0.35f);
            }
        }

        /// <summary>
        /// Ojo grande: cuenca honda, aro rojizo, iris naranja que brilla (emisivo), núcleo amarillo, pupila horizontal de
        /// pulpo, brillo húmedo, ceja carnosa encima y el párpado que baja al recibir daño o morir.
        /// </summary>
        void DrawEye(ShadedCanvas c, Pose p, MantleFrame mf)
        {
            float glow = p["eye"], lid = Mathf.Clamp01(p["lid"]);
            float r = (3.2f + 0.5f * Mathf.Clamp01(glow) - 0.35f * Mathf.Clamp01(-glow)) * Mathf.Sqrt(p["mp"]);
            var e = mf.At(-0.28f, 0.58f);
            int g = c.NewGroup();
            bool hot = glow >= 0.5f, dull = glow <= -0.5f || melt > 0.4f;
            var ring = hot ? EyeRingHot : dull ? EyeRingDull : EyeRing;
            var mid = hot ? EyeMidHot : dull ? EyeMidDull : EyeMid;
            var core = hot ? EyeCoreHot : dull ? EyeCoreDull : EyeCore;

            // Cuenca y ceja.
            Ell(c, e, r + 1.3f, r + 1.1f, 0f, Socket, 6.5f, 0f, g);
            var brow = new[] { Add(e, V(-r - 1.4f, r * 0.55f + 0.6f)), Add(e, V(-r * 0.25f, r + 2.1f)), Add(e, V(r + 1.1f, r + 1.1f)) };
            int gBrow = c.NewGroup();
            for (int k = 0; k < 2; k++) Cap(c, brow[k], brow[k + 1], 1.35f, 1.15f, Mantle, 7.2f, 0f, gBrow);
            c.Ridge(new[] { Add(brow[0], V(0.3f, 0.9f)), Add(brow[1], V(0f, 1f)), Add(brow[2], V(-0.2f, 0.8f)) }, 1, gBrow, Mantle);

            // Globo encendido.
            c.Ellipse(e, r, r * 1.05f, 0f, ring, 7f, 0f, g);
            c.Ellipse(Add(e, V(0.25f, 0.2f)), r * 0.74f, r * 0.78f, 0f, mid, 7.05f, 0f, g);
            c.Ellipse(Add(e, V(0.7f, 0.8f)), r * 0.38f, r * 0.36f, 0f, core, 7.1f, 0f, g);
            // Pupila de pulpo: una raja horizontal de 1 px (más corta al telegrafiar: se contrae).
            float py = Mathf.Floor(e.y - 0.2f) + 0.5f, half = hot ? r * 0.38f : r * 0.52f;
            c.Capsule(V(e.x - half - 0.2f, py), V(e.x + half - 0.2f, py), 0.55f, 0.55f, Pupil, 7.15f, 0f, g);

            // Párpado: carne del manto que tapa el ojo desde arriba.
            if (lid > 0.05f)
            {
                float edge = r * (1f - 2.1f * lid);
                float rl = r + 0.7f;
                Tinted(c.Custom(e.x - rl, e.y - rl, e.x + rl, e.y + rl, (px, py) =>
                {
                    float dx = px - e.x, dy = py - e.y;
                    if (dx * dx + dy * dy > rl * rl || dy < edge + dx * 0.15f) return (false, N3.Front);
                    return (true, new N3(dx / rl * 0.5f, 0.35f + dy / rl * 0.4f, 1f).Normalized());
                }, Mantle, 7.18f, 0f, g));
            }
            else
            {
                c.Decal(e.x - r * 0.35f, e.y + r * 0.45f, EyeGlint);
            }
            // Pliegue del párpado inferior.
            c.Fold(new[] { Add(e, V(-r - 0.5f, -r - 0.6f)), Add(e, V(0f, -r - 1.4f)), Add(e, V(r + 0.6f, -r - 0.8f)) }, 0, 1, 1, Mantle);
        }

        // ------------------------------------------------------------------
        // Estela, tinta y restos
        // ------------------------------------------------------------------

        /// <summary>
        /// Estela del latigazo: una media luna que sigue el arco real que traza la punta del brazo entre la pose anterior
        /// y la actual (fina al principio, gruesa junto al brazo), con un núcleo más vivo pegado al borde de ataque y la
        /// línea brillante de la punta.
        /// </summary>
        void DrawWhipTrail(ShadedCanvas c, Pose p, Vector2 shoulder, Vector2[] arm, SmearSpec s, float ph, float amp)
        {
            const int steps = 8;
            int n = arm.Length - 1;
            var tip = new Vector2[steps + 1];
            var inner = new Vector2[steps + 1];
            var core = new Vector2[steps + 1];
            for (int k = 0; k <= steps; k++)
            {
                float t = k / (float)steps;
                var chain = k == steps ? arm : Chain(shoulder, Mathf.Lerp(s.FromA, p["aFa"], t), Mathf.Lerp(s.FromL, p["aFl"], t),
                                                     Mathf.Lerp(s.FromC, p["aFc"], t), 70f * amp, ph + 0.7f, n, 1.7f, 4.6f);
                tip[k] = chain[n];
                // El borde interior se separa de la punta a medida que la estela se acerca al brazo.
                inner[k] = Mix(chain[n], chain[(int)(n * 0.45f)], t * t * s.Strength);
                core[k] = Mix(chain[n], chain[(int)(n * 0.75f)], t * t * t * s.Strength);
            }
            var soft = new List<Vector2>(tip);
            for (int k = steps; k >= 0; k--) soft.Add(inner[k]);
            c.Poly(soft.ToArray(), SmearSoft, 7.55f, 0f);
            var mid = new List<Vector2>();
            for (int k = steps / 3; k <= steps; k++) mid.Add(tip[k]);
            for (int k = steps; k >= steps / 3; k--) mid.Add(core[k]);
            c.Poly(mid.ToArray(), SmearMid, 7.56f, 0f);
            for (int k = steps / 2; k < steps; k++)
            {
                float r = 0.45f + 0.45f * (k + 1) / steps * s.Strength;
                c.Capsule(tip[k], tip[k + 1], r * 0.85f, r, s.Strength >= 0.9f ? SmearCore : SmearMid, 7.7f);
            }
        }

        /// <summary>Charco de tinta: lámina brillante de borde lobulado, gotas alrededor, reflejo del borde, onda y burbujas.</summary>
        void DrawPool(ShadedCanvas c, Pose p, bool gone)
        {
            float R = p["pool"], cx = p["poolX"];
            float ry = 1.7f + R * 0.075f;
            int g = c.NewGroup();
            c.Ellipse(V(cx, ry - 0.4f), R, ry, 0f, Ink, 9f, 0f, g, 0.5f);
            // Lóbulos: el contorno no es una elipse perfecta.
            if (R > 8f)
            {
                float r1 = ry * 0.8f + 0.4f, r2 = ry * 0.7f + 0.3f;
                c.Ellipse(V(cx - R * 0.42f, r1 - 0.4f), R * 0.55f, r1, 0f, Ink, 9f, 0f, g, 0.5f);
                c.Ellipse(V(cx + R * 0.5f, r2 - 0.4f), R * 0.48f, r2, 0f, Ink, 9f, 0f, g, 0.5f);
            }
            // Gotas salpicadas.
            for (int k = 0; k < 4 && R >= 10f; k++)
            {
                float side = k % 2 == 0 ? 1f : -1f;
                float x = cx + side * (R + 1.5f + PixelCanvas.Hash(k, 1, 331) * 3f + k * 1.5f);
                float rd = 0.8f + PixelCanvas.Hash(k, 2, 331) * 0.8f;
                c.Ellipse(V(x, rd * 0.7f - 0.3f), rd + 0.4f, rd * 0.7f, 0f, Ink, 9f, 0f, g, 0.5f);
            }
            // Reflejo del borde de arriba, brillos y una onda alrededor de donde se hunde el cuerpo.
            c.Ridge(new[] { V(cx - R * 0.75f, ry * 1.45f - 0.7f), V(cx - R * 0.3f, ry * 1.8f - 0.6f), V(cx + R * 0.25f, ry * 1.8f - 0.6f) }, 1, g, Ink);
            c.Glint(V(cx - R * 0.38f, ry * 1.25f), g, 0, Ink);
            if (R > 14f) c.Glint(V(cx + R * 0.55f, ry * 0.95f), g, 0, Ink);
            if (R > 18f)
            {
                c.Mark(new[] { V(cx - R * 0.55f, ry * 0.7f), V(cx - R * 0.2f, ry * 0.45f), V(cx + R * 0.3f, ry * 0.55f) }, 1, g, Ink, 2, 1);
                c.Crease(new[] { V(cx - R * 0.5f, ry * 0.35f), V(cx + R * 0.25f, ry * 0.25f) }, 1, g, Ink);
            }
            // Burbujas (anillos claros) que suben del charco.
            if (gone || R > 20f)
            {
                int gb = c.NewGroup();
                c.Ellipse(V(cx + R * 0.28f, ry + 0.9f), 1.7f, 1.4f, 0f, Ink, 9.3f, 0f, gb);
                c.Dot(V(cx + R * 0.28f, ry + 0.9f), -1, gb, Ink);
                c.Glint(V(cx + R * 0.28f - 0.7f, ry + 1.7f), gb, 0, Ink);
                int gb2 = c.NewGroup();
                c.Ellipse(V(cx - R * 0.5f, ry + 0.4f), 1.1f, 0.9f, 0f, Ink, 9.3f, 0f, gb2);
                c.Glint(V(cx - R * 0.5f - 0.4f, ry + 0.9f), gb2, 0, Ink);
            }
        }

        /// <summary>
        /// Lo que queda al final: un montículo de tinta que se hunde, puntas de tentáculo que se enroscan y desaparecen en
        /// el charco y el último rescoldo del ojo.
        /// </summary>
        void DrawRemains(ShadedCanvas c, Pose p)
        {
            float cx = p["poolX"], ph = p["ph"];
            int g = c.NewGroup();
            float k = Mathf.Clamp01(p["mp"] / 0.3f);
            float mry = 1.6f + 2.2f * k;
            c.Ellipse(V(cx - 2f, mry - 0.3f), 7.5f * (0.55f + 0.45f * k), mry, 0f, Ink, 9.2f, 0f, g).WithBump(Patterns.Lumps(2f, 0.8f, 881), 1f);
            c.Glint(V(cx - 4f, mry * 1.6f), g, 0, Ink);
            c.Dot(V(cx - 3f, mry * 1.6f), 1, g, Ink);
            // Puntas de tentáculo que se hunden enroscándose.
            float[] x = { 6f, -13f, 15f };
            float[] a = { 75f, 105f, 60f };
            float[] len = { 3f + 5f * k, 2f + 4f * k, 1.5f + 3f * k };
            float[] curl = { 230f, -220f, 200f };
            for (int i = 0; i < x.Length; i++)
            {
                var tip = Chain(V(cx + x[i], 1.6f), a[i], len[i], curl[i], 30f, ph + i, 6, 1.6f, 3f);
                DrawTentacle(c, tip, 1.4f * (0.6f + 0.4f * k), 0.4f, Ink, 9.25f, 0f, c.NewGroup(), 0, 0f, InkColor, 0f);
            }
            // Rescoldo del ojo, apagándose.
            if (k > 0.8f) c.Ellipse(V(cx - 1f, mry * 1.3f), 1f, 0.9f, 0f, EyeRingDull, 9.4f, 0f, g);
            else if (k > 0.5f) c.Dot(V(cx - 1f, mry * 1.3f), 0, g, Ink);
        }
    }
}
