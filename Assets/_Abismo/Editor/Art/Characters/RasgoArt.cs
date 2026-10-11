using System.Collections.Generic;
using UnityEngine;
using static Abismo.EditorTools.Rig;

namespace Abismo.EditorTools
{
    /// <summary>
    /// Rasgo de los Profundos (de la hoja conceptual "El Eco de lo Innombrable"): mole lenta y pesada con cabeza de
    /// calamar (cúpula craneal gris verdosa con surcos, cuatro ojos naranja que brillan, una barba de tentáculos que
    /// cuelga sobre el pecho y mechones de tentáculos en la nuca), torso encorvado de piel gris verdosa con placas óseas
    /// en el lomo y el vientre, brazos largos rematados en tentáculos y un tronco carnoso rojo-morado, curvado como el de
    /// la hoja, que se abre en patas tentaculares con ventosas. Mira a la derecha; el juego lo voltea al girar.
    ///
    /// Convenciones de pose:
    ///  - lean: inclinación del torso hacia delante (grados desde la vertical) · hipY: altura de la cintura
    ///  - brazos: aF1/aF2 (delantero) y aB1/aB2 (trasero) con Rig.Limb (0 = colgando, 90 = al frente, 180 = arriba);
    ///    cF/cB = cuánto se enroscan los tentáculos de la mano (+ antihorario), sF/sB = estiramiento, oF/oB = apertura
    ///  - patas: punto de apoyo pXx (x) y pXy (cuánto se levanta) de las patas F (delantera), B (trasera),
    ///    M (lejana delantera) y N (lejana trasera)
    ///  - wave: fase de la onda que recorre todos los tentáculos · eye: -2 apagado, -1 aturdido, 0 normal, 1 telegrafía
    ///  - barba: vaivén de la barba y los mechones por inercia (-1..1) · abre: la barba se abre y asoma el pico (rugido)
    ///  - flojo: tentáculos lacios (aturdido, muerto) · cola: vaivén del tentáculo de la espalda
    /// </summary>
    public sealed class RasgoArt : CharacterArt
    {
        public override string Id => "rasgo";
        public override int FrameWidth => 192;
        public override int FrameHeight => 144;
        public override Vector2 Pivot => new Vector2(96f, 8f);

        // ------------------------------------------------------------------
        // Materiales
        // ------------------------------------------------------------------

        // Piel gris verdosa oscura y húmeda: bultos y manchas pegados a cada pieza; brillo especular de 1 px en las crestas.
        static readonly PixelMaterial Piel = new PixelMaterial(Ramp.Make("3c4b47", 5, 0.1f, 0.32f, 1.62f))
        {
            Ambient = 0.24f, Rim = 0.62f, Dither = 0.02f, SpecularAt = 0.93f, SpecularPower = 30f, Specular = PixelCanvas.Hex("bcd8cc"),
            Bump = Patterns.Lumps(3.2f, 0.9f, 5), BumpStrength = 0.5f, Detail = Patterns.Mottle(2.6f, 0.77f, -1, 19),
        };
        // Cúpula del cráneo y cara: gris verdosa clara (la hoja la pinta gris menta) y muy húmeda.
        static readonly PixelMaterial Cupula = new PixelMaterial(Ramp.Make("586e68", 5, 0.1f, 0.3f, 1.62f))
        {
            Ambient = 0.25f, Rim = 0.55f, Dither = 0.02f, SpecularAt = 0.9f, SpecularPower = 28f, Specular = PixelCanvas.Hex("dff2e8"),
            Bump = Patterns.Lumps(4.5f, 0.7f, 9), BumpStrength = 0.45f, Detail = Patterns.Mottle(3f, 0.76f, -1, 23),
        };
        static readonly PixelMaterial PielBarba = new PixelMaterial(Ramp.Make("4f6560", 4, 0.1f, 0.34f, 1.65f))
        {
            Ambient = 0.26f, Rim = 0.7f, Dither = 0f, SpecularAt = 0.9f, SpecularPower = 22f, Specular = PixelCanvas.Hex("d4ece0"),
        };
        // Placas óseas del lomo, hombros y antebrazos: hueso verdoso con vetas y manchas.
        static readonly PixelMaterial Placa = new PixelMaterial(Ramp.Make("59645b", 5, 0.08f, 0.3f, 1.55f))
        {
            Ambient = 0.26f, Rim = 0.65f, Dither = 0f, Gloss = 0.12f, Bump = Patterns.Grain(2.6f, 0.5f, 3), BumpStrength = 0.35f,
            Detail = Patterns.Mottle(2.2f, 0.75f, -1, 29),
        };
        static readonly PixelMaterial PlacaVientre = new PixelMaterial(Ramp.Make("5f6153", 4, 0.08f, 0.34f, 1.5f))
        {
            Ambient = 0.28f, Rim = 0.4f, Dither = 0f, Bump = Patterns.Ridges(2.2f, 0.5f), BumpStrength = 0.4f,
        };
        // Carne rojo-morada del tronco, las patas y el tentáculo de la espalda: húmeda y brillante.
        static readonly PixelMaterial Carne = new PixelMaterial(Ramp.Make("6e3443", 5, 0.1f, 0.3f, 1.6f))
        {
            Ambient = 0.24f, Rim = 0.6f, Dither = 0.02f, SpecularAt = 0.86f, SpecularPower = 24f, Specular = PixelCanvas.Hex("f2c8cf"),
            Bump = Patterns.Lumps(2.6f, 0.6f, 31), BumpStrength = 0.4f, Detail = Patterns.Mottle(2.4f, 0.8f, -1, 37),
        };
        static readonly Color32 CarneTinte = PixelCanvas.Hex("7a3446");
        static readonly PixelMaterial Ventosa = new PixelMaterial(Ramp.Make("b0898d", 4, 0.08f, 0.45f, 1.3f)) { Ambient = 0.34f, Rim = 0.3f, Dither = 0f };
        static readonly PixelMaterial VentosaGris = new PixelMaterial(Ramp.Make("93a499", 4, 0.08f, 0.45f, 1.28f)) { Ambient = 0.34f, Rim = 0.3f, Dither = 0f };
        static readonly PixelMaterial Boca = new PixelMaterial(Ramp.Make("3a1626", 3, 0.05f, 0.5f, 1.3f)) { Ambient = 0.5f, Outline = false };
        static readonly PixelMaterial Pico = new PixelMaterial(Ramp.Make("b0a27e", 4, 0.08f, 0.4f, 1.3f)) { Ambient = 0.35f, Gloss = 0.3f, Dither = 0f };
        static readonly PixelMaterial Tinta = new PixelMaterial(Ramp.Make("221728", 4, 0.06f, 0.5f, 2f)) { Ambient = 0.2f, Gloss = 0.9f, Rim = 0.3f, Dither = 0f };
        static readonly PixelMaterial Polvo = new PixelMaterial(Ramp.Make("5f594f", 4, 0.06f, 0.55f, 1.5f))
        {
            Ambient = 0.42f, Rim = 0.3f, Dither = 0f, Outline = false, ReceivesContactShadow = false,
        };
        static readonly PixelMaterial Cascote = new PixelMaterial(Ramp.Make("4f4d46", 4, 0.06f, 0.4f, 1.6f)) { Ambient = 0.3f, Rim = 0.5f, Dither = 0f };

        // Ojos naranja (capa emisiva: brillan a oscuras y el bloom los realza).
        static readonly PixelMaterial OjoAro = PixelMaterial.Glow("8a3210");
        static readonly PixelMaterial OjoMedio = PixelMaterial.Glow("e2661a");
        static readonly PixelMaterial OjoNucleo = PixelMaterial.Glow("ffb84e");
        static readonly PixelMaterial OjoAroVivo = PixelMaterial.Glow("c8430c");
        static readonly PixelMaterial OjoMedioVivo = PixelMaterial.Glow("ff8c2a");
        static readonly PixelMaterial OjoNucleoVivo = PixelMaterial.Glow("fff0b0");
        static readonly PixelMaterial OjoApagado = PixelMaterial.Glow("3e2418");
        static readonly PixelMaterial OjoApagadoMedio = PixelMaterial.Glow("6a3a20");
        static readonly PixelMaterial Pupila = PixelMaterial.Glow("1a0a08");
        static readonly PixelMaterial OjoBrillo = PixelMaterial.Glow("fff8e6");
        static readonly Color32 LuzOjo = PixelCanvas.Hex("ff7a2a");

        // Estelas.
        static readonly PixelMaterial EstelaNucleo = new PixelMaterial(new[] { new Color32(236, 244, 228, 240) }) { Emissive = true, Outline = false, ReceivesContactShadow = false };
        static readonly PixelMaterial EstelaMedia = new PixelMaterial(new[] { new Color32(168, 198, 182, 200) }) { Emissive = true, Outline = false, ReceivesContactShadow = false };
        static readonly PixelMaterial EstelaSuave = new PixelMaterial(new[] { new Color32(92, 126, 116, 140) }) { Emissive = true, Outline = false, ReceivesContactShadow = false };

        // ------------------------------------------------------------------
        // Poses base
        // ------------------------------------------------------------------

        const float Brazo1 = 17f, Brazo2 = 15f, Dedo = 26f;

        static Pose Idle => new Pose().With(
            ("x", 0f), ("hipY", 42f), ("lean", 26f), ("head", 0f), ("neck", 0f),
            ("aF1", 16f), ("aF2", 46f), ("cF", 140f), ("sF", 1f), ("oF", 0.3f),
            ("aB1", 18f), ("aB2", 40f), ("cB", 120f), ("sB", 1f), ("oB", 0.3f),
            ("pFx", 25f), ("pBx", -27f), ("pMx", 10f), ("pNx", -12f),
            ("eye", 0f), ("barba", 0f), ("abre", 0f), ("flojo", 0f), ("cola", 0f));

        public override List<AnimSpec> Animations() => new List<AnimSpec>
        {
            new AnimSpec("idle", 6, 7f, true, f => DrawIdle(f)),
            new AnimSpec("walk", 8, 8f, true, f => DrawWalk(f)),
            new AnimSpec("windup", 6, 9f, false, f => DrawWindup(f)),
            new AnimSpec("attack", 4, 18f, false, f => DrawAttack(f)),
            new AnimSpec("slam_windup", 7, 8f, false, f => DrawSlamWindup(f)),
            new AnimSpec("slam", 4, 16f, false, f => DrawSlam(f)),
            new AnimSpec("recover", 6, 8f, false, f => DrawRecover(f)),
            new AnimSpec("hurt", 3, 12f, false, f => DrawHurt(f)),
            new AnimSpec("stagger", 6, 8f, true, f => DrawStagger(f)),
            new AnimSpec("death", 12, 12f, false, f => DrawDeath(f)),
        };

        // ------------------------------------------------------------------
        // Animaciones
        // ------------------------------------------------------------------

        ShadedCanvas DrawIdle(int f)
        {
            float t = f / 6f * Mathf.PI * 2f;
            var p = Idle.With(
                ("hipY", 42f + Mathf.Cos(t) * 0.7f), ("lean", 26f + Mathf.Sin(t) * 1.2f), ("head", Mathf.Sin(t - 0.7f) * 2f),
                ("aF1", 16f + 3f * Mathf.Sin(t + 0.5f)), ("aF2", 46f + 5f * Mathf.Sin(t + 1f)), ("cF", 140f + 25f * Mathf.Sin(t + 1.5f)),
                ("aB1", 18f + 3f * Mathf.Sin(t + 2f)), ("aB2", 40f + 5f * Mathf.Sin(t + 2.5f)), ("cB", 120f + 20f * Mathf.Sin(t + 3f)),
                ("barba", -0.15f * Mathf.Sin(t - 0.5f)), ("eye", 0.1f + 0.1f * Mathf.Sin(t * 2f)), ("wave", t), ("cola", Mathf.Sin(t + 1f)));
            return Draw(p);
        }

        /// <summary>Paso de una pata tentacular: apoyada se desliza hacia atrás; en el aire avanza levantándose.</summary>
        static void Paso(Pose p, string pata, float fase, float centro, float zancada, float alza)
        {
            fase = Mathf.Repeat(fase, 1f);
            const float apoyo = 0.6f;
            if (fase < apoyo)
            {
                p["p" + pata + "x"] = centro + zancada * (0.5f - fase / apoyo);
                p["p" + pata + "y"] = 0f;
            }
            else
            {
                float s = (fase - apoyo) / (1f - apoyo);
                float e = s * s * (3f - 2f * s);
                p["p" + pata + "x"] = centro - zancada * 0.5f + zancada * e;
                p["p" + pata + "y"] = Mathf.Sin(s * Mathf.PI) * alza;
            }
        }

        ShadedCanvas DrawWalk(int f)
        {
            float ph = f / 8f;
            float t = ph * Mathf.PI * 2f;
            var p = Idle.With(
                ("hipY", 42f + 1.1f * Mathf.Cos(t * 2f)), ("lean", 28f + 1.5f * Mathf.Sin(t * 2f + 0.6f)), ("x", 0.8f * Mathf.Sin(t)),
                ("head", -2f + 2f * Mathf.Sin(t * 2f - 1f)),
                ("aF1", 16f + 14f * Mathf.Sin(t)), ("aF2", 44f + 14f * Mathf.Sin(t - 0.6f)), ("cF", 130f + 30f * Mathf.Sin(t - 1.2f)),
                ("aB1", 16f - 14f * Mathf.Sin(t)), ("aB2", 38f - 14f * Mathf.Sin(t - 0.6f)), ("cB", 120f - 30f * Mathf.Sin(t - 1.2f)),
                ("barba", -0.3f * Mathf.Sin(t * 2f - 1.2f)), ("eye", 0.15f), ("wave", t * 2f), ("cola", Mathf.Sin(t)));
            Paso(p, "F", ph, 25f, 14f, 6f);
            Paso(p, "B", ph + 0.5f, -27f, 14f, 6f);
            Paso(p, "M", ph + 0.25f, 10f, 10f, 4f);
            Paso(p, "N", ph + 0.75f, -12f, 10f, 4f);
            return Draw(p);
        }

        // Barrido (parable, el juego lo tiñe de naranja): echa el brazo delantero atrás y arriba, enroscando los
        // tentáculos; el cuerpo se tuerce hacia atrás y los ojos se encienden.
        static Pose SweepWind => Idle.With(
            ("x", -3f), ("hipY", 43.5f), ("lean", 10f), ("head", 10f), ("neck", -4f),
            ("aF1", -100f), ("aF2", -122f), ("cF", -120f), ("sF", 0.9f), ("oF", 0.7f),
            ("aB1", 40f), ("aB2", 70f), ("cB", 90f),
            ("pFx", 29f), ("pBx", -29f), ("pMx", 12f), ("pNx", -14f), ("eye", 1f), ("barba", 0.6f));

        ShadedCanvas DrawWindup(int f)
        {
            var keys = new Keyframes()
                .Key(0, Idle.With(("eye", 0.35f), ("lean", 30f), ("hipY", 41f), ("aF1", 20f), ("aF2", 50f), ("barba", -0.2f)))
                .Key(2, SweepWind.With(("aF1", -80f), ("aF2", -100f), ("lean", 13f)), Ease.Out)
                .Key(4, SweepWind)
                .Key(5, SweepWind.With(("aF1", -106f), ("aF2", -132f), ("lean", 7f), ("x", -3.5f), ("eye", 1.2f), ("cF", -135f)));
            var p = keys.Evaluate(f);
            p["wave"] = f * 0.9f;
            p["cola"] = Mathf.Sin(f * 0.8f);
            return Draw(p);
        }

        static Pose SweepHit => Idle.With(
            ("x", 5f), ("hipY", 41f), ("lean", 38f), ("head", -6f), ("neck", 4f),
            ("aF1", 78f), ("aF2", 92f), ("cF", -12f), ("sF", 1.35f), ("oF", 0.2f),
            ("aB1", -40f), ("aB2", -20f), ("cB", 120f),
            ("pFx", 28f), ("pBx", -29f), ("pMx", 12f), ("pNx", -14f), ("eye", 1f), ("barba", -0.8f));

        ShadedCanvas DrawAttack(int f)
        {
            var keys = new Keyframes()
                .Key(0, SweepHit)
                .Key(1, SweepHit.With(("x", 6f), ("lean", 41f), ("aF1", 88f), ("aF2", 97f), ("cF", 12f), ("sF", 1.4f), ("barba", -1f)))
                .Key(2, SweepHit.With(("x", 5f), ("lean", 38f), ("aF1", 58f), ("aF2", 42f), ("cF", 140f), ("sF", 1.12f), ("barba", -0.4f), ("eye", 0.7f)))
                .Key(3, SweepHit.With(("x", 4f), ("lean", 34f), ("aF1", 40f), ("aF2", 32f), ("cF", 160f), ("sF", 1.05f), ("barba", 0f), ("eye", 0.5f)));
            var p = keys.Evaluate(f);
            p["wave"] = 5f + f * 1.1f;
            Estela e = default;
            if (f == 0) e = new Estela { Tipo = 1, Desde = 196f, Hasta = 352f, Fuerza = 1f };
            else if (f == 1) e = new Estela { Tipo = 1, Desde = 262f, Hasta = 358f, Fuerza = 0.6f };
            return Draw(p, e);
        }

        // Golpe aplastante (imparable, el juego lo tiñe de rojo): se yergue sobre las patas, alza los dos brazos por
        // encima de la cabeza en V con los tentáculos abiertos como garras, abre la barba (asoma el pico) y los ojos laten.
        static Pose SlamUp => Idle.With(
            ("x", -2f), ("hipY", 46f), ("lean", -4f), ("head", 14f), ("neck", -8f),
            ("aF1", 150f), ("aF2", 168f), ("cF", -120f), ("sF", 1f), ("oF", 0.85f),
            ("aB1", -160f), ("aB2", -172f), ("cB", 120f), ("sB", 1f), ("oB", 0.85f),
            ("pFx", 27f), ("pBx", -28f), ("pMx", 11f), ("pNx", -14f), ("eye", 1f), ("abre", 1f), ("barba", 0.3f));

        ShadedCanvas DrawSlamWindup(int f)
        {
            var keys = new Keyframes()
                .Key(0, Idle.With(("hipY", 40f), ("lean", 34f), ("head", -4f), ("aF1", 30f), ("aF2", 60f), ("aB1", 30f), ("aB2", 60f),
                                  ("eye", 0.45f), ("barba", -0.3f)))
                .Key(2, SlamUp.With(("aF1", 135f), ("aF2", 150f), ("aB1", -140f), ("aB2", -150f), ("lean", 4f), ("hipY", 44.5f)), Ease.Out)
                .Key(4, SlamUp.With(("hipY", 47f), ("lean", -7f), ("aF1", 153f), ("aF2", 172f), ("aB1", -164f), ("aB2", -176f), ("head", 16f)))
                .Key(6, SlamUp.With(("hipY", 47.5f), ("lean", -8f), ("aF1", 155f), ("aF2", 174f), ("aB1", -166f), ("aB2", -178f), ("head", 17f)));
            var p = keys.Evaluate(f);
            if (f >= 2) p["eye"] = 1f + 0.25f * (f % 2 == 0 ? 1f : 0f);
            p["wave"] = f * 0.8f;
            p["cola"] = Mathf.Sin(f * 0.9f);
            return Draw(p);
        }

        static Pose SlamHit => Idle.With(
            ("x", 9f), ("hipY", 34f), ("lean", 45f), ("head", 8f), ("neck", 4f),
            ("aF1", 42f), ("aF2", 47f), ("cF", -45f), ("sF", 1.45f), ("oF", 0.9f),
            ("aB1", 36f), ("aB2", 50f), ("cB", -40f), ("sB", 1.35f), ("oB", 0.8f),
            ("pFx", 30f), ("pBx", -27f), ("pMx", 13f), ("pNx", -14f), ("eye", 1.1f), ("barba", -1f), ("abre", 0.5f));

        ShadedCanvas DrawSlam(int f)
        {
            Pose p;
            Estela e = default;
            if (f == 0)
            {
                p = Idle.With(("x", 3f), ("hipY", 40f), ("lean", 28f), ("head", -2f), ("aF1", 118f), ("aF2", 122f), ("cF", -40f), ("sF", 1.2f), ("oF", 0.6f),
                              ("aB1", 100f), ("aB2", 110f), ("cB", -30f), ("sB", 1.15f), ("pFx", 28f), ("pBx", -28f), ("eye", 1.1f), ("barba", 0.7f), ("abre", 0.7f));
                e = new Estela { Tipo = 2, Desde = 105f, Hasta = -20f, Fuerza = 1f };
            }
            else if (f == 1) p = SlamHit;
            else if (f == 2) p = SlamHit.With(("hipY", 34.8f), ("lean", 44f), ("barba", -0.6f), ("cF", -30f));
            else p = SlamHit.With(("hipY", 35.4f), ("lean", 42f), ("barba", -0.2f), ("eye", 0.8f), ("cF", -10f), ("abre", 0.3f));
            p["wave"] = 3f + f * 0.7f;
            if (f >= 1) p["polvo"] = f;
            return Draw(p, e);
        }

        ShadedCanvas DrawRecover(int f)
        {
            var mid = Idle.With(("x", 3f), ("hipY", 38f), ("lean", 40f), ("head", -8f), ("aF1", 30f), ("aF2", 30f), ("cF", 120f), ("sF", 1.1f),
                                ("aB1", 30f), ("aB2", 34f), ("cB", 110f), ("eye", 0.35f), ("barba", -0.3f));
            var keys = new Keyframes()
                .Key(0, SlamHit.With(("hipY", 35.5f), ("lean", 42f), ("eye", 0.6f), ("abre", 0.2f), ("barba", 0f), ("cF", 20f), ("cB", 20f)))
                .Key(2, mid)
                .Key(5, Idle);
            var p = keys.Evaluate(f);
            p["wave"] = f * 0.9f;
            return Draw(p);
        }

        static Pose Hit => Idle.With(
            ("x", -3f), ("hipY", 42.5f), ("lean", 10f), ("head", 18f), ("neck", -6f),
            ("aF1", -30f), ("aF2", 0f), ("cF", -60f), ("aB1", -40f), ("aB2", -18f), ("cB", -40f),
            ("barba", 0.9f), ("eye", 1.3f), ("abre", 0.5f));

        ShadedCanvas DrawHurt(int f)
        {
            var keys = new Keyframes().Key(0, Hit, Ease.Out).Key(2, Pose.Lerp(Hit, Idle, 0.6f));
            var p = keys.Evaluate(f);
            p["wave"] = f * 1.2f;
            return Draw(p);
        }

        ShadedCanvas DrawStagger(int f)
        {
            float t = f / 6f * Mathf.PI * 2f;
            var p = Idle.With(
                ("hipY", 38f + 0.8f * Mathf.Sin(t)), ("lean", 34f + 4f * Mathf.Sin(t)), ("x", 1.5f * Mathf.Sin(t)),
                ("head", -12f + 7f * Mathf.Sin(t + 1f)), ("neck", 6f),
                ("aF1", 0f + 5f * Mathf.Sin(t + 0.6f)), ("aF2", 6f + 6f * Mathf.Sin(t + 1.2f)), ("cF", 50f),
                ("aB1", 6f + 5f * Mathf.Sin(t + 1.6f)), ("aB2", 10f + 6f * Mathf.Sin(t + 2.2f)), ("cB", 40f),
                ("barba", 0.4f * Mathf.Sin(t - 1f)), ("flojo", 1f), ("eye", f == 2 || f == 3 ? -1f : -0.6f), ("wave", t), ("cola", Mathf.Sin(t)));
            return Draw(p);
        }

        ShadedCanvas DrawDeath(int f)
        {
            var reel = Idle.With(("x", -5f), ("hipY", 41f), ("lean", 4f), ("head", 22f), ("aF1", -50f), ("aF2", -20f), ("aB1", -60f), ("aB2", -40f),
                                 ("barba", 1f), ("eye", 0.8f), ("abre", 0.8f));
            var lurch = Idle.With(("x", -2f), ("hipY", 37f), ("lean", 38f), ("head", -10f), ("aF1", 30f), ("aF2", 20f), ("aB1", 20f), ("aB2", 10f),
                                  ("barba", -0.6f), ("eye", 0.2f), ("flojo", 0.5f));
            var buckle = lurch.With(("x", 2f), ("hipY", 28f), ("lean", 56f), ("head", -16f), ("aF1", 60f), ("aF2", 70f), ("cF", 80f),
                                    ("aB1", 50f), ("aB2", 60f), ("pFx", 30f), ("pBx", -30f), ("pMx", 14f), ("pNx", -16f), ("eye", 0f), ("flojo", 0.8f));
            var fall = buckle.With(("x", 6f), ("hipY", 19f), ("lean", 78f), ("head", -40f), ("aF1", 68f), ("aF2", 30f), ("cF", -20f), ("aB1", 58f), ("aB2", 28f), ("cB", -20f),
                                   ("pFx", 33f), ("pBx", -33f), ("eye", -0.5f), ("flojo", 1f));
            var prone = fall.With(("hipY", 16f), ("lean", 84f), ("head", -62f), ("eye", -1f), ("aF1", 76f), ("aF2", 22f), ("cF", 40f), ("aB1", 64f), ("aB2", 20f), ("cB", 30f));
            var keys = new Keyframes()
                .Key(0, Hit, Ease.Out)
                .Key(1, reel)
                .Key(3, lurch, Ease.In)
                .Key(5, buckle, Ease.In)
                .Key(7, fall, Ease.In)
                .Key(8, prone)
                .Key(9, prone.With(("hipY", 16.6f), ("lean", 82f)))
                .Key(11, prone.With(("hipY", 15.6f), ("eye", -2f), ("flojo", 1.3f)));
            var p = keys.Evaluate(f);
            p["wave"] = f * (f < 8 ? 1f : 0.5f);
            p["tinta"] = Mathf.Clamp01((f - 7f) / 4f);
            if (f == 7 || f == 8) p["polvo"] = f - 6;
            if (f >= 10) p["eye"] = -2f;
            return Draw(p);
        }

        // ------------------------------------------------------------------
        // Dibujo del personaje a partir de una pose
        // ------------------------------------------------------------------

        struct Estela
        {
            /// <summary>0 = nada, 1 = barrido horizontal (pasa por delante del cuerpo), 2 = golpe por encima de la cabeza.</summary>
            public int Tipo;
            public float Desde, Hasta, Fuerza;
        }

        ShadedCanvas Draw(Pose p, Estela estela = default)
        {
            var c = NewFrame();
            float lean = p["lean"], ta = 90f - lean;
            float wave = p["wave"];
            var W = V(p["x"] - 6f, p["hipY"]);
            var U = Dir(ta);
            var B = Dir(ta + 90f);
            Vector2 T(float u, float v) => Add(W, Add(Scale(U, u), Scale(B, v)));
            // Donde el tronco se abre en patas
            var S = V(W.x + 4f, Mathf.Max(W.y - 26f, 9.5f));

            // --- Charco de tinta (muerte) ---
            float tinta = p["tinta"];
            if (tinta > 0.01f) Charco(c, V(W.x + 18f, 0.5f), tinta);
            float polvo = p["polvo"];

            // --- Tentáculo de la espalda (rojo-morado, detrás de todo) ---
            {
                var r = Radios(14, 4.6f, 0.6f, 0.9f);
                var pts = Linea(T(10f, 9f), 246f - lean * 0.2f + p["cola"] * 6f, 54f, -200f, wave + 0.8f, 16f, r);
                Tentaculo(c, pts, r, Carne, -1f, -0.1f, c.NewGroup(), -1f, Ventosa, 0.2f);
            }

            // --- Patas lejanas, brazo trasero y pata trasera ---
            Pata(c, Add(S, V(2f, -2f)), V(p["pMx"], 2f + p["pMy"]), 1f, 12f, 190f, wave + 1.6f, 4.6f, 0.4f, -0.18f);
            Pata(c, Add(S, V(-3f, -2f)), V(p["pNx"], 2f + p["pNy"]), -1f, 12f, 190f, wave + 3.1f, 4.6f, 0.5f, -0.2f);

            var shoulderF = T(19f, -1f);
            var shoulderB = T(20f, 5f);
            Brazo(c, shoulderB, p["aB1"], p["aB2"], p["cB"], p["sB"], p["oB"], wave + 2.2f, 1f, -0.14f);

            Pata(c, Add(S, V(-5f, -1f)), V(p["pBx"], 2f + p["pBy"]), -1f, 16f, 230f, wave + 0.7f, 5.8f, 1.5f, -0.1f);

            // --- Tronco carnoso: baja de la cintura en curva (como en la hoja) y se abre en patas ---
            int gTronco = c.NewGroup();
            var arriba = T(1f, 0f);
            var ctrl = V((arriba.x + S.x) * 0.5f - 6f, (arriba.y + S.y) * 0.5f);
            var tronco = Bezier(S, ctrl, arriba, 7);
            var rt = new float[tronco.Length];
            for (int i = 0; i < rt.Length; i++) rt[i] = Mathf.Lerp(Mathf.Min(8.5f, S.y - 0.6f), 10.5f, i / (float)(rt.Length - 1));
            Tentaculo(c, tronco, rt, Carne, 3f, 0f, gTronco, -1f, Ventosa, 0.05f);
            c.Ellipse(Add(S, V(0f, -1f)), 10.5f, Mathf.Min(6.5f, S.y - 1.6f), 0f, Carne, 3f, 0f, gTronco);

            // --- Torso: vientre, joroba y pecho ---
            int gTorso = c.NewGroup();
            c.Capsule(T(0f, 0.5f), T(9f, 1f), 10f, 11.5f, Piel, 4f, 0f, gTorso);
            c.Ellipse(T(15f, 4f), 12.5f, 12f, ta, Piel, 4f, 0f, gTorso);
            c.Ellipse(T(14f, -4f), 8.5f, 9f, ta, Piel, 4.01f, 0f, gTorso);
            // Faldón de carne gris donde el torso monta sobre el tronco
            for (int k = 0; k < 5; k++)
            {
                float v = -9f + k * 4.6f;
                var a = T(-1f, v);
                c.Capsule(a, Add(a, V(0.4f * (k - 2), -4.5f - (k % 2) * 1.5f)), 2.4f, 1.4f, Piel, 4.02f, -0.02f * k, gTorso);
            }
            Placas(c, T, ta);

            // --- Cuello y cabeza ---
            float hr = p["head"] - (lean - 26f) * 0.35f;
            var hp = Add(T(23f, -8f), Dir(ta - 40f + p["neck"], 2f));
            {
                // Punto más bajo de la cara y de la cúpula (elipses giradas)
                float ad = (hr + 112f) * Mathf.Deg2Rad;
                var dc0 = Add(hp, Rotate(V(-5f, 14f), V(0f, 0f), hr));
                var fc0 = Add(hp, Rotate(V(4f, 0.5f), V(0f, 0f), hr));
                float domeLow = dc0.y - Mathf.Sqrt(Mathf.Pow(18.5f * Mathf.Sin(ad), 2f) + Mathf.Pow(13f * Mathf.Cos(ad), 2f));
                float low = Mathf.Min(domeLow, fc0.y - 8.8f);
                if (low < 0.6f) hp = V(hp.x, hp.y + 0.6f - low);
            }
            Vector2 H(float x, float y) => Add(hp, Rotate(V(x, y), V(0f, 0f), hr));
            int gNeck = c.NewGroup();
            c.Capsule(T(19f, -1f), H(-4f, -1f), 7.5f, 6.5f, Piel, 4.6f, 0f, gNeck);
            Cabeza(c, p, hp, hr, wave);

            // --- Pata delantera ---
            Pata(c, Add(S, V(5f, -1f)), V(p["pFx"], 2f + p["pFy"]), 1f, 16f, 230f, wave, 5.8f, 7f, 0f);

            // --- Estela del golpe ---
            if (estela.Tipo == 1) EstelaBarrido(c, shoulderF, estela);
            else if (estela.Tipo == 2) EstelaGolpe(c, shoulderF, estela);

            // --- Brazo delantero: delante de todo, salvo alzado sobre la cabeza o echado atrás (entonces asoma por detrás
            //     de la cabeza y los ojos siguen viéndose) o tendido bajo la cabeza caída al morir ---
            float aF1 = p["aF1"];
            float zArmF = lean > 70f ? 4.55f : aF1 > 125f || aF1 < -60f ? 4.8f : 9f;
            Brazo(c, shoulderF, aF1, p["aF2"], p["cF"], p["sF"], p["oF"], wave, zArmF, 0f);

            // --- Polvo y cascotes del impacto ---
            if (polvo > 0.5f)
            {
                float x0 = p["tinta"] > 0f || p["hipY"] < 25f ? W.x + 14f : 40f;
                Impacto(c, x0, x0 + 34f, (polvo - 1f) * 0.5f);
            }
            return c;
        }

        // ------------------------------------------------------------------
        // Piezas
        // ------------------------------------------------------------------

        /// <summary>Altura (v, hacia la espalda) de la superficie del lomo a lo largo del torso (u).</summary>
        static float Lomo(float u)
        {
            float a = Mathf.Lerp(10.5f, 12.5f, Mathf.Clamp01(u / 9f));
            float k = (u - 15f) / 12.5f;
            float b = k * k < 1f ? 4f + 12f * Mathf.Sqrt(1f - k * k) : -99f;
            return Mathf.Max(a, b);
        }

        /// <summary>Superficie del pecho/vientre (v negativo) a lo largo del torso.</summary>
        static float Pecho(float u)
        {
            float a = -Mathf.Lerp(9.5f, 10.5f, Mathf.Clamp01(u / 9f));
            float k = (u - 14f) / 8.5f;
            float b = k * k < 1f ? -4f - 9f * Mathf.Sqrt(1f - k * k) : 99f;
            return Mathf.Min(a, b);
        }

        /// <summary>
        /// Placas óseas: escudos imbricados a lo largo del lomo (cada uno con su espolón hacia atrás, el canto iluminado,
        /// la junta y percebes agarrados) y placas segmentadas en el vientre, como las de un crustáceo.
        /// </summary>
        void Placas(ShadedCanvas c, System.Func<float, float, Vector2> T, float ta)
        {
            for (int i = 0; i < 5; i++)
            {
                float u = 1.5f + i * 5f;
                float vb = Lomo(u);
                int gp = c.NewGroup();
                var pts = new[]
                {
                    T(u - 4.4f, vb - 4.8f), T(u + 3.8f, vb - 4.6f), T(u + 4.4f, vb - 0.2f), T(u + 1.8f, vb + 1.8f),
                    T(u - 2.6f, vb + 3.6f), T(u - 4.8f, vb + 0.2f),
                };
                c.Poly(pts, Placa, 4.2f + i * 0.02f, 1.8f, 0f, gp, -0.3f, 0.25f).Frame(T(u, vb), ta);
                c.Ridge(new[] { T(u - 4.2f, vb + 0.2f), T(u - 2.6f, vb + 3f), T(u + 1.6f, vb + 1.4f), T(u + 3.8f, vb - 0.2f) }, 1, gp, Placa);
                c.Crease(new[] { T(u - 3.6f, vb - 3.8f), T(u + 3.2f, vb - 3.7f) }, 1, gp, Placa);
                c.Dot(T(u + 0.4f, vb - 1.4f), -2, gp, Placa);
            }
            HombrePez.Percebes(c, T(9f, Lomo(9f) - 1.2f), 3, 1f, 4.45f, 61);
            HombrePez.Percebes(c, T(19f, Lomo(19f) - 1.5f), 2, 1.1f, 4.45f, 67);

            for (int j = 0; j < 4; j++)
            {
                float u = 2f + j * 4.4f;
                float vf = Pecho(u);
                int gv = c.NewGroup();
                c.Poly(new[] { T(u - 2f, vf + 0.7f), T(u + 2.2f, vf + 0.4f), T(u + 2.2f, -3f), T(u - 2f, -3.4f) },
                       PlacaVientre, 4.15f + j * 0.01f, 1.4f, 0f, gv, 0.25f, -0.1f).Frame(T(u, vf), ta + 90f);
            }
        }

        /// <summary>
        /// Cabeza de calamar: cúpula craneal con surcos longitudinales y arrugas en la base, mechones de tentáculos en la
        /// nuca, cara con ceja ósea, cuatro ojos naranja (dos grandes con pupila en ranura y dos pequeños encima), boca
        /// con pico y la barba de tentáculos.
        /// </summary>
        void Cabeza(ShadedCanvas c, Pose p, Vector2 hp, float hr, float wave)
        {
            Vector2 H(float x, float y) => Add(hp, Rotate(V(x, y), V(0f, 0f), hr));
            float eye = p["eye"], abre = p["abre"], flojo = Mathf.Clamp01(p["flojo"]);
            int gHead = c.NewGroup(), gBrow = c.NewGroup();
            float ang = hr + 112f;
            var dc = H(-5f, 14f);
            var Ud = Dir(ang);
            var Vd = Dir(ang + 90f);
            Vector2 D(float u, float v) => Add(dc, Add(Scale(Ud, u), Scale(Vd, v)));

            // Mechones de tentáculos finos que cuelgan de la nuca sobre la espalda (como en la hoja)
            for (int k = 0; k < 6; k++)
            {
                var root = D(-6f + k * 3.6f, 10.5f - Mathf.Abs(k - 2.5f) * 0.4f);
                var r = Radios(8, 2f - k * 0.08f, 0.5f, 0.9f);
                float largo = 20f + k * 2.2f + PixelCanvas.Hash(k, 3, 57) * 5f;
                float a = -102f - k * 7f + hr * 0.3f + p["barba"] * 14f;
                float cur = (k % 2 == 0 ? 1f : -1f) * (35f + 40f * PixelCanvas.Hash(k, 4, 57)) * (1f - 0.7f * flojo);
                var pts = Linea(root, a, largo, cur, wave + k * 0.9f + 0.5f, 14f * (1f - 0.6f * flojo), r);
                Tentaculo(c, pts, r, Cupula, 4.7f - k * 0.005f, -0.04f - 0.02f * k, c.NewGroup(), Mathf.Sign(cur), null, 1f, CarneTinte, 0.4f);
            }
            // Dos ganchos gruesos en lo alto de la nuca (la "corona" de la hoja)
            for (int k = 0; k < 2; k++)
            {
                var root = D(9f + k * 4f, 7.5f - k * 2.5f);
                var r = Radios(7, 2.8f - k * 0.5f, 0.6f, 0.85f);
                var pts = Linea(root, ang + 70f - k * 25f, 11f - k * 2f, -150f, wave + k * 1.7f, 12f, r);
                Tentaculo(c, pts, r, Cupula, 4.9f, -0.05f, c.NewGroup(), -1f, null, 1f, CarneTinte, 0.35f);
            }

            // Cúpula
            c.Ellipse(dc, 18.5f, 13f, ang, Cupula, 5f, 0f, gHead).Soft();
            // Cara
            var fc = H(4f, 0.5f);
            var cara = c.Ellipse(fc, 9.6f, 8.6f, hr, Cupula, 5.02f, 0f, gHead);
            if (eye > 0.3f)
            {
                // La luz de los ojos tiñe la piel de alrededor cuando telegrafía.
                cara.WithTint(LuzOjo, 0.4f * Mathf.Min(1f, eye), (u, v) =>
                {
                    float d1 = (u + 1.2f) * (u + 1.2f) + (v - 3.5f) * (v - 3.5f);
                    float d2 = (u - 6.8f) * (u - 6.8f) + (v - 3.8f) * (v - 3.8f);
                    return d1 < 22f ? 1f : d2 < 12f ? 0.8f : d1 < 34f ? 0.4f : 0f;
                });
            }

            // Surcos longitudinales de la cúpula (convergen hacia la punta, como el manto de un calamar)
            foreach (float v0 in new[] { -7.5f, -1.5f, 4.5f })
            {
                var pts = new List<Vector2>();
                for (float u = -9f; u <= 15f; u += 2.4f)
                {
                    float k = Mathf.Sqrt(Mathf.Max(0.05f, 1f - (u / 19f) * (u / 19f)));
                    pts.Add(D(u, v0 * k + Mathf.Sin(u * 0.35f + v0) * 0.6f));
                }
                c.Fold(pts, gHead, 2, 1, Cupula);
            }
            // Arrugas transversales en la base de la cúpula, sobre la ceja
            for (int k = 0; k < 2; k++)
            {
                float u = -11.5f - k * 2.2f;
                c.Crease(new[] { D(u + 0.8f, -10f), D(u, -4f), D(u - 0.3f, 2f) }, 1, gHead, Cupula);
            }
            // Brillos húmedos en lo alto de la cúpula
            c.Glint(D(4f, -9.5f), gHead, 0, Cupula);
            c.Dot(D(5.5f, -9.6f), 1, gHead, Cupula);
            c.Dot(D(-2f, -10.4f), 1, gHead, Cupula);

            // Pliegues del manto en las mejillas, donde nace la barba
            c.Fold(new[] { H(-5f, 2f), H(-4f, -2.5f), H(-1.5f, -5.5f) }, gHead, 2, 1, Cupula);
            c.Crease(new[] { H(0.5f, 0.8f), H(3f, 0.2f), H(5.5f, 0.6f) }, 1, gHead, Cupula);
            c.Crease(new[] { H(9f, 1.2f), H(11f, 0.8f) }, 1, gHead, Cupula);

            // Boca con pico (solo se ve con la barba abierta)
            int gMouth = c.NewGroup();
            c.Ellipse(H(5.5f, -5.4f), 4.6f, 1.4f + 2.4f * abre, hr, Boca, 5.05f, 0f, gMouth);
            if (abre > 0.3f)
            {
                c.Poly(new[] { H(3.6f, -4.6f), H(7.6f, -4.8f), H(5.8f, -7f - 2.2f * abre) }, Pico, 5.07f, 1f, 0f, gMouth);
                c.Poly(new[] { H(4.2f, -8.4f - 2f * abre), H(7f, -8.2f - 2f * abre), H(5.2f, -6.6f - abre) }, Pico, 5.06f, 1f, -0.1f, gMouth);
            }

            // Ceja ósea abultada sobre los ojos
            c.Strand(Bezier(H(-2.8f, 6f), H(1.5f, 9.4f), H(6.2f, 7f), 5), 2.3f, 1.7f, Cupula, 5.3f, 0f, gBrow);
            c.Strand(Bezier(H(7.2f, 6.8f), H(10.4f, 9f), H(13.6f, 6.6f), 4), 1.8f, 1.3f, Cupula, 5.29f, -0.04f, gBrow);
            c.Ridge(new[] { H(-2f, 7.8f), H(1.5f, 10.4f), H(5f, 8.8f) }, 1, gBrow);
            c.Ridge(new[] { H(8f, 8.6f), H(10.4f, 9.8f), H(12.8f, 8f) }, 1, gBrow);

            // Ojos: dos grandes y dos pequeños encima
            float e = Mathf.Max(0f, eye);
            Ojo(c, H(2.8f, 4f), 2.8f + 0.5f * e, 1f, hr, eye, 5.4f, true);
            Ojo(c, H(10.8f, 4.3f), 2.1f + 0.35f * e, 0.78f, hr, eye, 5.35f, true);
            Ojo(c, H(0.5f, 11.6f), 1.25f + 0.3f * e, 1f, hr, eye, 5.4f, false);
            Ojo(c, H(7.2f, 12f), 1.05f + 0.3f * e, 0.85f, hr, eye, 5.4f, false);

            // Barba de tentáculos
            Barba(c, H, hr, wave, p["barba"], abre, p["flojo"]);
        }

        /// <summary>Ojo de calamar: cuenca oscura, iris naranja con luz interior, pupila en ranura y reflejo húmedo.</summary>
        void Ojo(ShadedCanvas c, Vector2 E, float r, float squash, float hr, float eye, float z, bool pupila)
        {
            int g = c.NewGroup();
            bool vivo = eye > 0.45f, apagado = eye < -0.25f, muerto = eye < -1.5f;
            c.Ellipse(E, (r + 0.9f) * squash, r + 0.8f, hr, Cupula, z, -0.3f, g);
            c.Mark(new[] { Add(E, Rotate(V(-(r + 0.6f) * squash, -0.6f), V(0f, 0f), hr)), Add(E, Rotate(V(0f, -r - 0.6f), V(0f, 0f), hr)), Add(E, Rotate(V((r + 0.6f) * squash, -0.4f), V(0f, 0f), hr)) }, 1, g);
            if (muerto)
            {
                c.Ellipse(E, (r - 0.2f) * squash, 0.7f, hr, Boca, z + 0.1f, 0f, g);
                return;
            }
            c.Ellipse(E, r * squash, r, hr, apagado ? OjoApagado : vivo ? OjoAroVivo : OjoAro, z + 0.1f, 0f, g);
            c.Ellipse(Add(E, V(0.2f, -0.2f)), Mathf.Max(0.6f, (r - 0.8f) * squash), Mathf.Max(0.6f, r - 0.8f), hr,
                      apagado ? OjoApagadoMedio : vivo ? OjoMedioVivo : OjoMedio, z + 0.11f, 0f, g);
            if (!apagado)
                c.Ellipse(Add(E, Rotate(V(0.3f, -0.6f), V(0f, 0f), hr)), Mathf.Max(0.6f, r * 0.42f * squash), Mathf.Max(0.55f, r * 0.34f), hr,
                          vivo ? OjoNucleoVivo : OjoNucleo, z + 0.12f, 0f, g);
            if (pupila)
            {
                float w = (vivo ? 0.4f : 0.6f) * r * squash;
                var a = Add(E, Rotate(V(-w, 0.2f), V(0f, 0f), hr));
                var b = Add(E, Rotate(V(w, 0.2f), V(0f, 0f), hr));
                c.Capsule(a, b, apagado ? 0.7f : 0.45f, apagado ? 0.7f : 0.45f, Pupila, z + 0.2f, 0f, g);
            }
            if (!apagado) HombrePez.Punto(c, Add(E, Rotate(V(-0.7f * squash, r * 0.45f), V(0f, 0f), hr)), OjoBrillo, z + 0.3f, g);
        }

        /// <summary>
        /// Barba: once tentáculos que cuelgan de la boca sobre el pecho, cada uno con su largo, su enrosque y su fase de
        /// onda (los de detrás más oscuros). "vaiven" los balancea por inercia, "abre" los separa (rugido) y "flojo" los
        /// deja lacios (aturdido, muerto).
        /// </summary>
        void Barba(ShadedCanvas c, System.Func<float, float, Vector2> H, float hr, float wave, float vaiven, float abre, float flojo)
        {
            const int n = 11;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                float arc = Mathf.Sin(t * Mathf.PI);
                var root = H(-3f + t * 16f, -3f - 3.2f * arc);
                float len = (16f + 13f * arc + PixelCanvas.Hash(i, 5, 41) * 5f) * (1f - 0.22f * abre);
                float ang = -90f + hr * 0.5f + (t - 0.5f) * (18f + abre * 80f) + vaiven * 22f + abre * (t - 0.5f) * 30f;
                float curl = (i % 2 == 0 ? 1f : -1f) * (45f + 60f * PixelCanvas.Hash(i, 7, 41)) * (1f - 0.75f * Mathf.Clamp01(flojo));
                bool cerca = i % 3 != 0;
                var r = Radios(8, cerca ? 2.1f : 1.8f, 0.5f, 1f);
                var pts = Linea(root, ang, len, curl, wave + i * 0.83f, 13f * (1f - 0.6f * Mathf.Clamp01(flojo)), r);
                Tentaculo(c, pts, r, PielBarba, cerca ? 6.5f + i * 0.01f : 5.01f + i * 0.001f, cerca ? 0f : -0.1f, c.NewGroup(),
                          Mathf.Sign(curl), null, 1f, CarneTinte, 0.25f);
            }
        }

        /// <summary>
        /// Brazo: bíceps abultado y antebrazo nudoso con placas óseas; la mano es un manojo de tres tentáculos (el central
        /// más largo) con ventosas por dentro que se enroscan, se estiran (latigazo) y se abren en abanico.
        /// </summary>
        void Brazo(ShadedCanvas c, Vector2 hombro, float a1, float a2, float enrosque, float estira, float abre, float wave,
                   float z, float shade)
        {
            int g = c.NewGroup();
            var codo = Limb(hombro, a1, Brazo1);
            var mano = Limb(codo, a2, Brazo2);
            // Nunca por debajo del suelo (al caer o al estrellar los brazos)
            codo = V(codo.x, Mathf.Max(codo.y, 4.5f));
            mano = V(mano.x, Mathf.Max(mano.y, 3.5f));
            float angBrazo = Mathf.Atan2(codo.y - hombro.y, codo.x - hombro.x) * Mathf.Rad2Deg;
            float angAnte = a2 - 90f;

            // Tentáculos de la mano (el lejano detrás del antebrazo, el cercano delante)
            for (int k = 0; k < 3; k++)
            {
                int orden = k == 0 ? 0 : k == 1 ? 2 : 1;
                float off = (k - 1) * (16f + abre * 26f);
                float largo = (k == 1 ? Dedo : Dedo * 0.7f) * estira;
                float cur = enrosque * (k == 1 ? 1f : 0.85f) + (k - 1) * 25f;
                var r = Radios(10, k == 1 ? 3f : 2.2f, 0.5f, 0.85f);
                var raiz = Add(mano, Dir(angAnte, 1.5f));
                var pts = Linea(raiz, angAnte + off, largo, cur, wave + k * 1.1f, 16f, r);
                Tentaculo(c, pts, r, Piel, z + 0.01f * orden - 0.015f, shade, c.NewGroup(), Mathf.Sign(cur == 0f ? 1f : cur),
                          VentosaGris, 0.25f, CarneTinte, 0.55f);
            }

            // Brazo y antebrazo
            c.Capsule(hombro, codo, 5.4f, 4.2f, Piel, z, shade, g);
            c.Ellipse(Mix(hombro, codo, 0.42f), 5.8f, 4.4f, angBrazo, Piel, z + 0.001f, shade, g);
            c.Capsule(codo, mano, 4.2f, 3.3f, Piel, z + 0.002f, shade, g);
            c.Ellipse(Mix(codo, mano, 0.3f), 4.8f, 3.8f, a2 - 90f, Piel, z + 0.003f, shade, g);

            // Placas óseas: hombrera, codo y dorso del antebrazo
            PlacaSobre(c, hombro, codo, -0.25f, 0.42f, 5.4f, 2.6f, z + 0.05f, shade);
            PlacaSobre(c, hombro, codo, 0.45f, 0.95f, 4.8f, 2.2f, z + 0.04f, shade);
            PlacaSobre(c, codo, mano, -0.05f, 0.5f, 4.2f, 2f, z + 0.045f, shade);
            PlacaSobre(c, codo, mano, 0.5f, 0.95f, 3.7f, 1.8f, z + 0.044f, shade);
        }

        /// <summary>Placa ósea sobre el lado exterior (de espaldas a la luz) de un segmento a→b, de t0 a t1, con canto claro y junta.</summary>
        void PlacaSobre(ShadedCanvas c, Vector2 a, Vector2 b, float t0, float t1, float radio, float grosor, float z, float shade)
        {
            var d = Sub(b, a);
            float l = Mathf.Max(0.01f, Mathf.Sqrt(d.x * d.x + d.y * d.y));
            var u = Scale(d, 1f / l);
            var n = V(-u.y, u.x);
            if (n.x * -0.5f + n.y * 0.86f < 0f) n = Scale(n, -1f);
            Vector2 P(float t, float s) => Add(a, Add(Scale(u, t * l), Scale(n, s)));
            int g = c.NewGroup();
            float rin = radio - grosor, rout = radio + 0.8f;
            var pts = new[] { P(t0, rin), P(t0 + 0.05f, rout - 0.3f), P((t0 + t1) * 0.5f, rout + 0.4f), P(t1, rout - 0.6f), P(t1 + 0.04f, rin - 0.6f) };
            c.Poly(pts, Placa, z, 1.4f, shade, g, n.x * 0.3f, n.y * 0.3f);
            c.Ridge(new[] { P(t0 + 0.08f, rout - 0.6f), P((t0 + t1) * 0.5f, rout - 0.2f), P(t1 - 0.05f, rout - 0.9f) }, 1, g, Placa);
            c.Crease(new[] { P(t1 - 0.02f, rin), P(t1 - 0.02f, rout - 1.2f) }, 1, g, Placa);
        }

        /// <summary>
        /// Pata tentacular: arco desde la raíz hasta el punto de apoyo (como una rodilla) y punta que se arrastra por el
        /// suelo hacia fuera y se enrosca hacia arriba. Ventosas por la cara de abajo; ondula con la fase <paramref name="wave"/>.
        /// </summary>
        void Pata(ShadedCanvas c, Vector2 raiz, Vector2 apoyo, float fuera, float largoPunta, float enrosque, float wave,
                  float r0, float z, float shade)
        {
            float alza = Mathf.Max(0f, apoyo.y - 2f);
            float rApoyo = r0 * 0.5f;
            apoyo = V(apoyo.x, rApoyo * 0.9f + 0.15f + alza);
            var ctrl = V(raiz.x + (apoyo.x - raiz.x) * 0.55f, apoyo.y + (raiz.y - apoyo.y) * 0.55f + 1.5f);
            var arco = Bezier(raiz, ctrl, apoyo, 8);
            // Onda que recorre el arco
            for (int i = 1; i < arco.Length - 1; i++)
            {
                float t = i / (float)(arco.Length - 1);
                var d = Sub(arco[i + 1], arco[i - 1]);
                float l = Mathf.Max(0.01f, Mathf.Sqrt(d.x * d.x + d.y * d.y));
                var nn = V(-d.y / l, d.x / l);
                arco[i] = Add(arco[i], Scale(nn, Mathf.Sin(wave - t * 3.5f) * 1.1f * Mathf.Sin(t * Mathf.PI)));
            }
            var rPunta = Radios(8, rApoyo, 0.55f, 0.9f);
            float angPunta = (fuera > 0f ? 0f : 180f) - fuera * alza * 4f;
            var punta = Linea(apoyo, angPunta, largoPunta, enrosque * fuera * (1f - alza * 0.08f), wave + 1f, 14f, rPunta);
            var pts = new Vector2[arco.Length + punta.Length - 1];
            var r = new float[pts.Length];
            for (int i = 0; i < arco.Length; i++)
            {
                pts[i] = arco[i];
                r[i] = Mathf.Lerp(r0, rApoyo, i / (float)(arco.Length - 1));
            }
            for (int i = 1; i < punta.Length; i++)
            {
                pts[arco.Length - 1 + i] = punta[i];
                r[arco.Length - 1 + i] = rPunta[i];
            }
            Tentaculo(c, pts, r, Carne, z, shade, c.NewGroup(), -fuera, Ventosa, 0.15f);
        }

        // ------------------------------------------------------------------
        // Tentáculos
        // ------------------------------------------------------------------

        /// <summary>Radios de un tentáculo de n puntos: de r0 a r1 con una caída (exponente) hacia la punta.</summary>
        static float[] Radios(int n, float r0, float r1, float caida)
        {
            var r = new float[n];
            for (int i = 0; i < n; i++) r[i] = r1 + (r0 - r1) * Mathf.Pow(1f - i / (float)(n - 1), caida);
            return r;
        }

        /// <summary>
        /// Línea central de un tentáculo: sale de <paramref name="raiz"/> con dirección <paramref name="ang"/> (grados
        /// matemáticos), se enrosca <paramref name="enrosque"/> grados (concentrados hacia la punta, + antihorario) y
        /// ondula con una onda viajera de fase <paramref name="onda"/>. Si toca el suelo, se apoya y se arrastra por él.
        /// </summary>
        static Vector2[] Linea(Vector2 raiz, float ang, float largo, float enrosque, float onda, float amp, float[] r)
        {
            int n = r.Length;
            var pts = new Vector2[n];
            pts[0] = raiz;
            float ds = largo / (n - 1);
            var q = raiz;
            float sentido = Mathf.Cos((ang + enrosque * 0.5f) * Mathf.Deg2Rad) >= 0f ? 1f : -1f;
            for (int i = 1; i < n; i++)
            {
                float t = (i - 0.5f) / (n - 1);
                float a = ang + enrosque * (0.3f * t + 0.7f * t * t * t) + amp * t * Mathf.Sin(onda - t * 5.2f);
                var d = Dir(a, ds);
                float suelo = r[i] * 0.9f + 0.2f;
                if (q.y + d.y < suelo)
                {
                    float ny = Mathf.Max(q.y + d.y, suelo);
                    float dy = ny - q.y;
                    float dx = Mathf.Sqrt(Mathf.Max(0f, ds * ds - dy * dy));
                    float sx = Mathf.Abs(d.x) > 0.05f * ds ? Mathf.Sign(d.x) : sentido;
                    d = V(sx * dx, dy);
                }
                q = Add(q, d);
                pts[i] = q;
            }
            return pts;
        }

        /// <summary>
        /// Dibuja un tentáculo: cápsulas encadenadas que se estrechan, ventosas claras con el centro hundido por el lado
        /// <paramref name="lado"/> (+1 = a la izquierda del avance) desde la fracción <paramref name="desde"/>, arrugas
        /// en el dorso y, opcionalmente, la punta teñida de otro color.
        /// </summary>
        void Tentaculo(ShadedCanvas c, Vector2[] pts, float[] r, PixelMaterial m, float z, float shade, int g, float lado,
                       PixelMaterial ventosa, float desde, Color32 tinte = default, float tinteCant = 0f)
        {
            int n = pts.Length;
            var largos = new float[n];
            for (int i = 1; i < n; i++)
            {
                var d = Sub(pts[i], pts[i - 1]);
                largos[i] = largos[i - 1] + Mathf.Sqrt(d.x * d.x + d.y * d.y);
            }
            float total = Mathf.Max(0.01f, largos[n - 1]);
            for (int i = 0; i < n - 1; i++)
            {
                var s = c.Capsule(pts[i], pts[i + 1], r[i], r[i + 1], m, z, shade, g);
                if (tinteCant > 0f)
                {
                    float t = largos[i + 1] / total;
                    float k = Mathf.Clamp01((t - 0.45f) / 0.55f);
                    if (k > 0f) s.WithTint(tinte, tinteCant * k * k);
                }
            }
            if (ventosa == null && r[0] < 2.5f) return;

            // Recorre el tentáculo a pasos proporcionales al grosor: ventosas por un lado y arrugas por el otro.
            float sPos = desde * total;
            int idx = 0;
            while (sPos < total - 1f)
            {
                while (idx < n - 2 && largos[idx + 1] < sPos) idx++;
                float seg = Mathf.Max(0.01f, largos[idx + 1] - largos[idx]);
                float f = Mathf.Clamp01((sPos - largos[idx]) / seg);
                var pt = Mix(pts[idx], pts[idx + 1], f);
                float rad = Mathf.Lerp(r[idx], r[idx + 1], f);
                var d = Sub(pts[idx + 1], pts[idx]);
                var u = Scale(d, 1f / seg);
                var nn = V(-u.y * lado, u.x * lado);
                if (ventosa != null && rad >= 1.1f)
                {
                    float rs = rad < 1.8f ? 0.7f : rad < 3f ? 1.2f : 1.6f;
                    var at = Add(pt, Scale(nn, rad * 0.8f));
                    if (at.y - rs < 0.2f) at = V(at.x, rs + 0.2f);
                    Ventosilla(c, at, rs, ventosa, z + 0.02f, shade, g);
                }
                if (rad > 2.4f)
                {
                    // Arruga en el dorso: un surco corto de un tono
                    c.Mark(new[] { Add(pt, Scale(nn, -rad * 0.95f)), Add(pt, Scale(nn, -rad * 0.3f)) }, -1, g, m);
                }
                sPos += Mathf.Max(2.2f, rad * 2.1f);
            }
        }

        /// <summary>Ventosa: disco claro (en el centro de un píxel, para que no baile) con el centro hundido si es grande.</summary>
        void Ventosilla(ShadedCanvas c, Vector2 at, float rs, PixelMaterial m, float z, float shade, int g)
        {
            float cx = Mathf.Floor(at.x + c.OriginX) + 0.5f - c.OriginX, cy = Mathf.Floor(at.y + c.OriginY) + 0.5f - c.OriginY;
            var p = V(cx, cy);
            c.Ellipse(p, rs, rs, 0f, m, z, shade, g);
            if (rs >= 1.5f) c.Dot(p, -2, g, m);
        }

        // ------------------------------------------------------------------
        // Efectos: estelas, impacto, charco
        // ------------------------------------------------------------------

        /// <summary>Banda elíptica (estela) entre dos ángulos alrededor de un centro, fina en la cola y gruesa en la punta.</summary>
        static System.Func<float, float, (bool, N3)> Banda(Vector2 centro, float rx, float ry, float desde, float hasta, float grosor)
        {
            float a0 = Mathf.Min(desde, hasta), a1 = Mathf.Max(desde, hasta);
            bool puntaAlFinal = hasta >= desde;
            return (px, py) =>
            {
                float dx = (px - centro.x) / rx, dy = (py - centro.y) / ry;
                float rr = Mathf.Sqrt(dx * dx + dy * dy);
                if (rr > 1f) return (false, N3.Front);
                float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                while (ang < a0) ang += 360f;
                while (ang > a0 + 360f) ang -= 360f;
                if (ang > a1) return (false, N3.Front);
                float t = (ang - a0) / Mathf.Max(0.001f, a1 - a0);
                if (!puntaAlFinal) t = 1f - t;
                float rad = Mathf.Lerp(rx, ry, Mathf.Abs(Mathf.Sin(ang * Mathf.Deg2Rad)));
                float g = grosor * (0.12f + 0.88f * t * t);
                return (rr >= 1f - g / Mathf.Max(1f, rad), N3.Front);
            };
        }

        /// <summary>
        /// Estela del barrido: tres trazos (uno por tentáculo) en elipses aplastadas que pasan por delante del cuerpo,
        /// de atrás hacia delante.
        /// </summary>
        void EstelaBarrido(ShadedCanvas c, Vector2 hombro, Estela e)
        {
            for (int k = 0; k < 3; k++)
            {
                var cen = Add(hombro, V(12f - k * 2f, -6f + k * 1.5f));
                float rx = 64f - k * 6f, ry = 24f - k * 6f;
                float f = e.Fuerza * (1f - k * 0.2f);
                float x0 = cen.x - rx - 2f, y0 = cen.y - ry - 2f, x1 = cen.x + rx + 2f, y1 = cen.y + ry + 2f;
                c.Custom(x0, y0, x1, y1, Banda(cen, rx, ry, e.Desde + k * 6f, e.Hasta, 7f * f), EstelaSuave, 8.4f + k * 0.01f);
                c.Custom(x0, y0, x1, y1, Banda(cen, rx - 0.5f, ry - 0.5f, e.Desde + k * 6f, e.Hasta, 4f * f), EstelaMedia, 8.5f + k * 0.01f);
                c.Custom(x0, y0, x1, y1, Banda(cen, rx - 0.5f, ry - 0.5f, e.Desde + 20f + k * 6f, e.Hasta, 1.8f * f), EstelaNucleo, 8.6f + k * 0.01f);
            }
        }

        /// <summary>Estela del golpe: tres trazos en arco (uno por tentáculo) desde encima de la cabeza hasta el suelo delante.</summary>
        void EstelaGolpe(ShadedCanvas c, Vector2 hombro, Estela e)
        {
            for (int k = 0; k < 3; k++)
            {
                float rr = 56f - k * 7f;
                var cen = Add(hombro, V(k * 1.5f, -k * 1f));
                float f = e.Fuerza * (1f - k * 0.2f);
                float x0 = cen.x - rr - 2f, y0 = cen.y - rr - 2f, x1 = cen.x + rr + 2f, y1 = cen.y + rr + 2f;
                c.Custom(x0, y0, x1, y1, Banda(cen, rr, rr, e.Desde - k * 5f, e.Hasta, 9f * f), EstelaSuave, 8.4f + k * 0.01f);
                c.Custom(x0, y0, x1, y1, Banda(cen, rr - 0.5f, rr - 0.5f, e.Desde - k * 5f, e.Hasta, 5f * f), EstelaMedia, 8.5f + k * 0.01f);
                c.Custom(x0, y0, x1, y1, Banda(cen, rr - 0.5f, rr - 0.5f, e.Desde - 25f - k * 5f, e.Hasta, 2f * f), EstelaNucleo, 8.6f + k * 0.01f);
            }
        }

        /// <summary>Polvo y cascotes que saltan del suelo al estrellar los brazos (k: 0 = impacto, 1 = se disipa).</summary>
        void Impacto(ShadedCanvas c, float x0, float x1, float k)
        {
            for (int i = 0; i < 7; i++)
            {
                float t = i / 6f;
                float x = Mathf.Lerp(x0 - 4f, x1 + 6f, t) + (PixelCanvas.Hash(i, 3, 91) - 0.5f) * 5f;
                float r = 2.4f + 2.6f * PixelCanvas.Hash(i, 4, 91) + 2.2f * k;
                x = Mathf.Min(x, 88f - r * 1.75f);
                bool delante = i % 2 == 0;
                float z = delante ? 10f + i * 0.01f : 0.2f + i * 0.01f;
                float sh = (PixelCanvas.Hash(i, 6, 91) - 0.5f) * 0.12f - k * 0.1f - (delante ? 0f : 0.08f);
                int g = c.NewGroup();
                // Nube: bola central y dos laterales más bajas, que suben y se hinchan al disiparse.
                float y = r * 0.8f + k * 3f * PixelCanvas.Hash(i, 5, 91);
                c.Ellipse(V(x, y), r, r * 0.85f, 0f, Polvo, z, sh, g);
                c.Ellipse(V(x - r * 0.9f, y - r * 0.35f), r * 0.7f, r * 0.6f, 0f, Polvo, z - 0.001f, sh - 0.04f, g);
                c.Ellipse(V(x + r * 0.95f, y - r * 0.3f), r * 0.75f, r * 0.62f, 0f, Polvo, z - 0.002f, sh - 0.02f, g);
            }
            if (k < 0.8f)
            {
                for (int i = 0; i < 7; i++)
                {
                    float t = i / 6f;
                    float x = Mathf.Lerp(x0, x1, t) + (PixelCanvas.Hash(i, 7, 93) - 0.5f) * 6f;
                    float h = 4f + 10f * PixelCanvas.Hash(i, 8, 93) + k * 10f;
                    float s = 0.9f + 0.9f * PixelCanvas.Hash(i, 9, 93);
                    var at = V(x + (t - 0.5f) * k * 10f, h);
                    float rot = PixelCanvas.Hash(i, 10, 93) * 90f;
                    c.Poly(new[] { Add(at, Dir(rot, s * 1.4f)), Add(at, Dir(rot + 110f, s)), Add(at, Dir(rot + 200f, s * 1.2f)), Add(at, Dir(rot + 290f, s * 0.9f)) },
                           Cascote, 10.5f, 0.8f, 0f, c.NewGroup());
                }
            }
        }

        /// <summary>Charco de tinta negra-violeta que se extiende bajo el cuerpo (muerte), con reflejos.</summary>
        void Charco(ShadedCanvas c, Vector2 centro, float k)
        {
            int g = c.NewGroup();
            c.Ellipse(centro, 10f + 40f * k, 1.8f + 2.4f * k, 0f, Tinta, -3f, 0f, g, 0.4f);
            c.Ellipse(Add(centro, V(-14f * k, 0.2f)), 6f + 14f * k, 1.4f + 1.6f * k, 0f, Tinta, -2.99f, 0.05f, g, 0.4f);
            // Reflejos de la superficie mojada y el canto del charco
            c.Glint(Add(centro, V(-6f - 10f * k, 0.6f)), g, 0, Tinta);
            c.Glint(Add(centro, V(8f + 14f * k, 0.5f)), g, 0, Tinta);
            c.Ridge(new[] { Add(centro, V(-22f * k, 1.6f + 1.4f * k)), Add(centro, V(-6f * k, 2f + 1.8f * k)) }, 1, g, Tinta);
            c.Ridge(new[] { Add(centro, V(10f * k, 1.8f + 1.6f * k)), Add(centro, V(24f * k, 1.2f + 1.2f * k)) }, 1, g, Tinta);
        }
    }
}
