using System.Collections.Generic;
using UnityEngine;

namespace Abismo.EditorTools
{
    /// <summary>Sprite de interfaz. Border = 9-slice (izquierda, abajo, derecha, arriba) en píxeles; 0 = sin 9-slice.</summary>
    public sealed class UISprite
    {
        public string Name;
        public PixelCanvas Canvas;
        public Vector4 Border;
    }

    /// <summary>
    /// Interfaz al estilo de Blasphemous: marcos de metal oscuro con filigrana de oro, barras de sangre y de
    /// revelación, frascos de láudano, el medallón con la escafandra del Ahogado. Se dibuja a 640×360 y el
    /// juego la escala ×2/×3, así que cada píxel cuenta.
    /// </summary>
    public static class UIArt
    {
        static Color32 C(string hex) => PixelCanvas.Hex(hex);

        static readonly Color32 Ink = C("080607");
        static readonly Color32 GoldDark = C("5a4218");
        static readonly Color32 Gold = C("8f6c2a");
        static readonly Color32 GoldLight = C("c9a24f");
        static readonly Color32 GoldShine = C("f2d78a");
        static readonly Color32 Iron = C("1c1a1e");
        static readonly Color32 IronLight = C("34303a");
        static readonly Color32 Panel = C("0e0c10");

        public static List<UISprite> All() => new List<UISprite>
        {
            Portrait(), BarFrame(), BarFill("ui_barra_vida", new[] { "3a0810", "6e0f18", "9c1a24", "b82a30", "e05a50", "8a1a22" }),
            BarFill("ui_barra_revelacion", new[] { "0c2e2a", "1f7a68", "4fd6a4", "a8ffe0" }),
            BarFill("ui_barra_fondo", new[] { "060507", "0b090c", "0e0b0f", "100d11", "130f14", "0a080b" }),
            BarFill("ui_barra_rastro", new[] { "8a7a50", "c8b27a", "e8d8a8", "e8d8a8", "f6ecc8", "b8a070" }),
            Flask(true), Flask(false), CoinIcon(), SmallFrame("ui_marco_oro", 24, 16, 6), SmallFrame("ui_aviso", 24, 16, 6),
            PanelSprite(), Separator(), BossFrame(), Vignette(), Glyph("ui_signo"), KeyCap(),
        };

        // ------------------------------------------------------------------
        // Utilidades
        // ------------------------------------------------------------------

        static void Px(PixelCanvas c, int x, int y, Color32 col) => c.Set(x, y, col);

        static void HLine(PixelCanvas c, int x0, int x1, int y, Color32 col)
        {
            for (int x = x0; x <= x1; x++) c.Set(x, y, col);
        }

        static void VLine(PixelCanvas c, int x, int y0, int y1, Color32 col)
        {
            for (int y = y0; y <= y1; y++) c.Set(x, y, col);
        }

        static void Fill(PixelCanvas c, int x0, int y0, int x1, int y1, Color32 col)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++) c.Set(x, y, col);
        }

        /// <summary>Marco dorado biselado de 3 px (tinta, oro con luz arriba-izquierda, sombra abajo-derecha).</summary>
        static void GoldBox(PixelCanvas c, int x0, int y0, int x1, int y1)
        {
            // Contorno de tinta.
            HLine(c, x0, x1, y0, Ink); HLine(c, x0, x1, y1, Ink); VLine(c, x0, y0, y1, Ink); VLine(c, x1, y0, y1, Ink);
            // Oro: claro arriba/izquierda, oscuro abajo/derecha.
            HLine(c, x0 + 1, x1 - 1, y1 - 1, GoldLight); VLine(c, x0 + 1, y0 + 1, y1 - 1, GoldLight);
            HLine(c, x0 + 1, x1 - 1, y0 + 1, GoldDark); VLine(c, x1 - 1, y0 + 1, y1 - 1, GoldDark);
            Px(c, x0 + 1, y1 - 1, GoldShine);
            // Línea interior oscura.
            HLine(c, x0 + 2, x1 - 2, y1 - 2, Ink); HLine(c, x0 + 2, x1 - 2, y0 + 2, Ink);
            VLine(c, x0 + 2, y0 + 2, y1 - 2, Ink); VLine(c, x1 - 2, y0 + 2, y1 - 2, Ink);
        }

        /// <summary>Remate de esquina: tres lóbulos de oro (trébol gótico).</summary>
        static void Trefoil(PixelCanvas c, int x, int y, int dx, int dy)
        {
            Px(c, x, y, GoldLight);
            Px(c, x + dx, y, Gold); Px(c, x, y + dy, Gold);
            Px(c, x + dx * 2, y, GoldDark); Px(c, x, y + dy * 2, GoldDark);
            Px(c, x + dx, y + dy, GoldShine);
            Px(c, x + dx * 2, y + dy, Ink); Px(c, x + dx, y + dy * 2, Ink);
        }

        // ------------------------------------------------------------------
        // Piezas
        // ------------------------------------------------------------------

        static UISprite BarFrame()
        {
            // 32×12, interior de 6 px de alto (filas 3..8). Puntas góticas en los extremos.
            var c = new PixelCanvas(32, 12);
            GoldBox(c, 2, 0, 29, 11);
            // Vaciar el interior (la barra se ve detrás).
            for (int y = 3; y <= 8; y++)
                for (int x = 5; x <= 26; x++) c.Set(x, y, PixelCanvas.Clear);
            // Puntas.
            for (int s = 0; s < 2; s++)
            {
                int x = s == 0 ? 0 : 31, d = s == 0 ? 1 : -1;
                Px(c, x, 5, Ink); Px(c, x, 6, Ink);
                Px(c, x + d, 4, Ink); Px(c, x + d, 7, Ink); Px(c, x + d, 5, GoldLight); Px(c, x + d, 6, Gold);
            }
            return new UISprite { Name = "ui_barra_marco", Canvas = c, Border = new Vector4(5, 3, 5, 3) };
        }

        static UISprite BarFill(string name, string[] rows)
        {
            var c = new PixelCanvas(8, rows.Length);
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < 8; x++) c.Set(x, y, C(rows[y]));
            return new UISprite { Name = name, Canvas = c, Border = new Vector4(1, 0, 1, 0) };
        }

        static UISprite Flask(bool full)
        {
            var c = new PixelCanvas(10, 14);
            var glass = C("8fb8b0");
            var glassDark = C("3f5a58");
            // Cuerpo redondo (filas 0..8) y cuello (9..11), corcho (12..13).
            for (int y = 0; y <= 8; y++)
                for (int x = 0; x < 10; x++)
                {
                    float dx = x + 0.5f - 5f, dy = y + 0.5f - 4.5f;
                    float d = dx * dx / 20f + dy * dy / 22f;
                    if (d > 1f) continue;
                    bool edge = d > 0.62f;
                    Color32 col = edge ? (dx < 0 ? glass : glassDark) : Panel;
                    if (!edge && full)
                    {
                        float level = y + (x % 2) * 0.1f;
                        col = level < 2.5f ? C("5a1408") : level < 5f ? C("a0381a") : level < 6f ? C("e0702a") : C("2a1010");
                        if (y >= 6) col = Panel;
                        if (x == 3 && y == 4) col = C("ffc070");
                    }
                    c.Set(x, y, col);
                }
            for (int y = 9; y <= 11; y++) { c.Set(4, y, glass); c.Set(5, y, glassDark); }
            Fill(c, 3, 12, 6, 13, C("7a5a38"));
            c.Set(3, 13, C("a07a50"));
            // Contorno de tinta alrededor.
            var copy = (Color32[])c.Pixels.Clone();
            for (int y = 0; y < 14; y++)
                for (int x = 0; x < 10; x++)
                {
                    if (copy[y * 10 + x].a > 0) continue;
                    bool near = false;
                    for (int k = 0; k < 4 && !near; k++)
                    {
                        int nx = x + (k == 0 ? 1 : k == 1 ? -1 : 0), ny = y + (k == 2 ? 1 : k == 3 ? -1 : 0);
                        if (nx >= 0 && ny >= 0 && nx < 10 && ny < 14 && copy[ny * 10 + nx].a > 0) near = true;
                    }
                    if (near) c.Set(x, y, Ink);
                }
            if (!full)
            {
                // Vacío: cristal apagado.
                for (int i = 0; i < c.Pixels.Length; i++)
                {
                    var p = c.Pixels[i];
                    if (p.a == 0 || (p.r == Ink.r && p.g == Ink.g)) continue;
                    c.Pixels[i] = PixelCanvas.Lerp(p, C("1a1a1e"), 0.55f);
                }
            }
            return new UISprite { Name = full ? "ui_frasco_lleno" : "ui_frasco_vacio", Canvas = c };
        }

        static UISprite CoinIcon()
        {
            var c = new PixelCanvas(12, 12);
            for (int y = 0; y < 12; y++)
                for (int x = 0; x < 12; x++)
                {
                    float dx = x + 0.5f - 6f, dy = y + 0.5f - 6f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d > 5.6f) continue;
                    Color32 col = d > 4.6f ? Ink : d > 3.6f ? (dx - dy < 0 ? GoldLight : GoldDark) : (dx + dy < -1.5f ? GoldLight : Gold);
                    c.Set(x, y, col);
                }
            // Signo de Dagón (un pez estilizado) troquelado.
            c.Set(5, 6, GoldDark); c.Set(6, 6, GoldDark); c.Set(7, 5, GoldDark); c.Set(7, 7, GoldDark); c.Set(4, 6, GoldDark);
            c.Set(3, 8, GoldShine); c.Set(4, 9, GoldShine);
            return new UISprite { Name = "ui_oro", Canvas = c };
        }

        static UISprite SmallFrame(string name, int w, int h, int border)
        {
            var c = new PixelCanvas(w, h);
            Fill(c, 1, 1, w - 2, h - 2, PixelCanvas.WithAlpha(Panel, 0.92f));
            GoldBox(c, 0, 0, w - 1, h - 1);
            Fill(c, 3, 3, w - 4, h - 4, PixelCanvas.WithAlpha(Panel, 0.92f));
            Trefoil(c, 1, h - 2, 1, -1);
            Trefoil(c, w - 2, h - 2, -1, -1);
            Trefoil(c, 1, 1, 1, 1);
            Trefoil(c, w - 2, 1, -1, 1);
            return new UISprite { Name = name, Canvas = c, Border = new Vector4(border, border - 1, border, border - 1) };
        }

        /// <summary>Tecla de hueso con bisel (14×14) para el aviso "[E] Rezar"; la letra la pone el texto del HUD.</summary>
        static UISprite KeyCap()
        {
            const int s = 14;
            var c = new PixelCanvas(s, s);
            var bone = C("d8cfb4");
            var boneLight = C("f4eedb");
            var boneDark = C("9a8f74");
            var boneDeep = C("5e5644");
            Fill(c, 1, 0, s - 2, s - 1, Ink);
            Fill(c, 0, 1, s - 1, s - 2, Ink);
            Fill(c, 1, 1, s - 2, s - 2, boneDeep);                 // canto inferior (la tecla "sobresale")
            Fill(c, 1, 3, s - 2, s - 2, boneDark);
            Fill(c, 2, 3, s - 3, s - 3, bone);
            Fill(c, 2, s - 3, s - 3, s - 3, boneLight);            // luz arriba
            Fill(c, 2, 4, 2, s - 3, boneLight);                    // luz a la izquierda
            c.Set(1, s - 2, Ink);
            c.Set(s - 2, s - 2, Ink);
            return new UISprite { Name = "ui_tecla", Canvas = c, Border = new Vector4(4, 4, 4, 4) };
        }

        static UISprite PanelSprite()
        {
            const int s = 48;
            var c = new PixelCanvas(s, s);
            // Fondo de piedra oscura con un poco de textura en grupos.
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float n = PixelCanvas.ValueNoise(x / 4f, y / 4f, s / 4, 301);
                    c.Set(x, y, n > 0.62f ? C("15121a") : n < 0.3f ? C("0a080c") : Panel);
                }
            GoldBox(c, 0, 0, s - 1, s - 1);
            // Segundo filete fino de oro por dentro.
            HLine(c, 5, s - 6, s - 6, GoldDark); HLine(c, 5, s - 6, 5, GoldDark);
            VLine(c, 5, 5, s - 6, GoldDark); VLine(c, s - 6, 5, s - 6, GoldDark);
            // Esquinas ornamentadas.
            for (int k = 0; k < 4; k++)
            {
                int cx = k % 2 == 0 ? 0 : s - 1, cy = k < 2 ? 0 : s - 1;
                int dx = k % 2 == 0 ? 1 : -1, dy = k < 2 ? 1 : -1;
                for (int i = 0; i < 9; i++)
                {
                    Px(c, cx + dx * i, cy + dy * 3, Gold);
                    Px(c, cx + dx * 3, cy + dy * i, Gold);
                }
                Fill(c, Mathf.Min(cx, cx + dx * 6), Mathf.Min(cy, cy + dy * 6), Mathf.Max(cx, cx + dx * 6), Mathf.Max(cy, cy + dy * 6), Ink);
                Fill(c, Mathf.Min(cx + dx, cx + dx * 5), Mathf.Min(cy + dy, cy + dy * 5), Mathf.Max(cx + dx, cx + dx * 5), Mathf.Max(cy + dy, cy + dy * 5), Gold);
                Fill(c, Mathf.Min(cx + dx * 2, cx + dx * 4), Mathf.Min(cy + dy * 2, cy + dy * 4), Mathf.Max(cx + dx * 2, cx + dx * 4), Mathf.Max(cy + dy * 2, cy + dy * 4), Ink);
                Px(c, cx + dx * 3, cy + dy * 3, GoldShine);
                Px(c, cx + dx * 7, cy + dy * 7, GoldLight);
                Px(c, cx + dx * 8, cy + dy * 6, GoldDark);
                Px(c, cx + dx * 6, cy + dy * 8, GoldDark);
            }
            return new UISprite { Name = "ui_panel", Canvas = c, Border = new Vector4(12, 12, 12, 12) };
        }

        static UISprite Separator()
        {
            const int w = 160, h = 9;
            var c = new PixelCanvas(w, h);
            int mid = w / 2;
            for (int x = 6; x < w - 6; x++)
            {
                float t = Mathf.Abs(x - mid) / (float)(mid - 6);
                if (t < 0.08f) continue;
                c.Set(x, 4, t > 0.85f ? GoldDark : Gold);
                if (t < 0.6f) c.Set(x, 5, GoldLight);
                if (t < 0.3f) c.Set(x, 3, GoldDark);
            }
            // Volutas en los extremos.
            foreach (int side in new[] { -1, 1 })
            {
                int ex = mid + side * (mid - 7);
                c.Set(ex, 4, GoldLight); c.Set(ex - side, 5, Gold); c.Set(ex - side * 2, 6, GoldDark); c.Set(ex - side, 3, Gold); c.Set(ex - side * 2, 2, GoldDark);
            }
            // Estrella del Signo en el centro.
            int[,] star =
            {
                { 0, 0, 0, 0, 1, 0, 0, 0, 0 },
                { 0, 0, 0, 1, 2, 1, 0, 0, 0 },
                { 1, 1, 1, 2, 3, 2, 1, 1, 1 },
                { 0, 1, 2, 2, 3, 2, 2, 1, 0 },
                { 0, 0, 1, 2, 3, 2, 1, 0, 0 },
                { 0, 1, 2, 1, 2, 1, 2, 1, 0 },
                { 0, 1, 1, 0, 1, 0, 1, 1, 0 },
            };
            var teal = new[] { PixelCanvas.Clear, Gold, GoldLight, C("6cf7c8") };
            for (int y = 0; y < 7; y++)
                for (int x = 0; x < 9; x++)
                    if (star[y, x] > 0) c.Set(mid - 4 + x, 7 - y, teal[star[y, x]]);
            return new UISprite { Name = "ui_separador", Canvas = c };
        }

        static UISprite BossFrame()
        {
            // 64×14: interior de 6 px (filas 4..9); extremos con cuernos y una gema.
            var c = new PixelCanvas(64, 14);
            GoldBox(c, 6, 1, 57, 12);
            for (int y = 4; y <= 9; y++)
                for (int x = 9; x <= 54; x++) c.Set(x, y, PixelCanvas.Clear);
            for (int s = 0; s < 2; s++)
            {
                int x0 = s == 0 ? 0 : 63, d = s == 0 ? 1 : -1;
                // Cuerno curvo.
                Px(c, x0 + d * 5, 13, Ink); Px(c, x0 + d * 4, 12, Gold); Px(c, x0 + d * 3, 11, GoldLight); Px(c, x0 + d * 2, 10, Gold);
                Px(c, x0 + d * 1, 8, GoldDark); Px(c, x0 + d * 2, 9, Gold); Px(c, x0, 7, Ink); Px(c, x0 + d, 7, Gold);
                Px(c, x0 + d * 2, 4, GoldDark); Px(c, x0 + d * 3, 3, Gold); Px(c, x0 + d * 4, 2, GoldLight); Px(c, x0 + d * 5, 1, Ink);
                Px(c, x0 + d * 1, 5, Gold); Px(c, x0 + d * 1, 6, GoldDark);
                // Gema carmesí.
                Px(c, x0 + d * 4, 6, C("c0283a")); Px(c, x0 + d * 4, 7, C("ff6a6a")); Px(c, x0 + d * 5, 6, C("6a0a14")); Px(c, x0 + d * 5, 7, C("a0182a"));
            }
            return new UISprite { Name = "ui_jefe_marco", Canvas = c, Border = new Vector4(12, 4, 12, 4) };
        }

        static UISprite Vignette()
        {
            const int s = 128;
            var c = new PixelCanvas(s, s);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x + 0.5f) / s * 2f - 1f, dy = (y + 0.5f) / s * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx * 0.9f + dy * dy * 1.1f);
                    float a = Mathf.Clamp01((d - 0.55f) / 0.75f);
                    a = a * a * (3f - 2f * a);
                    c.Set(x, y, new Color32(0, 0, 0, (byte)Mathf.RoundToInt(a * 255f)));
                }
            return new UISprite { Name = "ui_vineta", Canvas = c };
        }

        static UISprite Glyph(string name)
        {
            // Signo Antiguo pequeño (16×16) para el título y la pantalla de muerte.
            var c = new PixelCanvas(16, 16);
            for (int i = 0; i < 10; i++)
            {
                float a0 = (90f + i * 36f) * Mathf.Deg2Rad, a1 = (90f + (i + 1) * 36f) * Mathf.Deg2Rad;
                float r0 = i % 2 == 0 ? 7.5f : 3.2f, r1 = i % 2 == 0 ? 3.2f : 7.5f;
                for (float t = 0f; t <= 1f; t += 0.05f)
                {
                    float r = Mathf.Lerp(r0, r1, t), a = Mathf.Lerp(a0, a1, t);
                    c.Set(Mathf.RoundToInt(8f + Mathf.Cos(a) * r - 0.5f), Mathf.RoundToInt(8f + Mathf.Sin(a) * r - 0.5f), C("6cf7c8"));
                }
            }
            c.Set(7, 7, C("d8fff0")); c.Set(7, 8, C("d8fff0")); c.Set(8, 7, C("6cf7c8")); c.Set(8, 8, C("6cf7c8"));
            return new UISprite { Name = name, Canvas = c };
        }

        static UISprite Portrait()
        {
            const int s = 44;
            var c = new PixelCanvas(s, s);
            float cx = 22f, cy = 22f;
            // Fondo del medallón: agua oscura con un brillo verdoso.
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d > 21.5f) continue;
                    Color32 col;
                    if (d > 20.5f) col = Ink;
                    else if (d > 18.5f) col = (dx - dy) < -4f ? GoldLight : (dx - dy) > 4f ? GoldDark : Gold;
                    else if (d > 17.5f) col = Ink;
                    else col = dy > 4f ? C("0c1a1a") : C("081212");
                    c.Set(x, y, col);
                }
            // Remaches en el aro.
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f * Mathf.Deg2Rad;
                int px = Mathf.RoundToInt(cx + Mathf.Cos(a) * 19.5f - 0.5f), py = Mathf.RoundToInt(cy + Mathf.Sin(a) * 19.5f - 0.5f);
                c.Set(px, py, GoldShine);
            }
            // Escafandra de latón con la mirilla iluminada.
            var sc = new ShadedCanvas(s, s, 0f, 0f);
            var brass = new PixelMaterial(Ramp.Make("a07a34", 5, 0.12f, 0.3f, 1.6f)) { Gloss = 0.7f, Rim = 0.5f, Dither = 0f };
            var brassDark = new PixelMaterial(Ramp.Make("5f4520", 4, 0.1f)) { Gloss = 0.4f, Dither = 0f };
            var glassGlow = PixelMaterial.Glow("4fd6a4");
            var glassCore = PixelMaterial.Glow("c8fff0");
            int g = sc.NewGroup();
            sc.Ellipse(new Vector2(22f, 22f), 11f, 11.5f, 0f, brass, 1f, 0f, g);
            sc.Poly(new[] { new Vector2(11f, 8f), new Vector2(33f, 8f), new Vector2(31f, 13f), new Vector2(13f, 13f) }, brassDark, 1.2f, 1f, 0f, g);
            sc.Ellipse(new Vector2(25f, 22f), 6f, 6f, 0f, brassDark, 1.3f, 0f, g);
            sc.Ellipse(new Vector2(25f, 22f), 4.4f, 4.4f, 0f, glassGlow, 1.4f, 0f, g);
            sc.Ellipse(new Vector2(24f, 23f), 1.8f, 1.8f, 0f, glassCore, 1.5f, 0f, g);
            sc.Ellipse(new Vector2(14f, 25f), 2f, 2.6f, 0f, brassDark, 1.3f, 0f, g);
            var helmet = sc.Render(out _);
            for (int i = 0; i < helmet.Pixels.Length; i++)
            {
                if (helmet.Pixels[i].a == 0) continue;
                int x = i % s, y = i / s;
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                if (dx * dx + dy * dy < 17.5f * 17.5f) c.Pixels[i] = helmet.Pixels[i];
            }
            return new UISprite { Name = "ui_retrato", Canvas = c };
        }
    }
}
