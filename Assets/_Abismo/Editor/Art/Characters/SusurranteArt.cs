using System.Collections.Generic;
using UnityEngine;
using static Abismo.EditorTools.Rig;

namespace Abismo.EditorTools
{
    /// <summary>
    /// El Que Susurra en la Oscuridad (segundo jefe, final de la demo, en el Arrecife). Un sacerdote-dios con cabeza de
    /// pulpo bajo una capucha alta y puntiaguda de lino gris con nervaduras, cuatro ojos rojos que arden (capa emisiva),
    /// una barba de tentáculos que le cae hasta las rodillas, túnica gris azulada con ribetes y bordados rojos (ojos y
    /// llamas de tentáculo), un manto rojo morado a la espalda y seis tentáculos rojo morados que le brotan de los hombros
    /// y se agitan con ondas que viajan de la raíz a la punta. No tiene pies: repta sobre tentáculos que asoman bajo el
    /// bajo deshilachado. Empuña un tridente de madera de deriva y bronce con una gema-ojo roja (de ahí salen las esferas).
    /// Mira a la derecha. Mismo comportamiento y nombres de animación que <see cref="ArcipresteArt"/> (BossArchpriest).
    ///
    /// Claves de pose (lo que no se define vale 0):
    ///  - "x","y": desplazamiento; "agacha": baja la cadera; "inclina": tronco (+ = hacia delante); "cabeza": cabeceo (+ = abajo)
    ///  - "brazoD1","brazoD2","brazoT1","brazoT2": brazo delantero/trasero (0 = colgando, 90 = delante, 180 = arriba);
    ///    "mano": 0 = mano trasera suelta, 2 = abierta hacia el suelo (conjuro)
    ///  - "baculo": ángulo del tridente (90 = vertical); "agarre": distancia de la mano al regatón; "apoya": 1 = en el suelo
    ///  - "onda": fase de los tentáculos de la espalda; "abre": apertura; "alza": + alzados, − caídos; "enrosca": recogidos;
    ///    "viento": arrastrados hacia atrás; "clava": se hunden en el suelo (conjuro); "extra": los dos de la segunda fase
    ///  - "barba": fase de la barba; "grita": la barba se abre y enseña el pico; "tela": fase de la ropa; "vaiven": amplitud
    ///  - "pies": fase de los tentáculos-pie; "repta": cuánto reptan
    ///  - "ojos": 0 = cerrados, 1 = abiertos, 2+ = arden; "gema": tamaño de la gema; "cae": se derrumba; "hunde": se hunde en la tinta
    /// </summary>
    public sealed class SusurranteArt : CharacterArt
    {
        public override string Id => "susurrante";
        public override int FrameWidth => 288;
        public override int FrameHeight => 256;
        public override Vector2 Pivot => new Vector2(144f, 8f);

        /// <summary>
        /// De dónde salen las esferas, en unidades (32 px) desde el pivote y mirando a la derecha: el centro de la gema del
        /// tridente en los fotogramas de disparo de "orbs" (6, 7 y 8, en los que el brazo y el tridente no se mueven).
        /// Medido con <see cref="GemaEnEsferas"/>.
        /// </summary>
        public static Vector2 CastPoint => new Vector2(2.86f, 5.26f);

        const float CaderaY = 62f;
        const float CuelloX = 8f, CuelloY = 56f;
        const float LargoBaculo = 170f;

        // ------------------------------------------------------------------
        // Materiales
        // ------------------------------------------------------------------

        // Túnica gris azulada de lana pesada (trama fina en relieve) y el forro oscuro que asoma por los jirones.
        static readonly PixelMaterial Tunica = new PixelMaterial(Ramp.Make("363d50", 5, 0.1f, 0.28f, 1.8f))
        {
            Rim = 0.65f, Ambient = 0.2f, Dither = 0.02f, BandDither = 0.14f, Bump = Patterns.Weave(2f, 0.3f), BumpStrength = 0.4f,
        };
        static readonly PixelMaterial Forro = new PixelMaterial(Ramp.Make("262232", 4, 0.06f, 0.4f, 1.5f)) { Rim = 0.2f, Ambient = 0.25f, Dither = 0f };
        // Capucha y esclavina: lino gris con nervaduras; dentro de la capucha, sombra honda.
        static readonly PixelMaterial Capucha = new PixelMaterial(Ramp.Make("4f5560", 5, 0.1f, 0.3f, 1.65f)) { Rim = 0.75f, Ambient = 0.2f, Dither = 0.02f, BandDither = 0.12f };
        static readonly PixelMaterial ForroCapucha = new PixelMaterial(Ramp.Make("1c1826", 3, 0.05f, 0.55f, 1.5f)) { Rim = 0f, Ambient = 0.35f, Dither = 0f, Outline = false };
        static readonly PixelMaterial Manto = new PixelMaterial(Ramp.Make("4a1d36", 5, 0.08f, 0.28f, 1.7f)) { Rim = 0.8f, Ambient = 0.2f, Dither = 0.02f, BandDither = 0.12f };
        static readonly PixelMaterial Ribete = new PixelMaterial(Ramp.Make("821a26", 5, 0.06f, 0.3f, 1.55f)) { Rim = 0.6f, Ambient = 0.24f, Dither = 0f };
        static readonly PixelMaterial RibeteOscuro = new PixelMaterial(Ramp.Make("6a1a28", 4, 0.06f, 0.36f, 1.55f)) { Rim = 0.5f, Ambient = 0.24f, Dither = 0f };
        static readonly Color32 Salitre = PixelCanvas.Hex("8c9088");
        // Piel de pulpo gris verdosa, húmeda (brillo especular) y con motas.
        static readonly PixelMaterial Piel = new PixelMaterial(Ramp.Make("5d6c6a", 5, 0.12f, 0.3f, 1.62f))
        {
            Rim = 0.75f, Ambient = 0.22f, Dither = 0f, BandDither = 0.12f, SpecularAt = 0.9f, SpecularPower = 30f, Specular = PixelCanvas.Hex("e6f2ea"),
        };
        static readonly PixelMaterial PielOscura = new PixelMaterial(Ramp.Make("3a4646", 4, 0.1f, 0.38f, 1.5f)) { Rim = 0.5f, Ambient = 0.26f, Dither = 0f };
        static readonly PixelMaterial Barba = new PixelMaterial(Ramp.Make("64726e", 5, 0.12f, 0.26f, 1.8f))
        {
            Rim = 0.9f, Ambient = 0.22f, Dither = 0f, SpecularAt = 0.88f, SpecularPower = 26f, Specular = PixelCanvas.Hex("eef8f0"),
        };
        static readonly PixelMaterial BarbaClara = new PixelMaterial(Ramp.Make("b3b09e", 4, 0.08f, 0.45f, 1.3f)) { Rim = 0.4f, Ambient = 0.32f, Dither = 0f };
        // Tentáculos rojo morados (espalda y pies) con ventosas rosadas.
        static readonly PixelMaterial Carne = new PixelMaterial(Ramp.Make("5e1f3a", 5, 0.08f, 0.26f, 1.62f))
        {
            Rim = 0.6f, Ambient = 0.2f, Dither = 0f, BandDither = 0.1f, SpecularAt = 0.88f, SpecularPower = 26f, Specular = PixelCanvas.Hex("ffd8e0"),
        };
        static readonly PixelMaterial Ventosa = new PixelMaterial(Ramp.Make("a87282", 4, 0.06f, 0.4f, 1.32f)) { Rim = 0.4f, Ambient = 0.32f, Dither = 0f };
        static readonly PixelMaterial Pico = new PixelMaterial(Ramp.Make("332a2a", 4, 0.06f, 0.4f, 1.9f))
        {
            Gloss = 0.6f, Rim = 0.5f, Ambient = 0.25f, Dither = 0f, SpecularAt = 0.8f, Specular = PixelCanvas.Hex("e8dcd0"),
        };
        static readonly PixelMaterial Boca = new PixelMaterial(Ramp.Make("4a1222", 3, 0.05f, 0.45f, 1.4f)) { Ambient = 0.5f, Rim = 0f, Dither = 0f, Outline = false };
        static readonly PixelMaterial Hueso = new PixelMaterial(Ramp.Make("c2b696", 4, 0.08f, 0.42f, 1.28f)) { Rim = 0.5f, Ambient = 0.3f, Dither = 0f };
        static readonly PixelMaterial Bronce = new PixelMaterial(Ramp.Make("8a6a36", 5, 0.12f, 0.3f, 1.6f))
        {
            Gloss = 0.6f, Rim = 0.5f, Ambient = 0.25f, Dither = 0f, SpecularAt = 0.85f, Specular = PixelCanvas.Hex("fff0c8"),
        };
        static readonly PixelMaterial BronceOscuro = new PixelMaterial(Ramp.Make("5a4424", 4, 0.1f, 0.4f, 1.5f)) { Gloss = 0.4f, Rim = 0.4f, Ambient = 0.28f, Dither = 0f };
        static readonly Color32 Verdin = PixelCanvas.Hex("3f7a68");
        static readonly PixelMaterial Madera = new PixelMaterial(Ramp.Make("40322c", 4, 0.08f, 0.4f, 1.6f)) { Rim = 0.5f, Ambient = 0.24f, Dither = 0f };
        static readonly PixelMaterial Cuerda = new PixelMaterial(Ramp.Make("6e5e46", 4, 0.08f, 0.4f, 1.4f)) { Rim = 0.4f, Ambient = 0.28f, Dither = 0f, Detail = HombrePez.Torcida };
        static readonly PixelMaterial Tinta = new PixelMaterial(Ramp.Make("2a1d36", 4, 0.06f, 0.42f, 1.9f))
        {
            Gloss = 0.5f, Rim = 0.5f, Ambient = 0.22f, Dither = 0f, SpecularAt = 0.7f, Specular = PixelCanvas.Hex("c9b8f0"),
        };
        static readonly Color32 ColorTinta = PixelCanvas.Hex("221830");
        static readonly PixelMaterial Espuma = new PixelMaterial(Ramp.Make("b8ccc8", 3, 0.06f, 0.6f, 1.2f)) { Rim = 0.3f, Ambient = 0.45f, Dither = 0f };
        static readonly PixelMaterial GemaApagada = new PixelMaterial(Ramp.Make("5a1a2a", 4, 0.06f, 0.4f, 1.6f))
        {
            Gloss = 0.6f, Ambient = 0.3f, Dither = 0f, SpecularAt = 0.8f, Specular = PixelCanvas.Hex("ffd8e0"),
        };
        // Emisivos: ojos, gema, runas (no reciben luz; con el bloom de URP brillan).
        static readonly PixelMaterial OjoRojo = PixelMaterial.Glow("d0222e");
        static readonly PixelMaterial OjoVivo = PixelMaterial.Glow("ff5a40");
        static readonly PixelMaterial OjoBlanco = PixelMaterial.Glow("ffe8cc");
        static readonly PixelMaterial Pupila = PixelMaterial.Glow("1a0408");
        static readonly PixelMaterial OjoMuerto = new PixelMaterial(Ramp.Make("4a2a2e", 3, 0.05f)) { Ambient = 0.4f, Dither = 0f };
        static readonly PixelMaterial GemaFuera = PixelMaterial.Glow("a0103e");
        static readonly PixelMaterial GemaMedia = PixelMaterial.Glow("ff2e58");
        static readonly PixelMaterial GemaNucleo = PixelMaterial.Glow("ffe2ea");
        static readonly PixelMaterial GemaHalo = PixelMaterial.Glow("8a0a3450");
        static readonly PixelMaterial Runa = PixelMaterial.Glow("ff4a52");
        static readonly PixelMaterial RunaTenue = PixelMaterial.Glow("a8203a");
        static readonly PixelMaterial Chispa = PixelMaterial.Glow("ffb8a8");
        static readonly PixelMaterial Estela = PixelMaterial.Glow("ff5a7a80");
        static readonly PixelMaterial Polvo = PixelMaterial.Glow("58525e90");
        static readonly PixelMaterial Aura = PixelMaterial.Glow("5a082a48");
        // Máscara: lo que queda bajo el suelo no se pinta (tentáculos que se hunden, el cuerpo que se disuelve).
        static readonly PixelMaterial Invisible = PixelMaterial.Glow("00000000");

        // ------------------------------------------------------------------
        // Poses y animaciones
        // ------------------------------------------------------------------

        static Pose Base => new Pose().With(
            ("x", 0f), ("y", 0f), ("agacha", 0f), ("inclina", 6f), ("cabeza", 6f),
            ("brazoD1", 40f), ("brazoD2", 90f), ("brazoT1", -8f), ("brazoT2", 14f), ("mano", 0f),
            ("baculo", 92f), ("agarre", 80f), ("apoya", 1f),
            ("onda", 0f), ("abre", 1f), ("alza", 0f), ("enrosca", 0f), ("viento", 0f), ("clava", 0f), ("extra", 0f),
            ("barba", 0f), ("grita", 0f), ("tela", 0f), ("vaiven", 1f), ("pies", 0f), ("repta", 0f),
            ("ojos", 1f), ("gema", 1f), ("cae", 0f), ("hunde", 0f));

        public override List<AnimSpec> Animations() => new List<AnimSpec>
        {
            new AnimSpec("idle", 8, 8f, true, Reposo),
            new AnimSpec("walk", 8, 8f, true, Andar),
            new AnimSpec("intro", 12, 7f, false, Despertar),
            new AnimSpec("tentacles", 8, 10f, true, Conjuro),
            new AnimSpec("orbs", 9, 10f, false, Esferas),
            new AnimSpec("charge_windup", 6, 7f, false, PreparaEmbestida),
            new AnimSpec("charge", 4, 12f, true, Embestida),
            new AnimSpec("recover", 6, 5f, false, Recupera),
            new AnimSpec("phase", 10, 6f, false, CambioDeFase),
            new AnimSpec("death", 16, 10f, false, Muerte),
        };

        // Respira: los tentáculos de la espalda ondulan sin parar y las runas del susurro le rondan la cabeza.
        ShadedCanvas Reposo(int f)
        {
            float t = f / 8f * Mathf.PI * 2f;
            var p = Base.With(("agacha", 1.2f * (1f - Mathf.Cos(t))), ("inclina", 6f + 1.2f * Mathf.Sin(t - 0.4f)), ("cabeza", 6f + 2f * Mathf.Sin(t - 1f)),
                              ("brazoT2", 14f + 4f * Mathf.Sin(t - 0.9f)), ("onda", t), ("barba", t), ("tela", t), ("pies", t),
                              ("ojos", 1f + 0.12f * Mathf.Sin(t)), ("gema", 1f + 0.12f * Mathf.Sin(t * 2f)));
            return Dibujar(p, new Efectos { Fase = f / 8f });
        }

        // Repta: los tentáculos-pie se estiran y recogen por turnos; el tridente acompaña al paso.
        ShadedCanvas Andar(int f)
        {
            float t = f / 8f * Mathf.PI * 2f, s = Mathf.Sin(t);
            var p = Base.With(("agacha", 1.5f + 1.5f * Mathf.Cos(t * 2f)), ("inclina", 10f + 1.5f * Mathf.Sin(t * 2f)), ("cabeza", 8f + 2f * Mathf.Sin(t * 2f - 1f)),
                              ("apoya", 0f), ("baculo", 88f + 5f * s), ("agarre", 80f), ("brazoD1", 44f + 8f * s), ("brazoD2", 92f + 4f * s),
                              ("brazoT1", -6f - 12f * s), ("brazoT2", 14f - 8f * s),
                              ("onda", t * 2f), ("barba", t * 2f), ("tela", t * 2f - 0.8f), ("vaiven", 1.4f), ("pies", t), ("repta", 1f));
            return Dibujar(p, new Efectos { Fase = f / 8f });
        }

        // Despierta: dormido y encogido, abre los ojos uno a uno, se alza desplegando los tentáculos y clava el tridente gritando.
        ShadedCanvas Despertar(int f)
        {
            var dormido = Base.With(("agacha", 12f), ("inclina", 28f), ("cabeza", 28f), ("ojos", 0f), ("abre", 0.25f), ("alza", -0.9f), ("enrosca", 0.85f),
                                    ("brazoD1", 18f), ("brazoD2", 50f), ("brazoT1", 6f), ("brazoT2", 24f), ("gema", 0.3f), ("vaiven", 0.4f));
            var despierta = dormido.With(("agacha", 9f), ("inclina", 20f), ("cabeza", 12f), ("ojos", 0.8f), ("abre", 0.55f), ("alza", -0.45f), ("enrosca", 0.5f),
                                         ("gema", 0.7f), ("vaiven", 0.8f));
            var alza = Base.With(("agacha", -2f), ("inclina", -6f), ("cabeza", -14f), ("ojos", 1.6f), ("abre", 1.6f), ("alza", 0.7f),
                                 ("brazoD1", 120f), ("brazoD2", 140f), ("apoya", 0f), ("baculo", 94f), ("agarre", 120f),
                                 ("brazoT1", -130f), ("brazoT2", -150f), ("gema", 1.5f), ("grita", 0.5f), ("y", 4f), ("vaiven", 2f));
            var golpe = Base.With(("agacha", 5f), ("inclina", 14f), ("cabeza", -6f), ("ojos", 2.3f), ("abre", 1.4f), ("alza", 0.35f),
                                  ("brazoD1", 52f), ("brazoD2", 86f), ("apoya", 1f), ("brazoT1", -90f), ("brazoT2", -110f),
                                  ("gema", 1.8f), ("grita", 1f), ("vaiven", 2f));
            var keys = new Keyframes()
                .Key(0, dormido)
                .Key(2, dormido.With(("agacha", 12.5f), ("ojos", 0f)))
                .Key(5, despierta)
                .Key(8, alza, Ease.Out)
                .Key(9, golpe, Ease.In)
                .Key(11, golpe.With(("grita", 0.6f), ("ojos", 1.9f), ("inclina", 10f)));
            var p = keys.Evaluate(f);
            float t = f / 12f * Mathf.PI * 4f;
            p["onda"] = t;
            p["barba"] = t;
            p["tela"] = t * 0.5f;
            p["pies"] = t * 0.5f;
            if (f >= 9 && f <= 10) p["x"] = f % 2 == 0 ? 1.2f : -1.2f;
            return Dibujar(p, new Efectos { Fase = f / 12f, Polvo = f == 9 ? 2 : f == 10 ? 1 : 0, Runas = f >= 7 ? 1f : f >= 5 ? 0.3f : 0f });
        }

        // Conjuro: tridente clavado, la mano trasera abierta hacia el suelo; tres tentáculos se hunden en la tierra y laten.
        ShadedCanvas Conjuro(int f)
        {
            float t = f / 8f * Mathf.PI * 2f;
            var p = Base.With(("agacha", 4f + Mathf.Sin(t)), ("inclina", 12f), ("cabeza", 12f + 3f * Mathf.Sin(t * 2f)), ("ojos", 1.8f + 0.2f * Mathf.Sin(t * 2f)),
                              ("brazoD1", 36f), ("brazoD2", 80f), ("apoya", 1f), ("brazoT1", -62f + 3f * Mathf.Sin(t * 2f)), ("brazoT2", -44f + 4f * Mathf.Sin(t * 2f + 1f)),
                              ("mano", 2f), ("onda", t * 2f), ("clava", 1f), ("abre", 1.25f), ("alza", 0.5f), ("barba", t * 2f), ("tela", t), ("pies", t),
                              ("vaiven", 1.6f), ("gema", 1.3f + 0.2f * Mathf.Sin(t * 2f)), ("grita", 0.25f + 0.1f * Mathf.Sin(t * 2f)));
            return Dibujar(p, new Efectos { Fase = f / 8f, Runas = 1f, ManoBrilla = 1f, Recorte = true, Salpicaduras = true });
        }

        static Pose Apunta => Base.With(("agacha", 2f), ("inclina", 8f), ("cabeza", -2f), ("brazoD1", 92f), ("brazoD2", 112f), ("apoya", 0f),
                                         ("baculo", 68f), ("agarre", 134f), ("brazoT1", -40f), ("brazoT2", -62f), ("gema", 1.4f), ("ojos", 1.5f),
                                         ("abre", 1.35f), ("alza", 0.45f));

        // Esferas: alza el tridente, la gema crece (las chispas convergen) y estalla en el 7.º fotograma.
        // Del 3 al 8 el brazo y el tridente no se mueven: la gema queda en CastPoint.
        ShadedCanvas Esferas(int f)
        {
            return Dibujar(PoseEsferas(f), new Efectos
            {
                Fase = f / 9f, Runas = f >= 2 ? 0.8f : 0.3f, Converge = f >= 2 && f <= 6 ? 1f : 0f, Destello = f == 7 ? 2 : f == 8 ? 1 : 0,
            });
        }

        static Pose PoseEsferas(int f)
        {
            var reune = Base.With(("agacha", 3f), ("inclina", 4f), ("cabeza", 2f), ("brazoD1", 50f), ("brazoD2", 96f), ("apoya", 0f), ("baculo", 96f), ("agarre", 90f),
                                  ("gema", 1.1f), ("ojos", 1.2f), ("abre", 1.1f), ("alza", 0.2f));
            var keys = new Keyframes()
                .Key(0, Base.With(("apoya", 0f), ("agarre", 80f), ("brazoD1", 44f)))
                .Key(1, reune)
                .Key(3, Apunta, Ease.Out)
                .Key(6, Apunta.With(("gema", 2.6f), ("ojos", 1.9f), ("alza", 0.6f), ("abre", 1.5f)))
                .Key(7, Apunta.With(("gema", 0.8f), ("ojos", 2.2f), ("grita", 0.8f), ("abre", 1.6f), ("alza", 0.7f), ("cabeza", -6f)), Ease.Out)
                .Key(8, Apunta.With(("gema", 0.9f), ("grita", 0.3f), ("cabeza", -3f)));
            var p = keys.Evaluate(f);
            p["onda"] = f * 0.9f;
            p["barba"] = f * 0.8f;
            p["tela"] = f * 0.5f;
            p["pies"] = f * 0.3f;
            return p;
        }

        /// <summary>Centro de la gema del tridente en el fotograma <paramref name="f"/> de "orbs" (píxeles desde el pivote).</summary>
        internal static Vector2 GemaEnEsferas(int f)
        {
            var p = PoseEsferas(f);
            var b = CuerpoDe(p);
            return Gema(ExtremosBaculo(b, p));
        }

        // Embestida (preparación): se agacha, cabeza gacha, los tentáculos se recogen hacia atrás y los ojos arden.
        ShadedCanvas PreparaEmbestida(int f)
        {
            var baja = Base.With(("agacha", 12f), ("inclina", 30f), ("cabeza", 16f), ("ojos", 2f), ("brazoD1", 0f), ("brazoD2", 40f), ("apoya", 0f),
                                 ("baculo", 14f), ("agarre", 150f), ("brazoT1", -50f), ("brazoT2", -30f), ("abre", 0.7f), ("alza", -0.1f),
                                 ("enrosca", 0.5f), ("viento", 0.35f), ("x", -6f), ("grita", 0.3f));
            var keys = new Keyframes()
                .Key(0, Base.With(("agacha", 2f), ("inclina", 6f), ("cabeza", -4f), ("apoya", 0f), ("baculo", 84f), ("agarre", 80f)))
                // El tridente pasa de vertical a la ristre de golpe (en los ángulos intermedios no cabría en la celda).
                .Key(1, Pose.Lerp(Base.With(("agacha", 2f), ("inclina", 6f), ("cabeza", -4f)), baja, 0.45f).With(("baculo", 16f), ("agarre", 150f), ("apoya", 0f)))
                .Key(3, baja, Ease.Out)
                .Key(5, baja.With(("agacha", 13f), ("x", -8f), ("cabeza", 18f)));
            var p = keys.Evaluate(f);
            p["onda"] = f * 0.7f;
            p["barba"] = f * 0.6f;
            p["tela"] = f * 0.4f;
            if (f >= 4) p["x"] += f % 2 == 0 ? 1f : -1f;
            return Dibujar(p, new Efectos { Fase = f / 6f, Polvo = f >= 4 ? 1 : 0, Runas = 0.5f });
        }

        // Embestida: se lanza inclinado reptando a toda prisa; tentáculos, barba y manto arrastrados hacia atrás.
        ShadedCanvas Embestida(int f)
        {
            float t = f / 4f * Mathf.PI * 2f;
            var p = Base.With(("agacha", 9f + 2f * Mathf.Cos(t * 2f)), ("inclina", 32f), ("cabeza", 12f), ("ojos", 2f), ("grita", 0.5f),
                              ("brazoD1", 10f), ("brazoD2", 50f), ("apoya", 0f), ("baculo", 18f), ("agarre", 150f),
                              ("brazoT1", -64f - 14f * Mathf.Sin(t)), ("brazoT2", -40f),
                              ("onda", t * 2f), ("barba", t * 2f), ("tela", t * 2f), ("vaiven", 2.2f), ("viento", 1f), ("abre", 0.9f),
                              ("pies", t * 2f), ("repta", 1.6f));
            return Dibujar(p, new Efectos { Fase = f / 4f, Polvo = f % 2 == 0 ? 1 : 0, Velocidad = true });
        }

        // Recuperación: jadea apoyado en el tridente, los tentáculos caen sin fuerza y se recompone.
        ShadedCanvas Recupera(int f)
        {
            var cansado = Base.With(("agacha", 7f), ("inclina", 24f), ("cabeza", 16f), ("brazoD1", 34f), ("brazoD2", 76f), ("apoya", 1f),
                                    ("brazoT1", 16f), ("brazoT2", 34f), ("ojos", 0.6f), ("gema", 0.7f), ("abre", 0.7f), ("alza", -0.85f), ("vaiven", 0.6f));
            var keys = new Keyframes()
                .Key(0, cansado.With(("inclina", 28f), ("agacha", 8f)))
                .Key(3, cansado.With(("inclina", 20f), ("agacha", 5f), ("alza", -0.6f)))
                .Key(5, Pose.Lerp(cansado, Base, 0.75f));
            var p = keys.Evaluate(f);
            p["onda"] = f * 0.6f;
            p["barba"] = f * 0.7f;
            p["tela"] = f * 0.35f;
            p["agacha"] += f % 2 == 0 ? 0.6f : -0.6f;
            return Dibujar(p, new Efectos { Fase = f / 6f });
        }

        // Segunda fase: se encoge, y de golpe echa la cabeza atrás y grita con la barba abierta; brotan dos tentáculos más
        // y los ojos arden al blanco.
        ShadedCanvas CambioDeFase(int f)
        {
            var encoge = Base.With(("agacha", 8f), ("inclina", 26f), ("cabeza", 22f), ("ojos", 0.6f), ("abre", 0.45f), ("alza", -0.4f), ("enrosca", 0.7f),
                                   ("brazoD1", 26f), ("brazoD2", 60f), ("brazoT1", 10f), ("brazoT2", 30f), ("gema", 0.7f));
            var ruge = Base.With(("agacha", -1f), ("inclina", -10f), ("cabeza", -24f), ("grita", 1f), ("ojos", 2.6f), ("abre", 1.85f), ("alza", 0.85f),
                                 ("extra", 1f), ("brazoD1", 70f), ("brazoD2", 96f), ("apoya", 1f), ("brazoT1", -120f), ("brazoT2", -140f),
                                 ("gema", 1.8f), ("vaiven", 2.4f));
            var keys = new Keyframes()
                .Key(0, Base.With(("agacha", 3f), ("inclina", 14f), ("cabeza", 12f)))
                .Key(2, encoge, Ease.In)
                .Key(4, ruge, Ease.Out)
                .Key(9, ruge.With(("cabeza", -20f), ("grita", 0.9f)));
            var p = keys.Evaluate(f);
            p["extra"] = Mathf.Clamp01((f - 2.5f) / 2.5f);
            p["onda"] = f * 1.1f;
            p["barba"] = f * 1.1f;
            p["tela"] = f * 0.6f;
            p["pies"] = f * 0.5f;
            if (f >= 4) p["x"] = f % 2 == 0 ? 1.2f : -1.2f;
            return Dibujar(p, new Efectos { Fase = f / 10f, Runas = f >= 4 ? 1.6f : 0.4f, Polvo = f == 4 ? 2 : 0, Aura = f >= 4 ? 1f : 0f });
        }

        // Muerte: retrocede herido, se tambalea, cae de rodillas soltando el tridente, se desploma y se disuelve en un charco
        // de tinta con espuma, del que solo quedan las puntas de los tentáculos hundiéndose.
        ShadedCanvas Muerte(int f)
        {
            var golpe = Base.With(("x", -4f), ("inclina", -10f), ("cabeza", -18f), ("grita", 0.8f), ("ojos", 2.2f), ("brazoD1", 30f), ("brazoD2", 60f),
                                  ("apoya", 0f), ("baculo", 100f), ("agarre", 70f), ("brazoT1", -60f), ("brazoT2", -40f), ("abre", 1.6f), ("alza", 0.6f),
                                  ("vaiven", 2f));
            var tambalea = golpe.With(("x", -2f), ("inclina", 18f), ("cabeza", 18f), ("grita", 0.4f), ("agacha", 6f), ("ojos", 1.5f),
                                      ("brazoD1", 12f), ("brazoD2", 30f), ("brazoT1", 14f), ("brazoT2", 20f), ("abre", 1.2f), ("alza", -0.2f));
            var rodillas = tambalea.With(("agacha", 24f), ("inclina", 16f), ("cabeza", 24f), ("grita", 0.2f), ("ojos", 0.9f), ("brazoD1", 6f), ("brazoD2", 4f),
                                         ("brazoT1", 4f), ("brazoT2", 0f), ("abre", 0.9f), ("alza", -0.7f));
            var desploma = rodillas.With(("agacha", 30f), ("inclina", 40f), ("cabeza", 20f), ("ojos", 0.5f), ("alza", -1f), ("abre", 0.8f), ("hunde", 6f));
            var deshace = desploma.With(("agacha", 34f), ("inclina", 48f), ("hunde", 44f), ("ojos", 0.2f), ("alza", -1.2f));
            var keys = new Keyframes()
                .Key(0, golpe)
                .Key(2, golpe.With(("x", -5f), ("cabeza", -22f)))
                .Key(4, tambalea, Ease.InOut)
                .Key(7, rodillas, Ease.In)
                .Key(9, desploma)
                .Key(12, deshace, Ease.In)
                .Key(15, deshace.With(("hunde", 150f), ("ojos", 0f)));
            var p = keys.Evaluate(f);
            p["onda"] = f < 10 ? f * 0.9f : 9f + (f - 10) * 0.3f;
            p["barba"] = f * 0.8f;
            p["tela"] = f * 0.4f;
            p["vaiven"] = f < 10 ? 1.6f : 0.5f;
            var fx = new Efectos
            {
                Fase = f / 16f, Polvo = f == 7 ? 1 : 0, CaeBaculo = f >= 5 ? f - 4 : 0, Muerto = f >= 13, Recorte = f >= 8,
                Charco = f >= 7 ? Mathf.Clamp01((f - 6f) / 6f) : 0f, Espuma = f >= 9 ? 1f : 0f,
                Monticulo = f >= 9 ? (f <= 12 ? (f - 8) * 2.6f : Mathf.Max(2f, 10.4f - (f - 12) * 2.8f)) : 0f,
                TintaHasta = f >= 8 ? Mathf.Lerp(3f, 70f, Mathf.Clamp01((f - 8f) / 6f)) : 0f,
            };
            return Dibujar(p, fx);
        }

        // ------------------------------------------------------------------
        // Dibujo
        // ------------------------------------------------------------------

        struct Efectos
        {
            public float Fase;
            public int Polvo, Destello;
            /// <summary>0 = no; n = fotograma n-1 desde que suelta el tridente.</summary>
            public int CaeBaculo;
            public float Runas, Converge, Charco, Espuma, ManoBrilla, Aura;
            /// <summary>Alto (px) del montón de tinta burbujeante donde se hunde el cuerpo.</summary>
            public float Monticulo;
            /// <summary>Altura (px) hasta la que la tinta se ha comido la ropa y la carne (0 = nada).</summary>
            public float TintaHasta;
            public bool Velocidad, Muerto, Recorte, Salpicaduras;
        }

        /// <summary>Transformación del cuerpo: inclinación del tronco sobre la cadera, cabeceo, derrumbe y hundimiento.</summary>
        sealed class Cuerpo
        {
            public Vector2 Cadera;
            public float Inclina, Cabeceo, Caida, Hundido;
            public Vector2 PivoteCaida;

            public Vector2 W(Vector2 p)
            {
                if (Caida != 0f) p = Rotate(p, PivoteCaida, -Caida);
                return Hundido != 0f ? V(p.x, p.y - Hundido) : p;
            }

            Vector2 Crudo(float x, float y) => Rotate(V(Cadera.x + x, Cadera.y + y), Cadera, -Inclina);
            public Vector2 U(float x, float y) => W(Crudo(x, y));

            public Vector2 H(float x, float y)
            {
                Vector2 cuello = Crudo(CuelloX, CuelloY);
                return W(Rotate(V(cuello.x + x, cuello.y + y), cuello, -(Inclina * 0.5f + Cabeceo)));
            }

            public float AnguloCabeza => -(Inclina * 0.5f + Cabeceo) - Caida;
            public float AnguloTronco => -Inclina - Caida;
        }

        static Cuerpo CuerpoDe(Pose p)
        {
            var b = new Cuerpo
            {
                Cadera = V(p["x"], CaderaY - p["agacha"] + p["y"]),
                Inclina = p["inclina"],
                Cabeceo = p["cabeza"],
                Caida = p["cae"],
                Hundido = p["hunde"],
            };
            b.PivoteCaida = V(p["x"] + 24f, 0f);
            return b;
        }

        ShadedCanvas Dibujar(Pose p, Efectos fx)
        {
            var c = NewFrame();
            var b = CuerpoDe(p);
            var extremos = ExtremosBaculo(b, p);

            if (fx.Aura > 0f) DibujarAura(c, b, fx);
            if (fx.Runas > 0f) DibujarRunas(c, b, fx);
            DibujarTentaculosEspalda(c, b, p, fx);
            DibujarManto(c, b, p, fx);
            DibujarBrazo(c, b, p, false, fx);
            DibujarPies(c, b, p, fx, false);
            DibujarTunica(c, b, p, fx);
            DibujarPies(c, b, p, fx, true);
            DibujarCinturon(c, b, p);
            DibujarEsclavina(c, b, p, fx);
            DibujarCabeza(c, b, p, fx);
            if (fx.CaeBaculo == 0) DibujarTridente(c, p, fx, extremos);
            else DibujarTridenteCaido(c, fx);
            DibujarBrazo(c, b, p, true, fx);
            if (fx.Converge > 0f) DibujarConverge(c, Gema(extremos), fx);
            if (fx.Charco > 0f) DibujarCharco(c, b, fx);
            if (fx.Polvo > 0) DibujarPolvo(c, b, fx);
            if (fx.Velocidad) DibujarVelocidad(c, b, fx);
            if (fx.Recorte) c.Custom(-160f, -12f, 160f, 0f, (px, py) => (py < 0f, N3.Front), Invisible, 999f);
            return c;
        }

        // ---------------- Utilidades geométricas ----------------

        static bool DentroPoligono(Vector2[] poly, float x, float y)
        {
            bool dentro = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if ((poly[i].y > y) != (poly[j].y > y) &&
                    x < (poly[j].x - poly[i].x) * (y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    dentro = !dentro;
            }
            return dentro;
        }

        /// <summary>Distancia de un punto a una polilínea (abierta) o al contorno de un polígono (cerrada).</summary>
        static float Distancia(IList<Vector2> pts, float x, float y, bool cerrada)
        {
            float mejor = float.MaxValue;
            int n = pts.Count;
            for (int i = cerrada ? 0 : 1; i < n; i++)
            {
                Vector2 a = pts[i == 0 ? n - 1 : i - 1], e = pts[i];
                float dx = e.x - a.x, dy = e.y - a.y;
                float t = Mathf.Clamp01(((x - a.x) * dx + (y - a.y) * dy) / Mathf.Max(0.0001f, dx * dx + dy * dy));
                float ex = a.x + dx * t - x, ey = a.y + dy * t - y;
                mejor = Mathf.Min(mejor, ex * ex + ey * ey);
            }
            return Mathf.Sqrt(mejor);
        }

        static void Limites(IList<Vector2> pts, out float x0, out float y0, out float x1, out float y1)
        {
            x0 = y0 = float.MaxValue;
            x1 = y1 = float.MinValue;
            foreach (var q in pts)
            {
                x0 = Mathf.Min(x0, q.x); x1 = Mathf.Max(x1, q.x);
                y0 = Mathf.Min(y0, q.y); y1 = Mathf.Max(y1, q.y);
            }
        }

        /// <summary>
        /// Disolución: tiñe de tinta la parte de la pieza que queda por debajo de <paramref name="hasta"/> (px, con borde
        /// irregular). La máscara deshace el marco local de la pieza para trabajar en coordenadas del lienzo.
        /// </summary>
        static ShadedCanvas.Shape Entintar(ShadedCanvas.Shape s, float hasta)
        {
            if (hasta <= 0f) return s;
            float x0 = s.FrameX, y0 = s.FrameY, fc = s.FrameCos, fs = s.FrameSin;
            return s.WithTint(ColorTinta, 0.88f, (u, v) =>
            {
                float x = x0 + u * fc - v * fs, y = y0 + u * fs + v * fc;
                return y < hasta + 5f * (PixelCanvas.ValueNoise(x / 5f, 0f, 0, 77) - 0.5f) ? 1f : 0f;
            });
        }

        /// <summary>Manchas de salitre en grupos (o la tinta, si se está disolviendo).</summary>
        static ShadedCanvas.Shape Manchas(ShadedCanvas.Shape s, float hasta, int semilla)
        {
            if (hasta > 0f) return Entintar(s, hasta);
            return s.WithTint(Salitre, 0.22f, (u, v) =>
                PixelCanvas.ValueNoise(u / 5f, v / 3.5f, 0, semilla) > 0.66f && PixelCanvas.ValueNoise(u / 1.8f, v / 1.8f, 0, semilla + 1) > 0.35f ? 1f : 0f);
        }

        /// <summary>Tela abombada dentro de un contorno (capucha, esclavina): normal de un elipsoide girado.</summary>
        static ShadedCanvas.Shape Domo(ShadedCanvas c, Vector2[] poly, Vector2 centro, float rx, float ry, float ang, PixelMaterial m, float z, int g,
                                       System.Func<float, float, bool> pred = null)
        {
            Limites(poly, out float x0, out float y0, out float x1, out float y1);
            float ca = Mathf.Cos(ang * Mathf.Deg2Rad), sa = Mathf.Sin(ang * Mathf.Deg2Rad);
            return c.Custom(x0 - 1f, y0 - 1f, x1 + 1f, y1 + 1f, (px, py) =>
            {
                if (!DentroPoligono(poly, px, py)) return (false, N3.Front);
                if (pred != null && !pred(px, py)) return (false, N3.Front);
                float dx = px - centro.x, dy = py - centro.y;
                float lx = (dx * ca + dy * sa) / rx, ly = (-dx * sa + dy * ca) / ry;
                float nz = Mathf.Sqrt(Mathf.Max(0.12f, 1f - lx * lx - ly * ly));
                float wx = lx * ca - ly * sa, wy = lx * sa + ly * ca;
                return (true, new N3(wx * 0.85f, wy * 0.85f, nz).Normalized());
            }, m, z, 0f, g);
        }

        /// <summary>
        /// Tela que cuelga: dentro de un contorno, con la normal de un cilindro (se curva hacia los lados), un poco hacia
        /// arriba en los hombros y pliegues ondulados que crecen hacia el bajo. Coordenadas propias (s a lo largo desde
        /// arriba, r a través) para bordados y pliegues; las regiones reciben además la posición en el lienzo.
        /// </summary>
        sealed class Tela
        {
            readonly Vector2[] poly;
            public readonly Vector2 Top, Axis, Perp;
            public readonly float Len, HalfWidth, Folds, Phase, Amp;
            readonly float minX, minY, maxX, maxY;

            public Tela(Vector2[] contorno, Vector2 arriba, Vector2 abajo, float semiancho, float pliegues, float fase, float amplitud)
            {
                poly = contorno;
                Top = arriba;
                Vector2 d = Sub(abajo, arriba);
                Len = Mathf.Max(1f, d.magnitude);
                Axis = Scale(d, 1f / Len);
                Perp = new Vector2(-Axis.y, Axis.x);
                HalfWidth = semiancho;
                Folds = pliegues;
                Phase = fase;
                Amp = amplitud;
                Limites(contorno, out minX, out minY, out maxX, out maxY);
            }

            public float AxisAngle => Mathf.Atan2(Axis.y, Axis.x) * Mathf.Rad2Deg;
            public Vector2 At(float s, float r) => Add(Top, Add(Scale(Axis, s), Scale(Perp, r)));
            public bool Dentro(float x, float y) => DentroPoligono(poly, x, y);
            public float Borde(float x, float y) => Distancia(poly, x, y, true);

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

            /// <summary>Posiciones r de los valles de los pliegues (donde la normal pasa de mirar a un lado al otro).</summary>
            public List<float> Valles()
            {
                var list = new List<float>();
                for (int k = -6; k <= 6; k++)
                {
                    float u = (Mathf.PI * (2 * k + 1) - Phase) / (Folds * Mathf.PI);
                    if (u > -0.92f && u < 0.92f) list.Add(u * HalfWidth);
                }
                return list;
            }

            /// <summary>Parte de la tela que cumple pred(s, r, x, y) (null = toda), con el marco local en el eje de la tela.</summary>
            public ShadedCanvas.Shape Region(ShadedCanvas c, System.Func<float, float, float, float, bool> pred, PixelMaterial m, float z, int g)
            {
                return c.Custom(minX - 1f, minY - 1f, maxX + 1f, maxY + 1f, (px, py) =>
                {
                    if (!DentroPoligono(poly, px, py)) return (false, N3.Front);
                    Local(px, py, out float s, out float r);
                    if (pred != null && !pred(s, r, px, py)) return (false, N3.Front);
                    return (true, Normal(s, r));
                }, m, z, 0f, g).Frame(Top, AxisAngle);
            }
        }

        // ---------------- Tentáculos ----------------

        /// <summary>
        /// Tentáculo: cadena de cápsulas que se afina, con anillos en relieve continuos (el marco de cada tramo sigue la
        /// longitud recorrida), lomo oscuro y vientre un tono más claro, ventosas por el lado del vientre (dos hileras al
        /// tresbolillo donde es grueso) y brillos húmedos en el lomo. <paramref name="lado"/> = +1: vientre a la derecha del
        /// sentido de avance. Lo que queda por debajo de <paramref name="tinta"/> (px) ya es tinta.
        /// </summary>
        static void Tentaculo(ShadedCanvas c, IList<Vector2> pts, float r0, float r1, PixelMaterial piel, PixelMaterial ventosa,
                              float z, float shade, int g, float lado, float tinta, System.Func<int, float> grosor = null)
        {
            int n = pts.Count - 1;
            float acc = 0f;
            var radios = new float[n + 1];
            for (int k = 0; k <= n; k++) radios[k] = Mathf.Lerp(r0, r1, k / (float)n) * (grosor != null ? grosor(k) : 1f);
            for (int k = 0; k < n; k++)
            {
                Vector2 a = pts[k], e = pts[k + 1];
                Vector2 d = Sub(e, a);
                float len = Mathf.Max(0.001f, d.magnitude);
                float ra = radios[k], rb = radios[k + 1], rr = (ra + rb) * 0.5f;
                bool tinto = tinta > 0f && (a.y + e.y) * 0.5f < tinta;
                float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                c.Capsule(a, e, ra, rb, tinto ? Tinta : piel, z, shade, g)
                 .Frame(Sub(a, Scale(d, acc / len)), ang)
                 .WithBump(Patterns.Ridges(2.6f, 0.35f), 0.55f)
                 .WithDetail((u, v) => v * lado < -rr * 0.25f ? 1 : 0);
                acc += len;
            }
            for (int k = 1; k < n - 1; k++)
            {
                float rr = (radios[k] + radios[k + 1]) * 0.5f;
                if (rr < 1.25f) continue;
                Vector2 a = pts[k], e = pts[k + 1];
                if (tinta > 0f && (a.y + e.y) * 0.5f < tinta + 2f) continue;
                Vector2 d = Sub(e, a).normalized;
                Vector2 vientre = Scale(V(d.y, -d.x), lado);
                float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                float sr = Mathf.Max(0.72f, rr * 0.3f);
                Vector2 q = Add(Mix(a, e, 0.55f), Scale(vientre, rr * 0.62f));
                c.Ellipse(q, sr, sr * 0.85f, ang, ventosa, z + 0.01f, shade, g);
                if (sr > 1.05f) c.Dot(q, -2, g, ventosa);
                if (rr > 3.1f)
                {
                    Vector2 q2 = Add(Mix(a, e, 0.05f), Scale(vientre, rr * 0.22f));
                    c.Ellipse(q2, sr * 0.8f, sr * 0.7f, ang, ventosa, z + 0.01f, shade, g);
                }
                // Brillo húmedo en el lomo, solo si el lomo mira hacia la luz.
                Vector2 lomo = Scale(vientre, -1f);
                if (k % 3 == 1 && rr > 1.9f && lomo.y > 0.1f) c.Glint(Add(Mix(a, e, 0.5f), Scale(lomo, rr * 0.5f)), g, 0, piel);
            }
        }

        /// <summary>Un tentáculo de la espalda: raíz en el tronco, ángulo base, largo, grosor, profundidad y sentido del rizo.</summary>
        struct Brazo
        {
            public float X, Y, Ang, Largo, Radio, Z, Sombra, Rizo, Desfase, Onda;
            public bool Hunde;
            public Vector2 Destino;
        }

        static readonly Brazo[] Espalda =
        {
            new Brazo { X = -16f, Y = 50f, Ang = 106f, Largo = 96f, Radio = 5.2f, Z = 0.7f, Sombra = -0.06f, Rizo = 1f, Desfase = 0f, Onda = 1f },
            new Brazo { X = -22f, Y = 46f, Ang = 144f, Largo = 112f, Radio = 5.8f, Z = 0.9f, Sombra = 0f, Rizo = 1f, Desfase = 2.1f, Onda = 0.8f },
            new Brazo { X = -26f, Y = 36f, Ang = 180f, Largo = 94f, Radio = 5.4f, Z = 0.85f, Sombra = 0f, Rizo = -1f, Desfase = 0.9f, Onda = 1.2f, Hunde = true, Destino = new Vector2(-106f, -14f) },
            new Brazo { X = -24f, Y = 24f, Ang = 218f, Largo = 76f, Radio = 4.8f, Z = 0.8f, Sombra = -0.04f, Rizo = 1f, Desfase = 3.9f, Onda = 0.9f, Hunde = true, Destino = new Vector2(-62f, -14f) },
            new Brazo { X = -6f, Y = 52f, Ang = 62f, Largo = 96f, Radio = 4.8f, Z = 0.5f, Sombra = -0.12f, Rizo = -1f, Desfase = 5.2f, Onda = 1.1f },
            new Brazo { X = -8f, Y = 40f, Ang = 20f, Largo = 104f, Radio = 4.6f, Z = 0.4f, Sombra = -0.15f, Rizo = -1f, Desfase = 1.7f, Onda = 1f, Hunde = true, Destino = new Vector2(108f, -14f) },
            // Los dos que brotan en la segunda fase.
            new Brazo { X = -20f, Y = 42f, Ang = 162f, Largo = 90f, Radio = 4.6f, Z = 0.6f, Sombra = -0.1f, Rizo = -1f, Desfase = 4.4f, Onda = 1.1f },
            new Brazo { X = -12f, Y = 46f, Ang = 88f, Largo = 84f, Radio = 4.4f, Z = 0.45f, Sombra = -0.14f, Rizo = 1f, Desfase = 3.1f, Onda = 0.9f },
        };

        /// <summary>
        /// Línea central de un tentáculo de la espalda: cada tramo gira un poco (curva base), más una onda que viaja de la raíz
        /// a la punta (la fase "onda" avanza un ciclo entero por bucle) y un rizo en espiral en el último tercio.
        /// </summary>
        static List<Vector2> LineaBrazo(Cuerpo b, Pose p, Brazo t, float escala)
        {
            float abre = p["abre"], alza = p["alza"], enrosca = p["enrosca"], viento = Mathf.Clamp01(p["viento"]), ph = p["onda"], vaiven = p["vaiven"];
            Vector2 raiz = b.U(t.X, t.Y);
            const float medio = 125f;
            float ang = medio + (t.Ang - medio) * abre;
            if (alza > 0f) ang += (90f - ang) * Mathf.Min(1f, alza) * 0.35f;
            else ang += (t.Ang > 100f ? 1f : -1f) * -alza * 50f;
            ang = Mathf.Lerp(ang, 172f + (t.Ang - 125f) * 0.22f, viento * 0.85f);
            ang -= b.Inclina * 0.6f + b.Caida;
            const int n = 16;
            float total = 0f;
            for (int k = 1; k <= n; k++) total += Mathf.Lerp(1.15f, 0.5f, k / (float)n);
            float seg = t.Largo * escala / total;
            float amp = 7.5f * Mathf.Max(0.3f, vaiven) * t.Onda;
            var pts = new List<Vector2>(n + 1) { raiz };
            Vector2 q = raiz;
            for (int k = 1; k <= n; k++)
            {
                float s = k / (float)n;
                ang += t.Rizo * (3.6f + 9f * enrosca);
                ang += (Mathf.Sin(ph - s * 2.6f + t.Desfase) + 0.45f * Mathf.Sin(ph * 2f - s * 6.2f + t.Desfase * 1.7f)) * amp * (0.3f + s);
                ang += t.Rizo * Mathf.SmoothStep(0f, 1f, (s - 0.55f) / 0.45f) * (58f + 18f * enrosca);
                q = Add(q, Dir(ang, seg * Mathf.Lerp(1.15f, 0.5f, s)));
                pts.Add(q);
            }
            float clava = Mathf.Clamp01(p["clava"]);
            if (t.Hunde && clava > 0f)
            {
                // Conjuro: se arquea y se hunde en el suelo serpenteando (lo de debajo del suelo se recorta).
                Vector2 destino = V(t.Destino.x + b.Cadera.x, t.Destino.y);
                Vector2 control = Add(Mix(raiz, destino, 0.35f), V(0f, 46f));
                var curva = Bezier(raiz, control, destino, n + 1);
                for (int k = 1; k <= n; k++)
                {
                    float s = k / (float)n;
                    Vector2 dd = Sub(curva[Mathf.Min(n, k + 1)], curva[k - 1]).normalized;
                    Vector2 serpentea = Scale(V(-dd.y, dd.x), Mathf.Sin(ph * 2f - s * 7f + t.Desfase) * 6.5f * Mathf.Sin(s * Mathf.PI));
                    pts[k] = Mix(pts[k], Add(curva[k], serpentea), clava * Mathf.SmoothStep(0f, 1f, s * 3f));
                }
            }
            return pts;
        }

        static void DibujarTentaculosEspalda(ShadedCanvas c, Cuerpo b, Pose p, Efectos fx)
        {
            float extra = Mathf.Clamp01(p["extra"]), clava = Mathf.Clamp01(p["clava"]), ph = p["onda"];
            for (int i = 0; i < Espalda.Length; i++)
            {
                var t = Espalda[i];
                float escala = 1f, grueso = 1f;
                if (i >= 6)
                {
                    if (extra <= 0.02f) continue;
                    escala = extra;
                    grueso = 0.55f + 0.45f * extra;
                }
                var pts = LineaBrazo(b, p, t, escala);
                int g = c.NewGroup();
                System.Func<int, float> late = null;
                if (t.Hunde && clava > 0f)
                {
                    // Latido: bultos que bajan por el tentáculo hacia la tierra.
                    float fase = ph;
                    late = k => 1f + 0.2f * clava * Mathf.Max(0f, Mathf.Sin(fase * 2f - k * 0.9f));
                }
                Tentaculo(c, pts, t.Radio * 1.25f * grueso, t.Hunde ? Mathf.Lerp(0.8f, 2.4f, clava) : 0.8f, Carne, Ventosa, t.Z, t.Sombra, g, -t.Rizo, fx.TintaHasta, late);
                if (t.Hunde && clava > 0.5f && fx.Salpicaduras) Salpicadura(c, pts, fx, i);
            }
        }

        /// <summary>Donde el tentáculo entra en la tierra: charquito de tinta, gotas que saltan y un anillo de runas encendidas.</summary>
        static void Salpicadura(ShadedCanvas c, List<Vector2> pts, Efectos fx, int semilla)
        {
            float x = float.NaN;
            for (int k = 1; k < pts.Count; k++)
            {
                if (pts[k - 1].y >= 0f && pts[k].y < 0f)
                {
                    float t = pts[k - 1].y / Mathf.Max(0.001f, pts[k - 1].y - pts[k].y);
                    x = Mathf.Lerp(pts[k - 1].x, pts[k].x, t);
                    break;
                }
            }
            if (float.IsNaN(x)) return;
            int g = c.NewGroup();
            // Anillo de runas: elipse aplastada de trazos encendidos alrededor de la herida en el suelo.
            float rx = 12f, ry = 2.6f;
            c.Custom(x - rx - 1f, -ry - 1f, x + rx + 1f, ry + 1f, (px, py) =>
            {
                float lx = (px - x) / rx, ly = py / ry;
                float d = Mathf.Sqrt(lx * lx + ly * ly);
                bool trazo = d > 0.78f && d < 1.05f && Mathf.Repeat(Mathf.Atan2(ly, lx) * 4f + fx.Fase * 12f, 1.6f) < 1.05f;
                return (trazo, N3.Front);
            }, (semilla + (int)(fx.Fase * 8f)) % 2 == 0 ? Runa : RunaTenue, 0.3f);
            // Charquito de tinta.
            c.Ellipse(V(x, 0.6f), 7f, 1.8f, 0f, Tinta, 0.95f, 0f, g);
            // Gotas que saltan y caen (ciclo del bucle).
            for (int i = 0; i < 4; i++)
            {
                float fr = Mathf.Repeat(fx.Fase * 2f + i * 0.25f + semilla * 0.13f, 1f);
                float dx = (i % 2 == 0 ? 1f : -1f) * (3f + 6f * fr) * (0.6f + 0.4f * PixelCanvas.Hash(i, semilla, 3));
                float dy = 14f * fr * (1f - fr) * 4f * (0.5f + 0.5f * PixelCanvas.Hash(i, semilla, 4));
                if (dy < 0.8f) continue;
                c.Ellipse(V(x + dx, dy), 1f, 1.2f, 0f, Tinta, 0.96f, 0f, c.NewGroup());
            }
        }

        // ---------------- Pies (tentáculos bajo la túnica) ----------------

        static readonly float[] PieX = { -32f, -18f, -4f, 12f, 26f };
        static readonly float[] PieLargo = { 34f, 26f, 24f, 30f, 40f };
        static readonly float[] PieDir = { -1f, -1f, 1f, 1f, 1f };

        static void DibujarPies(ShadedCanvas c, Cuerpo b, Pose p, Efectos fx, bool delante)
        {
            float ph = p["pies"], repta = p["repta"];
            float suelo = -b.Hundido;
            for (int i = 0; i < PieX.Length; i++)
            {
                bool esDelante = i == 1 || i == 2 || i == 4;
                if (esDelante != delante) continue;
                int g = c.NewGroup();
                float w = ph + i * 1.37f;
                float alcance = PieLargo[i] * (1f + 0.2f * repta * Mathf.Sin(w) + 0.05f * Mathf.Sin(w));
                float dir = PieDir[i];
                Vector2 raiz = V(b.Cadera.x + PieX[i] + 3f * repta * Mathf.Cos(w), 5f + suelo);
                float ang = dir > 0f ? -32f : 212f;
                float objetivo = dir > 0f ? 0f : 180f;
                float punta = 0.25f + repta * Mathf.Max(0f, Mathf.Sin(w + 1f));
                const int n = 10;
                float seg = alcance / n;
                var pts = new List<Vector2> { raiz };
                Vector2 q = raiz;
                for (int k = 1; k <= n; k++)
                {
                    float s = k / (float)n;
                    ang = Mathf.Lerp(ang, objetivo, 0.35f);
                    if (s > 0.55f) ang += dir * (24f + 30f * punta) * (s - 0.55f) / 0.45f;
                    q = Add(q, Dir(ang, seg));
                    float r = Mathf.Lerp(4.6f, 0.8f, s);
                    if (s <= 0.65f) q.y = Mathf.Max(q.y, suelo + r * 0.85f);
                    pts.Add(q);
                }
                Tentaculo(c, pts, 4.6f, 0.8f, Carne, Ventosa, delante ? 2.3f : 1.7f, delante ? 0f : -0.1f, g, dir, fx.TintaHasta);
            }
        }

        // ---------------- Túnica ----------------

        static void DibujarTunica(ShadedCanvas c, Cuerpo b, Pose p, Efectos fx)
        {
            int g = c.NewGroup();
            float vaiven = p["vaiven"], ph = p["tela"], viento = p["viento"], repta = p["repta"];
            float bajoVaiven = Mathf.Sin(ph) * 2f * vaiven - viento * 10f;
            float ag = Mathf.Max(0f, p["agacha"]);
            float suelo = 0f;
            Vector2 cuB = b.U(-12f, 57f), cuF = b.U(12f, 57f);
            Vector2 hoB = b.U(-27f, 50f), hoF = b.U(21f, 50f);
            Vector2 peB = b.U(-32f, 30f), peF = b.U(26f, 30f);
            Vector2 ciB = b.U(-31f, 6f), ciF = b.U(25f, 4f);
            Vector2 bajoB = b.W(V(b.Cadera.x - 50f - ag * 0.8f + bajoVaiven * 1.3f, suelo));
            Vector2 bajoF = b.W(V(b.Cadera.x + 42f + ag * 0.7f + bajoVaiven * 0.5f, suelo));
            Vector2 medB = b.W(V(b.Cadera.x - 42f - ag * 0.4f + bajoVaiven * 0.8f, b.Cadera.y * 0.45f));
            Vector2 medF = b.W(V(b.Cadera.x + 34f + ag * 0.3f + bajoVaiven * 0.3f, b.Cadera.y * 0.45f));
            Vector2 bajoDir = Sub(bajoF, bajoB).normalized;
            Vector2 bajoArriba = V(-bajoDir.y, bajoDir.x);

            // Bajo deshilachado: dientes que tocan el suelo y muescas por las que asoman los tentáculos-pie.
            var contorno = new List<Vector2> { cuB, hoB, peB, ciB, medB, bajoB };
            var muescas = new List<Vector2>();
            const int dientes = 16;
            for (int i = 1; i < dientes; i++)
            {
                float t = i / (float)dientes + (PixelCanvas.Hash(i, 5, 31) - 0.5f) * 0.02f;
                float alto = i % 2 == 1
                    ? 2.5f + 5f * PixelCanvas.Hash(i, 3, 31) + Mathf.Sin(ph * 1.3f + i) * 0.8f + repta * 2.5f * Mathf.Max(0f, Mathf.Sin(p["pies"] + i))
                    : PixelCanvas.Hash(i, 4, 31) * 1.2f;
                var q = Add(Mix(bajoB, bajoF, t), Scale(bajoArriba, alto));
                contorno.Add(q);
                if (i % 2 == 1) muescas.Add(q);
            }
            contorno.Add(bajoF);
            contorno.Add(medF);
            contorno.Add(ciF);
            contorno.Add(peF);
            contorno.Add(hoF);
            contorno.Add(cuF);
            var tela = new Tela(contorno.ToArray(), Mix(cuB, cuF, 0.5f), Mix(bajoB, bajoF, 0.45f), 42f, 4.6f, ph * 0.8f + 0.6f, 0.95f);
            float largo = tela.Len;

            // Forro oscuro que se ve por las muescas del bajo.
            c.Poly(new[] { Mix(bajoB, bajoF, 0.03f), Mix(bajoB, bajoF, 0.97f), Add(Mix(bajoB, bajoF, 0.97f), Scale(bajoArriba, 9f)), Add(Mix(bajoB, bajoF, 0.03f), Scale(bajoArriba, 9f)) },
                   Forro, 1.95f, 0f, 0f, c.NewGroup());

            // Paño con manchas de salitre que suben desde el bajo y chorrean.
            tela.Region(c, null, Tunica, 2f, g).WithDetail((s, r) => s < largo * 0.74f ? Damasco(s, r) : 0).WithTint(Salitre, 0.26f, (s, r) =>
            {
                float sube = s - largo * 0.68f - PixelCanvas.ValueNoise(r / 3f, 0f, 0, 41) * 16f;
                return sube > 0f && PixelCanvas.ValueNoise(r / 2.4f, s / 6f, 0, 43) > 0.42f ? 1f : 0f;
            });

            // Cenefa roja del bajo con ojos bordados y, encima, llamas de tentáculo en hilo rojo oscuro.
            float bajoAng = Mathf.Atan2(bajoDir.y, bajoDir.x) * Mathf.Rad2Deg;
            float Sobre(float x, float y) => (x - bajoB.x) * bajoArriba.x + (y - bajoB.y) * bajoArriba.y;
            float AlLargo(float x, float y) => (x - bajoB.x) * bajoDir.x + (y - bajoB.y) * bajoDir.y;
            tela.Region(c, (s, r, x, y) => { float h = Sobre(x, y); return h >= 5.5f && h < 14.5f; }, Ribete, 2.05f, g)
                .Frame(bajoB, bajoAng).WithDetail(BordadoBajo);
            tela.Region(c, (s, r, x, y) => LlamaBordada(AlLargo(x, y), Sobre(x, y)), RibeteOscuro, 2.04f, g)
                .Frame(bajoB, bajoAng).WithDetail((u, v) => v > 22f ? 1 : 0);

            // Franja delantera (donde se abre la túnica): cadena de anillos bordados.
            Vector2 fa = b.U(16f, 52f), fb = Add(Mix(bajoB, bajoF, 0.8f), Scale(bajoArriba, 14f));
            Vector2 fd = Sub(fb, fa);
            float fl = Mathf.Max(1f, fd.magnitude);
            Vector2 fu = Scale(fd, 1f / fl);
            float fang = Mathf.Atan2(fd.y, fd.x) * Mathf.Rad2Deg;
            tela.Region(c, (s, r, x, y) =>
            {
                float px = x - fa.x, py = y - fa.y;
                float along = px * fu.x + py * fu.y;
                if (along < 0f || along > fl) return false;
                return Mathf.Abs(-px * fu.y + py * fu.x) < Mathf.Lerp(2.6f, 4.4f, along / fl);
            }, Ribete, 2.06f, g).Frame(fa, fang).WithDetail((u, v) =>
            {
                float w = Mathf.Lerp(2.6f, 4.4f, u / fl), av = Mathf.Abs(v);
                if (av > w - 0.9f) return -1;
                float cu = Mathf.Repeat(u, 9f) - 4.5f;
                float d = Mathf.Sqrt(cu * cu + v * v);
                if (Mathf.Abs(d - 1.7f) < 0.5f) return 2;
                if (d < 0.6f) return -1;
                if (Mathf.Abs(cu) > 3.6f && av < 0.6f) return 1;
                return 0;
            });

            // Pliegues pesados: surco + cresta en cada valle, desde la cintura hasta el bajo (cruzan también los bordados).
            foreach (float r in tela.Valles())
            {
                float s0 = largo * (0.24f + 0.14f * PixelCanvas.Hash((int)(r * 10f) + 9, 5, 7));
                var linea = new List<Vector2>();
                for (int k = 0; k <= 4; k++)
                {
                    float s = Mathf.Lerp(s0, largo * 1.06f, k / 4f);
                    linea.Add(tela.At(s, r * Mathf.Lerp(0.8f, 1.05f, k / 4f) + Mathf.Sin(ph + k + r) * 0.6f));
                }
                c.Fold(linea, g, 2, 2, Tunica);
                c.Crease(linea, 1, g, Ribete);
                c.Crease(linea, 1, g, RibeteOscuro);
            }

            // Desgarrones cerca del bajo y hebras sueltas colgando en las muescas.
            for (int i = 0; i < 3; i++)
            {
                float u = Sub(bajoF, bajoB).magnitude * (0.18f + 0.29f * i + 0.05f * PixelCanvas.Hash(i, 2, 77));
                Vector2 o = Add(Add(bajoB, Scale(bajoDir, u)), Scale(bajoArriba, 17f + 4f * PixelCanvas.Hash(i, 3, 77)));
                c.Mark(new[] { o, Add(o, V(1.5f, -3f)), Add(o, V(0.5f, -6f)) }, -3, g, Tunica);
                c.Mark(new[] { Add(o, V(1f, 0f)), Add(o, V(2.5f, -3f)) }, 1, g, Tunica);
            }
            foreach (var m in muescas)
            {
                float l = 2f + 2.5f * PixelCanvas.Hash(Mathf.FloorToInt(m.x), 7, 3);
                c.Capsule(m, Add(m, V(0.4f, -l)), 0.5f, 0.45f, Tunica, 2.01f, -0.05f, g);
            }

            // Disolución: la tinta sube por la tela (borde irregular con lenguas).
            if (fx.TintaHasta > 0f)
            {
                float hasta = fx.TintaHasta;
                tela.Region(c, (s, r, x, y) => y < hasta + 5f * (PixelCanvas.ValueNoise(x / 5f, 0f, 0, 77) - 0.5f) +
                                                  (PixelCanvas.Hash(Mathf.FloorToInt(x / 3f), 1, 79) > 0.72f ? 6f : 0f), Tinta, 2.09f, g);
            }
        }

        /// <summary>Damasco tono sobre tono: celosía de rombos tejida un tono más honda, con un botón claro en cada celda.</summary>
        static int Damasco(float s, float r)
        {
            const float alto = 18f, ancho = 12f;
            float a = s / alto + r / ancho, b = s / alto - r / ancho;
            const float grad = 0.1f; // |∇a| en 1/px
            float da = Mathf.Abs(Mathf.Repeat(a + 0.5f, 1f) - 0.5f) / grad, db = Mathf.Abs(Mathf.Repeat(b + 0.5f, 1f) - 0.5f) / grad;
            if (da < 0.55f || db < 0.55f) return -1;
            float ca = Mathf.Abs(Mathf.Repeat(a, 1f) - 0.5f) / grad, cb = Mathf.Abs(Mathf.Repeat(b, 1f) - 0.5f) / grad;
            if (ca < 0.8f && cb < 0.8f) return 1;
            return 0;
        }

        /// <summary>Cenefa del bajo (v de 5,5 a 14,5 sobre el borde): cantos oscuros, hileras de cuentas y ojos entre ondas.</summary>
        static int BordadoBajo(float u, float v)
        {
            if (v < 6.5f || v > 13.5f) return -1;
            if (v < 7.6f || v > 12.4f) return Mathf.Repeat(u, 3f) < 1f ? 2 : 0;
            float cu = Mathf.Repeat(u, 14f) - 7f, cv = v - 10f;
            float almendra = Mathf.Abs(cu) / 4.6f + Mathf.Abs(cv) / 1.9f;
            if (almendra > 0.74f && almendra < 1.08f) return 2;
            if (Mathf.Abs(cu) < 0.8f && Mathf.Abs(cv) < 1.1f) return -2;
            if (Mathf.Abs(cu) > 5.3f && Mathf.Abs(cv - Mathf.Sin(u * 1.3f) * 1.2f) < 0.6f) return 1;
            return 0;
        }

        /// <summary>Llamas de tentáculo bordadas sobre la cenefa: tallos ondulados de alto variable rematados en espiral.</summary>
        static bool LlamaBordada(float u, float h)
        {
            const float paso = 11f, base0 = 14.5f;
            if (h < base0) return false;
            int k = Mathf.FloorToInt(u / paso);
            float alto = 8f + 13f * PixelCanvas.Hash(k, 3, 91);
            float t = h - base0;
            if (t > alto + 3f) return false;
            float x0 = (k + 0.5f) * paso + 2.2f * Mathf.Sin(t * 0.42f + k * 1.7f);
            float grosor = 0.5f + 0.45f * Mathf.Clamp01(1f - t / alto);
            if (t <= alto && Mathf.Abs(u - x0) < grosor) return true;
            float lado = (k & 1) == 0 ? 1f : -1f;
            float xa = (k + 0.5f) * paso + 2.2f * Mathf.Sin(alto * 0.42f + k * 1.7f);
            float cx = xa + 1.7f * lado;
            float d = Mathf.Sqrt((u - cx) * (u - cx) + (t - alto) * (t - alto));
            return Mathf.Abs(d - 1.7f) < 0.55f && t > alto - 1.2f;
        }

        // ---------------- Manto rojo morado de la espalda ----------------

        static void DibujarManto(ShadedCanvas c, Cuerpo b, Pose p, Efectos fx)
        {
            int g = c.NewGroup();
            float ph = p["tela"], vaiven = p["vaiven"], viento = Mathf.Clamp01(p["viento"]);
            float vuelo = Mathf.Sin(ph - 0.9f) * 2.4f * vaiven + viento * 22f;
            Vector2 a0 = b.U(-4f, 56f), a1 = b.U(-26f, 56f), a2 = b.U(-38f, 44f);
            Vector2 med = b.W(V(b.Cadera.x - 50f - vuelo * 0.6f, b.Cadera.y * 0.62f + viento * 10f));
            Vector2 bajoB = b.W(V(b.Cadera.x - 64f - vuelo * 1.3f, 2f + viento * 22f));
            Vector2 bajoF = b.W(V(b.Cadera.x - 22f - vuelo * 0.4f, 0f));
            var pts = new List<Vector2> { a0, a1, a2, med, bajoB };
            const int dientes = 7;
            for (int i = 1; i < dientes; i++)
            {
                float t = i / (float)dientes;
                float sube = i % 2 == 1 ? 4f + 7f * PixelCanvas.Hash(i, 2, 57) : 0f;
                pts.Add(Add(Mix(bajoB, bajoF, t), V(0f, sube + Mathf.Sin(ph + i) * 0.8f)));
            }
            pts.Add(bajoF);
            pts.Add(b.U(-20f, 8f));
            pts.Add(b.U(-12f, 40f));
            var tela = new Tela(pts.ToArray(), b.U(-20f, 54f), Mix(bajoB, bajoF, 0.5f), 26f, 4f, ph * 0.7f, 0.9f);
            tela.Region(c, null, Manto, 1.1f, g).WithBump(Patterns.Weave(2f, 0.25f), 0.35f);
            tela.Region(c, (s, r, x, y) => tela.Borde(x, y) < 1.7f, Ribete, 1.12f, g);
            foreach (float r in tela.Valles())
            {
                var linea = new List<Vector2>();
                float s0 = tela.Len * (0.15f + 0.2f * PixelCanvas.Hash((int)(r * 10f) + 3, 1, 5));
                for (int k = 0; k <= 4; k++)
                {
                    float s = Mathf.Lerp(s0, tela.Len * 1.05f, k / 4f);
                    linea.Add(tela.At(s, r * Mathf.Lerp(0.85f, 1.05f, k / 4f) + Mathf.Sin(ph * 0.8f + k * 1.2f + r) * 0.7f));
                }
                c.Fold(linea, g, 2, 1, Manto);
            }
            if (fx.TintaHasta > 0f)
            {
                float hasta = fx.TintaHasta;
                tela.Region(c, (s, r, x, y) => y < hasta + 5f * (PixelCanvas.ValueNoise(x / 5f, 3f, 0, 77) - 0.5f), Tinta, 1.13f, g);
            }
        }

        // ---------------- Cinturón de cuerda con amuletos ----------------

        static void DibujarCinturon(ShadedCanvas c, Cuerpo b, Pose p)
        {
            int g = c.NewGroup();
            var cuerda = new List<Vector2> { b.U(-31f, 11f), b.U(-14f, 8f), b.U(4f, 7f), b.U(25f, 9f) };
            c.Strand(cuerda, 1.9f, 1.9f, Cuerda, 2.6f, 0f, g);
            Vector2 nudo = b.U(20f, 8f);
            c.Ellipse(nudo, 3f, 2.6f, b.AnguloTronco, Cuerda, 2.62f, 0f, g);
            c.Crease(new[] { Add(nudo, V(-1.5f, 1.5f)), Add(nudo, V(1.5f, -1.5f)) }, 1, g, Cuerda);
            // Dos cabos colgando con nudos y borla, y amuletos: un ídolo de bronce y un cráneo de pez.
            float ph = p["tela"], viento = Mathf.Clamp01(p["viento"]);
            for (int k = 0; k < 2; k++)
            {
                var cabo = new List<Vector2> { Add(nudo, V(k * 2f - 1f, -1f)) };
                Vector2 q = cabo[0];
                for (int i = 1; i <= 5; i++)
                {
                    q = Add(q, Dir(-90f - 6f * k + Mathf.Sin(ph + i * 0.7f + k) * 6f - viento * 50f, 4.4f - k * 0.6f));
                    cabo.Add(q);
                }
                int gc = c.NewGroup();
                c.Strand(cabo, 1.2f, 1f, Cuerda, 2.64f - k * 0.02f, k == 1 ? -0.08f : 0f, gc);
                c.Ellipse(cabo[2], 1.8f, 1.6f, 0f, Cuerda, 2.65f, 0f, gc);
                var fin = cabo[cabo.Count - 1];
                if (k == 0)
                {
                    // Ídolo de bronce: un pulpito sentado.
                    int gi = c.NewGroup();
                    c.Ellipse(Add(fin, V(0f, -2.5f)), 2.3f, 2.8f, 0f, Bronce, 2.66f, 0f, gi);
                    for (int t = -1; t <= 1; t++) c.Capsule(Add(fin, V(t * 1.2f, -4.6f)), Add(fin, V(t * 2f, -7f)), 0.8f, 0.5f, Bronce, 2.665f, 0f, gi);
                    c.Dot(Add(fin, V(0.6f, -2f)), -2, gi, Bronce);
                    c.Glint(Add(fin, V(0.8f, -1.2f)), gi, 0, Bronce);
                }
                else
                {
                    int gh = c.NewGroup();
                    c.Ellipse(Add(fin, V(0f, -2f)), 2.6f, 1.8f, -20f, Hueso, 2.63f, -0.06f, gh);
                    c.Dot(Add(fin, V(0.8f, -1.8f)), -2, gh, Hueso);
                }
            }
        }

        // ---------------- Esclavina (la capucha cae sobre los hombros) ----------------

        static void DibujarEsclavina(ShadedCanvas c, Cuerpo b, Pose p, Efectos fx)
        {
            int g = c.NewGroup();
            var curva = Bezier(V(-34f, 33f), V(-4f, 8f), V(30f, 29f), 25);
            var bajo = new List<Vector2>();
            var pespunte = new List<Vector2>();
            for (int i = 0; i < curva.Length; i++)
            {
                float t = i / (float)(curva.Length - 1);
                float feston = 2.6f * Mathf.Abs(Mathf.Sin(t * Mathf.PI * 6f));
                bajo.Add(b.U(curva[i].x, curva[i].y - feston));
                pespunte.Add(b.U(curva[i].x, curva[i].y + 1.8f));
            }
            var contorno = new List<Vector2> { b.U(16f, 57f), b.U(-14f, 59f), b.U(-28f, 55f), b.U(-35f, 45f) };
            contorno.AddRange(bajo);
            contorno.Add(b.U(32f, 38f));
            contorno.Add(b.U(27f, 50f));
            var poly = contorno.ToArray();
            Vector2 cuello = b.U(0f, 62f);
            float ta = b.AnguloTronco;
            Entintar(Domo(c, poly, b.U(-2f, 36f), 38f, 26f, ta, Manto, 3f, g).Frame(cuello, ta)
                .WithBump((u, v) => Nervio(u, v, 8f, 0.8f), 0.7f)
                .WithDetail((u, v) => Mathf.Repeat(Mathf.Atan2(u, -v) * Mathf.Rad2Deg, 8f) < 1.1f ? -1 : 0), fx.TintaHasta);
            // Ribete rojo del festón con pespunte claro.
            Domo(c, poly, b.U(-2f, 36f), 38f, 26f, ta, Ribete, 3.01f, g, (x, y) => Distancia(bajo, x, y, false) < 3.4f);
            c.Stitch(pespunte, g, 1, 1, 2, Ribete);
            // Cuentas de hueso en el fondo de cada festón.
            for (int j = 0; j < 6; j++)
            {
                float t = (2 * j + 1) / 12f;
                var at = Add(bajo[Mathf.RoundToInt(t * (bajo.Count - 1))], V(0f, -1.6f));
                int gb = c.NewGroup();
                c.Ellipse(at, 1.2f, 1.3f, 0f, Hueso, 3.02f, 0f, gb);
            }
            // Broche de bronce con una piedra roja.
            Vector2 broche = b.U(29f, 37f);
            int gr = c.NewGroup();
            c.Ellipse(broche, 3.4f, 3.4f, 0f, Bronce, 3.05f, 0f, gr).WithBump(Patterns.Rivets(2.2f, 0.7f, 0f, 0f), 0.5f);
            c.Ellipse(broche, 1.8f, 1.9f, 0f, GemaApagada, 3.06f, 0f, gr);
            c.Glint(Add(broche, V(0.5f, 0.6f)), gr, 0, GemaApagada);
        }

        /// <summary>Nervaduras que nacen de un punto (la punta de la capucha, el cuello): cresta triangular cada <paramref name="paso"/> grados.</summary>
        static float Nervio(float u, float v, float paso, float alto)
        {
            float d = Mathf.Sqrt(u * u + v * v);
            if (d < 6f) return 0f;
            float f = Mathf.Repeat(Mathf.Atan2(u, -v) * Mathf.Rad2Deg, paso) / paso;
            return (1f - Mathf.Abs(f * 2f - 1f)) * alto * Mathf.Clamp01((d - 6f) / 6f);
        }

        // ---------------- Cabeza, capucha, ojos y barba ----------------

        /// <summary>Escala de la cara de pulpo respecto a la capucha (la cara manda; la capucha la enmarca).</summary>
        const float EscalaCara = 1.2f;

        /// <summary>Punto de la cara (coordenadas de cabeza, escaladas desde la base del sifón).</summary>
        static Vector2 Cara(Cuerpo b, float x, float y) => b.H(8f + (x - 8f) * EscalaCara, 8f + (y - 8f) * EscalaCara);

        static void DibujarCabeza(ShadedCanvas c, Cuerpo b, Pose p, Efectos fx)
        {
            int g = c.NewGroup();
            float ha = b.AnguloCabeza;
            float ojos = p["ojos"], grita = Mathf.Clamp01(p["grita"]);
            const float k = EscalaCara;
            Vector2 H(float x, float y) => b.H(x, y);
            Vector2 F(float x, float y) => Cara(b, x, y);

            // Fondo de la capucha: sombra honda entre la cara y el borde.
            c.Poly(new[] { H(1f, -10f), H(5f, 0f), H(8f, 10f), H(11f, 19f), H(15.5f, 26.5f), H(22f, 31f), H(28f, 33.5f), H(35f, 26f), H(37f, 4f), H(28f, -10f) },
                   ForroCapucha, 3.3f, 0f, 0f, c.NewGroup());
            DibujarBarba(c, b, p, false, fx);

            // Cabeza de pulpo: la masa de la cara (casi toda bajo la capucha) y el hocico redondo de donde nace la barba.
            Entintar(c.Ellipse(F(13f, 13f), 16f * k, 15f * k, ha - 12f, Piel, 3.4f, 0f, g)
             .WithBump(Patterns.Sum(Patterns.Lumps(3.2f, 1.2f, 61), Verrugas), 0.6f).WithDetail(Motas), fx.TintaHasta);
            Entintar(c.Ellipse(F(23f, 5.5f), 9.5f * k, 7.5f * k, ha - 20f, Piel, 3.42f, 0f, g)
             .WithBump(Patterns.Sum(Patterns.Lumps(2.4f, 1f, 71), Verrugas), 0.6f).WithDetail(Motas), fx.TintaHasta);
            // Sifón: el embudo carnoso bajo el ojo, con anillos y la boca oscura.
            c.Capsule(F(9.5f, 5f), F(5f, 0.5f), 2.6f * k, 2.1f * k, Piel, 3.43f, 0f, g).WithBump(Patterns.Ridges(1.8f, 0.5f), 0.7f);
            c.Ellipse(F(4.8f, 0.3f), 1.2f * k, 1f * k, ha, Boca, 3.44f, 0f, g);
            // Cejas abultadas (arcos con verrugas) sobre los ojos.
            c.Capsule(F(11f, 22.5f), F(24f, 22f), 3.2f * k, 2.4f * k, Piel, 3.45f, 0f, g).WithBump(Patterns.Lumps(1.6f, 1.3f, 67), 0.8f);
            c.Ridge(new[] { F(12f, 25f), F(18f, 25.4f), F(24f, 24f) }, 1, g, Piel);
            c.Capsule(F(25f, 21f), F(30f, 19f), 2.2f * k, 1.6f * k, Piel, 3.44f, 0f, g);

            // Cuatro ojos (dos grandes arriba, dos pequeños debajo; los del lado lejano asoman por el perfil).
            float calor = Mathf.Max(0f, ojos);
            float Abre(float retraso) => Mathf.Clamp01(ojos * 1.7f - retraso) * (ojos > 1.6f ? 0.95f : 0.74f);
            Ojo(c, F(17.5f, 17f), 3.7f * k, ha, Abre(0f), calor, g, 3.5f, fx.Muerto);
            Ojo(c, F(23.2f, 11f), 2.4f * k, ha, Abre(0.3f), calor, g, 3.5f, fx.Muerto);
            Ojo(c, F(28f, 17.5f), 2.3f * k, ha, Abre(0.15f), calor, g, 3.48f, fx.Muerto);
            Ojo(c, F(29.6f, 11.4f), 1.5f * k, ha, Abre(0.45f), calor, g, 3.48f, fx.Muerto);
            // Arrugas: bolsas bajo los ojos, pliegue de la mejilla y poros alrededor del sifón.
            c.Fold(new[] { F(13f, 12.5f), F(17.5f, 11.4f), F(21f, 13.5f) }, g, 2, 1, Piel);
            c.Fold(new[] { F(19.5f, 6.6f), F(23.5f, 6f), F(26f, 8f) }, g, 2, 1, Piel);
            c.Crease(new[] { F(8f, 18f), F(9f, 13f), F(12f, 8.5f) }, 1, g, Piel);
            c.Crease(new[] { F(11f, 27f), F(16f, 28f) }, 1, g, Piel);
            for (int i = 0; i < 5; i++) c.Dot(F(7f + i * 1.8f, 9f - (i % 2) * 1.2f), -2, g, Piel);
            c.Glint(F(26f, 9f), g, 0, Piel);
            c.Glint(F(20f, 24.5f), g, 0, Piel);

            if (grita > 0.15f) DibujarPico(c, b, grita, g);
            DibujarBarba(c, b, p, true, fx);
            DibujarCapucha(c, b, p, fx);
        }

        static float Verrugas(float u, float v)
        {
            const float cs = 4f;
            int ci = Mathf.FloorToInt(u / cs), cj = Mathf.FloorToInt(v / cs);
            if (PixelCanvas.Hash(ci, cj, 5) > 0.35f) return 0f;
            float cx = (ci + 0.3f + 0.4f * PixelCanvas.Hash(ci, cj, 6)) * cs, cy = (cj + 0.3f + 0.4f * PixelCanvas.Hash(ci, cj, 7)) * cs;
            float rr = 0.8f + 0.6f * PixelCanvas.Hash(ci, cj, 8);
            float d2 = ((u - cx) * (u - cx) + (v - cy) * (v - cy)) / (rr * rr);
            return d2 < 1f ? Mathf.Sqrt(1f - d2) : 0f;
        }

        /// <summary>Cromatóforos: manchas oscuras en grupos sobre la piel del pulpo.</summary>
        static int Motas(float u, float v) => PixelCanvas.ValueNoise(u / 2.4f, v / 2.4f, 0, 13) > 0.72f ? -1 : 0;

        /// <summary>
        /// Ojo de pulpo: cuenca de piel oscura, globo rojo que arde (emisivo) con la luz más viva abajo-delante, pupila en
        /// barra horizontal que se estrecha al arder, párpado que baja según <paramref name="abierto"/> y un reflejo húmedo.
        /// </summary>
        static void Ojo(ShadedCanvas c, Vector2 en, float r, float ang, float abierto, float calor, int g, float z, bool muerto)
        {
            c.Ellipse(en, r + 1.4f, r + 1.1f, ang, PielOscura, z, 0f, g).WithBump(Patterns.Lumps(1.4f, 0.8f, 19), 0.6f);
            if (muerto)
            {
                c.Ellipse(en, r, r * 0.9f, ang, OjoMuerto, z + 0.01f, 0f, g);
                return;
            }
            if (abierto <= 0.04f)
            {
                c.Ellipse(en, r + 0.3f, r * 0.95f, ang, Piel, z + 0.01f, 0f, g);
                c.Crease(new[] { Add(en, Dir(ang + 180f, r)), Add(en, Dir(ang - 90f, 0.6f)), Add(en, Dir(ang, r)) }, 2, g, Piel);
                return;
            }
            c.Ellipse(en, r, r * 0.92f, ang, OjoRojo, z + 0.01f, 0f, g);
            if (calor > 0.5f) c.Ellipse(Add(en, Dir(ang - 60f, r * 0.28f)), r * (0.5f + 0.12f * Mathf.Clamp01(calor - 1f)), r * 0.42f, ang, OjoVivo, z + 0.02f, 0f, g);
            if (calor > 1.5f) c.Ellipse(Add(en, Dir(ang - 60f, r * 0.3f)), r * 0.26f, r * 0.22f, ang, OjoBlanco, z + 0.025f, 0f, g);
            float alto = Mathf.Max(0.45f, r * (0.28f - 0.06f * calor));
            c.Ellipse(en, r * (0.82f - 0.12f * Mathf.Clamp01(calor - 1f)), alto, ang, Pupila, z + 0.03f, 0f, g);
            if (abierto < 0.97f)
            {
                float corte = r * (2f * abierto - 1f);
                float ca = Mathf.Cos(ang * Mathf.Deg2Rad), sa = Mathf.Sin(ang * Mathf.Deg2Rad);
                float rr = r + 0.5f;
                c.Custom(en.x - rr - 1f, en.y - rr - 1f, en.x + rr + 1f, en.y + rr + 1f, (px, py) =>
                {
                    float dx = px - en.x, dy = py - en.y;
                    float lx = dx * ca + dy * sa, ly = -dx * sa + dy * ca;
                    // El párpado baja en diagonal hacia delante: mirada torva.
                    return (lx * lx + ly * ly <= rr * rr && ly > corte - lx * 0.35f, new N3(0f, 0.6f, 0.8f));
                }, Piel, z + 0.04f, 0f, g);
            }
            HombrePez.Punto(c, Add(en, Rotate(V(r * 0.3f, r * 0.45f), V(0f, 0f), ang)), OjoBlanco, z + 0.05f, g);
        }

        /// <summary>El pico córneo y la boca roja que asoman cuando la barba se abre (grito).</summary>
        static void DibujarPico(ShadedCanvas c, Cuerpo b, float grita, int g)
        {
            float ha = b.AnguloCabeza;
            Vector2 H(float x, float y) => Cara(b, x, y);
            Vector2 bisagra = H(19f, 1f);
            float abre = grita * 24f;
            Vector2 J(float x, float y) => Rotate(H(x, y), bisagra, -abre);
            c.Ellipse(H(24f, -2.5f), 6.5f * EscalaCara, (2f + 4.5f * grita) * EscalaCara, ha - 10f, Boca, 3.44f, 0f, g);
            c.Poly(new[] { H(18f, 3f), H(27f, 2.6f), H(31.5f, -0.5f), H(30.5f, -4f), H(28.4f, -1.4f), H(19f, -0.4f) }, Pico, 3.47f, 1f, 0f, g);
            c.Poly(new[] { J(19f, -1f), J(27.5f, -2.4f), J(28f, -5.4f), J(20f, -4.6f) }, Pico, 3.46f, 1f, 0f, g);
            c.Glint(H(25.5f, 2f), g, 0, Pico);
            for (int i = 0; i < 4; i++) HombrePez.Punto(c, Mix(H(21f + i * 2f, -2.2f), J(21f + i * 2f, -2.6f), 0.5f), Hueso, 3.455f, g);
        }

        static readonly float[] BarbaX = { 8f, 11f, 14f, 17f, 20f, 23f, 26f, 29f, 31f, 13f, 22f };
        static readonly float[] BarbaY = { 3.5f, 2f, 0.6f, -0.2f, -0.8f, -1.2f, -1f, -0.2f, 1.4f, 1.2f, -0.4f };
        static readonly float[] BarbaLargo = { 44f, 54f, 62f, 68f, 72f, 66f, 58f, 48f, 38f, 52f, 62f };

        /// <summary>Barba de tentáculos: cuelgan por su peso, ondulan con desfase y se rizan en la punta; al gritar se abren.</summary>
        static void DibujarBarba(ShadedCanvas c, Cuerpo b, Pose p, bool delante, Efectos fx)
        {
            float ph = p["barba"], vaiven = p["vaiven"], viento = Mathf.Clamp01(p["viento"]), grita = Mathf.Clamp01(p["grita"]);
            for (int i = 0; i < BarbaX.Length; i++)
            {
                bool esDelante = i % 2 == 1;
                if (esDelante != delante) continue;
                int g = c.NewGroup();
                Vector2 a = Cara(b, BarbaX[i], BarbaY[i]);
                float ang = -90f + (BarbaX[i] - 19f) * 2.6f + (PixelCanvas.Hash(i, 3, 17) - 0.5f) * 14f + b.AnguloCabeza * 0.3f;
                float abierto = -90f + (BarbaX[i] - 19f) * 6f + (BarbaX[i] > 19f ? 34f : -34f);
                ang = Mathf.Lerp(ang, abierto, grita);
                ang = Mathf.Lerp(ang, -128f, viento * 0.55f);
                const int n = 13;
                float seg = BarbaLargo[i] * 1.12f * (1f - 0.25f * grita) / n;
                var pts = new List<Vector2> { a };
                for (int k = 1; k <= n; k++)
                {
                    float s = k / (float)n;
                    ang += Mathf.Sin(ph - s * 2.8f + i * 1.37f) * (3.5f + 7f * s) * vaiven + (i % 3 - 1) * 1.6f;
                    if (k >= n - 3) ang += (i % 2 == 0 ? 1f : -1f) * (22f + 22f * grita);
                    a = Add(a, Dir(ang, seg));
                    pts.Add(a);
                }
                float r0 = i >= 2 && i <= 6 ? 3.5f : 2.9f;
                Tentaculo(c, pts, r0, 0.5f, Barba, BarbaClara, esDelante ? 4f : 3.35f, esDelante ? 0f : -0.1f, g, i % 2 == 0 ? 1f : -1f, fx.TintaHasta);
            }
        }

        static void DibujarCapucha(ShadedCanvas c, Cuerpo b, Pose p, Efectos fx)
        {
            int g = c.NewGroup();
            float ha = b.AnguloCabeza;
            Vector2 H(float x, float y) => b.H(x, y);
            Vector2 punta = H(-1f, 60f);
            // Contorno: borde de la abertura (delante) y una aguja alta, un poco abombada detrás por el manto del pulpo.
            var borde = new[] { H(1.5f, -9.5f), H(5f, 0f), H(8f, 10f), H(11f, 19f), H(15.5f, 26.5f), H(22f, 31f), H(28f, 33.5f), H(33f, 34f) };
            var contorno = new List<Vector2>(borde)
            {
                H(30f, 38.5f), H(24f, 44f), H(17f, 49f), H(10f, 53.5f), H(4f, 57f), punta, H(-6f, 54f), H(-12f, 46f),
                H(-18f, 37f), H(-22f, 26f), H(-24f, 13f), H(-23f, 1f), H(-19f, -7f), H(-10f, -11.5f),
            };
            var poly = contorno.ToArray();
            Vector2 centro = H(0f, 22f);
            Manchas(Domo(c, poly, centro, 27f, 36f, ha, Capucha, 3.8f, g).Frame(punta, ha)
                .WithBump((u, v) => Nervio(u, v, 7.5f, 0.9f), 0.8f)
                .WithDetail((u, v) =>
                {
                    float d = Mathf.Sqrt(u * u + v * v);
                    if (d < 6f) return 1;
                    return d > 9f && Mathf.Repeat(Mathf.Atan2(u, -v) * Mathf.Rad2Deg, 7.5f) < 1f ? -1 : 0;
                }), fx.TintaHasta, 151);

            // Ribete rojo de la abertura con pespunte claro.
            var dentro = new Vector2[borde.Length];
            var medio = new Vector2[borde.Length];
            for (int i = 0; i < borde.Length; i++)
            {
                Vector2 t = Sub(borde[Mathf.Min(borde.Length - 1, i + 1)], borde[Mathf.Max(0, i - 1)]).normalized;
                Vector2 nn = V(-t.y, t.x);
                dentro[i] = Add(borde[i], Scale(nn, 3.4f));
                medio[i] = Add(borde[i], Scale(nn, 1.7f));
            }
            var tira = new List<Vector2>(borde);
            for (int i = dentro.Length - 1; i >= 0; i--) tira.Add(dentro[i]);
            Entintar(Domo(c, tira.ToArray(), centro, 27f, 36f, ha, Ribete, 3.82f, g), fx.TintaHasta);
            c.Stitch(medio, g, 1, 1, 2, Ribete);
            c.Crease(dentro, 1, g, Capucha);

            // Cinta roja que ciñe la capucha por detrás de la cara (sigue la curva de la cabeza), con pespunte claro.
            var cintaAbajo = new[] { H(10.5f, 16f), H(4f, 13.5f), H(-4f, 11.5f), H(-13f, 10.5f), H(-23.6f, 10.5f) };
            var cintaArriba = new[] { H(-23.8f, 15.5f), H(-13f, 15.5f), H(-4f, 16.5f), H(4f, 18.5f), H(11.5f, 21f) };
            var cinta = new List<Vector2>(cintaAbajo);
            cinta.AddRange(cintaArriba);
            Entintar(Domo(c, cinta.ToArray(), centro, 27f, 36f, ha, Ribete, 3.81f, g, (x, y) => DentroPoligono(poly, x, y)), fx.TintaHasta);
            var cintaMedio = new Vector2[cintaAbajo.Length];
            for (int i = 0; i < cintaAbajo.Length; i++) cintaMedio[i] = Mix(cintaAbajo[i], cintaArriba[cintaAbajo.Length - 1 - i], 0.5f);
            c.Stitch(cintaMedio, g, 1, 2, 2, Ribete);

            // Ojo bordado en el costado; arde cuando el jefe se enciende.
            float ojos = p["ojos"];
            DibujarSigno(c, H(-8f, 25f), ha, ojos > 1.75f && !fx.Muerto ? Runa : RibeteOscuro, 3.83f, g);

            // Remate de hueso en la punta (una uña curva) con su anilla de bronce.
            int gp = c.NewGroup();
            c.Ellipse(punta, 2.3f, 1.9f, ha, Bronce, 3.85f, 0f, gp);
            c.Strand(new[] { H(-1f, 59.5f), H(-1.8f, 62.5f), H(-2.8f, 65.5f) }, 1.7f, 0.5f, Hueso, 3.86f, 0f, gp);
            c.Glint(Add(punta, V(0.6f, 0.6f)), gp, 0, Bronce);
        }

        /// <summary>Ojo bordado (o encendido) centrado en <paramref name="centro"/>, girado con la cabeza.</summary>
        static void DibujarSigno(ShadedCanvas c, Vector2 centro, float ang, PixelMaterial m, float z, int g)
        {
            float ca = Mathf.Cos(ang * Mathf.Deg2Rad), sa = Mathf.Sin(ang * Mathf.Deg2Rad);
            c.Custom(centro.x - 9f, centro.y - 11f, centro.x + 9f, centro.y + 7f, (px, py) =>
            {
                float dx = px - centro.x, dy = py - centro.y;
                float u = dx * ca + dy * sa, v = -dx * sa + dy * ca;
                float almendra = Mathf.Abs(u) / 5.2f + Mathf.Abs(v) / 2.6f;
                bool linea = almendra > 0.74f && almendra < 1.06f;
                linea |= u * u + v * v < 1.6f;
                return (linea, N3.Front);
            }, m, z, 0f, g);
        }

        // ---------------- Brazos ----------------

        static (Vector2 hombro, Vector2 codo, Vector2 mano) PuntosBrazo(Cuerpo b, Pose p, bool delante)
        {
            Vector2 hombro = delante ? b.U(16f, 46f) : b.U(-22f, 48f);
            float a1 = (delante ? p["brazoD1"] : p["brazoT1"]) + b.Inclina + b.Caida;
            float a2 = (delante ? p["brazoD2"] : p["brazoT2"]) + b.Inclina + b.Caida;
            Vector2 codo = Limb(hombro, a1, 28f);
            return (hombro, codo, Limb(codo, a2, 26f));
        }

        static void DibujarBrazo(ShadedCanvas c, Cuerpo b, Pose p, bool delante, Efectos fx)
        {
            int g = c.NewGroup();
            var (hombro, codo, mano) = PuntosBrazo(b, p, delante);
            float z = delante ? 3.9f : 1.5f, shade = delante ? 0f : -0.13f;
            Vector2 muneca = Mix(codo, mano, 0.62f);
            Vector2 ua = Sub(codo, hombro).normalized, ub = Sub(muneca, codo).normalized;
            Vector2 na = V(-ua.y, ua.x), nb = V(-ub.y, ub.x);
            // Manga ancha y acampanada de la túnica, con pliegues que caen del hombro y se arrugan en el codo.
            Entintar(c.Capsule(hombro, codo, 8f, 7f, Tunica, z, shade, g), fx.TintaHasta);
            Entintar(c.Capsule(codo, muneca, 7f, 8.4f, Tunica, z + 0.02f, shade, g), fx.TintaHasta);
            c.Fold(new[] { Add(hombro, Scale(na, 5f)), Add(Mix(hombro, codo, 0.55f), Scale(na, 3f)), Add(codo, Scale(na, -1.5f)) }, g, 2, 1, Tunica);
            c.Fold(new[] { Add(hombro, Scale(na, -3.5f)), Add(Mix(hombro, codo, 0.7f), Scale(na, -4f)) }, g, 2, 1, Tunica);
            for (int i = 0; i < 3; i++)
            {
                Vector2 m = Add(codo, Scale(ua, -2.5f + i * 2.4f));
                c.Fold(new[] { Add(m, Scale(na, -5f)), Add(Add(m, Scale(ua, 1.2f)), Scale(na, -1.5f)), Add(m, Scale(na, 1.6f)) }, g, 2, 1, Tunica);
            }
            c.Fold(new[] { Add(Mix(codo, muneca, 0.25f), Scale(nb, 3f)), Add(Mix(codo, muneca, 0.9f), Scale(nb, 7f)) }, g, 2, 1, Tunica);
            c.Fold(new[] { Add(Mix(codo, muneca, 0.35f), Scale(nb, -2.6f)), Add(Mix(codo, muneca, 0.92f), Scale(nb, -6.5f)) }, g, 2, 1, Tunica);
            // Puño: ribete rojo con ojos bordados y la boca oscura de la manga.
            Vector2 puno0 = Mix(codo, muneca, 0.8f);
            float punoLen = Sub(muneca, puno0).magnitude;
            c.Capsule(puno0, muneca, 8.2f, 8.6f, Ribete, z + 0.03f, shade, g).WithDetail((u, v) =>
            {
                if (u < 0.9f || u > punoLen - 0.9f) return -1;
                float cv = Mathf.Repeat(v + 0.5f, 5f) - 2.5f, cu = u - punoLen * 0.5f;
                float d = Mathf.Abs(cv) / 2f + Mathf.Abs(cu) / 1.2f;
                if (d > 0.7f && d < 1.1f) return 2;
                return 0;
            });
            float angB = Mathf.Atan2(ub.y, ub.x) * Mathf.Rad2Deg;
            c.Ellipse(Add(muneca, Scale(ub, 0.6f)), 2f, 6.6f, angB, Forro, z + 0.04f, shade, g);
            // Mano de tentáculos.
            int modo = delante ? (fx.CaeBaculo == 0 ? 1 : 0) : Mathf.RoundToInt(p["mano"]);
            Vector2 asta = delante ? Sub(ExtremosBaculo(b, p).arriba, ExtremosBaculo(b, p).abajo).normalized : V(0f, 1f);
            DibujarMano(c, mano, ub, delante ? 5.2f : z + 0.06f, shade, modo, asta, fx);
        }

        /// <summary>
        /// Mano de pulpo: palma carnosa y cuatro dedos-tentáculo con ventosas. Modo 0: sueltos y rizados; 1: agarran el asta
        /// (dirección <paramref name="asta"/>); 2: abiertos hacia el suelo, conjurando (con un anillo de runas encendido).
        /// </summary>
        static void DibujarMano(ShadedCanvas c, Vector2 mano, Vector2 dir, float z, float shade, int modo, Vector2 asta, Efectos fx)
        {
            int g = c.NewGroup();
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            c.Ellipse(mano, 5f, 4.2f, ang, Piel, z, shade, g).WithBump(Verrugas, 0.5f).WithDetail(Motas);
            if (modo == 1)
            {
                Vector2 sp = V(-asta.y, asta.x);
                if (sp.x > 0f) sp = Scale(sp, -1f);
                for (int j = 0; j < 4; j++)
                {
                    Vector2 o = Add(mano, Scale(asta, (j - 1.2f) * 2.7f));
                    var dedo = new[] { Add(o, Scale(sp, 3.4f)), Add(Add(o, Scale(sp, 0.6f)), Scale(asta, 0.6f)), Add(Add(o, Scale(sp, -2.2f)), Scale(asta, 0.3f)),
                                       Add(Add(o, Scale(sp, -3.2f)), Scale(asta, -1.2f)), Add(Add(o, Scale(sp, -2.3f)), Scale(asta, -2.6f)) };
                    int gd = c.NewGroup();
                    c.Strand(dedo, 1.6f, 0.7f, Piel, z + 0.1f + j * 0.005f, shade, gd);
                    c.Dot(dedo[2], 1, gd, Piel);
                }
                return;
            }
            float baseAng = modo == 2 ? -90f : ang - 10f;
            for (int j = 0; j < 4; j++)
            {
                float fa = baseAng + (j - 1.5f) * (modo == 2 ? 26f : 18f);
                var dedo = new List<Vector2> { Add(mano, Dir(fa, 3f)) };
                Vector2 q = dedo[0];
                for (int k = 1; k <= 4; k++)
                {
                    fa += (modo == 2 ? (j < 2 ? -9f : 9f) : 16f) + Mathf.Sin(fx.Fase * Mathf.PI * 4f + j + k) * 5f;
                    q = Add(q, Dir(fa, 2.6f));
                    dedo.Add(q);
                }
                int gd = c.NewGroup();
                Tentaculo(c, dedo, 1.6f, 0.5f, Piel, BarbaClara, z + 0.02f, shade, gd, 1f, fx.TintaHasta);
            }
            if (modo == 2 && fx.ManoBrilla > 0f)
            {
                // Anillo de runas que cae de la palma al suelo.
                Vector2 q = Add(mano, V(0f, -10f));
                float r = 6f + 2f * Mathf.Sin(fx.Fase * Mathf.PI * 4f);
                c.Custom(q.x - r - 1f, q.y - r - 1f, q.x + r + 1f, q.y + r + 1f, (px, py) =>
                {
                    float dx = (px - q.x) / r, dy = (py - q.y) / (r * 0.35f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    return (d > 0.75f && d < 1.08f, N3.Front);
                }, Runa, z + 0.2f);
                for (int i = 0; i < 5; i++)
                {
                    float fr = Mathf.Repeat(fx.Fase * 2f + i * 0.2f, 1f);
                    c.Ellipse(Add(mano, V((i - 2) * 3f, -4f - fr * 30f)), 0.7f, 0.9f, 0f, fr < 0.5f ? Chispa : Runa, z + 0.2f);
                }
            }
        }

        // ---------------- Tridente ----------------

        static (Vector2 abajo, Vector2 arriba) ExtremosBaculo(Cuerpo b, Pose p)
        {
            var (_, _, mano) = PuntosBrazo(b, p, true);
            if (p["apoya"] > 0.5f)
            {
                Vector2 abajo = V(mano.x + 5f, 0f);
                Vector2 d = Sub(mano, abajo).normalized;
                return (abajo, Add(abajo, Scale(d, LargoBaculo)));
            }
            Vector2 dir = Dir(p["baculo"] - b.Caida);
            // El regatón nunca se hunde en el suelo: si haría falta, el asta resbala entre los dedos.
            float agarre = p["agarre"];
            if (dir.y > 0.05f) agarre = Mathf.Min(agarre, (mano.y - 2.5f) / dir.y);
            return (Sub(mano, Scale(dir, agarre)), Add(mano, Scale(dir, LargoBaculo - agarre)));
        }

        /// <summary>Centro de la gema: engastada entre las púas, 9 px por encima del final del asta.</summary>
        static Vector2 Gema((Vector2 abajo, Vector2 arriba) e) => Add(e.arriba, Scale(Sub(e.arriba, e.abajo).normalized, 9f));

        static void DibujarTridente(ShadedCanvas c, Pose p, Efectos fx, (Vector2 abajo, Vector2 arriba) e)
        {
            int g = c.NewGroup();
            Vector2 abajo = e.abajo, arriba = e.arriba;
            Vector2 d = Sub(arriba, abajo).normalized, lado = V(-d.y, d.x);
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            float largo = Sub(arriba, abajo).magnitude;
            Vector2 At(float u, float v) => Add(Add(abajo, Scale(d, u)), Scale(lado, v));

            // Asta de madera de deriva ennegrecida: veta en espiral y nudos.
            c.Capsule(abajo, arriba, 2.3f, 2.8f, Madera, 5f, 0f, g)
             .WithDetail((u, v) => Mathf.Repeat(u * 0.45f + v * 1.4f, 4.2f) < 0.9f ? -1 : PixelCanvas.ValueNoise(u / 6f, v * 0.8f, 0, 97) > 0.8f ? 1 : 0);
            // Regatón de bronce.
            c.Capsule(abajo, At(7f, 0f), 1.3f, 2.7f, BronceOscuro, 5.02f, 0f, g).WithBump(Patterns.Ridges(2f, 0.6f), 1f);
            // Vendas rojas en espiral sobre la empuñadura alta, con un cabo suelto colgando.
            for (int i = 0; i < 7; i++)
            {
                float u = largo * 0.6f + i * 3.2f;
                c.Capsule(At(u - 1.2f, -2.5f), At(u + 1.2f, 2.5f), 1.15f, 1.15f, Ribete, 5.03f, 0f, g);
            }
            var cabo = new List<Vector2> { At(largo * 0.6f + 20f, 2f) };
            Vector2 q = cabo[0];
            for (int k = 1; k <= 5; k++)
            {
                q = Add(q, Dir(-95f + Mathf.Sin(p["tela"] + k * 0.9f) * 12f - Mathf.Clamp01(p["viento"]) * 70f, 3f));
                cabo.Add(q);
            }
            c.Strand(cabo, 1.2f, 0.8f, Ribete, 5.01f, -0.05f, g);
            // Anillos de bronce con verdín.
            foreach (float k in new[] { 0.24f, 0.46f, 0.86f })
            {
                Vector2 r0 = At(largo * k, 0f);
                int gr = c.NewGroup();
                c.Capsule(Sub(r0, Scale(d, 1.6f)), Add(r0, Scale(d, 1.6f)), 3f, 3f, Bronce, 5.04f, 0f, gr)
                 .WithBump(Patterns.Ridges(1.6f, 0.5f), 1f).WithTint(Verdin, 0.35f, Patterns.Spots(1.6f, 0.6f, 13));
                c.Glint(Add(r0, Scale(lado, -1.4f)), gr, 0, Bronce);
            }

            // Cabeza: collarín, media luna con cuernos, dos púas laterales curvas y la central, todas con lengüeta.
            int gh = c.NewGroup();
            c.Capsule(At(largo - 5f, 0f), At(largo + 3f, 0f), 3f, 3.4f, Bronce, 5.05f, 0f, gh).WithBump(Patterns.Ridges(1.8f, 0.6f), 1f);
            for (int s = -1; s <= 1; s += 2)
            {
                var cuerno = new List<Vector2>();
                for (int i = 0; i <= 5; i++)
                {
                    float t = i / 5f;
                    cuerno.Add(At(largo + 4f + 6f * t * t, 11f * t * s));
                }
                c.Strand(cuerno, 2.6f, 1.5f, Bronce, 5.06f, 0f, gh);
                var pua = new List<Vector2> { At(largo + 10f, 11f * s), At(largo + 16f, 12f * s), At(largo + 22f, 11f * s), At(largo + 27f, 8.5f * s) };
                c.Strand(pua, 1.9f, 1.1f, Bronce, 5.06f, 0f, gh);
                Punta(c, pua[3], Sub(pua[3], pua[2]).normalized, 2.3f, 6f, s, BronceOscuro, 5.07f, gh);
                c.Ridge(new[] { At(largo + 11f, 10f * s), At(largo + 21f, 10f * s) }, 1, gh, Bronce);
                // Amuleto de hueso colgando del cuerno.
                Vector2 h0 = At(largo + 9f, 11.5f * s);
                Vector2 h1 = Add(h0, Dir(-90f + Mathf.Sin(p["tela"] + s) * 10f, 5f));
                int ga = c.NewGroup();
                c.Capsule(h0, h1, 0.5f, 0.5f, Cuerda, 4.98f, 0f, ga);
                c.Ellipse(Add(h1, V(0f, -1.5f)), 1.3f, 2f, 0f, Hueso, 4.99f, 0f, ga);
            }
            c.Capsule(At(largo + 3f, 0f), At(largo + 30f, 0f), 2.1f, 1.4f, Bronce, 5.055f, 0f, gh);
            Punta(c, At(largo + 30f, 0f), d, 2.8f, 8f, 0, BronceOscuro, 5.07f, gh);
            c.Glint(At(largo + 2f, -2f), gh, 1, Bronce);

            // Jaula de la gema: dos varillas que la abrazan.
            float orb = p["gema"];
            Vector2 gema = Gema(e);
            float rj = Mathf.Max(3.2f, 2.4f * Mathf.Min(orb, 1.4f) + 1.8f);
            for (int s = -1; s <= 1; s += 2)
            {
                var vara = new List<Vector2>();
                for (int i = 0; i <= 6; i++)
                {
                    float t = i / 6f;
                    vara.Add(At(largo + 3f + t * 12f, s * (1.2f + Mathf.Sin(t * Mathf.PI) * rj)));
                }
                c.Strand(vara, 1f, 0.8f, BronceOscuro, 5.12f, 0f, gh);
            }
            // La gema-ojo: brasa roja con rendija de pupila; al cargar crece y la pupila se pierde en la luz.
            if (orb > 0.05f)
            {
                if (orb > 1.3f) c.Ellipse(gema, 4f * orb + 4f, 4f * orb + 4f, 0f, GemaHalo, 0.3f);
                c.Ellipse(gema, 2.4f * orb + 1.2f, 2.4f * orb + 1.2f, 0f, GemaFuera, 5.1f);
                c.Ellipse(gema, 1.8f * orb + 0.4f, 1.8f * orb + 0.4f, 0f, GemaMedia, 5.11f);
                if (orb > 0.6f) c.Ellipse(gema, 0.9f * orb, 0.9f * orb, 0f, GemaNucleo, 5.115f);
                if (orb < 1.7f) c.Ellipse(gema, 1.5f * Mathf.Max(0.8f, orb), 0.55f, ang, Pupila, 5.118f);
            }
            else c.Ellipse(gema, 2.4f, 2.4f, 0f, GemaApagada, 5.1f, 0f, c.NewGroup());

            if (fx.Destello > 0)
            {
                float s = fx.Destello == 2 ? 1f : 0.55f;
                c.Ellipse(gema, 18f * s, 18f * s, 0f, GemaHalo, 0.3f);
                c.Ellipse(gema, 9f * s, 9f * s, 0f, GemaMedia, 7.5f);
                c.Ellipse(gema, 5f * s, 5f * s, 0f, GemaNucleo, 7.6f);
                for (int i = 0; i < 6; i++)
                {
                    float a = i * 60f + 15f;
                    c.Capsule(Add(gema, Dir(a, 8f * s)), Add(gema, Dir(a, (i % 2 == 0 ? 26f : 17f) * s)), 1.3f * s, 0.3f, Runa, 7.4f);
                }
            }
        }

        /// <summary>Punta de lanza con lengüeta (barba) hacia atrás en el lado <paramref name="lengueta"/> (0 = en los dos).</summary>
        static void Punta(ShadedCanvas c, Vector2 base0, Vector2 dir, float ancho, float largo, int lengueta, PixelMaterial m, float z, int g)
        {
            Vector2 lado = V(-dir.y, dir.x);
            c.Poly(new[] { Add(base0, Scale(dir, largo)), Add(base0, Scale(lado, ancho)), Sub(base0, Scale(dir, 1.5f)), Sub(base0, Scale(lado, ancho)) }, Bronce, z, 1.2f, 0f, g);
            c.Ridge(new[] { Add(base0, Scale(dir, largo - 1f)), Add(base0, Scale(dir, 0.5f)) }, 1, g, Bronce);
            for (int s = -1; s <= 1; s += 2)
            {
                if (lengueta != 0 && s != -lengueta) continue;
                Vector2 b0 = Add(base0, Scale(lado, ancho * 0.8f * s));
                c.Capsule(b0, Add(Sub(b0, Scale(dir, 3.5f)), Scale(lado, 1.4f * s)), 0.9f, 0.35f, m, z + 0.01f, 0f, g);
            }
        }

        static void DibujarTridenteCaido(ShadedCanvas c, Efectos fx)
        {
            // Se le escapa de la mano y cae hacia atrás girando sobre el regatón; rebota una vez.
            int k = fx.CaeBaculo - 1;
            float t = Mathf.Min(1f, k / 5f);
            float ang = Mathf.Lerp(96f, 176f, t * t);
            if (k == 6) ang = 171f;
            Vector2 abajo = V(74f + 3f * t, 2f);
            var falsa = new Pose().With(("gema", Mathf.Max(0f, 1f - k / 4f)), ("tela", k * 0.7f));
            DibujarTridente(c, falsa, new Efectos(), (abajo, Add(abajo, Scale(Dir(ang), LargoBaculo))));
        }

        // ---------------- Efectos ----------------

        /// <summary>
        /// El susurro: brasas rojas que rondan la cabeza (por detrás del cuerpo) dejando una estela corta que se apaga;
        /// giran despacio y parpadean. Sin formas de letra: a ×1 son chispas, no escritura.
        /// </summary>
        static void DibujarRunas(ShadedCanvas c, Cuerpo b, Efectos fx)
        {
            int n = Mathf.RoundToInt(3f + fx.Runas * 5f);
            Vector2 centro = b.H(4f, 26f);
            for (int i = 0; i < n; i++)
            {
                float giro = (i % 2 == 0 ? 1f : -1f) * 90f;
                float a0 = i * (360f / n) + PixelCanvas.Hash(i, 1, 9) * 30f;
                float r = 50f + 16f * PixelCanvas.Hash(i, 2, 9);
                Vector2 Pos(float fase)
                {
                    float a = (a0 + fase * giro) * Mathf.Deg2Rad;
                    return Add(centro, V(Mathf.Cos(a) * r, Mathf.Sin(a) * r * 0.75f + Mathf.Sin(fase * Mathf.PI * 2f + i) * 4f));
                }
                Vector2 q = Pos(fx.Fase), q1 = Pos(fx.Fase - 0.05f), q2 = Pos(fx.Fase - 0.1f);
                bool vivo = PixelCanvas.Hash(i, Mathf.FloorToInt(fx.Fase * 8f), 11) > 0.3f;
                c.Capsule(q2, q1, 0.45f, 0.5f, RunaTenue, 0.33f);
                c.Capsule(q1, q, 0.5f, 0.7f, vivo ? Runa : RunaTenue, 0.34f);
                if (vivo) c.Ellipse(q, 0.9f, 0.9f, 0f, Chispa, 0.35f);
            }
        }

        /// <summary>Carga de las esferas: chispas que giran y convergen hacia la gema.</summary>
        static void DibujarConverge(ShadedCanvas c, Vector2 gema, Efectos fx)
        {
            for (int i = 0; i < 12; i++)
            {
                float fr = Mathf.Repeat(fx.Fase * 2.5f + i / 12f, 1f);
                float a = i * 67f + fr * 120f;
                Vector2 q = Add(gema, Dir(a, 7f + 30f * (1f - fr)));
                float r = 0.6f + 0.5f * fr;
                c.Ellipse(q, r, r, 0f, fr > 0.6f ? Chispa : Runa, 7f);
            }
        }

        static void DibujarCharco(ShadedCanvas c, Cuerpo b, Efectos fx)
        {
            float k = Mathf.Clamp01(fx.Charco);
            float cx = b.Cadera.x - 6f;
            float rx = 18f + 60f * k, ry = 2f + 3f * k;
            int g = c.NewGroup();
            c.Custom(cx - rx - 1f, -ry - 1f, cx + rx + 1f, ry + 2f, (px, py) =>
            {
                float lx = (px - cx) / rx, ly = (py - 0.5f) / ry;
                float borde = 1f - 0.2f * PixelCanvas.ValueNoise(px / 6f, 0f, 0, 131);
                if (lx * lx + ly * ly > borde) return (false, N3.Front);
                return (true, new N3(lx * 0.3f, 0.75f, 0.6f).Normalized());
            }, Tinta, 9f, 0f, g);
            float hm = fx.Monticulo;
            if (hm > 0.5f)
            {
                // Montón de tinta que borbotea donde se hunde el cuerpo (tapa lo que se disuelve).
                float mx = cx + 8f, mr = 18f + 12f * k;
                c.Custom(mx - mr - 1f, -1f, mx + mr + 1f, hm + 4f, (px, py) =>
                {
                    float lx = (px - mx) / mr;
                    if (lx * lx >= 1f || py < 0f) return (false, N3.Front);
                    float top = hm * Mathf.Sqrt(1f - lx * lx) + 2f * (PixelCanvas.ValueNoise(px / 4f, hm, 0, 137) - 0.5f) + ry * 0.6f;
                    if (py > top) return (false, N3.Front);
                    return (true, new N3(lx * 0.8f, 0.6f, 0.5f).Normalized());
                }, Tinta, 9.05f, 0f, g);
            }
            if (fx.Espuma <= 0f) return;
            int fotograma = Mathf.FloorToInt(fx.Fase * 16f);
            int n = Mathf.RoundToInt(16f + 14f * k);
            for (int i = 0; i < n; i++)
            {
                if (PixelCanvas.Hash(i, fotograma, 9) < 0.3f) continue;
                float x = cx + (PixelCanvas.Hash(i, 1, 7) * 2f - 1f) * rx * 0.85f;
                float r = 0.7f + 1.1f * PixelCanvas.Hash(i, fotograma, 8);
                float y = 0.5f + ry * 0.7f + r * 0.6f;
                float dm = (x - cx - 8f) / (18f + 12f * k);
                if (hm > 0.5f && dm * dm < 1f) y += hm * Mathf.Sqrt(1f - dm * dm) * 0.9f;
                int gb = c.NewGroup();
                c.Ellipse(V(x, y), r, r * 0.9f, 0f, Espuma, 9.1f, 0f, gb);
                if (r > 1.2f) c.Dot(V(x, y), -1, gb, Espuma);
            }
            // Burbujas que suben y revientan.
            for (int i = 0; i < 4; i++)
            {
                float fr = Mathf.Repeat(fx.Fase * 3f + i * 0.27f, 1f);
                float x = cx + (PixelCanvas.Hash(i, 4, 7) * 2f - 1f) * rx * 0.5f;
                c.Ellipse(V(x, 4f + fr * 14f), 1f, 1f, 0f, Espuma, 9.2f, 0f, c.NewGroup());
            }
        }

        static void DibujarPolvo(ShadedCanvas c, Cuerpo b, Efectos fx)
        {
            int n = fx.Polvo == 2 ? 8 : 4;
            for (int i = 0; i < n; i++)
            {
                float lado = i % 2 == 0 ? 1f : -1f;
                float x = b.Cadera.x + lado * (40f + i * 7f) + 4f;
                float r = (fx.Polvo == 2 ? 6.5f : 4.5f) - i * 0.4f;
                c.Ellipse(V(x, r * 0.6f), r * 1.4f, r, 0f, Polvo, 8f);
            }
        }

        static void DibujarVelocidad(ShadedCanvas c, Cuerpo b, Efectos fx)
        {
            for (int i = 0; i < 6; i++)
            {
                float y = 18f + i * 24f + (i % 2) * 6f;
                float x1 = b.Cadera.x - 56f - (i * 7 + (int)(fx.Fase * 16f)) % 12;
                float len = 18f + (i % 3) * 10f;
                float yy = y;
                c.Custom(x1 - len, yy - 1f, x1, yy + 1f, (px, py) => (Mathf.Abs(py - yy) < 0.6f, N3.Front), Estela, 0.5f);
            }
        }

        static void DibujarAura(ShadedCanvas c, Cuerpo b, Efectos fx)
        {
            Vector2 centro = b.H(2f, 24f);
            float r = 40f + 6f * Mathf.Sin(fx.Fase * Mathf.PI * 6f);
            c.Ellipse(centro, r, r * 1.1f, 0f, Aura, 0.1f);
        }
    }
}
