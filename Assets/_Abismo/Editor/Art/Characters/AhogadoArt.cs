using System.Collections.Generic;
using UnityEngine;
using static Abismo.EditorTools.Rig;

namespace Abismo.EditorTools
{
    /// <summary>
    /// El Ahogado: pescador de Innsmouth con escafandra de latón, abrigo encerado, capa con forro carmesí
    /// y el Garfio (una hoja con forma de ancla). Mira a la derecha; el juego lo voltea al girar.
    ///
    /// Cada animación se define con poses clave (ángulos en grados) que se interpolan:
    ///  - piernas/brazos: 0 = colgando, 90 = hacia delante, 180 = arriba, -90 = hacia atrás
    ///  - "sword": ángulo de la hoja (0 = al frente, 90 = arriba)
    /// </summary>
    public sealed class AhogadoArt : CharacterArt
    {
        public override string Id => "ahogado";
        public override int FrameWidth => 176;
        public override int FrameHeight => 120;
        public override Vector2 Pivot => new Vector2(88f, 8f);

        // ------------------------------------------------------------------
        // Materiales
        // ------------------------------------------------------------------

        static readonly PixelMaterial Brass = new PixelMaterial(Ramp.Make("b08a3e", 5, 0.12f, 0.3f, 1.5f))
        {
            Gloss = 0.6f, Ambient = 0.26f, Rim = 0.5f, SpecularAt = 0.9f, Specular = PixelCanvas.Hex("fff4c8"),
        };
        static readonly PixelMaterial BrassDark = new PixelMaterial(Ramp.Make("7a5a26", 4, 0.1f)) { Gloss = 0.3f, Specular = PixelCanvas.Hex("ffe9a8") };
        /// <summary>Cristal apagado del ojo de buey trasero (no brilla: solo refleja).</summary>
        static readonly PixelMaterial DeadGlass = new PixelMaterial(Ramp.Make("1f4a44", 4, 0.1f, 0.35f, 1.6f)) { Gloss = 0.8f, Ambient = 0.2f, Specular = PixelCanvas.Hex("d8fff2") };
        static readonly PixelMaterial GlassGlint = PixelMaterial.Glow("f0fffa");
        static readonly Color32 Verdigris = PixelCanvas.Hex("4f9a7c");
        // Abrigo de lona encerada: el tejido es un relieve pegado a cada pieza (no al lienzo), así no "nada" al moverse.
        static readonly PixelMaterial Coat = new PixelMaterial(Ramp.Make("2f4043", 5, 0.1f, 0.35f, 1.5f))
        {
            Ambient = 0.24f, Rim = 0.55f, Bump = Patterns.Weave(3f, 0.6f), BumpStrength = 0.45f,
        };
        static readonly PixelMaterial Lining = new PixelMaterial(Ramp.Make("6a2232", 4, 0.1f, 0.35f, 1.4f)) { Ambient = 0.22f, Rim = 0.6f, Dither = 0f, BandDither = 0.2f };
        static readonly PixelMaterial CapeOuter = new PixelMaterial(Ramp.Make("2a2b33", 4, 0.1f, 0.4f, 1.5f)) { Ambient = 0.24f, Rim = 0.6f, Dither = 0f };
        static readonly PixelMaterial Trousers = new PixelMaterial(Ramp.Make("262c36", 4, 0.08f)) { Ambient = 0.22f };
        static readonly PixelMaterial Leather = new PixelMaterial(Ramp.Make("5c3b25", 4, 0.1f)) { Ambient = 0.25f, Gloss = 0.15f };
        static readonly PixelMaterial Rope = new PixelMaterial(Ramp.Make("8a7448", 4, 0.08f)) { Ambient = 0.3f };
        static readonly PixelMaterial Strap = new PixelMaterial(Ramp.Make("553a25", 4, 0.1f, 0.32f, 1.5f)) { Ambient = 0.25f, Gloss = 0.12f, Rim = 0.5f };
        static readonly PixelMaterial BootLeather = new PixelMaterial(Ramp.Make("4a3222", 4, 0.1f, 0.32f, 1.5f)) { Ambient = 0.24f, Gloss = 0.18f, Rim = 0.5f };
        // Hoja del Garfio: el óxido va pegado a la hoja (Detail en coordenadas locales), no al lienzo, para que no "nade" al moverla.
        static readonly PixelMaterial Iron = new PixelMaterial(Ramp.Make("6f7b82", 5, 0.08f, 0.32f, 1.38f))
        {
            Gloss = 0.45f, Ambient = 0.3f, Rim = 0.45f, Dither = 0f, Specular = PixelCanvas.Hex("f2fffb"),
            Detail = (u, v) => PixelCanvas.ValueNoise(u / 3f, v / 2f, 0, 77) > 0.74f ? -1 : 0,
        };
        static readonly PixelMaterial IronDark = new PixelMaterial(Ramp.Make("4a535c", 4, 0.08f)) { Gloss = 0.3f, Ambient = 0.28f };
        static readonly PixelMaterial Hose = new PixelMaterial(Ramp.Make("3d3326", 4, 0.06f)) { Ambient = 0.25f };
        static readonly PixelMaterial GlassRing = PixelMaterial.Glow("1d6b5a");
        static readonly PixelMaterial GlassMid = PixelMaterial.Glow("3fd6a4");
        static readonly PixelMaterial GlassCore = PixelMaterial.Glow("c6ffe9");
        static readonly PixelMaterial Smear = new PixelMaterial(new[] { new Color32(228, 248, 238, 240) }) { Emissive = true, Outline = false, ReceivesContactShadow = false };
        static readonly PixelMaterial SmearMid = new PixelMaterial(new[] { new Color32(150, 214, 196, 200) }) { Emissive = true, Outline = false, ReceivesContactShadow = false };
        static readonly PixelMaterial SmearSoft = new PixelMaterial(new[] { new Color32(70, 140, 128, 140) }) { Emissive = true, Outline = false, ReceivesContactShadow = false };
        static readonly PixelMaterial Laudanum = PixelMaterial.Glow("ff9b4a");
        static readonly PixelMaterial Sign = PixelMaterial.Glow("8affd8");


        // ------------------------------------------------------------------
        // Poses base
        // ------------------------------------------------------------------

        static Pose Idle => new Pose().With(
            ("legF1", 10f), ("legF2", 2f), ("legB1", -9f), ("legB2", -16f),
            ("lean", 4f), ("head", -2f),
            ("armF1", 18f), ("armF2", 46f), ("sword", -62f),
            ("armB1", -10f), ("armB2", 8f),
            ("cape", 0.15f), ("hose", 0f));

        public override List<AnimSpec> Animations() => new List<AnimSpec>
        {
            new AnimSpec("idle", 8, 8f, true, f => DrawIdle(f)),
            new AnimSpec("run", 8, 14f, true, f => DrawRun(f)),
            new AnimSpec("jump", 3, 12f, false, f => DrawJump(f)),
            new AnimSpec("fall", 2, 8f, true, f => DrawFall(f)),
            new AnimSpec("land", 3, 15f, false, f => DrawLand(f)),
            new AnimSpec("attack1", 6, 20f, false, f => DrawAttack1(f)),
            new AnimSpec("attack2", 6, 20f, false, f => DrawAttack2(f)),
            new AnimSpec("attack3", 8, 20f, false, f => DrawAttack3(f)),
            new AnimSpec("airattack", 6, 20f, false, f => DrawAirAttack(f)),
            new AnimSpec("dodge", 6, 18f, false, f => DrawDodge(f)),
            new AnimSpec("parry", 4, 14f, false, f => DrawParry(f)),
            new AnimSpec("parry_success", 4, 16f, false, f => DrawParrySuccess(f)),
            new AnimSpec("heal", 10, 13.33f, false, f => DrawHeal(f)),
            new AnimSpec("cast", 6, 15f, false, f => DrawCast(f)),
            new AnimSpec("hurt", 3, 10f, false, f => DrawHurt(f)),
            new AnimSpec("death", 12, 12f, false, f => DrawDeath(f)),
            new AnimSpec("pray", 8, 8f, false, f => DrawPray(f)),
            new AnimSpec("execute", 16, 18f, false, f => DrawExecute(f)),
        };

        // ------------------------------------------------------------------
        // Animaciones
        // ------------------------------------------------------------------

        ShadedCanvas DrawIdle(int f)
        {
            float t = f / 8f * Mathf.PI * 2f;
            var p = Idle.With(("crouch", 0.6f + Mathf.Sin(t) * 0.6f), ("cape", 0.15f + Mathf.Sin(t + 0.6f) * 0.12f),
                              ("hose", Mathf.Sin(t + 1.2f) * 0.5f), ("armF2", 46f + Mathf.Sin(t) * 2f), ("sword", -62f + Mathf.Sin(t) * 1.5f));
            return Draw(p);
        }

        static readonly float[,] RunLegs =
        {
            // muslo delantero, espinilla delantera, muslo trasero, espinilla trasera, agacharse
            { 34f, 12f, -32f, -64f, 1f },
            { 20f, -8f, -16f, -86f, 3f },
            { 0f, -22f, 18f, -44f, 1.5f },
            { -24f, -44f, 44f, 22f, -1f },
            { -32f, -64f, 34f, 12f, 1f },
            { -16f, -86f, 20f, -8f, 3f },
            { 18f, -44f, 0f, -22f, 1.5f },
            { 44f, 22f, -24f, -44f, -1f },
        };

        ShadedCanvas DrawRun(int f)
        {
            float swing = Mathf.Sin(f / 8f * Mathf.PI * 2f);
            var p = Idle.With(
                ("legF1", RunLegs[f, 0]), ("legF2", RunLegs[f, 1]), ("legB1", RunLegs[f, 2]), ("legB2", RunLegs[f, 3]),
                ("crouch", RunLegs[f, 4]), ("lean", 13f), ("head", -6f),
                ("armB1", 30f * swing), ("armB2", 30f * swing + 40f),
                ("armF1", -26f - 8f * swing), ("armF2", -10f - 6f * swing), ("sword", -168f + 4f * swing),
                ("cape", 0.9f + 0.15f * Mathf.Sin(f * 1.7f)), ("hose", -0.8f + 0.3f * swing));
            return Draw(p);
        }

        ShadedCanvas DrawJump(int f)
        {
            Pose p;
            if (f == 0) p = Idle.With(("legF1", 40f), ("legF2", -30f), ("legB1", 10f), ("legB2", -60f), ("crouch", 4f), ("lean", 18f),
                                      ("armF1", -30f), ("armF2", -20f), ("sword", -160f), ("armB1", -40f), ("armB2", -20f), ("cape", 0.3f));
            else if (f == 1) p = Idle.With(("air", 1f), ("y", 6f), ("legF1", 10f), ("legF2", 0f), ("legB1", -14f), ("legB2", -20f), ("lean", 6f),
                                           ("armF1", 40f), ("armF2", 80f), ("sword", -40f), ("armB1", 60f), ("armB2", 110f), ("cape", -0.6f), ("hose", -1f));
            else p = Idle.With(("air", 1f), ("y", 10f), ("legF1", 50f), ("legF2", -10f), ("legB1", 10f), ("legB2", -50f), ("lean", 8f),
                               ("armF1", 30f), ("armF2", 70f), ("sword", -30f), ("armB1", 40f), ("armB2", 90f), ("cape", -0.4f), ("hose", -0.6f));
            return Draw(p);
        }

        ShadedCanvas DrawFall(int f)
        {
            var p = Idle.With(("air", 1f), ("y", 8f), ("legF1", 25f + f * 4f), ("legF2", 5f), ("legB1", -20f), ("legB2", -40f - f * 4f),
                              ("lean", 4f), ("head", 6f), ("armF1", 60f), ("armF2", 100f), ("sword", 20f + f * 3f),
                              ("armB1", -70f), ("armB2", -40f), ("cape", -1f), ("capeLift", 1f), ("hose", 1f));
            return Draw(p);
        }

        ShadedCanvas DrawLand(int f)
        {
            float k = 1f - f / 2f;
            var p = Pose.Lerp(Idle, Idle.With(("legF1", 50f), ("legF2", -36f), ("legB1", 6f), ("legB2", -70f), ("crouch", 6f), ("lean", 20f),
                                              ("armF1", 40f), ("armF2", 60f), ("sword", -80f), ("armB1", 20f), ("armB2", 40f), ("cape", -0.3f)), k);
            return Draw(p);
        }

        // Combo 1: tajo horizontal. Fotogramas 2-3 = golpe activo.
        ShadedCanvas DrawAttack1(int f)
        {
            var keys = new Keyframes()
                .Key(0, Idle.With(("armF1", -110f), ("armF2", -140f), ("sword", 150f), ("lean", -4f), ("head", 4f), ("crouch", 1f), ("armB1", 30f), ("armB2", 50f), ("cape", 0.3f)))
                .Key(1, Idle.With(("armF1", -125f), ("armF2", -155f), ("sword", 162f), ("lean", -7f), ("head", 6f), ("crouch", 2f), ("armB1", 40f), ("armB2", 60f), ("cape", 0.35f)), Ease.Out)
                .Key(2, Idle.With(("armF1", 82f), ("armF2", 94f), ("sword", -8f), ("lean", 16f), ("head", -6f), ("crouch", 2.5f), ("legF1", 30f), ("legF2", 6f), ("legB1", -26f), ("legB2", -40f), ("armB1", -40f), ("armB2", -20f), ("cape", 0.9f)))
                .Key(3, Idle.With(("armF1", 100f), ("armF2", 118f), ("sword", -38f), ("lean", 18f), ("head", -6f), ("crouch", 2.5f), ("legF1", 30f), ("legF2", 6f), ("legB1", -26f), ("legB2", -40f), ("armB1", -45f), ("armB2", -25f), ("cape", 1f)))
                .Key(5, Idle.With(("lean", 8f), ("crouch", 1f), ("cape", 0.5f)));
            var p = keys.Evaluate(f);
            var smear = f == 2 ? Arc(p, 150f, -8f, 1f) : f == 3 ? Arc(p, 20f, -38f, 0.7f) : default;
            return Draw(p, smear);
        }

        // Combo 2: revés ascendente.
        ShadedCanvas DrawAttack2(int f)
        {
            var keys = new Keyframes()
                .Key(0, Idle.With(("armF1", 60f), ("armF2", 40f), ("sword", -80f), ("lean", 14f), ("crouch", 3f), ("legF1", 30f), ("legF2", 0f), ("cape", 0.6f)))
                .Key(1, Idle.With(("armF1", 50f), ("armF2", 20f), ("sword", -100f), ("lean", 18f), ("crouch", 4f), ("legF1", 34f), ("legF2", -6f), ("cape", 0.7f)), Ease.Out)
                .Key(2, Idle.With(("armF1", 150f), ("armF2", 165f), ("sword", 98f), ("lean", 2f), ("head", 8f), ("crouch", 0f), ("legF1", 22f), ("legF2", 4f), ("legB1", -20f), ("legB2", -30f), ("armB1", -60f), ("armB2", -40f), ("cape", -0.2f)))
                .Key(3, Idle.With(("armF1", 160f), ("armF2", 175f), ("sword", 118f), ("lean", 0f), ("head", 8f), ("legF1", 22f), ("legF2", 4f), ("legB1", -20f), ("legB2", -30f), ("armB1", -60f), ("armB2", -40f), ("cape", -0.3f)))
                .Key(5, Idle.With(("lean", 6f), ("cape", 0.3f)));
            var p = keys.Evaluate(f);
            var smear = f == 2 ? Arc(p, -100f, 98f, 1f) : f == 3 ? Arc(p, 60f, 118f, 0.7f) : default;
            return Draw(p, smear);
        }

        // Combo 3: mandoble descendente con todo el peso. Fotogramas 3-4 = golpe.
        ShadedCanvas DrawAttack3(int f)
        {
            var keys = new Keyframes()
                .Key(0, Idle.With(("armF1", 140f), ("armF2", 160f), ("sword", 120f), ("lean", -6f), ("head", 8f), ("crouch", 1f), ("armB1", 120f), ("armB2", 150f), ("cape", 0.2f)))
                .Key(2, Idle.With(("armF1", 175f), ("armF2", 200f), ("sword", 172f), ("lean", -12f), ("head", 12f), ("crouch", -1.5f), ("legF1", 4f), ("legF2", -6f), ("legB1", -16f), ("legB2", -26f), ("armB1", 165f), ("armB2", 190f), ("cape", -0.4f), ("capeLift", 0.5f)), Ease.In)
                .Key(3, Idle.With(("armF1", 70f), ("armF2", 60f), ("sword", -40f), ("lean", 24f), ("head", -8f), ("crouch", 5f), ("legF1", 46f), ("legF2", -14f), ("legB1", -30f), ("legB2", -60f), ("armB1", 60f), ("armB2", 50f), ("cape", 1f)))
                .Key(4, Idle.With(("armF1", 62f), ("armF2", 48f), ("sword", -52f), ("lean", 26f), ("head", -8f), ("crouch", 6f), ("legF1", 48f), ("legF2", -16f), ("legB1", -30f), ("legB2", -64f), ("armB1", 50f), ("armB2", 40f), ("cape", 1.1f)))
                .Key(7, Idle.With(("lean", 8f), ("crouch", 1f), ("cape", 0.4f)));
            var p = keys.Evaluate(f);
            var smear = f == 3 ? Arc(p, 172f, -40f, 1.2f) : f == 4 ? Arc(p, 40f, -52f, 0.8f) : default;
            return Draw(p, smear);
        }

        ShadedCanvas DrawAirAttack(int f)
        {
            var keys = new Keyframes()
                .Key(0, Idle.With(("air", 1f), ("y", 8f), ("armF1", 150f), ("armF2", 170f), ("sword", 135f), ("legF1", 50f), ("legF2", -20f), ("legB1", 20f), ("legB2", -60f), ("lean", -4f), ("cape", -0.6f), ("capeLift", 0.6f)))
                .Key(2, Idle.With(("air", 1f), ("y", 8f), ("armF1", 40f), ("armF2", 10f), ("sword", -100f), ("legF1", 60f), ("legF2", -10f), ("legB1", 30f), ("legB2", -50f), ("lean", 20f), ("cape", -0.8f), ("capeLift", 0.8f)))
                .Key(5, Idle.With(("air", 1f), ("y", 8f), ("armF1", 50f), ("armF2", 60f), ("sword", -60f), ("legF1", 40f), ("legF2", 0f), ("legB1", 0f), ("legB2", -40f), ("lean", 10f), ("cape", -0.6f), ("capeLift", 0.6f)));
            var p = keys.Evaluate(f);
            var smear = f == 2 ? Arc(p, 135f, -100f, 1f) : f == 3 ? Arc(p, -40f, -100f, 0.7f) : default;
            return Draw(p, smear);
        }

        // Deslizamiento: muy bajo, invulnerable. La capa vuela hacia atrás.
        ShadedCanvas DrawDodge(int f)
        {
            var slide = Idle.With(("legF1", 84f), ("legF2", 92f), ("legB1", -20f), ("legB2", -100f), ("crouch", 11f), ("lean", 46f), ("head", -12f),
                                  ("armF1", -40f), ("armF2", -60f), ("sword", -175f), ("armB1", 20f), ("armB2", 80f), ("cape", 1.6f), ("hose", -1.5f));
            var keys = new Keyframes()
                .Key(0, Idle.With(("crouch", 4f), ("lean", 24f), ("legF1", 40f), ("legF2", -20f), ("cape", 0.8f)))
                .Key(1, slide)
                .Key(4, slide.With(("crouch", 10f), ("cape", 1.4f)))
                .Key(5, Idle.With(("crouch", 3f), ("lean", 16f), ("cape", 0.9f)));
            return Draw(keys.Evaluate(f));
        }

        static Pose Guard => Idle.With(("armF1", 70f), ("armF2", 150f), ("sword", 95f), ("armB1", 60f), ("armB2", 130f),
                                        ("lean", -2f), ("head", 4f), ("crouch", 2.5f), ("legF1", 26f), ("legF2", -4f), ("legB1", -22f), ("legB2", -36f), ("cape", 0.3f));

        ShadedCanvas DrawParry(int f)
        {
            var keys = new Keyframes().Key(0, Idle.With(("armF1", 40f), ("armF2", 90f), ("sword", 40f))).Key(1, Guard).Key(3, Guard.With(("crouch", 2f)));
            return Draw(keys.Evaluate(f));
        }

        ShadedCanvas DrawParrySuccess(int f)
        {
            var push = Guard.With(("armF1", 95f), ("armF2", 110f), ("sword", 58f), ("lean", -10f), ("head", 10f), ("crouch", 3f), ("cape", -0.4f));
            var keys = new Keyframes().Key(0, Guard).Key(1, push, Ease.Out).Key(3, Guard);
            var c = Draw(keys.Evaluate(f));
            if (f == 1) Spark(c, V(30f, 46f), 9f);
            return c;
        }

        ShadedCanvas DrawHeal(int f)
        {
            // Clava el Garfio en el suelo y se inyecta el láudano por la válvula de la escafandra.
            var planted = Idle.With(("sword", -88f), ("armF1", 30f), ("armF2", 60f));
            var reach = planted.With(("armB1", 20f), ("armB2", 60f), ("lean", 10f), ("head", -10f), ("armBFront", 1f));
            var drink = planted.With(("armB1", 135f), ("armB2", 175f), ("lean", -4f), ("head", 16f), ("crouch", 1.5f), ("armBFront", 1f));
            var keys = new Keyframes().Key(0, Idle).Key(1, planted).Key(2, reach).Key(4, drink).Key(7, drink.With(("crouch", 2f))).Key(9, Idle);
            var p = keys.Evaluate(f);
            bool vial = f >= 2 && f <= 7;
            return Draw(p, default, vial ? "vial" : null, f >= 4 && f <= 6);
        }

        ShadedCanvas DrawCast(int f)
        {
            var raise = Idle.With(("armB1", 92f), ("armB2", 88f), ("lean", -6f), ("head", 4f), ("crouch", 1.5f), ("cape", -0.3f), ("hose", 0.8f));
            var keys = new Keyframes().Key(0, Idle).Key(2, raise, Ease.Out).Key(4, raise.With(("armB1", 96f), ("lean", -9f))).Key(5, Idle);
            return Draw(keys.Evaluate(f), default, f >= 2 && f <= 4 ? "sign" : null);
        }

        ShadedCanvas DrawHurt(int f)
        {
            var hit = Idle.With(("lean", -16f), ("head", 18f), ("armF1", -40f), ("armF2", -10f), ("sword", -120f), ("armB1", -70f), ("armB2", -30f),
                                ("legF1", 30f), ("legF2", 10f), ("legB1", -20f), ("legB2", -30f), ("cape", 1.1f), ("hose", 1.2f));
            var keys = new Keyframes().Key(0, hit).Key(2, Pose.Lerp(hit, Idle, 0.5f));
            return Draw(keys.Evaluate(f));
        }

        ShadedCanvas DrawDeath(int f)
        {
            var stagger = Idle.With(("lean", -14f), ("head", 16f), ("armF1", -30f), ("armF2", 0f), ("sword", -130f), ("cape", 0.9f));
            var knees = Idle.With(("legF1", 80f), ("legF2", -10f), ("legB1", 70f), ("legB2", -20f), ("crouch", 0f), ("lean", 10f), ("head", -20f),
                                  ("armF1", 10f), ("armF2", 20f), ("sword", -88f), ("armB1", 0f), ("armB2", 10f), ("cape", 0.4f), ("kneel", 1f));
            var fallen = knees.With(("lean", 82f), ("head", -10f), ("armF1", 100f), ("armF2", 110f), ("sword", -175f), ("armB1", 90f), ("armB2", 100f), ("cape", 0.2f), ("drop", 1f));
            var keys = new Keyframes().Key(0, Idle).Key(3, stagger).Key(7, knees, Ease.In).Key(11, fallen, Ease.In);
            return Draw(keys.Evaluate(f));
        }

        ShadedCanvas DrawPray(int f)
        {
            var kneel = Idle.With(("legF1", 88f), ("legF2", -4f), ("legB1", 72f), ("legB2", -18f), ("lean", 6f), ("head", -24f),
                                  ("armF1", 30f), ("armF2", 120f), ("sword", -90f), ("armB1", 40f), ("armB2", 130f), ("cape", 0.1f), ("kneel", 1f));
            var keys = new Keyframes().Key(0, Idle).Key(4, kneel, Ease.Out).Key(7, kneel.With(("head", -28f)));
            return Draw(keys.Evaluate(f), default, f >= 5 ? "glow" : null);
        }

        // Ejecución: remate a un enemigo aturdido que está ≈40 px delante. Paso al frente con el Garfio a dos manos
        // sobre la cabeza (f0-4, destello en f4), lo clava hacia abajo-delante con todo el peso (impacto en f5: la punta
        // queda en ExecTip y no se mueve hasta f10), lo retuerce temblando (f6-10) y lo arranca en f11 con un floreo
        // hacia atrás hasta la guardia del idle (f15 = idle f0).
        static readonly Vector2 ExecTip = new Vector2(40f, 22f);

        /// <summary>Agarre por cinemática inversa: mano delantera en (x, y), hoja a "sword" grados, mano trasera en el puño.</summary>
        static Pose Grip(Pose p, float x, float y, float sword, float twoHands = 1f) =>
            p.With(("ik", 1f), ("handX", x), ("handY", y), ("sword", sword), ("grip2", twoHands));

        /// <summary>Espada clavada: la mano se coloca para que la punta siga en ExecTip con la hoja a "angle" grados.</summary>
        static Pose Planted(Pose p, float angle)
        {
            var hand = Sub(ExecTip, Dir(angle, 46f));
            return Grip(p, hand.x, hand.y, angle);
        }

        ShadedCanvas DrawExecute(int f)
        {
            var idle0 = Idle.With(("crouch", 0.6f), ("cape", 0.15f + Mathf.Sin(0.6f) * 0.12f), ("hose", Mathf.Sin(1.2f) * 0.5f));
            var step = Idle.With(("legF1", 52f), ("legF2", 18f), ("legB1", -14f), ("legB2", -20f), ("x", 0.5f));
            var lunge = Idle.With(("legF1", 36f), ("legF2", -2f), ("legB1", -28f), ("legB2", -23f), ("x", 1f));
            var drive = Idle.With(("legF1", 36f), ("legF2", -2f), ("legB1", -28f), ("legB2", -23f), ("x", 1.5f));

            var keys = new Keyframes()
                // Anticipación: se agacha y junta las manos en el puño; paso al frente alzando la hoja; capa hacia atrás.
                .Key(0, Grip(idle0.With(("crouch", 3f), ("lean", 12f), ("head", -4f), ("cape", 0.35f), ("hose", 0.2f)), 12f, 31f, -28f))
                .Key(1, Grip(step.With(("crouch", 0.5f), ("lean", 0f), ("head", 4f), ("cape", 0.6f), ("capeLift", 0.1f), ("hose", -0.3f)), 14f, 47f, 50f))
                .Key(2, Grip(lunge.With(("crouch", -0.5f), ("lean", -10f), ("head", 10f), ("cape", 1f), ("capeLift", 0.35f), ("hose", -0.8f)), 0f, 62f, 140f))
                .Key(3, Grip(lunge.With(("crouch", -1.5f), ("lean", -16f), ("head", 14f), ("cape", 1.25f), ("capeLift", 0.55f), ("hose", -1f)), -3.5f, 62.5f, 168f))
                // Sostenido arriba (destello en la hoja).
                .Key(4, Grip(lunge.With(("crouch", -1.8f), ("lean", -17f), ("head", 15f), ("cape", 1.1f), ("capeLift", 0.65f), ("hose", -0.9f)), -4f, 62.8f, 171f))
                // Golpe (impacto en f5): la hoja llega a ExecTip con una estela grande; f6 = continuación con todo el peso.
                .Key(5, Planted(drive.With(("crouch", -1f), ("lean", 13f), ("head", -6f), ("cape", 1.45f), ("capeLift", 0.65f), ("hose", -1.2f)), -56f))
                .Key(6, Planted(drive.With(("crouch", -0.5f), ("lean", 16f), ("head", -3f), ("cape", 1.2f), ("capeLift", 0.45f), ("hose", -0.8f)), -57f))
                // Clavada: se hunde, retuerce la hoja y tiembla (1 px); la capa sigue oscilando.
                .Key(7, Planted(drive.With(("crouch", -0.3f), ("lean", 15f), ("head", -4f), ("cape", 0.9f), ("capeLift", 0.3f), ("hose", -0.2f)), -56f))
                .Key(8, Planted(drive.With(("x", 2.5f), ("crouch", -1f), ("lean", 10f), ("head", -8f), ("cape", 0.45f), ("capeLift", 0.1f), ("hose", 0.4f)), -53f))
                .Key(9, Planted(drive.With(("x", 1.5f), ("crouch", -1f), ("lean", 11f), ("head", -7f), ("cape", 0.7f), ("capeLift", 0.2f), ("hose", 0.2f)), -54f))
                .Key(10, Planted(drive.With(("x", 2.5f), ("crouch", -1f), ("lean", 9f), ("head", -9f), ("cape", 0.5f), ("capeLift", 0.1f), ("hose", 0.3f)), -52f))
                // La arranca hacia arriba y atrás, floreo sobre la cabeza y sacudida hasta la guardia.
                .Key(11, Grip(lunge.With(("crouch", 0f), ("lean", -10f), ("head", 10f), ("cape", -0.2f), ("capeLift", 0.1f), ("hose", 0.6f)), 6f, 62f, 75f))
                .Key(12, Grip(Idle.With(("legF1", 26f), ("legF2", 0f), ("legB1", -18f), ("legB2", -20f), ("x", 2f), ("crouch", -0.5f), ("lean", -12f), ("head", 12f),
                                        ("armB1", -35f), ("armB2", -8f), ("cape", -0.4f), ("capeLift", 0.2f), ("hose", 0.8f)), 1f, 63f, 150f, 0f))
                .Key(13, Grip(Idle.With(("x", 1f), ("crouch", 2.5f), ("lean", 10f), ("head", -2f), ("armB1", -30f), ("armB2", -10f), ("cape", 0.2f), ("hose", 0.6f)), 17f, 33f, -36f, 0f))
                .Key(14, Idle.With(("crouch", 1.5f), ("lean", 6f), ("armF1", 24f), ("armF2", 52f), ("sword", -58f), ("cape", 0.1f), ("hose", 0.4f)))
                .Key(15, idle0);
            var p = keys.Evaluate(f);
            var smear = f == 5 ? new SmearSpec { On = true, From = 171f, To = -56f, Strength = 1.2f, AtHand = true }
                      : f == 6 ? new SmearSpec { On = true, From = 20f, To = -57f, Strength = 0.6f, AtHand = true }
                      : f == 11 ? new SmearSpec { On = true, From = -52f, To = 75f, Strength = 1f, AtHand = true }
                      : f == 13 ? new SmearSpec { On = true, From = 110f, To = -36f, Strength = 0.6f, AtHand = true } : default;
            var c = Draw(p, smear);
            if (f == 4)
            {
                // Destello en la hoja alzada: filo encendido y estrella.
                var hand = V(p["handX"], p["handY"]);
                var edge = Dir(p["sword"] + 90f, 2f);
                c.Capsule(Add(hand, Add(Dir(p["sword"], 14f), edge)), Add(hand, Add(Dir(p["sword"], 38f), edge)), 0.5f, 0.5f, SmearMid, 9.2f);
                Spark(c, Add(hand, Dir(p["sword"], 31f)), 7f);
            }
            return c;
        }

        // ------------------------------------------------------------------
        // Dibujo del personaje a partir de una pose
        // ------------------------------------------------------------------

        struct SmearSpec
        {
            public bool On;
            public float From, To, Strength;
            /// <summary>Estela centrada en la mano (la hoja gira sobre la muñeca) en vez de en el hombro.</summary>
            public bool AtHand;
        }

        static SmearSpec Arc(Pose p, float from, float to, float strength) => new SmearSpec { On = true, From = from, To = to, Strength = strength };

        /// <summary>
        /// Cinemática inversa de dos huesos: devuelve el codo para que la mano llegue a <paramref name="hand"/>
        /// (si no alcanza, la acerca hasta el brazo estirado). El codo queda en el lado horario de la línea
        /// hombro→mano: hacia abajo con el brazo al frente, hacia delante con el brazo en alto.
        /// </summary>
        static Vector2 Reach(Vector2 shoulder, ref Vector2 hand, float upper, float fore)
        {
            float dx = hand.x - shoulder.x, dy = hand.y - shoulder.y;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist < 0.001f) { dx = 0f; dy = -1f; dist = 1f; }
            float ux = dx / dist, uy = dy / dist;
            float reach = Mathf.Clamp(dist, Mathf.Abs(upper - fore) + 0.01f, upper + fore - 0.01f);
            hand = V(shoulder.x + ux * reach, shoulder.y + uy * reach);
            float along = (upper * upper - fore * fore + reach * reach) / (2f * reach);
            float off = Mathf.Sqrt(Mathf.Max(0f, upper * upper - along * along));
            return V(shoulder.x + ux * along + uy * off, shoulder.y + uy * along - ux * off);
        }

        const float Thigh = 14.5f, Shin = 14f, UpperArm = 11f, Forearm = 10f, Spine = 17f;

        ShadedCanvas Draw(Pose p, SmearSpec smear = default, string prop = null, bool propGlow = false)
        {
            var c = NewFrame();
            float lean = p["lean"];
            bool kneel = p["kneel"] > 0.5f;

            // --- Piernas (cinemática directa) y apoyo automático en el suelo ---
            var hip0 = V(p["x"] - lean * 0.06f, 0f);
            var kneeF = Limb(hip0, p["legF1"], Thigh);
            var ankleF = Limb(kneeF, p["legF2"], Shin);
            var kneeB = Limb(hip0, p["legB1"], Thigh);
            var ankleB = Limb(kneeB, p["legB2"], Shin);
            float lowest = Mathf.Min(ankleF.y, ankleB.y);
            if (kneel) lowest = Mathf.Min(lowest, Mathf.Min(kneeF.y, kneeB.y) + 1f);
            float hipY = p["air"] > 0.5f ? 27f + p["y"] : 2.5f - lowest - p["crouch"] * 0.15f + p["y"];
            if (p["air"] <= 0.5f) hipY -= 0f;
            var lift = V(0f, hipY);
            var hip = Add(hip0, lift);
            kneeF = Add(kneeF, lift); ankleF = Add(ankleF, lift);
            kneeB = Add(kneeB, lift); ankleB = Add(ankleB, lift);
            if (!kneel && p["air"] <= 0.5f)
            {
                // Agacharse: baja la cadera sin hundir los pies.
                float drop = p["crouch"];
                hip = Add(hip, V(0f, -drop));
            }
            if (p["drop"] > 0.5f) hip = Add(hip, V(0f, -4f));

            // --- Tronco y cabeza ---
            float torsoAngle = 90f - lean;
            var chest = Add(hip, Dir(torsoAngle, Spine - 3f));
            var neck = Add(hip, Dir(torsoAngle, Spine + 1f));
            var helmet = Add(neck, Rotate(V(1.5f, 8.5f), V(0f, 0f), -lean * 0.8f - p["head"]));
            var shoulderF = Add(chest, Dir(torsoAngle - 90f, 1.5f));
            var shoulderB = Add(chest, Dir(torsoAngle + 90f, 1.5f));
            var elbowF = Limb(shoulderF, p["armF1"], UpperArm);
            var handF = Limb(elbowF, p["armF2"], Forearm);
            var elbowB = Limb(shoulderB, p["armB1"], UpperArm);
            var handB = Limb(elbowB, p["armB2"], Forearm);
            // Cinemática inversa opcional (ejecución): "ik" lleva la mano delantera a (handX, handY) para que la hoja
            // clavada no se mueva aunque el cuerpo tiemble; "grip2" lleva la mano trasera a la empuñadura (a dos manos).
            if (p["ik"] > 0f)
            {
                handF = Mix(handF, V(p["handX"], p["handY"]), p["ik"]);
                elbowF = Reach(shoulderF, ref handF, UpperArm, Forearm);
            }
            if (p["grip2"] > 0f)
            {
                handB = Mix(handB, Add(handF, Dir(p["sword"], -4f)), p["grip2"]);
                elbowB = Reach(shoulderB, ref handB, UpperArm, Forearm);
            }

            int gHelmet = c.NewGroup(), gTorso = c.NewGroup(), gLegF = c.NewGroup(), gLegB = c.NewGroup();
            int gArmF = c.NewGroup(), gArmB = c.NewGroup(), gCape = c.NewGroup(), gSword = c.NewGroup();

            // --- Capa (detrás de todo) ---
            float sway = p["cape"], capeLift = p["capeLift"];
            var capeTop = Add(neck, Dir(torsoAngle + 90f, 5f));
            var capeBottom = Add(hip, V(-11f - sway * 14f, -17f + capeLift * 14f + Mathf.Abs(sway) * 4f));
            var capeMid = Add(Mix(capeTop, capeBottom, 0.5f), V(-4f - sway * 4f, capeLift * 3f));
            var capePoints = new List<Vector2> { Add(capeTop, V(6f, 0f)), capeTop };
            capePoints.AddRange(Bezier(capeTop, capeMid, capeBottom, 6));
            capePoints.AddRange(Tatters(capeBottom, Add(hip, V(2f - sway * 4f, -12f + capeLift * 8f)), 5, 3f, 17));
            capePoints.Add(Add(chest, V(2f, 0f)));
            // Exterior de lana oscura asomando por detrás del forro carmesí (dos capas).
            var outer = new Vector2[capePoints.Count];
            for (int k = 0; k < outer.Length; k++) outer[k] = Add(capePoints[k], V(-1.3f, 0.5f));
            c.Poly(outer, CapeOuter, -0.1f, 2f, -0.08f, gCape, -0.35f, 0f);
            c.Poly(capePoints.ToArray(), Lining, 0f, 2.5f, -0.05f, gCape, -0.35f, 0f);
            // Pliegues que caen del hombro, agujeros y hebras sueltas en el borde raído.
            var hemEnd = Add(hip, V(2f - sway * 4f, -12f + capeLift * 8f));
            for (int k = 1; k <= 3; k++)
            {
                float t = k / 4f;
                var top = Mix(capeTop, Add(chest, V(2f, 0f)), t * 0.7f);
                var bottom = Add(Mix(capeBottom, hemEnd, t), V(0f, 2.2f));
                var mid = Add(Mix(top, bottom, 0.5f), V(-1.2f - sway * 1.5f, 0f));
                c.Fold(Bezier(top, mid, bottom, 4), gCape, 2, 1, Lining);
            }
            c.Dot(Add(Mix(capeBottom, hemEnd, 0.3f), V(0.4f, 3.4f)), -3, gCape, Lining);
            c.Dot(Add(Mix(capeBottom, hemEnd, 0.62f), V(0f, 4.2f)), -3, gCape, Lining);
            foreach (float t in new[] { 0.15f, 0.55f, 0.85f })
            {
                var tip = Mix(capeBottom, hemEnd, t);
                c.Capsule(Add(tip, V(0f, -1.6f)), Add(tip, V(-0.8f - sway, -4f)), 0.45f, 0.4f, Lining, -0.05f, -0.1f, gCape);
            }

            // --- Manguera de la escafandra (detrás del cuerpo) ---
            var hoseStart = Add(helmet, Rotate(V(-8f, -1f), V(0f, 0f), -lean));
            var hoseEnd = Add(hip, V(-6f, 3f));
            var hoseCtrl = Add(Mix(hoseStart, hoseEnd, 0.5f), V(-9f - p["hose"] * 5f, p["hose"] * 2f));
            int gHose = c.NewGroup();
            var hosePts = Bezier(hoseStart, hoseCtrl, hoseEnd, 7);
            c.Strand(hosePts, 2.1f, 1.7f, Hose, 0.5f, 0f, gHose);
            // Anillas de refuerzo de la manguera (aros oscuros con su canto claro) y abrazadera de latón en la cadera.
            for (int k = 1; k < hosePts.Length - 1; k++)
            {
                var d = Sub(hosePts[k + 1], hosePts[k - 1]);
                float l = Mathf.Max(0.001f, Mathf.Sqrt(d.x * d.x + d.y * d.y));
                var n = V(-d.y / l, d.x / l);
                c.Crease(new[] { Add(hosePts[k], Scale(n, -2.2f)), Add(hosePts[k], Scale(n, 2.2f)) }, 2, gHose, Hose);
                var lit = Add(hosePts[k], Scale(d, 0.9f / l));
                c.Ridge(new[] { Add(lit, Scale(n, -1.6f)), Add(lit, Scale(n, 1.6f)) }, 1, gHose, Hose);
            }
            c.Ellipse(hoseEnd, 2.3f, 2.3f, 0f, BrassDark, 0.55f, 0f, c.NewGroup());

            // --- Brazo y pierna traseros (más oscuros: están lejos de la luz) ---
            DrawLeg(c, hip, kneeB, ankleB, 1f, -0.14f, gLegB);
            // En la curación el brazo trasero pasa por delante del pecho para acercar el frasco a la válvula.
            bool armBInFront = p["armBFront"] > 0.5f;
            float zArmB = armBInFront ? 10.5f : 1.5f;
            DrawArm(c, shoulderB, elbowB, handB, zArmB, armBInFront ? -0.04f : -0.12f, gArmB);
            if (prop == "vial") DrawVial(c, handB, propGlow, zArmB + 0.2f);
            if (prop == "sign") DrawSign(c, Add(handB, V(4f, 1f)));

            // --- Torso con abrigo ---
            var waist = Add(hip, Dir(torsoAngle, 4f));
            c.Capsule(hip, chest, 6.2f, 7.4f, Coat, 4f, 0f, gTorso);
            var hemF = Add(Mix(hip, kneeF, 0.62f), V(3.5f, 0f));
            var hemB = Add(Mix(hip, kneeB, 0.7f), V(-5f - sway * 4f, 0f));
            var coatPoints = new List<Vector2>
            {
                Add(chest, Dir(torsoAngle - 90f, 6.5f)), Add(waist, Dir(torsoAngle - 90f, 6.8f)), hemF,
            };
            coatPoints.AddRange(Tatters(hemF, hemB, 4, 2.5f, 5));
            coatPoints.Add(Add(waist, Dir(torsoAngle + 90f, 7.2f)));
            coatPoints.Add(Add(chest, Dir(torsoAngle + 90f, 7f)));
            c.Poly(coatPoints.ToArray(), Coat, 4.5f, 2.5f, 0f, gTorso, 0.15f, 0f);
            DrawTorsoDetail(c, hip, chest, neck, waist, torsoAngle, hemF, hemB, gTorso);

            // --- Pierna delantera ---
            DrawLeg(c, hip, kneeF, ankleF, 6f, 0f, gLegF);

            // --- Escafandra ---
            DrawHelmet(c, p, neck, helmet, lean, gHelmet, out var port);

            // --- Brazo delantero y el Garfio ---
            if (smear.On && smear.AtHand)
                DrawSmear(c, handF, smear, 10f, 48f);
            else if (smear.On)
            {
                var tip = Add(handF, Dir(p["sword"], 46f));
                float reach = Mathf.Sqrt((tip.x - shoulderF.x) * (tip.x - shoulderF.x) + (tip.y - shoulderF.y) * (tip.y - shoulderF.y));
                float baseR = Mathf.Sqrt((handF.x - shoulderF.x) * (handF.x - shoulderF.x) + (handF.y - shoulderF.y) * (handF.y - shoulderF.y)) + 8f;
                DrawSmear(c, shoulderF, smear, baseR, reach + 2f);
            }
            DrawSword(c, handF, p["sword"], 9f, gSword);
            DrawArm(c, shoulderF, elbowF, handF, 9.5f, 0f, gArmF);
            if (prop == "glow") c.Ellipse(Add(port, V(0f, 0f)), 1.6f, 1.6f, 0f, GlassCore, 8.6f, 0f, gHelmet);

            return c;
        }

        /// <summary>
        /// Escafandra de latón: peto remachado con tuercas de mariposa, casco martilleado con costura remachada y
        /// verdín, ojo de buey frontal (aro con pernos, rejilla y cristal encendido con reflejo), ojo de buey trasero
        /// apagado, válvula con volante y racor de la manguera.
        /// </summary>
        void DrawHelmet(ShadedCanvas c, Pose p, Vector2 neck, Vector2 helmet, float lean, int g, out Vector2 port)
        {
            float hr = -lean * 0.8f - p["head"];
            Vector2 H(float x, float y) => Add(helmet, Rotate(V(x, y), V(0f, 0f), hr));

            // Peto (corselete) remachado y dos tuercas de mariposa que lo sujetan al casco.
            var collar = Add(neck, V(0.5f, 1.5f));
            c.Ellipse(collar, 8.5f, 3.6f, -lean * 0.5f, BrassDark, 7f, 0f, g).WithBump(Patterns.Rivets(3f, 0.9f, -0.8f, 0.5f), 1.6f);
            var collarAxis = Dir(-lean * 0.5f);
            int gn = c.NewGroup();
            foreach (float k in new[] { -7.4f, 7f })
            {
                var nut = Add(collar, Add(Scale(collarAxis, k), V(0f, 1.4f)));
                c.Capsule(Add(nut, V(-1.3f, 0.9f)), Add(nut, V(1.3f, 0.9f)), 0.6f, 0.6f, Brass, 7.06f, 0f, gn);
                c.Ellipse(nut, 1.1f, 1.2f, 0f, BrassDark, 7.07f, 0f, gn);
            }

            // Casco: latón pulido con un poco de verdín en el borde inferior (años bajo el agua).
            c.Ellipse(helmet, 9.4f, 10f, -lean * 0.3f, Brass, 8f, 0f, g)
             .Soft()
             .WithTint(Verdigris, 0.45f, (u, v) => v < -4.5f && v > -6.5f && PixelCanvas.ValueNoise(u / 1.6f, v / 1.4f, 0, 31) > 0.7f ? 1f : 0f);
            // Aro del cuello: banda oscura atornillada donde el casco se une al peto.
            int gr = c.NewGroup();
            var ring = H(-0.4f, -7.9f);
            c.Ellipse(ring, 8.6f, 2.1f, hr, BrassDark, 8.05f, 0f, gr)
             .WithTint(Verdigris, 0.5f, (u, v) => PixelCanvas.ValueNoise(u / 1.5f, v, 0, 37) > 0.7f ? 1f : 0f);
            for (int k = 0; k < 4; k++)
                c.Dot(H(-6.2f + k * 3.9f, -7.6f - Mathf.Abs(k - 1.6f) * 0.25f), 2, gr, BrassDark);
            // Brillo especular del latón (arriba-delante) con su halo de 1 px.
            c.Glint(H(2.2f, 6.6f), g, 0, Brass);
            c.Dot(H(1.2f, 6.6f), 1, g, Brass);
            c.Dot(H(2.2f, 5.6f), 1, g, Brass);

            // Rejilla lateral de ventilación (placa oscura con dos ranuras), detrás de la oreja.
            var grille = H(-5.8f, 1.8f);
            int gb = c.NewGroup();
            c.Ellipse(grille, 2.4f, 2f, 90f + hr, BrassDark, 8.12f, 0f, gb).WithDetail(Patterns.Stripes(1.6f, 0.8f, -1, 0.6f));

            // Válvula de escape con volante.
            var valve = Add(helmet, Rotate(V(-1f, 10f), V(0f, 0f), hr));
            var valveTop = Add(valve, Rotate(V(0f, 3f), V(0f, 0f), -lean));
            c.Capsule(valve, valveTop, 1.8f, 1.4f, BrassDark, 8.1f, 0f, g);
            c.Capsule(Add(valveTop, Rotate(V(-2.2f, 0.3f), V(0f, 0f), -lean)), Add(valveTop, Rotate(V(2.2f, 0.3f), V(0f, 0f), -lean)), 0.55f, 0.55f, Brass, 8.11f, 0f, g);

            // Racor de la manguera (tuerca estriada) en la nuca.
            var nozzle = H(-8.2f, -1.5f);
            c.Ellipse(nozzle, 2f, 2.4f, hr, BrassDark, 8.14f, 0f, c.NewGroup()).WithBump(Patterns.Ridges(1.6f, 0.6f), 1.2f);

            // Ojo de buey frontal: aro oscuro con cuatro pernos, cristal encendido y reflejo.
            port = H(4.6f, 0.3f);
            int gp = c.NewGroup();
            c.Ellipse(port, 5.2f, 5.6f, 0f, BrassDark, 8.2f, 0f, gp);
            for (int k = 0; k < 4; k++)
            {
                var bolt = Add(port, Dir(45f + k * 90f + hr, 4.7f));
                c.Dot(bolt, 2, gp, BrassDark);
            }
            c.Ellipse(port, 3.8f, 4.2f, 0f, GlassRing, 8.3f, 0f, gp);
            c.Ellipse(Add(port, V(0.4f, 0.2f)), 2.6f, 3f, 0f, GlassMid, 8.4f, 0f, gp);
            c.Ellipse(Add(port, V(0.9f, 0.9f)), 1.1f, 1.3f, 0f, GlassCore, 8.5f, 0f, gp);
            c.Capsule(Add(port, V(-0.6f, 1.6f)), Add(port, V(0.3f, 2.7f)), 0.45f, 0.45f, GlassGlint, 8.6f);
        }

        /// <summary>
        /// Detalle del abrigo y el equipo: pliegues de la falda, tapeta con botones, pespunte del bajo, bandolera
        /// cosida con hebilla, cinturón con hebilla, bolsas de cuero con solapa y un rollo de cuerda a la espalda.
        /// </summary>
        void DrawTorsoDetail(ShadedCanvas c, Vector2 hip, Vector2 chest, Vector2 neck, Vector2 waist, float torsoAngle,
                             Vector2 hemF, Vector2 hemB, int gTorso)
        {
            // T(a, d): a px a lo largo de la espalda desde la cadera, d px hacia delante (negativo = hacia atrás).
            Vector2 T(float a, float d) => Add(hip, Add(Dir(torsoAngle, a), Dir(torsoAngle - 90f, d)));

            // Cuello alto del abrigo bajo el peto.
            c.Ellipse(Add(neck, Dir(torsoAngle, -1.5f)), 5.4f, 2.8f, torsoAngle - 90f, Coat, 6.5f, 0f, gTorso);
            // Pliegues de la falda del abrigo (surco + cresta) y pespunte del bajo.
            for (int k = 1; k <= 3; k++)
            {
                float t = k / 4f;
                var top = Mix(T(2.5f, 5f), T(2.5f, -6f), t);
                var bottom = Add(Mix(hemF, hemB, t), V(0f, 1.2f));
                c.Fold(new[] { top, Mix(top, bottom, 0.55f), bottom }, gTorso, 2, 1, Coat);
            }
            c.Stitch(new[] { Add(hemF, V(-0.5f, 1.8f)), Add(hemB, V(0.5f, 1.8f)) }, gTorso, 1, 2, 1, Coat);
            // Tapeta delantera con dos botones de latón.
            c.Crease(new[] { T(16f, 4.4f), T(5f, 4.8f) }, 1, gTorso, Coat);
            c.Ellipse(T(12.6f, 3.9f), 0.75f, 0.75f, 0f, Brass, 4.95f, 0f, gTorso);
            c.Ellipse(T(9f, 4.1f), 0.75f, 0.75f, 0f, Brass, 4.95f, 0f, gTorso);
            // Pliegue bajo el pecho, donde el abrigo se recoge en el cinturón.
            c.Fold(new[] { T(7.5f, -5f), T(6.2f, -1f), T(6.8f, 3f) }, gTorso, 1, 1, Coat);

            // Bandolera cosida del hombro trasero a la cadera delantera, con hebilla.
            int gStrap = c.NewGroup();
            var s0 = T(16.5f, -5.5f);
            var s1 = T(2f, 6.2f);
            c.Capsule(s0, s1, 1.15f, 1.15f, Strap, 4.85f, 0f, gStrap);
            c.Stitch(new[] { s0, s1 }, gStrap, 1, 2, 1, Strap);
            var sb = Mix(s0, s1, 0.42f);
            c.Ellipse(sb, 1.7f, 1.5f, torsoAngle, Brass, 4.87f, 0f, gStrap);
            c.Dot(sb, -2, gStrap, Brass);

            // Cinturón con pespunte y hebilla delantera.
            int gBelt = c.NewGroup();
            var b0 = Add(waist, Dir(torsoAngle + 90f, 6.5f));
            var b1 = Add(waist, Dir(torsoAngle - 90f, 6.5f));
            c.Capsule(b0, b1, 1.6f, 1.6f, Leather, 4.9f, 0f, gBelt);
            c.Stitch(new[] { Add(b0, Dir(torsoAngle, 0.6f)), Add(b1, Dir(torsoAngle, 0.6f)) }, gBelt, 1, 2, 1, Leather);
            var buckle = T(4f, 4.6f);
            c.Poly(new[] { Add(buckle, Add(Dir(torsoAngle, 1.9f), Dir(torsoAngle - 90f, -1.4f))), Add(buckle, Add(Dir(torsoAngle, 1.9f), Dir(torsoAngle - 90f, 1.4f))),
                           Add(buckle, Add(Dir(torsoAngle, -1.9f), Dir(torsoAngle - 90f, 1.4f))), Add(buckle, Add(Dir(torsoAngle, -1.9f), Dir(torsoAngle - 90f, -1.4f))) },
                   Brass, 4.95f, 0.8f, 0f, gBelt);
            c.Dot(buckle, -2, gBelt, Brass);

            // Bolsa delantera colgando del cinturón (solapa con botón).
            int gPouch = c.NewGroup();
            c.Poly(new[] { T(3.2f, 1.2f), T(3.2f, 5.0f), T(-2.6f, 4.8f), T(-3f, 1.4f) }, Leather, 4.88f, 1.2f, 0f, gPouch);
            c.Crease(new[] { T(0.6f, 1.4f), T(0.4f, 4.8f) }, 2, gPouch, Leather);
            c.Ellipse(T(0.9f, 3.1f), 0.7f, 0.7f, 0f, Brass, 4.89f, 0f, gPouch);
            // Bolsa trasera y cabo de cuerda colgando a la espalda.
            int gBack = c.NewGroup();
            c.Poly(new[] { T(3f, -3.4f), T(3f, -7.6f), T(-3.6f, -7.8f), T(-3.2f, -3.2f) }, Strap, 4.7f, 1.4f, 0f, gBack);
            c.Crease(new[] { T(0.4f, -3.4f), T(0.6f, -7.6f) }, 1, gBack, Strap);
            c.Capsule(Add(waist, V(-2f, -2f)), Add(waist, V(3f, -6f)), 0.9f, 0.9f, Rope, 4.91f, 0f, c.NewGroup())
             .WithBump(Patterns.Ridges(1.5f, 0.6f), 1.2f);
        }

        void DrawLeg(ShadedCanvas c, Vector2 hip, Vector2 knee, Vector2 ankle, float z, float shade, int group)
        {
            c.Capsule(hip, knee, 3.8f, 3.1f, Trousers, z, shade, group);
            // Pliegue del pantalón y correa del muslo.
            var down = Sub(knee, hip);
            float dl = Mathf.Max(0.001f, Mathf.Sqrt(down.x * down.x + down.y * down.y));
            var front = V(-down.y / dl, down.x / dl);
            if (front.x < 0f) front = Scale(front, -1f);
            c.Fold(new[] { Add(Mix(hip, knee, 0.25f), Scale(front, 1.2f)), Add(Mix(hip, knee, 0.8f), Scale(front, 1.8f)) }, group, 2, 1, Trousers);
            int gBand = c.NewGroup();
            var band = Mix(hip, knee, 0.5f);
            c.Capsule(Add(band, Scale(front, -3.5f)), Add(band, Scale(front, 3.5f)), 0.85f, 0.85f, Strap, z + 0.03f, shade, gBand);
            // Caña de la bota con vuelta, cordones y rodillera.
            var shinEnd = Add(ankle, V(0f, 2f));
            c.Capsule(knee, shinEnd, 3.2f, 2.8f, BootLeather, z + 0.1f, shade, group);
            var sd = Sub(shinEnd, knee);
            float sl = Mathf.Max(0.001f, Mathf.Sqrt(sd.x * sd.x + sd.y * sd.y));
            var sf = V(-sd.y / sl, sd.x / sl);
            if (sf.x < 0f) sf = Scale(sf, -1f);
            int gCuff = c.NewGroup();
            var cuff = Mix(knee, shinEnd, 0.2f);
            c.Capsule(Add(cuff, Scale(sf, -3.3f)), Add(cuff, Scale(sf, 3.3f)), 1.2f, 1.2f, BootLeather, z + 0.12f, shade, gCuff);
            // Cordones: cruces claras alternas con huecos oscuros.
            var l0 = Add(Mix(knee, shinEnd, 0.36f), Scale(sf, 1.6f));
            var l1 = Add(Mix(knee, shinEnd, 0.95f), Scale(sf, 1.4f));
            c.Mark(new[] { l0, l1 }, 1, group, BootLeather, 1, 1);
            c.Mark(new[] { Add(l0, Scale(sf, -1f)), Add(l1, Scale(sf, -1f)) }, -1, group, BootLeather, 1, 1);
            // Bota de buzo: empeine de cuero, puntera de latón y suela de plomo.
            var toe = Add(ankle, V(6.5f, -1.6f));
            c.Poly(new[] { Add(ankle, V(-3f, 2.5f)), Add(ankle, V(2.5f, 2.5f)), Add(toe, V(0f, 1.2f)), Add(toe, V(0.5f, -1f)), Add(ankle, V(-3.2f, -2.5f)) },
                   BootLeather, z + 0.2f, 1.5f, shade, group);
            int gSole = c.NewGroup();
            c.Poly(new[] { Add(ankle, V(-3.6f, -1.1f)), Add(ankle, V(7.4f, -1.3f)), Add(ankle, V(7.6f, -2.5f)), Add(ankle, V(-3.7f, -2.5f)) },
                   IronDark, z + 0.25f, 0.8f, shade, gSole);
            c.Ellipse(Add(toe, V(-0.4f, 0.2f)), 1.9f, 1.5f, 0f, Brass, z + 0.24f, shade, gSole);
        }

        void DrawArm(ShadedCanvas c, Vector2 shoulder, Vector2 elbow, Vector2 hand, float z, float shade, int group)
        {
            c.Capsule(shoulder, elbow, 3.3f, 2.7f, Coat, z, shade, group);
            c.Capsule(elbow, hand, 2.7f, 2.3f, Coat, z + 0.05f, shade, group);
            // Pliegue de la manga en el codo.
            var e0 = Mix(shoulder, elbow, 0.8f);
            var across = Rotate(Sub(elbow, shoulder), V(0f, 0f), 90f);
            float al = Mathf.Max(0.001f, Mathf.Sqrt(across.x * across.x + across.y * across.y));
            across = Scale(across, 1f / al);
            c.Fold(new[] { Add(e0, Scale(across, -2.2f)), Add(Mix(shoulder, elbow, 0.92f), Scale(across, 1.6f)) }, group, 2, 1, Coat);
            // Costura de la sisa (pespunte claro donde la manga se une al hombro).
            c.Stitch(new[] { Add(Mix(shoulder, elbow, 0.3f), Scale(across, -2.8f)), Add(Mix(shoulder, elbow, 0.36f), Scale(across, 2.8f)) }, group, 1, 1, 1, Coat);
            // Brazal de cuero con dos correas.
            int gBr = c.NewGroup();
            c.Capsule(Mix(elbow, hand, 0.28f), Mix(elbow, hand, 0.82f), 2.75f, 2.45f, Strap, z + 0.07f, shade, gBr)
             .WithDetail(Patterns.Stripes(2.6f, 0.9f, -2, 0.8f));
            // Guante.
            c.Ellipse(hand, 2.6f, 2.4f, 0f, Leather, z + 0.1f, shade, group);
            c.Dot(Add(hand, V(0.6f, 1f)), 1, group, Leather);
        }

        /// <summary>
        /// El Garfio de Devil Reef: una hoja de pesca convertida en arma. Pomo con el ojo de un anzuelo (anilla hueca),
        /// empuñadura envuelta en tiras de cuero con virolas, guarda de latón en cola de pez con botón central,
        /// hoja con filo brillante, acanaladura y óxido, y una uña curva con lengüeta (el anzuelo) hacia atrás.
        /// La punta sigue en (46, 0) a lo largo de la hoja (la ejecución depende de ello).
        /// </summary>
        void DrawSword(ShadedCanvas c, Vector2 hand, float angle, float z, int group)
        {
            var axis = Dir(angle);
            var side = Dir(angle + 90f);
            Vector2 At(float along, float across) => Add(hand, Add(Scale(axis, along), Scale(side, across)));

            // Pomo: ojo del anzuelo (anilla con hueco de verdad).
            var eye = At(-7.2f, 0f);
            var ringPts = new List<Vector2>();
            for (int k = 0; k <= 10; k++) ringPts.Add(Add(eye, Dir(angle + k * 36f, 1.7f)));
            c.Strand(ringPts, 0.75f, 0.75f, IronDark, z + 0.04f, 0f, group);
            // Empuñadura envuelta en diagonal, con virolas de latón en los extremos.
            c.Capsule(At(-5f, 0f), At(3f, 0f), 1.5f, 1.5f, Leather, z, 0f, group)
             .WithDetail((u, v) => Mathf.Repeat(u + v * 0.9f, 2.2f) < 0.9f ? -1 : 0);
            c.Capsule(At(-5.4f, 0f), At(-4.4f, 0f), 1.75f, 1.75f, BrassDark, z + 0.05f, 0f, group);
            c.Capsule(At(2.2f, 0f), At(3.2f, 0f), 1.75f, 1.75f, BrassDark, z + 0.05f, 0f, group);
            // Guarda en cola de pez: barra curva con remates en bola y botón central.
            int gGuard = c.NewGroup();
            c.Strand(new[] { At(6.4f, -5.6f), At(4.6f, -3.6f), At(4.1f, 0f), At(4.6f, 3.6f), At(6.4f, 5.6f) }, 1.15f, 1.15f, Brass, z + 0.1f, 0f, gGuard);
            c.Ellipse(At(6.6f, -5.9f), 1.25f, 1.25f, 0f, Brass, z + 0.11f, 0f, gGuard);
            c.Ellipse(At(6.6f, 5.9f), 1.25f, 1.25f, 0f, Brass, z + 0.11f, 0f, gGuard);
            c.Ellipse(At(4.6f, 0f), 1.7f, 1.7f, 0f, BrassDark, z + 0.12f, 0f, gGuard);
            c.Glint(At(4.9f, 0.6f), gGuard, 0, BrassDark);
            // Hoja: ancha, con lomo recto y filo curvado, termina en punta.
            c.Poly(new[]
            {
                At(5.5f, -2.6f), At(20f, -3.3f), At(34f, -3.6f), At(41f, -2.4f), At(46f, 0.6f),
                At(40f, 2.6f), At(30f, 3f), At(5.5f, 2.6f),
            }, Iron, z + 0.05f, 1.6f, 0f, group).Frame(hand, angle);
            // El anzuelo: uña curva que nace del lomo y vuelve hacia la empuñadura, con su lengüeta.
            int gHook = c.NewGroup();
            c.Strand(Bezier(At(36.5f, 2f), At(34f, 9.5f), At(25f, 8.6f), 6), 2.2f, 0.7f, Iron, z + 0.06f, -0.05f, gHook);
            c.Poly(new[] { At(25.6f, 9.4f), At(23.2f, 9.2f), At(27.6f, 6.4f) }, Iron, z + 0.065f, 0.8f, -0.05f, gHook);
            c.Ridge(Bezier(At(35.6f, 3f), At(33.6f, 9.6f), At(26f, 9.4f), 6), 1, gHook, Iron);
            // Acanaladura central y filo brillante.
            c.Crease(new[] { At(8f, 0.2f), At(31f, 0.2f) }, 2, group, Iron);
            c.Ridge(new[] { At(6.5f, -1.8f), At(20f, -2.5f), At(34f, -2.8f), At(40.5f, -1.7f), At(44.5f, 0.3f) }, 2, group, Iron);
            c.Glint(At(41.5f, -1.2f), group, 0, Iron);
        }

        /// <summary>Estela en media luna: fina en la cola, gruesa en la punta; borde exterior brillante.</summary>
        void DrawSmear(ShadedCanvas c, Vector2 shoulder, SmearSpec s, float inner, float outer)
        {
            float band = outer - inner;
            float thin = s.Strength >= 1f ? 1f : 0.55f;
            float r = outer + 1f;
            c.Custom(shoulder.x - r, shoulder.y - r, shoulder.x + r, shoulder.y + r,
                     SmearArc(shoulder, inner + band * (1f - thin), outer, s.From, s.To), SmearSoft, 8.6f);
            c.Custom(shoulder.x - r, shoulder.y - r, shoulder.x + r, shoulder.y + r,
                     SmearArc(shoulder, outer - band * 0.55f * thin, outer, s.From, s.To), SmearMid, 8.7f);
            c.Custom(shoulder.x - r, shoulder.y - r, shoulder.x + r, shoulder.y + r,
                     SmearArc(shoulder, outer - band * 0.22f * thin, outer, s.From, s.To), Smear, 8.8f);
        }

        void DrawVial(ShadedCanvas c, Vector2 hand, bool glowing, float z)
        {
            c.Capsule(Add(hand, V(0f, 1f)), Add(hand, V(0f, 6f)), 1.9f, 1.7f, glowing ? Laudanum : PixelMaterial.Glow("c8601e"), z);
            c.Ellipse(Add(hand, V(0f, 7f)), 1.2f, 1f, 0f, Leather, z + 0.05f);
        }

        void DrawSign(ShadedCanvas c, Vector2 at)
        {
            c.Ellipse(at, 5f, 5f, 0f, PixelMaterial.Glow("1f6b58"), 12f);
            c.Capsule(Add(at, V(0f, -3.5f)), Add(at, V(0f, 3.5f)), 0.7f, 0.7f, Sign, 12.1f);
            c.Capsule(Add(at, V(0f, 0.5f)), Add(at, V(-2.5f, 3f)), 0.6f, 0.6f, Sign, 12.1f);
            c.Capsule(Add(at, V(0f, 0.5f)), Add(at, V(2.5f, 3f)), 0.6f, 0.6f, Sign, 12.1f);
            c.Capsule(Add(at, V(0f, -1.5f)), Add(at, V(-2f, 0.5f)), 0.6f, 0.6f, Sign, 12.1f);
            c.Capsule(Add(at, V(0f, -1.5f)), Add(at, V(2f, 0.5f)), 0.6f, 0.6f, Sign, 12.1f);
        }

        static void Spark(ShadedCanvas c, Vector2 at, float size)
        {
            var white = PixelMaterial.Glow("f4fff8");
            c.Capsule(Add(at, V(-size, 0f)), Add(at, V(size, 0f)), 0.8f, 0.8f, white, 13f);
            c.Capsule(Add(at, V(0f, -size)), Add(at, V(0f, size)), 0.8f, 0.8f, white, 13f);
            c.Capsule(Add(at, V(-size * 0.5f, -size * 0.5f)), Add(at, V(size * 0.5f, size * 0.5f)), 0.6f, 0.6f, white, 13f);
            c.Capsule(Add(at, V(-size * 0.5f, size * 0.5f)), Add(at, V(size * 0.5f, -size * 0.5f)), 0.6f, 0.6f, white, 13f);
        }

        /// <summary>Borde deshilachado entre dos puntos (bajo de la capa y del abrigo).</summary>
        static IEnumerable<Vector2> Tatters(Vector2 from, Vector2 to, int teeth, float depth, int seed)
        {
            int count = teeth * 2;
            for (int i = 1; i < count; i++)
            {
                float t = i / (float)count;
                var p = Mix(from, to, t);
                float d = (i % 2 == 1 ? depth : 0f) * (0.6f + 0.8f * PixelCanvas.Hash(i, seed, 3));
                yield return Add(p, V(0f, -d));
            }
        }
    }
}
