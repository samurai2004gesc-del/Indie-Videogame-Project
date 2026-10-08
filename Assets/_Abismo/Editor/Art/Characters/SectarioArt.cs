using System.Collections.Generic;
using UnityEngine;
using static Abismo.EditorTools.Rig;

namespace Abismo.EditorTools
{
    /// <summary>
    /// Sectario de la Orden Esotérica de Dagón: túnica larga violeta con capirote puntiagudo, máscara de pez dorada,
    /// esclavina con ribetes de oro, cíngulo de cuerda con medalla y un báculo de madera de deriva coronado por una
    /// garra de oro que sujeta una brasa naranja-púrpura (el ÚNICO acento de su paleta). Mira a la derecha.
    ///
    /// La túnica y la esclavina son "campanas" (<see cref="Bell"/>): una superficie con volumen calculada por píxel
    /// con pliegues, ribete en el bajo y cortes en el borde, que se inclina con el cuerpo (también tumbado al morir).
    ///
    /// Convenciones de las poses (grados):
    ///  - brazos: 0 = colgando, 90 = hacia delante, 180 = arriba, -90 = hacia atrás (Rig.Limb)
    ///  - "staff": ángulo matemático del báculo (90 = vertical); "grip": distancia de la mano al pie del báculo
    ///  - "lean": inclinación del tronco hacia delante; "head": cabeceo (positivo = mirar abajo)
    /// </summary>
    public sealed class SectarioArt : CharacterArt
    {
        public override string Id => "sectario";
        public override int FrameWidth => 128;
        public override int FrameHeight => 128;
        public override Vector2 Pivot => new Vector2(64f, 8f);

        // ------------------------------------------------------------------
        // Materiales
        // ------------------------------------------------------------------

        static readonly PixelMaterial Robe = new PixelMaterial(Ramp.Make("342a42", 5, 0.1f, 0.3f, 1.8f)) { Ambient = 0.2f, Rim = 0.7f, Dither = 0.04f };
        static readonly PixelMaterial HoodInner = new PixelMaterial(new[] { PixelCanvas.Hex("0d0912"), PixelCanvas.Hex("181020") }) { Ambient = 0.45f, Rim = 0f, Dither = 0f };
        static readonly PixelMaterial Trim = new PixelMaterial(Ramp.Make("85632a", 4, 0.1f, 0.35f, 1.5f)) { Gloss = 0.45f, Ambient = 0.26f, Rim = 0.45f, Dither = 0f };
        static readonly PixelMaterial MaskGold = new PixelMaterial(Ramp.Make("b48b3a", 5, 0.12f, 0.3f, 1.55f)) { Gloss = 0.75f, Ambient = 0.3f, Rim = 0.5f, Dither = 0f };
        static readonly PixelMaterial MaskDark = new PixelMaterial(Ramp.Make("4a3115", 3, 0.08f)) { Ambient = 0.45f, Dither = 0f };
        static readonly PixelMaterial Skin = new PixelMaterial(Ramp.Make("7b8b77", 4, 0.1f)) { Ambient = 0.3f, Rim = 0.5f, Dither = 0f };
        static readonly PixelMaterial Wood = new PixelMaterial(Ramp.Make("4e382a", 4, 0.08f)) { Ambient = 0.26f, Rim = 0.6f, Gloss = 0.12f, Dither = 0f };
        static readonly PixelMaterial Rope = new PixelMaterial(Ramp.Make("6b5639", 4, 0.08f)) { Ambient = 0.3f, Dither = 0f };
        static readonly PixelMaterial DeadGlass = new PixelMaterial(Ramp.Make("3b3445", 4, 0.06f)) { Gloss = 0.7f, Ambient = 0.3f, Rim = 0.5f, Dither = 0f };

        // Brasa del báculo: de fuera hacia el núcleo.
        static readonly PixelMaterial[] OrbLive = { PixelMaterial.Glow("d9402e"), PixelMaterial.Glow("ff8a36"), PixelMaterial.Glow("ffc56e"), PixelMaterial.Glow("fff4d8") };
        static readonly PixelMaterial[] OrbDim = { PixelMaterial.Glow("6c1f3c"), PixelMaterial.Glow("a6382e"), PixelMaterial.Glow("d4663a"), PixelMaterial.Glow("eea05e") };
        // Halo púrpura translúcido: siempre al fondo (z muy baja) para no "agujerear" otras piezas.
        static readonly PixelMaterial CoronaIn = PixelMaterial.Glow("9a36b4aa");
        static readonly PixelMaterial CoronaOut = PixelMaterial.Glow("5a1c7c60");
        static readonly PixelMaterial EmberHot = PixelMaterial.Glow("ffd27a");
        static readonly PixelMaterial EmberMid = PixelMaterial.Glow("ff7a2c");
        static readonly PixelMaterial EmberCool = PixelMaterial.Glow("b0389a");
        static readonly PixelMaterial EyeGlow = PixelMaterial.Glow("ff9a46");
        static readonly PixelMaterial EyeDim = PixelMaterial.Glow("8a3a24");
        static readonly PixelMaterial FlashCore = PixelMaterial.Glow("fff6e0");
        static readonly PixelMaterial FlashMid = PixelMaterial.Glow("ffa040");
        static readonly PixelMaterial SmearCore = PixelMaterial.Glow("ffe2a0");
        static readonly PixelMaterial SmearMid = PixelMaterial.Glow("ff7e34");
        static readonly PixelMaterial SmearSoft = PixelMaterial.Glow("8a2fa8a0");
        static readonly PixelMaterial Smoke = PixelMaterial.Glow("5e4a6e90");
        static readonly PixelMaterial SmokeLight = PixelMaterial.Glow("8a6e9670");

        // ------------------------------------------------------------------
        // Poses base
        // ------------------------------------------------------------------

        static Pose Idle => new Pose().With(
            ("lean", 4f), ("head", 0f), ("crouch", 0f),
            ("armF1", 30f), ("armF2", 75f), ("staff", 88f), ("grip", 27f), ("plant", 1f),
            ("armB1", -6f), ("armB2", 12f),
            ("orb", 1f), ("fFx", 5f), ("fBx", -1.5f));

        public override List<AnimSpec> Animations() => new List<AnimSpec>
        {
            new AnimSpec("idle", 6, 7f, true, f => DrawIdle(f)),
            new AnimSpec("walk", 8, 10f, true, f => DrawWalk(f)),
            new AnimSpec("windup", 8, 10f, false, f => DrawWindup(f)),
            new AnimSpec("cast", 4, 12f, false, f => DrawCast(f)),
            new AnimSpec("hurt", 3, 12f, false, f => DrawHurt(f)),
            new AnimSpec("stagger", 6, 8f, true, f => DrawStagger(f)),
            new AnimSpec("death", 10, 12f, false, f => DrawDeath(f)),
        };

        // ------------------------------------------------------------------
        // Animaciones
        // ------------------------------------------------------------------

        ShadedCanvas DrawIdle(int f)
        {
            float t = f / 6f * Mathf.PI * 2f;
            var p = Idle.With(("crouch", 0.5f - 0.5f * Mathf.Cos(t)), ("lean", 4f + 0.8f * Mathf.Sin(t)), ("head", 1.5f * Mathf.Sin(t - 0.6f)),
                              ("hood", 0.7f * Mathf.Sin(t - 1.2f)), ("sway", 0.08f * Mathf.Sin(t + 0.5f)), ("rope", 0.6f * Mathf.Sin(t - 1f)),
                              ("fold", 0.25f * Mathf.Sin(t)), ("armB2", 12f + 3f * Mathf.Sin(t - 0.8f)),
                              ("orb", 1f + 0.07f * Mathf.Sin(t * 2f)));
            return Draw(p, new Fx { Embers = 1, Phase = f / 6f });
        }

        static readonly float[,] WalkFeet =
        {
            // pie cercano x, alto; pie lejano x, alto; agacharse
            { 6f, 0f, -6f, 0f, 1.2f },
            { 3f, 0f, -3.5f, 1.5f, 0.6f },
            { 0f, 0f, 1f, 2.6f, -0.2f },
            { -3f, 0f, 4.5f, 1.2f, 0.4f },
            { -6f, 0f, 6f, 0f, 1.2f },
            { -3.5f, 1.5f, 3f, 0f, 0.6f },
            { 1f, 2.6f, 0f, 0f, -0.2f },
            { 4.5f, 1.2f, -3f, 0f, 0.4f },
        };

        ShadedCanvas DrawWalk(int f)
        {
            float t = f / 8f * Mathf.PI * 2f;
            float s = Mathf.Sin(t);
            var p = Idle.With(
                ("fFx", WalkFeet[f, 0]), ("fFy", WalkFeet[f, 1]), ("fBx", WalkFeet[f, 2]), ("fBy", WalkFeet[f, 3]), ("crouch", WalkFeet[f, 4]),
                ("lean", 8f), ("head", 2f + Mathf.Sin(t * 2f) * 1.5f), ("plant", 0f), ("grip", 23f), ("staff", 88f + 3f * s),
                ("armF1", 32f + 5f * s), ("armF2", 76f + 4f * s), ("armB1", -8f - 10f * s), ("armB2", 8f - 8f * s),
                ("sway", 0.55f + 0.12f * Mathf.Sin(t * 2f + 1f)), ("hood", -0.6f + 0.5f * Mathf.Sin(t * 2f - 1.4f)),
                ("rope", 0.8f * Mathf.Sin(t * 2f - 1.2f) - 0.4f), ("fold", t * 0.5f), ("orb", 1f + 0.06f * Mathf.Sin(t * 3f)));
            return Draw(p, new Fx { Embers = 1, Phase = f / 8f, Drift = 1f });
        }

        // Alza el báculo: anticipación (se encoge), sube y la brasa crece fotograma a fotograma.
        ShadedCanvas DrawWindup(int f)
        {
            var raised = Idle.With(("crouch", -0.5f), ("lean", -8f), ("head", -12f), ("armF1", 148f), ("armF2", 162f), ("staff", 100f), ("grip", 25f), ("plant", 0f),
                                   ("armB1", -135f), ("armB2", -160f), ("palm", 1f), ("orb", 1.8f), ("sway", 0.3f), ("hood", 0.8f), ("rope", 0.6f));
            var keys = new Keyframes()
                .Key(0, Idle.With(("crouch", 1.5f), ("lean", 10f), ("head", 6f), ("armF1", 40f), ("armF2", 70f), ("staff", 80f), ("grip", 24f), ("plant", 0f),
                                  ("armB1", 10f), ("armB2", 30f), ("orb", 1.05f), ("sway", -0.1f), ("hood", -0.4f)))
                .Key(1, Idle.With(("crouch", 3f), ("lean", 15f), ("head", 10f), ("armF1", 28f), ("armF2", 52f), ("staff", 72f), ("grip", 22f), ("plant", 0f),
                                  ("armB1", 20f), ("armB2", 50f), ("orb", 1.15f), ("sway", -0.2f), ("hood", -0.8f)), Ease.Out)
                .Key(3, raised.With(("orb", 1.45f), ("hood", 1.6f), ("sway", 0.45f), ("armB1", -110f), ("armB2", -140f)), Ease.Out)
                .Key(5, raised)
                .Key(7, raised.With(("orb", 2.15f), ("lean", -9f), ("head", -13f), ("hood", 0.5f)));
            var p = keys.Evaluate(f);
            if (f >= 5) p["x"] = (f % 2 == 0 ? 0.5f : -0.5f); // tiembla con el poder contenido
            return Draw(p, new Fx { Embers = 2, Phase = f / 7f, EyeBright = f >= 3 });
        }

        // Lanza: barrido con estela, destello en la punta y continuación.
        ShadedCanvas DrawCast(int f)
        {
            var keys = new Keyframes()
                .Key(0, Idle.With(("armF1", 118f), ("armF2", 108f), ("staff", 62f), ("grip", 30f), ("plant", 0f), ("lean", 8f), ("crouch", 1f), ("head", 2f),
                                  ("armB1", -70f), ("armB2", -50f), ("orb", 2f), ("hood", 1.8f), ("sway", 0.6f), ("rope", 1f)))
                .Key(1, Idle.With(("armF1", 98f), ("armF2", 90f), ("staff", 32f), ("grip", 32f), ("plant", 0f), ("lean", 16f), ("crouch", 2.5f), ("head", 8f),
                                  ("armB1", -55f), ("armB2", -25f), ("orb", 0.6f), ("hood", 0.6f), ("sway", 0.85f), ("rope", 1.4f), ("fFx", 8f)), Ease.Out)
                .Key(2, Idle.With(("armF1", 92f), ("armF2", 82f), ("staff", 24f), ("grip", 32f), ("plant", 0f), ("lean", 18f), ("crouch", 3f), ("head", 10f),
                                  ("armB1", -45f), ("armB2", -20f), ("orb", 0.55f), ("hood", -0.6f), ("sway", 0.7f), ("rope", 0.6f), ("fFx", 8f)))
                .Key(3, Idle.With(("armF1", 62f), ("armF2", 72f), ("staff", 52f), ("grip", 28f), ("plant", 0f), ("lean", 10f), ("crouch", 1.5f), ("head", 4f),
                                  ("armB1", -20f), ("armB2", 0f), ("orb", 0.8f), ("hood", -0.8f), ("sway", 0.35f), ("rope", -0.4f), ("fFx", 6f)));
            var p = keys.Evaluate(f);
            var fx = new Fx { Flash = f == 1 ? 1 : f == 2 ? 2 : 0, Smear = f == 0, EyeBright = f <= 1 };
            return Draw(p, fx);
        }

        ShadedCanvas DrawHurt(int f)
        {
            var hit = Idle.With(("x", -2f), ("lean", -14f), ("head", -14f), ("crouch", 1f), ("armF1", 8f), ("armF2", 40f), ("staff", 104f), ("grip", 24f), ("plant", 0f),
                                ("armB1", -50f), ("armB2", -30f), ("sway", -0.6f), ("hood", 2.4f), ("rope", -1.4f), ("orb", 0.8f), ("dim", 1f));
            var keys = new Keyframes().Key(0, hit).Key(1, Pose.Lerp(hit, Idle, 0.45f).With(("plant", 0f), ("dim", 0f)), Ease.Out).Key(2, Pose.Lerp(hit, Idle, 0.8f).With(("plant", 0f)));
            var p = keys.Evaluate(f);
            return Draw(p, new Fx { Embers = 3, Phase = f / 3f });
        }

        // Aturdido: encorvado, apoyado en el báculo, la cabeza le cuelga y la brasa chisporrotea.
        ShadedCanvas DrawStagger(int f)
        {
            float t = f / 6f * Mathf.PI * 2f;
            var p = Idle.With(("x", 1.2f * Mathf.Sin(t)), ("lean", 20f + 3f * Mathf.Sin(t + 0.5f)), ("crouch", 4f + 0.6f * Mathf.Cos(t * 2f)),
                              ("head", 18f + 9f * Mathf.Sin(t - 0.8f)), ("armF1", 52f), ("armF2", 110f), ("staff", 100f), ("plant", 1f),
                              ("armB1", 6f + 8f * Mathf.Sin(t - 1f)), ("armB2", 4f + 10f * Mathf.Sin(t - 1.6f)),
                              ("hood", 1.6f * Mathf.Sin(t - 1.6f)), ("sway", 0.2f * Mathf.Sin(t)), ("rope", 0.9f * Mathf.Sin(t - 1.4f)),
                              ("orb", 0.75f + 0.08f * Mathf.Sin(t * 3f)), ("dim", f % 3 == 1 ? 0f : 1f), ("fFx", 6f), ("fBx", -3f));
            return Draw(p, new Fx { Embers = 3, Phase = f / 6f });
        }

        ShadedCanvas DrawDeath(int f)
        {
            var hit = Idle.With(("x", -2f), ("lean", -18f), ("head", -18f), ("crouch", 1f), ("armF1", -20f), ("armF2", 20f), ("staff", 122f), ("grip", 26f), ("plant", 0f),
                                ("armB1", -60f), ("armB2", -40f), ("sway", -0.7f), ("hood", 2.6f), ("rope", -1.6f), ("orb", 1.35f));
            var buckle = hit.With(("x", -2f), ("lean", 6f), ("head", 14f), ("crouch", 7f), ("armF1", 10f), ("armF2", 20f), ("armB1", -10f), ("armB2", 0f),
                                  ("sway", 0.2f), ("hood", -1.5f), ("rope", 1f), ("fFx", -2f), ("fBx", -7f));
            var kneel = buckle.With(("lean", 18f), ("head", 26f), ("crouch", 13f), ("armF1", 18f), ("armF2", 8f), ("armB1", 4f), ("armB2", 4f),
                                    ("sway", 0f), ("hood", 0.6f), ("rope", 0f));
            var topple = kneel.With(("lean", 55f), ("head", 20f), ("crouch", 14f), ("armF1", 80f), ("armF2", 60f), ("lie", 0.45f), ("hood", 1.4f));
            var lying = kneel.With(("lean", 86f), ("head", 22f), ("crouch", 15f), ("armF1", 96f), ("armF2", 92f), ("armB1", 60f), ("armB2", 80f),
                                   ("lie", 1f), ("hood", 0f), ("rope", 0f));
            var keys = new Keyframes()
                .Key(0, hit)
                .Key(2, buckle, Ease.In)
                .Key(4, kneel)
                .Key(5, kneel.With(("lean", 24f), ("head", 30f)))
                .Key(6, topple, Ease.In)
                .Key(7, lying.With(("crouch", 15.5f)), Ease.Out)
                .Key(9, lying);
            var p = keys.Evaluate(f);

            // El báculo sale despedido hacia atrás y cae detrás de él; la brasa se apaga.
            if (f >= 2)
            {
                float[] ang = { 0f, 0f, 146f, 166f, 175f, 177f, 177f, 177f, 177f, 177f };
                float[] bx = { 0f, 0f, 8f, 9f, 10f, 10f, 10f, 10f, 10f, 10f };
                float[] by = { 0f, 0f, 16f, 6f, 1.6f, 2.4f, 1.6f, 1.6f, 1.6f, 1.6f };
                p["free"] = 1f;
                p["staff"] = ang[f];
                p["sbx"] = bx[f];
                p["sby"] = by[f];
            }
            p["orb"] = f <= 1 ? 1.35f - f * 0.3f : f <= 3 ? 0.8f - (f - 2) * 0.2f : 0f;
            p["dim"] = f >= 1 ? 1f : 0f;
            p["dead"] = f >= 4 ? 1f : 0f;
            return Draw(p, new Fx { Smoke = f >= 4 && f <= 8 ? f - 3 : 0, EyeOff = f >= 6 });
        }

        // ------------------------------------------------------------------
        // Dibujo del personaje a partir de una pose
        // ------------------------------------------------------------------

        struct Fx
        {
            public int Embers;       // 1 suben, 2 remolino hacia la brasa, 3 chisporroteo
            public float Phase;
            public float Drift;
            public bool Smear;
            public int Flash;        // 1 destello, 2 onda que se disipa
            public int Smoke;        // 1..5 humo de la brasa apagada
            public bool EyeBright, EyeOff;
        }

        const float UpperArm = 9.5f, Forearm = 8.5f, StaffLen = 62f;

        ShadedCanvas Draw(Pose p, Fx fx)
        {
            var c = NewFrame();
            float lean = p["lean"], ta = 90f - lean, crouch = p["crouch"], lie = p["lie"];
            float sway = p["sway"];

            // --- Esqueleto ---
            var hip = V(p["x"], 24f - crouch);
            var chest = Add(hip, Dir(ta, 12f));
            var neck = Add(hip, Dir(ta, 16.5f));
            float hr = -(lean * 0.75f + p["head"]);
            var head = Add(neck, Rotate(V(1.2f, 6.2f), V(0f, 0f), hr));
            var shoulderF = Add(Add(chest, Dir(ta, 2f)), Dir(ta - 90f, 2.5f));
            var shoulderB = Add(Add(chest, Dir(ta, 2f)), Dir(ta + 90f, 2.5f));
            var elbowF = Limb(shoulderF, p["armF1"], UpperArm);
            var wristF = Limb(elbowF, p["armF2"], Forearm);
            var elbowB = Limb(shoulderB, p["armB1"], UpperArm);
            var wristB = Limb(elbowB, p["armB2"], Forearm);
            var handF = Add(wristF, Scale(Norm(Sub(wristF, elbowF)), 2f));
            var handB = Add(wristB, Scale(Norm(Sub(wristB, elbowB)), 2f));

            int gRobe = c.NewGroup(), gMantle = c.NewGroup(), gHood = c.NewGroup(), gMask = c.NewGroup();
            int gArmF = c.NewGroup(), gArmB = c.NewGroup(), gStaff = c.NewGroup(), gRope = c.NewGroup();

            // --- Pies descalzos (asoman bajo el bajo de la túnica) ---
            var footF = V(hip.x + p["fFx"], p["fFy"]);
            var footB = V(hip.x + p["fBx"], p["fBy"]);
            if (lie < 0.3f)
            {
                DrawFoot(c, footB, 0.5f, -0.14f, c.NewGroup());
                DrawFoot(c, footF, 3.5f, 0f, c.NewGroup());
            }

            // --- Túnica ---
            float standHemF = 9f + crouch * 0.25f + Mathf.Max(0f, -sway) * 3f;
            float standHemB = 12.5f + sway * 3.5f + crouch * 0.3f;
            var hemCenter = Mix(V(hip.x - 1f, -1.5f), V(hip.x - 16f, 2.5f), lie);
            var robe = new Bell(chest, hemCenter, Mathf.Lerp(5.6f, 5f, lie), Mathf.Lerp(6.6f, 6f, lie),
                                Mathf.Lerp(standHemF, 4f, lie), Mathf.Lerp(standHemB, 6.5f, lie))
            {
                Folds = 4f, FoldAmp = 0.45f, FoldPhase = p["fold"], Ground = 0f, Teeth = 7f, TeethDepth = 1.3f, Seed = 5,
                BackLift = Mathf.Max(0f, sway) * 2.2f,
            };
            float lead = Mathf.Max(p["fFx"], p["fBx"]);
            float leadLift = p["fFx"] >= p["fBx"] ? p["fFy"] : p["fBy"];
            if (lie < 0.3f)
            {
                robe.LiftX = hip.x + lead + 2.5f;
                robe.LiftAmt = 1.2f + leadLift * 0.9f;
                robe.LiftW = 4.5f;
            }
            AddBell(c, robe, Robe, 4f, gRobe);
            c.Custom(robe.MinX, robe.MinY, robe.MaxX, robe.MaxY, robe.HemBand(1.6f, 3.1f), Trim, 4.05f, 0f, gRobe);
            float tBelt = robe.TAt(Add(hip, Dir(ta, 1f)));
            c.Custom(robe.MinX, robe.MinY, robe.MaxX, robe.MaxY, robe.Stripe(0.74f, 0.75f, tBelt), Trim, 4.04f, 0f, gRobe);

            // --- Cíngulo de cuerda con medalla ---
            var belt = Add(hip, Dir(ta, 1f));
            float bt = robe.TAt(belt);
            var beltBack = robe.Point(bt, -robe.BackAt(bt) + 0.5f);
            var beltFront = robe.Point(bt, robe.FrontAt(bt) - 0.3f);
            c.Capsule(beltBack, beltFront, 1.1f, 1.1f, Rope, 4.5f, 0f, gRope);
            var knot = robe.Point(bt, robe.FrontAt(bt) * 0.55f);
            float rs = p["rope"];
            var ropeEnd = Add(knot, V(0.8f - rs * 2.2f + lean * 0.05f, -11f + Mathf.Abs(rs) * 0.6f));
            if (lie > 0.5f) ropeEnd = Add(knot, V(2f, -3f));
            var ropeCtrl = Add(Mix(knot, ropeEnd, 0.5f), V(1.5f - rs * 0.8f, 0f));
            c.Ellipse(knot, 1.6f, 1.4f, 0f, Rope, 4.6f, 0f, gRope);
            c.Strand(Bezier(knot, ropeCtrl, ropeEnd, 6), 0.8f, 0.7f, Rope, 4.55f, 0f, gRope);
            var ropeEnd2 = Add(knot, V(-0.6f - rs * 1.6f, -7f));
            if (lie > 0.5f) ropeEnd2 = Add(knot, V(-2f, -2f));
            c.Strand(Bezier(knot, Add(Mix(knot, ropeEnd2, 0.5f), V(-0.8f, 0f)), ropeEnd2, 5), 0.75f, 0.65f, Rope, 4.52f, -0.06f, gRope);
            // Medalla de Dagón colgando del cabo largo.
            c.Ellipse(Add(ropeEnd, V(0f, -1.6f)), 1.9f, 2.1f, 0f, MaskGold, 4.7f, 0f, c.NewGroup());
            c.Decal(ropeEnd.x, ropeEnd.y - 1.8f, PixelCanvas.Hex("3a2410"));

            // --- Brazo trasero (lejos de la luz) ---
            float zArmB = 1f;
            DrawArm(c, shoulderB, elbowB, wristB, handB, zArmB, -0.12f, gArmB, p["palm"] > 0.5f);

            // --- Esclavina (capa corta sobre los hombros) ---
            float mantleDir = ta + 180f + (270f - (ta + 180f)) * 0.35f;
            var mantleTop = Add(neck, Dir(ta, 0.5f));
            var mantle = new Bell(mantleTop, Add(mantleTop, Dir(mantleDir, 10.5f)), 3.6f, 4.6f, 8.6f, 9.6f + Mathf.Max(0f, sway) * 1.5f)
            {
                Folds = 3f, FoldAmp = 0.3f, FoldPhase = p["fold"] * 0.6f + 1f, Teeth = 5f, TeethDepth = 1.7f, Seed = 2, Flare = 0.45f,
            };
            if (lie > 0.5f) mantle.Ground = 0f;
            AddBell(c, mantle, Robe, 6f, gMantle);
            c.Custom(mantle.MinX, mantle.MinY, mantle.MaxX, mantle.MaxY, mantle.HemBand(1.1f, 2.2f), Trim, 6.05f, 0f, gMantle);

            // --- Capucha puntiaguda con máscara de pez ---
            DrawHood(c, head, hr, p["hood"], gHood, gMask, fx);

            // --- Báculo ---
            float sa = p["staff"];
            var axis = Dir(sa);
            Vector2 sBase;
            if (p["free"] > 0.5f) sBase = V(p["sbx"], p["sby"]);
            else if (p["plant"] > 0.5f)
            {
                float sn = Mathf.Max(0.2f, axis.y);
                sBase = Sub(handF, Scale(axis, handF.y / sn));
            }
            else sBase = Sub(handF, Scale(axis, p["grip"]));
            var orbCenter = Add(sBase, Scale(axis, StaffLen + 6.5f));
            float orbSize = p["orb"];
            int orbState = p["dead"] > 0.5f ? 2 : p["dim"] > 0.5f ? 1 : 0;

            if (fx.Smear) DrawSmear(c, shoulderF, orbCenter);
            DrawStaff(c, sBase, sa, 10f, gStaff, orbSize, orbState);

            // --- Brazo delantero ---
            DrawArm(c, shoulderF, elbowF, wristF, handF, 5.5f, 0f, gArmF, false, 9.5f, p["free"] < 0.5f);

            // --- Efectos ---
            if (fx.Embers > 0 && orbState < 2) DrawEmbers(c, orbCenter, orbSize, fx);
            if (fx.Flash > 0) DrawFlash(c, orbCenter, fx.Flash);
            if (fx.Smoke > 0) DrawSmoke(c, orbCenter, fx.Smoke);
            return c;
        }

        static Vector2 Norm(Vector2 v)
        {
            float l = Mathf.Sqrt(v.x * v.x + v.y * v.y);
            return l > 0.0001f ? V(v.x / l, v.y / l) : V(1f, 0f);
        }

        static void AddBell(ShadedCanvas c, Bell b, PixelMaterial m, float z, int group)
        {
            c.Custom(b.MinX, b.MinY, b.MaxX, b.MaxY, b.Body(), m, z, 0f, group);
        }

        void DrawFoot(ShadedCanvas c, Vector2 heel, float z, float shade, int group)
        {
            c.Capsule(Add(heel, V(0.2f, 1.8f)), Add(heel, V(0.6f, 7.5f)), 1.6f, 1.9f, Skin, z - 0.05f, shade - 0.08f, group);
            c.Poly(new[] { Add(heel, V(-1.7f, 2.8f)), Add(heel, V(2.2f, 3f)), Add(heel, V(5.4f, 1.4f)), Add(heel, V(6.2f, 0.2f)), Add(heel, V(-1.9f, 0f)) },
                   Skin, z, 1.2f, shade, group);
        }

        /// <summary>Brazo con manga acampanada y ribete dorado; la mano agarra el báculo o se abre (palma palmeada).</summary>
        void DrawArm(ShadedCanvas c, Vector2 shoulder, Vector2 elbow, Vector2 wrist, Vector2 hand, float z, float shade, int group,
                     bool palm, float zSleeve = -1f, bool grips = false)
        {
            if (zSleeve < 0f) zSleeve = z + 0.5f;
            c.Capsule(shoulder, elbow, 2.9f, 2.6f, Robe, z, shade, group);
            var d = Norm(Sub(wrist, elbow));
            var s = V(-d.y, d.x);
            var open = Add(wrist, Scale(d, 1.4f));
            var c1 = Add(open, Scale(s, 4.5f));
            var c2 = Add(open, Scale(s, -4.5f));
            // La manga cuelga: el borde más bajo cae un poco más.
            if (c1.y < c2.y) c1 = Add(c1, V(-d.x * 1f, -2.6f)); else c2 = Add(c2, V(-d.x * 1f, -2.6f));
            var e1 = Add(Add(elbow, Scale(s, 2.7f)), Scale(d, -1f));
            var e2 = Add(Add(elbow, Scale(s, -2.7f)), Scale(d, -1f));
            var belly = c1.y < c2.y ? Add(Mix(e1, c1, 0.55f), V(0f, -1.2f)) : Add(Mix(e2, c2, 0.55f), V(0f, -1.2f));
            if (c1.y < c2.y) c.Poly(new[] { e1, belly, c1, c2, e2 }, Robe, zSleeve, 2f, shade, group, 0.1f, 0f);
            else c.Poly(new[] { e1, c1, c2, belly, e2 }, Robe, zSleeve, 2f, shade, group, 0.1f, 0f);
            // Interior oscuro de la manga y ribete.
            c.Capsule(Add(Mix(c1, c2, 0.5f), Scale(d, -0.4f)), Add(Mix(c1, c2, 0.5f), Scale(d, -0.4f)), 2.2f, 2.2f, HoodInner, zSleeve + 0.02f, 0f, group);
            c.Capsule(c1, c2, 0.85f, 0.85f, Trim, zSleeve + 0.05f, shade, group);
            float zHand = grips ? 10.6f : zSleeve + 0.1f;
            c.Ellipse(hand, 2.1f, 1.9f, 0f, Skin, zHand, shade, group);
            if (palm)
            {
                // Mano abierta con dedos largos y membranas.
                var up = d;
                for (int i = -1; i <= 1; i++)
                {
                    var dir = Norm(Add(up, Scale(s, i * 0.55f)));
                    c.Capsule(Add(hand, Scale(dir, 1.2f)), Add(hand, Scale(dir, 4.6f - Mathf.Abs(i) * 0.6f)), 0.65f, 0.45f, Skin, zHand + 0.05f, shade, group);
                }
                c.Poly(new[] { Add(hand, Scale(Norm(Add(up, Scale(s, -0.55f))), 3.4f)), Add(hand, Scale(up, 3.6f)), Add(hand, Scale(Norm(Add(up, Scale(s, 0.55f))), 3.4f)), hand },
                       Skin, zHand + 0.02f, 0.5f, shade - 0.12f, group);
            }
        }

        void DrawHood(ShadedCanvas c, Vector2 head, float hr, float lag, int gHood, int gMask, Fx fx)
        {
            Vector2 H(float x, float y) => Add(head, Rotate(V(x, y), V(0f, 0f), hr));
            var shell = new[]
            {
                H(-6.4f, -7.6f), H(-8.6f, -3f), H(-8.8f, 1.5f), H(-7.6f, 6.5f), H(-5.6f - lag * 0.5f, 12f),
                H(-3.6f - lag * 1.6f, 18.6f - Mathf.Abs(lag) * 0.3f), H(-1.7f - lag * 0.6f, 13.6f), H(1.8f, 9f), H(5.2f, 6.6f),
                H(7.3f, 5.5f), H(6.5f, 3.8f), H(6.7f, 0f), H(6.2f, -4.5f), H(4.6f, -7f), H(1.5f, -8.6f),
            };
            c.Poly(shell, Robe, 9f, 2.5f, 0f, gHood, -0.32f, 0.12f);
            // Cara delantera del capirote: otro plano más girado hacia la luz (se lee el volumen del cono).
            var front = new[]
            {
                H(-3.4f - lag * 1.5f, 17.2f), H(-1.7f - lag * 0.6f, 13.6f), H(1.8f, 9f), H(5.2f, 6.6f), H(7.3f, 5.5f), H(6.5f, 3.8f),
                H(6.7f, 0f), H(6.2f, -4.5f), H(4.6f, -7f), H(1.5f, -8.6f), H(-1.8f, -7.8f), H(-1.9f, 2f), H(-2.3f, 8.5f),
            };
            c.Poly(front, Robe, 9.01f, 1.8f, 0f, gHood, 0.22f, 0.18f);
            // Hueco de la capucha (sombra profunda) y la máscara dorada asomando.
            c.Ellipse(H(2.8f, -0.8f), 4f, 5.6f, hr, HoodInner, 9.05f, 0f, gHood);
            c.Ellipse(H(5f, -1f), 3.1f, 4.4f, hr - 6f, MaskGold, 9.1f, 0f, gMask);
            // Labios de pez gruesos y salientes.
            c.Capsule(H(6.9f, -3f), H(9.4f, -3.3f), 1.25f, 1.1f, MaskGold, 9.12f, 0f, gMask);
            c.Capsule(H(6.6f, -4.9f), H(9f, -4.8f), 1.1f, 1f, MaskGold, 9.11f, -0.12f, gMask);
            c.Capsule(H(7.4f, -4.1f), H(9.9f, -4.1f), 0.5f, 0.5f, MaskDark, 9.15f, 0f, gMask);
            // Ojo saltón: aro de oro, cuenca oscura y una brasa dentro.
            c.Ellipse(H(5.8f, 0.9f), 2.3f, 2.3f, 0f, MaskGold, 9.13f, 0.12f, gMask);
            c.Ellipse(H(5.9f, 0.9f), 1.5f, 1.5f, 0f, MaskDark, 9.14f, 0f, gMask);
            if (!fx.EyeOff) c.Ellipse(H(6.2f, 1f), fx.EyeBright ? 0.95f : 0.75f, fx.EyeBright ? 0.95f : 0.75f, 0f, fx.EyeBright ? EyeGlow : EyeDim, 9.16f, 0f, gMask);
            // Branquias grabadas.
            c.Capsule(H(2.7f, -3.6f), H(3f, -0.2f), 0.42f, 0.42f, MaskDark, 9.13f, 0f, gMask);
            c.Capsule(H(3.7f, -4.2f), H(3.9f, -1.6f), 0.42f, 0.42f, MaskDark, 9.13f, 0f, gMask);
            // Visera de la capucha sobre la máscara.
            c.Poly(new[] { H(-0.2f, 6.1f), H(7.6f, 5.7f), H(7f, 3.6f), H(1.8f, 4f) }, Robe, 9.3f, 1.4f, 0f, gHood, 0.15f, 0.35f);
        }

        /// <summary>Báculo de madera de deriva con anillas y una garra de oro que sujeta la brasa.</summary>
        void DrawStaff(ShadedCanvas c, Vector2 bottom, float angle, float z, int group, float orbSize, int orbState)
        {
            var ax = Dir(angle);
            var side = Dir(angle + 90f);
            Vector2 At(float along, float across) => Add(bottom, Add(Scale(ax, along), Scale(side, across)));

            c.Capsule(At(0f, 0f), At(StaffLen, 0f), 1.15f, 1.2f, Wood, z, 0f, group);
            c.Ellipse(At(StaffLen * 0.36f, 0.4f), 1.6f, 1.4f, angle, Wood, z + 0.01f, 0f, group);
            c.Ellipse(At(StaffLen * 0.62f, -0.4f), 1.5f, 1.4f, angle, Wood, z + 0.01f, 0f, group);
            c.Capsule(At(0f, 0f), At(2.5f, 0f), 1.45f, 1.35f, Trim, z + 0.02f, 0f, group);
            c.Capsule(At(StaffLen - 7f, 0f), At(StaffLen - 5.6f, 0f), 1.7f, 1.7f, Trim, z + 0.02f, 0f, group);
            c.Capsule(At(StaffLen - 3.6f, 0f), At(StaffLen - 2.2f, 0f), 1.7f, 1.7f, Trim, z + 0.02f, 0f, group);
            c.Ellipse(At(StaffLen, 0f), 2.4f, 1.6f, angle - 90f, Trim, z + 0.03f, 0f, group);

            // Garra: dos púas que abrazan la brasa (una por detrás, otra por delante) y una espina central.
            float os = Mathf.Max(1f, orbSize);
            float spread = 4.6f + (os - 1f) * 2.6f, reach = 11.5f + (os - 1f) * 3f;
            c.Strand(Bezier(At(StaffLen, -1f), At(StaffLen + 4f, -spread - 0.8f), At(StaffLen + reach, -2.2f), 6), 1f, 0.5f, Trim, z - 0.3f, 0f, group);
            c.Strand(Bezier(At(StaffLen, 1f), At(StaffLen + 4f, spread + 0.8f), At(StaffLen + reach, 2.2f), 6), 1f, 0.5f, MaskGold, z + 0.3f, 0f, group);
            c.Capsule(At(StaffLen, 0f), At(StaffLen + reach + 2f, 0f), 0.8f, 0.4f, Trim, z - 0.35f, 0f, group);

            var o = At(StaffLen + 6.5f, 0f);
            if (orbState == 2)
            {
                c.Ellipse(o, 2.8f, 2.8f, 0f, DeadGlass, z + 0.1f, 0f, group);
                return;
            }
            if (orbSize <= 0.05f) return;
            var mats = orbState == 1 ? OrbDim : OrbLive;
            float r = 3.1f * orbSize;
            if (orbState == 0)
            {
                c.Ellipse(o, r * 2.1f + 1f, r * 2.1f + 1f, 0f, CoronaOut, -3f);
                c.Ellipse(o, r * 1.55f + 0.5f, r * 1.55f + 0.5f, 0f, CoronaIn, -2.9f);
            }
            c.Ellipse(o, r, r, 0f, mats[0], z + 0.1f);
            c.Ellipse(Add(o, V(0.3f, 0.3f)), r * 0.74f, r * 0.74f, 0f, mats[1], z + 0.11f);
            c.Ellipse(Add(o, V(0.5f, 0.5f)), r * 0.46f, r * 0.46f, 0f, mats[2], z + 0.12f);
            c.Ellipse(Add(o, V(0.6f, 0.6f)), Mathf.Max(0.6f, r * 0.2f), Mathf.Max(0.6f, r * 0.2f), 0f, mats[3], z + 0.13f);
        }

        void DrawEmbers(ShadedCanvas c, Vector2 orb, float size, Fx fx)
        {
            if (fx.Embers == 1)
            {
                for (int i = 0; i < 3; i++)
                {
                    float ph = Frac(fx.Phase + i / 3f);
                    var pos = Add(orb, V(Mathf.Sin(ph * 6.28f + i * 2.1f) * 2.2f - fx.Drift * ph * 7f, 3.5f * size + ph * 14f));
                    var mat = ph < 0.34f ? EmberHot : ph < 0.67f ? EmberMid : EmberCool;
                    c.Capsule(pos, Add(pos, V(fx.Drift * 0.8f, -1.3f)), 0.6f, 0.45f, mat, 11.5f);
                }
            }
            else if (fx.Embers == 2)
            {
                // Remolino de chispas que la brasa absorbe.
                for (int i = 0; i < 6; i++)
                {
                    float a = i * 60f + fx.Phase * 160f;
                    float rad = Mathf.Lerp(20f, 7f, fx.Phase) + (i % 2) * 3f;
                    var pos = Add(orb, Dir(a, rad));
                    var toward = Dir(a + 180f - 35f, 2.2f);
                    c.Capsule(pos, Add(pos, toward), 0.6f, 0.45f, i % 3 == 0 ? EmberHot : i % 3 == 1 ? EmberMid : EmberCool, 11.5f);
                }
            }
            else if (fx.Embers == 3)
            {
                for (int i = 0; i < 2; i++)
                {
                    float ph = Frac(fx.Phase + i * 0.5f);
                    var pos = Add(orb, V((i == 0 ? 2f : -2.5f) + ph * (i == 0 ? 3f : -2f), -2f - ph * 9f + ph * ph * -3f));
                    c.Capsule(pos, Add(pos, V(0f, 1.2f)), 0.55f, 0.45f, ph < 0.5f ? EmberMid : EmberCool, 11.5f);
                }
            }
        }

        void DrawSmear(ShadedCanvas c, Vector2 shoulder, Vector2 orb)
        {
            float dx = orb.x - shoulder.x, dy = orb.y - shoulder.y;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            float to = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            float from = to + 62f;
            float outer = dist + 4f, rr = outer + 1f;
            c.Custom(shoulder.x - rr, shoulder.y - rr, shoulder.x + rr, shoulder.y + rr, SmearArc(shoulder, dist - 7f, outer, from, to), SmearSoft, -2.5f);
            c.Custom(shoulder.x - rr, shoulder.y - rr, shoulder.x + rr, shoulder.y + rr, SmearArc(shoulder, dist - 2f, outer - 1f, from - 8f, to), SmearMid, 11f);
            c.Custom(shoulder.x - rr, shoulder.y - rr, shoulder.x + rr, shoulder.y + rr, SmearArc(shoulder, dist + 1f, outer - 1.5f, from - 18f, to), SmearCore, 11.1f);
        }

        void DrawFlash(ShadedCanvas c, Vector2 at, int stage)
        {
            if (stage == 1)
            {
                c.Ellipse(at, 11f, 11f, 0f, CoronaOut, -3f);
                c.Ellipse(at, 7.5f, 7.5f, 0f, CoronaIn, -2.9f);
                c.Custom(at.x - 9f, at.y - 9f, at.x + 9f, at.y + 9f, Ring(at, 6.2f, 7.4f, 0f), FlashMid, 12f);
                c.Ellipse(at, 4.2f, 4.2f, 0f, FlashMid, 12.1f);
                c.Ellipse(at, 2.8f, 2.8f, 0f, FlashCore, 12.2f);
                c.Capsule(Add(at, V(-12f, 0f)), Add(at, V(13f, 0f)), 0.75f, 0.75f, FlashCore, 12.3f);
                c.Capsule(Add(at, V(0f, -9f)), Add(at, V(0f, 10f)), 0.7f, 0.7f, FlashCore, 12.3f);
                c.Capsule(Add(at, V(-5f, -5f)), Add(at, V(5f, 5f)), 0.6f, 0.6f, FlashMid, 12.25f);
                c.Capsule(Add(at, V(-5f, 5f)), Add(at, V(5f, -5f)), 0.6f, 0.6f, FlashMid, 12.25f);
            }
            else
            {
                c.Custom(at.x - 13f, at.y - 13f, at.x + 13f, at.y + 13f, Ring(at, 10f, 11.2f, 0.35f), CoronaIn, -2.8f);
                c.Ellipse(at, 2f, 2f, 0f, FlashMid, 12.1f);
                for (int i = 0; i < 5; i++)
                {
                    var d = Dir(20f + i * 72f, 8f + (i % 2) * 2f);
                    var pos = Add(at, d);
                    c.Capsule(pos, Add(pos, Scale(Norm(d), -1.6f)), 0.6f, 0.45f, i % 2 == 0 ? EmberMid : EmberCool, 11.5f);
                }
            }
        }

        void DrawSmoke(ShadedCanvas c, Vector2 orb, int stage)
        {
            float k = stage;
            for (int i = 0; i < 3; i++)
            {
                var pos = Add(orb, V(Mathf.Sin(k * 0.9f + i * 2f) * 2.5f + i * 1.5f - 1.5f, 3f + k * 3.2f + i * 3.5f));
                float r = 1.6f + k * 0.35f + (i == 1 ? 0.6f : 0f);
                if (stage >= 4 && i == 0) continue;
                c.Ellipse(pos, r, r * 0.85f, 0f, i == 1 ? SmokeLight : Smoke, -3f + i * 0.01f);
            }
            if (stage <= 2) c.Ellipse(Add(orb, V(0.3f, 0.6f)), 0.8f, 0.8f, 0f, EmberMid, 10.3f);
        }

        static System.Func<float, float, (bool, N3)> Ring(Vector2 center, float r0, float r1, float gaps)
        {
            return (px, py) =>
            {
                float dx = px - center.x, dy = py - center.y;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d < r0 || d > r1) return (false, N3.Front);
                if (gaps > 0f)
                {
                    float a = Mathf.Atan2(dy, dx);
                    if (Mathf.Sin(a * 5f + 0.7f) < -1f + gaps * 2f) return (false, N3.Front);
                }
                return (true, N3.Front);
            };
        }

        static float Frac(float v) => v - Mathf.Floor(v);

        // ------------------------------------------------------------------
        // Campana de tela (túnica, esclavina): superficie con volumen, pliegues, ribete y bajo dentado.
        // ------------------------------------------------------------------

        sealed class Bell
        {
            public readonly float Tx, Ty, Ax, Ay, Px, Py, Len, TopF, TopB, HemF, HemB;
            public float Folds = 4f, FoldAmp = 0.4f, FoldPhase, Flare = 0.3f;
            public float Ground = -999f, Teeth = 6f, TeethDepth = 1f;
            public int Seed;
            public float LiftX, LiftAmt, LiftW = 4f, BackLift;
            public readonly float MinX, MinY, MaxX, MaxY;

            public Bell(Vector2 top, Vector2 hem, float topF, float topB, float hemF, float hemB)
            {
                Tx = top.x;
                Ty = top.y;
                float dx = hem.x - top.x, dy = hem.y - top.y;
                Len = Mathf.Max(1f, Mathf.Sqrt(dx * dx + dy * dy));
                Ax = dx / Len;
                Ay = dy / Len;
                // "Delante" = eje girado 90° antihorario (con el eje hacia abajo, apunta a la derecha).
                Px = -Ay;
                Py = Ax;
                TopF = topF;
                TopB = topB;
                HemF = hemF;
                HemB = hemB;
                var a = Point(0f, topF); var b = Point(0f, -topB); var cc = Point(1f, hemF); var d = Point(1f, -hemB);
                MinX = Mathf.Min(Mathf.Min(a.x, b.x), Mathf.Min(cc.x, d.x)) - 2f;
                MaxX = Mathf.Max(Mathf.Max(a.x, b.x), Mathf.Max(cc.x, d.x)) + 2f;
                MinY = Mathf.Min(Mathf.Min(a.y, b.y), Mathf.Min(cc.y, d.y)) - 2f;
                MaxY = Mathf.Max(Mathf.Max(a.y, b.y), Mathf.Max(cc.y, d.y)) + 2f;
            }

            static float Profile(float t) => 0.3f * t + 0.7f * t * t;
            public float FrontAt(float t) => TopF + (HemF - TopF) * Profile(Mathf.Clamp01(t));
            public float BackAt(float t) => TopB + (HemB - TopB) * Profile(Mathf.Clamp01(t));
            public Vector2 Point(float t, float r) => new Vector2(Tx + Ax * Len * t + Px * r, Ty + Ay * Len * t + Py * r);
            public float TAt(Vector2 q) => ((q.x - Tx) * Ax + (q.y - Ty) * Ay) / Len;

            float Tooth(float u)
            {
                float k = u * Teeth + Seed * 0.37f;
                int i = Mathf.FloorToInt(k);
                float f = k - i;
                return TeethDepth * Mathf.Abs(f * 2f - 1f) * (0.6f + 0.8f * PixelCanvas.Hash(i, Seed, 11));
            }

            bool Inside(float px, float py, out float t, out float u, out float bottom, out float r, out float band)
            {
                float dx = px - Tx, dy = py - Ty;
                float s = dx * Ax + dy * Ay;
                r = dx * Px + dy * Py;
                t = s / Len;
                u = 0f;
                bottom = 0f;
                band = 0f;
                if (t < 0f) return false;
                float front = FrontAt(t), back = -BackAt(t);
                if (r > front || r < back) return false;
                u = (r - back) / Mathf.Max(0.001f, front - back);
                float tooth = Tooth(u);
                float dAxis = Len - s - tooth;
                if (dAxis < 0f) return false;
                bottom = dAxis;
                band = Len - s;
                if (Ground > -900f)
                {
                    float lift = 0f;
                    if (LiftAmt > 0f) lift += LiftAmt * Mathf.Max(0f, 1f - Mathf.Abs(px - LiftX) / LiftW);
                    if (BackLift > 0f) lift += BackLift * Mathf.Max(0f, 1f - u / 0.35f);
                    float g = py - Ground - lift;
                    if (g - tooth < 0f) return false;
                    bottom = Mathf.Min(bottom, g - tooth);
                    band = Mathf.Min(band, g);
                }
                return true;
            }

            N3 Normal(float t, float u, float bottom)
            {
                float uc = u * 2f - 1f;
                float nx = uc * 0.95f + FoldAmp * (0.25f + 0.75f * t) * Mathf.Sin(uc * Folds * Mathf.PI + FoldPhase);
                float nz = Mathf.Sqrt(Mathf.Max(0.08f, 1f - uc * uc));
                float ny = Flare;
                if (bottom < 1.6f) ny -= (1f - bottom / 1.6f) * 0.45f;
                return new N3(nx * Px - ny * Ax, nx * Py - ny * Ay, nz).Normalized();
            }

            public System.Func<float, float, (bool, N3)> Body() => (px, py) =>
            {
                if (!Inside(px, py, out float t, out float u, out float b, out _, out _)) return (false, N3.Front);
                return (true, Normal(t, u, b));
            };

            public System.Func<float, float, (bool, N3)> HemBand(float from, float to) => (px, py) =>
            {
                if (!Inside(px, py, out float t, out float u, out float b, out _, out float band) || band > to || band < from) return (false, N3.Front);
                return (true, Normal(t, u, b));
            };

            public System.Func<float, float, (bool, N3)> Stripe(float uAt, float halfWidth, float t0) => (px, py) =>
            {
                if (!Inside(px, py, out float t, out float u, out float b, out float r, out _) || t < t0) return (false, N3.Front);
                float front = FrontAt(t), back = -BackAt(t);
                float rs = back + (front - back) * uAt;
                if (Mathf.Abs(r - rs) > halfWidth) return (false, N3.Front);
                return (true, Normal(t, u, b));
            };
        }
    }
}
