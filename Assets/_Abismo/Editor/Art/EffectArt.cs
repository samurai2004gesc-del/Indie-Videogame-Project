using System.Collections.Generic;
using UnityEngine;

namespace Abismo.EditorTools
{
    /// <summary>
    /// Efectos de combate pintados por código: chispazo de golpe, sangre, estrella de daño, bloqueo, polvo,
    /// estallido del Signo Antiguo, impacto de ejecución y la muerte de las criaturas.
    /// Todos se dibujan para un golpe que viaja HACIA LA DERECHA (+x): el juego los voltea.
    /// Se pintan sin luces (unlit) y con colores planos por bandas: cada efecto acumula un "campo de calor"
    /// que luego se cuantiza a 3-5 tonos (sin antialiasing borroso; el bloom de URP hace brillar los claros).
    /// </summary>
    public static class EffectArt
    {
        /// <summary>Todas las animaciones de efectos de combate. Se dibujan SIN luces (unlit), con mezcla alfa normal; el bloom de URP hace brillar los píxeles claros.</summary>
        public static List<PropAnimation> All() => new List<PropAnimation>
        {
            Chispazo(), Sangre(), Estrella(), Bloqueo(), Polvo(), Signo(), Ejecucion(), Muerte(),
        };

        // ------------------------------------------------------------------
        // Paletas
        // ------------------------------------------------------------------

        static Color32 Hex(string h) => PixelCanvas.Hex(h);

        /// <summary>Fuego del chispazo y de la ejecución: de la brasa oscura al blanco.</summary>
        static readonly Color32[] Fuego = { Hex("c2410c"), Hex("ff7a1a"), Hex("ffb22e"), Hex("ffe066"), Hex("fffbe6") };
        static readonly float[] UmbralFuego = { 0.10f, 0.27f, 0.45f, 0.63f, 0.83f };

        static readonly Color32[] Hielo = { Hex("4f8fd0"), Hex("9fd8ff"), Hex("e8f6ff") };
        static readonly float[] UmbralHielo = { 0.12f, 0.38f, 0.68f };

        static readonly Color32[] Signo4 = { Hex("0f6f62"), Hex("2fbfa0"), Hex("7dffd8"), Hex("e9fff8") };
        static readonly float[] UmbralSigno = { 0.12f, 0.34f, 0.58f, 0.82f };

        static readonly Color32 SangreOscura = Hex("5a0a10"), SangreMedia = Hex("8e1218"), SangreViva = Hex("c41e26"), SangreBrillo = Hex("e8484c");
        static readonly Color32 IcorBase = Hex("1f2c24"), IcorLuz = Hex("4a6650");

        static PropAnimation Nueva(string name, float fps, float px, float py) =>
            new PropAnimation { Name = name, Fps = fps, Pivot01 = new Vector2(px, py), Unlit = true, Normals = null };

        static float Rnd(int i, int k, int seed) => PixelCanvas.Hash(i, k, seed);

        const float Grado = Mathf.PI / 180f;

        // ------------------------------------------------------------------
        // Campo de calor y primitivas
        // ------------------------------------------------------------------

        /// <summary>Intensidad por píxel (0 = vacío, 1 = blanco). Las primitivas se combinan con el máximo.</summary>
        sealed class Campo
        {
            public readonly int W, H;
            public readonly float[] V;
            public Campo(int w, int h) { W = w; H = h; V = new float[w * h]; }
            public float Get(int x, int y) => x >= 0 && y >= 0 && x < W && y < H ? V[y * W + x] : 0f;
            public void Max(int x, int y, float v)
            {
                if (x < 0 || y < 0 || x >= W || y >= H) return;
                int i = y * W + x;
                if (v > V[i]) V[i] = v;
            }
            public void Poner(int x, int y, float v)
            {
                if (x < 0 || y < 0 || x >= W || y >= H) return;
                V[y * W + x] = v;
            }
        }

        /// <summary>Cuantiza el campo a la paleta (umbral mínimo de cada color, de frío a caliente).</summary>
        static void Pintar(PixelCanvas c, Campo campo, Color32[] colores, float[] umbrales)
        {
            for (int i = 0; i < campo.V.Length; i++)
            {
                float v = campo.V[i];
                if (v < umbrales[0]) continue;
                int k = 0;
                while (k + 1 < colores.Length && v >= umbrales[k + 1]) k++;
                c.Pixels[i] = colores[k];
            }
        }

        /// <summary>
        /// Púa: aguja de A a B, ancha cerca de la base y afilada en la punta B. El calor va de la base a la punta y
        /// cae hacia los lados (el eje queda como un núcleo más claro). <paramref name="trozos"/> &gt; 0 la rompe en
        /// segmentos (brasas) que ocupan la fracción <paramref name="lleno"/> de cada trozo.
        /// </summary>
        static void Pua(Campo c, float ax, float ay, float bx, float by, float ancho, float calorBase, float calorPunta,
            float caida = 0.3f, float cintura = 0.18f, float trozos = 0f, float lleno = 1f, float fase = 0f)
        {
            float dx = bx - ax, dy = by - ay;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            if (len < 0.5f) return;
            float ux = dx / len, uy = dy / len;
            float pad = ancho * 0.5f + 1f;
            int x0 = Mathf.FloorToInt(Mathf.Min(ax, bx) - pad), x1 = Mathf.CeilToInt(Mathf.Max(ax, bx) + pad);
            int y0 = Mathf.FloorToInt(Mathf.Min(ay, by) - pad), y1 = Mathf.CeilToInt(Mathf.Max(ay, by) + pad);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float px = x + 0.5f - ax, py = y + 0.5f - ay;
                    float along = px * ux + py * uy;
                    if (along < -0.3f || along > len) continue;
                    float t = Mathf.Clamp01(along / len);
                    if (trozos > 0f)
                    {
                        float p = t * trozos + fase;
                        if (p - Mathf.Floor(p) > lleno) continue;
                    }
                    float perp = Mathf.Abs(-px * uy + py * ux);
                    float forma = t < cintura ? Mathf.Lerp(0.6f, 1f, t / cintura) : (1f - t) / (1f - cintura);
                    float half = Mathf.Max(forma * ancho * 0.5f, t < 0.93f ? 0.5f : 0.3f);
                    if (perp > half) continue;
                    float h = Mathf.Lerp(calorBase, calorPunta, Mathf.Pow(t, 0.75f)) - caida * Mathf.Clamp01((perp - 0.5f) / Mathf.Max(0.5f, half - 0.5f));
                    c.Max(x, y, h);
                }
            }
        }

        /// <summary>Trazo recto de grosor constante con núcleo más caliente.</summary>
        static void Trazo(Campo c, float ax, float ay, float bx, float by, float ancho, float calor, float caida = 0.3f)
        {
            float dx = bx - ax, dy = by - ay;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            float ux = len > 0.001f ? dx / len : 1f, uy = len > 0.001f ? dy / len : 0f;
            float half = Mathf.Max(0.5f, ancho * 0.5f), pad = half + 1f;
            int x0 = Mathf.FloorToInt(Mathf.Min(ax, bx) - pad), x1 = Mathf.CeilToInt(Mathf.Max(ax, bx) + pad);
            int y0 = Mathf.FloorToInt(Mathf.Min(ay, by) - pad), y1 = Mathf.CeilToInt(Mathf.Max(ay, by) + pad);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float px = x + 0.5f - ax, py = y + 0.5f - ay;
                    float along = Mathf.Clamp(px * ux + py * uy, 0f, len);
                    float qx = px - ux * along, qy = py - uy * along;
                    float d = Mathf.Sqrt(qx * qx + qy * qy);
                    if (d > half) continue;
                    c.Max(x, y, calor - caida * Mathf.Clamp01((d - 0.5f) / Mathf.Max(0.5f, half - 0.5f)));
                }
            }
        }

        /// <summary>Disco de destello: <paramref name="calor"/> en el centro y <paramref name="borde"/> en el borde.</summary>
        static void Disco(Campo c, float cx, float cy, float r, float calor, float borde, float aplastado = 1f)
        {
            int x0 = Mathf.FloorToInt(cx - r - 1), x1 = Mathf.CeilToInt(cx + r + 1);
            int y0 = Mathf.FloorToInt(cy - r * aplastado - 1), y1 = Mathf.CeilToInt(cy + r * aplastado + 1);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx, dy = (y + 0.5f - cy) / aplastado;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
                    if (d > 1f) continue;
                    c.Max(x, y, Mathf.Lerp(calor, borde, d * d));
                }
            }
        }

        /// <summary>Anillo de radio r; con <paramref name="huecos"/> &gt; 0 se rompe en arcos (ruido angular).</summary>
        static void Anillo(Campo c, float cx, float cy, float r, float grosor, float calor, float caida, float huecos = 0f, int semilla = 0)
        {
            float half = Mathf.Max(0.5f, grosor * 0.5f);
            int x0 = Mathf.FloorToInt(cx - r - half - 1), x1 = Mathf.CeilToInt(cx + r + half + 1);
            int y0 = Mathf.FloorToInt(cy - r - half - 1), y1 = Mathf.CeilToInt(cy + r + half + 1);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float d = Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - r);
                    if (d > half) continue;
                    if (huecos > 0f)
                    {
                        float a = (Mathf.Atan2(dy, dx) / (2f * Mathf.PI) + 0.5f) * 14f;
                        if (PixelCanvas.ValueNoise(a, 0.5f, 14, semilla) < huecos) continue;
                    }
                    c.Max(x, y, calor - caida * Mathf.Clamp01((d - 0.5f) / Mathf.Max(0.5f, half - 0.5f)));
                }
            }
        }

        /// <summary>
        /// Media luna (estela de un tajo): arco de la circunferencia (cx, cy, radio = filo exterior) que va del ángulo
        /// a0 al a1 (radianes; el sentido marca hacia dónde avanza la hoja). El grosor es máximo en <paramref name="sesgo"/>
        /// y se afila en los extremos. El filo exterior es el más caliente. <paramref name="rayas"/> &gt; 0 abre
        /// líneas de velocidad (huecos finos a lo largo del arco) como las estelas de Blasphemous.
        /// </summary>
        static void Arco(Campo c, float cx, float cy, float radio, float a0, float a1, float grosor, float calorFuera, float calorDentro,
            float sesgo = 0.55f, float calorCola = 1f, int rayas = 0, int semilla = 0)
        {
            float span = Mathf.Abs(a1 - a0), dir = a1 >= a0 ? 1f : -1f;
            int x0 = Mathf.FloorToInt(cx - radio - 1), x1 = Mathf.CeilToInt(cx + radio + 1);
            int y0 = Mathf.FloorToInt(cy - radio - 1), y1 = Mathf.CeilToInt(cy + radio + 1);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    if (r > radio + 0.3f || r < radio - grosor - 1f) continue;
                    float rel = Mathf.Repeat((Mathf.Atan2(dy, dx) - a0) * dir, 2f * Mathf.PI);
                    if (rel > span) continue;
                    float s = rel / span;
                    float perfil = s < sesgo ? Mathf.Pow(s / sesgo, 0.8f) : Mathf.Pow((1f - s) / (1f - sesgo), 0.65f);
                    float th = Mathf.Max(grosor * perfil, 0.7f);
                    if (r < radio - th) continue;
                    float u = Mathf.Clamp01((radio - r) / th);
                    int nr = Mathf.Min(rayas, Mathf.FloorToInt(th / 3f));
                    if (nr >= 2)
                    {
                        // Huecos finos paralelos al filo que no recorren todo el arco.
                        float banda = u * nr;
                        int k = Mathf.FloorToInt(banda);
                        float fr = banda - k;
                        float s0 = Rnd(k, 1, semilla) * 0.5f, s1 = s0 + 0.25f + Rnd(k, 2, semilla) * 0.45f;
                        if (k > 0 && fr * th / nr < 1f && s > s0 && s < s1) continue;
                    }
                    float cola = Mathf.Lerp(calorCola, 1f, Mathf.Clamp01(s / Mathf.Max(0.01f, sesgo)));
                    c.Max(x, y, Mathf.Lerp(calorFuera, calorDentro, u) * cola);
                }
            }
        }

        /// <summary>Destello en cruz (+) de brazos finos.</summary>
        static void Cruz(Campo c, float x, float y, float brazo, float calor, float calorPunta)
        {
            float x0 = Mathf.Floor(x) + 0.5f, y0 = Mathf.Floor(y) + 0.5f;
            Pua(c, x0, y0, x0 + brazo + 0.5f, y0, 1f, calor, calorPunta, 0f, 0.05f);
            Pua(c, x0, y0, x0 - brazo - 0.5f, y0, 1f, calor, calorPunta, 0f, 0.05f);
            Pua(c, x0, y0, x0, y0 + brazo + 0.5f, 1f, calor, calorPunta, 0f, 0.05f);
            Pua(c, x0, y0, x0, y0 - brazo - 0.5f, 1f, calor, calorPunta, 0f, 0.05f);
        }

        /// <summary>Quita píxeles opacos sin ningún vecino opaco (nada de ruido suelto).</summary>
        static void QuitarSueltos(PixelCanvas c)
        {
            var copia = (Color32[])c.Pixels.Clone();
            for (int y = 0; y < c.Height; y++)
            {
                for (int x = 0; x < c.Width; x++)
                {
                    if (copia[y * c.Width + x].a == 0) continue;
                    bool solo = true;
                    for (int oy = -1; oy <= 1 && solo; oy++)
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            if (ox == 0 && oy == 0) continue;
                            int nx = x + ox, ny = y + oy;
                            if (nx >= 0 && ny >= 0 && nx < c.Width && ny < c.Height && copia[ny * c.Width + nx].a > 0) { solo = false; break; }
                        }
                    if (solo) c.Pixels[y * c.Width + x] = PixelCanvas.Clear;
                }
            }
        }

        // ------------------------------------------------------------------
        // Gotas (sangre, icor) con metabolas: se funden en chorros y racimos
        // ------------------------------------------------------------------

        /// <summary>Gota balística: nace en (X, Y) con velocidad (Vx, Vy) px/fotograma y cae con la gravedad.</summary>
        sealed class Gota
        {
            public float X, Y, Vx, Vy, R, Nace, Vida;
            public bool Icor;
        }

        struct Mancha
        {
            public float X, Y, R, Dx, Dy, Estira, Tono;
            public bool Icor;
        }

        /// <summary>Posición y velocidad de cada gota en el instante t (con rozamiento del aire y gravedad).</summary>
        static List<Mancha> Simular(List<Gota> gotas, float t, float gravedad, float roce = 0.22f)
        {
            var lista = new List<Mancha>();
            foreach (var g in gotas)
            {
                float a = t - g.Nace;
                if (a < 0f || a > g.Vida) continue;
                // El icor asoma cuando la gota ya se ha separado de la herida (si no, parece un agujero en la sangre).
                if (g.Icor && a < 0.8f) continue;
                float k = a / g.Vida;
                float e = Mathf.Exp(-roce * a), recorrido = (1f - e) / roce;
                float vx = g.Vx * e, vy = g.Vy * e - gravedad * a;
                float vel = Mathf.Sqrt(vx * vx + vy * vy);
                float r = g.R * Mathf.Sqrt(1f - k * k) * Mathf.Clamp01(0.55f + a * 1.2f);
                if (g.Icor) r = Mathf.Min(r, 1.5f);
                if (r < 0.55f) continue;
                lista.Add(new Mancha
                {
                    X = g.X + g.Vx * recorrido, Y = g.Y + g.Vy * recorrido - 0.5f * gravedad * a * a, R = r,
                    Dx = vel > 0.01f ? vx / vel : 1f, Dy = vel > 0.01f ? vy / vel : 0f,
                    Estira = 1f + Mathf.Min(2.6f, vel * 0.3f), Tono = 1f - k, Icor = g.Icor,
                });
            }
            return lista;
        }

        /// <summary>
        /// Pinta las manchas: sangre roja con grumos y borde inferior oscuros, brillo húmedo arriba; icor negro-verdoso.
        /// Cada mancha es una lágrima (cabeza redonda, cola larga hacia atrás) y se funden entre sí como metabolas.
        /// </summary>
        static void PintarManchas(PixelCanvas c, List<Mancha> manchas, int semilla)
        {
            int w = c.Width, h = c.Height;
            var fs = new float[w * h]; var fi = new float[w * h]; var tono = new float[w * h];
            const float alcance = 1.6f, umbral = 0.372f;
            foreach (var m in manchas)
            {
                float ra = m.R * alcance * (1f + (m.Estira - 1f) * 1.5f) + 1f;
                int x0 = Mathf.FloorToInt(m.X - ra), x1 = Mathf.CeilToInt(m.X + ra);
                int y0 = Mathf.FloorToInt(m.Y - ra), y1 = Mathf.CeilToInt(m.Y + ra);
                for (int y = Mathf.Max(0, y0); y <= Mathf.Min(h - 1, y1); y++)
                {
                    for (int x = Mathf.Max(0, x0); x <= Mathf.Min(w - 1, x1); x++)
                    {
                        float px = x + 0.5f - m.X, py = y + 0.5f - m.Y;
                        float along = px * m.Dx + py * m.Dy;
                        // Cabeza delante (poco estirada) y cola detrás (muy estirada).
                        float al = along / (m.R * (along >= 0f ? 1f + (m.Estira - 1f) * 0.35f : 1f + (m.Estira - 1f) * 1.5f));
                        float pe = (-px * m.Dy + py * m.Dx) / m.R;
                        float q2 = (al * al + pe * pe) / (alcance * alcance);
                        if (q2 >= 1f) continue;
                        float kk = (1f - q2) * (1f - q2);
                        int i = y * w + x;
                        if (m.Icor) fi[i] += kk;
                        else { fs[i] += kk; tono[i] += kk * m.Tono; }
                    }
                }
            }
            bool S(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && fs[y * w + x] >= umbral;
            bool I(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && fi[y * w + x] >= umbral;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    if (I(x, y))
                    {
                        c.Pixels[i] = !I(x, y + 1) && I(x, y - 1) && I(x - 1, y) && I(x + 1, y) ? IcorLuz : IcorBase;
                        continue;
                    }
                    if (!S(x, y)) continue;
                    bool vieja = tono[i] / Mathf.Max(0.001f, fs[i]) < 0.28f;
                    bool abajo = !S(x, y - 1);
                    bool dentro = S(x - 1, y) && S(x + 1, y) && S(x, y + 1) && !abajo;
                    bool hondo = dentro && S(x - 2, y) && S(x + 2, y) && S(x, y + 2) && S(x, y - 2);
                    Color32 col = vieja ? SangreMedia : SangreViva;
                    // Grumos oscuros dentro de las masas grandes.
                    if (dentro && PixelCanvas.ValueNoise(x * 0.42f, y * 0.42f, 0, semilla) > (hondo ? 0.6f : 0.7f)) col = vieja ? SangreOscura : SangreMedia;
                    if (abajo) col = vieja ? SangreOscura : SangreMedia;
                    // Brillo húmedo en el borde de arriba de las masas gruesas.
                    if (!vieja && !S(x, y + 1) && S(x, y - 1) && S(x, y - 2) && S(x - 1, y) && S(x + 1, y)) col = SangreBrillo;
                    c.Pixels[i] = col;
                }
            }
        }

        // ------------------------------------------------------------------
        // 1. Chispazo (golpe en carne)
        // ------------------------------------------------------------------

        struct Rayo { public float Ang, Largo, Ancho; }

        static PropAnimation Chispazo()
        {
            const int W = 72, H = 56;
            const float cx = 36f, cy = 28f;
            var anim = Nueva("fx_chispazo", 24f, 0.5f, 0.5f);

            var rayos = new List<Rayo>();
            // Abanico hacia delante (+x): tres agujas dominantes y otras más cortas a los lados.
            float[] angD = { 2f, 19f, -16f, 36f, -33f, 55f, -52f, 9f };
            float[] larD = { 32f, 27f, 29f, 21f, 22f, 15f, 15f, 17f };
            float[] ancD = { 7f, 5.4f, 5.8f, 4.4f, 4.4f, 3.6f, 3.6f, 2.8f };
            for (int i = 0; i < angD.Length; i++) rayos.Add(new Rayo { Ang = angD[i] * Grado, Largo = larD[i], Ancho = ancD[i] });
            // Unas pocas hacia atrás y en vertical, cortas.
            float[] angA = { 172f, 197f, 148f, 218f, 98f, 265f };
            float[] larA = { 13f, 10f, 8f, 8f, 11f, 10f };
            for (int i = 0; i < angA.Length; i++) rayos.Add(new Rayo { Ang = angA[i] * Grado, Largo = larA[i], Ancho = 3f });

            // Por fotograma: inicio y punta (fracción del largo), ancho y calor de la base y de la punta.
            float[] tIn = { 0f, 0f, 0f, 0.45f, 0.74f, 0.92f };
            float[] tOut = { 0f, 0.82f, 1.0f, 1.03f, 1.05f, 1.07f };
            float[] anc = { 0f, 1.0f, 0.78f, 0.5f, 0.45f, 0.42f };
            float[] cB = { 0f, 1.08f, 0.92f, 0.66f, 0.5f, 0.36f };
            float[] cP = { 0f, 0.48f, 0.38f, 0.3f, 0.3f, 0.28f };

            for (int f = 0; f < 6; f++)
            {
                var campo = new Campo(W, H);
                if (f == 0)
                {
                    // Destello blanco con núcleo y rayos cortos.
                    Disco(campo, cx, cy, 7f, 1.1f, 0.66f);
                    for (int i = 0; i < 8; i++)
                    {
                        float a = (i * 45f + 8f + (Rnd(i, 0, 11) - 0.5f) * 12f) * Grado;
                        float l = (i % 2 == 0 ? 14f : 10f) + Rnd(i, 1, 11) * 3f;
                        if (i == 0) l += 6f;
                        if (i == 4) l += 2f;
                        Pua(campo, cx + Mathf.Cos(a) * 3f, cy + Mathf.Sin(a) * 3f, cx + Mathf.Cos(a) * l, cy + Mathf.Sin(a) * l, i % 2 == 0 ? 4.4f : 3.2f, 1.0f, 0.5f);
                    }
                }
                else
                {
                    for (int i = 0; i < rayos.Count; i++)
                    {
                        var r = rayos[i];
                        if (f == 4 && i % 3 == 2) continue;
                        if (f == 5 && i % 2 == 1) continue;
                        float ca = Mathf.Cos(r.Ang), sa = Mathf.Sin(r.Ang);
                        float a0 = Mathf.Max(1.5f, r.Largo * tIn[f]), a1 = r.Largo * tOut[f];
                        if (f >= 4) a0 = Mathf.Min(a0, a1 - 3f);
                        Pua(campo, cx + ca * a0, cy + sa * a0, cx + ca * a1, cy + sa * a1, r.Ancho * anc[f], cB[f], cP[f], 0.42f, f >= 3 ? 0.3f : 0.14f);
                    }
                    if (f <= 2) Disco(campo, cx, cy, f == 1 ? 4.5f : 3.5f, 1.1f, f == 1 ? 0.86f : 0.8f);
                    // Media luna naranja (arco del tajo) que atraviesa el chispazo; el filo exterior arde en blanco.
                    if (f == 1) Arco(campo, cx - 15f, cy + 2f, 20f, 74f * Grado, -84f * Grado, 6.5f, 0.98f, 0.3f, 0.55f, 1f, 3, 13);
                    if (f == 2) Arco(campo, cx - 12f, cy + 1f, 20f, 62f * Grado, -86f * Grado, 5f, 0.75f, 0.28f, 0.6f, 1f, 2, 14);
                    if (f == 3) Arco(campo, cx - 10f, cy + 0f, 20f, 36f * Grado, -88f * Grado, 2.6f, 0.48f, 0.22f, 0.65f);
                    if (f >= 2 && f <= 4) Cruz(campo, cx + 17f + f * 2f, cy + 10f - f, f == 2 ? 4f : 3f, 1f - (f - 2) * 0.2f, 0.45f);
                    if (f == 3 || f == 4) Cruz(campo, cx + 8f + f, cy - 12f - f, 3f, 0.9f - (f - 3) * 0.25f, 0.4f);
                }
                var canvas = new PixelCanvas(W, H);
                Pintar(canvas, campo, Fuego, UmbralFuego);
                QuitarSueltos(canvas);
                anim.Frames.Add(canvas);
            }
            return anim;
        }

        // ------------------------------------------------------------------
        // 2. Sangre
        // ------------------------------------------------------------------

        /// <summary>Genera la salpicadura: chorro hacia delante, racimos de gotas, algo de icor y gotas lentas que caen.</summary>
        static List<Gota> Salpicadura(float cx, float cy, float escala, float rapidez, float nace, int semilla)
        {
            var g = new List<Gota>();
            Gota Nueva(float ang, float vel, float r, float t, float vida, bool icor = false) => new Gota
            {
                X = cx, Y = cy, Vx = Mathf.Cos(ang * Grado) * vel * rapidez, Vy = Mathf.Sin(ang * Grado) * vel * rapidez,
                R = r * escala, Nace = nace + t, Vida = vida, Icor = icor,
            };
            // Chorro: gotas casi alineadas que salen seguidas; las primeras van más rápido y el chorro se estira.
            for (int i = 0; i < 9; i++)
                g.Add(Nueva(12f + (Rnd(i, 0, semilla) - 0.5f) * 8f, 9.5f - i * 0.4f, 3.0f - i * 0.12f, i * 0.12f, 3.6f + Rnd(i, 1, semilla) * 1.0f));
            // Racimos: grupos de gotas con velocidades parecidas.
            float[] angR = { 38f, -12f, 62f, 22f, 2f, -32f, 84f, 48f };
            float[] velR = { 6.5f, 7f, 5f, 8f, 5f, 4.4f, 3.8f, 4f };
            for (int k = 0; k < angR.Length; k++)
            {
                int n = 4 + Mathf.FloorToInt(Rnd(k, 3, semilla) * 4f);
                for (int j = 0; j < n; j++)
                {
                    int id = k * 10 + j;
                    g.Add(Nueva(angR[k] + (Rnd(id, 4, semilla) - 0.5f) * 20f, velR[k] * (0.78f + Rnd(id, 5, semilla) * 0.4f),
                        0.9f + Rnd(id, 6, semilla) * 1.4f, Rnd(id, 7, semilla) * 0.5f, 4.5f + Rnd(id, 8, semilla) * 2.5f, (k == 1 || k == 3 || k == 5) && j == 2));
                }
            }
            // Unas pocas hacia atrás.
            for (int i = 0; i < 5; i++)
                g.Add(Nueva(150f + Rnd(i, 10, semilla) * 60f, 2f + Rnd(i, 11, semilla) * 2.2f, 1.0f + Rnd(i, 12, semilla), 0f, 3.6f));
            // Borbotón inicial en el punto de impacto.
            for (int i = 0; i < 7; i++) g.Add(Nueva(i * 51f + 20f + Rnd(i, 19, semilla) * 20f, 3.6f, 1.7f, -0.45f, 1.9f));
            // Gotas lentas que caen bajo el centro (las últimas en desaparecer).
            for (int i = 0; i < 7; i++)
            {
                float vx = -0.6f + Rnd(i, 13, semilla) * 2.4f, vy = 0.4f + Rnd(i, 14, semilla) * 1.8f;
                g.Add(new Gota
                {
                    X = cx + (Rnd(i, 15, semilla) - 0.4f) * 8f * escala, Y = cy, Vx = vx * escala, Vy = vy * escala,
                    R = (1.2f + Rnd(i, 16, semilla) * 1.1f) * escala, Nace = nace + 0.8f + Rnd(i, 17, semilla) * 1.6f,
                    Vida = 5f + Rnd(i, 18, semilla) * 1.6f, Icor = i == 5,
                });
            }
            return g;
        }

        /// <summary>Corona del primer instante: púas de sangre que revientan del punto de impacto, casi todas hacia delante.</summary>
        static List<Mancha> Corona(float cx, float cy, float escala, int semilla)
        {
            var lista = new List<Mancha>();
            float[] ang = { -38f, -14f, 6f, 24f, 46f, 70f, 104f, 196f, 236f };
            float[] dist = { 7f, 10f, 12f, 11f, 9f, 7f, 5f, 5f, 4.5f };
            for (int i = 0; i < ang.Length; i++)
            {
                float a = (ang[i] + (Rnd(i, 0, semilla) - 0.5f) * 10f) * Grado, d = dist[i] * escala;
                // La cola (punta) mira hacia fuera: Dx/Dy apuntan al centro.
                lista.Add(new Mancha { X = cx + Mathf.Cos(a) * d * 0.55f, Y = cy + Mathf.Sin(a) * d * 0.55f, R = (1.2f + Rnd(i, 1, semilla) * 0.5f) * escala, Dx = -Mathf.Cos(a), Dy = -Mathf.Sin(a), Estira = 3.4f, Tono = 1f });
            }
            lista.Add(new Mancha { X = cx + 1f, Y = cy, R = 3.4f * escala, Dx = 1f, Dy = 0f, Estira = 1.2f, Tono = 1f });
            return lista;
        }

        static PropAnimation Sangre()
        {
            const int W = 80, H = 64;
            var anim = Nueva("fx_sangre", 20f, 0.5f, 0.5f);
            var gotas = Salpicadura(40f, 32f, 1f, 1.45f, 0f, 21);
            for (int f = 0; f < 8; f++)
            {
                var canvas = new PixelCanvas(W, H);
                var manchas = Simular(gotas, f == 0 ? 0.3f : f + 0.7f, 0.95f);
                if (f == 0) manchas.AddRange(Corona(40f, 32f, 1f, 22));
                PintarManchas(canvas, manchas, 23);
                QuitarSueltos(canvas);
                anim.Frames.Add(canvas);
            }
            return anim;
        }

        // ------------------------------------------------------------------
        // 3. Estrella de daño (el jugador recibe un golpe)
        // ------------------------------------------------------------------

        static PropAnimation Estrella()
        {
            const int W = 64, H = 64;
            const float cx = 32f, cy = 32f;
            var anim = Nueva("fx_estrella", 24f, 0.5f, 0.5f);
            var blanco = Hex("fffefa"); var rosa = Hex("ffd3cc"); var ribete = Hex("e8585c");
            // Ocho puntas irregulares: cuatro largas casi en cruz y cuatro cortas en diagonal.
            float[] ang = new float[8], lar = new float[8], anc = new float[8];
            for (int i = 0; i < 8; i++)
            {
                ang[i] = (i * 45f + 6f + (Rnd(i, 0, 31) - 0.5f) * 16f) * Grado;
                lar[i] = (i % 2 == 0 ? 1f : 0.58f) * (0.78f + Rnd(i, 1, 31) * 0.22f);
                anc[i] = (i % 2 == 0 ? 1f : 0.72f) * (0.85f + Rnd(i, 2, 31) * 0.3f);
            }
            // Crece rapidísimo y se adelgaza; al final se vacía por dentro y se rompe.
            float[] radio = { 15f, 23f, 26.5f, 27.5f, 28f };
            float[] ancho = { 7.5f, 8f, 5.6f, 3.4f, 2.2f };
            float[] nucleo = { 6f, 6.5f, 4.5f, 0f, 0f };
            float[] hueco = { 0f, 0f, 0f, 6f, 14f };
            for (int f = 0; f < 5; f++)
            {
                var campo = new Campo(W, H);
                for (int i = 0; i < 8; i++)
                {
                    float ca = Mathf.Cos(ang[i]), sa = Mathf.Sin(ang[i]);
                    float r0 = f == 4 ? radio[f] * lar[i] * 0.45f : 0f, r1 = radio[f] * lar[i];
                    Pua(campo, cx + ca * r0, cy + sa * r0, cx + ca * r1, cy + sa * r1, ancho[f] * anc[i], 1f, 1f, 0f, f == 4 ? 0.3f : 0.1f);
                }
                if (nucleo[f] > 0f) Disco(campo, cx, cy, nucleo[f], 1f, 1f);
                var m = new bool[W * H];
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                        m[y * W + x] = campo.V[y * W + x] > 0.5f && dx * dx + dy * dy >= hueco[f] * hueco[f];
                    }
                bool M(int x, int y) => x >= 0 && y >= 0 && x < W && y < H && m[y * W + x];
                var canvas = new PixelCanvas(W, H);
                for (int y = 0; y < H; y++)
                {
                    for (int x = 0; x < W; x++)
                    {
                        int i = y * W + x;
                        if (M(x, y))
                        {
                            // Banda rosada solo donde la forma es gruesa: las puntas finas quedan blancas.
                            bool borde = !M(x + 1, y) || !M(x - 1, y) || !M(x, y + 1) || !M(x, y - 1);
                            bool grueso = (M(x + 2, y) && M(x - 2, y)) || (M(x, y + 2) && M(x, y - 2)) || (M(x + 2, y) ^ M(x - 2, y)) && (M(x, y + 2) ^ M(x, y - 2));
                            canvas.Pixels[i] = borde && grueso && f <= 2 ? rosa : blanco;
                        }
                        else if (M(x + 1, y) || M(x - 1, y) || M(x, y + 1) || M(x, y - 1)) canvas.Pixels[i] = ribete;
                    }
                }
                QuitarSueltos(canvas);
                anim.Frames.Add(canvas);
            }
            return anim;
        }

        // ------------------------------------------------------------------
        // 4. Bloqueo (golpe parado por escudo o arma)
        // ------------------------------------------------------------------

        static PropAnimation Bloqueo()
        {
            const int W = 56, H = 56;
            const float cx = 28f, cy = 28f;
            var anim = Nueva("fx_bloqueo", 24f, 0.5f, 0.5f);
            // Chispas de metal: rebotan hacia atrás (-x) y hacia arriba/abajo, frenan y caen. vel = alcance relativo.
            const int n = 12;
            var ang = new float[n]; var vel = new float[n];
            for (int i = 0; i < n; i++)
            {
                float u = (i + 0.5f) / n;
                bool delante = i == 3 || i == 8;
                ang[i] = (delante ? (i < 6 ? 62f : -55f) + (Rnd(i, 0, 41) - 0.5f) * 16f : 102f + u * 156f + (Rnd(i, 1, 41) - 0.5f) * 12f) * Grado;
                vel[i] = (delante ? 0.68f : 0.8f) + Rnd(i, 2, 41) * 0.22f;
            }
            // Por fotograma: distancia de la cabeza y de la cola de cada chispa, caída por gravedad, calor y grosor.
            float[] cabeza = { 0f, 14f, 20.5f, 23.5f, 25.5f, 27f };
            float[] cola = { 0f, 8f, 9f, 14f, 19.5f, 23.5f };
            float[] caida = { 0f, 0f, 0.6f, 1.8f, 3.4f, 5.2f };
            float[] calor = { 0f, 1.05f, 0.95f, 0.75f, 0.55f, 0.42f };
            float[] ancho = { 0f, 2.4f, 2.2f, 1.8f, 1.4f, 1.2f };
            for (int f = 0; f < 6; f++)
            {
                var campo = new Campo(W, H);
                if (f == 0)
                {
                    // Destello del choque: núcleo blanco y cruz alargada en vertical (la cara del escudo).
                    const float k = 1f;
                    Disco(campo, cx, cy, 5f * k, 1.1f, 0.75f);
                    Pua(campo, cx, cy + 2f, cx + 1f, cy + 20f * k, 4f * k, 1.05f, 0.5f, 0.35f, 0.1f);
                    Pua(campo, cx, cy - 2f, cx - 1f, cy - 18f * k, 4f * k, 1.05f, 0.5f, 0.35f, 0.1f);
                    Pua(campo, cx - 2f, cy, cx - 13f * k, cy, 3.4f * k, 1f, 0.45f, 0.35f, 0.1f);
                    Pua(campo, cx + 2f, cy, cx + 8f * k, cy, 3f * k, 0.95f, 0.45f, 0.35f, 0.1f);
                }
                if (f == 1) Disco(campo, cx, cy, 3.5f, 1.05f, 0.85f);
                // Anillo pequeño que se abre y se rompe.
                float[] rr = { 5.5f, 6.5f, 0f, 0f, 0f, 0f };
                float[] gr = { 2.6f, 1.6f, 0f, 0f, 0f, 0f };
                if (rr[f] > 0f) Anillo(campo, cx, cy, rr[f], gr[f], 0.98f - f * 0.15f, 0.4f, 0f, 43);
                if (f >= 1)
                {
                    for (int i = 0; i < n; i++)
                    {
                        if (f >= 4 && i % 3 == 1) continue;
                        if (f == 5 && i % 2 == 0) continue;
                        float ca = Mathf.Cos(ang[i]), sa = Mathf.Sin(ang[i]), k = vel[i];
                        float hx = cx + ca * cabeza[f] * k, hy = cy + sa * cabeza[f] * k - caida[f];
                        float tx = cx + ca * cola[f] * k, ty = cy + sa * cola[f] * k - caida[f] * 0.4f;
                        float c0 = calor[f] * (0.85f + 0.15f * Rnd(i, 3, 41));
                        Pua(campo, hx, hy, tx, ty, ancho[f], c0, 0.3f, 0f, 0.01f);
                    }
                }
                if (f == 2 || f == 3) Cruz(campo, cx - 10f - f, cy + 9f + f, f == 2 ? 4f : 3f, 1f, 0.4f);
                if (f == 3 || f == 4) Cruz(campo, cx - 4f, cy - 13f - f, 3f, 0.9f, 0.4f);
                var canvas = new PixelCanvas(W, H);
                Pintar(canvas, campo, Hielo, UmbralHielo);
                QuitarSueltos(canvas);
                anim.Frames.Add(canvas);
            }
            return anim;
        }

        // ------------------------------------------------------------------
        // 5. Polvo (aterrizaje, esquiva)
        // ------------------------------------------------------------------

        /// <summary>
        /// Nubecilla: círculo con el borde mordido por ruido, en tres tonos (luz arriba hacia fuera, cuerpo, sombra
        /// abajo hacia dentro). Se deshace por grumos de ruido, nunca por píxeles sueltos.
        /// </summary>
        static void Nube(PixelCanvas c, float cx, float cy, float r, Color32 luz, Color32 medio, Color32 sombra, float erosion, int semilla, float ladoLuz)
        {
            int x0 = Mathf.FloorToInt(cx - r - 2), x1 = Mathf.CeilToInt(cx + r + 2);
            int y0 = Mathf.FloorToInt(cy - r - 2), y1 = Mathf.CeilToInt(cy + r + 2);
            for (int y = Mathf.Max(0, y0); y <= Mathf.Min(c.Height - 1, y1); y++)
            {
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(c.Width - 1, x1); x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float ruido = PixelCanvas.ValueNoise(x * 0.45f, y * 0.45f, 0, semilla);
                    float rr = r * (0.84f + 0.26f * ruido);
                    if (dx * dx + dy * dy > rr * rr) continue;
                    if (erosion > 0f && PixelCanvas.ValueNoise(x * 0.24f + 7f, y * 0.3f, 0, semilla + 5) < erosion) continue;
                    float lx = dx - ladoLuz * r * 0.3f, ly = dy - r * 0.45f;
                    float sx = dx + ladoLuz * r * 0.25f, sy = dy + r * 0.5f;
                    Color32 col = lx * lx + ly * ly < r * r * 0.4f ? luz : (sx * sx + sy * sy < r * r * 0.45f ? sombra : medio);
                    c.Pixels[y * c.Width + x] = col;
                }
            }
        }

        static PropAnimation Polvo()
        {
            const int W = 64, H = 24;
            const float cx = 32f;
            var anim = Nueva("fx_polvo", 16f, 0.5f, 0f);
            // Tres nubecillas por lado: la de fuera rueda más lejos y es la más grande.
            float[] dist0 = { 7f, 4f, 1.5f }, dist1 = { 20f, 13f, 6.5f };
            float[] rad0 = { 4.2f, 3.6f, 3f }, rad1 = { 6.2f, 5.4f, 4.6f };
            float[] sube = { 1.5f, 3f, 4.5f };
            for (int f = 0; f < 7; f++)
            {
                var canvas = new PixelCanvas(W, H);
                float t = f / 6f;
                float ease = 1f - (1f - t) * (1f - t) * (1f - t);
                float aMul = Mathf.Lerp(1f, 0.62f, t);
                var luz = Hex("a39985"); luz.a = (byte)Mathf.RoundToInt(185f * aMul);
                var medio = Hex("8a8070"); medio.a = (byte)Mathf.RoundToInt(195f * aMul);
                var sombra = Hex("6a6258"); sombra.a = (byte)Mathf.RoundToInt(200f * aMul);
                float erosion = f < 3 ? 0f : (f - 2) * 0.16f;
                for (int lado = -1; lado <= 1; lado += 2)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        int sd = k + (lado > 0 ? 10 : 0);
                        float d = Mathf.Lerp(dist0[k], dist1[k], ease) + Rnd(sd, 0, 51) * 1.5f;
                        float r = Mathf.Lerp(rad0[k], rad1[k], ease) * (0.92f + Rnd(sd, 1, 51) * 0.16f);
                        float y = r * 0.5f + t * sube[k];
                        Nube(canvas, cx + lado * d, y, r, luz, medio, sombra, erosion, 53 + sd + f * 3, -lado);
                    }
                }
                QuitarSueltos(canvas);
                anim.Frames.Add(canvas);
            }
            return anim;
        }

        // ------------------------------------------------------------------
        // 6. Signo Antiguo (hechizo)
        // ------------------------------------------------------------------

        static PropAnimation Signo()
        {
            const int W = 96, H = 96;
            const float cx = 48f, cy = 48f;
            var anim = Nueva("fx_signo", 20f, 0.5f, 0.5f);
            float[] esc = { 0f, 0.66f, 0.94f, 1f, 1.02f, 1.03f, 1.04f, 1.05f };
            float[] giro = { 0f, -16f, -5f, 0f, 0f, 0f, 0f, 0f };
            float[] luz = { 1f, 1.05f, 1.05f, 1f, 0.88f, 0.7f, 0.5f, 0.32f };
            float[] linea = { 0f, 4.2f, 4.4f, 4f, 3.6f, 3.2f, 2.6f, 2.2f };
            float[] anillo = { 10f, 17f, 25f, 32f, 37f, 41f, 44f, 46f };
            float[] grosor = { 3f, 4f, 3.4f, 2.8f, 2.2f, 1.8f, 1.4f, 1.1f };
            float[] llama = { 10f, 28f, 36f, 38f, 36f, 30f, 20f, 11f };
            for (int f = 0; f < 8; f++)
            {
                var campo = new Campo(W, H);
                float L = luz[f];
                // Anillo que se expande y al final se rompe en arcos.
                Anillo(campo, cx, cy, anillo[f], grosor[f], 0.95f * L, 0.5f, f >= 4 ? 0.25f + (f - 4) * 0.12f : 0f, 61);
                // Estrella de cinco puntas (gira un poco al aparecer).
                if (esc[f] > 0f)
                {
                    float R = 30f * esc[f];
                    var px = new float[5]; var py = new float[5];
                    for (int i = 0; i < 5; i++)
                    {
                        float a = (90f + giro[f] + i * 72f) * Grado;
                        px[i] = cx + Mathf.Cos(a) * R; py[i] = cy + Mathf.Sin(a) * R;
                    }
                    for (int i = 0; i < 5; i++)
                    {
                        int j = (i + 2) % 5;
                        if (f >= 6)
                        {
                            // La estrella se rompe en trozos.
                            for (int s = 0; s < 5; s++)
                            {
                                if (Rnd(i * 5 + s, f, 63) < 0.4f) continue;
                                float t0 = s / 5f + 0.02f, t1 = t0 + 0.13f;
                                Trazo(campo, Mathf.Lerp(px[i], px[j], t0), Mathf.Lerp(py[i], py[j], t0), Mathf.Lerp(px[i], px[j], t1), Mathf.Lerp(py[i], py[j], t1), linea[f], L, 0.6f);
                            }
                        }
                        else Trazo(campo, px[i], py[i], px[j], py[j], linea[f], L, 0.6f);
                    }
                    // Destellos en las puntas.
                    if (f >= 2 && f <= 4) for (int i = 0; i < 5; i++) Cruz(campo, px[i], py[i], f == 2 ? 5f : 4f, 1.05f * L, 0.5f);
                }
                else
                {
                    // Primer fotograma: el ojo se abre con un haz vertical.
                    Disco(campo, cx, cy, 6.5f, 1.1f, 0.6f);
                    Pua(campo, cx, cy + 3f, cx, cy + 22f, 3.4f, 1.05f, 0.45f, 0.4f, 0.1f);
                    Pua(campo, cx, cy - 3f, cx, cy - 18f, 3.4f, 1.05f, 0.45f, 0.4f, 0.1f);
                    Pua(campo, cx + 3f, cy, cx + 10f, cy, 2.4f, 1f, 0.45f, 0.3f, 0.1f);
                    Pua(campo, cx - 3f, cy, cx - 10f, cy, 2.4f, 1f, 0.45f, 0.3f, 0.1f);
                }
                // Pilar-llama vertical que atraviesa el ojo.
                float alto = llama[f];
                float baseY = cy - alto * 0.3f;
                float vaiven = Mathf.Sin(f * 1.9f) * 1.6f;
                for (int y = 0; y < H; y++)
                {
                    float t = (y + 0.5f - baseY) / alto;
                    if (t < 0f || t > 1f) continue;
                    float half = 6f * Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.55f)) * (1f - 0.4f * t);
                    float centro = cx + vaiven * t * t;
                    for (int x = 0; x < W; x++)
                    {
                        float d = Mathf.Abs(x + 0.5f - centro);
                        if (d > Mathf.Max(0.5f, half)) continue;
                        campo.Max(x, y, (1.08f - 0.72f * d / Mathf.Max(1f, half) - 0.35f * t) * Mathf.Max(L, 0.8f));
                    }
                }
                // Ojo: almendra de contorno oscuro, blanco encendido, iris turquesa y pupila rasgada.
                if (f >= 1 && f <= 6)
                {
                    float ojoW = f == 1 ? 7f : 9f, ojoH = f == 1 ? 3.4f : 4.4f;
                    float apertura = f >= 6 ? 0.5f : 1f;
                    ojoH *= apertura;
                    for (int y = Mathf.FloorToInt(cy - ojoH - 1); y <= Mathf.CeilToInt(cy + ojoH + 1); y++)
                    {
                        for (int x = Mathf.FloorToInt(cx - ojoW - 1); x <= Mathf.CeilToInt(cx + ojoW + 1); x++)
                        {
                            float dx = (x + 0.5f - cx) / ojoW, dy = y + 0.5f - cy;
                            float lim = ojoH * (1f - dx * dx);
                            if (Mathf.Abs(dx) > 1f || Mathf.Abs(dy) > lim + 0.5f) continue;
                            bool borde = Mathf.Abs(dy) > lim - 0.6f || Mathf.Abs(dx) > 0.9f;
                            float ix = x + 0.5f - cx, iy = y + 0.5f - cy;
                            bool iris = ix * ix + iy * iy <= 2.9f * 2.9f * apertura;
                            bool pupila = Mathf.Abs(ix) < 0.6f && Mathf.Abs(iy) < 2.6f * apertura;
                            float v = borde ? 0.2f : (pupila ? 0.2f : (iris ? 0.42f : 0.95f));
                            campo.Poner(x, y, Mathf.Max(0.15f, v * (borde || pupila ? 1f : L)));
                        }
                    }
                }
                // Motas que escapan hacia fuera y hacia arriba.
                if (f >= 2)
                {
                    for (int i = 0; i < 20; i++)
                    {
                        float a = (i * 18f + Rnd(i, 0, 67) * 14f) * Grado;
                        float t = f - 2f + Rnd(i, 1, 67) * 0.8f;
                        float r0 = 12f + Rnd(i, 2, 67) * 18f;
                        float mx = cx + Mathf.Cos(a) * (r0 + t * (2.2f + Rnd(i, 3, 67) * 2f));
                        float my = cy + Mathf.Sin(a) * (r0 + t * 2f) + t * t * 0.7f;
                        float vida = 1f - t / 6.8f;
                        if (vida <= 0f || (f >= 6 && i % 2 == 0)) continue;
                        int qx = Mathf.FloorToInt(mx), qy = Mathf.FloorToInt(my);
                        float h = 0.25f + 0.85f * vida;
                        if (i % 3 == 0)
                        {
                            // Mota grande en cruz.
                            campo.Max(qx, qy, h); campo.Max(qx + 1, qy, h * 0.7f); campo.Max(qx - 1, qy, h * 0.7f);
                            campo.Max(qx, qy + 1, h * 0.7f); campo.Max(qx, qy - 1, h * 0.7f);
                        }
                        else { campo.Max(qx, qy, h * 0.9f); campo.Max(qx, qy + 1, h * 0.75f); }
                    }
                }
                var canvas = new PixelCanvas(W, H);
                Pintar(canvas, campo, Signo4, UmbralSigno);
                QuitarSueltos(canvas);
                anim.Frames.Add(canvas);
            }
            return anim;
        }

        // ------------------------------------------------------------------
        // 7. Ejecución (clímax del combate)
        // ------------------------------------------------------------------

        static PropAnimation Ejecucion()
        {
            const int W = 128, H = 96;
            const float cx = 64f, cy = 48f;
            var anim = Nueva("fx_ejecucion", 20f, 0.5f, 0.5f);
            var gotas = Salpicadura(cx + 3f, cy - 1f, 1.4f, 1.9f, 0.6f, 71);
            // Gran tajo descendente: circunferencia con centro abajo a la izquierda; el filo pasa justo por el impacto.
            const float ax = cx - 30f, ay = cy - 26f, aR = 43f;
            float[] a0 = { 116f, 118f, 116f, 100f, 80f, 58f, 0f, 0f, 0f };
            float[] a1 = { -20f, -21f, -22f, -23f, -24f, -25f, 0f, 0f, 0f };
            float[] arG = { 5f, 16f, 15f, 11f, 7f, 3.5f, 0f, 0f, 0f };
            float[] arF = { 1.1f, 1.1f, 1.02f, 0.85f, 0.62f, 0.42f, 0f, 0f, 0f };
            float[] arD = { 0.9f, 0.3f, 0.26f, 0.22f, 0.18f, 0.15f, 0f, 0f, 0f };
            // Abanico de agujas (hacia delante, unas pocas atrás).
            float[] angD = { 4f, 18f, -14f, 33f, -30f, 50f, -48f, 66f, -64f, 176f, 156f, 200f, 96f, 268f };
            float[] larD = { 56f, 48f, 50f, 40f, 40f, 30f, 30f, 22f, 22f, 26f, 18f, 18f, 24f, 22f };
            float[] ancD = { 9f, 7f, 7.5f, 6f, 6f, 5f, 5f, 4f, 4f, 5f, 4f, 4f, 4.5f, 4.5f };
            float[] tIn = { 0f, 0f, 0.08f, 0.4f, 0.7f, 0.88f, 1.0f, 0f, 0f };
            float[] tOut = { 0f, 0.7f, 0.95f, 1.02f, 1.05f, 1.07f, 1.08f, 0f, 0f };
            float[] anc = { 0f, 1f, 0.8f, 0.6f, 0.45f, 0.35f, 0.3f, 0f, 0f };
            float[] cB = { 0f, 1.1f, 0.98f, 0.75f, 0.58f, 0.45f, 0.34f, 0f, 0f };
            for (int f = 0; f < 9; f++)
            {
                var canvas = new PixelCanvas(W, H);
                // La sangre va debajo del fuego.
                if (f >= 1) PintarManchas(canvas, Simular(gotas, f + 0.2f, 0.9f), 72);
                var campo = new Campo(W, H);
                if (arG[f] > 0f)
                    Arco(campo, ax, ay, aR + f * 1.3f, a0[f] * Grado, a1[f] * Grado, arG[f], arF[f], arD[f], 0.55f, f == 0 ? 1f : 0.55f, f >= 1 ? 5 : 0, 73 + f);
                if (f == 0)
                {
                    // Destello enorme: disco blanco y cruz de rayos larguísimos.
                    Disco(campo, cx, cy, 15f, 1.15f, 0.7f, 0.82f);
                    Pua(campo, cx + 6f, cy, cx + 62f, cy + 1f, 8f, 1.1f, 0.5f, 0.45f, 0.1f);
                    Pua(campo, cx - 6f, cy, cx - 56f, cy - 1f, 7f, 1.05f, 0.5f, 0.45f, 0.1f);
                    Pua(campo, cx, cy + 5f, cx - 1f, cy + 46f, 6f, 1.05f, 0.48f, 0.45f, 0.1f);
                    Pua(campo, cx, cy - 5f, cx + 1f, cy - 46f, 6f, 1.05f, 0.48f, 0.45f, 0.1f);
                    for (int i = 0; i < 4; i++)
                    {
                        float a = (45f + i * 90f) * Grado;
                        Pua(campo, cx + Mathf.Cos(a) * 6f, cy + Mathf.Sin(a) * 6f, cx + Mathf.Cos(a) * 24f, cy + Mathf.Sin(a) * 24f, 5f, 1f, 0.45f, 0.45f, 0.1f);
                    }
                }
                if (f >= 1 && f <= 6)
                {
                    for (int i = 0; i < angD.Length; i++)
                    {
                        if (f >= 5 && i % 2 == 1) continue;
                        float a = angD[i] * Grado, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                        float r0 = Mathf.Max(3f, larD[i] * tIn[f]), r1 = larD[i] * tOut[f];
                        if (f >= 4) r0 = Mathf.Min(r0, r1 - 4f);
                        Pua(campo, cx + ca * r0, cy + sa * r0, cx + ca * r1, cy + sa * r1, ancD[i] * anc[f], cB[f], 0.3f, 0.45f, f >= 3 ? 0.3f : 0.12f);
                    }
                }
                if (f >= 1 && f <= 2) Disco(campo, cx, cy, 12f - f * 3f, 1.15f, 0.72f, 0.85f);
                if (f >= 2 && f <= 5)
                {
                    Cruz(campo, cx + 34f + f * 3f, cy + 16f + f, 6f - f * 0.7f, 1.05f - (f - 2) * 0.15f, 0.45f);
                    Cruz(campo, cx + 14f + f * 2f, cy - 24f - f, 5f - f * 0.5f, 1f - (f - 2) * 0.15f, 0.45f);
                    Cruz(campo, cx - 20f - f * 2f, cy + 22f + f, 4f - f * 0.4f, 0.95f - (f - 2) * 0.15f, 0.45f);
                }
                // Brasas que caen al final.
                if (f >= 5)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        if (Rnd(i, f, 79) < 0.3f + (f - 5) * 0.12f) continue;
                        float a = (Rnd(i, 3, 77) * 150f - 70f) * Grado, d = 20f + Rnd(i, 4, 77) * 30f + (f - 5) * 3f;
                        float bx = cx + Mathf.Cos(a) * d, by = cy + Mathf.Sin(a) * d * 0.8f - (f - 5) * (f - 5) * 1.2f;
                        float h = 0.62f - (f - 5) * 0.08f;
                        Pua(campo, bx, by, bx - Mathf.Cos(a) * 3f, by - Mathf.Sin(a) * 3f + 1.5f, 2.2f, h, h * 0.55f, 0f, 0.2f);
                    }
                }
                var fuego = new PixelCanvas(W, H);
                Pintar(fuego, campo, Fuego, UmbralFuego);
                for (int i = 0; i < fuego.Pixels.Length; i++) if (fuego.Pixels[i].a > 0) canvas.Pixels[i] = fuego.Pixels[i];
                QuitarSueltos(canvas);
                anim.Frames.Add(canvas);
            }
            return anim;
        }

        // ------------------------------------------------------------------
        // 8. Muerte de una criatura (se deshace en humo e icor)
        // ------------------------------------------------------------------

        static PropAnimation Muerte()
        {
            const int W = 80, H = 80;
            const float cx = 40f, cy = 16f;
            var anim = Nueva("fx_muerte", 14f, 0.5f, 0.2f);
            var oscuro = Hex("1f2a2a"); var medio = Hex("2b403c"); var claro = Hex("3e5f58");
            var mota = Hex("8fffd0"); var motaNucleo = Hex("e6fff4"); var motaCola = Hex("4fbf9a");
            var espectro = new[] { Hex("3e5f58"), Hex("4fbf9a"), Hex("8fffd0"), Hex("e6fff4") };
            var umbralEspectro = new[] { 0.15f, 0.4f, 0.62f, 0.85f };
            const int nB = 8;
            for (int f = 0; f < 10; f++)
            {
                var canvas = new PixelCanvas(W, H);
                // Bocanadas de humo: nacen en el cuerpo, suben en columna, crecen y se deshilachan.
                // Primero se calcula a qué bocanada pertenece cada píxel (la más nueva tapa a las viejas).
                var id = new int[W * H];
                var rad = new float[nB]; var pcx = new float[nB]; var pcy = new float[nB];
                for (int i = 0; i < W * H; i++) id[i] = -1;
                for (int i = 0; i < nB; i++)
                {
                    float nace = i < 3 ? 0f : (i - 2) * 0.5f;
                    float a = f - nace + 0.5f;
                    rad[i] = 0f;
                    if (a < 0f) continue;
                    float vida = 6f + Rnd(i, 0, 81) * 2f;
                    float k = a / vida;
                    if (k >= 1f) continue;
                    float x0 = cx + (Rnd(i, 1, 81) - 0.5f) * 26f, y0 = cy + 5f + Rnd(i, 2, 81) * 12f;
                    float sube = 2.9f + Rnd(i, 3, 81) * 1.4f;
                    pcx[i] = x0 + Mathf.Sin(a * 0.8f + i * 1.7f) * 2f + (x0 - cx) * 0.06f * a;
                    pcy[i] = y0 + sube * a + 0.1f * a * a;
                    rad[i] = (8f + Rnd(i, 4, 81) * 4f) * (0.5f + 0.6f * Mathf.Sqrt(Mathf.Min(1f, k * 2.5f)));
                    float erosion = k < 0.25f ? 0f : (k - 0.25f) * 1.5f;
                    MarcarHumo(id, W, H, i, pcx[i], pcy[i], rad[i], erosion, 83 + i);
                }
                bool M(int x, int y) => x >= 0 && y >= 0 && x < W && y < H && id[y * W + x] >= 0;
                for (int y = 0; y < H; y++)
                {
                    for (int x = 0; x < W; x++)
                    {
                        int p = id[y * W + x];
                        if (p < 0) continue;
                        // Luz en el borde de arriba de la masa y en la cresta de cada lóbulo que tapa a otro de detrás.
                        int arriba = y + 1 < H ? id[(y + 1) * W + x] : -1;
                        bool cresta = !M(x, y + 1) || !M(x - 1, y + 1) || (arriba >= 0 && arriba < p);
                        bool bajo = !M(x, y - 1) || !M(x, y - 2) || (y + 0.5f - pcy[p]) < -rad[p] * 0.3f;
                        canvas.Pixels[y * W + x] = cresta ? claro : (bajo ? oscuro : medio);
                    }
                }
                // Reventón inicial: icor y un anillo de luz espectral.
                if (f <= 2)
                {
                    var g = new List<Mancha>();
                    for (int i = 0; i < 10; i++)
                    {
                        float a = (i * 36f + Rnd(i, 0, 85) * 20f) * Grado, d = (f + 0.8f) * (3.5f + Rnd(i, 1, 85) * 3f);
                        g.Add(new Mancha { X = cx + Mathf.Cos(a) * d, Y = cy + 12f + Mathf.Sin(a) * d * 0.8f - f * f * 0.8f, R = (2.2f - f * 0.5f) * (0.7f + Rnd(i, 2, 85) * 0.6f), Dx = Mathf.Cos(a), Dy = Mathf.Sin(a), Estira = 1.8f, Tono = 1f, Icor = true });
                    }
                    PintarManchas(canvas, g, 86);
                }
                if (f <= 1)
                {
                    // El alma que escapa: destello con rayos cortos, sobre todo hacia arriba.
                    var luz = new Campo(W, H);
                    for (int i = 0; i < 7; i++)
                    {
                        float a = (90f + (i - 3) * 38f + (Rnd(i, 0, 89) - 0.5f) * 12f) * Grado;
                        float l = (i == 3 ? 16f : 9f + Rnd(i, 1, 89) * 4f) * (f == 0 ? 1f : 1.5f), l0 = f == 0 ? 2f : l * 0.5f;
                        Pua(luz, cx + Mathf.Cos(a) * l0, cy + 12f + Mathf.Sin(a) * l0, cx + Mathf.Cos(a) * l, cy + 12f + Mathf.Sin(a) * l, f == 0 ? 3f : 2f, f == 0 ? 1f : 0.7f, 0.35f, 0.3f, 0.15f);
                    }
                    if (f == 0) Disco(luz, cx, cy + 12f, 4f, 1.05f, 0.7f);
                    var capa = new PixelCanvas(W, H);
                    Pintar(capa, luz, espectro, umbralEspectro);
                    for (int i = 0; i < capa.Pixels.Length; i++) if (capa.Pixels[i].a > 0) canvas.Pixels[i] = capa.Pixels[i];
                }
                // Motas verde-espectral que ascienden balanceándose (más rápidas que el humo).
                for (int i = 0; i < 22; i++)
                {
                    float nace = Rnd(i, 0, 87) * 4.5f;
                    float a = f - nace;
                    if (a < 0f) continue;
                    float vida = 4.5f + Rnd(i, 1, 87) * 3.5f;
                    if (a > vida) continue;
                    float mx = cx + (Rnd(i, 2, 87) - 0.5f) * 30f + Mathf.Sin(a * 1.2f + i * 2f) * 2.5f;
                    float my = cy + 6f + Rnd(i, 3, 87) * 14f + a * (4f + Rnd(i, 4, 87) * 2.5f);
                    int ix = Mathf.FloorToInt(mx), iy = Mathf.FloorToInt(my);
                    bool grande = i % 3 == 0 && a < vida * 0.7f;
                    PonerPix(canvas, ix, iy, mota); PonerPix(canvas, ix, iy - 1, motaCola);
                    if (grande)
                    {
                        PonerPix(canvas, ix + 1, iy, mota); PonerPix(canvas, ix, iy + 1, mota); PonerPix(canvas, ix + 1, iy + 1, motaNucleo);
                        PonerPix(canvas, ix + 1, iy - 1, motaCola); PonerPix(canvas, ix, iy - 2, motaCola);
                    }
                    else if (a < vida * 0.5f) PonerPix(canvas, ix, iy + 1, motaNucleo);
                }
                QuitarSueltos(canvas);
                anim.Frames.Add(canvas);
            }
            return anim;
        }

        static void PonerPix(PixelCanvas c, int x, int y, Color32 col)
        {
            if (c.InBounds(x, y)) c.Pixels[y * c.Width + x] = col;
        }

        /// <summary>Marca en <paramref name="id"/> los píxeles de una bocanada (círculo mordido por ruido que se deshilacha).</summary>
        static void MarcarHumo(int[] id, int w, int h, int indice, float cx, float cy, float r, float erosion, int semilla)
        {
            int x0 = Mathf.FloorToInt(cx - r - 2), x1 = Mathf.CeilToInt(cx + r + 2);
            int y0 = Mathf.FloorToInt(cy - r - 2), y1 = Mathf.CeilToInt(cy + r + 2);
            for (int y = Mathf.Max(0, y0); y <= Mathf.Min(h - 1, y1); y++)
            {
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(w - 1, x1); x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float ruido = PixelCanvas.ValueNoise(x * 0.36f, y * 0.36f, 0, semilla);
                    // Al deshacerse encoge y se muerde por el borde con ruido grueso (grumos, no píxeles sueltos).
                    float muerde = erosion * PixelCanvas.ValueNoise(x * 0.2f + 3f, y * 0.2f, 0, semilla + 7);
                    float rr = r * (0.82f + 0.3f * ruido) * (1f - 1.1f * muerde);
                    if (dx * dx + dy * dy > rr * rr) continue;
                    id[y * w + x] = indice;
                }
            }
        }
    }
}
