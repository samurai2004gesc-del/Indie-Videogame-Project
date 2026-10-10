using System.Collections.Generic;
using UnityEngine;
using static Abismo.EditorTools.Rig;

namespace Abismo.EditorTools
{
    /// <summary>
    /// El Arcipreste de las Mareas (jefe): un sacerdote-pez gigantesco, encorvado bajo una casulla carmesí con ribetes y
    /// galón de oro sobre un alba sucia. Cabeza de mero con un único ojo dorado, barba de tentáculos que se mueve sola y
    /// una mitra de oro altísima con el Signo Antiguo encendido (el acento de su paleta, verde mar). Lleva un báculo de
    /// hierro rematado en espiral con uñas de ancla y una brasa verde. Mira a la derecha.
    ///
    /// Claves de pose:
    ///  - "x","y": desplazamiento; "crouch": baja la cadera; "lean": inclina el tronco (grados, + = hacia delante)
    ///  - "head": cabeceo (+ = mirar abajo); "jaw": boca abierta (0..1)
    ///  - "armF1","armF2","armB1","armB2": brazo delantero/trasero (0 = colgando, 90 = delante, 180 = arriba)
    ///  - "staff": ángulo del báculo (90 = vertical), "grip": distancia de la mano a la punta inferior, "plant": 1 = clavado
    ///  - "beard","robe": fase del movimiento secundario; "sway": amplitud; "glow": brillo de la mitra (0..1.5)
    ///  - "orb": tamaño de la brasa del báculo; "footF","footB": pasos; "fall": se derrumba (grados)
    /// </summary>
    public sealed class ArcipresteArt : CharacterArt
    {
        public override string Id => "arcipreste";
        public override int FrameWidth => 288;
        public override int FrameHeight => 240;
        public override Vector2 Pivot => new Vector2(144f, 8f);

        const float HipY = 56f;
        const float StaffLength = 168f;
        const float HS = 1.22f; // escala de la cabeza (exagerada, como en los jefes de Blasphemous)

        // ------------------------------------------------------------------
        // Materiales
        // ------------------------------------------------------------------

        static readonly PixelMaterial Alb = new PixelMaterial(Ramp.Make("8a8170", 5, 0.1f, 0.3f, 1.45f)) { Rim = 0.6f, Ambient = 0.22f, Dither = 0.04f };
        static readonly PixelMaterial Sleeve = new PixelMaterial(Ramp.Make("665d4e", 5, 0.1f, 0.3f, 1.5f)) { Rim = 0.6f, Ambient = 0.22f, Dither = 0.04f };
        static readonly PixelMaterial Chasuble = new PixelMaterial(Ramp.Make("7c1527", 5, 0.1f, 0.28f, 1.65f)) { Rim = 0.7f, Ambient = 0.2f, Dither = 0.04f };
        static readonly PixelMaterial Gold = new PixelMaterial(Ramp.Make("b48a33", 5, 0.12f, 0.3f, 1.6f))
        {
            Gloss = 0.7f, Rim = 0.5f, Ambient = 0.26f, Dither = 0f, SpecularAt = 0.86f, Specular = PixelCanvas.Hex("fff6d2"),
        };
        // Oro viejo del brocado (más apagado que los galones para que no compita con ellos).
        static readonly PixelMaterial Brocade = new PixelMaterial(Ramp.Make("7e5c26", 4, 0.1f, 0.4f, 1.55f)) { Gloss = 0.55f, Rim = 0.35f, Ambient = 0.16f, Dither = 0f };
        static readonly PixelMaterial Lace = new PixelMaterial(Ramp.Make("a39a84", 5, 0.1f, 0.34f, 1.42f)) { Rim = 0.55f, Ambient = 0.24f, Dither = 0f };
        static readonly PixelMaterial GemRed = new PixelMaterial(Ramp.Make("9c1830", 4, 0.06f, 0.35f, 1.5f))
        {
            Gloss = 0.6f, Rim = 0.3f, Ambient = 0.3f, Dither = 0f, SpecularAt = 0.8f, Specular = PixelCanvas.Hex("ffe8ec"),
        };
        static readonly PixelMaterial GemGreen = new PixelMaterial(Ramp.Make("1d8a72", 4, 0.06f, 0.35f, 1.5f))
        {
            Gloss = 0.6f, Rim = 0.3f, Ambient = 0.3f, Dither = 0f, SpecularAt = 0.8f, Specular = PixelCanvas.Hex("e8fff6"),
        };
        static readonly Color32 Grime = PixelCanvas.Hex("3d3a2c");
        static readonly PixelMaterial Barnacle = new PixelMaterial(Ramp.Make("a39a80", 4, 0.08f, 0.4f, 1.35f)) { Rim = 0.5f, Ambient = 0.3f, Dither = 0f };
        // Fondo hundido del medallón de la mitra (esmalte oscuro donde se engasta el Signo).
        static readonly PixelMaterial MitreRecess = new PixelMaterial(Ramp.Make("2f5a50", 4, 0.06f, 0.45f, 1.4f)) { Ambient = 0.4f, Rim = 0.2f, Dither = 0f, Outline = false };
        static readonly PixelMaterial GoldDark = new PixelMaterial(Ramp.Make("6c4e20", 4, 0.1f, 0.4f, 1.5f)) { Gloss = 0.4f, Rim = 0.4f, Ambient = 0.3f, Dither = 0f };
        static readonly PixelMaterial Skin = new PixelMaterial(Ramp.Make("4c675f", 5, 0.12f, 0.3f, 1.55f)) { Rim = 0.65f, Gloss = 0.18f, Ambient = 0.22f, Dither = 0.03f };
        static readonly PixelMaterial Belly = new PixelMaterial(Ramp.Make("8f9878", 4, 0.1f, 0.4f, 1.4f)) { Rim = 0.4f, Ambient = 0.3f, Dither = 0f };
        static readonly PixelMaterial Tentacle = new PixelMaterial(Ramp.Make("486058", 5, 0.12f, 0.3f, 1.6f)) { Rim = 0.7f, Gloss = 0.25f, Ambient = 0.22f, Dither = 0f };
        static readonly PixelMaterial Mouth = new PixelMaterial(Ramp.Make("3a1018", 3, 0.06f, 0.4f, 1.3f)) { Ambient = 0.5f, Rim = 0f, Dither = 0f, Outline = false };
        static readonly PixelMaterial Teeth = new PixelMaterial(Ramp.Make("cfc4a2", 3, 0.05f, 0.6f, 1.2f)) { Ambient = 0.5f, Rim = 0f, Dither = 0f, Outline = false };
        static readonly PixelMaterial Iron = new PixelMaterial(Ramp.Make("3b3634", 4, 0.06f, 0.4f, 1.6f)) { Gloss = 0.35f, Rim = 0.5f, Ambient = 0.26f, Dither = 0f };
        static readonly PixelMaterial Claw = new PixelMaterial(Ramp.Make("d2c8ae", 3, 0.05f, 0.5f, 1.2f)) { Ambient = 0.4f, Dither = 0f };
        static readonly PixelMaterial Brow = new PixelMaterial(Ramp.Make("2f413d", 4, 0.1f, 0.4f, 1.4f)) { Ambient = 0.3f, Rim = 0.4f, Dither = 0f };
        static readonly PixelMaterial EyeGlow = PixelMaterial.Glow("ffcf4a");
        static readonly PixelMaterial EyeCore = PixelMaterial.Glow("fff3c4");
        static readonly PixelMaterial EyeDead = new PixelMaterial(Ramp.Make("6b6040", 3, 0.05f)) { Ambient = 0.4f, Dither = 0f };
        static readonly PixelMaterial Pupil = PixelMaterial.Glow("140a04");
        static readonly PixelMaterial SignHot = PixelMaterial.Glow("d8fff0");
        static readonly PixelMaterial SignGlow = PixelMaterial.Glow("6cf7c8");
        static readonly PixelMaterial SignDim = PixelMaterial.Glow("2c7d68");
        static readonly PixelMaterial SignOff = new PixelMaterial(Ramp.Make("4f4a32", 3, 0.05f)) { Ambient = 0.4f, Dither = 0f };
        static readonly PixelMaterial OrbCore = PixelMaterial.Glow("eafff6");
        static readonly PixelMaterial OrbMid = PixelMaterial.Glow("5fe8c4");
        static readonly PixelMaterial OrbOut = PixelMaterial.Glow("2a9c8890");
        static readonly PixelMaterial OrbHalo = PixelMaterial.Glow("1d6f6650");
        static readonly PixelMaterial Spark = PixelMaterial.Glow("b8ffe6");
        static readonly PixelMaterial Smear = PixelMaterial.Glow("9cffe080");
        static readonly PixelMaterial Dust = PixelMaterial.Glow("6d6a5c90");
        static readonly Color32 Glint = PixelCanvas.Hex("fff6d8");
        static readonly Color32 Stitch = PixelCanvas.Hex("e2bd5a");
        static readonly Color32 Gill = PixelCanvas.Hex("1d2b29");

        // ------------------------------------------------------------------
        // Poses
        // ------------------------------------------------------------------

        static Pose Stand => new Pose().With(
            ("x", 0f), ("y", 0f), ("crouch", 0f), ("lean", 10f), ("head", 4f), ("jaw", 0.1f),
            ("armF1", 22f), ("armF2", 62f), ("armB1", -8f), ("armB2", 18f),
            ("staff", 92f), ("grip", 74f), ("plant", 1f),
            ("beard", 0f), ("robe", 0f), ("sway", 1f), ("glow", 0.55f), ("orb", 1f),
            ("footF", 10f), ("footB", -12f), ("liftF", 0f), ("liftB", 0f), ("fall", 0f), ("mitre", 1f));

        public override List<AnimSpec> Animations() => new List<AnimSpec>
        {
            new AnimSpec("idle", 8, 8f, true, DrawIdle),
            new AnimSpec("walk", 8, 8f, true, DrawWalk),
            new AnimSpec("intro", 12, 7f, false, DrawIntro),
            new AnimSpec("tentacles", 8, 10f, true, DrawTentacles),
            new AnimSpec("orbs", 9, 10f, false, DrawOrbs),
            new AnimSpec("charge_windup", 6, 7f, false, DrawChargeWindup),
            new AnimSpec("charge", 4, 12f, true, DrawCharge),
            new AnimSpec("recover", 6, 5f, false, DrawRecover),
            new AnimSpec("phase", 10, 6f, false, DrawPhase),
            new AnimSpec("death", 16, 10f, false, DrawDeath),
        };

        // ------------------------------------------------------------------
        // Animaciones
        // ------------------------------------------------------------------

        ShadedCanvas DrawIdle(int f)
        {
            float t = f / 8f * Mathf.PI * 2f;
            var p = Stand.With(("crouch", 1f - Mathf.Cos(t)), ("lean", 10f + 1.2f * Mathf.Sin(t - 0.4f)), ("head", 4f + 2f * Mathf.Sin(t - 1f)),
                               ("jaw", 0.1f + 0.06f * Mathf.Sin(t - 1.5f)), ("armB2", 18f + 4f * Mathf.Sin(t - 0.9f)),
                               ("beard", t), ("robe", t), ("glow", 0.5f + 0.12f * Mathf.Sin(t)), ("orb", 1f + 0.08f * Mathf.Sin(t * 2f)));
            return Draw(p, new Fx { Phase = f / 8f });
        }

        // Paso pesado: el báculo acompaña al paso delantero; la casulla se balancea con retraso.
        ShadedCanvas DrawWalk(int f)
        {
            float t = f / 8f * Mathf.PI * 2f;
            float s = Mathf.Sin(t);
            var p = Stand.With(("footF", 10f * Mathf.Cos(t)), ("footB", -10f * Mathf.Cos(t)),
                               ("liftF", Mathf.Max(0f, Mathf.Sin(t)) * 5f), ("liftB", Mathf.Max(0f, -Mathf.Sin(t)) * 5f),
                               ("crouch", 1.5f + 1.5f * Mathf.Cos(t * 2f)), ("x", 0f), ("lean", 13f + 1.5f * Mathf.Sin(t * 2f)),
                               ("head", 6f + 2f * Mathf.Sin(t * 2f - 1f)), ("plant", 0f), ("staff", 88f + 6f * s), ("grip", 70f),
                               ("armF1", 40f + 8f * s), ("armF2", 72f + 4f * s), ("armB1", -6f - 14f * s), ("armB2", 16f - 10f * s),
                               ("beard", t * 2f), ("robe", t * 2f - 0.8f), ("sway", 1.4f), ("glow", 0.55f));
            return Draw(p, new Fx { Phase = f / 8f });
        }

        // Despierta: se recoge, se alza con los brazos abiertos y clava el báculo rugiendo.
        ShadedCanvas DrawIntro(int f)
        {
            var gather = Stand.With(("crouch", 6f), ("lean", 26f), ("head", 18f), ("jaw", 0f), ("armF1", 20f), ("armF2", 40f), ("armB1", 10f), ("armB2", 30f),
                                    ("glow", 0.2f), ("orb", 0.6f));
            var rise = Stand.With(("crouch", -2f), ("lean", -6f), ("head", -14f), ("jaw", 0.5f), ("armF1", 150f), ("armF2", 165f), ("plant", 0f),
                                  ("staff", 96f), ("grip", 112f), ("y", 6f), ("armB1", -130f), ("armB2", -150f), ("glow", 1.1f), ("orb", 1.5f));
            var slam = Stand.With(("crouch", 4f), ("lean", 18f), ("head", -4f), ("jaw", 1f), ("armF1", 72f), ("armF2", 80f), ("plant", 1f),
                                  ("armB1", -100f), ("armB2", -120f), ("glow", 1.5f), ("orb", 1.6f));
            var keys = new Keyframes()
                .Key(0, Stand)
                .Key(3, gather, Ease.InOut)
                .Key(6, rise, Ease.Out)
                .Key(7, rise.With(("y", 8f), ("lean", -8f)))
                .Key(8, slam, Ease.In)
                .Key(11, slam.With(("jaw", 0.85f), ("head", -8f), ("lean", 14f), ("glow", 1.3f)));
            var p = keys.Evaluate(f);
            float t = f / 12f * Mathf.PI * 4f;
            p["beard"] = t;
            p["robe"] = t * 0.5f;
            p["sway"] = f >= 5 ? 2f : 1f;
            var fx = new Fx { Phase = f / 12f, Dust = f == 8 ? 2 : f == 9 ? 1 : 0, Sparks = f >= 5 ? 2 : 0 };
            if (f >= 8 && f <= 10) p["x"] = f % 2 == 0 ? 1f : -1f;
            return Draw(p, fx);
        }

        // Invoca tentáculos: báculo clavado, mano izquierda en alto; la mitra arde.
        ShadedCanvas DrawTentacles(int f)
        {
            float t = f / 8f * Mathf.PI * 2f;
            var p = Stand.With(("crouch", 3f + Mathf.Sin(t)), ("lean", 4f), ("head", -10f + 3f * Mathf.Sin(t * 2f)), ("jaw", 0.55f + 0.2f * Mathf.Sin(t * 2f)),
                               ("armF1", 62f), ("armF2", 78f), ("plant", 1f), ("armB1", -160f + 5f * Mathf.Sin(t * 2f)), ("armB2", -172f + 8f * Mathf.Sin(t * 2f + 1f)),
                               ("beard", t * 2f), ("robe", t), ("sway", 2.2f), ("glow", 1.25f + 0.25f * Mathf.Sin(t * 2f)), ("orb", 1.4f));
            return Draw(p, new Fx { Phase = f / 8f, Sparks = 3, HandGlow = true });
        }

        // Esferas: apunta con el báculo, la brasa crece (7 fotogramas) y estalla en el 7.º.
        ShadedCanvas DrawOrbs(int f)
        {
            var aim = Stand.With(("crouch", 3f), ("lean", 16f), ("head", 0f), ("jaw", 0.3f), ("armF1", 92f), ("armF2", 96f), ("plant", 0f), ("staff", 42f), ("grip", 104f),
                                 ("armB1", 60f), ("armB2", 80f), ("glow", 0.9f), ("orb", 1.2f));
            var keys = new Keyframes()
                .Key(0, Stand.With(("crouch", 2f), ("lean", 6f), ("head", -4f), ("armF1", 60f), ("armF2", 90f), ("plant", 0f), ("staff", 72f), ("grip", 96f), ("orb", 1.1f)))
                .Key(2, aim, Ease.Out)
                .Key(6, aim.With(("orb", 2.4f), ("glow", 1.2f), ("lean", 12f), ("crouch", 4f)))
                .Key(7, aim.With(("orb", 0.7f), ("lean", 22f), ("head", 6f), ("jaw", 0.8f), ("crouch", 5f), ("armF1", 98f), ("staff", 36f)), Ease.Out)
                .Key(8, aim.With(("orb", 0.8f), ("lean", 18f), ("jaw", 0.4f), ("crouch", 4f)));
            var p = keys.Evaluate(f);
            p["beard"] = f * 0.8f;
            p["robe"] = f * 0.5f;
            if (f >= 4 && f <= 6) p["x"] = f % 2 == 0 ? 0.8f : -0.8f;
            return Draw(p, new Fx { Phase = f / 9f, Sparks = f >= 2 && f <= 6 ? 2 : 0, OrbFlash = f == 7 ? 2 : f == 8 ? 1 : 0 });
        }

        // Embestida (preparación): se agacha, cabeza gacha, báculo en ristre.
        ShadedCanvas DrawChargeWindup(int f)
        {
            var low = Stand.With(("crouch", 12f), ("lean", 40f), ("head", 18f), ("jaw", 0.45f), ("armF1", 70f), ("armF2", 80f), ("plant", 0f), ("staff", 8f), ("grip", 122f),
                                 ("armB1", -40f), ("armB2", -20f), ("footF", 18f), ("footB", -22f), ("glow", 0.9f), ("x", -6f));
            var keys = new Keyframes()
                .Key(0, Stand.With(("crouch", 2f), ("lean", 6f), ("head", -6f), ("plant", 0f), ("staff", 84f)))
                .Key(3, low, Ease.Out)
                .Key(5, low.With(("crouch", 13f), ("x", -8f), ("head", 20f)));
            var p = keys.Evaluate(f);
            p["beard"] = f * 0.6f;
            p["robe"] = f * 0.4f;
            if (f >= 4) p["x"] += f % 2 == 0 ? 1f : -1f;
            return Draw(p, new Fx { Phase = f / 6f, Dust = f >= 4 ? 1 : 0 });
        }

        // Embestida: carrera inclinada, la casulla ondea hacia atrás.
        ShadedCanvas DrawCharge(int f)
        {
            float t = f / 4f * Mathf.PI * 2f;
            var p = Stand.With(("crouch", 9f + 2f * Mathf.Cos(t * 2f)), ("lean", 36f), ("head", 12f), ("jaw", 0.7f), ("armF1", 74f), ("armF2", 82f), ("plant", 0f),
                               ("staff", 6f), ("grip", 122f), ("armB1", -60f - 15f * Mathf.Sin(t)), ("armB2", -40f),
                               ("footF", 20f * Mathf.Cos(t)), ("footB", -20f * Mathf.Cos(t)), ("liftF", Mathf.Max(0f, Mathf.Sin(t)) * 8f), ("liftB", Mathf.Max(0f, -Mathf.Sin(t)) * 8f),
                               ("beard", t * 2f), ("robe", t * 2f), ("sway", 2.5f), ("wind", 1f), ("glow", 0.9f));
            return Draw(p, new Fx { Phase = f / 4f, Dust = f % 2 == 0 ? 1 : 0, Speed = true });
        }

        // Recuperación: jadea apoyado en el báculo.
        ShadedCanvas DrawRecover(int f)
        {
            var tired = Stand.With(("crouch", 7f), ("lean", 28f), ("head", 16f), ("jaw", 0.5f), ("armF1", 46f), ("armF2", 70f), ("plant", 1f),
                                   ("armB1", 20f), ("armB2", 40f), ("glow", 0.35f), ("orb", 0.7f));
            var keys = new Keyframes()
                .Key(0, tired.With(("lean", 32f), ("crouch", 8f)))
                .Key(3, tired.With(("lean", 24f), ("crouch", 5f), ("jaw", 0.3f)))
                .Key(5, Pose.Lerp(tired, Stand, 0.75f));
            var p = keys.Evaluate(f);
            p["beard"] = f * 0.7f;
            p["robe"] = f * 0.35f;
            p["crouch"] += f % 2 == 0 ? 0.6f : -0.6f;
            return Draw(p, new Fx { Phase = f / 6f });
        }

        // Cambio de fase: ruge con la cabeza al cielo, brazos abiertos; la mitra arde al máximo.
        ShadedCanvas DrawPhase(int f)
        {
            var roar = Stand.With(("crouch", -1f), ("lean", -10f), ("head", -26f), ("jaw", 1f), ("armF1", 120f), ("armF2", 140f), ("plant", 0f), ("staff", 100f), ("grip", 104f),
                                  ("armB1", -120f), ("armB2", -140f), ("glow", 1.5f), ("orb", 1.8f), ("sway", 2.5f));
            var keys = new Keyframes()
                .Key(0, Stand.With(("crouch", 5f), ("lean", 24f), ("head", 20f), ("jaw", 0f), ("armF1", 30f), ("armB1", 10f), ("glow", 0.3f)))
                .Key(2, Stand.With(("crouch", 8f), ("lean", 30f), ("head", 24f), ("jaw", 0f), ("armF1", 20f), ("armF2", 40f), ("armB1", 20f), ("armB2", 40f), ("glow", 0.6f)), Ease.In)
                .Key(4, roar, Ease.Out)
                .Key(9, roar.With(("head", -22f), ("jaw", 0.9f)));
            var p = keys.Evaluate(f);
            p["beard"] = f * 1.1f;
            p["robe"] = f * 0.6f;
            if (f >= 4) p["x"] = f % 2 == 0 ? 1.2f : -1.2f;
            return Draw(p, new Fx { Phase = f / 10f, Sparks = f >= 4 ? 3 : 0, Dust = f == 4 ? 2 : 0, Aura = f >= 4 });
        }

        // Muerte: retrocede, cae de rodillas, el báculo se le escapa hacia atrás, se desploma hacia delante y la mitra rueda.
        ShadedCanvas DrawDeath(int f)
        {
            var hit = Stand.With(("x", -4f), ("lean", -12f), ("head", -18f), ("jaw", 0.9f), ("armF1", 20f), ("armF2", 50f), ("plant", 0f), ("staff", 110f), ("grip", 96f),
                                 ("armB1", -60f), ("armB2", -40f), ("glow", 1.2f));
            var stagger = hit.With(("x", -2f), ("lean", 20f), ("head", 20f), ("jaw", 0.6f), ("crouch", 6f), ("armF1", 10f), ("armF2", 20f), ("armB1", 20f), ("armB2", 10f),
                                   ("glow", 0.8f));
            var kneel = stagger.With(("crouch", 28f), ("lean", 18f), ("head", 26f), ("jaw", 0.4f), ("armF1", 4f), ("armF2", 0f), ("armB1", 4f), ("armB2", 0f),
                                     ("glow", 0.45f), ("footF", 16f), ("footB", -18f));
            var slump = kneel.With(("crouch", 33f), ("lean", 62f), ("head", 18f), ("fall", 10f), ("armF1", -14f), ("armF2", -4f), ("armB1", -20f), ("armB2", -10f), ("glow", 0.2f));
            var down = kneel.With(("crouch", 36f), ("lean", 78f), ("head", 6f), ("fall", 16f), ("armF1", -36f), ("armF2", -20f), ("armB1", -44f), ("armB2", -30f), ("jaw", 0.5f), ("glow", 0f));
            var keys = new Keyframes()
                .Key(0, hit)
                .Key(2, hit.With(("x", -5f), ("head", -22f)))
                .Key(4, stagger, Ease.InOut)
                .Key(7, kneel, Ease.In)
                .Key(8, kneel.With(("crouch", 29f), ("head", 30f)))
                .Key(9, kneel.With(("lean", 24f)))
                .Key(11, slump, Ease.In)
                .Key(12, down.With(("lean", 82f)), Ease.In)
                .Key(13, down)
                .Key(15, down.With(("jaw", 0.3f)));
            var p = keys.Evaluate(f);
            p["beard"] = f < 13 ? f * 0.9f : 13f * 0.9f + (f - 13) * 0.25f;
            p["robe"] = f * 0.4f;
            p["sway"] = f < 12 ? 1.5f : 0.4f;
            p["orb"] = Mathf.Lerp(1.4f, 0f, f / 9f);
            var fx = new Fx { Phase = f / 16f, Dust = f == 7 ? 1 : f == 12 ? 2 : f == 13 ? 1 : 0, Dead = f >= 12,
                              StaffDrop = f >= 4 ? f - 3 : 0, MitreRoll = f >= 11 ? f - 10 : 0 };
            if (f >= 11) p["mitre"] = 0f;
            return Draw(p, fx);
        }

        // ------------------------------------------------------------------
        // Dibujo
        // ------------------------------------------------------------------

        struct Fx
        {
            public float Phase;
            public int Dust, Sparks, OrbFlash;
            /// <summary>0 = no; n = fotograma n-1 desde que suelta el báculo / pierde la mitra.</summary>
            public int StaffDrop, MitreRoll;
            public bool HandGlow, Speed, Aura, Dead;
        }

        /// <summary>Transformación del cuerpo: inclinación del tronco sobre la cadera y derrumbe sobre las rodillas.</summary>
        sealed class Body
        {
            public Vector2 Hip;
            public float Lean, HeadTilt, Fall;
            public Vector2 FallPivot;

            public Vector2 W(Vector2 p) => Fall == 0f ? p : Rotate(p, FallPivot, -Fall);
            public Vector2 U(float x, float y) => W(Rotate(V(Hip.x + x, Hip.y + y), Hip, -Lean));
            Vector2 UpperRaw(float x, float y) => Rotate(V(Hip.x + x, Hip.y + y), Hip, -Lean);
            public Vector2 H(float x, float y)
            {
                Vector2 neck = UpperRaw(10f, 48f);
                Vector2 p = Rotate(Add(neck, V(x * HS, y * HS)), neck, -(Lean * 0.4f + HeadTilt));
                return W(p);
            }
            public float HeadAngle => -(Lean * 0.4f + HeadTilt) - Fall;
            public float UpperAngle => -Lean - Fall;
        }

        ShadedCanvas Draw(Pose p, Fx fx)
        {
            var c = NewFrame();
            var b = new Body
            {
                Hip = V(p["x"], HipY - p["crouch"] + p["y"]),
                Lean = p["lean"],
                HeadTilt = p["head"],
                Fall = p["fall"],
            };
            b.FallPivot = V(p["x"] + 22f, 2f);

            if (fx.Aura) DrawAura(c, b, fx);
            DrawArm(c, b, p, front: false, fx);
            DrawRobe(c, b, p);
            DrawFeet(c, b, p);
            DrawChasuble(c, b, p);
            DrawHead(c, b, p, fx);
            if (fx.StaffDrop == 0) DrawStaff(c, b, p, fx, StaffEnds(b, p));
            else DrawDroppedStaff(c, p, fx);
            DrawArm(c, b, p, front: true, fx);
            if (fx.Dust > 0) DrawDust(c, b, fx);
            if (fx.Sparks > 0) DrawSparks(c, b, p, fx);
            if (fx.Speed) DrawSpeedLines(c, b, fx);
            return c;
        }

        // ---------------- Túnica (alba) y pies ----------------

        static void DrawRobe(ShadedCanvas c, Body b, Pose p)
        {
            int g = c.NewGroup();
            float sway = p["sway"], ph = p["robe"], wind = p["wind"];
            Vector2 waistB = b.U(-25f, 6f), waistF = b.U(19f, 4f);
            float hemSway = Mathf.Sin(ph) * 2.2f * sway - wind * 10f;
            float ground = 0f;
            float hipDrop = HipY - b.Hip.y; // al arrodillarse la tela se amontona
            var hemB = b.W(V(b.Hip.x - 38f - hipDrop * 0.6f + hemSway * 1.3f, ground));
            var hemF = b.W(V(b.Hip.x + 31f + hipDrop * 0.5f + hemSway * 0.6f, ground));
            var midB = b.W(V(b.Hip.x - 33f + hemSway * 0.8f, b.Hip.y * 0.45f));
            var midF = b.W(V(b.Hip.x + 26f + hemSway * 0.3f, b.Hip.y * 0.45f));
            // El alba acaba un poco por encima del bajo: debajo asoma el encaje festoneado.
            Vector2 hemDir = Sub(hemF, hemB).normalized;
            Vector2 hemUp = V(-hemDir.y, hemDir.x);
            var pts = new List<Vector2> { waistB, midB, Add(hemB, Scale(hemUp, 2.5f)) };
            const int n = 9;
            for (int i = 1; i < n; i++)
            {
                float t = i / (float)n;
                pts.Add(Add(Mix(hemB, hemF, t), Scale(hemUp, 2.5f + Mathf.Sin(ph + t * 6f) * 0.5f)));
            }
            pts.Add(Add(hemF, Scale(hemUp, 2.5f)));
            pts.Add(midF);
            pts.Add(waistF);
            var alb = new Drape(pts.ToArray(), Mix(waistB, waistF, 0.5f), Mix(hemB, hemF, 0.45f), 36f, 6f, ph * 0.8f, 0.7f);
            float hemLen = Sub(hemF, hemB).magnitude;
            // Mugre de salitre que sube desde el bajo (manchas que chorrean).
            alb.AddTo(c, Alb, 2f, g).Frame(alb.Top, alb.AxisAngle)
               .WithTint(Grime, 0.42f, (s, r) =>
               {
                   float wet = s - alb.Len * 0.62f - PixelCanvas.ValueNoise(r / 3f, 0f, 0, 41) * 14f;
                   return wet > 0f && PixelCanvas.ValueNoise(r / 2.2f, s / 5f, 0, 43) > 0.38f ? 1f : 0f;
               });

            // Pliegues pesados: surcos con cresta iluminada que nacen en la cintura y se abren hacia el bajo.
            foreach (float r in alb.Valleys())
            {
                float s0 = alb.Len * (0.16f + 0.12f * PixelCanvas.Hash((int)(r * 10f), 3, 7));
                var line = new List<Vector2>();
                for (int k = 0; k <= 4; k++)
                {
                    float s = Mathf.Lerp(s0, alb.Len * 1.08f, k / 4f);
                    line.Add(alb.At(s, r * Mathf.Lerp(0.82f, 1.05f, k / 4f) + Mathf.Sin(ph + k + r) * 0.6f));
                }
                c.Fold(line, g, 2, 1, Alb);
            }

            // Encaje del bajo: festones con calados (agujeros "hondos") en dos hileras y un galón dorado encima.
            const float laceH = 9f;
            float hemAng = Mathf.Atan2(hemDir.y, hemDir.x) * Mathf.Rad2Deg;
            float lMinX = Mathf.Min(hemB.x, hemF.x) - laceH - 2f, lMaxX = Mathf.Max(hemB.x, hemF.x) + laceH + 2f;
            float lMinY = Mathf.Min(hemB.y, hemF.y) - laceH - 2f, lMaxY = Mathf.Max(hemB.y, hemF.y) + laceH + 2f;
            (bool, N3) LaceSample(float px, float py, float v0, float v1, bool scallop)
            {
                float dx = px - hemB.x, dy = py - hemB.y;
                float u = dx * hemDir.x + dy * hemDir.y, v = dx * hemUp.x + dy * hemUp.y;
                if (u < -0.5f || u > hemLen + 0.5f || v > v1) return (false, N3.Front);
                float bottom = v0;
                if (scallop)
                {
                    float q = Mathf.Repeat(u + 1f, 5f) / 5f * 2f - 1f;
                    bottom = 1.9f - 1.9f * Mathf.Sqrt(Mathf.Max(0f, 1f - q * q));
                }
                if (v < bottom) return (false, N3.Front);
                alb.Local(px, py, out float s, out float r);
                return (true, alb.Normal(s, r));
            }
            c.Custom(lMinX, lMinY, lMaxX, lMaxY, (x, y) => LaceSample(x, y, 0f, laceH, true), Lace, 2.06f, 0f, g)
             .Frame(hemB, hemAng)
             .WithDetail((u, v) =>
             {
                 // Hilera baja: ojales sobre cada festón. Hilera alta: rombos calados con punto central.
                 float cu = Mathf.Repeat(u + 1f, 5f) - 2.5f;
                 if (v > 2.2f && v < 3.4f && Mathf.Abs(cu) < 0.6f) return -2;
                 float du = Mathf.Abs(Mathf.Repeat(u - 1.5f, 5f) - 2.5f), dv = Mathf.Abs(v - 5.6f);
                 float dd = du + dv * 1.2f;
                 if (dd > 1.5f && dd < 2.4f) return -2;
                 if (dd < 0.5f) return -1;
                 if (v > 7.6f) return -1;
                 return 0;
             })
             .WithTint(Grime, 0.35f, Patterns.Spots(2.5f, 0.62f, 47));
            c.Custom(lMinX, lMinY, lMaxX, lMaxY, (x, y) => LaceSample(x, y, laceH - 0.4f, laceH + 2f, false), GoldDark, 2.08f, 0f, g)
             .Frame(hemB, hemAng).WithDetail(Patterns.Stripes(3f, 1f, 1, 0.5f));
        }

        static void DrawFeet(ShadedCanvas c, Body b, Pose p)
        {
            for (int k = 0; k < 2; k++)
            {
                bool front = k == 0;
                float fx = b.Hip.x + (front ? p["footF"] : p["footB"]) + 8f;
                float lift = front ? p["liftF"] : p["liftB"];
                if (b.Fall > 30f) continue;
                int g = c.NewGroup();
                float z = front ? 2.4f : 1.2f, shade = front ? 0f : -0.14f;
                Vector2 heel = b.W(V(fx - 6f, 3f + lift));
                Vector2 toe = b.W(V(fx + 9f, 2f + lift * 0.6f));
                c.Capsule(heel, toe, 3.6f, 2.6f, Skin, z, shade, g);
                for (int i = 0; i < 3; i++)
                {
                    Vector2 claw0 = Add(toe, V(1f, -1f + i * 1.3f));
                    c.Capsule(claw0, Add(claw0, V(4f, -1.5f - i * 0.4f)), 1.1f, 0.4f, Claw, z + 0.05f, shade, g);
                }
            }
        }

        // ---------------- Casulla ----------------

        static void DrawChasuble(ShadedCanvas c, Body b, Pose p)
        {
            int g = c.NewGroup();
            float ph = p["robe"], sway = p["sway"], wind = p["wind"];
            float swing = Mathf.Sin(ph - 0.6f) * 1.8f * sway - wind * 12f;
            // La casulla cuelga de los hombros; abajo la gravedad la endereza (mezcla entre el tronco y la vertical).
            Vector2 sB = b.U(-24f, 44f), sF = b.U(16f, 46f), collar = b.U(-4f, 50f);
            Vector2 HangPoint(float lx, float ly, float mix)
            {
                Vector2 upper = b.U(lx, ly);
                Vector2 hang = b.W(V(b.Hip.x + lx, b.Hip.y + ly));
                return Mix(upper, hang, mix);
            }
            Vector2 backMid = HangPoint(-34f, 14f, 0.3f);
            Vector2 backHem = Add(HangPoint(-36f, -26f, 0.65f), V(swing * 1.2f, 0f));
            Vector2 tip = Add(HangPoint(-4f, -36f, 0.7f), V(swing, 0f));
            Vector2 frontHem = Add(HangPoint(30f, -22f, 0.65f), V(swing * 0.7f, 0f));
            Vector2 frontMid = HangPoint(30f, 14f, 0.3f);
            Vector2 hump = b.U(-31f, 34f), shoulderTop = b.U(-10f, 52f);
            var outline = new[] { collar, shoulderTop, sF, Add(frontMid, V(1f, 0f)), frontHem, Mix(frontHem, tip, 0.5f) + V(0f, -2f), tip,
                                  Mix(tip, backHem, 0.5f) + V(0f, -2f), backHem, backMid, hump, sB };
            // Tela pesada con volumen (se curva alrededor del cuerpo, pliegues hondos abajo).
            var drape = new Drape(outline, b.U(-6f, 50f), tip, 34f, 5f, ph * 0.6f, 1.05f);
            const float galloon = 4.6f;

            // Campo carmesí con damasco: granadas tejidas en un tono más hondo dentro de cada ojiva.
            drape.Region(c, (s, r, e) => e >= galloon, Chasuble, 3.5f, g).Frame(drape.Top, drape.AxisAngle)
                 .WithDetail((s, r) => Damask(s, r));
            // Brocado: celosía de ojivas en hilo de oro viejo con un botón de oro en el centro de cada celda.
            drape.Region(c, (s, r, e) => e >= galloon + 0.6f && (OgeeLine(s, r, BrocW, BrocH, 0.44f) || OgeeKnot(s, r)), Brocade, 3.52f, g);

            // Galón: canto de oro, trenza oscura tachonada de perlas y otro canto de oro hacia dentro.
            drape.Region(c, (s, r, e) => e < 1.3f, Gold, 3.55f, g);
            drape.Region(c, (s, r, e) => e >= 1.3f && e < 3.3f, GoldDark, 3.55f, g);
            drape.Region(c, (s, r, e) => e >= 3.3f && e < galloon, Gold, 3.55f, g);
            int k = 0;
            foreach (var pearl in drape.EdgePoints(2.3f, 3f))
            {
                if (k++ % 2 == 0) c.Dot(pearl, 3, g, GoldDark);
                else c.Dot(pearl, -1, g, GoldDark);
            }

            // Pliegues pesados: surco + cresta en cada valle de la tela, desde media altura hasta el bajo.
            foreach (float r in drape.Valleys())
            {
                float s0 = drape.Len * (0.28f + 0.16f * PixelCanvas.Hash((int)(r * 10f) + 50, 5, 7));
                var line = new List<Vector2>();
                for (int i = 0; i <= 4; i++)
                {
                    float s = Mathf.Lerp(s0, drape.Len * 1.05f, i / 4f);
                    line.Add(drape.At(s, r * Mathf.Lerp(0.9f, 1.04f, i / 4f) + Mathf.Sin(ph * 0.7f + i * 1.3f + r) * 0.7f));
                }
                c.Fold(line, g, 2, 1, Chasuble);
                c.Crease(line, 1, g, Brocade);
            }

            // Aurifrisia (galón vertical) bordada: cantos oscuros, cadena de rombos y pedrería.
            Vector2 o0 = b.U(6f, 46f), o1 = b.U(13f, 46f);
            Vector2 o2 = Add(Mix(tip, frontHem, 0.42f), V(0f, 1f)), o3 = Add(Mix(tip, frontHem, 0.18f), V(0f, 1f));
            Vector2 oTop = Mix(o0, o1, 0.5f), oBot = Mix(o3, o2, 0.5f);
            Vector2 oDir = Sub(oBot, oTop);
            float oLen = Mathf.Max(1f, oDir.magnitude);
            float oAng = Mathf.Atan2(oDir.y, oDir.x) * Mathf.Rad2Deg;
            float w0 = Sub(o1, o0).magnitude * 0.5f, w1 = Sub(o2, o3).magnitude * 0.5f;
            c.Poly(new[] { o0, o1, o2, o3 }, Gold, 3.6f, 1f, 0f, g).Frame(oTop, oAng).WithDetail((u, v) =>
            {
                float hw = Mathf.Lerp(w0, w1, Mathf.Clamp01(u / oLen));
                float av = Mathf.Abs(v);
                if (av > hw - 1f) return -2;
                if (av > hw - 2f && av <= hw - 1f) return 1;
                float du = Mathf.Repeat(u - 2f, 9f) - 4.5f;
                float dd = Mathf.Abs(du) / 3.8f + av / Mathf.Max(1.6f, hw - 2.2f);
                if (dd > 0.78f && dd < 1.08f) return -2;
                if (Mathf.Abs(du) > 4f && av < 0.7f) return -1;
                return 0;
            });
            // Gemas engastadas en los rombos (alternas: esmeralda de mar y rubí), con su brillo.
            for (int i = 0; ; i++)
            {
                float u = 6.5f + i * 9f;
                if (u > oLen - 3f) break;
                Vector2 at = Add(oTop, Scale(oDir, u / oLen));
                int gg = c.NewGroup();
                var gem = i % 2 == 0 ? GemGreen : GemRed;
                c.Ellipse(at, 1.7f, 1.9f, oAng, gem, 3.65f, 0f, gg);
                c.Glint(Add(at, V(-0.5f, 0.6f)), gg, 0, gem);
            }
        }

        // ---------------- Brocado (coordenadas de la tela: s a lo largo, r a través, en píxeles) ----------------

        const float BrocW = 13f, BrocH = 30f;

        /// <summary>Celosía de ojivas: dos familias de ondas que se tocan formando celdas en forma de llama.</summary>
        static bool OgeeLine(float s, float r, float w, float h, float width)
        {
            float f = s * 2f * Mathf.PI / h;
            float a = w * 0.5f * Mathf.Sin(f);
            float slope = w * 0.5f * (2f * Mathf.PI / h) * Mathf.Cos(f);
            float norm = Mathf.Sqrt(1f + slope * slope);
            float d1 = Mathf.Abs(Mathf.Repeat(r - a + w * 0.5f, w) - w * 0.5f);
            float d2 = Mathf.Abs(Mathf.Repeat(r + a + w * 0.5f, w) - w * 0.5f);
            return Mathf.Min(d1, d2) / norm < width;
        }

        /// <summary>Coordenadas dentro de la celda de la celosía más cercana (centros al tresbolillo).</summary>
        static void OgeeCell(float s, float r, out float dr, out float ds)
        {
            float hh = BrocH * 0.5f;
            float ar = r - Mathf.Round(r / BrocW) * BrocW;
            float aS = s - (Mathf.Floor(s / hh) * hh + hh * 0.5f);
            float br = r - (Mathf.Round((r - BrocW * 0.5f) / BrocW) * BrocW + BrocW * 0.5f);
            float bS = s - Mathf.Round(s / hh) * hh;
            float da = ar * ar / (BrocW * BrocW) + aS * aS / (hh * hh);
            float db = br * br / (BrocW * BrocW) + bS * bS / (hh * hh);
            if (da <= db) { dr = ar; ds = aS; }
            else { dr = br; ds = bS; }
        }

        static bool OgeeKnot(float s, float r)
        {
            OgeeCell(s, r, out float dr, out float ds);
            return Mathf.Abs(dr) < 0.6f && Mathf.Abs(ds + 0.5f) < 0.6f;
        }

        /// <summary>Damasco: una granada (cuerpo y corona de tres puntas) tejida en un tono más hondo en cada celda.</summary>
        static int Damask(float s, float r)
        {
            OgeeCell(s, r, out float dr, out float ds);
            // ds crece hacia abajo (a lo largo de la tela): la corona queda arriba (ds negativo).
            float body = dr * dr / (2.6f * 2.6f) + (ds - 1f) * (ds - 1f) / (3.2f * 3.2f);
            if (body < 1f) return -1;
            if (ds < -1.6f && ds > -4.2f && (Mathf.Abs(dr) < 0.55f || Mathf.Abs(Mathf.Abs(dr) - 1.8f) < 0.55f && ds > -3.4f)) return -1;
            return 0;
        }

        /// <summary>
        /// Tela que cuelga: dentro de un contorno, con la normal de un cilindro (se curva hacia los lados), un poco
        /// hacia arriba en los hombros y pliegues ondulados que crecen hacia el bajo. Coordenadas propias de la tela
        /// (s a lo largo desde arriba, r a través, en píxeles) para bordados, damascos y pliegues.
        /// </summary>
        sealed class Drape
        {
            readonly Vector2[] poly;
            public readonly Vector2 Top, Axis, Perp;
            public readonly float Len, HalfWidth, Folds, Phase, Amp;
            readonly float minX, minY, maxX, maxY;

            public Drape(Vector2[] outline, Vector2 top, Vector2 bottom, float halfWidth, float folds, float phase, float amp)
            {
                poly = outline;
                Top = top;
                Vector2 d = Sub(bottom, top);
                Len = Mathf.Max(1f, d.magnitude);
                Axis = Scale(d, 1f / Len);
                Perp = new Vector2(-Axis.y, Axis.x);
                HalfWidth = halfWidth;
                Folds = folds;
                Phase = phase;
                Amp = amp;
                minX = minY = float.MaxValue;
                maxX = maxY = float.MinValue;
                foreach (var q in outline)
                {
                    minX = Mathf.Min(minX, q.x); maxX = Mathf.Max(maxX, q.x);
                    minY = Mathf.Min(minY, q.y); maxY = Mathf.Max(maxY, q.y);
                }
            }

            /// <summary>Ángulo del eje (para el marco local: u = s a lo largo, v = r a través).</summary>
            public float AxisAngle => Mathf.Atan2(Axis.y, Axis.x) * Mathf.Rad2Deg;

            public Vector2 At(float s, float r) => Add(Top, Add(Scale(Axis, s), Scale(Perp, r)));

            public void Local(float px, float py, out float s, out float r)
            {
                float qx = px - Top.x, qy = py - Top.y;
                s = qx * Axis.x + qy * Axis.y;
                r = qx * Perp.x + qy * Perp.y;
            }

            public N3 Normal(float s, float r)
            {
                float t = Mathf.Clamp01(s / Len);
                float u = Mathf.Clamp(r / HalfWidth, -1f, 1f);
                float lx = u * 0.8f + Amp * (0.2f + 0.8f * t) * Mathf.Sin(u * Folds * Mathf.PI + Phase);
                float ly = 0.45f * Mathf.Clamp01(1f - t * 3f);
                float lz = Mathf.Sqrt(Mathf.Max(0.06f, 1f - lx * lx - ly * ly));
                return new N3(Perp.x * lx - Axis.x * ly, Perp.y * lx - Axis.y * ly, lz).Normalized();
            }

            /// <summary>Posiciones r (px) de los valles de los pliegues (donde la normal pasa de mirar a un lado al otro).</summary>
            public List<float> Valleys()
            {
                var list = new List<float>();
                for (int k = -6; k <= 6; k++)
                {
                    float u = (Mathf.PI * (2 * k + 1) - Phase) / (Folds * Mathf.PI);
                    if (u > -0.92f && u < 0.92f) list.Add(u * HalfWidth);
                }
                return list;
            }

            /// <summary>Tela entera (o desde una distancia al borde).</summary>
            public ShadedCanvas.Shape AddTo(ShadedCanvas c, PixelMaterial cloth, float z, int group) =>
                Region(c, (s, r, e) => true, cloth, z, group);

            /// <summary>Parte de la tela que cumple pred(s, r, distancia al borde): bordados, galones, damascos...</summary>
            public ShadedCanvas.Shape Region(ShadedCanvas c, System.Func<float, float, float, bool> pred, PixelMaterial m, float z, int group)
            {
                return c.Custom(minX, minY, maxX, maxY, (px, py) =>
                {
                    if (!InPoly(px, py)) return (false, N3.Front);
                    Local(px, py, out float s, out float r);
                    if (!pred(s, r, EdgeDistance(px, py))) return (false, N3.Front);
                    return (true, Normal(s, r));
                }, m, z, 0f, group);
            }

            /// <summary>Puntos a lo largo del contorno, metidos <paramref name="inset"/> px hacia dentro, cada <paramref name="spacing"/> px.</summary>
            public List<Vector2> EdgePoints(float inset, float spacing)
            {
                var list = new List<Vector2>();
                for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                {
                    Vector2 a = poly[j], e = Sub(poly[i], a);
                    float l = e.magnitude;
                    if (l < spacing) continue;
                    Vector2 nrm = V(-e.y / l, e.x / l);
                    int count = Mathf.FloorToInt(l / spacing);
                    for (int k = 1; k < count; k++)
                    {
                        Vector2 q = Add(a, Scale(e, k / (float)count));
                        Vector2 inPt = Add(q, Scale(nrm, inset));
                        if (!InPoly(inPt.x, inPt.y)) inPt = Add(q, Scale(nrm, -inset));
                        if (!InPoly(inPt.x, inPt.y) || EdgeDistance(inPt.x, inPt.y) < inset - 0.4f) continue;
                        list.Add(inPt);
                    }
                }
                return list;
            }

            bool InPoly(float x, float y)
            {
                bool inside = false;
                for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                {
                    if ((poly[i].y > y) != (poly[j].y > y) &&
                        x < (poly[j].x - poly[i].x) * (y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                        inside = !inside;
                }
                return inside;
            }

            float EdgeDistance(float x, float y)
            {
                float best = float.MaxValue;
                for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                {
                    Vector2 a = poly[j], bb = poly[i];
                    float dx = bb.x - a.x, dy = bb.y - a.y;
                    float t = Mathf.Clamp01(((x - a.x) * dx + (y - a.y) * dy) / Mathf.Max(0.0001f, dx * dx + dy * dy));
                    float ex = a.x + dx * t - x, ey = a.y + dy * t - y;
                    best = Mathf.Min(best, ex * ex + ey * ey);
                }
                return Mathf.Sqrt(best);
            }
        }

        // ---------------- Cabeza, mitra y barba ----------------

        static void DrawHead(ShadedCanvas c, Body b, Pose p, Fx fx)
        {
            int g = c.NewGroup();
            float ha = b.HeadAngle;
            float jaw = Mathf.Clamp01(p["jaw"]);

            // Cuello grueso con papada: piel abultada y pliegues.
            c.Ellipse(b.H(2f, -2f), 13f * HS, 10f * HS, ha, Skin, 4.4f, -0.05f, g).WithBump(Patterns.Lumps(3f, 1.4f, 61), 0.7f);
            for (int i = 0; i < 3; i++)
                c.Fold(new[] { b.H(-9f + i * 2.5f, -6f - i * 2.2f), b.H(-3f + i * 2f, -9f - i * 2f), b.H(4f + i * 1.5f, -9.5f - i * 2f) }, g, 2, 1, Skin);

            // Barba de tentáculos (detrás de la mandíbula los traseros; delante los delanteros).
            DrawBeard(c, b, p, front: false);

            // Cráneo de mero: frente ancha que cae hacia una boca enorme, toda de escamas menudas.
            c.Ellipse(b.H(10f, 6f), 18f * HS, 13f * HS, ha - 8f, Skin, 5f, 0f, g).WithBump(Patterns.Scales(4.2f, 3f, 0.9f), 0.5f);
            c.Ellipse(b.H(22f, 3f), 11f * HS, 8.5f * HS, ha - 18f, Skin, 5.05f, 0f, g).WithBump(Patterns.Scales(3.6f, 2.6f, 0.8f), 0.45f);
            // Mandíbula inferior (gira al abrir la boca).
            Vector2 hinge = b.H(2f, -2f);
            float open = jaw * 26f;
            Vector2 J(float x, float y) => Rotate(b.H(x, y), hinge, -open);
            var mouthBack = new[] { b.H(6f, 0f), b.H(34f, -1f), J(32f, -6f), J(8f, -8f) };
            c.Poly(mouthBack, Mouth, 5.08f, 0.5f, 0f, g);
            c.Ellipse(J(18f, -6f), 16f * HS, 5.5f * HS, ha - open - 4f, Belly, 5.12f, 0f, g).WithDetail(Patterns.Stripes(2.4f, 1f, -1, 0.5f));
            c.Ellipse(J(16f, -3.5f), 15f * HS, 3.5f * HS, ha - open - 3f, Skin, 5.14f, 0f, g);
            // Dientes: aguja arriba y abajo.
            for (int i = 0; i < 6; i++)
            {
                Vector2 up = b.H(12f + i * 3.8f, -0.5f - i * 0.15f);
                c.Capsule(up, Add(up, Dir(ha - 90f, 3f + (i % 2))), 0.9f, 0.3f, Teeth, 5.1f, 0f, g);
                Vector2 low = J(12f + i * 3.6f, -4.8f);
                c.Capsule(low, Add(low, Dir(ha - open + 90f, 2.5f + (i % 2))), 0.8f, 0.3f, Teeth, 5.1f, 0f, g);
            }
            // Labio superior carnoso con su arista húmeda y la comisura marcada.
            c.Capsule(b.H(8f, 0.8f), b.H(33f, -0.5f), 2.2f * HS, 1.6f * HS, Skin, 5.16f, 0f, g);
            c.Ridge(new[] { b.H(12f, 2.4f), b.H(22f, 2f), b.H(31f, 1.1f) }, 1, g, Skin);
            c.Crease(new[] { b.H(6f, -0.6f), b.H(9f, -1.6f) }, 2, g, Skin);
            c.Glint(b.H(27f, 1.6f), g, 0, Skin);

            // Branquias: tres hendiduras curvas con el labio de arriba iluminado.
            for (int i = 0; i < 3; i++)
            {
                var slit = new[] { b.H(-5f + i * 3.2f, 9f - i * 0.4f), b.H(-3.6f + i * 3.2f, 4f), b.H(-4.6f + i * 3.2f, -1f + i * 0.6f) };
                c.Fold(slit, g, 2, 1, Skin);
            }
            // Arrugas de la frente y percebes incrustados en el cogote.
            c.Crease(new[] { b.H(4f, 15f), b.H(10f, 16.6f), b.H(16f, 16.2f) }, 1, g, Skin);
            c.Crease(new[] { b.H(2f, 11.5f), b.H(8f, 13f) }, 1, g, Skin);
            for (int i = 0; i < 4; i++)
            {
                Vector2 at = b.H(-6f + i * 3.4f - (i == 3 ? 6f : 0f), 9f + (i % 2) * 3.5f - (i == 3 ? 7f : 0f));
                int gb = c.NewGroup();
                float rr = i == 1 ? 1.8f : 1.3f;
                c.Ellipse(at, rr, rr * 0.85f, ha, Barnacle, 5.4f, 0f, gb);
                c.Dot(Add(at, V(0.3f, 0.2f)), -2, gb, Barnacle);
            }

            // Ceja ósea (con bultos) y el único ojo dorado.
            c.Capsule(b.H(12f, 14f), b.H(25f, 12f), 2.6f * HS, 1.6f * HS, Brow, 5.3f, 0f, g).WithBump(Patterns.Lumps(1.6f, 1.2f, 67), 0.8f);
            c.Ridge(new[] { b.H(13f, 15.6f), b.H(24f, 13.6f) }, 1, g, Brow);
            Vector2 eye = b.H(19f, 9f);
            if (fx.Dead)
            {
                c.Ellipse(eye, 3.4f * HS, 3f * HS, ha, EyeDead, 5.25f, 0f, g);
            }
            else
            {
                c.Ellipse(eye, 3.8f * HS, 3.4f * HS, ha, EyeGlow, 5.25f, 0f, g);
                c.Ellipse(Add(eye, Dir(ha, 0.6f)), 1.6f, 1.5f, ha, EyeCore, 5.26f, 0f, g);
                c.Ellipse(Add(eye, Dir(ha, 1.2f)), 0.7f, 2.4f, ha, Pupil, 5.27f, 0f, g);
                Vector2 wet = Add(eye, Add(Dir(ha + 90f, 2.6f), Dir(ha, -1.8f)));
                c.Decal(wet.x, wet.y, Glint);
            }
            // Párpado: un pliegue carnoso por encima y otro por debajo del ojo.
            c.Fold(new[] { b.H(14.5f, 12.4f), b.H(19f, 13.2f), b.H(23.5f, 11.6f) }, g, 2, 1, Skin);
            c.Crease(new[] { b.H(15f, 5.4f), b.H(19.5f, 4.8f), b.H(23f, 6f) }, 1, g, Skin);

            DrawBeard(c, b, p, front: true);
            if (p["mitre"] > 0.5f) DrawMitre(c, b, p, fx);
            else if (fx.MitreRoll > 0) DrawRollingMitre(c, b, fx);
        }

        static readonly float[] BeardX = { 4f, 9f, 14f, 19f, 24f, 28f, 12f };
        static readonly float[] BeardLen = { 26f, 34f, 38f, 33f, 27f, 20f, 30f };

        static void DrawBeard(ShadedCanvas c, Body b, Pose p, bool front)
        {
            float ph = p["beard"], sway = p["sway"], wind = p["wind"];
            float jaw = Mathf.Clamp01(p["jaw"]);
            Vector2 hinge = b.H(2f, -2f);
            for (int i = 0; i < BeardX.Length; i++)
            {
                bool isFront = i % 2 == 1;
                if (isFront != front) continue;
                int g = c.NewGroup();
                Vector2 a = Rotate(b.H(BeardX[i], -8f + (i == 6 ? 2f : 0f)), hinge, -jaw * 26f);
                var pts = new List<Vector2> { a };
                float ang = -90f - 6f + i * 2f - b.Fall * 0.3f;
                const int n = 8;
                float seg = BeardLen[i] * HS / n;
                for (int k = 1; k <= n; k++)
                {
                    float t = k / (float)n;
                    ang += Mathf.Sin(ph * 1.3f - t * 4f + i * 1.7f) * 9f * sway * t + wind * -7f * t;
                    // Las puntas se rizan.
                    if (k >= n - 2) ang += (i % 2 == 0 ? 22f : -22f);
                    a = Add(a, Dir(ang, seg));
                    pts.Add(a);
                }
                float r0 = i == 2 || i == 3 ? 2.6f : 2.1f;
                float z = isFront ? 5.2f : 4.9f, shade = isFront ? 0f : -0.12f;
                // Tentáculo anillado (relieve en cada tramo) que se afina hacia la punta.
                for (int k = 0; k < n; k++)
                {
                    float ra = Mathf.Lerp(r0, 0.5f, k / (float)n), rb = Mathf.Lerp(r0, 0.5f, (k + 1) / (float)n);
                    c.Capsule(pts[k], pts[k + 1], ra, rb, Tentacle, z, shade, g).WithBump(Patterns.Ridges(2.4f, 0.5f), 0.8f);
                }
                // Ventosas claras con el hueco oscuro por la cara de delante, y un brillo húmedo.
                for (int k = 1; k < n - 1; k++)
                {
                    Vector2 d = Sub(pts[k + 1], pts[k]).normalized;
                    Vector2 side = V(d.y, -d.x);
                    if (side.x < 0f) side = Scale(side, -1f);
                    float rr = Mathf.Lerp(r0, 0.5f, k / (float)n);
                    if (rr < 0.9f) break;
                    Vector2 q = Add(Mix(pts[k], pts[k + 1], 0.5f), Scale(side, rr * 0.55f));
                    c.Dot(q, 2, g, Tentacle);
                    if (rr > 1.5f) c.Dot(Add(q, Scale(side, -1f)), -1, g, Tentacle);
                }
                if (isFront) c.Glint(Add(pts[2], Scale(Sub(pts[1], pts[3]).normalized, 0.2f) + V(-0.8f, 0.6f)), g, 0, Tentacle);
            }
        }

        static void DrawMitre(ShadedCanvas c, Body b, Pose p, Fx fx)
        {
            int g = c.NewGroup();
            float ha = b.HeadAngle;
            Vector2 hOrigin = b.H(0f, 0f);
            // Ínfulas (cintas carmesí con galón de oro y flecos) que cuelgan por detrás.
            float ph = p["robe"];
            for (int k = 0; k < 2; k++)
            {
                int gl = c.NewGroup();
                Vector2 a0 = b.H(-6f + k * 3f, 14f);
                var pts = new List<Vector2> { a0 };
                float ang = -100f - k * 6f - p["wind"] * 50f;
                for (int i = 1; i <= 5; i++)
                {
                    ang += Mathf.Sin(ph - i * 0.8f + k) * 6f;
                    a0 = Add(a0, Dir(ang, 4.6f));
                    pts.Add(a0);
                }
                float sh = k == 0 ? -0.1f : -0.2f;
                c.Strand(pts, 2.5f, 2.2f, Chasuble, 4.2f, sh, gl);
                c.Strand(pts, 0.9f, 0.8f, Gold, 4.21f, sh, gl);
                Vector2 end = pts[pts.Count - 1], dEnd = Sub(end, pts[pts.Count - 2]).normalized;
                Vector2 sEnd = V(-dEnd.y, dEnd.x);
                for (int f = -1; f <= 1; f++)
                {
                    Vector2 f0 = Add(end, Scale(sEnd, f * 1.4f));
                    c.Capsule(f0, Add(f0, Scale(dEnd, 3.2f + (f == 0 ? 0.8f : 0f))), 0.6f, 0.4f, GoldDark, 4.22f, sh, gl);
                }
            }

            // Cuerpo de la mitra (perfil apuntado, muy alto) en oro repujado: rombos abombados con un botón en cada uno.
            var shape = new[] { b.H(-9f, 13f), b.H(21f, 13f), b.H(20f, 28f), b.H(15f, 41f), b.H(7f, 51f), b.H(-1f, 42f), b.H(-8f, 28f) };
            c.Poly(shape, Gold, 5.6f, 2.5f, 0f, g, 0.1f, 0f).Frame(hOrigin, ha)
             .WithBump((u, v) => MitreDiamond(u / HS, v / HS, out _) * 0.55f, 0.9f)
             .WithDetail((u, v) =>
             {
                 MitreDiamond(u / HS, v / HS, out float centre);
                 if (centre < 0.55f) return -1;
                 return centre > 3.1f ? 1 : 0;
             });
            // Bandas de los cantos y la central (aurifrisia), en oro oscuro con perlas.
            var frontBand = new[] { b.H(21f, 13f), b.H(20f, 28f), b.H(15f, 41f), b.H(7f, 51f), b.H(7f, 46.5f), b.H(12.4f, 39.4f), b.H(16.8f, 28f), b.H(17.6f, 19f) };
            var backBand = new[] { b.H(7f, 51f), b.H(-1f, 42f), b.H(-8f, 28f), b.H(-9f, 13f), b.H(-5.6f, 19f), b.H(-4.6f, 28f), b.H(1.6f, 40.2f), b.H(7f, 46.5f) };
            var titulus = new[] { b.H(4.4f, 19f), b.H(9.8f, 19f), b.H(8.4f, 47f), b.H(5.6f, 47f) };
            foreach (var band in new[] { frontBand, backBand, titulus })
                c.Poly(band, GoldDark, 5.64f, 1f, 0f, g, 0.05f, 0f);
            void PearlRow(Vector2 a, Vector2 bb, float spacing)
            {
                float l = Sub(bb, a).magnitude;
                int count = Mathf.Max(1, Mathf.FloorToInt(l / spacing));
                for (int i = 0; i <= count; i++) c.Dot(Mix(a, bb, i / (float)count), i % 2 == 0 ? 3 : 1, g, GoldDark);
            }
            PearlRow(b.H(19f, 16.5f), b.H(18.3f, 28f), 2.6f);
            PearlRow(b.H(18.3f, 28f), b.H(13.8f, 40f), 2.6f);
            PearlRow(b.H(13.8f, 40f), b.H(7.4f, 48.5f), 2.6f);
            PearlRow(b.H(-7.3f, 16.5f), b.H(-6.3f, 28f), 2.6f);
            PearlRow(b.H(-6.3f, 28f), b.H(0.2f, 41f), 2.6f);
            PearlRow(b.H(0.2f, 41f), b.H(6.6f, 48.5f), 2.6f);
            c.Ridge(new[] { b.H(-8.4f, 14f), b.H(-7.4f, 28f), b.H(-0.6f, 42f), b.H(6.8f, 50.2f) }, 1, g, GoldDark);
            // Banda inferior (círculo) con cabujones alternos y perlas entre ellos.
            c.Poly(new[] { b.H(-9.5f, 12.5f), b.H(21.5f, 12.5f), b.H(21f, 19f), b.H(-9f, 19f) }, GoldDark, 5.66f, 1f, 0f, g);
            c.Ridge(new[] { b.H(-9f, 18.4f), b.H(20.8f, 18.4f) }, 1, g, GoldDark);
            c.Crease(new[] { b.H(-9.2f, 13.2f), b.H(21.2f, 13.2f) }, 1, g, GoldDark);
            for (int i = 0; i < 4; i++)
            {
                Vector2 gem = b.H(-5f + i * 7.5f, 15.8f);
                int gg = c.NewGroup();
                var gm = i % 2 == 0 ? GemGreen : GemRed;
                c.Ellipse(gem, 1.7f, 1.9f, ha, gm, 5.7f, 0f, gg);
                c.Glint(Add(gem, Add(Dir(ha, -0.5f), Dir(ha + 90f, 0.7f))), gg, 0, gm);
                if (i < 3)
                {
                    c.Dot(b.H(-1.25f + i * 7.5f, 16.6f), 3, g, GoldDark);
                    c.Dot(b.H(-1.25f + i * 7.5f, 14.6f), 3, g, GoldDark);
                }
            }

            // El Signo Antiguo: estrella de cinco puntas con el ojo-llama, engastada en un medallón.
            float glow = p["glow"];
            var signMat = glow > 1.05f ? SignHot : glow > 0.4f ? SignGlow : glow > 0.05f ? SignDim : SignOff;
            Vector2 sc = b.H(7f, 30f);
            float r = 6.6f * HS + (glow > 1f ? (glow - 1f) * 2f : 0f);
            int gm2 = c.NewGroup();
            c.Ellipse(sc, 6.6f * HS + 2.2f, 6.6f * HS + 2.2f, 0f, GoldDark, 5.72f, 0f, gm2).WithBump(Patterns.Rivets(3.2f, 0.9f, 0f, 0f), 0.5f);
            c.Ellipse(sc, 6.6f * HS + 0.3f, 6.6f * HS + 0.3f, 0f, MitreRecess, 5.73f, 0f, gm2);
            c.Poly(Star(sc, r, r * 0.45f, ha + 90f), signMat, 5.75f, 1f, 0f, g);
            c.Capsule(Add(sc, Dir(ha + 90f, -2.4f)), Add(sc, Dir(ha + 90f, 2.6f)), 0.9f, 0.5f, glow > 0.05f ? SignDim : GoldDark, 5.8f, 0f, g);
            if (glow > 1.05f && !fx.Dead)
            {
                // Halo: el Signo arde.
                c.Ellipse(sc, r + 5f, r + 5f, 0f, OrbHalo, 5.5f);
            }
            c.Glint(b.H(1f, 40f), g, 1, Gold);
            c.Glint(b.H(-6.6f, 24f), g, 0, GoldDark);
            c.Glint(b.H(14.6f, 18f), g, 0, GoldDark);
        }

        /// <summary>Repujado de la mitra: rejilla de rombos (diagonales cada 7 unidades de cabeza). Devuelve la altura del cojín
        /// y en <paramref name="centre"/> la distancia a la junta más cercana (0 en la junta, 3.5 en el centro del rombo).</summary>
        static float MitreDiamond(float x, float y, out float centre)
        {
            float a = Mathf.Repeat(x + y, 7f), bb = Mathf.Repeat(x - y, 7f);
            float da = Mathf.Min(a, 7f - a), db = Mathf.Min(bb, 7f - bb);
            centre = Mathf.Min(da, db);
            return Mathf.Clamp01(centre / 1.6f);
        }

        static void DrawRollingMitre(ShadedCanvas c, Body b, Fx fx)
        {
            int g = c.NewGroup();
            float k = fx.MitreRoll - 1;
            Vector2 start = b.H(6f, 30f);
            Vector2 center = V(Mathf.Min(118f, Mathf.Lerp(start.x, start.x + 18f, Mathf.Min(1f, k / 3f)) + k * 1.5f), Mathf.Max(11f, Mathf.Lerp(start.y, 11f, Mathf.Min(1f, k / 2f))));
            float ang = -100f - k * 35f;
            if (k >= 3) ang = -100f - 3f * 35f - (k - 3) * 6f;
            var shape = new[] { Add(center, Rotate(V(-15f, -11f), V(0f, 0f), ang + 90f)), Add(center, Rotate(V(15f, -11f), V(0f, 0f), ang + 90f)),
                                Add(center, Rotate(V(14f, 6f), V(0f, 0f), ang + 90f)), Add(center, Rotate(V(1f, 33f), V(0f, 0f), ang + 90f)),
                                Add(center, Rotate(V(-14f, 6f), V(0f, 0f), ang + 90f)) };
            c.Poly(shape, Gold, 4f, 2f, 0f, g);
            c.Poly(Star(Add(center, Rotate(V(0f, 6f), V(0f, 0f), ang + 90f)), 6f, 2.7f, ang + 180f), SignOff, 4.1f, 1f, 0f, g);
        }

        static Vector2[] Star(Vector2 c, float rOut, float rIn, float rotation)
        {
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = rotation + i * 36f;
                pts[i] = Add(c, Dir(a, i % 2 == 0 ? rOut : rIn));
            }
            return pts;
        }

        // ---------------- Brazos y báculo ----------------

        static (Vector2 shoulder, Vector2 elbow, Vector2 hand) ArmPoints(Body b, Pose p, bool front)
        {
            Vector2 shoulder = front ? b.U(12f, 40f) : b.U(-18f, 42f);
            float a1 = (front ? p["armF1"] : p["armB1"]) + b.Lean + b.Fall;
            float a2 = (front ? p["armF2"] : p["armB2"]) + b.Lean + b.Fall;
            Vector2 elbow = Limb(shoulder, a1, 24f);
            Vector2 hand = Limb(elbow, a2, 21f);
            return (shoulder, elbow, hand);
        }

        static void DrawArm(ShadedCanvas c, Body b, Pose p, bool front, Fx fx)
        {
            int g = c.NewGroup();
            var (shoulder, elbow, hand) = ArmPoints(b, p, front);
            float z = front ? 7f : 1.5f, shade = front ? 0f : -0.13f;
            // Manga ancha del alba (tejido de lino con relieve), pliegues que caen del hombro y se arrugan en el codo.
            c.Capsule(shoulder, elbow, 8.5f, 6.5f, Sleeve, z, shade, g).WithBump(Patterns.Weave(2f, 0.35f), 0.5f);
            Vector2 wrist = Mix(elbow, hand, 0.78f);
            c.Capsule(elbow, wrist, 6.5f, 9.5f, Sleeve, z + 0.02f, shade, g).WithBump(Patterns.Weave(2f, 0.35f), 0.5f);
            Vector2 ua = Sub(elbow, shoulder).normalized, ub = Sub(wrist, elbow).normalized;
            Vector2 na = V(-ua.y, ua.x), nb = V(-ub.y, ub.x);
            c.Fold(new[] { Add(shoulder, Scale(na, 4.5f)), Add(Mix(shoulder, elbow, 0.55f), Scale(na, 2.5f)), Add(elbow, Scale(na, -1.5f)) }, g, 2, 1, Sleeve);
            c.Fold(new[] { Add(shoulder, Scale(na, -3f)), Add(Mix(shoulder, elbow, 0.7f), Scale(na, -3.5f)) }, g, 2, 1, Sleeve);
            for (int i = 0; i < 3; i++)
            {
                // Arrugas del codo: arcos cortos cruzados.
                Vector2 m = Add(elbow, Scale(ua, -2.5f + i * 2.4f));
                c.Fold(new[] { Add(m, Scale(na, -4.8f)), Add(Add(m, Scale(ua, 1.2f)), Scale(na, -1.5f)), Add(m, Scale(na, 1.4f)) }, g, 2, 1, Sleeve);
            }
            c.Fold(new[] { Add(Mix(elbow, wrist, 0.25f), Scale(nb, 2.5f)), Add(Mix(elbow, wrist, 0.9f), Scale(nb, 5.5f)) }, g, 2, 1, Sleeve);
            c.Fold(new[] { Add(Mix(elbow, wrist, 0.35f), Scale(nb, -2.4f)), Add(Mix(elbow, wrist, 0.92f), Scale(nb, -6f)) }, g, 2, 1, Sleeve);
            // Puño bordado: galón de oro con perlas entre dos cantos.
            Vector2 cuff0 = Mix(elbow, wrist, 0.84f);
            float cuffLen = Sub(wrist, cuff0).magnitude;
            c.Capsule(cuff0, wrist, 9.2f, 9.7f, GoldDark, z + 0.03f, shade, g).WithDetail((u, v) =>
            {
                if (u < 0.8f || u > cuffLen - 0.8f) return 1;
                if (Mathf.Abs(u - cuffLen * 0.5f) < 0.7f) return Mathf.Repeat(v + 0.5f, 2.6f) < 1f ? 3 : -1;
                return 0;
            });
            // Mano palmeada con garras, nudillos huesudos y anillos de oro con piedra.
            Vector2 dir = Sub(hand, elbow).normalized;
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            c.Ellipse(hand, 5f, 4f, ang, Skin, z + 0.05f, shade, g).WithBump(Patterns.Scales(2.6f, 2f, 0.7f), 0.5f);
            for (int i = 0; i < 3; i++)
            {
                float fa = ang - 30f + i * 26f;
                Vector2 k0 = Add(hand, Dir(fa, 3.5f));
                Vector2 k1 = Add(k0, Dir(fa + 25f, 4.5f));
                c.Capsule(k0, k1, 1.6f, 1.1f, Skin, z + 0.06f, shade, g);
                c.Capsule(k1, Add(k1, Dir(fa + 55f, 3f)), 1f, 0.35f, Claw, z + 0.07f, shade, g);
                c.Dot(k0, 1, g, Skin);
                c.Dot(k1, 1, g, Skin);
                if (i != 1)
                {
                    int gr = c.NewGroup();
                    Vector2 ring = Add(k0, Dir(fa + 12f, 1.6f));
                    Vector2 across = Dir(fa + 102f, 1.3f);
                    c.Capsule(Sub(ring, across), Add(ring, across), 0.85f, 0.85f, Gold, z + 0.065f, shade, gr);
                    c.Dot(ring, i == 0 ? -1 : 2, gr, Gold);
                    c.Glint(Add(ring, V(-0.6f, 0.6f)), gr, 0, Gold);
                }
            }
            if (!front && fx.HandGlow)
            {
                c.Ellipse(hand, 9f, 9f, 0f, OrbHalo, z - 0.1f);
                c.Ellipse(Add(hand, Dir(ang, 4f)), 3f, 3f, 0f, OrbMid, z + 0.08f);
            }
        }

        static (Vector2 bottom, Vector2 top) StaffEnds(Body b, Pose p)
        {
            var (_, _, hand) = ArmPoints(b, p, true);
            if (p["plant"] > 0.5f)
            {
                Vector2 bottom = V(hand.x + 6f, 0f);
                Vector2 d = Sub(hand, bottom).normalized;
                return (bottom, Add(bottom, Scale(d, StaffLength)));
            }
            Vector2 dir = Dir(p["staff"] - b.Fall);
            return (Sub(hand, Scale(dir, p["grip"])), Add(hand, Scale(dir, StaffLength - p["grip"])));
        }

        static void DrawStaff(ShadedCanvas c, Body b, Pose p, Fx fx, (Vector2 bottom, Vector2 top) ends)
        {
            int g = c.NewGroup();
            Vector2 bottom = ends.bottom, top = ends.top;
            Vector2 d = Sub(top, bottom).normalized;
            Vector2 side = V(-d.y, d.x);
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            // Asta de hierro torsionado (estría en espiral) con manchas de óxido.
            c.Capsule(bottom, top, 1.9f, 2.1f, Iron, 6.8f, 0f, g)
             .WithDetail((u, v) => Mathf.Repeat(u + v * 2.2f, 5f) < 1.2f ? -1 : PixelCanvas.ValueNoise(u / 4f, v, 0, 83) > 0.78f ? -1 : 0);
            // Regatón de oro en la punta inferior.
            c.Capsule(bottom, Add(bottom, Scale(d, 5f)), 2.3f, 2.6f, GoldDark, 6.82f, 0f, g).WithBump(Patterns.Ridges(2f, 0.6f), 1f);
            // Anillos de oro labrados.
            for (int i = 1; i <= 3; i++)
            {
                Vector2 ring = Add(bottom, Scale(d, StaffLength * (0.25f + i * 0.17f)));
                int gr = c.NewGroup();
                c.Capsule(Sub(ring, Scale(d, 1.5f)), Add(ring, Scale(d, 1.5f)), 2.8f, 2.8f, Gold, 6.85f, 0f, gr).WithBump(Patterns.Ridges(1.5f, 0.5f), 1f);
                c.Glint(Add(ring, Scale(side, 1.2f)), gr, 0, Gold);
            }
            // Nudo (manzana) bajo el remate, con una gema.
            Vector2 knop = Sub(top, Scale(d, 15f));
            int gk = c.NewGroup();
            c.Ellipse(knop, 4.2f, 3.4f, ang, Gold, 6.88f, 0f, gk).WithBump(Patterns.Rivets(2.4f, 0.8f, 0f, 0f), 0.6f);
            c.Crease(new[] { Add(knop, Scale(side, -3.6f)), Add(knop, Scale(side, 3.6f)) }, 1, gk, Gold);
            c.Ellipse(Add(knop, Scale(side, 0.8f)), 1.3f, 1.4f, ang, GemRed, 6.9f, 0f, c.NewGroup());
            // Uñas de ancla bajo el remate.
            Vector2 crown = Sub(top, Scale(d, 6f));
            for (int s = -1; s <= 1; s += 2)
            {
                var fluke = new List<Vector2> { crown };
                float fa = ang + 180f + s * 70f;
                Vector2 q = crown;
                for (int i = 0; i < 4; i++)
                {
                    fa += -s * 28f;
                    q = Add(q, Dir(fa, 3.4f));
                    fluke.Add(q);
                }
                c.Strand(fluke, 1.8f, 0.6f, Gold, 6.9f, 0f, g);
                c.Ridge(fluke, 1, g, Gold);
            }
            // Voluta (cayado) que se enrosca hacia delante: oro labrado con una acanaladura y cuentas en el canto.
            var spiral = new List<Vector2>();
            var groove = new List<Vector2>();
            var beads = new List<Vector2>();
            Vector2 start = top;
            float radius = 9f;
            Vector2 center = Add(start, Dir(ang - 90f, radius));
            for (int i = 0; i <= 14; i++)
            {
                float t = i / 14f;
                float a = ang + 90f - t * 330f;
                float r = Mathf.Lerp(radius, 3.5f, t);
                spiral.Add(Add(center, Dir(a, r)));
                if (t < 0.85f) groove.Add(Add(center, Dir(a, r + 0.6f)));
                if (i % 2 == 0 && t < 0.8f) beads.Add(Add(center, Dir(a, r + Mathf.Lerp(2.4f, 1.6f, t))));
            }
            c.Strand(spiral, 2.4f, 1.3f, Gold, 6.9f, 0f, g);
            c.Crease(groove, 1, g, Gold);
            foreach (var bead in beads) c.Dot(bead, 2, g, Gold);
            // Crochets (hojas de oro) en el lomo de la voluta.
            for (int i = 0; i < 3; i++)
            {
                float a = ang + 90f - (0.12f + i * 0.2f) * 330f;
                float r = Mathf.Lerp(radius, 3.5f, 0.12f + i * 0.2f) + 2.6f;
                Vector2 leaf = Add(center, Dir(a, r));
                int gl = c.NewGroup();
                c.Capsule(leaf, Add(leaf, Dir(a + 40f, 2.2f)), 1.2f, 0.4f, Gold, 6.89f, 0f, gl);
            }
            c.Glint(Add(center, Dir(ang + 135f, radius + 0.4f)), g, 1, Gold);
            // Gema verde en el centro de la espiral, sujeta por cuatro garras.
            float orb = p["orb"];
            float prong = 2.6f * Mathf.Max(0.6f, orb) + 1.4f;
            for (int i = 0; i < 4; i++)
            {
                Vector2 q = Add(center, Dir(ang + 45f + i * 90f, prong));
                c.Capsule(q, Add(center, Dir(ang + 45f + i * 90f, prong - 1.6f)), 0.9f, 0.45f, GoldDark, 6.98f, 0f, g);
            }
            if (orb > 0.05f)
            {
                if (orb > 1.3f) c.Ellipse(center, 4f * orb + 3f, 4f * orb + 3f, 0f, OrbHalo, 6.7f);
                c.Ellipse(center, 2.6f * orb + 1f, 2.6f * orb + 1f, 0f, OrbOut, 6.95f);
                c.Ellipse(center, 1.9f * orb, 1.9f * orb, 0f, OrbMid, 6.96f);
                if (orb > 0.6f) c.Ellipse(center, 1f * orb, 1f * orb, 0f, OrbCore, 6.97f);
            }
            if (fx.OrbFlash > 0)
            {
                float s = fx.OrbFlash == 2 ? 1f : 0.55f;
                c.Ellipse(center, 16f * s, 16f * s, 0f, OrbHalo, 6.6f);
                c.Ellipse(center, 9f * s, 9f * s, 0f, OrbMid, 7.5f);
                c.Ellipse(center, 5f * s, 5f * s, 0f, OrbCore, 7.6f);
                // Rayos en cruz.
                for (int i = 0; i < 4; i++)
                {
                    Vector2 r0 = Add(center, Dir(i * 90f + 45f, 6f * s));
                    c.Capsule(r0, Add(center, Dir(i * 90f + 45f, 22f * s)), 1.4f * s, 0.3f, Smear, 7.4f);
                }
            }
        }

        static void DrawDroppedStaff(ShadedCanvas c, Pose p, Fx fx)
        {
            // Se le escapa de la mano y cae hacia atrás girando sobre su punta; rebota una vez en el suelo.
            int k = fx.StaffDrop - 1;
            float t = Mathf.Min(1f, k / 5f);
            float ang = Mathf.Lerp(98f, 177f, t * t);
            if (k == 6) ang = 172f;
            Vector2 bottom = V(30f + 4f * t, 2f);
            Vector2 dir = Dir(ang);
            var fake = new Pose().With(("orb", Mathf.Max(0f, 1f - k / 4f)));
            DrawStaff(c, null, fake, new Fx(), (bottom, Add(bottom, Scale(dir, StaffLength))));
        }

        // ---------------- Efectos ----------------

        static void DrawDust(ShadedCanvas c, Body b, Fx fx)
        {
            int n = fx.Dust == 2 ? 7 : 4;
            for (int i = 0; i < n; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                float x = b.Hip.x + side * (30f + i * 7f) + 14f;
                float r = (fx.Dust == 2 ? 6f : 4f) - i * 0.4f;
                c.Ellipse(V(x, r * 0.6f), r * 1.4f, r, 0f, Dust, 8f);
            }
        }

        static void DrawSparks(ShadedCanvas c, Body b, Pose p, Fx fx)
        {
            Vector2 sc = b.H(7f, 33f);
            for (int i = 0; i < fx.Sparks * 3; i++)
            {
                float a = i * 47f + fx.Phase * 220f;
                float r = 12f + PixelCanvas.Hash(i, 2, 5) * 22f + fx.Phase * 10f;
                Vector2 q = Add(sc, Dir(a, r));
                q.y += fx.Phase * 14f;
                c.Ellipse(q, 0.8f, 0.8f, 0f, Spark, 9f);
            }
        }

        static void DrawAura(ShadedCanvas c, Body b, Fx fx)
        {
            Vector2 sc = b.H(7f, 33f);
            float r = 26f + 6f * Mathf.Sin(fx.Phase * Mathf.PI * 6f);
            c.Ellipse(sc, r, r, 0f, OrbHalo, 0f);
        }

        static void DrawSpeedLines(ShadedCanvas c, Body b, Fx fx)
        {
            for (int i = 0; i < 6; i++)
            {
                float y = 20f + i * 22f + (i % 2) * 6f;
                float x1 = b.Hip.x - 44f - (i * 7 + (int)(fx.Phase * 16f)) % 12;
                float len = 18f + (i % 3) * 10f;
                float yy = y;
                c.Custom(x1 - len, yy - 1f, x1, yy + 1f, (px, py) => (Mathf.Abs(py - yy) < 0.6f, N3.Front), Smear, 0.5f);
            }
        }
    }
}
