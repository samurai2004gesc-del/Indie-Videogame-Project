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

        static readonly PixelMaterial Brass = new PixelMaterial(Ramp.Make("b08a3e", 5, 0.12f, 0.3f, 1.5f)) { Gloss = 0.6f, Ambient = 0.26f, Rim = 0.5f };
        static readonly PixelMaterial BrassDark = new PixelMaterial(Ramp.Make("7a5a26", 4, 0.1f)) { Gloss = 0.3f };
        static readonly PixelMaterial Coat = new PixelMaterial(Ramp.Make("2f4043", 5, 0.1f, 0.35f, 1.5f)) { Ambient = 0.24f, Rim = 0.55f, Texture = ClothNoise };
        static readonly PixelMaterial Lining = new PixelMaterial(Ramp.Make("6a2232", 4, 0.1f, 0.35f, 1.4f)) { Ambient = 0.22f, Rim = 0.6f };
        static readonly PixelMaterial Trousers = new PixelMaterial(Ramp.Make("262c36", 4, 0.08f)) { Ambient = 0.22f };
        static readonly PixelMaterial Leather = new PixelMaterial(Ramp.Make("5c3b25", 4, 0.1f)) { Ambient = 0.25f, Gloss = 0.15f };
        static readonly PixelMaterial Rope = new PixelMaterial(Ramp.Make("8a7448", 4, 0.08f)) { Ambient = 0.3f };
        static readonly PixelMaterial Iron = new PixelMaterial(Ramp.Make("6f7b82", 5, 0.08f, 0.32f, 1.38f)) { Gloss = 0.45f, Ambient = 0.3f, Rim = 0.45f, Dither = 0f, Texture = RustNoise };
        static readonly PixelMaterial Hose = new PixelMaterial(Ramp.Make("3d3326", 4, 0.06f)) { Ambient = 0.25f };
        static readonly PixelMaterial GlassRing = PixelMaterial.Glow("1d6b5a");
        static readonly PixelMaterial GlassMid = PixelMaterial.Glow("3fd6a4");
        static readonly PixelMaterial GlassCore = PixelMaterial.Glow("c6ffe9");
        static readonly PixelMaterial Smear = new PixelMaterial(new[] { new Color32(228, 248, 238, 240) }) { Emissive = true, Outline = false, ReceivesContactShadow = false };
        static readonly PixelMaterial SmearMid = new PixelMaterial(new[] { new Color32(150, 214, 196, 200) }) { Emissive = true, Outline = false, ReceivesContactShadow = false };
        static readonly PixelMaterial SmearSoft = new PixelMaterial(new[] { new Color32(70, 140, 128, 140) }) { Emissive = true, Outline = false, ReceivesContactShadow = false };
        static readonly PixelMaterial Laudanum = PixelMaterial.Glow("ff9b4a");
        static readonly PixelMaterial Sign = PixelMaterial.Glow("8affd8");

        static float ClothNoise(int x, int y) => (PixelCanvas.Hash(x / 2, y / 3, 41) - 0.5f) * 0.08f;
        static float RustNoise(int x, int y) => PixelCanvas.ValueNoise(x / 3f, y / 3f, 0, 77) > 0.72f ? -0.22f : 0f;

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

        // ------------------------------------------------------------------
        // Dibujo del personaje a partir de una pose
        // ------------------------------------------------------------------

        struct SmearSpec
        {
            public bool On;
            public float From, To, Strength;
        }

        static SmearSpec Arc(Pose p, float from, float to, float strength) => new SmearSpec { On = true, From = from, To = to, Strength = strength };

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
            c.Poly(capePoints.ToArray(), Lining, 0f, 2.5f, -0.05f, gCape, -0.35f, 0f);

            // --- Manguera de la escafandra (detrás del cuerpo) ---
            var hoseStart = Add(helmet, Rotate(V(-8f, -1f), V(0f, 0f), -lean));
            var hoseEnd = Add(hip, V(-6f, 3f));
            var hoseCtrl = Add(Mix(hoseStart, hoseEnd, 0.5f), V(-9f - p["hose"] * 5f, p["hose"] * 2f));
            c.Strand(Bezier(hoseStart, hoseCtrl, hoseEnd, 7), 2.1f, 1.7f, Hose, 0.5f, 0f, c.NewGroup());

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
            // Cinturón con cuerdas
            c.Capsule(Add(waist, Dir(torsoAngle + 90f, 6.5f)), Add(waist, Dir(torsoAngle - 90f, 6.5f)), 1.6f, 1.6f, Leather, 4.8f, 0f, gTorso);
            c.Capsule(Add(waist, V(-2f, -2f)), Add(waist, V(3f, -6f)), 0.9f, 0.9f, Rope, 4.9f, 0f, gTorso);

            // --- Pierna delantera ---
            DrawLeg(c, hip, kneeF, ankleF, 6f, 0f, gLegF);

            // --- Escafandra ---
            c.Ellipse(Add(neck, V(0.5f, 1.5f)), 8.5f, 3.6f, -lean * 0.5f, BrassDark, 7f, 0f, gHelmet);
            c.Ellipse(helmet, 9.4f, 10f, -lean * 0.3f, Brass, 8f, 0f, gHelmet);
            var valve = Add(helmet, Rotate(V(-1f, 10f), V(0f, 0f), -lean * 0.8f - p["head"]));
            c.Capsule(valve, Add(valve, Rotate(V(0f, 3f), V(0f, 0f), -lean)), 1.8f, 1.6f, BrassDark, 8.1f, 0f, gHelmet);
            var port = Add(helmet, Rotate(V(4.6f, 0.3f), V(0f, 0f), -lean * 0.8f - p["head"]));
            c.Ellipse(port, 5.2f, 5.6f, 0f, BrassDark, 8.2f, 0f, gHelmet);
            c.Ellipse(port, 3.8f, 4.2f, 0f, GlassRing, 8.3f, 0f, gHelmet);
            c.Ellipse(Add(port, V(0.4f, 0.2f)), 2.6f, 3f, 0f, GlassMid, 8.4f, 0f, gHelmet);
            c.Ellipse(Add(port, V(0.9f, 0.9f)), 1.1f, 1.3f, 0f, GlassCore, 8.5f, 0f, gHelmet);
            foreach (var bolt in new[] { V(-5f, 5f), V(-6f, -3f), V(1f, -7.5f) })
            {
                var b = Add(helmet, Rotate(bolt, V(0f, 0f), -lean * 0.8f - p["head"]));
                c.Ellipse(b, 1.1f, 1.1f, 0f, BrassDark, 8.15f, 0f, gHelmet);
            }

            // --- Brazo delantero y el Garfio ---
            if (smear.On)
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

        void DrawLeg(ShadedCanvas c, Vector2 hip, Vector2 knee, Vector2 ankle, float z, float shade, int group)
        {
            c.Capsule(hip, knee, 3.8f, 3.1f, Trousers, z, shade, group);
            c.Capsule(knee, Add(ankle, V(0f, 2f)), 3.2f, 2.8f, Leather, z + 0.1f, shade, group);
            // Bota apuntando hacia delante
            var toe = Add(ankle, V(6.5f, -1.6f));
            c.Poly(new[] { Add(ankle, V(-3f, 2.5f)), Add(ankle, V(2.5f, 2.5f)), Add(toe, V(0f, 1.2f)), Add(toe, V(0.5f, -1f)), Add(ankle, V(-3.2f, -2.5f)) },
                   Leather, z + 0.2f, 1.5f, shade, group);
        }

        void DrawArm(ShadedCanvas c, Vector2 shoulder, Vector2 elbow, Vector2 hand, float z, float shade, int group)
        {
            c.Capsule(shoulder, elbow, 3.3f, 2.7f, Coat, z, shade, group);
            c.Capsule(elbow, hand, 2.7f, 2.3f, Coat, z + 0.05f, shade, group);
            c.Ellipse(hand, 2.6f, 2.4f, 0f, Leather, z + 0.1f, shade, group);
        }

        /// <summary>El Garfio: empuñadura, guarda de latón y una hoja larga rematada en una uña de ancla.</summary>
        void DrawSword(ShadedCanvas c, Vector2 hand, float angle, float z, int group)
        {
            var axis = Dir(angle);
            var side = Dir(angle + 90f);
            Vector2 At(float along, float across) => Add(hand, Add(Scale(axis, along), Scale(side, across)));

            c.Capsule(At(-5f, 0f), At(3f, 0f), 1.5f, 1.5f, Leather, z, 0f, group);
            c.Ellipse(At(-5.5f, 0f), 1.9f, 1.9f, 0f, BrassDark, z + 0.05f, 0f, group);
            c.Poly(new[] { At(3f, -5f), At(5.5f, -5f), At(5.5f, 5f), At(3f, 5f) }, Brass, z + 0.1f, 1.2f, 0f, group);
            // Hoja: ancha, con lomo recto y filo curvado, termina en punta.
            c.Poly(new[]
            {
                At(5.5f, -2.6f), At(20f, -3.3f), At(34f, -3.6f), At(41f, -2.4f), At(46f, 0.6f),
                At(40f, 2.6f), At(30f, 3f), At(5.5f, 2.6f),
            }, Iron, z + 0.05f, 1.6f, 0f, group);
            // Uña de ancla hacia atrás desde el lomo.
            c.Poly(new[] { At(31f, 2.6f), At(27f, 7.5f), At(24f, 9.5f), At(29.5f, 9f), At(36f, 2.8f) }, Iron, z + 0.06f, 1.2f, -0.05f, group);
            // Acanaladura central más oscura.
            c.Capsule(At(8f, 0f), At(30f, 0f), 0.6f, 0.6f, Iron, z + 0.07f, -0.25f, group);
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
